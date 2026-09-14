# Data Model: Socket autenticado e fila

**Feature**: `003-authenticated-socket-queue` | **Date**: 2026-09-14

Tipos novos ou que mudam. Todo arquivo novo com `#nullable enable`; nenhum membro
chamado `id`. Formas das mensagens: contratos do backend citados na spec.

---

## Núcleo (`Anathema.Net.Core`)

**`IFrameTicker`** (`Time/`)
- `event Action? Ticked` — um aviso por quadro, na thread principal, depois de
  drenar a `MainThreadQueue`.

**`IWebSocketFactory`** (`Socket/`)
- `IWebSocket Create()` — uma instância nova por chamada (uma conexão).

**`MatchDeniedFrame : ServerFrame`** (`Protocol/Frames/`)
- `TypeName = "match_denied"`, `string Error`.
- Registrado em `GenericServerFrames.CreateUnion()`.

**`BackgroundSignalMode`** (`Lifecycle/`): `AndroidPause`,
`DesktopStopsOnFocusLoss`, `DesktopKeepsRunning`. `LifecycleSignalFilter` recebe
o modo; o construtor com `bool` delega (`true` → `DesktopStopsOnFocusLoss`,
`false` → `AndroidPause`).

---

## Conexão (`Anathema.Net.Connection/Connection`)

**`ConnectionTarget`**
- `Uri BaseUrl` (absoluta, `ws`/`wss`), parâmetros extras de query (ex.: `matchId`).
- `Uri WithToken(AccessToken token)` — acrescenta `token=` escapado.
- `static ConnectionTarget Matchmaking(Uri baseUrl)`,
  `static ConnectionTarget Match(Uri baseUrl, MatchId match)`.
- `ToString()` só com esquema, host e caminho: nunca query, nunca token (FR-016).

**`ConnectionPhase`**: `Disconnected`, `Connecting`, `Connected`,
`WaitingRetry`, `RenewingToken`, `Suspended`, `GaveUp`.

**`SuspensionReason`**: `Background`, `NoNetwork`.

**`ConnectionStatus`** (imutável)
- `ConnectionPhase Phase`
- `int Attempt`, `TimeSpan Wait` — só em `WaitingRetry`
- `SuspensionReason? Suspension` — só em `Suspended`
- `GiveUpReason? GiveUp` — só em `GaveUp`

**`GiveUpReason`**
- `GiveUpKind Kind`: `NoSession`, `SessionExpired`, `TokenRefusedRepeatedly`,
  `MatchRefused`, `AttemptsExhausted`.
- `MatchRefusalDetail? Match`: `NoMatchId` (4400), `NotAParticipant` (4403),
  `MatchNotFound` (4404), `Unspecified`.
- `int? Attempts` — em `AttemptsExhausted` e `TokenRefusedRepeatedly`.
- `string PlayerText()` — texto em português para o aviso (FR-013).

**`SocketEnd`** (resultado do classificador)
- `SocketEndKind Kind`: `TokenRefused`, `MatchRefused`, `Dropped`.
- `MatchRefusalDetail? Match`, `int? CloseCode`.

**`SocketEndClassifier`**: `SocketEnd Classify(bool sawAuthDenied, bool sawMatchDenied, SocketClosure closure)`.
Tabela em research R7.

**`ReconnectPolicy`, `ReconnectPlan`, `ReconnectAction`** — movidos de
`Assets/Scripts/Core/Network/Reconnect/`. Mudança de regra: 1000 deixa de ser
terminal (research R7).

**`Heartbeat`, `HeartbeatAction`** — movidos. Acrescentam `ForgivePause()` e
`NotePingSent()` (research R8).

**`PingLedger`**
- `PingMarker Next(MonotonicInstant now)` — `sent_at_ms` e sequência.
- `TimeSpan? Match(PingMarker? echoed, MonotonicInstant now)` — latência, ou nulo
  se o marcador não é pendente.
- Até 8 pendentes; o mais velho sai.

**`ConnectionTiming`**: `PingInterval` 10 s, `SilenceLimit` 30 s,
`PauseThreshold` 5 s. Validação: `SilenceLimit > PingInterval > PauseThreshold > 0`;
exceção com valor recebido e forma esperada.

**`ConnectionSettings`**
- `ReconnectPolicy Policy` — uma por conexão, com a contagem dela.
- `ConnectionTiming Timing`.
- `static ForMatchmaking()`, `static ForMatch()` (valores em research R15).

**`ConnectionPorts`** (agrupa o que a conexão recebe, para o construtor não passar
de poucos parâmetros)
- `IWebSocketFactory Sockets`, `IMonotonicClock Clock`, `IFrameTicker Ticker`,
  `IAppLifecycle Lifecycle`, `INetworkReachability Reachability`,
  `MainThreadQueue Queue`, `IClientLog Log`.

