using UnityEngine;

[CreateAssetMenu(menuName="Village/Run Economy",fileName="RunEconomy")]
public sealed class RunEconomyData : ScriptableObject
{
    [Tooltip("Estoque da nova run. Referências e quantidades podem ser balanceadas sem mudar Game.")]
    public ResourceAmount[] startingStock=new ResourceAmount[0];
    [Tooltip("Reserva mínima entregue após vencer uma noite. Não cresce automaticamente com o dia; instalações com trabalhadores complementam a produção.")]
    public ResourceAmount[] dawnGrant=new ResourceAmount[0];

    void OnValidate()
    {
        Clamp(startingStock);
        Clamp(dawnGrant);
    }
    static void Clamp(ResourceAmount[] entries)
    {
        if (entries == null) return;
        for (int i=0;i<entries.Length;i++) entries[i].amount=Mathf.Max(0,entries[i].amount);
    }
}
