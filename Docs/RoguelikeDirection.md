# Direção definida para as próximas etapas

Decisões do usuário. Apenas reembolso acumulado, menu inicial e recuperação ao amanhecer foram implementados nesta etapa.

- A run não terá salvamento para continuar na mesma noite. A progressão permanente será uma moeda especial,
  utilizada no menu para levar melhorias, itens ou construções para a próxima run. A moeda e a loja ainda precisam de implementação.
- O núcleo e o jogador recuperam vida a cada novo dia. Futuras melhorias poderão reparar o núcleo durante a noite;
  a cura oferecida durante o dia/noite será ligada aos serviços do alquimista quando definidos.
- Serviços, personagens e rotinas de NPCs serão desenvolvidos junto à lore.
- O fim da demo será definido pela narrativa, possivelmente entre 5 e 10 dias. Não fixar automaticamente um dia final agora.
- A futura expedição usará áreas maiores e módulos/prefabs conectáveis: lagos, estruturas e vilas abandonadas, com encontros inimigos.
- Derrota futura na expedição não deve encerrar a run como na vila: haverá perda de carga, retorno à vila e uma consequência
  de vida/debuff para a noite seguinte. Percentuais, vida de retorno e debuff ainda precisam ser definidos e implementados.
- A dificuldade das noites deverá usar composições variáveis, não somente uma quantidade crescente fixa.
  Uma noite mais numerosa pode ser seguida de outra no mínimo do bloco com inimigos mais fortes.
  O primeiro diretor está implementado em NightCompositions.md: abertura fixa, piso por blocos de cinco noites e perfis editáveis.
- Tutorial por objetivos, apresentação e feedback serão construídos junto à lore e ao visual.

O menu inicial atual é uma base funcional; não inclui ainda moeda permanente, seleção de vantagens ou itens entre runs.
