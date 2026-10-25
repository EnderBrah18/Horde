# Dias, levas e descoberta de expedições

## Calendário

Continuam existindo somente manhã e noite, sem relógio automático nem período de tarde.
A partida começa na manhã do Dia 1. Espaço inicia a noite desse mesmo dia; vencer a noite leva à manhã do dia seguinte.
No amanhecer vitorioso, núcleo e jogador vivos recuperam toda a vida até seus máximos atuais.
Isso não repara defesas ou instalações e não ressuscita atores derrotados. Voltar de expedição no mesmo dia não ativa essa recuperação.
Perder não avança o calendário. Tab conserva a troca atual entre os modos de câmera/controle.

A interface mostra Dia e, durante a noite, Leva atual/total. Uma leva é uma rodada de combate com vários grupos.
Dentro dela, os grupos usam Spawn Interval e Group Interval e podem coexistir. A próxima leva só começa após
todos os grupos da leva atual terminarem de surgir e todos os inimigos morrerem ou desertarem.
O amanhecer ocorre somente depois da última leva. Produção e recompensas continuam sendo entregues uma vez por noite.

Em Game > Balanceamento das noites:

- Groups Per Wave: grupos por leva; padrão 3.
- Initial Waves: levas da primeira noite; padrão 3.
- Nights Per Extra Wave: noites necessárias para acrescentar uma leva; padrão 2.
- Max Waves: limite de levas; padrão 5.
- Group Interval: intervalo entre os grupos da mesma leva.
- Wave Interval: pausa depois de resolver uma leva, antes da próxima; padrão 5 segundos.
- Base Common Enemies + Commons Per Round determinam os comuns por grupo da primeira noite.
- A partir da segunda noite, Night Director determina o total e os perfis; consulte NightCompositions.md.

Com Initial Waves/Nights Per Extra Wave/Max Waves em 3/2/5: noites 1 e 2 têm 3 levas;
noites 3 e 4 têm 4; a partir da noite 5, são 5. Com Groups Per Wave = 3, a primeira noite tem
9 grupos no total: 3 na primeira rodada, 3 na segunda e 3 na terceira.
A vila usa a configuração da cena e do diretor noturno.
Vida e dano dependem dos arquétipos sorteados. A progressão do mínimo de inimigos ocorre em blocos de cinco noites,
usando o recorde do bloco anterior, e não um aumento linear fixo.

Game.Day fornece o dia exibido. O antigo estado serializado round migra para nightsStarted.
Game.round permanece como propriedade de compatibilidade para o identificador de noite usado pela produção
e pelo reembolso das construções. Não representa uma leva. CurrentWave e TotalWaves representam as levas.
Isso mantém a entrega única no amanhecer e a queda do reembolso ao iniciar a primeira noite da construção.

Show Initial Day Guidance exibe orientações na primeira manhã; não é um tutorial com objetivos obrigatórios.

## Descoberta

Assets/Resources/Expeditions/RotaDaMata.asset configura a primeira rota, Passagem da Mata.
Discovery Day = 2 libera a rota na manhã seguinte à primeira noite vencida.
Use 3 para liberar após duas noites. A condição é conferida ao iniciar a partida e depois de cada amanhecer vitorioso.

Display Name, Description e Discovery Message são editáveis. A descoberta inicial usa uma mensagem de um
sobrevivente que encontrou uma passagem; um NPC mensageiro com chegada animada ainda não foi implementado.
O aviso aparece na tela e no registro F3; a mensagem permanece consultável no posto de expedições.
Cada destino é descoberto uma única vez por partida, e continua liberado depois das viagens.

Game > Descoberta de expedições > Expedition Destinations permite escolher destinos. Vazio carrega Resources/Expeditions.
Novos destinos são criados com Create > Village > Expedition. Não há custo ou missão adicional de liberação nesta versão.
Destinos desconhecidos não são listados. Destinos descobertos ficam disponíveis se sua cena estiver registrada na build.

## Ponto de acesso e viagem inicial

CenaPrincipal contém Posto_Expedicoes em (-8, 0.75, -12), próximo ao jogador inicial.
Em outras cenas com Game sem posto, um marcador equivalente é criado ao iniciar.

1. Vença a primeira noite e leia a notícia na manhã do Dia 2.
2. Use Tab para exploração, aproxime-se do posto e pressione E.
3. Escolha Viajar: Passagem da Mata.
4. Na região de teste, mova-se com WASD. Perto do marcador amarelo, E retorna à vila.

A cena Expedicoes é carregada de forma aditiva. A região inicial tem trilhas, floresta e pontos de coleta,
criados em runtime longe da vila. Seu tamanho é configurado em Test Region Size, limitado entre 11 e 101 células de 2 metros.
Essa cena é preparada para carregar a partir da vila; abrir e jogar apenas ela não inicializa uma partida completa.
Coleta e carga estão disponíveis, com distribuição de recursos por semente. Ainda não há inimigos ou geração por regiões infinitas.
Veja ExpeditionCollection.md para parâmetros de recursos, carga e câmera.

A vila não é descarregada/reconstruída. O mesmo Game e jogador são mantidos; o contexto de GridWorld é trocado
temporariamente e restaurado no retorno. NPCs e reassentamento ficam suspensos; não é possível construir,
atribuir trabalhos, alternar para construção ou iniciar a noite durante a viagem. A volta mantém o mesmo dia.
Vida do jogador, saldos, casas, trabalhos, níveis e identificadores de reembolso permanecem nos objetos originais.
R reinicia a cena da vila e descarta a partida/viagem, como o reinício anterior. Não há save em disco.

Build Settings começa por MenuPrincipal (antiga SampleScene) e inclui CenaPrincipal e Expedicoes.
O menu tem Nova run e Sair. Nova run inicia a vila do zero; a derrota na vila oferece retorno ao menu.
O nome de cena em ExpeditionData deve coincidir com uma cena incluída na build.

## Verificação

Tests/Progression/ProgressionTests.cs cobre 37 casos de calendário, sequência de levas, condições de descoberta e limite diário pessoal.
Os testes de população e instalações continuam verificando vagas, trabalhadores e pagamento único.
Esses testes não substituem o Play Mode para carregamento aditivo, câmera e input.

Roteiro manual: Dia 1 sem rota; 3 levas da primeira noite; derrota sem desbloqueio; vitória → Dia 2 e aviso único;
E no posto; viajar/retornar duas vezes; conferir vida, saldo, NPCs, atribuições e reembolso; iniciar a noite do Dia 2;
alterar Discovery Day para 3 e confirmar liberação somente após duas noites vencidas.

Morte do jogador na vila agora inicia respawn de 10 segundos com câmera livre, sem encerrar a noite. Só o núcleo destruído encerra a run. O fluxo de derrota na expedição permanece separado. Veja PlayerRespawn.md.
