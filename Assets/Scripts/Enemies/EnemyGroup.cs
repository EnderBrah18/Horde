using System.Collections.Generic;
using UnityEngine;

public class EnemyGroup : MonoBehaviour
{
    public List<Enemy> members = new List<Enemy>();
    public Enemy Leader;
    public bool Marching;
    public int minSize = 5;     // tamanho mínimo para marchar (ou ter um Tank)
    public int expected;        // total planejado do grupo
    public int spawned;
    public GroupTactic tactic = GroupTactic.Tactical;
    public bool randomizeTactic = true;
    [Range(0f, 1f)] public float tacticalGroupChance = 0.7f;
    [Range(0f, 1f)] public float loneWolfChance = 0.1f;
    public Structure CommandTarget;
    [Min(0f)] public float cohesionRadius = 8f;
    [Min(0f)] public float leaderLeashRadius = 14f;
    [Min(0f)] public float cohesionStrength = 0.8f;
    [Min(0f)] public float groupMergeRadius = 16f;
    [Min(1f)] public float memberDetachRadius = 32f;
    [Min(0f)] public float memberDetachDelay = 2f;
    [Min(1f)] public float formationSpacing = 2.5f;
    [Min(1f)] public float formationRearDistance = 4f;
    [Min(0f)] public float swarmSpread = 7f;
    readonly Dictionary<Enemy, float> separatedFor = new Dictionary<Enemy, float>();

    bool tacticChosen;
    bool absorbed;
    readonly HashSet<Enemy> socialProfilesApplied = new HashSet<Enemy>();

    float marchDelay = -1f;
    float panic = -1f;
    float mergeCheckTimer;

    const float GroupMergeCheckInterval = 0.4f;

    public void Add(Enemy e)
    {
        members.Add(e);
        spawned++;
    }

    public void ApplySocialProfile(Enemy e)
    {
        if (e == null || !socialProfilesApplied.Add(e)) return;

        if (!tacticChosen)
        {
            if (randomizeTactic)
                tactic = Random.value < tacticalGroupChance ? GroupTactic.Tactical : GroupTactic.Aggressive;
            tacticChosen = true;
        }

        if (e.type == EnemyType.Tank)
        {
            e.SetSocialRole(EnemySocialRole.Soldier);
            return;
        }
        e.SetSocialRole(EnemySocialRole.Soldier);
        bool rebelliousTooLarge = e.socialProfile != null &&
            e.socialProfile.behavior == EnemySocialBehavior.Rebellious && members.Count > 3;
        if (rebelliousTooLarge || Random.value < loneWolfChance)
            e.BecomeLoneWolf();
    }

    public void Remove(Enemy e)
    {
        separatedFor.Remove(e);
        members.Remove(e);
        if (Leader == e) CommandTarget = null;
        if (Leader == e) Leader = null;
    }

    public Vector3 CohesionSteering(Enemy member)
    {
        if (!Marching || member == null || Leader == null || members.Count < 2)
            return Vector3.zero;

        Vector3 anchor;
        if (member == Leader)
        {
            Vector3 center = Vector3.zero;
            int count = 0;
            foreach (var other in members)
            {
                if (other == null || other == Leader || other.state == EState.Flee) continue;
                center += other.transform.position;
                count++;
            }
            if (count == 0) return Vector3.zero;
            anchor = center / count;
            float distance = Vector3.Distance(member.transform.position, anchor);
            if (distance <= leaderLeashRadius) return Vector3.zero;
            return HorizontalDirection(anchor - member.transform.position) *
                   Mathf.Clamp01((distance - leaderLeashRadius) / Mathf.Max(0.1f, cohesionRadius));
        }

        if (Leader.state == EState.Flee) return Vector3.zero;
        anchor = Leader.transform.position;
        float followerDistance = Vector3.Distance(member.transform.position, anchor);
        if (followerDistance <= cohesionRadius) return Vector3.zero;
        return HorizontalDirection(anchor - member.transform.position) *
               Mathf.Clamp01((followerDistance - cohesionRadius) / Mathf.Max(0.1f, cohesionRadius));
    }

