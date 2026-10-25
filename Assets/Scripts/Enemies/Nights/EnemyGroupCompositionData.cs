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
    public bool uniformTemperament;
    public EnemyTemperament temperament=EnemyTemperament.Disciplined;
    public bool uniformBehavior;
    public EnemySocialBehavior behavior=EnemySocialBehavior.Proactive;
    public bool uniformPersonality;
    public Personality personality=Personality.Objective;
    public bool forceTactic;
    public GroupTactic tactic=GroupTactic.Tactical;
}
