using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Game))]
public sealed class VillageProduction : MonoBehaviour
{
    Game game;
    int lastProcessedRound;
    // Mantido para consumidores existentes: corresponde apenas à população comum.
    public int HousingCapacity => PopulationCapacity;
    public int PopulationCapacity
    {
        get
        {
            int capacity = 0;
            foreach (var facility in FindObjectsByType<VillageFacility>()) capacity += facility.PopulationCapacity;
            return capacity;
        }
    }
    public int EssentialNpcCapacity
    {
        get
        {
            int capacity = 0;
            foreach (var facility in FindObjectsByType<VillageFacility>()) capacity += facility.EssentialNpcCapacity;
            return capacity;
        }
    }

    void OnEnable()
    {
        game = GetComponent<Game>();
        game.GameplayEventRaised += OnGameplayEvent;
    }
    void OnDisable()
    {
        if (game != null) game.GameplayEventRaised -= OnGameplayEvent;
    }
    void OnGameplayEvent(GameplayEvent gameEvent)
    {
        if (gameEvent.type != GameplayEventType.DawnReached || game.lost || game.night ||
            game.round <= 0 || game.round <= lastProcessedRound) return;
        lastProcessedRound = game.round;
        int count = 0;
        foreach (var facility in FindObjectsByType<VillageFacility>())
            if (facility.TryProduce(game.round, game.wallet)) count++;
        if (count > 0)
            game.RaiseGameplayEvent(GameplayEventType.FacilitiesProduced, gameObject, count,
                $"{count} instalação(ões) entregaram recursos após a noite {game.round}");
    }
}
