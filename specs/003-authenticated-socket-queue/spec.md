# Feature Specification: Socket autenticado e fila

**Feature Branch**: `003-authenticated-socket-queue` (nenhum branch criado; spec no `main`)

**Created**: 2026-09-14

**Status**: Draft

**Input**: User description: "Socket autenticado e fila: a conexão WebSocket que se mantém sozinha (token, reconexão, heartbeat, segundo plano, troca de rede) e o matchmaking sobre ela. Terceira de cinco features da camada de rede. Estado e mensagens de partida ficam para a feature 4." (descrição completa na mensagem que abriu esta feature)

## Contexto

Terceira de cinco features da camada de rede do cliente. No Android, perder o
socket é rotina: minimizar, bloquear a tela, trocar de Wi-Fi para dados móveis.
O token de acesso dura 5 minutos e vence com o app minimizado.

Hoje a conexão vive no `BaseClient`, que depende da NativeWebSocket (ela achata
os close codes 4001/44xx), de singletons de cena (`WebSocketDispatcher`,
`PlayerSession.Instance`, `TokenRefreshService.Instance`) e da ponte síncrona de
token deixada pela feature 002 — e nenhum teste o alcança. O
`MatchmakingClient` nunca manda `join_queue`, então a fila não funciona com o
backend atual.

Esta feature constrói a conexão autenticada sobre as portas da 001
(`IWebSocket`, `SocketClosure`, `MainThreadQueue`, `IProtocolCodec`,
`DiscriminatedUnion`, `IMonotonicClock`, `IAppLifecycle`,
`INetworkReachability`, `IClientLog` e seus fakes) e da 002
(`IAccessTokenSource`, `AccountSession.SessionExpired`, `DeckId`, `DeckProblem`,
`DeckProblemUnion`, `PlayerDecks`), e faz o código antigo (`BaseClient`,
`MatchmakingClient`, `MatchClient`, `Heartbeat`, `ReconnectPolicy`,
`ReconnectOverlay`, `PlayButton`) evoluir no lugar, sem camada paralela.

Os "usuários" desta feature são o jogador — que aperta Jogar e é pareado mesmo
trocando de rede ou minimizando o app — e as features 4 e 5, que recebem uma
conexão de partida que se mantém sozinha.

**Contratos** — são do backend e não são copiados aqui. Fonte da verdade:

- `C:/Users/gabri/Projetos/dev_container/anathema/backend/specs/011-deck-catalog-api/contracts/matchmaking_messages.md` — `join_queue`, `match_found`, recusas de fila, `matchmaking_failed`
- `C:/Users/gabri/Projetos/dev_container/anathema/backend/specs/013-socket-heartbeat/contracts/heartbeat_messages.md` — `ping`/`pong`, gates e close codes
- `C:/Users/gabri/Projetos/dev_container/anathema/backend/specs/009-match-protocol/contracts/server_frames.md` — `match_start` ao conectar e ao reconectar
- `C:/Users/gabri/Projetos/dev_container/anathema/backend/specs/009-match-protocol/contracts/refusal_codes.md` — `malformed_message`, `unknown_message_type`

Quando esta spec e um contrato discordam, vale o contrato.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Conexão que se recupera sozinha e sabe quando desistir (Priority: P1)

Uma parte do cliente (fila ou partida) pede uma conexão a um socket do servidor
e passa a recebê-la aberta e autenticada, sem cuidar de token nem de queda. Se o
servidor recusar o token, a conexão renova e reabre. Se a rede cair, ela espera
um pouco mais a cada vez e volta. Se o servidor disser que a partida não existe
ou que o jogador não participa dela, ou se a sessão de conta expirou, ela
desiste e diz o motivo. Quem consome vê em que ponto a conexão está e recebe os
frames em ordem, na thread principal.

**Why this priority**: fila e partida dependem disso. Sem um núcleo testável
que distingue recusa de token, recusa de partida e queda de rede, cada socket
reimplementa a reconexão e erra em silêncio.

**Independent Test**: testes EditMode com `FakeWebSocket`, `FakeMonotonicClock`,
um fake de `IAccessTokenSource` e jitter determinístico roteirizam abertura,
frames de gate, close codes, renovações e esperas, e verificam URLs abertas,
estados, eventos e motivo de desistência — sem cena e sem servidor.

**Acceptance Scenarios**:

