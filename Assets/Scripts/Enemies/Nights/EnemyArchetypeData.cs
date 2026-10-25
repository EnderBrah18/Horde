using UnityEngine;

[CreateAssetMenu(menuName="Village/Enemy Archetype",fileName="NewEnemyArchetype")]
public sealed class EnemyArchetypeData : ScriptableObject
{
    public string displayName="Inimigo";
    [TextArea] public string description;
    [Tooltip("Comportamento implementado atualmente: Common ou Tank. Variantes não criam uma nova IA automaticamente.")]
    public EnemyType runtimeType;
    public EnemyBalanceSettings balance=new EnemyBalanceSettings();
}
