using System.Collections.Generic;
using UnityEngine;

public enum EnemyType { Common, Tank }
public enum Personality { Objective, Hunter }   // Objective = ObjectiveRooted
public enum EState { Rally, March, Flee }

public class Enemy : MonoBehaviour
{
    public static readonly List<Enemy> All = new List<Enemy>();

    public EnemyType type;
    public Personality personality;
    public float braveza = 1f;
    public EnemyGroup group;
    public EState state = EState.Rally;

    public float speed = 3.2f;
    public float damage = 4f;
    public float attackCooldown = 1f;
    public float attackRange = 3f;
    [Min(0f)] public float tacticRadius = 16f;
    [Min(0f)] public float damagedTargetMemory = 20f;
    public Health hp;
    public EnemySocialProfile socialProfile;

    public bool CanDesert => socialProfile == null ||
        (socialProfile.temperament != EnemyTemperament.Disciplined &&
         socialProfile.temperament != EnemyTemperament.Loyal &&
         socialProfile.temperament != EnemyTemperament.Berserker);

    float atkTimer;
    float wanderTimer;
    float damagedTargetTimer;
    float targetDecisionTimer;
    float stuckTimer;
    float recoveryTimer;
    int recoverySide = 1;
    Vector3 wanderDest;
    Vector3 home;
    Structure damagedByTarget;
    Structure selectedTarget;
    string lastTargetReason;
    bool targetDebugPending;
    bool movementDebugPending;
    bool hasDebugApproach;
    Vector2Int debugApproachCell;
    Vector3 debugApproachPosition;
    Vector3 debugRouteDirection;

    const float TargetDecisionInterval = 0.25f;
    const float StuckRecoveryDelay = 0.65f;

    void OnEnable() { All.Add(this); }
    void OnDisable()
    {
        All.Remove(this);
        if (hp != null)
        {
            hp.DamagedBy -= OnDamagedBy;
            hp.Died -= OnDied;
        }
    }

    public void Init(EnemyType t, Personality p, float b, EnemyGroup g)
    {
        type = t; personality = p; braveza = b; group = g;
        socialProfile = GetComponent<EnemySocialProfile>();
        if (socialProfile == null) socialProfile = gameObject.AddComponent<EnemySocialProfile>();
        socialProfile.RollOnSpawn();
        home = transform.position;
        hp = GetComponent<Health>();
        if (hp != null)
        {
            hp.DamagedBy -= OnDamagedBy;
            hp.DamagedBy += OnDamagedBy;
            hp.Died -= OnDied;
            hp.Died += OnDied;
        }

        if (t == EnemyType.Tank) { speed = 2.2f; damage = 14f; hp.Setup(140f); }
        else { speed = 3.2f; damage = 4f; hp.Setup(25f); }
        Color c;
        if (t == EnemyType.Tank) c = new Color(0.45f, 0f, 0f);
        else if (b >= 0.99f) c = new Color(0.9f, 0.1f, 0.1f);      // Fanático
        else if (b >= 0.4f) c = new Color(1f, 0.55f, 0f);           // Cobiçoso
        else c = new Color(1f, 0.6f, 0.85f);                        // Covarde
        GetComponent<Renderer>().material.color = c;
    }

    public void ApplyBalance(EnemyBalanceSettings balance)
    {
        if (balance == null) return;
        speed = balance.speed;
        damage = balance.damage;
        attackCooldown = balance.attackCooldown;
        attackRange = balance.attackRange;
        tacticRadius = balance.tacticRadius;
        if (hp != null) hp.Setup(balance.maxHealth);
    }

    public void SetSocialRole(EnemySocialRole role)
    {
        if (socialProfile == null) socialProfile = GetComponent<EnemySocialProfile>();
        if (socialProfile != null) socialProfile.role = role;
    }

    public void BecomeLoneWolf()
    {
        if (group != null) group.Remove(this);
        group = null;
        state = EState.March;
        SetSocialRole(EnemySocialRole.LoneWolf);
    }

    void OnDied(Health h)
    {
        if (group != null) group.Remove(this);
        Game.I.killed++;
    }

    public void Desert()
    {
        if (state == EState.Flee) return;
        if (!CanDesert) return;
        var g = group;
        group = null;
        if (g != null) g.Remove(this);
        state = EState.Flee;
        GetComponent<Renderer>().material.color = Color.gray;
        Game.I.deserted++;
    }

