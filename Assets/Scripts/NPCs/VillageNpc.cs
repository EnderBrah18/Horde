using UnityEngine;

public enum VillageNpcState { Idle, GoingToWork, Working, GoingHome, Sheltered, Homeless, Injured, NoRoute, Dead }

[DisallowMultipleComponent, RequireComponent(typeof(Health))]
public sealed class VillageNpc : MonoBehaviour
{
    public NpcData data;
    [SerializeField] VillageFacility residence;
    [SerializeField] VillageFacility workplace;
    [SerializeField] NpcRoleData role;
    public VillageFacility Residence => residence;
    public VillageFacility Workplace => workplace;
    public NpcRoleData Role => role;
    public VillageNpcState State { get; private set; }
    public bool IsDead => State == VillageNpcState.Dead;
    public bool IsInjured => health != null && health.current <= 0f && !IsDead;
    public bool IsHomeless => data == null || residence == null || !residence.IsOperational ||
        (data.category == NpcCategory.Essential ? residence.EssentialNpcCapacity : residence.PopulationCapacity) <= 0;
    public bool CanWork => isActiveAndEnabled && !IsDead && !IsInjured && role != null && role.Accepts(workplace);
    Health health;
    VillagePopulation population;
    VillageFacility destination;
    Vector3 goal;
    bool hasGoal;
    float nextRouteCheck;

    void Awake()
    {
        health = GetComponent<Health>();
        health.destroyOnDeath = false;
        if (data != null) health.Setup(Mathf.Max(1f, data.maxHealth));
        health.Died += OnDied;
        var body = GetComponent<Collider>();
        if (body != null) body.isTrigger = true;
        if (data != null && data.prefab == null)
        {
            var renderer = GetComponent<Renderer>();
            if (renderer != null)
            {
                var properties = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(properties);
                properties.SetColor("_BaseColor", data.placeholderColor);
                properties.SetColor("_Color", data.placeholderColor);
                renderer.SetPropertyBlock(properties);
            }
        }
    }

    void Start()
    {
        population = Game.I != null ? Game.I.GetComponent<VillagePopulation>() : null;
        if (population != null) population.Register(this);
    }

    public bool CanPerform(NpcRoleData candidate) => data != null && data.allowedRoles != null &&
        System.Array.IndexOf(data.allowedRoles, candidate) >= 0;
    public void SetResidence(VillageFacility home) { residence = home; hasGoal = false; }
    public void SetAssignment(NpcRoleData assignment, VillageFacility facility)
    { role = assignment; workplace = facility; hasGoal = false; }
    public void Recover() { if (health != null) health.Setup(Mathf.Max(1f, data.maxHealth)); State = VillageNpcState.Idle; }

    void OnDied(Health source)
    {
        if (data != null && data.category == NpcCategory.Essential) State = VillageNpcState.Injured;
        else
        {
            State = VillageNpcState.Dead;
            if (population != null) population.Remove(this);
            Destroy(gameObject);
        }
    }

    void Update()
    {
        if (data == null || Game.I == null || Game.I.lost || Game.I.IsOnExpedition || Game.I.ExpeditionTransition || IsDead) return;
        if (IsInjured) { State = VillageNpcState.Injured; return; }
        bool night = Game.I.night;
        VillageFacility target = night ? (IsHomeless ? null : residence) : CanWork ? workplace : IsHomeless ? null : residence;
        if (target == null) { State = IsHomeless ? VillageNpcState.Homeless : VillageNpcState.Idle; return; }
        if (target != destination || Time.time >= nextRouteCheck)
        {
            destination = target;
            nextRouteCheck = Time.time + 2f;
            hasGoal = VillagePopulation.TryEntrance(target, transform.position, out goal);
        }
        var grid = GridWorld.I;
        if (!hasGoal || grid == null) { State = VillageNpcState.NoRoute; return; }
        Vector3 position = transform.position; position.y = goal.y;
        if (Vector3.Distance(position, goal) < 0.2f)
        { State = !night && target == workplace ? VillageNpcState.Working : night ? VillageNpcState.Sheltered : VillageNpcState.Idle; return; }
        if (!grid.TryOpenRoute(position, grid.WorldToCell(goal), out Vector3 direction))
        { State = VillageNpcState.NoRoute; return; }
        State = !night && target == workplace ? VillageNpcState.GoingToWork : VillageNpcState.GoingHome;
        float step = Mathf.Min(Mathf.Max(0.1f, data.moveSpeed) * Time.deltaTime, 0.2f);
        if (grid.WorldToCell(position) == grid.WorldToCell(goal)) direction = (goal - position).normalized;
        var next = transform.position + direction * Mathf.Min(step, Vector3.Distance(position, goal));
        var cell = grid.WorldToCell(next);
        if (!grid.InBounds(cell) || grid.IsHardBlocked(cell) || grid.Get(cell) != null)
        { hasGoal = false; State = VillageNpcState.NoRoute; return; }
        transform.position = next;
        if (direction.sqrMagnitude > 0.001f) transform.rotation = Quaternion.LookRotation(direction);
    }

    void OnDestroy()
    {
        if (health != null) health.Died -= OnDied;
        if (population != null) population.Remove(this);
    }
}
