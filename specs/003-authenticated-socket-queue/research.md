# Research: Socket autenticado e fila

**Feature**: `003-authenticated-socket-queue` | **Date**: 2026-09-14

Cada item: decisão, por quê, alternativas. As formas das mensagens são as dos
contratos do backend citados na spec e não são repetidas.

---

## R1. Onde moram a conexão e a fila

**Decisão**: assembly nova `Anathema.Net.Connection` (`noEngineReferences: true`),
referenciando `Anathema.Net.Core` e `Anathema.Net.Account`.

- `Connection/`: conexão autenticada, política de reconexão, heartbeat,
  classificação do fim de socket, suspensão.
- `Matchmaking/`: fila, frames de fila, recusas de fila.
- `match_denied` vai para `Anathema.Net.Core/Protocol/Frames`, ao lado de
  `auth_denied`: é gate, e quem o lê é a conexão, não a partida.
- `ConnectionFrames.CreateUnion()` (Connection) monta a união do codec:
  `GenericServerFrames.CreateUnion()` + `match_found` + `matchmaking_failed`.
  `LiveNetworkAdapters` passa a usá-la; `Anathema.Net.Unity` referencia
  `Anathema.Net.Connection`.

**Por quê**: a conexão precisa de `IAccessTokenSource`, que mora na conta.
Colocá-la no núcleo criaria ciclo; colocá-la na conta misturaria HTTP de conta
com socket. O princípio VI já prevê "orquestração da sessão" como camada
própria.

**Alternativas**:
- Conexão em `Anathema.Net.Core`: o núcleo passaria a depender da conta.
- Conexão em `Anathema.Net.Account`: duas responsabilidades numa assembly.
- Duas assemblies (`Connection` e `Matchmaking`): a fila tem uns dez tipos e um
  consumidor só. Separar agora é cerimônia; a feature 5 pode separar se a
  fachada pedir.

---

## R2. Tempo: quadros, esperas e silêncio

**Decisão**: porta nova `IFrameTicker` (`Anathema.Net.Core/Time`), com
`event Action? Ticked`.

- `NetworkLayerHost.Update` faz, nesta ordem: `reachability.Poll()`,
  `queue.Drain()`, `ticker.Raise()`.
- A cada tick a conexão lê `IMonotonicClock.Now`. Espera de nova tentativa é um
  instante (`retryAt`), não um contador; o heartbeat recebe o delta desde o tick
  anterior.
- Nada de `Task.Delay`, `System.Threading.Timer` nem `Time.deltaTime`.
- Adaptador `UnityFrameTicker` (`Anathema.Net.Unity`); fake `FakeFrameTicker`
  com `Tick()`.

**Por quê**: espera e silêncio viram função do relógio injetado. O teste avança
o `FakeMonotonicClock` e chama `Tick()`, sem esperar tempo real (SC-012). A ordem
`Drain` → `Tick` entrega os frames que já estavam na fila antes de avaliar o
silêncio.

**Alternativas**:
- `Task.Delay`: roda no pool de threads, volta fora da thread principal e só se
  testa esperando.
- `Heartbeat.Tick(Time.deltaTime)` como hoje: `Time.deltaTime` é do motor e é
  limitado por `maximumDeltaTime`, o que esconde a pausa em vez de revelá-la.

---

## R3. Frame recém-chegado nunca declara a conexão morta (FR-020, FR-021)

**Decisão**: duas proteções.

1. **Salto de tempo**: se o delta entre dois ticks passa do limiar de pausa
   (`ConnectionTiming.PauseThreshold`, padrão 5 s), a conexão chama
   `Heartbeat.ForgivePause()` antes de `Tick`. Idem ao voltar ao primeiro plano.
2. **Confirmação atrás da fila**: quando o heartbeat responde `DeclareDead`, a
   conexão não derruba na hora. Ela enfileira na `MainThreadQueue` uma
   confirmação marcada com a geração do socket. Como a fila é FIFO, todo frame
   que o socket recebeu antes do enfileiramento é entregue antes, e zera o
   silêncio. Ao rodar, a confirmação só derruba se a geração ainda é a mesma e o
   silêncio continua acima do limite.