    void Update()
    {
        if (Game.I == null || Game.I.lost) return;
        float dt = Time.deltaTime;
        atkTimer -= dt;
        targetDecisionTimer -= dt;
        recoveryTimer = Mathf.Max(0f, recoveryTimer - dt);
        if (damagedTargetTimer > 0f)
        {
            damagedTargetTimer -= dt;
            if (damagedTargetTimer <= 0f || damagedByTarget == null || damagedByTarget.health == null ||
                damagedByTarget.health.current <= 0f)
                damagedByTarget = null;
        }

        Vector3 dir = Vector3.zero;
        float mult = 1f;
        switch (state)
        {
            case EState.Rally:
                dir = Wander(dt); mult = 0.4f;
                if (group != null && group.Marching) state = EState.March;
                break;
            case EState.March:
                dir = MarchDir();
                if (group != null && dir.sqrMagnitude > 0.0001f)
                    dir += group.CohesionSteering(this) * group.cohesionStrength;
                // Keep attackers stationary while they are in range of their target.
                if (dir.sqrMagnitude > 0.0001f) dir += Separation() * 1.5f;
                break;
            case EState.Flee:
                dir = FleeDir(); mult = 1.2f;
                break;
        }
        if (state != EState.March) dir += Separation() * 1.5f;

        if (state == EState.March && recoveryTimer > 0f && dir.sqrMagnitude > 0.0001f)
            dir = Quaternion.AngleAxis(45f * recoverySide, Vector3.up) * dir;

        Vector3 positionBeforeMove = transform.position;
        Move(dir, mult, dt);
        UpdateStuckRecovery(positionBeforeMove, dir, dt);
        if (DebugTargetingEnabled() && movementDebugPending)
        {
            Vector3 toTarget = selectedTarget != null ? selectedTarget.transform.position - transform.position : Vector3.zero;
            toTarget.y = 0f;
            Vector3 moveDirection = dir;
            moveDirection.y = 0f;
            float alignment = moveDirection.sqrMagnitude > 0.0001f && toTarget.sqrMagnitude > 0.0001f
                ? Vector3.Dot(moveDirection.normalized, toTarget.normalized)
                : 0f;
            Vector3 toApproach = hasDebugApproach ? debugApproachPosition - transform.position : Vector3.zero;
            toApproach.y = 0f;
            float approachAlignment = moveDirection.sqrMagnitude > 0.0001f && toApproach.sqrMagnitude > 0.0001f
                ? Vector3.Dot(moveDirection.normalized, toApproach.normalized)
                : 0f;
            string approachInfo = hasDebugApproach
                ? $"cell={debugApproachCell} pos={debugApproachPosition} routeDir={debugRouteDirection} alignment={approachAlignment:0.00}"
                : "none";
            Debug.Log($"[EnemyAI][Move] {name} target={TargetName(selectedTarget)} targetDistance=" +
                      $"{(selectedTarget != null ? FlatDistance(transform.position, selectedTarget.transform.position) : -1f):0.0} " +
                      $"pos={transform.position} moveDir={moveDirection.normalized} toTarget={toTarget.normalized} " +
                      $"targetAlignment={alignment:0.00} " +
                      $"approach={approachInfo}", this);
            movementDebugPending = false;
        }
    }

    Vector3 Wander(float dt)
    {
        wanderTimer -= dt;
        if (wanderTimer <= 0f)
        {
            Vector2 r = Random.insideUnitCircle * 3f;
            wanderDest = home + new Vector3(r.x, 0f, r.y);
            wanderTimer = Random.Range(1f, 2.5f);
        }
        Vector3 d = wanderDest - transform.position;
        d.y = 0f;
        return d.magnitude > 0.3f ? d.normalized : Vector3.zero;
    }