1. **Given** uma fonte de token que entrega "A" e depois, ao renovar, "B", **When** a conexão abre com `?token=A`, recebe `auth_denied` e fecha com 4001, **Then** o estado passa por renovando token, a renovação é pedida uma vez, a próxima abertura usa `?token=B` e, ao chegar o primeiro frame que não é negação de gate, o estado é conectado.
2. **Given** um limite de 2 recusas de token seguidas, **When** três aberturas seguidas recebem `auth_denied` + 4001, **Then** a conexão desiste com o motivo "token recusado repetidamente" e não abre de novo.
3. **Given** uma renovação que falha por transporte, **When** ela termina, **Then** a conexão espera o próximo passo do backoff, a recusa não é contada no limite de recusas de token, e a próxima abertura pede um token válido de novo.
4. **Given** uma renovação que responde "sessão expirada", **When** ela termina, **Then** a conexão desiste com o motivo "sessão expirada" e não abre de novo.
5. **Given** o socket de partida, **When** chega `match_denied` e o fechamento 4404, **Then** a conexão desiste com o motivo "partida não existe", sem renovar token e sem nova tentativa; o mesmo vale para 4400 ("sem matchId") e 4403 ("não participa da partida").
6. **Given** uma conexão provada e jitter determinístico, **When** o socket cai três vezes seguidas sem código (queda de rede), **Then** as esperas anunciadas crescem a cada tentativa até o teto, cada uma com o jitter esperado, o evento "esperando nova tentativa" traz o número da tentativa e a espera, e ao reabrir e ser provada a conexão emite "voltou depois de cair" uma vez.
7. **Given** uma conexão esperando nova tentativa ou renovando token, **When** quem consome sai de propósito, **Then** o socket é fechado, nenhuma abertura acontece depois — nem quando a espera ou a renovação terminam — e o estado é desconectado.
8. **Given** uma conexão aberta, **When** chegam três frames válidos e um texto que não decodifica, **Then** os três são entregues em ordem na thread principal, o inválido é registrado no log e descartado, e a conexão continua aberta.

---

### User Story 2 - Entrar na fila com um deck e ser pareado, mesmo caindo (Priority: P1)

O jogador aperta Jogar. O cliente entra na fila com um deck; enquanto procura,
se o socket cair, a conexão volta e o cliente entra na fila de novo com o mesmo
deck, sem o jogador fazer nada. Quando o servidor pareia, o cliente recebe quem é
o oponente e o identificador da partida, fecha o socket de fila e segue para a
partida. Se o servidor recusar o deck, o cliente diz exatamente por quê.

**Why this priority**: hoje a fila não funciona com o backend. É a jornada que
leva o jogador da Home à partida.

**Independent Test**: testes EditMode com os mesmos fakes roteirizam `join_queue`
enviado, cada recusa do contrato 011, `match_found`, `matchmaking_failed` e
quedas durante a busca; LiveServer com duas contas novas.

**Acceptance Scenarios**:

1. **Given** a fila fora de busca, **When** o jogador entra com o `DeckId` 4, **Then** a conexão abre, o primeiro envio de fila é `join_queue` com `deck_id` 4 e, enviado sem recusa, o estado da fila é procurando.
2. **Given** a fila procurando, **When** chega `match_found`, **Then** a fila entrega um pareamento tipado com `MatchId`, o próprio jogador e o oponente (cada um com `UserId`, apelido, ícone e nível), o estado é pareado, e o socket de fila é fechado de propósito, sem reconexão.
3. **Given** a fila procurando, **When** chega recusa `deck_not_found` com `deck_id` 4, **Then** a fila entrega a recusa "deck não encontrado" com o `DeckId` 4, o estado é fora da fila e o socket continua aberto; um novo `join_queue` pelo mesmo socket funciona.
4. **Given** a fila procurando, **When** chega recusa `invalid_deck` com dois problemas, **Then** a recusa "deck inválido" traz os dois `DeckProblem` na ordem do servidor, cada um com seus campos.
5. **Given** a fila procurando, **When** chega recusa `deck_not_specified`, `malformed_message`, `unknown_message_type` ou um `code` que o cliente não conhece, **Then** cada uma vira seu próprio tipo de recusa — a desconhecida preservando o texto do `code` — e o estado é fora da fila.
6. **Given** a fila procurando, **When** chega `matchmaking_failed`, **Then** a fila entrega esse aviso separado das recusas de deck, com o texto do servidor, e o estado é fora da fila com o socket aberto.
7. **Given** a fila procurando com o `DeckId` 4, **When** o socket cai sem código, **Then** a conexão espera o backoff, reabre e o primeiro envio do novo socket é `join_queue` com `deck_id` 4, sem ação de quem consome; o estado volta a procurando.
8. **Given** a fila procurando e um limite de tentativas da fila, **When** todas as tentativas de reconexão falham, **Then** a fila emite "saiu da fila" com o motivo da desistência e o estado é fora da fila.
9. **Given** a fila procurando, **When** o jogador sai, **Then** o socket é fechado de propósito, o estado é fora da fila, e nenhum aviso de falha nem nova tentativa acontece.

---

### User Story 3 - Perceber conexão morta sem confundir pausa com silêncio (Priority: P2)

Uma conexão pode ficar pendurada sem fechar (Wi-Fi que cai, NAT que expira). O
cliente manda `ping` com um marcador de tempo e mede quanto o `pong` demora; se o
servidor ficar mudo tempo demais depois de já ter respondido, a conexão é
derrubada e reconecta. Mas o tempo em que o app ficou parado — minimizado, com a
tela bloqueada, ou travado num carregamento de cena — não é silêncio do servidor,
e frames que chegaram durante a pausa nunca fazem a conexão ser declarada morta.

**Why this priority**: sem heartbeat, a busca na fila fica pendurada para sempre;
com um heartbeat que confunde pausa com silêncio, toda volta do segundo plano
derruba uma conexão boa. Depende da história 1.

**Independent Test**: EditMode com `FakeMonotonicClock` avançando entre
avaliações, `FakeWebSocket` entregando `pong` e outros frames, verificando
pings enviados, latência exposta e fechamento provocado.

**Acceptance Scenarios**:

