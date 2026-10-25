# Personalidades e perseguição

## PlayerHunter implementado

Player Hunt Style no EnemyArchetypeData escolhe Stalker (padrão) ou Tracker. Ambos precisam detectar a vítima dentro de Tactic Radius antes de abandonar o núcleo.

Tracker segue a posição atual continuamente após a primeira detecção, mesmo fora do raio. Não usa os timers de tolerância/investigação nem congela uma posição. Vítima morta ou inválida encerra a perseguição e permite retomar o núcleo. Não altera fuga ou outros sistemas sociais. O ciclo abaixo descreve Stalker.

O alvo inicial é o núcleo. O jogador só é adquirido dentro de Tactic Radius; dano não revela automaticamente um jogador distante.

1. Detectou: persegue a posição atual do jogador.
2. Saiu do raio: começa Player Hunt Grace Duration, padrão 5 segundos, editável no EnemyArchetypeData e aplicado ao Enemy ao nascer.
3. Durante a contagem, continua acompanhando a posição real do jogador.
4. Na atualização em que a contagem termina, congela a posição atual. Não usa a posição do instante da saída do raio.
5. Vai ao ponto congelado; movimentos posteriores do jogador fora do raio não alteram o destino.
6. Ao chegar sem redetectar o jogador, retoma o núcleo. Sem núcleo válido, aguarda.
7. Se o jogador entrar novamente no raio em qualquer etapa, retoma a perseguição e cancela a contagem antiga. A próxima saída inicia uma contagem completa.

Exemplo: sai do raio em t=10 na posição A. Continua correndo e chega a B em t=15. O destino memorizado é B. A posição C em t=16 não é revelada ao inimigo, salvo nova detecção.

Player Hunt Investigation Duration (20 segundos por padrão) é o limite de tentativa de alcançar a posição congelada. Serve para abandonar pontos inacessíveis; chegar ao destino encerra a investigação imediatamente, sem esperar esse limite. Vítima morta/inválida cancela a caça. Fuga e reunião do grupo continuam sistemas separados. O timer é atualizado mesmo durante repulsão enquanto a região de navegação está ativa.

Configuração vale para definições PlayerHunter e também para um inimigo que receba essa personalidade de uma composição. Os demais focos permanecem sem esse ciclo de perseguição. Obstáculos podem ser destruídos para alcançar o destino; não viram um objetivo permanente.

## Ideias reservadas, sem implementação nesta etapa

- NpcHunter: nome já disponível no Inspetor, sem seleção/ataque a moradores por enquanto. Futuramente avança ao núcleo até perceber um NPC localmente e então desvia para caçá-lo. Não conhece NPCs pelo mapa ao nascer.
- Subpersonalidade Stalker: rastreamento temporário e investigação da última posição conhecida, especialmente adequado às expedições. O ciclo atual do PlayerHunter já usa essa regra de memória; sua extração para uma opção compartilhada entre tipos de caçador fica para depois.
- Generalizar o seletor Stalker/Tracker para NpcHunter: ainda futuro; ambos já estão disponíveis no PlayerHunter.
- Ambusher: fica escondido ou em área de espera e dá o bote quando percebe uma vítima ferida. Definir depois condições de esconderijo, alcance, limiar de vida e desistência.
- Executioner: móvel, ágil e oportunista, procura vítimas feridas percebidas e aproveita chances de finalização. Diferente do Ambusher, não depende de ficar parado em um esconderijo. Velocidade, salto e outras capacidades não são fornecidos só pelo nome da personalidade.

Ambusher e Executioner não foram adicionados à enumeração nem à IA. A caça e o dano contra NPCs e a geração de módulos continuam tarefas separadas. Derrota com retorno forçado da expedição está implementada em ExpeditionDefeats.md.

## Verificação

Os testes independentes da cena usam PlayerHuntMemory para verificar detecção inicial, contagem, atualização durante tolerância, captura no fim, congelamento, redetecção, timer reiniciado, chegada, morte da vítima, tolerância zero e limite de investigação. Não substituem Play Mode.

No Play Mode: observar um PlayerHunter distante avançar ao núcleo; aproximar-se e fugir do raio; mudar de direção durante os 5 segundos e novamente depois; confirmar que ele vai à posição do fim do timer; confirmar retomada do núcleo e redetecção; repetir em expedição sem núcleo; testar um obstáculo destrutível e uma posição inacessível.
