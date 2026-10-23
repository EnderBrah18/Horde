using UnityEngine;

public enum EnemyTemperament { Disciplined, Loyal, Wild, Berserker }
public enum EnemySocialRole { Leader, Soldier, LoneWolf }
public enum EnemySocialBehavior { Proactive, Passive, Rebellious }
public enum GroupTactic { Aggressive, Tactical }

// Additive social/personality layer for the prototype's existing Enemy and EnemyGroup systems.
[DisallowMultipleComponent]
public class EnemySocialProfile : MonoBehaviour
{
    [Header("Individual personality")]
    public bool randomizeOnSpawn = true;
    public EnemyTemperament temperament = EnemyTemperament.Disciplined;
    public EnemySocialBehavior behavior = EnemySocialBehavior.Proactive;

    [Header("Group role")]
    public EnemySocialRole role = EnemySocialRole.Soldier;

    public void RollOnSpawn()
    {
        if (!randomizeOnSpawn) return;

        temperament = (EnemyTemperament)Random.Range(0, 4);
        behavior = (EnemySocialBehavior)Random.Range(0, 3);
    }
}
