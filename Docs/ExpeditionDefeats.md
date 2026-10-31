# Derrota nas expedições

PlayerVitals mantém o objeto de jogador da cena (Health.destroyOnDeath=false). A morte continua emitindo Health.Died; Game.PlayerDied distingue expedição e vila.

- Na vila, morrer inicia respawn com câmera livre e espera de 10 segundos. Somente a destruição do núcleo marca lost; veja PlayerRespawn.md.
- Na expedição, a morte inicia retorno forçado, sem marcar lost, sem exigir proximidade do ponto de extração e sem esperar E.
- A transição bloqueia movimento/ataque e novos danos via PlayerVitals. O grid da vila é restaurado antes de reviver o jogador, suspendendo a IA da expedição.
- Reutiliza o mesmo jogador, posição e rotação de saída, restaura câmera e descarrega a cena da expedição.
- O dia não avança. A viagem pessoal daquele dia continua consumida.

## Balanceamento

No ExpeditionManager da CenaPrincipal, Derrota na expedição:

- Defeat Cargo Loss Percent: percentual perdido por recurso, inicialmente 50%. A quantidade mantida é arredondada para baixo. Exemplo: 7 madeiras e 3 minérios viram 3 madeiras e 1 minério.
- Defeat Return Health Percent: vida atual no retorno, percentual da vida máxima, inicialmente 50%. Sempre retorna vivo, com pelo menos 1 ponto de vida.

Esses valores são padrões de teste, editáveis. A perda só afeta a carga da expedição, não o saldo já guardado na vila. Penalidade de carga é aplicada uma única vez e a entrega também é única. Uma carga totalmente perdida ou vazia não impede o retorno.

Caso o saldo da vila esteja no limite de inteiro e não comporte a entrega, o retorno forçado ocorre e o restante fica em espera na run. A entrega é tentada novamente periodicamente quando há espaço, sem repetir a penalidade.

O retorno normal entrega toda a carga e preserva a vida atual. Nenhum retorno ativa a cura do amanhecer. A vida reduzida após derrota permanece para a noite; vencer a noite cura normalmente no novo dia. Outros debuffs de atributos ainda não foram definidos nem implementados.

## Validação

Compilação e testes de carga/regras cobrem perda percentual, arredondamento, limites, perda única, entrega única, carga vazia, recuperação com vida positiva e entrega adiada por saldo saturado.

Validação no Play Mode ainda necessária: liberar dia 1, viajar, coletar dois recursos e receber dano fatal longe da extração. Deve retornar automaticamente à posição de saída, com vida configurada e aviso; o jogador deve poder mover/atacar e iniciar a noite; outra viagem no dia deve permanecer bloqueada. Morrer na vila deve iniciar o respawn; somente destruir o núcleo deve encerrar a run. Vencer a noite deve recuperar a vida. Testar também carga vazia, perda de 0% e 100%, retorno normal ferido e múltiplos inimigos atingindo na mesma atualização.