    Vector3 MarchDir()
    {
        Vector3 pos = transform.position;
        hasDebugApproach = false;
        if (targetDecisionTimer <= 0f || (selectedTarget != null && !IsValidTarget(selectedTarget)))
        {
            Structure previousTarget = selectedTarget;
            string previousReason = lastTargetReason;
            selectedTarget = PickTarget(pos);
            targetDecisionTimer = TargetDecisionInterval;
            if (DebugTargetingEnabled() &&
                (targetDebugPending || previousTarget != selectedTarget || previousReason != lastTargetReason))
            {
                LogTargetDecision(pos, previousTarget, targetDebugPending);
                movementDebugPending = true;
            }
            targetDebugPending = false;
        }
        Structure t = selectedTarget;

        if (group != null && group.Leader == this)
            group.CommandTarget = t;

        if (t != null && FlatDistance(pos, t.transform.position) <= attackRange)
        {
            TryAttack(t);
            return Vector3.zero;
        }

        if (type == EnemyType.Tank)
            return GridWorld.I.FlowDirForTank(pos); // Tank mantém o Núcleo como destino.
        if (t != null && t.kind != StructureKind.Core)
        {
            Vector2Int approachCell = GridWorld.I.AttackApproachCell(t, GetEntityId().GetHashCode(), attackRange);
            debugApproachCell = approachCell;
            debugApproachPosition = GridWorld.I.CellToWorld(approachCell);
            debugApproachPosition.y = pos.y;
            hasDebugApproach = true;
            debugRouteDirection = GridWorld.I.FlowDir(pos, approachCell);
            return debugRouteDirection;
        }
        return GridWorld.I.FlowDir(pos);
    }

    // Prioridades pessoais vêm antes da sugestão do líder; inimigos comuns não têm um alvo global fixo.
    Structure PickTarget(Vector3 pos)
    {
        // O Tank sempre prioriza o Núcleo; paredes só são atacadas quando o fluxo
        // de rota decide que atravessá-las custa menos do que contorná-las.
        if (type == EnemyType.Tank)
        {
            Structure coreTarget = FindCoreTarget();
            if (coreTarget != null)
            {
                lastTargetReason = "Tank: prioridade fixa do Núcleo";
                return coreTarget;
            }
        }

        // Primeiro escolhe o alvo pessoal mais próximo dentro do raio tático.
        // A memória de dano não deve fazer o inimigo abandonar uma ameaça local
        // mais próxima para perseguir quem o atingiu anteriormente.
        Structure personalTarget = PickPersonalPriorityTarget(pos);
        if (personalTarget != null)
        {
            lastTargetReason = "alvo pessoal mais próximo no raio tático";
            return personalTarget;
        }

        if (group != null && group.Leader != null && group.Leader != this && IsValidTarget(group.CommandTarget) &&
            ShouldFollowLeaderOrder())
        {
            lastTargetReason = "ordem do líder (sem alvo pessoal local)";
            return group.CommandTarget;
        }

        Structure perceivedTarget = FindNearestPerceivedTarget(pos);
        if (perceivedTarget != null)
        {
            lastTargetReason = "estrutura percebida mais próxima";
            return perceivedTarget;
        }

        // Ser atingido revela quem atacou, mas isso é apenas o último recurso:
        // não deve desviar a horda do alvo local, da ordem do líder ou de outra
        // estrutura percebida que ela já esteja priorizando.
        if (IsKnownDamagedTarget(damagedByTarget))
        {
            lastTargetReason = "último recurso: estrutura que causou dano";
            return damagedByTarget;
        }

        lastTargetReason = "nenhum alvo válido; segue fluxo do Núcleo";
        return null;
    }

    Structure PickPersonalPriorityTarget(Vector3 pos)
    {
        bool berserker = socialProfile != null && socialProfile.temperament == EnemyTemperament.Berserker;

        Structure best = null;
        float bestDistance = float.MaxValue;
        foreach (var structure in Structure.All)
        {
            if (!IsValidTarget(structure)) continue;
            if (!IsPersonalPriorityCandidate(structure, berserker) || !CanPerceive(structure, pos)) continue;

            float distance = FlatDistance(pos, structure.transform.position);
            if (distance < bestDistance) { best = structure; bestDistance = distance; }
        }
        return best;
    }

    bool IsPersonalPriorityCandidate(Structure structure, bool berserker)
    {
        return structure != null &&
               (berserker || personality == Personality.Objective || structure.kind == StructureKind.Tower);
    }

    bool ShouldFollowLeaderOrder()
    {
        if (socialProfile == null) return true;
        return group.tactic == GroupTactic.Tactical ||
               (socialProfile.temperament != EnemyTemperament.Wild &&
                socialProfile.temperament != EnemyTemperament.Berserker &&
                socialProfile.behavior != EnemySocialBehavior.Rebellious &&
                (socialProfile.temperament == EnemyTemperament.Disciplined ||
                 socialProfile.temperament == EnemyTemperament.Loyal ||
                 socialProfile.behavior == EnemySocialBehavior.Passive));
    }