**Por quê**: o socket recebe em outra thread. Um frame pode entrar na fila
depois do `Drain` e antes do `Tick` do mesmo quadro; depois de uma pausa longa,
vários frames esperam na fila enquanto o primeiro tick vê 60 s de delta. Só o
limiar cobre o segundo caso; só a confirmação cobre o primeiro.

**Teste**: salto de 60 s com um `pong` enfileirado na `MainThreadQueue` antes do
tick → nenhum fechamento (spec US3-4). Silêncio de 31 s em ticks de 1 s, sem
frame → confirmação enfileirada → `Drain` → derruba (US3-2).

**Alternativas**:
- Instante de recepção gravado na thread do socket: exige mudar `IWebSocket`
  da 001 e ler estado entre threads.
- Só o limiar de pausa: não cobre o frame entre `Drain` e `Tick`.

---

## R4. Pedido de token assíncrono e resultado que chega tarde

**Decisão**:

- `GetValidAsync` e `RenewNowAsync` são aguardados dentro de um método `async
  Task` da conexão, sem `ConfigureAwait(false)`. A tarefa é guardada e
  observada; nada de `async void`.
- Cada tentativa tem uma geração (`attemptGeneration`). Resultado que volta com
  geração diferente — saída de propósito, troca de rede, volta ao primeiro plano
  que reiniciou a tentativa — é descartado e registrado (`connection_stale_token_result`).
- Exceção inesperada na tentativa vira log `connection_attempt_failed` e é
  tratada como queda comum.

**Por quê**: no player, a continuação volta à thread principal pelo contexto de
sincronização do Unity, e os adaptadores HTTP da 002 já completam as tarefas
pela `MainThreadQueue` (research R2 da 002). Nos testes o fake completa na hora,
ou é segurado para provar o descarte.

**Alternativas**:
- Callback, como o `TokenRefreshService`: é justamente a ponte que sai.
- Trava: tudo roda na thread principal; trava só esconderia ordem errada.

---

## R5. Fake de `IAccessTokenSource` e o construtor interno de `AccessToken`

**Decisão**:

- `Anathema.Net.Fakes` passa a referenciar `Anathema.Net.Account`.
- `AccountAssemblyInfo.cs` ganha `[assembly: InternalsVisibleTo("Anathema.Net.Fakes")]`.
- `FakeAccessTokenSource` cria `AccessToken` pelo construtor interno; roteiriza
  desfechos de `GetValidAsync` e `RenewNowAsync` em fila; `HoldNextRenewal()`
  segura uma renovação; registra quantas chamadas houve.
- `SpoiledFirstTokenSource` (também em Fakes) embrulha um `IAccessTokenSource`
  real e troca o texto do primeiro token válido por um inválido. Serve ao teste
  LiveServer "token inválido renova e conecta".

**Por quê**: a descrição pede um fake de `IAccessTokenSource`. O construtor é
interno para impedir token sem leitor no código de produção, e continua assim:
só a assembly de fakes, que é só de editor e só com `UNITY_INCLUDE_TESTS`,
enxerga.

**Alternativas**:
- Construtor público: qualquer código de produção criaria token sem leitor.
- Porta nova só com texto (`IConnectionTokens`) e adaptador: duplica a porta que
  a 002 criou exatamente para isto.
- `SessionAccessTokens` real com HTTP fake em cada teste: acopla os testes da
  conexão à sessão de conta e aos JWT de teste.

---

## R6. Um socket por tentativa, e o socket descartado

**Decisão**:

- Porta `IWebSocketFactory` (`Anathema.Net.Core/Socket`) com `IWebSocket Create()`.
- `DotNetWebSocketFactory` (Unity) cria `DotNetWebSocket`; `FakeWebSocketFactory`
  (Fakes) guarda `Created` e `Latest`.
