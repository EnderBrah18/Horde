public static class EnemyPositioningRules
{
    public static bool CanCoverBehind(bool alive, bool marching, bool sameNavigation, bool sameScene,
        bool isSelf, bool isTank, float distance, float detectionRadius) =>
        alive && marching && sameNavigation && sameScene && !isSelf && isTank && distance <= detectionRadius;

    public static bool ShouldLeaveGroup(bool hasGroup, bool isLeader, bool anchorInOwnGroup) =>
        hasGroup && !isLeader && !anchorInOwnGroup;
}
