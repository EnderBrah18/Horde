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
        float cycle = Mathf.Max(0.1f, data.cycleSeconds);
        timer += Time.deltaTime;
        if (timer >= cycle)
        {
            timer -= cycle;
            Game.I.wallet.Add(data.produces, data.amount);
        }
    }
}