- A conexão marca cada socket com uma geração. Ao descartar um socket (morte
  por silêncio, troca de rede, saída de propósito), ela **desliga os handlers
  antes de chamar `Close()`**.

**Por quê**: `IWebSocket` é uma conexão por instância (contrato da 001). O
`DotNetWebSocket` espera até 5 s pelo eco do close antes de publicar `Closed`
(research R4 da 001); esperar esse aviso atrasaria a reabertura e contradiz
"recicla na hora" (FR-024). Com os handlers desligados, o `Closed` tardio do
socket antigo nunca chega à conexão.

**Alternativas**:
- `Func<IWebSocket>`: não é fake nomeado (princípio VII).
- Reusar a instância: proibido pelo contrato da 001.

---

## R7. Como o fim de um socket é classificado

**Decisão**: `SocketEndClassifier` (puro) recebe o que o socket atual mostrou
(`auth_denied` visto, `match_denied` visto) e o `SocketClosure`, e devolve
`SocketEnd`:

| Visto no socket | Close code | `SocketEnd` |
|---|---|---|
| `auth_denied` | qualquer ou nenhum | `TokenRefused` |
| — | 4001 | `TokenRefused` |
| qualquer | 4400 | `MatchRefused(NoMatchId)` |
| qualquer | 4403 | `MatchRefused(NotAParticipant)` |
| qualquer | 4404 | `MatchRefused(MatchNotFound)` |
| `match_denied` | outro ou nenhum | `MatchRefused(Unspecified)` |
| — | qualquer outro, nenhum, 1000, 1001 | `Dropped(code)` |

`auth_denied` tem precedência: o servidor manda um gate por socket, e o de
autenticação vem primeiro.

A conexão traduz para o `ReconnectPolicy` existente:

- `TokenRefused` → `policy.OnClosed(AuthRejected)` → `RefreshTokenThenRetry` ou `GiveUp`;
- `MatchRefused` → desiste direto, sem passar pela política;
- `Dropped` → `policy.OnClosed(code ?? AbnormalClosure)`;
- prova de sessão → `policy.Reset()`;
- volta ao primeiro plano, rede de volta, troca de rede → `policy.ResetBackoff()`.

Mudanças no `ReconnectPolicy` e nos testes dele, e só elas:

- 1000 sai dos códigos terminais (spec, Assumptions). O teste
  `CodigosTerminaisDesistemSemGastarTentativa` perde 1000, e
  `FechamentoNormalDoServidorEhReconectavel` entra.
- A abertura deixa de chamar `ResetBackoff`: só a prova zera (FR-009). O método
  continua, usado nas reaberturas imediatas de FR-023 a FR-025, e o teste dele
  continua igual.
- Classe e testes ganham `#nullable enable` e o namespace
  `Anathema.Net.Connection`. A API continua em `double` de segundos, para os
  testes mudarem só no que a spec muda; os comentários ficam.

**Por quê**: a política e os 14 testes dela continuam valendo. O que muda é a
fonte do código: vem do `SocketClosure` exato do `DotNetWebSocket`, e não mais
dos flags `tokenRejected`/`rejectionReason` do `BaseClient`, que existiam porque a
NativeWebSocket achatava o número.

**Alternativas**: reescrever a política por tipo de fim, perdendo os testes que
a descrição pede para manter.

---

## R8. Heartbeat e latência

**Decisão**:

- `Heartbeat` vai para `Anathema.Net.Connection` com a API atual (`Tick`,
  `NoteInbound`, `NotePong`, `Reset`, `SilenceSeconds`) e os 10 testes dele.
  Ganha:
  - `ForgivePause()`: zera o silêncio e mantém o timeout armado;
  - `NotePingSent()`: zera a contagem do intervalo, para o ping da abertura e da
    volta não ser seguido por outro logo depois.
