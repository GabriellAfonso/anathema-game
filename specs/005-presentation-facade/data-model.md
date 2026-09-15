# Data Model: Fachada da apresentação e prova final

**Feature**: `005-presentation-facade` | **Date**: 2026-09-14

Tipos em `Anathema.Net.Facade` salvo indicação. Todos imutáveis, exceto os que
publicam avisos. Nenhum campo se chama `id`.

---

## Estado do app

### `ClientStage` (enum público)

`SignedOut`, `SignedIn`, `Searching`, `Paired`, `InMatch`, `MatchFinished`,
`MatchUnavailable`.

### `ClientState` (público)

| Campo | Tipo | Presente em |
|---|---|---|
| `Stage` | `ClientStage` | sempre |
| `Self` | `UserId?` | todos menos `SignedOut` |
| `SignedOutReason` | `SignedOutReason?` | `SignedOut` |
| `Pairing` | `MatchPairing?` | `Paired`, `InMatch`, `MatchFinished`, `MatchUnavailable` |
| `Match` | `LiveMatch?` | `Paired` (depois de o catálogo carregar), `InMatch`, `MatchFinished`; em `MatchUnavailable` a sessão já foi descartada |
| `Result` | `MatchResult?` | `MatchFinished` |
| `Unavailable` | `MatchUnavailable?` | `MatchUnavailable` |

Fábricas internas por estágio; um campo fora do estágio é sempre nulo.

### `SignedOutReason` (enum público)

`Startup` (composição, sem sessão), `SignedOut` (pedido), `SessionExpired`
(renovação recusada, ou conexão desistiu por `SessionExpired`/`NoSession`).

### `ClientStageChange` (público)

`Previous : ClientState`, `Current : ClientState`.

### `StageRequestResult` (público)

`Applied : bool`, `Stage : ClientStage` (o estágio depois da chamada).

### Transições (dono: `ClientStages`, interno)

| De | Gatilho | Para | Efeito |
|---|---|---|---|
| `SignedOut` | `SignInAsync` → `SignedIn`; `ResumeAsync` → `Resumed` | `SignedIn` | `Self` do outcome |
| `SignedIn` | `Queue.Join` → `Started` | `Searching` | — |
| `Searching` | `Queue.Leave` | `SignedIn` | — |
| `Searching` | fila `Refused` | `SignedIn` | publica `Queue.Refused` |
| `Searching` | fila `LeftQueue` (motivo que não é de sessão) ou `MatchmakingFailed` | `SignedIn` | publica `Queue.Left` |
| `Searching` | fila `Paired` | `Paired` | publica `Queue.Paired`; `MatchOpening.Open` |
| `Paired` | `LiveMatch` → `Live` (primeira vez) | `InMatch` | — |
| `Paired`, `InMatch` | `LiveMatch` → `Finished` | `MatchFinished` | `HistoryRowLookup.Start` |
| `Paired` | catálogo falhou (não `SessionUnavailable`) | `MatchUnavailable` | `CatalogUnavailable` |
| `Paired`, `InMatch` | `LiveMatch` → `Refused` | `MatchUnavailable` | `MatchRefused` |
| `Paired`, `InMatch` | `LiveMatch` → `GaveUp` (motivo que não é de sessão) | `MatchUnavailable` | `ConnectionGaveUp` |
| `MatchUnavailable` | `RetryMatch()` | `Paired` | descarta a `LiveMatch` velha; `MatchOpening.Open` |
| `MatchFinished`, `MatchUnavailable` | `ReturnToLobby()` | `SignedIn` | descarta a `LiveMatch`; cancela lookup |
| qualquer um menos `SignedOut` | `Account.SignOut()` | `SignedOut(SignedOut)` | deixa fila, descarta partida, cancela; `AccountSession.SignOut` |
| qualquer um menos `SignedOut` | sessão expirada (R7) | `SignedOut(SessionExpired)` | idem, sem chamar `SignOut` da sessão |

Nada mais muda o estágio. Queda e volta da partida não aparecem aqui.

---

## Resultado e indisponibilidade

### `MatchResult` (público)

