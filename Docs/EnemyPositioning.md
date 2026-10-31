# Preferências de posicionamento dos inimigos

No EnemyArchetypeData, Preferência de posicionamento contém:

- Prefer Tank Cover: opt-in para procurar cobertura atrás de um Tank, mesmo de outro grupo. Desligado por padrão para preservar as definições existentes.
- Tank Cover Distance: distância da primeira fileira atrás do Tank, padrão 4 unidades.
- Tank Cover Spacing: separação lateral e entre fileiras, padrão 2,5 unidades.
- O raio para encontrar Tanks é Balance > Tactic Radius.

EnemyPositioning é adicionado/configurado na inicialização pela definição. Não é uma nova Personality ou Runtime Type e não muda a escolha de vítima. Os Tanks não procuram outros Tanks como cobertura.

## Aquisição e movimento

Escolhe o Tank elegível mais próximo com uma rota livre até uma posição atrás dele. A busca ocorre a cada 0,5 segundo. Só aceita inimigos vivos, ativos, em March, da mesma cena e GridWorld. Nenhuma posição da vila é usada por inimigos da expedição.

Um Tank adquirido é mantido até 125% do raio de detecção para evitar trocas constantes na borda. Morte, fuga, saída da região ou distância excessiva liberam a referência; o inimigo procura outro Tank ou retoma sua navegação pessoal. Posições bloqueadas ou sem caminho livre não obrigam o inimigo a destruir algo apenas para recuperar a formação.

A posição atrás acompanha a orientação do Tank. O cálculo usa transform.forward projetado no chão; atrás é a direção oposta. Não depende de onde o seguidor nasceu ou chegou. O Tank normalmente vira na direção do movimento e, durante combate, pode virar para seu alvo. A formação gira junto com essa orientação. Seguidores ocupam fileiras, com posições central/esquerda/direita; velocidades são ajustadas para acompanhar e esperar. Colisões, repulsão, separação e navegação continuam funcionando. Ataques já ao alcance têm precedência sobre reposicionamento.

## Cálculo único para cobertura atrás de Tank

TankCoverFormation centraliza a geometria, a rota de aproximação e o ajuste de velocidade. Grupos táticos antigos, seguidores individuais e grupos guiados por líder externo reservam lugares no mesmo FormationSlots por Tank. Não há mais duas contagens de seguidores ou duas fórmulas de fileira para essa cobertura.

Cada inimigo mantém sua reserva entre frames e mudanças de origem da solicitação. A saída libera a vaga sem renumerar os demais. Morte, fuga, troca de Tank, repulsão e fim do reposicionamento liberam reservas; também há limpeza de referências inválidas. Um ponto bloqueado falha e libera a reserva, permitindo seguir o caminho normal.

Os lugares são central, esquerda e direita, passando à fileira seguinte a cada três seguidores. A formação usa a maior distância traseira e o maior espaçamento solicitados por seus participantes; mantém essas medidas até esvaziar para não oscilar com saídas e chegadas. Isso evita que parâmetros individuais e parâmetros do grupo produzam posições incompatíveis. Valores individuais continuam sendo as solicitações de cada participante.

A dispersão agressiva ao redor do líder continua independente: ela não é uma formação de cobertura atrás de Tank.

## Membro, líder e inimigo solto

- Membro que escolhe Tank externo sai voluntariamente do grupo e acompanha sem se tornar membro do grupo do Tank. Não precisa ter bravura alta e não foge por essa decisão. O grupo de origem ajusta os contadores para não interpretar a saída como morte.
- Membro que escolhe Tank do próprio grupo mantém a associação.
- Líder mantém seu grupo; seus integrantes usam a cobertura atrás do Tank externo durante deslocamentos, mesmo que não tenham o toggle individual. Ataques imediatos e Tanks integrantes mantêm seus comportamentos próprios.
- Enquanto o líder acompanha Tank externo, suspensão da separação automática e proteção contra fusões mantêm a identidade desse grupo. Perder o Tank retoma as regras normais de separação/fusão.
- Inimigo solto pode acompanhar sem precisar entrar num grupo. Essa preferência não o torna recrutável por proximidade.

A regra não modifica o grupo, o alvo ou a personalidade do Tank escolhido. Moral, fuga, perda do líder e comportamento de combate continuam sistemas separados.

## Base para futuros papéis

O componente separa escolha de alvo (Personality), tipo físico (Runtime Type) e posição desejada (EnemyPositioning). Futuramente pode receber posições de retaguarda para suporte e regras de escolta/proteção. Suporte, cura e proteção ativa de aliados ainda não foram implementados.

## Proteção de suporte — planejamento futuro, não implementado

Regras solicitadas para uma etapa posterior:

- A proteção funciona entre grupos e também para inimigos sem grupo.
- Um grupo sem suporte pode tentar proteger outro grupo que tenha suporte, sem exigir fusão dos grupos.
- Um inimigo solto pode tentar proteger um suporte próximo.
- O suporte não abandona seu grupo para procurar cobertura atrás de um Tank externo.
- Nenhum integrante abandona um grupo com suporte apenas para se esconder atrás de um Tank. A obrigação de preservar a escolta tem precedência sobre a preferência individual por cobertura.
- Ao encontrar um Tank, o reposicionamento deve preservar o grupo e o suporte: o Tank pode ocupar a frente da formação ou o líder pode conduzir a formação inteira para trás dele. Isso não autoriza retirar o Tank do próprio grupo sem uma política adicional.
- A presença de suporte não impede aproveitar cobertura; impede que a busca por cobertura desmonte sua proteção.

Modelo proposto para reaproveitar checagens: primeiro decidir quem precisa ser protegido e se o vínculo do grupo pode ser alterado; depois escolher uma âncora/posição de formação. A mesma busca local de aliados e validação de cena, navegação, vida e alcance pode atender tanto cobertura atrás de Tank quanto escolta de suporte. A autorização para sair do grupo deve ser uma regra compartilhada, consultada antes de qualquer transferência por posicionamento.

A prioridade planejada é preservar a escolta do suporte antes de satisfazer uma preferência individual por cobertura. O líder pode mover todos sem quebrar esse compromisso. Detalhes de escolha entre vários suportes, troca de escolta, morte do protegido e coordenação com o grupo original do Tank ainda precisam ser definidos. Não foram implementados Runtime Type Support, proteção, cura ou reposicionamento do Tank nesta etapa. A regra atual de saída para cobertura permanece como está até a implementação desse sistema.

## Verificação

O alocador de vagas é testado com grupos antigos, individuais e escoltas compartilhando reservas, manutenção de lugares, reutilização de vagas e 5.000 operações de entrada/saída. As regras puras verificam elegibilidade por vida/estado/cena/navegação/raio, exclusão de si e decisão de sair ou manter grupo. Compilação não substitui o teste de movimento.

No Play Mode: habilitar em um asset comum; aproximar um grupo sem Tank de um Tank externo; observar membro saindo e líder mantendo grupo; verificar posição atrás com Tank parado/em movimento; matar, afastar ou fazer o Tank fugir; bloquear a retaguarda; testar ataque próximo, repulsão, grupos diferentes e expedição com a vila carregada.
