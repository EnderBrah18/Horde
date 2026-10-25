using System;
using UnityEngine;

public enum GameplayEventType
{
    EnemySpawned,
    StructureBuilt,
    StructureAttacked,
    StructureDestroyed,
    CoreDestroyed,
    NightStarted,
    DawnReached,
    EnemyGroupMerged,
    FacilityUnlocked,
    FacilityUpgraded,
    FacilitiesProduced,
    FacilityBroken,
    FacilityRepaired,
    WaveStarted,
    ExpeditionDiscovered,
    ExpeditionDeparted,
    ExpeditionReturned
}

[Serializable]
public struct GameplayEvent
{
    public GameplayEventType type;
    public Vector3 position;
    public GameObject subject;
    public float value;
    public float time;
    public string details;

    public GameplayEvent(GameplayEventType type, Vector3 position, GameObject subject, float value, string details)
    {
        this.type = type;
        this.position = position;
        this.subject = subject;
        this.value = value;
        this.time = Time.time;
        this.details = details;
    }
}