| Campo | Tipo | Nota |
|---|---|---|
| `Match` | `MatchId` | da `LiveMatch` |
| `Outcome` | `MatchOutcome` | do espelho |
| `Won` | `bool` | `Mirror.DidIWin` |
| `RowStatus` | `HistoryRowStatus` | `Fetching` → `Resolved` ou `Unavailable` |
| `Row` | `MatchHistoryRow?` | só em `Resolved` |
| `Attempts` | `int` | leituras feitas, 0 a 4 |

### `HistoryRowStatus` (enum público)

`Fetching`, `Resolved`, `Unavailable`.

### `MatchUnavailable` (público)

| Campo | Tipo | Nota |
|---|---|---|
| `Match` | `MatchId` | |
| `Kind` | `MatchUnavailableKind` | `CatalogUnavailable`, `MatchRefused`, `ConnectionGaveUp` |
| `GiveUp` | `GiveUpReason?` | em `MatchRefused` e `ConnectionGaveUp` |
| `CatalogFailure` | `AccountCallFailure?` | em `CatalogUnavailable`, quando foi falha de transporte |
| `PlayerText` | `string` | texto para o jogador (motivo da 003 ou "catálogo indisponível") |

### `HistoryRowLookup` (interno)

Estado: geração, `MatchId`, `DidIWin`, tentativas feitas, próximo instante
monotônico. Esperas `[1 s, 2 s, 4 s]`, máximo 4 leituras. Anda só no tique.

---

## Conta, fila e saúde

### `ClientAccount` (público)

| Membro | Tipo |
|---|---|
| `SessionState` | `AccountSessionState` |
| `Self` | `UserId?` |
| `RegisterAsync(RegistrationForm)` | `Task<AccountCallOutcome<AccountCreated, RegistrationRefusal>>` |
| `SignInAsync(string, Password)` | `Task<SignInOutcome>` (`AlreadySignedIn` fora de `SignedOut`) |
| `ResumeAsync()` | `Task<ResumeOutcome>` |
| `SignOut()` | `StageRequestResult` |
| `ReadProfileAsync()` | `Task<AccountCallOutcome<OwnProfile, ProfileRefusal>>` |

### `ClientQueue` (público)

| Membro | Tipo |
|---|---|
| `Phase` | `QueuePhase` |
| `Join(DeckId)` | `QueueJoinResult` |
| `Leave()` | `StageRequestResult` |
| `Paired` | `EventFeed<MatchPairing>` |
| `Refused` | `EventFeed<QueueRefusal>` |
| `Left` | `EventFeed<QueueExit>` |

- `QueueJoinResult`: `Kind` (`Started`, `AlreadyQueued`, `NotApplicable`) e `Stage`.
- `QueueExit`: `Kind` (`ConnectionGaveUp`, `MatchmakingFailed`), `GiveUp` (`GiveUpReason?`), `ErrorForLog` (`string`, só log).

### `ConnectionHealth` (público)

| Membro | Tipo |
|---|---|
| `LastLatency` | `TimeSpan?` (da conexão ativa) |
| `Reconnecting` | `EventFeed<ReconnectingNotice>` — `Attempt : int`, `Wait : TimeSpan` |
| `Recovered` | `EventFeed<RecoveredNotice>` — sem campo |
| `GaveUp` | `EventFeed<GaveUpNotice>` — `Reason : GiveUpReason`, `PlayerText : string` |
| `LatencyMeasured` | `EventFeed<TimeSpan>` |

Conexão ativa por estágio: `Searching` → fila; `Paired`, `InMatch`,
`MatchFinished`, `MatchUnavailable` → partida; os outros → nenhuma.
`HealthRelay` (interno) guarda a última tentativa para a suspensão sem rede,
como o `BaseClient` fazia.

---

## A fachada

### `AnathemaClient` (público, `IDisposable`)

