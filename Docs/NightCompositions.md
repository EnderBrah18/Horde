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

O total é distribuído exatamente entre todos os grupos da noite, com tamanhos que diferem no máximo em um inimigo.
Não há excedente adicionado ao final. Para um total 23 em 9 grupos: cinco grupos de 3 e quatro grupos de 2.
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
- Group Compositions: perfis que poderão preencher os grupos da segunda noite em diante.

O HUD noturno e os eventos F3 exibem total planejado, total surgido, mínimo e recorde do bloco.
Initial Waves, Nights Per Extra Wave e Max Waves ainda controlam quantas levas distribuem o total;
eles não multiplicam mais a população depois da primeira noite. Commons Per Round agora só compõe a abertura,
sem acrescentar inimigos linearmente a cada noite.

## Arquétipos e perfis

Create > Village > Enemy Archetype define uma variante com referência à IA existente (Common/Tank) e atributos próprios.
Create > Village > Enemy Group Composition define:

- primeira noite elegível (nunca antes da segunda), peso de sorteio e faixa de população relativa ao mínimo;
- membros, mínimo garantido de cada arquétipo e peso para preencher as vagas restantes;
- temperamento e comportamento social uniformes opcionais;
- personalidade de alvo uniforme opcional (Objective/Hunter para comuns; Tank conserva sua função);
- tática agressiva ou tática forçada opcional.

Perfil incompatível com o tamanho do grupo ou sem membros válidos é ignorado. Se nenhum servir, usa a composição comum
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
