using UnityEngine;

public sealed class EnemySpawnOverrides
{
    public EnemyBalanceSettings balance;
    public Personality? personality;
    public float? bravery;
    public EnemyTemperament? temperament;
    public EnemySocialBehavior? behavior;
}

// Shared by nights, expedition encounters and enemies placed directly in scenes.
public static class EnemyFactory
{
    public static bool CanSpawn(EnemyArchetypeData definition) => definition!=null && definition.balance!=null &&
        (definition.prefab==null || definition.prefab.GetComponentInChildren<Renderer>(true)!=null);

    public static Enemy Spawn(EnemyArchetypeData definition,Vector3 groundPosition,GridWorld navigation,
        EnemyGroup group=null,Transform parent=null,EnemySpawnOverrides overrides=null)
    {
        if(!CanSpawn(definition) || navigation==null) return null;
        var balance=overrides!=null&&overrides.balance!=null?overrides.balance:definition.balance;
        GameObject obj;
        if(definition.prefab!=null)
        {
            obj=Object.Instantiate(definition.prefab);
            if(definition.applyScaleToPrefab) obj.transform.localScale=balance.scale;
        }
        else
        {
            bool tall=definition.primitiveShape==PrimitiveType.Cylinder||definition.primitiveShape==PrimitiveType.Capsule;
            obj=Game.Prim(definition.primitiveShape,groundPosition, balance.scale,Color.red);
            groundPosition.y+=balance.scale.y*(tall?1f:0.5f);
        }
        obj.transform.position=groundPosition;
        if(parent!=null)obj.transform.SetParent(parent,true);
        obj.name=definition.displayName;
        int layer=LayerMask.NameToLayer("Enemies");
        if(layer>=0)foreach(var child in obj.GetComponentsInChildren<Transform>(true))child.gameObject.layer=layer;
        var health=obj.GetComponent<Health>(); if(health==null)health=obj.AddComponent<Health>();
        var enemy=obj.GetComponent<Enemy>(); if(enemy==null)enemy=obj.AddComponent<Enemy>();
        enemy.InitializeFromDefinition(definition,navigation,group,overrides);
        obj.SetActive(true);
        return enemy;
    }
}