| Membro | Tipo |
|---|---|
| `State` | `ClientState` |
| `StageChanged` | `EventFeed<ClientStageChange>` |
| `ResultUpdated` | `EventFeed<MatchResult>` |
| `Account` | `ClientAccount` |
| `Catalog` | `CardCatalog` |
| `Decks` | `PlayerDecks` |
| `History` | `MatchHistory` |
| `Queue` | `ClientQueue` |
| `CurrentMatch` | `LiveMatch?` (= `State.Match`) |
| `Health` | `ConnectionHealth` |
| `RetryMatch()` | `StageRequestResult` |
| `ReturnToLobby()` | `StageRequestResult` |
| `Dispose()` | fecha fila e partida, cancela, cala todos os feeds |

Construção interna, por `ClientPorts`: `IHttpTransport`, `IWebSocketFactory`,
`IMonotonicClock`, `IFrameTicker`, `IAppLifecycle`, `INetworkReachability`,
`MainThreadQueue`, `IClientLog`, `IProtocolCodec`, `IRefreshTokenVault`,
`AccountRoutes`, `ConnectionRoutes`, `AccountTiming`.

---

## Núcleo: `EventFeed<T>` (`Anathema.Net.Core`)

| Membro | Visibilidade | Garantia |
|---|---|---|
| `Subscribe(Action<T>)` → `IDisposable` | público | ouvinte nulo lança com o nome do feed |
| `Publish(T)` | interno | cópia dos ouvintes; confere descarte por ouvinte; exceção → `feed_listener_failed` com nome do feed e tipo da exceção |
| construtor `(string name, IClientLog log)` | interno | |

---

## Composição (`Anathema.Net.Unity`)

- **`ServerRoutes`** (público): `RegisterUrl`, `LoginUrl`, `RefreshUrl`, `ProfileUrl`, `CardsUrl`, `DecksUrl`, `MatchesUrl`, `MatchmakingUrl`, `MatchUrl` (`Uri`). Nenhum nulo.
- **`ClientCompositionOptions`** (público): `Routes`, `VaultSlot : RefreshTokenVaultSlot?`, `Log : IClientLog?`, `AllowCleartext : bool`, `AllowClockJumps : bool`.
- **`ComposedClient`** (público, `IDisposable`): `Client`, `Pump()`, `AttachTo(GameObject)`, `DropSockets()`, `JumpClock(TimeSpan)`.
- **`SteppableMonotonicClock`** (interno): relógio de dentro e deslocamento acumulado; o salto só avança.
- **`DroppableWebSocketFactory`** (interno): fábrica de dentro e sockets vivos; `DropAll()` aborta e esquece.

---

## Borda de cena (`Anathema.Client.Scenes`)

- **`ISceneClientConsumer`**: `void BindClient(AnathemaClient client)`.
- **`SceneRoute`**: `static string? For(ClientStage stage)`.
- **`SceneRouter`**: fachada, assinatura de `StageChanged`, carregador de cena.
- **`SceneClientBinder`**: fachada; liga consumidores por cena carregada, uma vez por componente.
- **`SceneSubscriptions`**: `Add(IDisposable)`, `DisposeAll()`.
- **`ClientHost`**: `configDev`, `configProd`, `isProd`, `ComposedClient`, `SceneRouter`, `SceneClientBinder`.

---

## Prova (`Anathema.Client.Proof`)

- **`ProofSetup`**: rotas, tempo limite (15 min), rodada da queda (3), rodada mínima da vez parada (2), salto (5 min 30 s), sufixo de nomes.
- **`ProofPlayer`**: rótulo, `ComposedClient`, `ProofLog`, `ProofBot`, slot.
- **`ProofBot`**: envios por tipo, recusas, falha, turnos em que viu "tempo acabando".
- **`ProofStrategy`**: o estado da `SmokeStrategy` (tentadas, mulligan enviado, puxou uma vez), mais o que falta cobrir, a flag de remover uma vez e a vez a deixar estourar.
- **`CommandCoverage`**: os 11 itens (mulligan trocando, mulligan sem trocar, jogar unidade, feitiço com alvo, feitiço sem alvo, passar, declarar ataque, puxar atacante, confirmar ataque, atribuir bloqueador, remover bloqueador, encerrar defesa); `Missing`.
- **`ProofLog`** (`IClientLog`): entradas em ordem, com marca de instante; repassa ao console.
- **`ProofStep`**: nome, `Passed`, detalhe. **`ProofFailure`**: exceção com passo, esperado e recebido.