- Ping logo depois de abrir e na volta ao primeiro plano com o socket aberto.
- `PingLedger`: gera `PingMarker(sentAtMs, sequence)` com `sentAtMs =
  clock.Now.Ticks / TimeSpan.TicksPerMillisecond` e sequência crescente por
  conexão; guarda até 8 marcadores pendentes (o mais velho sai); casa o `pong`
  pelo marcador e devolve a latência. Marcador desconhecido ou ausente: vida e
  prova, sem latência.

**Números** (`ConnectionTiming`, injetáveis):

| Valor | Padrão | Origem |
|---|---|---|
| Intervalo de ping | 10 s | `Heartbeat` atual; spec 013 do backend |
| Silêncio aceito | 30 s | `Heartbeat` atual; spec 013 do backend (SC-010 de lá) |
| Limiar de pausa | 5 s | maior que um carregamento de cena típico travando a thread principal (< 2 s), menor que o intervalo |

**Alternativas**:
- Latência pela hora do sistema: proibido (constituição).
- Casar `pong` por posição: o contrato 013 manda casar pelo `type`, e pode
  haver vários pings pendentes.

---

## R9. Segundo plano, rede e Windows

**Decisão**:

- `ConnectionSuspension` (Connection) junta `IAppLifecycle` e
  `INetworkReachability` em dois flags, `InBackground` e `WithoutNetwork`.
  Enquanto qualquer um está ligado, `retryAt` não dispara e o estado é
  `Suspended` (segundo plano ganha de sem rede no motivo). Socket aberto não é
  fechado por isso.
- **Volta ao primeiro plano**: `ForgivePause()`. Socket aberto → ping imediato.
  Socket caído, esperando ou suspenso → `ResetBackoff()` e abertura já.
- **Rede**:
  - `Current == None` → liga `WithoutNetwork`;
  - `Previous != None && Current != None` → recicla: descarta o socket (R6) e
    abre outro já, sem `OnClosed` e sem tentativa;
  - `Previous == None` → desliga `WithoutNetwork` e recicla: abre já, descartando o
    socket se ele ainda parecer aberto (ajuste da implementação: o socket da
    interface perdida não é confiável, e o heartbeat levaria 30 s para perceber).
- **Android**: o sistema para o `Update`. O `WentToBackground` enfileirado no
  `OnApplicationPause(true)` só é entregue no primeiro `Drain` depois da volta,
  junto com o `Closed` do socket que caiu e o `ReturnedToForeground`, nessa
  ordem. A conexão passa por suspensa e reabre na mesma drenagem. Wi-Fi → dados
  móveis é percebido em até 1 s pelo polling existente
  (`NetworkKindTracker.PollInterval`), dentro dos 10 s de SC-009.
- **Windows**:
  - `ProjectSettings.asset`: `runInBackground: 0 → 1`.
  - `LifecycleSignalFilter` ganha o modo `BackgroundSignalMode`:
    `AndroidPause`, `DesktopStopsOnFocusLoss`, `DesktopKeepsRunning`. O
    construtor com `bool` continua e delega.
  - `UnityAppLifecycle.Create` escolhe `DesktopKeepsRunning` fora do Android
    quando `Application.runInBackground` está ligado; nesse modo pausa e perda
    de foco não são segundo plano.
  - Teste de editor `RunInBackgroundSettingTests` lê
    `PlayerSettings.runInBackground` (FR-026).

**Por quê**: com Run In Background o player não para ao minimizar. A
documentação do Unity não garante que `OnApplicationPause(true)` deixa de chegar
ao minimizar, e o filtro atual trataria esse aviso como segundo plano,
suspendendo tentativas no Windows. O modo explícito tira a dúvida do caminho; o
quickstart §6 confirma no build.

**Alternativas**:
- Fechar o socket ao ir para segundo plano no Android: perde a fila à toa quando
  a volta é rápida e o socket sobreviveu.
- Serviço em primeiro plano no Android: fora do escopo.
- `ConnectivityManager.NetworkCallback`: item do `Game/TODO.md` da 001, só se
  1 s se mostrar lento.