1. **Given** uma conexão aberta, **When** o relógio avança o intervalo de ping, **Then** um `ping` é enviado com o marcador do instante monotônico atual; **When** chega o `pong` com o mesmo marcador 80 ms depois, **Then** a latência exposta é 80 ms.
2. **Given** uma conexão que já recebeu um `pong`, **When** o relógio avança 30 s em avaliações regulares sem nenhum frame, **Then** a conexão é derrubada pelo cliente e segue o caminho de queda (nova tentativa com backoff).
3. **Given** uma conexão que ainda não recebeu nenhum `pong`, **When** passam 30 s sem frame, **Then** a conexão não é derrubada.
4. **Given** uma conexão provada, **When** o relógio salta 60 s entre duas avaliações e há frames recém-chegados à espera de entrega, **Then** a conexão não é derrubada, os frames são entregues e a contagem de silêncio recomeça.
5. **Given** um socket de fila que, depois de aberto, só recebe `pong`, **When** o primeiro `pong` chega, **Then** a conexão conta como provada: recusas de token e tentativas voltam a zero.
6. **Given** uma conexão provada e o app em segundo plano por 7 minutos com o socket ainda aberto, **When** o app volta, **Then** a contagem de silêncio é zerada antes de qualquer avaliação, um `ping` é enviado imediatamente, e a conexão só é derrubada se ficar em silêncio pelo limite a partir da volta.

---

### User Story 4 - Sobreviver ao segundo plano e à troca de rede (Priority: P2)

No celular, o jogador procura partida, minimiza o app ou troca de Wi-Fi para
dados móveis, e ao voltar está de novo na fila sem pedir login e sem apertar
nada. Enquanto o app está em segundo plano ou sem rede, o cliente não gasta
tentativas à toa. No Windows, minimizar não interrompe nada.

**Why this priority**: é o motivo da feature no Android, mas depende das
histórias 1 a 3.

**Independent Test**: EditMode com `FakeAppLifecycle` e `FakeNetworkReachability`
roteirizando ida e volta do segundo plano, Wi-Fi → dados, sem rede → rede, e
verificando aberturas, esperas e contagem de tentativas; quickstart manual no
Android e no Windows.

**Acceptance Scenarios**:

1. **Given** uma conexão esperando nova tentativa, **When** o app vai para segundo plano e o relógio avança além da espera, **Then** nenhuma abertura acontece, o estado é suspenso (segundo plano) e nenhuma tentativa é consumida.
2. **Given** a mesma conexão suspensa, **When** o app volta, **Then** a conexão pede um token válido e abre imediatamente, com backoff zerado.
3. **Given** uma conexão aberta, **When** o app volta do segundo plano e o socket continua aberto, **Then** um `ping` é enviado na hora e nenhuma reabertura acontece.
4. **Given** uma conexão aberta em Wi-Fi, **When** a rede muda para dados móveis, **Then** o socket antigo é fechado e um novo é aberto imediatamente, sem espera, sem consumir tentativa e sem desistir.
5. **Given** uma conexão que caiu e a rede em "sem rede", **When** o relógio avança muito além de todas as esperas, **Then** nenhuma abertura acontece, o estado é suspenso (sem rede) e nenhuma tentativa é consumida; **When** a rede volta, **Then** a conexão abre imediatamente.
6. **Given** a fila procurando no Android, **When** o jogador minimiza por mais de 5 minutos e volta, **Then** a fila está procurando de novo com o mesmo deck, sem pedir login.
7. **Given** a fila procurando no Windows, **When** o jogador minimiza a janela, **Then** o socket não cai e a fila continua procurando.

---

### User Story 5 - Jogar do botão até a partida, pelo código de hoje (Priority: P2)

O jogador entra, chega à Home, aperta Jogar e, pareado com outro jogador, vê a
`VersusScene` e em seguida recebe o início da partida — o mesmo fluxo de hoje,
agora sobre a conexão nova. O aviso de reconexão continua aparecendo quando a
conexão cai e sumindo quando volta. A biblioteca de socket antiga e os singletons
de conexão saem do projeto.

**Why this priority**: prova que o núcleo novo sustenta o jogo real; mas é
religação, não comportamento novo, e depende das histórias 1 a 4.

**Independent Test**: dois jogadores do Multiplayer Play Mode percorrem Login →
Home → Jogar → pareamento → `VersusScene` → `match_start`; testes LiveServer
contra o backend local.

**Acceptance Scenarios**:

1. **Given** dois jogadores virtuais logados com o deck inicial, **When** os dois apertam Jogar, **Then** os dois chegam à `VersusScene` e recebem `match_start` da mesma partida.
2. **Given** o jogador sem nenhum deck na lista, **When** aperta Jogar, **Then** nada é enviado à fila e o fato é registrado no log.
3. **Given** a partida em andamento, **When** o socket de partida cai e volta, **Then** o `match_start` recebido de novo é tratado como hoje, sem recarregar a cena da partida.
4. **Given** qualquer socket de fila ou partida, **When** a conexão espera nova tentativa, **Then** o aviso de reconexão aparece com o número da tentativa; **When** ela volta, **Then** o aviso some; **When** ela desiste, **Then** o aviso mostra o motivo.
5. **Given** o projeto depois desta feature, **When** se procura a NativeWebSocket, o `WebSocketDispatcher`, o `ConnectionClient`, o `TokenRefreshService` ou a leitura síncrona `PlayerSession.Token`, **Then** nenhum deles existe.