    public Vector3 TravelSteering(Enemy member, Vector3 route, bool engagingStructure)
    {
        if (Leader == null || member.type == EnemyType.Tank || engagingStructure)
            return route;

        Enemy anchor = Leader;
        if (tactic == GroupTactic.Tactical)
        {
            anchor = null;
            float furthestProgress = float.NegativeInfinity;
            foreach (var candidate in members)
            {
                if (candidate == null || candidate.type != EnemyType.Tank || candidate.state == EState.Flee) continue;
                float progress = Vector3.Dot(candidate.transform.position - member.transform.position, route.normalized);
                if (progress > furthestProgress) { furthestProgress = progress; anchor = candidate; }
            }
            if (anchor == null) return route;
        }
        else if (member == Leader) return route;

        int slot = 0;
        foreach (var other in members)
        {
            if (other == member) break;
            if (other != null && other.type != EnemyType.Tank) slot++;
        }
        Vector3 forward = anchor.transform.forward;
        forward.y = 0f;
        forward.Normalize();
        Vector3 right = Vector3.Cross(Vector3.up, forward);
        if (tactic == GroupTactic.Tactical)
        {
            int row = slot / 3;
            int column = slot % 3 - 1;
            Vector3 destination = anchor.transform.position - forward * (formationRearDistance + row * formationSpacing)
                                  + right * column * formationSpacing;
            Vector3 delta = destination - member.transform.position;
            delta.y = 0f;
            // Faster members wait rather than walking backwards or overtaking the front line.
            float gap = Vector3.Dot(delta, forward);
            if (gap <= 0.6f) return Vector3.zero;
            var grid = GridWorld.I;
            Vector2Int cell = grid.WorldToCell(destination);
            if (!grid.InBounds(cell) || grid.IsHardBlocked(cell) || grid.Get(cell) != null) return route;
            // Formation is a gentle preference; never reverse the route to regain a slot.
            Vector3 formationDirection = Vector3.Dot(delta.normalized, route.normalized) > 0f
                ? route + delta.normalized * 0.4f : route;
            float matchingSpeed = anchor.speed / Mathf.Max(0.1f, member.speed);
            float pace = Mathf.Clamp01(matchingSpeed + (gap - formationSpacing) / (formationSpacing * 3f));
            return formationDirection.normalized * pace;
        }

        // Stable, different offsets keep the swarm broad without frame-to-frame jitter.
        float angle = slot * 2.399963f;
        Vector3 spreadPosition = Leader.transform.position +
                                 (right * Mathf.Cos(angle) + forward * Mathf.Sin(angle)) * swarmSpread;
        Vector3 spreadDirection = HorizontalDirection(spreadPosition - member.transform.position);
        return route + spreadDirection * 0.55f + CohesionSteering(member) * cohesionStrength * 0.3f;
    }

    static Vector3 HorizontalDirection(Vector3 direction)
    {
        direction.y = 0f;
        return direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector3.zero;
    }

    void Update()
    {
        if (absorbed) return;
        if (Game.I != null && Game.I.lost) return;

        members.RemoveAll(m => m == null);
        if (members.Count == 0)
        {
            if (spawned >= expected) Destroy(gameObject);
            return;
        }

        // 1) Reunião (Rally)
        if (!Marching)
        {
            bool hasTank = members.Exists(m => m.type == EnemyType.Tank);
            if (marchDelay < 0f && (members.Count >= minSize || hasTank || spawned >= expected))
                marchDelay = 1.5f;

            if (marchDelay >= 0f)
            {
                marchDelay -= Time.deltaTime;
                if (marchDelay <= 0f)
                {
                    Marching = true;
                    AssignLeader();
                    foreach (var m in members) m.state = EState.March;
                }
            }
            return;
        }

        // 2) Deserção por queda do grupo (Cobiçoso foge com metade, etc.)
        if (spawned >= expected)
        {
            foreach (var m in members.ToArray())
                if (m != null && m.braveza < 0.99f && members.Count < expected * m.braveza)
                    m.Desert();
        }

        // 3) Perda do líder: pânico, depois decide entre assumir ou desertar
        if (Leader == null && members.Count > 0)
        {
            if (panic < 0f)
            {
                panic = 1.5f;
                foreach (var m in members.ToArray())
                    if (m != null && m.braveza < 0.3f) m.Desert();       // Covarde foge na hora
            }
            else
            {
                panic -= Time.deltaTime;
                if (panic <= 0f)
                {
                    foreach (var m in members.ToArray())
                        if (m != null && m.braveza < 0.7f && Random.value < 0.5f) m.Desert();
                    AssignLeader();
                    panic = -1f;
                }
            }
        }

        UpdateSeparatedMembers();
        mergeCheckTimer -= Time.deltaTime;
        if (mergeCheckTimer <= 0f)
        {
            mergeCheckTimer = GroupMergeCheckInterval;
            RecruitNearbyMembers();
            TryMergeWithNearbyGroup();
        }
    }

    void UpdateSeparatedMembers()
    {
        if (Leader == null) return;
        foreach (var member in members.ToArray())
        {
            if (member == null || member == Leader) continue;
            Vector3 delta = member.transform.position - Leader.transform.position;
            delta.y = 0f;
            if (delta.sqrMagnitude <= memberDetachRadius * memberDetachRadius)
            {
                separatedFor.Remove(member);
                continue;
            }
            separatedFor.TryGetValue(member, out float elapsed);
            elapsed += Time.deltaTime;
            separatedFor[member] = elapsed;
            if (elapsed >= memberDetachDelay) member.LeaveSeparatedGroup();
        }
    }

