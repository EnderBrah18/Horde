# Composição das noites

## Primeira noite fixa

Night 1 usa as quantidades de Game: Initial Waves (limitado por Max Waves), Groups Per Wave e
Base Common Enemies + Commons Per Round por grupo. Se Tank Every Rounds = 1, inclui também um Tank por grupo.
Na CenaPrincipal atual: 3 levas × 3 grupos × 5 comuns = 45 inimigos.

A composição da primeira noite é preparada com First Night Seed, independente do sorteio das entradas.
Perfis aleatórios de grupo não alteram essa abertura. Os indivíduos recebem a mesma composição planejada em cada run,
com temperamento disciplinado e comportamento proativo; decisões da IA durante a batalha continuam dinâmicas.
As posições de entrada são sorteadas individualmente por grupo entre os pontos ativos, como antes.
Não há rodízio obrigatório: todos os grupos podem sair do mesmo lado. A CenaPrincipal mantém as quatro entradas ativas.

## Mínimo por blocos de cinco noites

O histórico é exclusivo da run, sem persistência entre tentativas:

- Noites 1–5: mínimo igual ao total da primeira noite.
- Noites 6–10: mínimo igual ao recorde das noites 1–5.
- Noites 11–15: mínimo igual ao recorde das noites 6–10; assim por diante.

O recorde é registrado com o total realmente surgido ao vencer a noite, incluindo inimigos que depois desertaram.
Não é número de abates. Uma noite numerosa não aumenta o mínimo dentro do mesmo bloco.
Exemplo: primeira noite com 10; maior noite do bloco com 23 → mínimo 23 nas noites 6–10, sem arredondar para 25.

## Variação e grupos

Depois da primeira noite, o diretor sorteia o total antes do combate. O padrão permite repetir o mínimo com 25% de chance.
Caso sorteie um aumento, exige ao menos 10 inimigos extras ou 50% do mínimo, o maior dos dois,
arredondando somente o acréscimo para lotes de 5. O acréscimo máximo padrão é 200% do mínimo.
Com mínimo 10, as opções são 10, 20, 25 e 30; não 11 ou 15.

Depois da abertura, a distribuição também é sorteada. O diretor pode manter as levas previstas,
concentrar a maior parte dos inimigos em uma delas ou reunir a noite em um único ataque.
Cada leva escolhe entre grupos dispersos, grupos equilibrados e um exército com possíveis grupos menores de apoio.
O peso de exércitos aumenta com o número da noite. O tamanho do grupo não fica limitado a cinco.
Exemplo com 60 inimigos em uma leva: 12 grupos de 5, 3 grupos de 20 ou um grupo de 60.
Uma noite de 90 pode concentrar 60 em uma leva e repartir os outros 30 entre as demais.
Os totais sempre fecham exatamente; LoneWolves que se separam não são repostos.
O limiar de reunião do grupo se ajusta ao seu tamanho. Se uma configuração pedir mais grupos do que inimigos,
a quantidade de grupos/levas se ajusta para não produzir grupos vazios.
Cada leva continua esperando os inimigos da anterior morrerem ou desertarem antes de avançar.

## Inspector

Game > Composição e histórico das noites > Night Director aponta para
Assets/Resources/NightDirector/DefaultNightDirector.asset. Sem referência, o mesmo asset é carregado como padrão.

No diretor:

- First Night Seed: composição reproduzível da primeira noite.
- Repeat Minimum Chance: chance de repetir a quantidade mínima.
- Minimum Extra Enemies: menor aumento absoluto.
- Minimum Increase Percent: menor aumento relativo.
- Maximum Increase Percent: maior aumento relativo.
- Extra Enemy Batch Size: lote usado para os acréscimos.
- Single Assault Chance: chance de reunir toda a noite em uma única leva com um único grupo (padrão 10%).
- Concentrated Wave Chance: chance de distribuir as levas com uma força dominante (padrão 50%, quando não há ataque único).
- Minimum/Maximum Dominant Share: fração sorteada para a força dominante (padrão 55–80%); respeita ao menos um inimigo nas demais partes.
- Dispersed/Balanced/Army Group Weight: pesos dos três formatos de grupo em cada leva (padrão 1/1/1).
- Army Weight Per Night: peso adicional de exércitos após a segunda noite (padrão 0,1 por noite).
- Maximum Group Spawn Duration: tempo máximo de materialização do grupo inteiro (padrão 2 s; 0 = todos juntos).
- Health/Damage/Speed/Attack Rate Increase Per Night: acréscimos lineares sobre os atributos originais do arquétipo.
- Maximum Health/Damage/Speed/Attack Rate Multiplier: limites dos multiplicadores.
- Group Compositions: perfis que poderão preencher os grupos da segunda noite em diante.