---

### Edge Cases

**Token e gates**
- `auth_denied` sem o 4001 depois (servidor caiu no meio), ou 4001 sem `auth_denied` antes: cada um sozinho já conta como recusa de token.
- `auth_denied` e 4001 do mesmo socket: uma única recusa de token, uma única renovação.
- `match_denied` sem close code (queda logo depois do frame): desiste do mesmo jeito, com motivo "partida recusada" sem detalhe; close code 44xx sem `match_denied`: desiste com o motivo do código.
- `match_denied` ou 44xx no socket de fila (não previsto pelo contrato): tratado igual, como terminal; registrado no log.
- Pedido de token sem sessão autenticada (jogador saiu com a busca em curso): desiste com "sem sessão", sem abrir.
- Renovação que termina depois de uma saída de propósito: descartada, nenhuma abertura.
- Token nunca aparece em log, nem dentro da URL registrada.

**Quedas e reconexão**
- Fechamento 1000 ou 1001 vindo do servidor: tratado como queda comum, com backoff (o servidor não fecha socket ocioso nem o de partida no fim; um fechamento assim só vem de reinício ou deploy).
- Falha ao abrir (host inacessível, handshake recusado): mesma coisa que queda sem código.
- Socket que abre e cai antes de ser provado: a tentativa não é zerada; só a prova zera tentativas e backoff.
- Fechamento e erro do mesmo socket chegando juntos: uma única nova tentativa agendada.
- Saída de propósito durante a abertura: o socket em abertura é fechado; nenhum evento de reconexão depois.
- Envio sem conexão aberta (ex.: `ping` na fronteira de uma queda): resultado observável, registrado, sem exceção e sem derrubar a conexão.

**Heartbeat**
- `pong` com marcador que o cliente não mandou ou sem marcador: conta como vida e como prova, sem latência medida.
- Vários pings antes de um pong (latência alta): cada `pong` é casado pelo marcador, não pela posição; frames de outro tipo entre ping e pong não atrapalham.
- Carregamento de cena que trava a thread principal por segundos: salto entre avaliações, tratado como pausa.
- Frame de partida chegando a cada poucos segundos: conta como vida; o ping continua sendo mandado no intervalo.

**Segundo plano e rede**
- Ida ao segundo plano com renovação ou abertura já em curso: elas terminam; se o socket abrir, fica aberto; se falhar, a próxima tentativa fica suspensa.
- Troca de rede com a conexão já esperando nova tentativa: a espera é descartada e a abertura acontece na hora.
- Troca de rede enquanto o app está em segundo plano: nenhuma abertura até voltar; na volta, abre na hora.
- Wi-Fi → sem rede → dados móveis: trata como sem rede (suspende) e depois rede de volta (abre na hora), sem reciclar duas vezes.
- Wi-Fi com portal cativo: reportado como rede disponível; as tentativas seguem o backoff normal.
- Volta ao primeiro plano com a conexão desistida ou desconectada de propósito: nada acontece.

**Fila**
- Entrar na fila enquanto a fila já está conectando ou procurando: recusado localmente como "já na fila", sem mandar nada (ver Assumptions).
- `join_queue` enviado sem sucesso (socket fechou entre abrir e enviar): a busca continua e o reenvio acontece na próxima abertura.
- Queda durante a busca e reenvio recusado (ex.: o deck foi apagado enquanto a conexão voltava): a recusa é entregue normalmente e o estado é fora da fila; não conta como tentativa.
- Queda com a fila fora de busca (depois de uma recusa, socket aberto): não reconecta; o estado continua fora da fila e a próxima entrada abre de novo.
- `match_found` que não decodifica (campo faltando): registrado no log como frame inválido; o estado continua procurando.
- `match_found` chegando depois de o jogador sair: descartado; o socket já foi fechado.
- Recusa chegando depois do `match_found`: descartada.
- Queda entre o pareamento no servidor e a chegada do `match_found`: limitação conhecida do backend, sem contorno no cliente (ver Assumptions).

**Partida**
- `match_start` recebido de novo a cada reconexão: entregue como qualquer frame; o tratamento continua o de hoje.
- Socket de partida aberto depois do fim da partida: o servidor não fecha; a conexão continua viva até sair de propósito.

## Requirements *(mandatory)*

### Functional Requirements

**Conexão autenticada**

