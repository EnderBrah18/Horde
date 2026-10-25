# Inimigos em módulos de expedição

Adicione ExpeditionEnemyRegion à raiz de cada prefab modular. O retângulo vermelho aparece ao selecionar o objeto; Local Center e Region Size estão em coordenadas locais, acompanhando posição e rotação do módulo.

## Conteúdo

- Individual Count: quantidade de inimigos soltos, sem EnemyGroup. Individuals define as opções locais; vazio não gera individuais, mesmo com Individual Count maior que zero. A própria região determina os tipos; o ExpeditionManager cadastra somente destinos.
- Group Count: quantidade de grupos. Compositions aceita os mesmos EnemyGroupCompositionData das noites; sorteio por Selection Weight.
- Spawn Count da composição define o tamanho; zero usa Default Group Size. Minimum Per Group é obrigatório; o tamanho cresce se necessário para acomodar os mínimos. Weight preenche as vagas restantes. Perfis com mais de 200 membros são recusados.
- Os atributos e prioridades vêm de EnemyArchetypeData. As opções uniformes e a tática da composição são aplicadas aos grupos. First Night e limites de população da composição se aplicam apenas às noites, não aos módulos.
- Cada região escolhe seus inimigos em Individuals e suas composições em Compositions. Não existe autorização adicional de tipos no ExpeditionManager.

## Posições

- Random In Region ignora marcadores e sorteia em toda a área.
- Spawn Points usa Spawn Points; Random Fallback permite posições aleatórias quando os pontos não servem.
- Mixed usa Spawn Point Chance para mesclar pontos e posições aleatórias. Marcadores não têm prioridade obrigatória.
- Group Radius espalha os integrantes perto da posição inicial do grupo, ainda dentro da região.
- Safe Distance protege entrada e extração. Enemy Spacing evita sobreposição entre encontros gerados. As tentativas são limitadas por Placement Attempts; sem espaço válido o inimigo/grupo é omitido. O grupo é posicionado inteiro ou omitido.
- Posições devem estar na malha, sem recursos/estruturas bloqueando a área e com caminho livre até a entrada. Não se ativa nenhum collider desativado da vila.

O ExpeditionController dispara uma geração após os recursos e a navegação estarem prontos. O componente não spawna em Start e não gera novamente a cada frame. Spawn pode ser chamado explicitamente para módulos instanciados posteriormente, passando o controller inicializado e as reservas de posições. Não há respawn periódico nem ativação por proximidade nesta etapa.

A cena Expedicoes contém exemplos Regiao_Floresta e Regiao_Grupo_Exemplo. As quantidades e listas são editáveis na cena; listas Individuals vazias não geram inimigos individuais. São exemplos de configuração; não são novos assets de bandidos. Uma visita reinicia os encontros. Com Vary Seed Per Visit ligado, cada entrada sorteia uma seed, inclusive na primeira viagem de uma nova run; desligado, a seed fixa permite reproduzir a geração.

Validar no Play Mode: viajar no dia 1 com toggle; individuais sem marcadores; escolta com tipos obrigatórios; pontos bloqueados com e sem fallback; mover/girar a região; listas vazias ou definições inválidas; retorno e nova visita no dia seguinte. A regra de derrota e retorno forçado está descrita em ExpeditionDefeats.md.

Uniform Bravery nas composições força Bravery (0 a 1,2) para todos os integrantes, inclusive Tanks, tanto nas noites quanto nas expedições. Desligado preserva o sorteio noturno ou a bravura do asset na expedição. Não muda inimigos individuais e não altera a composição fixa da primeira noite.
