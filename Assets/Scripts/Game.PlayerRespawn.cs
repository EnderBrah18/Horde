using System.Collections;
using UnityEngine;

public partial class Game
{
    readonly PlayerRespawnCountdown respawnCountdown = new PlayerRespawnCountdown();
    public bool IsPlayerRespawning => respawnCountdown.Running;
    public float PlayerRespawnRemaining => respawnCountdown.Remaining;

    IEnumerator RespawnPlayerInVillage()
    {
        var vitals = PlayerVitals.I;
        if (vitals == null || !respawnCountdown.Start(vitals.villageRespawnDelay)) yield break;
        combatCameraMode = false;
        EmptyHand();
        if (cursor != null) cursor.SetActive(false);
        vitals.HideUntilRespawn();
        Say("Você caiu. A vila continua lutando. Use a câmera livre enquanto aguarda o retorno.", 10f);
        while (respawnCountdown.Running)
        {
            yield return null;
            if (vitals == null) { respawnCountdown.Cancel(); yield break; }
            if (!respawnCountdown.Tick(Time.deltaTime, lost)) continue;
            vitals.RespawnAtVillageOrigin();
            combatCameraMode = true;
            CenterCameraOn(vitals.transform.position);
            Say("Você retornou ao ponto inicial da vila.", 5f);
        }
    }
}