- **FR-001**: MUST existir um único componente de conexão autenticada, reutilizado pelo socket de fila e pelo de partida; cada socket só acrescenta parâmetros de URL e trata os próprios frames.
- **FR-002**: Antes de cada abertura, a conexão MUST pedir um token válido à porta de token, imediatamente antes de abrir, e colocá-lo no parâmetro `token` da URL — nunca em cabeçalho, nunca reaproveitado de antes de uma espera.
- **FR-003**: Pedido de token que responde sem sessão ou sessão expirada MUST fazer a conexão desistir com esse motivo; pedido que falha por transporte MUST levar a esperar o backoff sem contar recusa de token.
- **FR-004**: `auth_denied`, fechamento 4001, ou os dois no mesmo socket MUST contar como uma recusa de token e levar a: estado renovando token, uma renovação imediata na porta de token, e nova abertura.
- **FR-005**: Recusas de token seguidas acima de um limite (padrão 2, injetável) MUST fazer a conexão desistir com o motivo "token recusado repetidamente".
- **FR-006**: Renovação que responde sessão expirada MUST fazer a conexão desistir com esse motivo; renovação que falha por transporte ou por indisponibilidade MUST levar a esperar o backoff sem contar recusa de token.
- **FR-007**: `match_denied` ou fechamento 4400, 4403 ou 4404 MUST fazer a conexão desistir, sem renovar token e sem nova tentativa, com motivo tipado: sem identificador de partida (4400), não participa da partida (4403), partida não existe (4404), ou partida recusada sem detalhe.
- **FR-008**: Qualquer outro fechamento — com ou sem código, inclusive 1000 e 1001 vindos do servidor — e toda falha ao abrir MUST levar a nova tentativa com espera exponencial, teto e jitter; a fonte de aleatoriedade do jitter MUST ser injetável; o limite de tentativas MUST ser configurável por socket.
- **FR-009**: A conexão MUST considerar-se provada no primeiro frame decodificado do socket atual que não é negação de gate — inclusive `pong`. A prova MUST zerar a contagem de recusas de token, as tentativas e o backoff; abrir o socket sozinho MUST NOT zerar nada.
- **FR-010**: Saída de propósito MUST fechar o socket e MUST NOT reabrir — nem quando uma espera, uma renovação ou uma abertura em curso terminarem depois — e nenhum evento de reconexão MUST ser emitido depois dela.
- **FR-011**: A conexão MUST expor o estado atual e avisar cada mudança: desconectado, conectando, conectado, esperando nova tentativa (número da tentativa e espera), renovando token, suspenso (segundo plano ou sem rede) e desistiu (motivo).
- **FR-012**: A conexão MUST emitir "voltou depois de cair" exatamente uma vez por recuperação, quando a conexão reaberta é provada; a primeira conexão MUST NOT emitir esse aviso.
- **FR-013**: O motivo de desistência MUST ser uma família fechada — sem sessão, sessão expirada, token recusado repetidamente, partida recusada (com o detalhe de FR-007) e tentativas esgotadas — com um texto legível pelo jogador derivado dela.
- **FR-014**: Frames MUST ser decodificados pelo codec do projeto e entregues a quem consome na ordem de chegada, na thread principal. Frame que não decodifica MUST ser registrado no log e descartado, sem derrubar a conexão.
- **FR-015**: Enviar uma mensagem sem conexão aberta MUST devolver um resultado observável de "não enviado", registrado no log, sem exceção.
- **FR-016**: O token de acesso MUST NOT aparecer em nenhum registro de log, inclusive em URLs registradas.

**Heartbeat**

- **FR-017**: A conexão aberta MUST mandar `ping` logo depois de abrir e, a partir daí, a cada intervalo (padrão 10 s, injetável), com um marcador do instante monotônico do envio no payload.
- **FR-018**: Cada `pong` MUST ser casado pelo marcador ecoado, não pela posição, e expor a latência medida no relógio monotônico; `pong` sem marcador conhecido MUST contar como vida e prova, sem latência.
- **FR-019**: Qualquer frame recebido MUST zerar a contagem de silêncio. Só depois do primeiro `pong` do socket atual, silêncio igual ou maior que o limite (padrão 30 s, injetável e maior que o intervalo) MUST derrubar a conexão pelo caminho de queda de FR-008.
- **FR-020**: Tempo pausado MUST NOT contar como silêncio: ao voltar ao primeiro plano, e sempre que o intervalo entre duas avaliações passar de um limiar (injetável, maior que o intervalo normal entre quadros e menor que o limite de silêncio), a contagem de silêncio MUST ser zerada antes de ser avaliada.
- **FR-021**: Frames recebidos e ainda não entregues no momento de uma avaliação MUST ser contados como vida antes dela; a conexão MUST NOT ser declarada morta enquanto houver frame recém-chegado.

**Segundo plano e rede**

- **FR-022**: Com o app em segundo plano, novas tentativas MUST ficar suspensas: o tempo suspenso MUST NOT consumir tentativas, e o estado MUST ser suspenso (segundo plano). O cliente MUST NOT fechar um socket aberto por ir para segundo plano.
- **FR-023**: Ao voltar ao primeiro plano, a conexão MUST zerar a detecção de silêncio; se o socket caiu, está esperando ou está suspenso, MUST abrir imediatamente, com backoff zerado, pedindo um token válido antes (FR-002); se o socket continua aberto, MUST mandar um `ping` imediato. Conexão desistida ou desconectada de propósito MUST NOT reagir.
- **FR-024**: Mudança entre dois tipos de rede disponíveis (Wi-Fi/rede local ↔ dados móveis) com o socket aberto, abrindo ou esperando MUST fechar o socket atual e abrir outro imediatamente, sem espera, sem consumir tentativa e sem desistir.
- **FR-025**: Sem rede, a conexão MUST NOT tentar abrir; o estado MUST ser suspenso (sem rede) e nenhuma tentativa MUST ser consumida. Quando a rede voltar, MUST abrir imediatamente.
- **FR-026**: No build Windows, o jogo MUST continuar executando minimizado ou sem foco, e minimizar MUST NOT fechar o socket nem suspender tentativas ou heartbeat. A configuração que garante isso MUST ser verificada por teste automatizado.

