using System;
using UnityEngine;

[Serializable]
public class EnemyBalanceSettings
{
    [Min(1f)] public float maxHealth = 25f;
    [Min(0f)] public float speed = 3.2f;
    [Min(0f)] public float damage = 4f;
    [Min(0.01f)] public float attackCooldown = 1f;
    [Min(0f)] public float attackRange = 3f;
    [Min(0f)] public float tacticRadius = 10f;
    public Vector3 scale = new Vector3(0.9f, 0.9f, 0.9f);
}
