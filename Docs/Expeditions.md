# Expedições — preparação

Planejamento geral. Dias, levas, descoberta por mensagem, posto de acesso e viagem/retorno em uma região fixa
de teste já foram implementados; veja DaysAndExpeditions.md para configuração e validação.
Coleta, carga, entrega e distribuição de recursos por semente já têm uma primeira versão; veja ExpeditionCollection.md.
Encontros, estruturas exploráveis, expansão por regiões e expedições de NPCs continuam planejados.

## Objetivo da primeira versão

Fechar o ciclo: sair da vila durante a exploração, coletar recursos em outra cena, retornar e usar
a carga para recrutamento, restauração, construção e produção. Primeiro o jogador viaja pessoalmente;
o envio de NPCs e suas rotinas de expedição ficam para uma etapa posterior.

Expedições oferecem uma fonte renovável de recursos sem esgotar o mapa principal. Cada viagem pode
ter recursos finitos; novas viagens renovam a oferta. O mapa inicial pode ser limitado em tamanho.
A expansão por regiões geradas conforme o jogador avança deve entrar depois de validar coleta e retorno.

## Situação anterior à primeira implementação

- ResourceData já descreve materiais, alimentos, recursos mágicos e manufaturados.
- Wallet pertence ao Game da cena e mantém saldos em memória; não é um sistema de salvamento.
- PlayerController, PlayerAttack e PlayerVitals dependem do Game e/ou GridWorld atuais.
- Ainda não há mochila, nós de coleta, extração ou transição de expedição.
- O Build Settings atual lista apenas SampleScene; registrar CenaPrincipal e Expedicoes faz parte da implementação.

## Primeira viagem jogável

1. Interagir com um ponto de saída na exploração diurna.
2. Entrar numa região de expedição com ponto de chegada e retorno garantidos.
3. Coletar madeira, pedra, minério e alimento em nós configuráveis.
4. Guardar a coleta numa carga temporária separada da carteira da vila.
5. Retornar pelo ponto de extração.
6. Transferir a carga uma única vez para a vila, preservando o restante da partida.

Começar com uma região pequena de teste. Em seguida, trocar o mapa fixo por geração com semente,
mantendo as mesmas interfaces de coleta, carga e extração. Não depender de NPCs, serviços ou missões para testar esse ciclo.

## Organização proposta

| Parte | Responsabilidade |
|---|---|
| ExpeditionData — ScriptableObject | Configuração da viagem: região, geração, recursos e limites |
| ResourceNodeData — ScriptableObject | Tipo de coleta, recurso, rendimento e interação necessária |
| ResourceNode | Quantidade restante e interação de coleta no mundo |
| ExpeditionCargo | Carga temporária; capacidade e transferência validada |
| ExpeditionSession | Estado da viagem: entrando, explorando, retornando, concluída ou falha |
| ExpeditionExit | Entrada da viagem e extração, conforme o contexto |
| ExpeditionGenerator | Geração determinística, áreas de recursos, estruturas e acessibilidade |
| Session/scene coordinator | Preservar a vila, transferir controle e restaurar contexto no retorno |

ResourceNodeData referencia ResourceData, sem recriar tipos de recursos ou gravar quantidades nos assets.
As quantidades de coleta, capacidade de carga e parâmetros de geração devem aparecer no Inspector.

## Preservação da partida

O retorno precisa manter saldos, noite atual, construções colocadas e seus reembolsos, níveis e vida das instalações,
moradores, atribuições de trabalho e vida do jogador. Recarregar CenaPrincipal do zero não atende a esse requisito.

Antes de escolher a estratégia, separar dependências de controle e navegação do jogador que hoje assumem uma única vila.
Uma alternativa é manter a cena da vila carregada e suspensa enquanto uma cena aditiva de expedição fica ativa.
Isso exige tratar os singletons Game.I/GridWorld.I, listas estáticas, câmeras, iluminação, input e atualização dos NPCs.
Não criar um segundo Game completo na expedição sem resolver essas dependências.

Suspensão em memória não equivale a salvamento em disco. Persistência entre sessões pode vir depois,
mas sair e retornar dentro da mesma partida deve funcionar antes da geração procedural.

## Regras propostas e decisões pendentes

- Saída apenas durante a exploração diurna para a primeira versão.
- Proposta inicial: suspender a simulação da vila durante a viagem, evitando uma noite automática sem o jogador.
  A passagem do tempo e eventual custo de viagem ainda precisam ser definidos.
- Capacidade da carga: definir se o limite será por unidades, peso ou espaços. Não adicionar peso a ResourceData antes dessa decisão.
- Morte ou abandono: definir perda total, parcial ou possibilidade de recuperar a carga. Não usar automaticamente a derrota da vila.
- Recursos entram na economia da vila somente na extração bem-sucedida; retorno repetido não pode duplicar entrega.
- A escassez e as fontes dos cristais de alto valor precisam de balanceamento próprio, sem torná-los coleta abundante por padrão.
- Expedições de NPCs devem reutilizar configurações de recursos e recompensas, com simulação própria de risco e duração posteriormente.

## Ordem de implementação

1. Contexto de viagem e preservação da vila; ida e volta sem coleta.
2. Nós de recurso e carga temporária numa região fixa.
3. Extração com transferência única, limite de carga e tratamento de falha conforme as regras escolhidas.
4. Geração com semente e caminhos acessíveis entre chegada, coleta e extração.
5. Encontros, estruturas exploráveis e risco; depois, expansão por regiões e expedições de NPCs.

## Critérios de validação

- Viajar e voltar não reinicia nem duplica a vila ou seus NPCs.
- A construção recente mantém o reembolso correto depois do retorno.
- Vida, empregos, vagas e níveis das instalações são preservados.
- Coleta respeita o estoque do nó e a capacidade da carga.
- Extração entrega a carga exatamente uma vez e limpa a sessão.
- Uma entrada indisponível não cobra recursos nem deixa o jogador preso entre cenas.
- Reinício e derrota descartam corretamente o contexto da expedição.
- A mesma semente gera a mesma distribuição e sempre permite alcançar a extração.