**Fila**

- **FR-027**: A fila MUST permitir entrar com um `DeckId`: se desconectada, abre a conexão (estado conectando) e manda `join_queue` com esse `deck_id` a cada abertura; se já conectada, manda na hora. Enviado com sucesso, o estado MUST ser procurando.
- **FR-028**: Entrar na fila com a fila conectando ou procurando MUST ser recusado localmente com o resultado "já na fila", sem mandar nada.
- **FR-029**: A fila MUST permitir sair: o socket é fechado de propósito (FR-010), o estado é fora da fila, e nenhum aviso de falha é emitido.
- **FR-030**: `match_found` MUST ser entregue como pareamento tipado — `MatchId`, o próprio jogador e o oponente, cada um com `UserId`, apelido, ícone e nível —, o estado MUST ser pareado, e o socket de fila MUST ser fechado de propósito logo depois.
- **FR-031**: Recusas de fila MUST ser comparadas pelo `code` e entregues tipadas: `deck_not_specified`; `deck_not_found` com o `DeckId` ecoado; `invalid_deck` com a lista de `DeckProblem` na ordem do servidor, lida pela mesma família da feature 002; `malformed_message`; `unknown_message_type`; e código desconhecido preservando o texto do `code`. O texto de `error` só serve para leitura humana.
- **FR-032**: `matchmaking_failed` MUST ser um aviso próprio, separado das recusas, com o texto do servidor.
- **FR-033**: Recusa e `matchmaking_failed` MUST levar ao estado fora da fila com o socket aberto, sem reconexão e sem reenvio.
- **FR-034**: Queda durante a busca MUST seguir a reconexão da conexão (com o limite de tentativas da fila), e cada reabertura MUST reenviar `join_queue` com o mesmo `DeckId`, sem ação de quem consome; durante a reconexão o estado MUST ser conectando.
- **FR-035**: Desistência da conexão com a fila conectando ou procurando MUST emitir "saiu da fila" com o motivo da desistência e deixar o estado fora da fila.
- **FR-036**: Queda com a fila fora de busca MUST NOT reconectar; a próxima entrada abre de novo.
- **FR-037**: A queda entre o pareamento no servidor e a chegada do `match_found` MUST ser registrada no research como limitação conhecida e como pedido ao backend, sem contorno no cliente (ver Assumptions).

**Código existente, evoluindo no lugar**

- **FR-038**: `BaseClient` MUST passar a ser, ou delegar para, a conexão autenticada; `MatchmakingClient` e `MatchClient` MUST usá-la. MUST NOT existir segunda implementação de conexão.
- **FR-039**: `MatchClient` MUST abrir com `matchId` e `token` na URL, desistir em `match_denied`/44xx (FR-007) e, a cada reconexão, receber `match_start` de novo; o que ele faz com o payload de `match_start` MUST continuar como hoje.
- **FR-040**: `MatchmakingClient` MUST entrar e sair da fila por FR-027 a FR-036. O botão Jogar MUST entrar com o primeiro deck da lista de decks do jogador; sem deck, ou com a lista indisponível, MUST NOT entrar e MUST registrar o fato no log.
- **FR-041**: O fluxo `match_found` → `VersusScene` → conectar `MatchClient` com o `MatchId` recebido MUST continuar funcionando.
- **FR-042**: `ReconnectOverlay` MUST continuar mostrando o aviso de reconexão com o número da tentativa enquanto um socket de fila ou partida espera nova tentativa ou está suspenso sem rede, escondê-lo quando o socket volta, e mostrar o motivo quando desiste; MUST NOT mostrar nada numa saída de propósito nem no fechamento da fila depois do `match_found`.
- **FR-043**: `Heartbeat` e `ReconnectPolicy` MUST ir para o núcleo com os testes existentes; um teste existente só MUST mudar onde esta spec muda o comportamento (1000 reconectável, zerar na prova, pausa não é silêncio), e cada mudança MUST ser registrada no plano.
- **FR-044**: A NativeWebSocket (`Assets/WebSocket`), `WebSocketDispatcher`, `ConnectionClient`, a leitura síncrona `PlayerSession.Token` e `TokenRefreshService` MUST ser removidos, sem referência restante.
- **FR-045**: Os clientes de socket MUST receber as dependências na construção e MUST NOT ler singletons `Instance` para token, conexão ou despacho. `PlayerSession.Instance` MAY continuar como raiz de composição das cenas até a feature 5, registrado como dívida no plano e em `Game/TODO.md`.
- **FR-046**: Os clientes de socket MUST registrar pelo `IClientLog`, com campos estruturados; `Debug.Log` e `JsonUtility` MUST NOT ser usados neles.
- **FR-047**: Nenhum tipo do núcleo MUST referenciar o motor, a biblioteca de JSON de terceiros ou a implementação concreta de socket, verificado pelo teste de fronteira existente.

### Key Entities