---

## R10. Fila

**Decisão**: `MatchQueue` sobre uma `AuthenticatedConnection`, com a intenção de
busca `DeckId? searchDeck`.

| Evento | Efeito |
|---|---|
| `Join(deck)` com fase ≠ fora da fila | `JoinOutcome.AlreadyQueued`, nada enviado |
| `Join(deck)` fora da fila | guarda o deck; fase conectando; conexão desconectada ou desistida → `Connect`; conexão conectada → envia |
| conexão entra em `Connected` com deck guardado | envia `join_queue`; `Sent` → procurando; `NotOpen`/`Failed` → continua conectando (a próxima abertura reenvia) |
| `message_refused` | `QueueRefusalReader`; fase fora da fila; deck limpo; aviso `Refused`; socket aberto |
| `matchmaking_failed` | fase fora da fila; deck limpo; aviso `MatchmakingFailed`; socket aberto |
| `match_found` | fase pareado; aviso `Paired`; `connection.Leave()` |
| conexão em `WaitingRetry`, `RenewingToken` ou `Suspended` com deck | fase conectando |
| conexão caiu sem deck (fora de busca) | `connection.Leave()` (FR-036) |
| conexão `GaveUp` com deck | fase fora da fila; deck limpo; aviso `LeftQueue(motivo)` |
| `Leave()` | `connection.Leave()`; fase fora da fila; deck limpo; nenhum aviso |
| frame com fase pareado ou fora da fila sem socket | descartado |

Leitura das recusas (`QueueRefusalReader`, sobre `MessageRefusedFrame.Details`):

- `deck_not_found`: `ReadDeckId("deck_id")`;
- `invalid_deck`: `ReadObjectList("deck_problems")`, cada item por
  `DeckProblemUnion.Create().ReadNested`, na ordem;
- código conhecido com forma quebrada (campo ausente ou de tipo errado): recusa
  `Unrecognized` com o texto do `code`, e log `queue_refusal_out_of_contract`
  com o caminho do campo.

`match_found` lê `match_id` com `ReadMatchId` e `self`/`opponent` com
`ReadUserId("user_id")`, `ReadText("nickname")`, `ReadText("icon")`,
`ReadInteger("level")`.

**Por quê**: o contrato 011 diz que entrar é mandar sem recusa, que a recusa não
fecha o socket, que fechar tira da fila e que o `join_queue` de novo substitui a
entrada (o que torna o reenvio seguro). "Já na fila" e "queda ociosa não
reconecta" estão justificados na spec (Assumptions).

**Alternativas**:
- Considerar procurando só depois do próximo `pong`: não há resposta de sucesso
  no contrato; o `pong` não diz nada sobre a fila.
- Reconectar o socket de fila ocioso: gasta tentativas e mostra aviso sem
  motivo.

---

## R11. Limitações do backend (investigação pedida pela spec, FR-037)

### Queda entre o pareamento e a chegada do `match_found` — confirmada

Evidências, no backend em `C:/Users/gabri/Projetos/dev_container/anathema/backend`:

- `server/apps/game/matchmaking/queue.py`, `PAIR_SCRIPT`: `LPOP` tira os dois
  jogadores da fila e `HDEL` apaga os decks, num passo atômico.
- `server/apps/game/consumers/matchmaking.py`, `open_match`: grava a partida
  (`self.matches.save(match)`) e só depois chama `announce_match` para os dois.
- `announce_match`: `group_send` ao grupo `matchmaking.user.<user_id>`
  (`BaseConsumer.user_group`). Grupo sem canal vivo descarta a mensagem; canal
  meio aberto recebe e nunca entrega.
- `server/apps/game/consumers/base.py`: o servidor não fecha socket ocioso e não
  tem heartbeat próprio; `disconnect` só roda quando o transporte percebe.
- Rotas (`server/apps/*/urls.py`, `routing.py`): `/game/cards/`,
  `/game/matches/` (histórico de partidas terminadas), `/players/me/`,
  `/players/decks/`, `ws/matchmaking/`, `ws/match/`. Nenhuma expõe partida em
  andamento.
