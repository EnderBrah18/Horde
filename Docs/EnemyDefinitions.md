# Definições e criação de inimigos

## Dados compartilhados

EnemyArchetypeData é a definição usada por noites, encontros futuros e inimigos colocados em cenas.
O script fica em Assets/Scripts/Enemies/Definitions; os assets compartilhados ficam em Assets/Resources/Enemies.
Os antigos assets de arquétipos foram movidos preservando seus GUIDs, sem quebrar as composições existentes.

Exemplos:

- Fodder: comum, esfera, comportamento base de estruturas.
- Saboteur: cubo, personalidade com foco rígido em torres.
- Tank: cilindro, capacidades de Tank e personalidade Objective.
- Horda/Elite: variantes de atributos usadas pelas composições noturnas existentes.
- Perseguidor: exemplo de prioridade no jogador, percepção de 80 metros, sem dependência do núcleo.

Create > Village > Enemy Archetype cria novos inimigos sem duplicar a IA. Configure nome, vida, dano,
velocidade, alcance, resistência à repulsão, escala, personalidade, foco e parâmetros sociais.
Runtime Type continua escolhendo capacidades já programadas (Common/Tank); Saboteur é uma personalidade/definição,
não uma terceira enumeração física. Novas capacidades como tiro ou cura exigirão comportamento próprio.

## Foco imposto pela personalidade

- Objective (valor serializado 0): busca o núcleo local, independentemente da distância. Não troca por torres, jogador, facilities, ordem de líder ou reação a dano.
- Saboteur (valor 1, antigo Hunter): busca a torre válida mais próxima em toda a região, sem depender do raio de percepção. Sem torre, aguarda; não usa o núcleo como destino secundário.
- NpcHunter (valor 3): nome reservado para futura caça a moradores percebidos localmente. Ainda não seleciona nem ataca NPCs, estruturas ou jogador substituto.
- Simple (valor 4): escolhe a estrutura ou jogador mais próximo dentro de Tactic Radius. Muros são somente obstáculos. Sem alvo local, avança para o núcleo da região, podendo mudar de alvo ao perceber algo mais perto. Sem núcleo e sem alvo percebido, aguarda.
- PlayerHunter (valor 2): avança para o núcleo até detectar o jogador em Tactic Radius. Ao sair do raio, rastreia por Player Hunt Grace Duration (padrão 5 s); ao terminar o tempo, memoriza a posição atual da vítima, sem atualizar depois. Investiga essa posição e retoma o núcleo ao chegar sem reencontrar o alvo. Redetecção cancela o timer e retoma a caça. Pode ser aplicado a qualquer tipo físico, inclusive Tank. Sem núcleo, aguarda após terminar a busca. Player Hunt Investigation Duration (padrão 20 s) limita buscas inacessíveis.

A prioridade duplicada Target Priority foi removida. O tipo Tank mantém atributos e características físicas, mas não escolhe o objetivo. Composições com Uniform Personality também podem alterar o foco de Tanks.
Os GUIDs dos assets foram preservados; Hunter.asset passou a Saboteur.asset e Perseguidor passou a PlayerHunter. A personalidade da abertura mantém os valores numéricos anteriores.

Target Player Without Core mantém o fallback contextual anterior: em mapas sem núcleo, Objective e Saboteur podem procurar o jogador dentro de Tactic Radius. Desmarque para manter estritamente o alvo da personalidade mesmo sem núcleo.
Estruturas só são atacadas como obstáculos de passagem ou quando correspondem ao foco. Obstáculos indestrutíveis continuam bloqueando; dano e repulsão continuam funcionando. Moral, fuga e reunião de grupo continuam sistemas separados.
Sem alvo principal válido, as personalidades de foco rígido aguardam; Simple mantém a seleção local descrita acima. O foco em NPCs ainda precisa da integração de seleção/ataque a moradores.

A busca considera apenas estruturas atacáveis na mesma cena e dentro do GridWorld. A vila carregada em paralelo não fornece alvos aos inimigos da expedição.

## Visual e criação

Prefab é opcional. Sem ele, Primitive Shape e Balance > Scale montam o protótipo.
Um prefab deve ter um Renderer na raiz ou nos filhos. Seu pivô deve estar no chão;
a escala autoral é mantida, salvo se Apply Scale To Prefab estiver marcado.
Enemy e Health são adicionados à raiz quando ausentes. Para o outline atual em prefabs,
o componente usa MeshFilter na raiz; prefabs mais complexos ainda precisarão adaptar esse efeito.

EnemyFactory.Spawn(definition, groundPosition, grid, group, parent, overrides) centraliza criação,
vida, camada, visual, definição, prioridades e vínculo de grupo. Group e Parent são opcionais;
um inimigo sem grupo começa em marcha, sem esperar uma reunião que nunca acontece.
Spawn Overrides permite ao diretor noturno aplicar composição social. Os atributos de combate vêm do asset em todas as noites, inclusive na abertura.

Para preparar um inimigo diretamente numa cena de teste:

1. Crie seu objeto visual, adicione Enemy e atribua Archetype.
2. Tenha um GridWorld configurado na mesma cena e um jogador com PlayerVitals/Health.
3. Use Perseguidor ou Personality = PlayerHunter. Não é necessário ter core.

A inicialização espera a navegação da própria cena. Em geração modular/aditiva, prefira chamar EnemyFactory
com o GridWorld pronto e Parent da região, posicionando o inimigo dentro de seus limites.

## Compatibilidade e próximas etapas

As noites continuam passando pelo mesmo planejador, preservando a contagem fixa da abertura e o histórico por blocos.
A abertura usa os atributos das definições Fodder/Saboteur/Tank; perfis posteriores usam os atributos dos seus assets.
Os atributos de inimigos são configurados nas definições; variantes usam definições e composições próprias.
A escolta de exemplo agora garante um comum, um Saboteur e um Tank quando cabe no grupo.
Composição com menos vagas que os mínimos configurados usa outro perfil válido ou o fallback.

A derrota em expedições agora retorna automaticamente à vila, com perda de carga e vida reduzida configuráveis no ExpeditionManager. Veja ExpeditionDefeats.md. Debuffs adicionais de atributos continuam pendentes.

Compilação e testes do diretor verificam estabilidade da abertura, quotas, prioridades e filtros de contexto com substitutos de Unity.
No Play Mode, validar: Fodder/Saboteur/Tank na vila; Perseguidor em mapa sem núcleo; Tank sem núcleo com fallback;
jogador atrás de obstáculo; core em outra cena carregada; prefab e inimigo solo; contagens e levas das noites.

Uniform Bravery nas composições força Bravery (0 a 1,2) para todos os integrantes, inclusive Tanks, tanto nas noites quanto nas expedições. Desligado preserva o sorteio noturno ou a bravura do asset na expedição. Não muda inimigos individuais e não altera a composição fixa da primeira noite.

Planejamento de personalidades e rastreamento futuro: veja EnemyPersonalityRoadmap.md.

Player Hunt Style seleciona Stalker (padrão, com os timers descritos) ou Tracker (perseguição contínua após primeira detecção). Em Tracker, sair do raio não encerra o rastreamento e os timers não são usados. A vítima ainda precisa estar viva e válida na região de navegação.

Prefer Tank Cover configura cobertura atrás de Tanks percebidos, inclusive de outros grupos. Distância e espaçamento ficam no mesmo asset. Essa preferência é independente da personalidade e vem desligada. Detalhes: EnemyPositioning.md.
