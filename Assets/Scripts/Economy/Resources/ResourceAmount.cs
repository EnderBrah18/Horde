using System;
using UnityEngine;

// Usado em custos, recompensas e receitas com qualquer combinação de recursos.
[Serializable]
public struct ResourceAmount
{
    public ResourceData resource;
    [Min(0)] public int amount;

    public ResourceAmount(ResourceData resource, int amount)
    {
        this.resource = resource;
        this.amount = amount;
    }
}