- `on_connect` do `MatchmakingConsumer` não faz nada: reconectar não reenvia
  `match_found`.

Cenários:

1. O socket de A cai (Wi-Fi → dados) e, antes de o servidor perceber, B entra e
   fecha o par. O `match_found` de A vai para o canal morto. A partida existe e o
   prazo do mulligan corre (`opening_match_clock`).
2. O cliente de A reconecta e reenvia `join_queue`. A volta para a fila e pode
   ser pareado de novo, com uma partida perdida em aberto.

**Registro**: limitação conhecida, sem contorno no cliente (constituição,
princípio I). Pedido ao backend: reenviar `match_found` ao conectar em
`ws/matchmaking/` quando o usuário tem partida viva ainda no mulligan, **ou**
rota que devolva a partida em andamento do usuário autenticado.

### Saída tardia do socket antigo — confirmada na mesma leitura

- `MatchmakingConsumer.on_disconnect` chama `self.queue.leave(self.user_id)`.
- `LEAVE_SCRIPT` faz `LREM` e `HDEL` pelo `user_id`, sem saber de qual socket
  veio a entrada.

Cenário: o socket antigo de A fica meio aberto; o cliente recicla, o socket novo
reenvia `join_queue` (A entra de novo); o servidor só então percebe a morte do
antigo e roda `leave(A)`, que remove a entrada do socket novo. O cliente fica em
procurando sem estar na fila, e nada o avisa.

**Registro**: limitação conhecida, sem contorno no cliente. Pedido ao backend:
`leave` só remove a entrada se ela pertence ao canal que fechou (guardar
`channel_name` ao lado do deck e comparar no script).

Os dois pedidos entram no `Game/TODO.md` do vault, na seção desta feature.

---

## R12. `match_start` e o `MatchClient` até a feature 4

**Decisão**: `AuthenticatedConnection` expõe, além de `FrameReceived(ServerFrame)`,
`RawTextReceived(string)` com o texto inteiro de cada frame aceito. Só o
`MatchClient` assina, para continuar desserializando `match_start` com
`JsonConvert` para `MatchStateDTO` e chamando `MatchSession.Instance.ApplyState`,
como hoje. Registrado em Complexity Tracking e no `Game/TODO.md`.

**Por quê**: a spec deixa o tratamento de `match_start` para a feature 4 (FR-039),
e o codec não devolve JSON cru. Um evento de texto cru, documentado como ponte, é
o menor desvio e sai inteiro quando `match_start` virar frame tipado.

**Alternativas**:
- Registrar `match_start` tipado agora: é escopo da feature 4.
- `IPayloadReader.RawJson()`: poria texto JSON no contrato de leitura do núcleo,
  para sempre.
- `[Obsolete]` no evento: produz aviso de compilação, e a porta de qualidade exige
  zero aviso novo.

---

## R13. Composição, cenas e o que sai

**Decisão**:

- `PlayerSession` continua raiz de composição. `ComposeAccount` vira
  `ComposeNetwork`: adaptadores, ciclo de vida, alcançabilidade, `UnityFrameTicker`,
  `NetworkLayerHost`, conta e `LiveConnectionServices`. Expõe `Matchmaking`
  (`MatchmakingClient`) e `Match` (`MatchClient`) e liga o `ReconnectOverlay`.
- `NetworkBootstrap..cs` sai com o `.meta` e com o componente na
  `BootstrapScene`. O nome do arquivo (dois pontos) não bate com o nome da
  classe, e o Unity exige nome igual para anexar `MonoBehaviour`; o componente
  pode já estar como script ausente na cena. Conferir ao abrir a cena; em
  qualquer caso a composição passa ao `PlayerSession`, que resolve também a
  ordem de `Awake` entre os dois.