    void RecruitNearbyMembers()
    {
        if (absorbed || Leader == null || groupMergeRadius <= 0f) return;
        foreach (var enemy in Enemy.All)
        {
            if (enemy == null || !enemy.AvailableForRecruitment) continue;
            Vector3 delta = enemy.transform.position - Leader.transform.position;
            delta.y = 0f;
            if (delta.sqrMagnitude <= groupMergeRadius * groupMergeRadius)
                enemy.JoinNearbyGroup(this);
        }
    }

    void TryMergeWithNearbyGroup()
    {
        if (!Marching || Leader == null || expected <= 0 || spawned < expected || members.Count == 0)
            return;

        EnemyGroup[] groups = FindObjectsByType<EnemyGroup>(FindObjectsSortMode.None);
        Vector3 ownCenter = GroupCenter();
        foreach (var other in groups)
        {
            if (other == null || other == this || other.absorbed || !other.Marching || other.Leader == null ||
                other.expected <= 0 || other.spawned < other.expected || other.members.Count == 0)
                continue;

            float mergeRadius = Mathf.Max(groupMergeRadius, other.groupMergeRadius);
            if (mergeRadius <= 0f) continue;
            if ((GroupCenter(other.members) - ownCenter).sqrMagnitude > mergeRadius * mergeRadius)
                continue;

            float ownBravery = Leader.braveza;
            float otherBravery = other.Leader.braveza;
            if (Mathf.Approximately(ownBravery, otherBravery)) continue;
            bool otherLeads = otherBravery > ownBravery;
            EnemyGroup survivor = otherLeads ? other : this;
            EnemyGroup donor = otherLeads ? this : other;
            Enemy winningLeader = otherLeads ? other.Leader : Leader;
            survivor.Absorb(donor, winningLeader);
            return;
        }
    }

    Vector3 GroupCenter()
    {
        return GroupCenter(members);
    }

    static Vector3 GroupCenter(List<Enemy> groupMembers)
    {
        Vector3 center = Vector3.zero;
        int count = 0;
        foreach (var member in groupMembers)
        {
            if (member == null || member.state == EState.Flee) continue;
            center += member.transform.position;
            count++;
        }
        return count > 0 ? center / count : Vector3.zero;
    }

    void Absorb(EnemyGroup donor, Enemy winningLeader)
    {
        if (donor == null || donor == this || donor.absorbed) return;

        int absorbedMembers = donor.members.Count;
        foreach (var member in donor.members)
        {
            if (member == null || members.Contains(member)) continue;
            members.Add(member);
            member.group = this;
            member.state = EState.March;
        }

        expected += donor.expected;
        spawned += donor.spawned;
        Marching = true;
        Leader = winningLeader != null && members.Contains(winningLeader) ? winningLeader : Leader;
        RefreshLeaderRolesAndCrown();

        donor.members.Clear();
        donor.Leader = null;
        donor.CommandTarget = null;
        donor.absorbed = true;

        string mergeDetails = $"{name} absorveu {donor.name}; líder {Leader.name} (bravura {Leader.braveza:0.00}); " +
                              $"{members.Count} inimigos no grupo";
        if (Game.I != null)
            Game.I.RaiseGameplayEvent(GameplayEventType.EnemyGroupMerged, Leader.gameObject, members.Count, mergeDetails);
        if (Game.I != null && Game.I.debugEnemyTargeting)
            Debug.Log($"[EnemyAI][Group] {mergeDetails}; membros incorporados={absorbedMembers}", this);

        Destroy(donor.gameObject);
    }

    void RefreshLeaderRolesAndCrown()
    {
        foreach (var member in members)
        {
            if (member == null) continue;
            bool isLeader = member == Leader;
            member.SetSocialRole(isLeader ? EnemySocialRole.Leader : EnemySocialRole.Soldier);
            Transform crown = member.transform.Find("Crown");
            if (isLeader && crown == null)
            {
                var crownObject = Game.Prim(PrimitiveType.Sphere, Vector3.zero, Vector3.one * 0.35f, Color.white);
                crownObject.name = "Crown";
                crownObject.transform.SetParent(member.transform, false);
                crownObject.transform.localPosition = new Vector3(0f, 1.4f, 0f);
            }
            else if (!isLeader && crown != null)
            {
                Destroy(crown.gameObject);
            }
        }
    }

    void AssignLeader()
    {
        Enemy best = null;
        float bs = -1f;
        foreach (var m in members)
        {
            if (m == null || m.state == EState.Flee ||
                (m.type != EnemyType.Tank && m.socialProfile != null &&
                 m.socialProfile.temperament == EnemyTemperament.Berserker)) continue;
            float s = m.braveza + Random.value * 0.01f;
            if (s > bs) { bs = s; best = m; }
        }
        Leader = best;
        if (best == null)
        {
            CommandTarget = null;
            foreach (var member in members.ToArray())
                if (member != null) member.BecomeLoneWolf();
            return;
        }
        if (best != null)
        {
            RefreshLeaderRolesAndCrown();
        }
    }
}
