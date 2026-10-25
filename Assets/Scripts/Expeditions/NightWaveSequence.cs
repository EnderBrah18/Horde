using System;

// Tracks whole combat rounds, independently of the timing of individual groups.
public sealed class NightWaveSequence
{
    public int CurrentWave { get; private set; } = 1;
    public int TotalWaves { get; }
    readonly int[] groupCounts;
    public int GroupsPerWave => groupCounts[CurrentWave - 1];
    public int SpawnedGroups { get; private set; }

    public NightWaveSequence(int totalWaves, int groupsPerWave)
    {
        TotalWaves = Math.Max(1, totalWaves);
        groupCounts = new int[TotalWaves];
        for (int i = 0; i < TotalWaves; i++) groupCounts[i] = Math.Max(1, groupsPerWave);
    }

    public NightWaveSequence(int[] groupsPerWave)
    {
        if (groupsPerWave == null || groupsPerWave.Length == 0) throw new ArgumentException("Wave schedule is empty.");
        groupCounts = (int[])groupsPerWave.Clone();
        foreach (int count in groupCounts) if (count < 1) throw new ArgumentException("Wave must contain a group.");
        TotalWaves = groupCounts.Length;
    }

    public void RecordGroupSpawned()
    {
        if (SpawnedGroups >= GroupsPerWave) throw new InvalidOperationException("All groups in this wave have already spawned.");
        SpawnedGroups++;
    }

    public bool CanFinishNight(int remainingEnemies) => CurrentWave == TotalWaves &&
        SpawnedGroups == GroupsPerWave && remainingEnemies == 0;

    public bool TryAdvance(int remainingEnemies)
    {
        if (CurrentWave >= TotalWaves || SpawnedGroups < GroupsPerWave || remainingEnemies != 0) return false;
        CurrentWave++;
        SpawnedGroups = 0;
        return true;
    }
}
