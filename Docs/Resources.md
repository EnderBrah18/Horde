# Recursos do jogo

## Organização

- `Assets/Scripts/Economy/Resources/ResourceData.cs`: definição editável de um recurso.
- `Assets/Scripts/Economy/Resources/ResourceAmount.cs`: referência a um recurso e quantidade.
- `Assets/Scripts/Economy/Resources/DefaultResources.cs`: acesso à madeira e aos cristais usados pelo início da partida e expansão.
- `Assets/Scripts/Economy/Wallet.cs`: saldos da partida e operações de recebimento/pagamento.
- `Assets/Resources/ResourceDefinitions/`: assets, separados em Materials, Food e Magical.

## Criar um recurso

1. Na pasta desejada, use **Create > Village > Resource**.
2. Preencha `Id` com um identificador único e estável, como `iron_ore`.
3. Edite `Display Name`, `Description`, `Category`, `Icon` e `Color`.
4. Arraste esse asset para `Production Per Night` em um nível de VillageFacilityData ou para uma entrada em `Costs`.

Duplicar um asset também funciona; altere seu Id para não repetir o identificador original.
Nome e descrição podem mudar. Preserve o Id quando futuros saves passarem a usá-lo.
Os ícones iniciais estão vazios, prontos para receber sprites.

## Quantidades e custos

O ScriptableObject descreve o recurso, mas não guarda o saldo. Cada carteira guarda suas próprias quantidades, sem modificar os assets.

```csharp
wallet.Add(resource, 10);
int available = wallet.Get(resource);
bool paid = wallet.Spend(resource, 3);

var costs = new[]
{
    new ResourceAmount(wood, 20),
    new ResourceAmount(stone, 8)
};
if (wallet.Spend(costs)) { /* construir */ }
```

Pagamentos com vários recursos são integrais: sem saldo suficiente, nenhum recurso é retirado.
Entradas repetidas do mesmo recurso são somadas antes da validação. Valores negativos são recusados.
O evento `Changed(ResourceData, int)` fornece o novo saldo para a futura HUD em Canvas.

## Integração atual

- As construções existentes mantêm seus custos e devolvem 70% dos recursos ao serem vendidas.
- `BuildingData.Costs` aceita qualquer combinação de recursos. Uma lista vazia significa construção gratuita.
- Instalações usam `VillageFacilityData.Levels[].Production Per Night`, com vários recursos por nível. A entrega acontece pelo `VillageProduction`, no evento de amanhecer.
- Os antigos Suprimentos foram migrados para Cristais de Mana, mantendo quantidades iniciais, recompensas e custos.
- Expansão e início da partida ainda usam os dois recursos padrão. Seus campos de quantidade continuam no Game, VillageBuildArea e PlaytestScenario.
- Os novos assets não criam pontos de coleta, receitas ou produtores automaticamente.

Não mova nem remova os assets `Materials/Wood` e `Magical/ManaCrystal` sem atualizar os caminhos em DefaultResources.
Outros recursos são ligados por referência e podem ser organizados em outras pastas.

## Testes

`Tests/Economy/WalletTests.cs` exercita a implementação real de Wallet e ResourceAmount fora do Unity,
com substitutos mínimos para o asset ResourceData e o atributo Min. Os testes verificam saldos,
pagamento integral, custos repetidos, custos inválidos, overflow e notificações para a HUD.