- `BaseClient` vira adaptador fino sobre `AuthenticatedConnection`, com os
  eventos que o `ReconnectOverlay` já assina (`OnReconnecting`,
  `OnReconnected`, `OnGaveUp`).
- `MatchmakingClient` recebe o `MatchClient` pelo construtor (sai
  `NetworkBootstrap.MatchClient`).
- `VersusContext.SetContext(MatchPairing)` substitui `SetContext(string json)`.
- Saem: `Assets/WebSocket/` inteiro, `Dispatcher.cs`, `ConnectionClient.cs`,
  `TokenRefreshService.cs`, `PlayerSession.Token`, `ErrorPayloadDTO.cs`,
  `VersusDTO.cs`, `Anathema.Reconnect.asmdef` e `Anathema.Reconnect.Tests.asmdef`
  (o conteúdo vai para `Anathema.Net.Connection` e seus testes).

**Por quê**: FR-038 a FR-046. O `PlayerSession.Instance` fica como raiz das cenas
até a feature 5 (FR-045), mas nenhum cliente de socket o lê para token, conexão
ou despacho: tudo entra pelo construtor.

**Alternativas**:
- Manter `NetworkBootstrap` compondo os clientes: dois objetos de composição na
  mesma cena, com ordem de `Awake` indefinida entre eles.
- Apagar `BaseClient` e fazer o overlay assinar a conexão: muda três arquivos
  antigos a mais sem ganho de comportamento.

---

## R14. Testes LiveServer

**Decisão**: `Assets/Tests/EditMode/Net.Unity/LiveServer/LiveQueueTests.cs`,
`[Explicit]`, `[Category("LiveServer")]`, `[UnityTest]`, no mesmo molde de
`LiveAccountTests` (fila da thread principal, `LiveNetworkAdapters`, guarda DPAPI
em pasta temporária, `LocalAccountRoutes`).

- Reais: `DotNetWebSocket`, `UnityHttpTransport`, relógio, codec, sessão de conta.
- Ciclo de vida e rede: `FakeAppLifecycle` e `FakeNetworkReachability(LocalArea)`,
  porque sinais do sistema não são produzíveis num teste de editor.
- Ticks: `FakeFrameTicker`, chamado pelo laço do teste junto com `queue.Drain()`.
- Contas novas por `AccountRegistration`; deck inicial pela primeira linha de
  `PlayerDecks.ListAsync`.
- "Derrubar o socket procurando" é `FakeNetworkReachability.SimulateKind(CarrierData)`:
  recicla o socket real, o servidor vê o fechamento e tira da fila, e o socket
  novo reentra — o mesmo caminho do Wi-Fi → dados.
- `deck_id` em texto: mensagem de teste `TextDeckJoinMessage : IOutgoingMessage`
  mandada por `AuthenticatedConnection.SendAsync`.
- Token inválido: `SpoiledFirstTokenSource` sobre `account.Tokens`.
- `matchId` inventado: a desistência com `MatchRefusalDetail.MatchNotFound` só
  sai do close code 4404 (R7), então prova o código real.

**Alternativas**: derrubar o socket por um adaptador de teste com `Abort` —
exigiria operação nova em `IWebSocket` só para teste.

---

## R15. Valores padrão

| Onde | Valor | Padrão | Origem |
|---|---|---|---|
| todos | recusas de token seguidas | 2 | `ReconnectPolicy` |
| todos | jitter | 0,2 | `ReconnectPolicy` |
| fila | espera base / teto / tentativas | 0,5 s / 5 s / 5 | `NetworkBootstrap.QueuePolicy` |
| partida | espera base / teto / tentativas | 0,5 s / 15 s / sem limite | `NetworkBootstrap.MatchPolicy` |
| todos | ping / silêncio / pausa | 10 s / 30 s / 5 s | R8 |
| todos | marcadores pendentes | 8 | R8 |

`ConnectionSettings.ForMatchmaking()` e `ConnectionSettings.ForMatch()` guardam
esses valores com os comentários de por quê que hoje estão no
`NetworkBootstrap`.
