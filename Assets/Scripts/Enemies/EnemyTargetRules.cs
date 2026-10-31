public static class EnemyTargetRules
{
    public static bool IsLocalStructure(bool attackable, bool sameScene, bool insideNavigation) => attackable && sameScene && insideNavigation;
    public static bool PreferPlayer(Personality personality, bool hasLocalCore, bool fallbackWithoutCore) =>
        (!hasLocalCore && fallbackWithoutCore &&
            (personality == Personality.Objective || personality == Personality.Saboteur));
    public static bool CanAcquirePlayer(Personality personality, bool hasLocalCore, bool fallbackWithoutCore,
        float distance, float perception) =>
        (personality == Personality.Simple || personality == Personality.PlayerHunter) ? distance <= perception :
        PreferPlayer(personality, hasLocalCore, fallbackWithoutCore) &&
        (personality == Personality.PlayerHunter || distance <= perception);
    public static bool MatchesStructure(Personality personality, StructureKind kind) =>
        ((personality == Personality.Objective || personality == Personality.PlayerHunter) && kind == StructureKind.Core) ||
        (personality == Personality.Saboteur && kind == StructureKind.Tower);
    public static bool CanSelectStructure(Personality personality, StructureKind kind, float distance, float perception) =>
        personality == Personality.Simple ? kind != StructureKind.Wall && distance <= perception :
        MatchesStructure(personality, kind);
}
