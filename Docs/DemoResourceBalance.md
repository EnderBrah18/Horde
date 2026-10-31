# Base de recursos e combate para o primeiro test drive

Esta é uma base inicial para testes, não um balanceamento validado por uma run completa. A decisão é economia restritiva sem limite fixo de torres/cercas. A meta da demo é dez dias e pelo menos uma hora; o próximo recorte narrativo pode trabalhar os primeiros cinco dias, sem fixar o encerramento agora. Nenhum temporizador artificial ou vitória automática foi adicionado.

## Onde editar

- Game.economy → Assets/Resources/Economy/DefaultRunEconomy.asset: estoque inicial e reserva após noite vencida. Não há mais campos antigos concorrentes de madeira/cristais no Game.
- Resources/Buildings: custos, vida, dano, alcance, cadência e cadeia de melhorias.
- Resources/Enemies: atributos, personalidade, rastreamento e preferência por Tank de cada definição.
- Resources/NightDirector/Compositions: grupos disponíveis, entrada por noite e opções uniformes.
- Resources/NightDirector/DefaultNightDirector.asset: população aleatória, distribuição e multiplicadores.
- Resources/VillageFacilities e NPCs: restauração, produção com trabalhadores, moradia e recrutamento.

## Estoque e fontes

Estoque inicial: 120 madeira, 36 pedra, 10 ferro, 14 fibras, 20 grãos, 5 cristais.

Reserva por noite vencida: 12 madeira, 4 pedra, 1 cristais. É fixa e não aumenta automaticamente com o número do dia. É uma reserva mínima; instalações operacionais e expedições complementam a economia.

- Madeira/pedra: estruturas e restaurações.
- Ferro: defesas avançadas e melhorias. O recurso usado é IronOre, ainda sem refinamento separado.
- Fibras: cercas e balistas. Fazenda e coleta renovam o estoque.
- Carvão: melhorias de torres avançadas; mina e expedição fornecem.
- Grãos: recrutamento comum, fazenda e estalagem. Não existe consumo alimentar diário nesta etapa.
- Ervas: recuperação de especialistas e restauração de instalações ligadas à alquimia.
- Cristais: moeda/recurso raro de alto valor, reservado para melhorias altas, especialistas e expansão.
- Peixe, carne e espuma celestial permanecem para receitas e serviços futuros; não foi inventada uma função só para gastar todos os assets.

A rota atual contém madeira, pedra, ferro, grãos, fibras, carvão e ervas. Sua capacidade continua em 100 unidades por viagem pessoal diária. Listas de inimigos das regiões da expedição foram preservadas: lista individual vazia continua vazia.

## Construções

Os custos de melhorias abaixo são adicionais. Reembolso continua considerando o investimento acumulado e a idade da construção; melhorar não reinicia a elegibilidade de reembolso integral.

| Asset | Disponibilidade | Custo | Vida | Dano / intervalo | Alcance |
|---|---|---|---:|---:|---:|
| Cerca | menu | 8 madeira, 2 fibras | 140 | — | — |
| Cerca reforçada | melhoria | 8 madeira, 6 pedra, 2 ferro | 220 | — | — |
| Muralha de pedra | menu | 4 madeira, 16 pedra | 300 | — | — |
| Muralha reforçada | melhoria | 18 pedra, 4 ferro | 440 | — | — |
| Torre de vigia | menu | 28 madeira, 8 pedra | 180 | 9 / 1 s | 14 |
| Torre reforçada | melhoria | 22 madeira, 16 pedra, 6 ferro | 270 | 14 / 1 s | 16 |
| Torre veterana | melhoria | 28 madeira, 24 pedra, 14 ferro, 2 cristais | 380 | 20 / 0.95 s | 17 |
| Balista | menu | 42 madeira, 12 pedra, 12 ferro, 6 fibras | 220 | 32 / 2.4 s | 22 |
| Balista reforçada | melhoria | 30 madeira, 18 pedra, 14 ferro, 6 carvão | 320 | 48 / 2.2 s | 24 |
| Torre sentinela | menu | 20 madeira, 16 pedra, 8 ferro | 240 | 6 / 0.6 s | 10 |
| Sentinela reforçada | melhoria | 18 madeira, 20 pedra, 12 ferro, 4 carvão | 340 | 10 / 0.65 s | 11 |

As três famílias ofensivas têm papéis por atributos: vigia generalista, balista de alcance/impacto e sentinela de curta distância/cadência. Balistas continuam com disparo direto de alvo único; não há perfuração, explosão ou projétil físico novo. As duas famílias defensivas são cerca e muralha de pedra. Não há limites artificiais de quantidade.

TorreEspecial permanece como asset, fora do menu e sem ligação nas melhorias regulares. Os quatro objetos Ponto Defesa Especial da CenaPrincipal estão inativos; os colliders das construções existentes não foram ativados.

## Abertura viável

Um exemplo que cabe no estoque: duas vigias + restaurar casa comum + restaurar serraria + recrutar um morador. Junto do morador comum já presente, permite atribuir dois trabalhadores à serraria. Sobram 18 madeiras, 8 pedras, 16 grãos, 10 ferros, 14 fibras e 5 cristais. Atribuição de trabalho é uma decisão do jogador, não foi automatizada.

Se o jogador ignorar toda a economia, o estoque compra no máximo quatro vigias básicas; não compra uma balista inicial por faltar ferro. Isso é consequência dos preços e do estoque, não uma trava de construção. Custos de substituição, melhorias, restaurações e mão de obra concorrem pelos mesmos recursos. A expansão passa a custar 60 madeiras e mantém a cobrança de cristais.