**`ConnectionSuspension`** — flags `InBackground`, `WithoutNetwork`; avisos
`Suspended`, `Resumed(ResumeCause)` com `ResumeCause`: `Foreground`,
`NetworkBack`, `NetworkKindChanged`.

**`SilenceWatch`** — heartbeat, `PingLedger`, delta entre ticks, confirmação
enfileirada (research R3). Avisos `PingDue`, `SilenceConfirmed`,
`LatencyMeasured(TimeSpan)`.

**`SocketAttempt`** — um socket físico: geração, abertura com token, handlers,
flags de gate vistos, prova. Avisos `Opened`, `Proven`, `FrameArrived(ServerFrame, string)`,
`Ended(SocketEnd)`. `Discard()` desliga handlers e fecha (research R6).

**`AuthenticatedConnection`** (orquestra; estado só na thread principal; `IDisposable`)
- Construtor: `(ConnectionPorts ports, IAccessTokenSource tokens, IProtocolCodec codec, ConnectionSettings settings)`.
- `ConnectionStatus Status`, `TimeSpan? LastLatency`.
- `void Connect(ConnectionTarget target)`, `void Leave()`,
  `Task<SocketSendOutcome> SendAsync(IOutgoingMessage message)`.
- Eventos: `StatusChanged(ConnectionStatus)`, `FrameReceived(ServerFrame)`,
  `RawTextReceived(string)` (ponte da feature 4, research R12), `Recovered`,
  `LatencyMeasured(TimeSpan)`.

### Estados da conexão

```text
Disconnected ──Connect──► Connecting ──aberto──► Connected ──prova──► (Recovered, se veio de queda)
     ▲                     │   ▲                    │
     │                     │   │                    ├─TokenRefused─► RenewingToken ─Renewed─► Connecting
     │                     │   │                    │                     ├─Expired/NoSession─► GaveUp
     │                     │   │                    │                     └─Unavailable───────► WaitingRetry
     │                     │   │                    ├─MatchRefused─────────────────────────► GaveUp
     │                     │   │                    └─Dropped / silêncio ─► WaitingRetry ─retryAt─┘
     │                     │   └───────────── Suspended ◄─ segundo plano / sem rede (sem socket aberto)
     │                     └─token: sessão indisponível ─► GaveUp; token indisponível ─► WaitingRetry
     └───────────── Leave (de qualquer estado) ─────────────────────────────────────────────
GaveUp ──Connect──► Connecting (política zerada)
```

| Transição | Efeitos |
|---|---|
| `Connect` | guarda o alvo; zera política; geração da tentativa++; pede token |
| token `Valid` | cria socket pela fábrica; `Open(target.WithToken(token))`; log sem query |
| aberto | fase `Connected`; `NotePingSent` + ping; heartbeat zerado |
| prova (1º frame que não é gate, inclusive `pong`) | `policy.Reset()`; `Recovered` se houve queda desde a última prova |
| `TokenRefused` | `policy.OnClosed(4001)`; `RefreshTokenThenRetry` → `RenewingToken` + `RenewNowAsync`; `GiveUp` → `TokenRefusedRepeatedly` |
| `Dropped` | `policy.OnClosed(code ?? 1006)`; `Retry` → `retryAt = now + wait`, `WaitingRetry`; `GiveUp` → `AttemptsExhausted` |
| `MatchRefused` | `GaveUp(MatchRefused, detalhe)`; nenhuma tentativa |
| tick com `now >= retryAt` e sem suspensão | geração++; pede token |
| segundo plano / sem rede sem socket aberto | `Suspended(motivo)`; `retryAt` congelado |
| volta ao primeiro plano / rede de volta | `ForgivePause`; aberto → ping; senão `ResetBackoff` e pede token já |
| troca Wi-Fi ↔ dados | `Discard` do socket; geração++; `ResetBackoff`; pede token já |
| silêncio confirmado | `Discard`; trata como `Dropped(null)` |
| `Leave` | geração++; `Discard`; fase `Disconnected`; nenhum evento de reconexão depois |

---

## Fila (`Anathema.Net.Connection/Matchmaking`)

**`QueuePhase`**: `OutOfQueue`, `Connecting`, `Searching`, `Paired`.

**`JoinOutcome`**: `Started`, `AlreadyQueued`.

**`JoinQueueMessage : IOutgoingMessage`** — `type` `join_queue`, `DeckId Deck`,
escrito como `deck_id` inteiro.

