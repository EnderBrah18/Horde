using UnityEngine;

// Position preferences are independent of target personality and group membership.
[DisallowMultipleComponent]
public sealed class EnemyPositioning : MonoBehaviour
{
    public Enemy Anchor { get; private set; }
    Enemy owner;
    bool preferTankCover;
    float rearDistance;
    float spacing;
    float scanTimer;

    public void Configure(Enemy enemy, EnemyArchetypeData definition)
    {
        owner = enemy;
        preferTankCover = definition.preferTankCover;
        rearDistance = Mathf.Max(1f, definition.tankCoverDistance);
        spacing = Mathf.Max(1f, definition.tankCoverSpacing);
        Anchor = null;
        scanTimer = 0f;
    }

    bool Valid(Enemy tank, float radius)
    {
        if (owner == null || tank == null) return false;
        return tank.isActiveAndEnabled && EnemyPositioningRules.CanCoverBehind(
            tank.hp != null && tank.hp.current > 0f, tank.state == EState.March,
            tank.NavigationGrid == owner.NavigationGrid, tank.gameObject.scene == owner.gameObject.scene,
            tank == owner, tank.type == EnemyType.Tank,
            FlatDistance(owner.transform.position, tank.transform.position), Mathf.Max(0f, radius));
    }

    public bool TrySteer(Vector3 route, bool attacking, out Vector3 movement)
    {
        movement = route;
        if (owner == null || !preferTankCover || owner.type == EnemyType.Tank || owner.state != EState.March)
        { Anchor = null; return false; }
        if (!Valid(Anchor, owner.tacticRadius * 1.25f)) Anchor = null;
        if (attacking) return false;
        scanTimer -= Time.deltaTime;
        if (scanTimer <= 0f)
        {
            scanTimer = 0.5f;
            if (Anchor == null)
            {
                float nearest = float.PositiveInfinity;
                foreach (var candidate in Enemy.All)
                {
                    if (!Valid(candidate, owner.tacticRadius)) continue;
                    float distance = FlatDistance(owner.transform.position, candidate.transform.position);
                    if (distance < nearest && TankCoverFormation.CanReach(owner, candidate, rearDistance, spacing))
                    { Anchor = candidate; nearest = distance; }
                }
            }
        }
        if (Anchor == null) return false;
        if (!TankCoverFormation.TrySteer(owner, Anchor, rearDistance, spacing, out movement))
        { Anchor = null; movement = route; return false; }
        if (EnemyPositioningRules.ShouldLeaveGroup(owner.group != null,
            owner.group != null && owner.group.Leader == owner,
            owner.group != null && Anchor.group == owner.group)) owner.LeaveGroupForPositioning();
        return true;
    }

    public Enemy ExternalGroupAnchor => owner != null && owner.group != null && owner.group.Leader == owner &&
        Anchor != null && Anchor.group != owner.group && Valid(Anchor, owner.tacticRadius * 1.25f) ? Anchor : null;

    public bool TrySteerGroupMember(Enemy member, Vector3 route, out Vector3 movement)
    {
        movement = route;
        var anchor = ExternalGroupAnchor;
        if (anchor == null || member == null || member.type == EnemyType.Tank ||
            member.NavigationGrid != anchor.NavigationGrid || member.state != EState.March) return false;
        return TankCoverFormation.TrySteer(member, anchor, rearDistance, spacing, out movement);
    }

    static float FlatDistance(Vector3 a, Vector3 b)
    {
        a.y = b.y = 0f;
        return Vector3.Distance(a,b);
    }
}
