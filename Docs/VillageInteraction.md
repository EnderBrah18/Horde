# Interação com a vila

## Modo construção

A partida começa com a mão vazia. Use o botão **Mão vazia**, **0**, **Esc** ou o botão direito para sair da ferramenta de construção.
Clique numa casa, instalação, construção ou ponto especial para selecioná-lo. O retângulo ciano indica o alvo.
A seleção usa os limites visuais dos objetos, inclusive ruínas sem collider ativo.

Escolha uma construção no menu ou com as teclas 1 a 9 para equipá-la. Clicar novamente no mesmo cartão deixa a mão vazia.
Trocar de categoria também deixa a mão vazia. Q/E giram a prévia somente enquanto uma construção está equipada.
O botão direito não vende mais instantaneamente: a venda fica no painel do objeto selecionado.
Construções colocadas durante o dia devolvem 100% do investimento acumulado até o início da próxima noite.
O investimento inclui a construção original e todas as melhorias pagas, com os custos registrados no momento da compra.
Após essa noite começar, o reembolso passa a 70%. Melhorias preservam a data de colocação original e não renovam o reembolso integral.
O botão de venda informa a porcentagem vigente. Estruturas pré-existentes na cena usam 70%.

Restauração/desbloqueio, reparo e melhoria de casas e instalações são ações exclusivas deste modo,
assim como melhorias e venda de defesas e construção em pontos especiais. Os custos são pagos com os recursos da vila.

## Exploração

Aproxime o jogador de uma instalação, casa ou construção e pressione **E** quando aparecer a indicação.
O painel mostra estado, vida, capacidade e produção. O botão **Gerenciar construção** muda para o modo construção,
deixa a mão vazia, mantém o objeto selecionado e centraliza a câmera nele. A ação de gestão é escolhida nesse modo.
Essa passagem está disponível apenas durante o dia.

A exploração será o acesso aos serviços e aos NPCs: comércio, melhorias de equipamentos, conversas, missões e recrutamento.
O recrutamento nas casas e a atribuição de trabalho aos NPCs já estão disponíveis. Comércio, equipamentos, conversas
e missões ainda serão implementados. Consulte VillageNpcs.md para configurar os personagens e testar a rotina.
Melhorar o prédio da ferraria pertence à construção; melhorar uma espada pelo ferreiro pertencerá ao serviço na exploração.
Pressione E, Esc, botão direito ou X para fechar. O painel também fecha ao sair do alcance ou trocar de modo.
O alcance fica em **Game > Interação com a vila > Interaction Range**, inicialmente 5 metros da borda do objeto.

## Combate

Durante a noite, a interação permite consultar o estado das estruturas. Não há desbloqueio, reparo, melhoria,
venda ou passagem para gestão pelo painel. Reparos emergenciais e regras de acesso aos serviços durante ataques ficam para uma etapa futura.
O painel não pausa o combate. Enquanto estiver aberto, cliques não iniciam ataques; ainda é possível mover o jogador.

## Pontos especiais

Anéis dourados marcam os pontos quando a mão está vazia, ou quando o jogador se aproxima.
O painel lista construções cujo Special Build Point Id corresponde ao Required Building Id do ponto.
Construir exige espaço livre, recursos e inclusão na área de construção liberada. O painel informa quando a área precisa ser ampliada.

TorreEspecial.asset está configurada como exemplo para os quatro pontos DefesaEspecial existentes.
Esses pontos ficam além do raio inicial da vila; expanda a área antes de usá-los.
Um ponto ocupado permite inspecionar sua construção e volta a ficar disponível após sua destruição ou venda.

## Melhorias de defesas

BuildingData possui **Upgrade**, que referencia o próximo asset. Os Costs do próximo asset são o preço da melhoria.
**Show In Build Menu** pode ser desmarcado para versões acessíveis apenas por melhoria.
A torre básica já referencia TorreReforcada.asset como exemplo editável.
A melhoria valida espaço e recursos antes de substituir a construção e mantém posição, rotação e ligação ao ponto especial.

## Organização e verificação

Assets/Scripts/Interaction/Game.Interaction.cs contém a seleção e o painel, como parte da classe parcial Game.
O painel usa IMGUI para funcionar com a interface atual; ainda não é a futura HUD em Canvas.
Nenhum collider de construção foi ativado para implementar a seleção.

A compilação e a integridade das referências podem ser verificadas fora do Editor; cliques, disposição do painel,
proximidade e troca entre modos precisam de validação no Play Mode.
