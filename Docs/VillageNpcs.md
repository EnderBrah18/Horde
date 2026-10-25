# NPCs da vila

## Primeira entrega

A base permite recrutar, ocupar moradia, atribuir trabalho e acompanhar a rotina entre instalação e residência.
O componente VillagePopulation é adicionado automaticamente ao objeto Game ao iniciar a partida.
Para personalizar o catálogo antes de jogar, adicione VillagePopulation manualmente ao mesmo objeto e preencha Recruitment Catalog.
Com catálogo vazio, são carregados os NpcData em Resources/NPCs.

Exemplos editáveis:

- Morador: população comum, custa 1 cristal de mana; pode ser trabalhador de uma instalação de produção.
- Artur, o Ferreiro: nome provisório, personagem essencial único, custa 3 cristais; trabalha na instalação Ferreiro.
- Trabalhador: até 8 por instalação nos níveis de exemplo. O limite principal fica em cada nível da instalação;
  Slots Per Facility da função continua como limite adicional (100 no exemplo de trabalhador).
- Ferreiro: 1 especialista na ferraria.

Os custos e nomes são provisórios e ficam nos assets. Os personagens usam cápsulas coloridas quando não há prefab.

## Uso no jogo

1. No modo construção, restaure uma Casa ou Residência Essencial e a instalação de trabalho desejada.
2. Mude para exploração durante o dia. Aproxime-se da casa e pressione E.
3. Use Recrutar. A categoria deve corresponder à casa, deve haver vaga e os recursos são pagos somente ao recrutar.
4. Aproxime-se do NPC e pressione E. Escolha uma função e instalação na lista.
5. Durante o dia o NPC vai ao trabalho. À noite vai até o acesso à residência. Sem trabalho, fica junto à residência.

Os NPCs ficam no ponto de acesso externo à estrutura; interiores e animações de entrada ainda não existem.
A navegação usa GridWorld com caminhos livres, sem quebrar estruturas ou ativar colliders de construções.
Quando não há caminho, o painel informa isso e a rotina tenta novamente. A atribuição é mantida quando o prédio quebra.

## Moradia e vida

Recrutar ocupa uma vaga; restaurar uma casa não cria NPCs. Personagens essenciais usam exclusivamente as residências essenciais.
Casas quebradas suspendem suas vagas. Os moradores continuam na vila, ficam desabrigados e recebem automaticamente
uma vaga compatível livre em outra residência, se houver. Sem alternativa, conservam o vínculo com a casa quebrada até seu reparo.
Novos recrutamentos nunca usam vagas suspensas ou ocupadas.

Moradores comuns que chegam a zero de vida morrem e liberam a vaga. Essenciais ficam feridos e indisponíveis,
sem serem removidos. Na exploração diurna, E no NPC ferido oferece tratamento: exige residência ativa e paga Recovery Costs.
Os exemplos cobram 1 cristal de mana. Guardas, ataques contra NPCs e combate autônomo ainda não foram implementados.

## Dados e scripts

- NpcData: identidade, categoria, retrato, prefab, vida, velocidade, custos e funções permitidas. Essenciais são únicos por asset.
- NpcRoleData: função, tipo de instalação, referência opcional a instalação específica, vagas de trabalho e disponibilidade de serviço.
- VillageNpc: vida, residência, função, trabalho, estado e movimento. Também pode ser colocado previamente na cena com seus dados.
- VillagePopulation: registro, recrutamento, vagas, reassentamento, atribuição e recuperação.
- Game.NpcInteraction: integração ao painel atual; recrutamento e atribuição pertencem à exploração diurna.

Para criar recursos: Create > Village > NPC ou NPC Role. Um prefab deve ter sua raiz no centro do personagem,
na escala apropriada; VillageNpc e Health são adicionados à raiz quando ausentes. O collider da raiz é configurado como trigger.
Para NPCs já na cena, adicione VillageNpc, atribua NpcData e configure Residence/Workplace/Role no Inspector.
Mantenha apenas uma instância de cada personagem essencial na cena.

HasServiceSpecialist consulta se uma instalação operacional tem alguém saudável com função de serviço atribuída.
É uma ligação para os serviços futuros; lojas, equipamentos, conversas e missões ainda não existem.
Instalações de produção exigem trabalhadores: por nível, Minimum Workers define o mínimo, Maximum Workers o limite,
Extra Workers Per Bonus o tamanho do grupo adicional e Production Bonus Per Group o bônus por grupo completo.
O padrão é 2/8/2/1. Com produção base de 10: 0–1 trabalhador entrega 0, 2–3 entrega 10, 4–5 entrega 11,
6–7 entrega 12 e 8 entrega 13. O bônus soma uma vez a cada recurso distinto produzido, sem criar saída para entradas zeradas.
O cálculo usa trabalhadores atribuídos e aptos no amanhecer, mesmo que estejam abrigados; não exige presença física no trabalho.
Funções precisam ter Production Worker marcado. Feridos, mortos e NPCs desativados não contribuem.
Um ferido mantém sua vaga de trabalho reservada até a atribuição ser removida.
O painel mostra trabalhadores aptos, vagas ocupadas e previsão da entrega. Prédios quebrados não produzem.

Não há postos de guarda, expedições, salvamento,
animações, separação física entre NPCs ou interface em Canvas nesta etapa.

## Verificação

Tests/NPCs/RunPopulationTests.ps1 executa 31 verificações das transações reais de VillagePopulation e NpcRoleData
com substitutos de Unity, NPCs e instalações. Cobre categorias, vagas, duplicidade, custos, atribuição, suspensão,
recuperação e reassentamento. Não verifica instanciação, renderização ou movimento reais.

No Play Mode, confira: recrutamento nos dois tipos de casa; desconto e limite de vagas; E no NPC; atribuição ao
trabalho; deslocamento por obstáculos; mudança de rotina ao iniciar a noite; casa e local de trabalho quebrados;
morte comum e ferimento/recuperação do essencial. Verifique também a apresentação do painel e dos prefabs.
