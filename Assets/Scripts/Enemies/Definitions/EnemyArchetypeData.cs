using UnityEngine;

[CreateAssetMenu(menuName="Village/Enemy Archetype",fileName="NewEnemyArchetype")]
public sealed class EnemyArchetypeData : ScriptableObject
{
    public string displayName="Inimigo";
    [TextArea] public string description;
    [Tooltip("Comportamento implementado atualmente: Common ou Tank. Variantes não criam uma nova IA automaticamente.")]
    public EnemyType runtimeType;
    public Personality personality=Personality.Objective;
    [Header("Preferência de posicionamento")]
    [Tooltip("Procura Tanks no Tactic Radius, inclusive de outros grupos. Membros saem do grupo; líderes mantêm o grupo e conduzem sua formação.")]
    public bool preferTankCover;
    [Min(1f)] public float tankCoverDistance=4f;
    [Min(1f)] public float tankCoverSpacing=2.5f;
    [Header("PlayerHunter: perseguição")]
    [Tooltip("Stalker memoriza a última posição após a tolerância. Tracker rastreia continuamente depois da primeira detecção, sem usar os timers abaixo.")]
    public PlayerHuntStyle playerHuntStyle=PlayerHuntStyle.Stalker;
    [Tooltip("Após sair do raio, continua rastreando por este tempo. Ao terminar, memoriza a posição daquele instante.")]
    [Min(0f)] public float playerHuntGraceDuration=5f;
    [Tooltip("Limite para investigar a posição memorizada caso a rota esteja bloqueada. Ao chegar sem reencontrar a vítima, retoma o núcleo imediatamente.")]
    [Min(0.1f)] public float playerHuntInvestigationDuration=20f;
    [Header("Contexto sem núcleo")]
    public bool targetPlayerWithoutCore=true;
    [Header("Apresentação")]
    public UnityEngine.GameObject prefab;
    public PrimitiveType primitiveShape=PrimitiveType.Sphere;
    public bool applyScaleToPrefab;
    [Header("Personalidade individual")]
    [Range(0f,1.2f)] public float bravery=1f;
    public bool randomizeSocialOnSpawn=true;
    public EnemyTemperament temperament=EnemyTemperament.Disciplined;
    public EnemySocialBehavior behavior=EnemySocialBehavior.Proactive;
    public EnemyBalanceSettings balance=new EnemyBalanceSettings();
}