## Produção e população

| Instalação | Nível 1, com 2 trabalhadores | Nível 2, com 2 trabalhadores |
|---|---|---|
| Serraria | 24 madeira | 36 madeira |
| Pedreira | 10 pedra | 18 pedra |
| Mina | 5 ferro, 3 carvão | 9 ferro, 5 carvão |
| Fazenda | 8 grãos, 5 fibras | 14 grãos, 8 fibras |

O mínimo continua em dois trabalhadores; nível 1 aceita até seis e nível 2 até oito. Cada dupla extra dá bônus por recurso produzido: +2 na serraria/pedreira e +1 na mina/fazenda. Os valores são editáveis por nível. Moradias comuns têm 4/6/8 vagas e essenciais 1/2/3. Serviços sem ações prontas continuam sem produção fictícia.

## Inimigos e composições

| Definição | Personalidade | Vida base | Dano / intervalo | Velocidade |
|---|---|---:|---:|---:|
| BatedorStalker | PlayerHunter / Stalker | 68 | 7 / 1.3 s | 3.3 |
| Elite | Objective | 110 | 9 / 1.5 s | 2.4 |
| Errante | Simple | 48 | 5 / 1.4 s | 2.4 |
| Escudeiro | Objective | 280 | 10 / 1.8 s | 1.7 |
| Fodder | Objective | 54 | 6 / 1.4 s | 2.5 |
| Horda | Simple | 32 | 3.5 / 1.2 s | 2.8 |
| Perseguidor | PlayerHunter / Stalker | 78 | 8 / 1.3 s | 3.2 |
| Rastreador | PlayerHunter / Tracker | 92 | 7.5 / 1.4 s | 3.1 |
| Saboteur | Saboteur | 68 | 8 / 1.6 s | 2.7 |
| Sapador | Saboteur | 86 | 11 / 1.7 s | 2.6 |
| Seguidor | Objective / cobertura de Tank | 64 | 5.5 / 1.4 s | 2.5 |
| Tank | Objective | 240 | 12 / 1.8 s | 1.6 |

| Composição | A partir da noite | Papel |
|---|---:|---|
| Caçadores Stalker | 4 | caça com detecção e memória temporária |
| Escolta com Tank | 3 | Tank, seguidores e comuns |
| Horda de errantes | 2 | numerosa, frágil, Simple; não foge ao primeiro abate |
| Linha pesada de escolta | 7 | Escudeiros e retaguarda em lotes maiores |
| Patrulha errante | 2 | preenche lotes e sobras com Simple, sem adicionar Tank implícito |
| Dupla de rastreadores | 6 | dupla Tracker |
| Ataque às torres | 3 | Sapador e Saboteur focados em torres |
| Veteranos disciplinados | 3 | grupo menor e resistente |

## Noites e resistência

A primeira noite continua fixa em 45 inimigos, três levas de três grupos no cenário atual; os lados de entrada continuam sorteados independentemente. A partir da segunda noite os perfis novos entram conforme First Night, pesos e tamanho disponível. Nenhum perfil ativo usa NPC Hunter, suporte ou outra capacidade ainda não implementada.

A população pode repetir o mínimo ou dar saltos de pelo menos 15 e 35% do mínimo, quantizados em 5; o teto do salto é 60%. O maior total das cinco noites anteriores continua definindo o piso do bloco seguinte. Não é um teto permanente e não garante crescimento em toda noite.

Multiplicadores de atributos: +6% de vida/noite (teto 1,7×), +3,5% de dano/noite (teto 1,4×), +0,5% de taxa de ataque/noite (teto 1,1×), sem crescimento de velocidade. Na décima noite a vida está em 1,54× e o dano em 1,315×. O núcleo tem 1.200 de vida e continua recuperando ao amanhecer vitorioso. Instalações têm 300 e casas 320, quebrando segundo as regras existentes.

Estimativa ideal: uma vigia acerta um Fodder de 54 HP seis vezes, levando 5 segundos do primeiro ao último disparo; um comum precisa de 30 acertos para destruir seus 180 HP. São contas isoladas, sem movimentação, concentração de vários atacantes, alcance ou participação do jogador. Não substituem combate real.

## Verificações executadas

- Compilação dos scripts de runtime e Editor.
- CheckDemoBaseline.py: referências, cadeias de melhoria sem ciclos, abertura econômica, fontes renováveis, perfis elegíveis e estimativas de combate.
- GenerateDemoSimulation.py gera uma simulação do planejador real usando os campos serializados dos assets; 100 runs × 10 noites, com Unity substituída apenas no contexto de teste. Todas as 12 definições apareceram. Faixas observadas: noite 1 = 45; noites 2–5 = 45–70; noites 6–10 = 45–110. O piso específico varia conforme o recorde real do bloco anterior.
- Validação de referências de cenas/assets e regressões de carteira, produção e composições.

A simulação não executa deslocamento, tiros, colisões, IA de perseguição nem mede duração de uma run. O teste ainda necessário é jogar com o pacote de abertura, medir perdas/reposições e receitas por manhã e cronometrar noites/expedições. Ajustar os assets após esse teste, sem declarar que dez dias já garantem uma hora.

Para reproduzir a auditoria, execute Python em Tests/Balance/CheckDemoBaseline.py. GenerateDemoSimulation.py gera Temp/DemoBalanceSimulation.cs e .rsp para o SDK .NET 10 local; compile com csc.dll do SDK e execute a DLL. Não modifica os assets.
