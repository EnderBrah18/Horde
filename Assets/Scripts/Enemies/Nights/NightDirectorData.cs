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
    [Header("Distribuição dos ataques (a partir da segunda noite)")]
    [Range(0f,1f)] public float singleAssaultChance=0.1f;
    [Range(0f,1f)] public float concentratedWaveChance=0.5f;
    [Range(0.5f,1f)] public float minimumDominantShare=0.55f;
    [Range(0.5f,1f)] public float maximumDominantShare=0.8f;
    [Min(0f)] public float dispersedGroupWeight=1f;
    [Min(0f)] public float balancedGroupWeight=1f;
    [Min(0f)] public float armyGroupWeight=1f;
    [Tooltip("Peso adicional de exércitos por noite após a segunda. Não aumenta o total de inimigos.")]
    [Min(0f)] public float armyWeightPerNight=0.1f;
    [Tooltip("Duração máxima para materializar um grupo inteiro. 0 faz todos nascerem juntos. A abertura mantém seu intervalo original.")]
    [Min(0f)] public float maximumGroupSpawnDuration=2f;
    [Header("Atributos por noite (multiplicadores sobre o Enemy Archetype)")]
    [Min(0f)] public float healthIncreasePerNight=0.08f;
    [Min(1f)] public float maximumHealthMultiplier=4f;
    [Min(0f)] public float damageIncreasePerNight=0.05f;
    [Min(1f)] public float maximumDamageMultiplier=3f;
    [Min(0f)] public float speedIncreasePerNight=0.005f;
    [Min(1f)] public float maximumSpeedMultiplier=1.15f;
    [Min(0f)] public float attackRateIncreasePerNight=0.01f;
    [Min(1f)] public float maximumAttackRateMultiplier=1.35f;
    public EnemyGroupCompositionData[] groupCompositions=new EnemyGroupCompositionData[0];

    public EnemyBalanceSettings BalanceForNight(EnemyBalanceSettings source,int night)
    {
        if(source==null) return null;
        return new EnemyBalanceSettings {
            maxHealth=source.maxHealth*Multiplier(night,healthIncreasePerNight,maximumHealthMultiplier),
            damage=source.damage*Multiplier(night,damageIncreasePerNight,maximumDamageMultiplier),
            speed=source.speed*Multiplier(night,speedIncreasePerNight,maximumSpeedMultiplier),
            attackCooldown=source.attackCooldown/Multiplier(night,attackRateIncreasePerNight,maximumAttackRateMultiplier),
            attackRange=source.attackRange,tacticRadius=source.tacticRadius,
            knockbackResistance=source.knockbackResistance,scale=source.scale
        };
    }

    static float Multiplier(int night,float increase,float cap) =>
        (float)System.Math.Min(System.Math.Max(1f,cap),1d+System.Math.Max(0,night-1)*(double)System.Math.Max(0f,increase));
}