**`MatchFoundFrame : ServerFrame`** — `TypeName = "match_found"`,
`MatchPairing Pairing`.

**`MatchmakingFailedFrame : ServerFrame`** — `TypeName = "matchmaking_failed"`,
`string Error`.

**`MatchPairing`** — `MatchId Match`, `PairedPlayer Self`, `PairedPlayer Opponent`.

**`PairedPlayer`** — `UserId User`, `string Nickname`, `string Icon`, `long Level`.

**`QueueRefusal`**
- `QueueRefusalKind Kind`: `DeckNotSpecified`, `DeckNotFound`, `InvalidDeck`,
  `MalformedMessage`, `UnknownMessageType`, `Unrecognized`.
- `string Code` — o texto recebido (sempre preenchido).
- `string Error` — só leitura humana.
- `DeckId? Deck` — em `DeckNotFound` (e `InvalidDeck` quando vier).
- `IReadOnlyList<DeckProblem> Problems` — em `InvalidDeck`, na ordem; vazia nos
  outros.

**`QueueRefusalReader`** — `QueueRefusal Read(MessageRefusedFrame frame)`; nunca lança.

**`ConnectionFrames`** — `static DiscriminatedUnion<ServerFrame> CreateUnion()`:
genéricos + `match_found` + `matchmaking_failed`.

**`MatchQueue`** (estado só na thread principal; `IDisposable`)
- Construtor: `(AuthenticatedConnection connection, ConnectionTarget target, IClientLog log)`.
- `QueuePhase Phase`, `DeckId? SearchDeck`.
- `JoinOutcome Join(DeckId deck)`, `void Leave()`.
- Eventos: `PhaseChanged(QueuePhase)`, `Paired(MatchPairing)`,
  `Refused(QueueRefusal)`, `MatchmakingFailed(string)`, `LeftQueue(GiveUpReason)`.

### Estados da fila

```text
OutOfQueue ──Join──► Connecting ──join_queue enviado──► Searching ──match_found──► Paired (socket fechado)
    ▲                    ▲                                  │
    │                    └──── queda / renovação / suspensão ┘ (reenvia ao reabrir)
    ├──── recusa / matchmaking_failed (socket aberto) ◄──────┤
    ├──── conexão desistiu (LeftQueue) ◄────────────────────┤
    └──── Leave (socket fechado) ◄──────────────────────────┘
```

Transições detalhadas em research R10.

---

## Borda Unity (`Anathema.Net.Unity`)

**`UnityFrameTicker : IFrameTicker`** — `void Raise()`.

**`DotNetWebSocketFactory : IWebSocketFactory`** —
`(MainThreadQueue, CleartextPolicy, IClientLog)`.

**`NetworkLayerHost`** — `Attach(queue, lifecycle, reachability, ticker)`;
`Update`: `Poll` → `Drain` → `Raise`.

**`LiveConnectionServices`** (`IDisposable`)
- `static FromAdapters(LiveNetworkAdapters adapters, IAppLifecycle lifecycle, INetworkReachability reachability, IFrameTicker ticker, IAccessTokenSource tokens, ConnectionRoutes routes)`.
- `AuthenticatedConnection MatchmakingConnection`, `MatchQueue Queue`,
  `AuthenticatedConnection MatchConnection`, `ConnectionRoutes Routes`.

**`ConnectionRoutes`** (`Anathema.Net.Connection`) — `Uri Matchmaking`, `Uri Match`;
`AppConfig.BuildConnectionRoutes()` monta a partir de `WsUrl(matchmakingConsumerUrl)`
e `WsUrl(matchConsumerUrl)`.

---

## Código anterior (`Assembly-CSharp`)

| Tipo | Forma depois |
|---|---|
| `BaseClient` | `abstract`; recebe `AuthenticatedConnection`; `Connect()`/`Disconnect()` delegam; eventos `OnReconnecting(int, double)`, `OnReconnected`, `OnGaveUp(string)` traduzidos de `StatusChanged`/`Recovered` |
| `MatchmakingClient : BaseClient` | recebe `MatchQueue` e `MatchClient`; `Join(DeckId)`, `Leave()`; `Paired` → `VersusContext`, `VersusScene`, `matchClient.Connect(pairing.Match)` |
| `MatchClient : BaseClient` | `Connect(MatchId)`; assina `RawTextReceived` só para `match_start` |
| `VersusContext` | `SetContext(MatchPairing)` preenche `Player`, `Opponent`, `MatchId` |
| `PlayerSession` | compõe e expõe `Account`, `Matchmaking`, `Match`, `Log`; sem `Token` |
| `ClientConnectionState` | removido (substituído por `ConnectionPhase`) |
