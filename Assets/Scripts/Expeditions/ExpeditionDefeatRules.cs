using System;

public static class ExpeditionDefeatRules
{
    // Round retained cargo down, so losing a fraction of a unit never creates resources.
    public static int RetainedAmount(int amount, int lossPercent) =>
        (int)((long)Math.Max(0, amount) * (100 - Math.Max(0, Math.Min(100, lossPercent))) / 100);

    public static float ReturnHealth(float maximum, int percent) =>
        Math.Max(1f, Math.Max(1f, maximum) * Math.Max(1, Math.Min(100, percent)) / 100f);
}