- **Conexão autenticada**: um socket lógico que sobrevive a vários sockets físicos; tem estado, contagem de tentativas, contagem de recusas de token e, enquanto aberta, heartbeat.
- **Estado da conexão**: desconectado, conectando, conectado, esperando nova tentativa (tentativa, espera), renovando token, suspenso (segundo plano | sem rede), desistiu (motivo).
- **Motivo de desistência**: sem sessão, sessão expirada, token recusado repetidamente, partida recusada (sem identificador | não participa | não existe | sem detalhe), tentativas esgotadas.
- **Política de reconexão**: espera base, teto, jitter, limite de tentativas, limite de recusas de token; uma por socket.
- **Heartbeat**: intervalo de ping, limite de silêncio, limiar de pausa, última latência medida.
- **Ping / Pong**: mensagem do contrato 013 com marcador de instante monotônico; o pong ecoa o marcador.
- **Estado da fila**: fora da fila, conectando, procurando, pareado; com o `DeckId` da busca atual.
- **Pareamento**: `MatchId`, jogador próprio e oponente (`UserId`, apelido, ícone, nível).
- **Recusa de fila**: deck não especificado, deck não encontrado (`DeckId`), deck inválido (lista de `DeckProblem`), mensagem malformada, tipo de mensagem desconhecido, código desconhecido (texto do `code`); todas com o `error` só para leitura.
- **Falha de pareamento**: `matchmaking_failed`, com o texto do servidor.
- **Saiu da fila**: aviso com o motivo de desistência da conexão.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A suíte EditMode passa 100% pelo comando único da constituição, com o editor fechado, e o código novo não produz nenhum aviso de compilação.
- **SC-002**: Zero referências à NativeWebSocket, ao `WebSocketDispatcher`, ao `ConnectionClient`, ao `TokenRefreshService` e à leitura síncrona `PlayerSession.Token` restam no projeto.
- **SC-003**: Zero tipos do núcleo referenciam o motor, a biblioteca de JSON de terceiros ou a implementação concreta de socket, verificado por teste automatizado.
- **SC-004**: Em 100% dos roteiros EditMode desta feature, nenhuma abertura acontece depois de uma saída de propósito, e zero tentativas são consumidas enquanto o app está em segundo plano ou sem rede.
- **SC-005**: Em 100% dos roteiros de gate, a conexão desiste sem nova abertura depois de `match_denied`/44xx, e abre no máximo (limite + 1) vezes seguidas com o token recusado.
- **SC-006**: Em 0 roteiros um salto de tempo entre avaliações, com frames recém-chegados, derruba a conexão; em 100% dos roteiros de silêncio real acima do limite depois do primeiro `pong`, ela é derrubada e reconecta.
- **SC-007**: Os testes LiveServer passam contra o backend local subido com `docker compose up`: duas contas novas na fila com o deck inicial recebem `match_found` com o mesmo `MatchId`; `deck_id` inexistente dá `deck_not_found` e `deck_id` em texto dá `deck_not_specified`, com o socket aberto e um `join_queue` válido funcionando depois; derrubar o socket de uma conta procurando a devolve à fila sozinha e ela ainda é pareada; o socket de partida com o `MatchId` recebido recebe `match_start`, e com um `matchId` inventado recebe `match_denied` e fechamento 4404 real; um socket aberto com token de acesso inválido renova e conecta.
- **SC-008**: Com dois jogadores do Multiplayer Play Mode, Login → Home → Jogar → pareamento → `VersusScene` → `match_start` completa em 100% das tentativas, sem ação além de apertar Jogar.
- **SC-009**: Seguindo só o quickstart no Android (build de desenvolvimento), o jogador que troca de Wi-Fi para dados móveis procurando partida está procurando de novo em até 10 segundos, sem tocar na tela, na primeira tentativa.
- **SC-010**: Seguindo só o quickstart no Android, depois de mais de 5 minutos minimizado procurando partida, o jogador está procurando de novo em até 10 segundos após voltar, sem pedir login.
- **SC-011**: Seguindo só o quickstart no Windows, minimizar a janela por 2 minutos procurando partida não derruba o socket, e a busca continua.
- **SC-012**: Os testes EditMode desta feature, somados, executam em menos de 5 segundos (fora a inicialização do editor), sem espera real de tempo.

## Assumptions

