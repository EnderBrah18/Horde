# Instalações da vila

## Organização

- `Assets/Scripts/BuildingData.cs`: construções posicionáveis, com tipos Wall e Tower, categorias Defesas e Ofensivas e custos em recursos.
- `Assets/Scripts/Village/VillageFacilityData.cs`: identidade, tipo, vida e níveis de uma instalação.
- `Assets/Scripts/Village/VillageFacility.cs`: estado e operações da instalação colocada na cena.
- `Assets/Scripts/Village/VillageProduction.cs`: entrega de produção após uma noite vencida.
- `Assets/Resources/VillageFacilities/`: definições das 12 instalações e das casas da CenaPrincipal.

## Configurar uma instalação

1. Crie o asset com **Create > Village > Facility**.
2. Configure nome, descrição, ícone, tipo e vida máxima.
3. Em **Levels**, o primeiro elemento contém o custo de desbloqueio e os recursos produzidos no nível 1.
4. Adicione elementos para os níveis seguintes. Cada um define seu próprio custo de melhoria e sua produção completa por noite; a produção não é somada com a dos níveis anteriores.
5. No objeto existente da cena, adicione **VillageFacility** e arraste o asset para **Data**.
6. Marque **Start Unlocked** para iniciar ativo; **Starting Level** define o nível inicial nesse caso.

Os objetos iniciais começam bloqueados. Para testar em Play Mode durante o dia, use o menu de contexto do componente:

- **Desbloquear (pagar custo em Play Mode)**
- **Melhorar (pagar custo em Play Mode)**

Essas operações debitam a carteira real. A futura interface/interação poderá chamar
`facility.TryUnlock(Game.I.wallet)` e `facility.TryUpgrade(Game.I.wallet)` e consultar `Level`, `IsUnlocked` e `CanUpgrade`.
Os métodos representam as operações econômicas; a interface deve controlar quando são oferecidos ao jogador.

O atual nível fica visível no Inspector durante a execução. Alterações e desbloqueios não são salvos entre partidas.

## Produção

O componente **VillageProduction** está no mesmo objeto que Game nas duas cenas.
Ele ouve **DawnReached** e paga apenas instalações ativas, desbloqueadas e vivas.
Cada instalação registra a rodada paga para impedir duplicação.
Não existe produção por segundos, ao iniciar a cena ou após uma derrota.

Os exemplos de Serraria, Pedreira, Mina e Fazenda têm produção configurada e exigem trabalhadores atribuídos e aptos.
Em cada nível, edite Minimum Workers (mínimo, padrão 2), Maximum Workers (limite, padrão 8),
Extra Workers Per Bonus (trabalhadores extras por grupo, padrão 2) e Production Bonus Per Group (bônus por grupo, padrão 1).
Ao atingir o mínimo, entrega a produção base; cada grupo extra completo soma o bônus a cada recurso distinto produzido.
Os valores são por nível e o bônus não se acumula entre noites. O painel exibe a previsão conforme a equipe atual.
As demais instalações são serviços sem saída automática inicial; receitas ainda serão implementadas.
É possível configurar produção em uma instalação de serviço também; a regra de trabalhadores aplica-se ao tipo Production.
Consulte VillageNpcs.md para recrutamento e atribuição.

Os assets têm dois níveis de exemplo. Os valores de Pedreira, Mina e Fazenda são iniciais para teste e podem ser balanceados no Inspector.
O primeiro nível da Serraria conserva o antigo custo e a quantidade do Producao.asset, agora pagos por noite, sem temporizador.

As recompensas de amanhecer já existentes em Game (Dawn Wood e Dawn Crystals Base) continuam separadas da produção. Configure Dawn Wood como zero caso a madeira deva vir exclusivamente das instalações/coleta.

## Migração

O antigo Producer e os campos de produção do BuildingData foram removidos.
O asset Producao virou VillageFacilities/Serraria, preservando seu GUID.
O Core.asset antigo, sem referências nas cenas, foi removido: o núcleo do mapa de teste agora é criado diretamente como Structure.
Os estados de collider dos objetos da cena não foram alterados.
StructureKind.Facility preserva o valor numérico 3 do antigo Producer para os registros internos do mapa e da IA.

## Verificação

`Tests/Village/FacilityTests.cs` exercita os scripts reais com substitutos mínimos da API Unity.
Os casos cobrem abandono, quebra, reparo, capacidade, navegação, custos, melhoria, pagamento por rodada,
saídas inválidas, derrota e duplicação de eventos/gestores. A aparência e as interações na cena ainda precisam de teste em Play Mode.
