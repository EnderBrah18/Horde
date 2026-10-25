using UnityEngine;

[CreateAssetMenu(menuName="Village/Night Director",fileName="NightDirector")]
public sealed class NightDirectorData : ScriptableObject
{
    public int firstNightSeed=1337;
    [Range(0f,1f)] public float repeatMinimumChance=0.25f;
    [Min(1)] public int minimumExtraEnemies=10;
    [Min(0)] public int minimumIncreasePercent=50;
    [Min(0)] public int maximumIncreasePercent=200;
    [Min(1)] public int extraEnemyBatchSize=5;
    public EnemyGroupCompositionData[] groupCompositions=new EnemyGroupCompositionData[0];
}