- **Trechos cortados da descrição** foram lidos assim: "depende d… (achata…)" = depende da NativeWebSocket; "seguidas acima de um limite" = recusas de token seguidas; "`pong` conta como prova de sessão autenti…" = autenticada, porque hoje só frames que não são `pong` zeram a contagem e o socket de fila só recebe `pong` até o `match_found`; "a detecç…" = a detecção de silêncio; "sem rede: espe…" = espera sem gastar tentativas; "sair (fecha o socket de p…" = de propósito; "o socket de fila é fe…" = fechado; "`deck_not_found` (…`deck_id` ecoado)" = com o `deck_id`; "eventos eq…" = equivalentes; "manter o so…" = manter o socket vivo em segundo plano; "renovação sem … sem contar" = sem rede, espera sem contar; "volt…" = volta conectada, com "voltou depois de cair"; "volta ao primeiro plano …" = reconecta na hora; "`match_s…`" = `match_start`.
- **"nenhum `I…`" nos critérios de pronto** foi lido como "nenhum singleton `Instance` lido pelos clientes de socket nem pelo núcleo" (FR-045), já que `PlayerSession.Instance` pode continuar como raiz de composição. Se o corte dizia outra coisa, este é o ponto a corrigir antes do plano.
- **Limites padrão**, todos injetáveis e ajustáveis no plano com justificativa, partindo dos valores que o código já usa: 2 recusas de token seguidas (`ReconnectPolicy`); ping a cada 10 s e silêncio de 30 s (`Heartbeat`); fila com espera base 0,5 s, teto 5 s e 5 tentativas, partida com espera base 0,5 s, teto 15 s e sem limite de tentativas (`NetworkBootstrap`). "Poucas tentativas" da fila = o limite de tentativas da conexão de fila, que só zera na prova.
- **Zerar na prova, não na abertura**: o código atual zera o backoff ao abrir. Como o gate aceita o socket antes de recusar e o `pong` agora prova a sessão, zerar só na prova (FR-009) impede que um socket que abre e cai repetidamente nunca chegue ao limite; o `ping` logo depois de abrir (FR-017) faz a prova chegar em uma ida e volta.
- **1000 e 1001 do servidor são reconectáveis**: o `ReconnectPolicy` atual trata 1000 como terminal, mas a descrição diz que toda queda fora dos gates vai para o backoff, e o servidor não fecha socket ocioso nem o de partida no fim. O teste existente muda com esse registro (FR-043).
- **Estado "suspenso"** é acrescentado aos estados da descrição para o segundo plano e o sem-rede serem observáveis (testes e aviso de reconexão); não gasta tentativa.
- **Entrar enquanto procura é recusado localmente** (FR-028): no backend, um `join_queue` recusado não tira a entrada anterior da fila (`handle_join_queue` recusa antes de chamar a fila e não chama `leave`), então aceitar "trocar de deck procurando" deixaria o cliente dizendo "fora da fila" com o jogador ainda na fila. Trocar de deck é sair e entrar. Não há tela de escolha de deck nesta feature.
- **Queda fora de busca não reconecta** (FR-036): o socket de fila sem busca não sustenta nada, e reconectá-lo gastaria tentativas e mostraria aviso sem motivo.
- **Windows e segundo plano**: hoje `ProjectSettings.asset` tem `runInBackground: 0`; a feature liga a opção e o teste de FR-026 a verifica. Como o adaptador de ciclo de vida da 001 reporta minimizar no Windows é conferido no research.
- **Limitação conhecida — `match_found` perdido** (confirmada lendo o backend): o pareamento acontece no `PAIR_SCRIPT` de `server/apps/game/matchmaking/queue.py`, que tira os dois jogadores da fila de forma atômica; `MatchmakingConsumer.open_match` (`server/apps/game/consumers/matchmaking.py`) grava a partida e manda `match_found` por `group_send` ao grupo `matchmaking.user.<user_id>`. Se o socket do jogador já caiu, o grupo está vazio e a mensagem se perde; se o socket está meio aberto (o servidor não fecha socket ocioso nem tem heartbeat próprio), o `match_found` vai para um canal morto. Nenhuma rota expõe partida em andamento (`/game/cards/`, `/game/matches/` só com histórico, `/players/me/`, `/players/decks/`), e reconectar não reenvia `match_found`. O cliente reentra na fila e pode ser pareado de novo enquanto a partida perdida corre o prazo do mulligan. Vai para o research como limitação e pedido ao backend (reenviar `match_found` ao reconectar, ou rota de partida em andamento), sem contorno no cliente.
- **Limitação conhecida — saída tardia do socket antigo** (encontrada na mesma leitura): `on_disconnect` chama `queue.leave(self.user_id)` por usuário, não por socket. Se o servidor só percebe a morte do socket antigo depois de o socket novo reenviar `join_queue`, a saída do antigo remove a entrada do novo, e o cliente fica em "procurando" sem estar na fila. Também vai para o research como pedido ao backend, sem contorno no cliente.
- **`PlayButton`**: a classe no arquivo `UI/PlayButton.cs` se chama `MyButtonScript`; "PlayButton" na descrição se refere a ela. A lista de decks vem de `PlayerDecks.ListAsync` da feature 002.
- **Aviso de reconexão** continua ligado aos dois sockets, como no `NetworkBootstrap` de hoje.
- **Tecnologias citadas na descrição** (`ClientWebSocket`, Run In Background, NativeWebSocket) são restrições já decididas pela constituição ou código a remover, e vão para o plano e o research.
- **Dependências**: portas, fakes e codec da 001; `IAccessTokenSource`, `AccountSession`, `PlayerDecks`, `DeckId` e `DeckProblemUnion` da 002; backend local em `C:/Users/gabri/Projetos/dev_container/anathema/backend` com `docker compose up` para LiveServer e quickstart; `scripts/smoke_match.py` como referência de cliente que funciona.
- **Fora do escopo**: formas de `match_start`/`match_update`, espelho, relógio da vez e comandos (feature 4); fachada para a apresentação e prova final (feature 5); tela de escolha de deck, tela de fila e qualquer UI além de manter `ReconnectOverlay` e o botão Jogar funcionando; serviço nativo em primeiro plano no Android para manter o socket vivo em segundo plano; contorno no cliente para as limitações do backend acima; iOS, WebGL, macOS e Linux.
