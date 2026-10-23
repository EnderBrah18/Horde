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

    bool tacticChosen;
    readonly HashSet<Enemy> socialProfilesApplied = new HashSet<Enemy>();

    float marchDelay = -1f;
    float panic = -1f;

    public void Add(Enemy e) { members.Add(e); spawned++; }

    public void ApplySocialProfile(Enemy e)
    {
        if (e == null || !socialProfilesApplied.Add(e)) return;

        if (!tacticChosen)
        {
            if (randomizeTactic)
                tactic = Random.value < tacticalGroupChance ? GroupTactic.Tactical : GroupTactic.Aggressive;
            tacticChosen = true;
        }

        e.SetSocialRole(EnemySocialRole.Soldier);
        bool rebelliousTooLarge = e.socialProfile != null &&
            e.socialProfile.behavior == EnemySocialBehavior.Rebellious && members.Count > 3;
        if (rebelliousTooLarge || Random.value < loneWolfChance)
            e.BecomeLoneWolf();
    }

    public void Remove(Enemy e)
    {
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

    static Vector3 HorizontalDirection(Vector3 direction)
    {
        direction.y = 0f;
        return direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector3.zero;
    }

    void Update()
    {
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
    }

    void AssignLeader()
    {
        Enemy best = null;
        float bs = -1f;
        foreach (var m in members)
        {
            if (m == null || m.state == EState.Flee) continue;
            float s = (m.type == EnemyType.Tank ? 2f : m.braveza) + Random.value * 0.01f;
            if (s > bs) { bs = s; best = m; }
        }
        Leader = best;
        if (best != null)
        {
            best.SetSocialRole(EnemySocialRole.Leader);
            foreach (var member in members)
                if (member != null && member != best) member.SetSocialRole(EnemySocialRole.Soldier);

            var crown = Game.Prim(PrimitiveType.Sphere, Vector3.zero, Vector3.one * 0.35f, Color.white);
            crown.name = "Crown";
            crown.transform.SetParent(best.transform, false);
            crown.transform.localPosition = new Vector3(0f, 1.4f, 0f);
        }
    }
}
