using UnityEngine;

// Gera recurso a cada ciclo. Por enquanto cai direto na carteira;
// na etapa seguinte vira "acumula e o jogador coleta" (com automação como upgrade).
public class Producer : MonoBehaviour
{
    BuildingData data;
    float timer;

    public void Setup(BuildingData d) { data = d; }

    void Update()
    {
        if (data == null || Game.I == null || Game.I.lost) return;
        timer += Time.deltaTime;
        if (timer >= data.cycleSeconds)
        {
            timer -= data.cycleSeconds;
            Game.I.wallet.Add(data.produces, data.amount);
        }
    }
}
