using System;
using UnityEngine;

[Serializable]
public sealed class EnemyCompositionSlot
{
    public EnemyArchetypeData archetype;
    [Min(0)] public int minimumPerGroup;
    [Min(0f)] public float weight=1f;
}

[CreateAssetMenu(menuName="Village/Enemy Group Composition",fileName="NewGroupComposition")]
public sealed class EnemyGroupCompositionData : ScriptableObject
{
    public string displayName="Grupo";
    [Min(2)] public int firstNight=2;
    [Min(0f)] public float selectionWeight=1f;
    [Min(1f)] public float minimumPopulationRatio=1f;
    [Min(1f)] public float maximumPopulationRatio=10f;
    public EnemyCompositionSlot[] members=new EnemyCompositionSlot[0];
    [Tooltip("Quantidade fornecida por chamada desta composição. 0 preenche todas as vagas restantes do lote. Se faltar, outra composição será sorteada.")]
    [Min(0)] public int spawnCount;
    public bool uniformTemperament;
    public EnemyTemperament temperament=EnemyTemperament.Disciplined;
    public bool uniformBehavior;
    public EnemySocialBehavior behavior=EnemySocialBehavior.Proactive;
    public bool uniformPersonality;
    public Personality personality=Personality.Objective;
    [Tooltip("Força a mesma bravura em todos os integrantes, inclusive Tanks. Desligado mantém o sistema atual de cada spawn.")]
    public bool uniformBravery;
    [Range(0f, 1.2f)] public float bravery=1f;
    public bool forceTactic;
    public GroupTactic tactic=GroupTactic.Tactical;

    public float ResolveBravery(float current) => uniformBravery
        ? (float)Math.Max(0d, Math.Min(1.2d, bravery)) : current;
}
