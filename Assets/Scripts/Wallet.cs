public enum ResourceType { Wood, Supplies }

// Carteira simples com dois recursos:
//  Madeira     = local, produzida pela vila (Serraria)
//  Suprimentos = raros, chegam por fora (por enquanto, a cada amanhecer; depois, pelos círculos mágicos)
public class Wallet
{
    public int Wood;
    public int Supplies;

    public int Get(ResourceType t) => t == ResourceType.Wood ? Wood : Supplies;

    public void Add(ResourceType t, int amount)
    {
        if (t == ResourceType.Wood) Wood += amount;
        else Supplies += amount;
    }

    public bool CanAfford(int wood, int supplies) => Wood >= wood && Supplies >= supplies;

    public bool Spend(int wood, int supplies)
    {
        if (!CanAfford(wood, supplies)) return false;
        Wood -= wood;
        Supplies -= supplies;
        return true;
    }
}