O HUD noturno e os eventos F3 exibem total planejado, total surgido, mínimo e recorde do bloco.
Initial Waves, Nights Per Extra Wave e Max Waves controlam as levas previstas quando não ocorre ataque único;
eles não multiplicam mais a população depois da primeira noite. Commons Per Round agora só compõe a abertura,
sem acrescentar inimigos linearmente a cada noite.

Groups Per Wave mantém a abertura e serve de referência para o formato equilibrado nas noites seguintes.
O formato disperso usa o tamanho inicial como referência; o formato de exército pode reunir toda uma leva
em um grupo ou dividir uma força dominante e apoios. Cada grupo sorteia sua entrada independentemente,
sem rodízio obrigatório: todos podem surgir do mesmo lado. Não há garantia de distribuir os apoios pelos outros três lados.

Grupos grandes materializam vários indivíduos por frame quando necessário para respeitar a duração máxima,
evitando uma fila de 60 × Spawn Interval. O intervalo entre grupos permanece no Game. O grupo reúne sua força
antes de marchar, salvo comportamentos individuais como LoneWolves e a antecipação de rally por Tanks.

Os atributos usam multiplicador = min(limite, 1 + (noite − 1) × acréscimo), sem alterar os assets.
Padrão: vida +8% por noite (máximo 4×), dano +5% (máximo 3×), velocidade +0,5% (máximo 1,15×)
e frequência de ataque +1% (máximo 1,35×; divide Attack Cooldown).
Na noite 20: vida 2,52×, dano 1,95×, velocidade 1,095× e frequência 1,19×.
Alcance, percepção, escala e resistência à repulsão continuam pertencendo ao arquétipo.
A primeira noite não recebe multiplicadores nem as novas distribuições. Estes valores são um ponto inicial de balanceamento,
que precisa de validação no Play Mode junto da economia, defesas e capacidade de dano do jogador.

## Arquétipos e perfis

Create > Village > Enemy Archetype define uma variante com referência à IA existente (Common/Tank) e atributos próprios.
Create > Village > Enemy Group Composition define:

- primeira noite elegível (nunca antes da segunda), peso de sorteio e faixa de população relativa ao mínimo;
- membros, mínimo garantido de cada arquétipo e peso para preencher as vagas restantes;
- Spawn Count: quantidade fornecida por chamada; 0 preenche o restante do lote (compatibilidade com assets existentes);
- temperamento e comportamento social uniformes opcionais;
- personalidade de foco uniforme opcional (Objective/Saboteur/PlayerHunter), aplicada a todos os tipos, inclusive Tanks;
- tática agressiva ou tática forçada opcional.

Um lote pode consumir várias composições: uma com Spawn Count = 2 fornece dois indivíduos;
outra composição é sorteada para preencher as três vagas restantes de um lote de cinco.
A mesma composição pode ser sorteada novamente. A contribuição é limitada às vagas restantes;
um perfil cujos mínimos não cabem nessa contribuição é ignorado. Cada indivíduo conserva
as configurações sociais da composição que o forneceu. A primeira composição com Force Tactic
define a tática do grupo físico, pois ele só possui uma tática. A abertura fixa não muda.

Perfil incompatível com as vagas restantes ou sem membros válidos é ignorado. Se nenhum servir, usa a composição comum
com os tipos já existentes, sem mudar a quantidade planejada. Misturas garantidas podem usar Weight = 0 e Minimum Per Group = 1
para incluir exatamente um Tank, preenchendo as outras vagas com comuns.

Exemplos incluídos: horda fraca e numerosa para populações altas, veteranos para populações próximas do mínimo,
e escolta com exatamente um Tank por grupo. Tudo é editável nos assets.

Esses dados preparam os futuros grupos de animais, snipers e suportes. Criar um asset não implementa tiro, cura ou nova IA:
esses comportamentos precisam de código próprio quando os novos inimigos forem adicionados.

## Verificação

Tests/Nights/NightDirectorTests.cs usa o planejador, as definições e o histórico reais com substitutos de Unity.
Cobre abertura fixa, 5.000 sorteios, blocos de cinco noites, recordes, quotas exatas, composição com Tank,
uniformidade social e fallback de perfis inválidos. Não simula movimento ou combate.
No Play Mode: comparar duas primeiras noites de runs diferentes, conferir F3 e contagens,
vencer cinco noites para conferir o novo piso na sexta e testar perfis em grupos pequenos.
