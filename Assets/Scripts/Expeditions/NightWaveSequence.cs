using System;

// Tracks whole combat rounds, independently of the timing of individual groups.
public sealed class NightWaveSequence
{
    public int CurrentWave { get; private set; } = 1;
    public int TotalWaves { get; }
    public int GroupsPerWave { get; }
    public int SpawnedGroups { get; private set; }

    public NightWaveSequence(int totalWaves, int groupsPerWave)
    { TotalWaves = Math.Max(1, totalWaves); GroupsPerWave = Math.Max(1, groupsPerWave); }

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