    bool CanPerceive(Structure structure, Vector3 pos)
    {
        if (structure == damagedByTarget && damagedTargetTimer > 0f) return true;
        return FlatDistance(pos, structure.transform.position) <= tacticRadius;
    }

    bool IsKnownDamagedTarget(Structure structure)
    {
        return damagedTargetTimer > 0f && IsValidTarget(structure);
    }

    static bool IsValidTarget(Structure structure)
    {
        return structure != null && structure.isActiveAndEnabled && structure.health != null &&
               structure.health.current > 0f;
    }

    static float FlatDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }

    Structure FindCoreTarget()
    {
        foreach (var structure in Structure.All)
            if (IsValidTarget(structure) && structure.kind == StructureKind.Core)
                return structure;
        return null;
    }

    Structure FindNearestPerceivedTarget(Vector3 pos)
    {
        Structure best = null;
        float bestDistance = float.MaxValue;
        foreach (var structure in Structure.All)
        {
            if (!IsValidTarget(structure) || !CanPerceive(structure, pos)) continue;
            float distance = FlatDistance(pos, structure.transform.position);
            if (distance < bestDistance) { best = structure; bestDistance = distance; }
        }
        return best;
    }

    void OnDamagedBy(Health health, float amount, GameObject source)
    {
        if (source == null)
        {
            if (DebugTargetingEnabled())
                Debug.Log($"[EnemyAI][Damage] {name} recebeu {amount:0.#} dano sem origem.", this);
            return;
        }

        Structure sourceStructure = source.GetComponentInParent<Structure>();
        if (!IsValidTarget(sourceStructure))
        {
            if (DebugTargetingEnabled())
                Debug.Log($"[EnemyAI][Damage] {name} recebeu {amount:0.#} dano de {source.name}, " +
                          "mas a origem não foi reconhecida como uma estrutura válida.", this);
            return;
        }

        damagedByTarget = sourceStructure;
        damagedTargetTimer = damagedTargetMemory;
        targetDebugPending = true;
        if (DebugTargetingEnabled())
        {
            float distance = FlatDistance(transform.position, sourceStructure.transform.position);
            Debug.Log($"[EnemyAI][Damage] {name} recebeu {amount:0.#} dano de {sourceStructure.name} " +
                      $"(dist={distance:0.0}, tacticRadius={tacticRadius:0.0}, currentTarget={TargetName(selectedTarget)}). " +
                      "A origem será considerada apenas depois dos demais alvos.", this);
        }
    }

    bool DebugTargetingEnabled()
    {
        return Game.I != null && Game.I.debugEnemyTargeting;
    }

    void LogTargetDecision(Vector3 position, Structure previousTarget, bool afterDamage)
    {
        var candidates = new List<Structure>();
        bool berserker = socialProfile != null && socialProfile.temperament == EnemyTemperament.Berserker;
        foreach (var structure in Structure.All)
        {
            if (!IsValidTarget(structure)) continue;
            float distance = FlatDistance(position, structure.transform.position);
            bool inRadius = distance <= tacticRadius;
            bool isDamagedSource = structure == damagedByTarget && damagedTargetTimer > 0f;
            if (!inRadius && !isDamagedSource) continue;
            candidates.Add(structure);
        }
        candidates.Sort((a, b) => FlatDistance(position, a.transform.position)
            .CompareTo(FlatDistance(position, b.transform.position)));

        var candidateDescriptions = new List<string>();
        foreach (var structure in candidates)
        {
            float distance = FlatDistance(position, structure.transform.position);
            bool inRadius = distance <= tacticRadius;
            bool isDamagedSource = structure == damagedByTarget && damagedTargetTimer > 0f;
            bool personalPriority = type == EnemyType.Tank
                ? structure.kind == StructureKind.Core
                : IsPersonalPriorityCandidate(structure, berserker);
            candidateDescriptions.Add($"{structure.name}[{structure.kind}]={distance:0.0}" +
                                     $"{(inRadius ? " raio" : " fora-raio")}{(personalPriority ? " prioridade" : "")}" +
                                     $"{(isDamagedSource ? " causou-dano" : "")}");
        }

        string role = group == null ? "Solo" : group.Leader == this ? "Líder" : "Membro";
        Debug.Log($"[EnemyAI][Target] {name} type={type}/{personality} role={role} " +
                  $"previous={TargetName(previousTarget)} -> selected={TargetName(selectedTarget)} " +
                  $"reason={lastTargetReason}; tacticRadius={tacticRadius:0.0}; attackRange={attackRange:0.0}; " +
                  $"damagedBy={TargetName(damagedByTarget)}{(afterDamage ? " [decisão após dano]" : "")}; " +
                  $"candidates nearest-first=[{string.Join(", ", candidateDescriptions)}]", this);
    }

    static string TargetName(Structure structure)
    {
        return structure != null
            ? $"{structure.name}/{structure.kind}@({structure.transform.position.x:0.0},{structure.transform.position.z:0.0})"
            : "nenhum";
    }

    Vector3 FleeDir()
    {
        Vector3 d = transform.position - GridWorld.I.CellToWorld(GridWorld.I.CoreCell);
        d.y = 0f;
        return d.normalized;
    }

    Vector3 Separation()
    {
        Vector3 f = Vector3.zero;
        foreach (var e in All)
        {
            if (e == this) continue;
            Vector3 d = transform.position - e.transform.position;
            d.y = 0f;
            float m = d.magnitude;
            float r = 0.6f * (transform.localScale.x + e.transform.localScale.x);
            if (m < r && m > 0.001f) f += d / m * (r - m);
        }
        return f;
    }

    void Move(Vector3 dir, float mult, float dt)
    {
        dir.y = 0f;
        if (dir.sqrMagnitude > 1f) dir.Normalize();

        var g = GridWorld.I;
        Vector3 pos = transform.position;
        Vector3 np = pos + dir * speed * mult * dt;
        var c = g.WorldToCell(np);

        if (!g.InBounds(c))
        {
            if (state == EState.Flee) Destroy(gameObject);   // fugiu do mapa
            return;
        }

        if (g.IsHardBlocked(c))
        {
            if (state != EState.March || !TryFindDetour(pos, dir, mult, dt,
                    out Vector3 hardBlockDetour, out Vector3 hardBlockDirection)) return;
            np = hardBlockDetour;
            dir = hardBlockDirection;
            c = g.WorldToCell(np);
        }

        var s = g.Get(c);
        if (s != null)
        {
            if (state == EState.March)
            {
                // Separation can push an enemy off its route. Recheck the route before
                // deciding that the blocking structure must be attacked.
                Vector3 routeDir = MarchDir();
                if (routeDir.sqrMagnitude <= 0.0001f) return;
                Vector3 routePos = pos + routeDir.normalized * speed * mult * dt;
                var routeCell = g.WorldToCell(routePos);
                var routeBlocker = g.Get(routeCell);
                if (routeBlocker != null)
                {
                    if (type != EnemyType.Tank || g.IsTankBreakableWall(routeBlocker))
                        TryAttack(routeBlocker);
                    return;
                }
                np = routePos;
                dir = routeDir;
            }
            else
            {
                Vector3 nx = new Vector3(np.x, pos.y, pos.z);
                Vector3 nz = new Vector3(pos.x, pos.y, np.z);
                if (Free(nx)) np = nx; else if (Free(nz)) np = nz; else return;
            }
        }

        if (WouldHitStructure(pos, np, out Structure hitStructure))
        {
            // A colisão não deve fazer o inimigo contornar o próprio alvo.
            // Isso era especialmente visível quando Hunter elegia uma torre:
            // ele chegava ao collider e passava rente sem entrar no ataque.
            if (hitStructure != null && hitStructure == selectedTarget &&
                FlatDistance(pos, hitStructure.transform.position) <= attackRange)
            {
                TryAttack(hitStructure);
                return;
            }

            if (type == EnemyType.Tank && g.IsTankBreakableWall(hitStructure))
            {
                TryAttack(hitStructure);
                return;
            }

            if (TryFindDetour(pos, dir, mult, dt, out Vector3 detourPosition, out Vector3 detourDirection))
            {
                np = detourPosition;
                dir = detourDirection;
            }
            else
            {
                // If no clear flank exists, attack the obstruction instead of
                // repeatedly pushing into its collider.
                if (type != EnemyType.Tank || g.IsTankBreakableWall(hitStructure))
                    TryAttack(hitStructure);
                return;
            }
        }

        transform.position = np;
        if (dir.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(dir);
    }

    bool TryFindDetour(Vector3 position, Vector3 preferredDirection, float mult, float dt,
        out Vector3 detourPosition, out Vector3 detourDirection)
    {
        detourPosition = position;
        detourDirection = Vector3.zero;
        preferredDirection.y = 0f;
        if (preferredDirection.sqrMagnitude < 0.0001f) return false;
        preferredDirection.Normalize();

        Vector3 targetDirection = selectedTarget != null
            ? selectedTarget.transform.position - position
            : preferredDirection;
        targetDirection.y = 0f;
        if (targetDirection.sqrMagnitude > 0.0001f) targetDirection.Normalize();

        float stepDistance = speed * mult * dt;
        float bestScore = float.NegativeInfinity;
        for (int angle = 20; angle <= 160; angle += 20)
        {
            for (int pass = 0; pass < 2; pass++)
            {
                int side = pass == 0 ? recoverySide : -recoverySide;
                Vector3 candidateDirection = Quaternion.AngleAxis(angle * side, Vector3.up) * preferredDirection;
                Vector3 candidatePosition = position + candidateDirection * stepDistance;
                if (!Free(candidatePosition) || WouldHitStructure(position, candidatePosition)) continue;

                float score = Vector3.Dot(candidateDirection, preferredDirection) * 0.35f +
                              Vector3.Dot(candidateDirection, targetDirection) * 0.65f;
                if (score <= bestScore) continue;
                bestScore = score;
                detourPosition = candidatePosition;
                detourDirection = candidateDirection;
            }
        }

        return bestScore > float.NegativeInfinity;
    }

    void UpdateStuckRecovery(Vector3 positionBeforeMove, Vector3 intendedDirection, float dt)
    {
        if (state != EState.March || intendedDirection.sqrMagnitude < 0.0001f ||
            (IsValidTarget(selectedTarget) &&
             FlatDistance(positionBeforeMove, selectedTarget.transform.position) <= attackRange))
        {
            stuckTimer = 0f;
            return;
        }

        float moved = Vector3.Distance(positionBeforeMove, transform.position);
        if (moved < Mathf.Max(0.01f, speed * dt * 0.15f))
            stuckTimer += dt;
        else
            stuckTimer = 0f;

        if (stuckTimer < StuckRecoveryDelay) return;

        stuckTimer = 0f;
        recoverySide *= -1;
        recoveryTimer = 0.45f;
        targetDecisionTimer = 0f;
    }

    bool WouldHitStructure(Vector3 from, Vector3 to)
    {
        return WouldHitStructure(from, to, out _);
    }

    bool WouldHitStructure(Vector3 from, Vector3 to, out Structure hitStructure)
    {
        hitStructure = null;
        Vector3 displacement = to - from;
        displacement.y = 0f;
        float distance = displacement.magnitude;
        if (distance <= 0.0001f) return false;

        int structureMask = 0;
        foreach (var structure in Structure.All)
        {
            if (structure == null) continue;
            var collider = structure.GetComponent<Collider>();
            if (collider == null || !collider.enabled || !collider.gameObject.activeInHierarchy) continue;
            structureMask |= 1 << structure.gameObject.layer;
        }
        if (structureMask == 0) return false;

        float radius = Mathf.Max(0.15f, Mathf.Max(transform.lossyScale.x, transform.lossyScale.z) * 0.45f);
        if (!Physics.SphereCast(from, radius, displacement.normalized, out RaycastHit hit, distance + 0.01f,
            structureMask, QueryTriggerInteraction.Ignore)) return false;
        hitStructure = hit.collider.GetComponentInParent<Structure>();
        return true;
    }

    bool Free(Vector3 p)
    {
        var c = GridWorld.I.WorldToCell(p);
        return GridWorld.I.InBounds(c) && !GridWorld.I.IsHardBlocked(c) && GridWorld.I.Get(c) == null;
    }

    void TryAttack(Structure s)
    {
        if (s == null || atkTimer > 0f) return;
        if (DebugTargetingEnabled())
            Debug.Log($"[EnemyAI][Attack] {name} attacks={TargetName(s)} selected={TargetName(selectedTarget)} " +
                      $"distance={FlatDistance(transform.position, s.transform.position):0.0} range={attackRange:0.0} " +
                      $"sameAsSelected={s == selectedTarget}", this);
        atkTimer = attackCooldown;
        s.health.Damage(damage);
    }
}
