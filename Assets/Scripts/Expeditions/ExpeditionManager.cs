using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class ExpeditionEntry
{
    public ExpeditionData destination;
}

[DisallowMultipleComponent]
public sealed class ExpeditionManager : MonoBehaviour
{
    [Header("Destinos da vila")]
    public ExpeditionEntry[] expeditions = new ExpeditionEntry[0];
    [Header("Derrota na expedição")]
    [Tooltip("Percentual perdido de cada recurso carregado. O restante é entregue ao retornar.")]
    [Range(0,100)] public int defeatCargoLossPercent=50;
    [Tooltip("Vida ao retornar após derrota, como percentual da vida máxima. Permanece reduzida para a noite; o amanhecer vitorioso cura normalmente.")]
    [Range(1,100)] public int defeatReturnHealthPercent=50;
    [Header("Teste de expedições")]
    [InspectorName("Liberar expedições no dia 1")]
    [Tooltip("Libera os destinos cadastrados na manhã do dia 1. Mantém o limite de uma viagem pessoal por dia.")]
    public bool unlockExpeditionsOnFirstDay;

    public IEnumerable<ExpeditionData> Destinations
    {
        get
        {
            var seen = new HashSet<ExpeditionData>();
            foreach (var entry in expeditions ?? new ExpeditionEntry[0])
                if (entry != null && entry.destination != null && seen.Add(entry.destination))
                    yield return entry.destination;
        }
    }

    public ExpeditionEntry Find(ExpeditionData destination)
    {
        if (destination == null) return null;
        foreach (var entry in expeditions ?? new ExpeditionEntry[0])
            if (entry != null && entry.destination == destination) return entry;
        return null;
    }
}
