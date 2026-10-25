# Ruínas, casas e obstáculos

Casas e instalações são objetos essenciais. Começam abandonadas (nível 0), sem bloquear movimento e imunes a dano.
Quando desbloqueadas, recuperam a forma original, passam a bloquear e podem ser atacadas.

Ao zerar a vida, preservam o objeto, o asset e o nível, mas ficam quebradas. Param de produzir e liberar capacidade de NPCs,
ficam atravessáveis e deixam de ser alvos. O reparo restaura o mesmo nível e toda a vida; não gera uma segunda produção naquela rodada.

## Inspector

- VillageFacility: Start Unlocked, Starting Level, Ruin Height e Ruin Color.
- VillageFacilityData: por nível, Costs, Repair Costs, Production Per Night e Npc Capacity.
- Menu de contexto do componente, durante o dia em Play Mode: Desbloquear, Melhorar e Reparar.

A representação de ruína atual é provisória: o mesmo modelo fica achatado e escurecido. Modelos finais de ruína podem substituir esse visual posteriormente.

As moradias se dividem em dois tipos:

- Casa.asset usa Housing e oferece 2, 4 e 6 vagas de população comum por nível.
- ResidenciaEssencial.asset usa EssentialHousing e oferece 1, 2 e 3 vagas para NPCs nomeados por nível.

Na CenaPrincipal, Residencia_Essenciais_01 usa o segundo tipo; as outras dez casas permanecem comuns.
Você pode trocar o asset Data em qualquer moradia para mudar seu tipo.
VillageProduction.PopulationCapacity soma as vagas comuns e EssentialNpcCapacity soma as vagas de NPCs nomeados.
HousingCapacity continua disponível como equivalente à capacidade comum, sem misturar as duas categorias.
Isso prepara os limites separados para NPCs essenciais e população/tropas; recrutamento, entrada de moradores, ocupação de vagas e comportamento dessas unidades ainda não foram implementados.
Quebrar uma casa reduz a capacidade disponível; as regras para habitantes excedentes serão definidas com o recrutamento.

## Obstáculos e ataques

- Structure.CanBeAttacked centraliza a elegibilidade de alvos.
- Structure.BlocksMovement centraliza bloqueio de personagens e rotas.
- Tag Muro: bloqueio permanente do grid, invulnerável e ignorado na seleção de alvos, inclusive por Tanks e Berserkers.
- Tag Destrocos: atravessável e não atacável. Reconstrução em defesas especiais é uma etapa futura.
- Cercas e construções comuns continuam destrutíveis. Obstáculos atacáveis encontrados durante a marcha podem ser atacados por qualquer tipo de inimigo.
- Berserkers podem escolher qualquer estrutura atacável. Ruínas e muros permanentes continuam sendo exceções.

Nenhum collider de construção foi ativado. As verificações de movimento também filtram estruturas não bloqueantes quando encontram colliders já existentes.

## Contorno de oclusão

OcclusionOutline está no jogador e é adicionado aos inimigos ao nascerem. Cor e largura são editáveis.
Dois shaders URP usam profundidade e o bit de stencil 8, reservado ao usuário no URP instalado,
para desenhar apenas a borda do personagem que está encoberta por geometria. Não dependem de raycasts nem de colliders nas construções.

O jogador usa ciano e os inimigos usam laranja. O sistema atual acompanha os MeshFilters dos personagens provisórios.
Os materiais ficam em Assets/Resources/Effects e os shaders em Assets/Shaders.

Os testes independentes do Unity cobrem estados, reparo, navegação lógica, capacidade e proteção contra dano.
A renderização dos shaders e a aparência das ruínas precisam ser verificadas no Play Mode.
