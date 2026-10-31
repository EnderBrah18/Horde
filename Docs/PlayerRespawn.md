# Morte e retorno do jogador na vila

Somente destruir o núcleo marca lost e encerra a run. Morrer na vila não muda o dia, a leva ou os inimigos da noite.

- PlayerVitals captura posição e rotação em Awake, antes do controle e das viagens. Essa é a origem autoral do jogador na cena, sem criar outro objeto nem mover o spawn no Editor.
- Ao morrer na vila, Game inicia a contagem e força combatCameraMode=false, permitindo câmera livre com movimento, zoom e arrasto.
- O mesmo jogador permanece ativo como objeto, com vida zero; seus renderers e colliders próprios ficam temporariamente desativados. Os estados anteriores são preservados. Colliders das construções não são alterados.
- Movimento, ataques, interação, construção, troca por Tab e início de noite ficam bloqueados durante a contagem. Inimigos e estruturas continuam lutando, sem pausar o tempo.
- O HUD mostra os segundos restantes.
- Após Village Respawn Delay (padrão 10 s, no PlayerVitals do jogador), restaura posição/rotação iniciais, vida máxima, visibilidade e colliders previamente habilitados. Aplica a proteção breve de dano já configurada em Damage Invulnerability.
- A câmera volta a seguir o jogador em modo combate (ou exploração, se a noite já terminou).
- Se o núcleo morrer durante a contagem, a derrota cancela o retorno. Não há ressurreição depois de lost.
- Se amanhecer durante a espera, a cura diária não ressuscita o jogador antecipadamente: o timer continua e o respawn recupera sua vida.

A derrota em expedições continua usando o retorno forçado com penalidades do ExpeditionManager, sem esse timer. Veja ExpeditionDefeats.md. O ponto de retorno da expedição é o ponto da viagem; o ponto de respawn da vila é a origem capturada ao iniciar a cena.

## Validação

Testes puros cobrem 10 segundos, callbacks repetidos, deltas zero/negativo, conclusão única, nova morte, cancelamento e prioridade da destruição do núcleo. Compilação e validação de assets não substituem Play Mode.

Testar em Play Mode: mover o jogador longe da origem e morrer durante a noite; conferir contagem, câmera livre e inimigos continuando; Tab não deve sair da câmera livre; ao terminar deve retornar à origem com vida cheia e controle normal. Repetir morte; destruir o núcleo durante a contagem; deixar a noite terminar durante a contagem; confirmar que derrota em expedição continua retornando com vida reduzida e perda de carga.
