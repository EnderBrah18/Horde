using UnityEngine;

public static class EnemyCatalog
{
    public static EnemyArchetypeData Resolve(EnemyType type,Personality personality)
    {
        string name=type==EnemyType.Tank?"Tank":personality==Personality.Saboteur?"Saboteur":"Fodder";
        return Resources.Load<EnemyArchetypeData>("Enemies/"+name);
    }
}
