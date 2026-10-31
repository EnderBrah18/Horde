# Menu principal

A cena `Assets/Scenes/MenuPrincipal.unity` usa Canvas Screen Space Overlay, CanvasScaler (1280×720), TextMeshPro e EventSystem com InputSystemUIInputModule.

- Iniciar partida: carrega CenaPrincipal e bloqueia cliques repetidos durante o carregamento.
- Opções: volume geral com prévia, resolução, janela/tela cheia sem bordas, qualidade gráfica e VSync. Aplicar salva em PlayerPrefs; Voltar/Esc descarta a prévia de áudio. Padrão apenas preenche os controles até Aplicar.
- Como jogar: controles atuais e objetivo básico.
- Créditos: texto provisório editável na hierarquia.
- Sair: confirmação; encerra o aplicativo na build e para o Play Mode no editor.
- Esc retorna ao início ou abre a confirmação de saída na tela inicial. Navegação padrão do EventSystem por teclado e mouse.

As preferências são aplicadas antes de qualquer cena, inclusive quando o playtest começa diretamente na vila. Elas não salvam a run.

Textos, cores, posições e referências ficam nos objetos filhos de MenuPrincipal/MenuCanvas. MainMenu liga os eventos dos botões em Awake usando seus nomes; mantenha os nomes dos botões ao personalizar legendas. Não adicione os mesmos callbacks manualmente.

Tools > Pandora > Reconstruir menu principal (Canvas) recria a cena base: use apenas quando quiser substituir o layout inteiro. A montagem automática ocorre apenas enquanto a cena ainda não contém MenuCanvas; depois disso, edições manuais são preservadas.

Prestígio, compras permanentes, remapeamento de teclas, trilha e canais separados de música/efeitos ficam para seus respectivos sistemas. O controle de áudio atual é o volume geral do AudioListener.
