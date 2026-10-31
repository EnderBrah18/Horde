using System;

// Calendar rules are independent of scenes, waves spawned and expedition travel.
public static class VillageProgressionRules
{
    public static int DayNumber(int nightsStarted, bool night, bool lost) =>
        night || lost ? Math.Max(1, nightsStarted) : Math.Max(0, nightsStarted) + 1;

    public static int WavesForNight(int nightNumber, int initial, int nightsPerExtraWave, int maximum) =>
        (int)Math.Min(Math.Max(1, maximum), Math.Max(1, initial) +
            (long)Math.Max(0, nightNumber - 1) / Math.Max(1, nightsPerExtraWave));

    public static bool CanDiscover(int day, int discoveryDay, bool night, bool lost, bool discovered) =>
        !night && !lost && !discovered && day >= Math.Max(1, discoveryDay);

    public static bool CanTakePersonalExpedition(int day, int lastDepartureDay) => day >= 1 && lastDepartureDay < day;
}
