# Região e coleta da expedição

A região inicial deixou de ser somente o chão do teste de transição. Agora contém trilhas em cruz,
árvores, pedras, minérios de ferro, grãos e uma faixa de floresta delimitando o mapa.
Os visuais são protótipos feitos com primitivas, sem dependência de modelos externos.

## Jogar

1. Descubra a rota e viaje pelo posto como antes.
2. Ande com WASD. Perto de um recurso, a indicação mostra E e o estoque restante.
3. Pressione E para coletar uma parcela do estoque; repita para coletar mais.
4. A carga temporária aparece à esquerda. O padrão é 100 unidades somando todos os tipos de recursos.
5. Volte ao marcador amarelo no cruzamento central e pressione E para entregar a carga e retornar.
6. Consulte Armazém da vila no posto para conferir todos os saldos, inclusive recursos fora do HUD principal.

O jogador pode fazer somente **uma expedição pessoal por dia**, compartilhada entre todos os destinos.
O painel do posto informa essa regra. Após a viagem, os botões ficam bloqueados e o painel avisa:
“Já está muito tarde. Você não pode sair em outra expedição hoje. Aguarde o próximo dia.”
A próxima manhã libera uma nova saída automaticamente. Trocar de modo ou de destino não renova a saída.
Uma falha ao carregar a cena não consome a viagem; o registro ocorre quando a expedição está pronta para jogar.
Essa restrição é exclusiva do jogador. O limite de futuras expedições de NPCs ainda não foi definido.

Coleta não credita a vila imediatamente. O retorno deposita todos os recursos uma única vez.
Se faltar espaço na carga, apenas a quantidade que cabe é removida do nó. Um nó esgotado desaparece e libera o caminho.
Novas viagens geram estoques novos; o mundo atual é uma região limitada, sem expansão infinita por chunks.
Reiniciar com R descarta a partida e a carga, como já ocorria. Ainda não há inimigos na região nem uma nova regra de morte/recuperação.

## Inspector

No asset RotaDaMata:

- Test Region Size: tamanho em células de 2 metros (11 a 101).
- Seed: semente inicial.
- Vary Seed Per Visit: altera a distribuição a cada visita; desligue para repetir o mapa com a mesma configuração.
- Resource Nodes: tipos de coleta; vazio usa Resources/Expeditions/Nodes.
- Cargo Capacity: capacidade total em unidades, inicialmente 100; não é peso.
- Exploration Camera Zoom: aproximação da câmera na região, inicialmente 18. O zoom da vila é restaurado ao voltar.

Cada ResourceNodeData é criado com Create > Village > Resource Node e referencia um ResourceData existente:

- Resource: recurso entregue.
- Visual/Color: árvore, rocha, caixa ou plantação e sua cor.
- Nodes Per Region: quantidade desejada de pontos, limitada a 200 por definição.
- Minimum/Maximum Stock: estoque sorteado por ponto, limitado a 1 a 1.000.000.
- Amount Per Interaction: quantidade coletada a cada E.

Há exemplos de madeira, pedra, minério de ferro e grãos em Assets/Resources/Expeditions/Nodes.
O gerador tenta posicionar os pontos com espaçamento, fora das trilhas e da área de chegada.
Mapas pequenos ou configurações muito densas podem conter menos pontos do que a quantidade desejada.
Os obstáculos entram na navegação GridWorld sem ativar colliders de construções da vila.

## Organização e validação

Game.ExpeditionGeneration organiza os visuais e a distribuição por semente. ResourceNode mantém o estoque da instância;
ResourceNodeData contém a definição editável. ExpeditionCargo controla capacidade e entrega única.
Wallet.TryDeposit valida e credita todo o lote antes de notificar observadores da carteira.

Tests/Expeditions/CargoTests.cs cobre 23 verificações de coleta, capacidade, entrega, duplicação, overflow,
falhas de observadores e esgotamento/liberação do nó usando os scripts reais com substitutos mínimos de Unity.
A compilação e esses testes não verificam a apresentação ou a navegação real.

No Play Mode: confira os quatro recursos, colisão/navegação entre árvores, E até esgotar um nó,
carga cheia/parcial, retorno com carga vazia e cheia, saldos no posto, segunda viagem e restauração do zoom.
