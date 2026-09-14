# Implementation Plan: Socket autenticado e fila

**Branch**: nenhum (spec no `main`) | **Date**: 2026-09-14 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/003-authenticated-socket-queue/spec.md`

## Summary

Esta feature constrói, sobre as portas da 001 e a conta da 002:

- **Conexão autenticada** (`AuthenticatedConnection`, assembly nova
  `Anathema.Net.Connection`):
  - token pedido a `IAccessTokenSource` antes de cada abertura;
  - renovação em `auth_denied`/4001 e desistência em `match_denied`/44xx;
  - backoff com o `ReconnectPolicy` existente, zerado só na prova de sessão
    (inclusive `pong`);
  - heartbeat existente com latência, pausa perdoada e confirmação de silêncio
    atrás da fila;
  - suspensão em segundo plano e sem rede, reciclagem na troca de rede;
  - tudo movido pelo `IFrameTicker` e pelo relógio monotônico.
- **Fila** (`MatchQueue`) sobre a conexão: `join_queue`, `match_found` tipado,
  recusas por `code` (com `DeckProblem` da 002), reentrada automática ao cair
  procurando.
- **Código antigo evoluindo no lugar**:
  - `BaseClient` vira adaptador da conexão;
  - `MatchmakingClient` usa a fila e `MatchClient` usa a conexão de partida;
  - `PlayButton` entra com o primeiro deck;
  - `PlayerSession` compõe tudo e `ReconnectOverlay` segue igual;
  - saem NativeWebSocket, `WebSocketDispatcher`, `ConnectionClient`,
    `TokenRefreshService`, `PlayerSession.Token` e `NetworkBootstrap`.
- **Windows**: Run In Background ligado e minimizar deixa de ser segundo plano.

Contratos do protocolo, citados e não copiados:
`C:/Users/gabri/Projetos/dev_container/anathema/backend/specs/011-deck-catalog-api/contracts/matchmaking_messages.md`,
`.../013-socket-heartbeat/contracts/heartbeat_messages.md`,
`.../009-match-protocol/contracts/server_frames.md` e `refusal_codes.md`.

Achados do research que mudam ou completam a spec:

- **Duas limitações do backend confirmadas** (research R11): o `match_found` se
  perde se o socket cai entre o pareamento e a entrega, e o `leave` por `user_id`
  do socket antigo pode tirar da fila a entrada do socket novo. As duas viram
  pedido ao backend, sem contorno no cliente.
- **`NetworkBootstrap..cs` tem nome de arquivo que não bate com a classe**
  (research R13). A composição dos clientes passa ao `PlayerSession`.
- **`AccessToken` tem construtor interno**. O fake de `IAccessTokenSource` exige
  `InternalsVisibleTo("Anathema.Net.Fakes")` (research R5).
- **Frame chegando entre `Drain` e tick** derrubaria conexão boa. A morte por
  silêncio passa por uma confirmação enfileirada (research R3).

## Technical Context

**Language/Version**: C# 9 (Unity 6000.2.8f1), `#nullable enable` em todo arquivo novo; .NET Standard 2.1 (`apiCompatibilityLevel: 6`)

**Primary Dependencies**:
- da 001: `IWebSocket`, `SocketClosure`, `MainThreadQueue`, `IProtocolCodec`, `DiscriminatedUnion`, `IMonotonicClock`, `IAppLifecycle`, `INetworkReachability`, `IClientLog` e os fakes;
- da 002: `IAccessTokenSource`, `PlayerDecks`, `DeckId`, `DeckProblemUnion`;
- `System.Net.WebSockets.ClientWebSocket`, só pelo `DotNetWebSocket` existente;
- Unity Test Framework 1.6.0.

Nenhum pacote novo; um pacote sai (NativeWebSocket, embutido em `Assets/WebSocket`).

**Storage**: N/A (nenhum dado persistido; fila e conexão só em memória)

**Testing**: EditMode pelo comando único da constituição, com `FakeWebSocketFactory`, `FakeAccessTokenSource`, `FakeFrameTicker`, `FakeMonotonicClock`, `FakeAppLifecycle`, `FakeNetworkReachability` e `MainThreadQueue` drenada pelo teste; `[Explicit]` + `[Category("LiveServer")]` contra o backend local

**Target Platform**: Windows desktop (player e editor, Multiplayer Play Mode) e Android (IL2CPP, ARM64)

**Project Type**: biblioteca interna do cliente Unity (camada de rede), consumida pelo código de cena antigo agora e pelas features 4 e 5

**Performance Goals**:
- testes EditMode da feature < 5 s (SC-012);
- de volta à fila em ≤ 10 s depois de Wi-Fi → dados ou da volta do segundo plano (SC-009, SC-010);
- percepção de troca de rede ≤ 1 s (polling existente).

**Constraints**:
- `Anathema.Net.Connection` sem UnityEngine, Newtonsoft ou `System.Net.WebSockets` (SC-003);
- token nunca em log (FR-016);
- hora do sistema nunca lida;
- nenhuma espera real nos testes;
- nenhum aviso de compilação novo;
- sem contorno no cliente para limitação do backend.

**Scale/Scope**:
- assemblies: 1 nova de produção (`Anathema.Net.Connection`) e 1 nova de teste; 2 removidas (`Anathema.Reconnect`, `Anathema.Reconnect.Tests`); 6 existentes evoluem (`Core`, `Unity`, `Account`, `Config`, `Fakes`, `Net.Editor.Tests`);
- ~45 tipos pequenos;
- ~9 arquivos antigos evoluem e ~12 saem;
- 6 casos LiveServer.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Princípio / regra | Como o plano cumpre | Estado |
|---|---|---|
| I. Contratos são a fonte | Fila, heartbeat e gates seguem os contratos 011, 013 e 009 citados por caminho. Limitações do backend viram pedido, sem contorno (research R11) | ✅ |
| II. Servidor é a autoridade | Deck não validado; "procurando" é só "mandou sem recusa", como o contrato define. Recusa por `code`. Desistência só em gate do servidor ou limite local de reconexão, que é conveniência de rede e não regra de jogo | ✅ |
| III. Identidade tipada | `DeckId` no `join_queue`; `MatchId`, `UserId` no `match_found`; `ConnectionTarget.Match(Uri, MatchId)`; nenhum membro `id` | ✅ |
| IV. Tipos explícitos | `match_found`, `matchmaking_failed`, `match_denied` na união de frames; `QueueRefusalKind` e `GiveUpKind` fechados, com braço desconhecido; `SocketEnd` fechado; nada de `JObject` fora do codec | ✅ (ver Complexity Tracking: `RawTextReceived`) |
| V. Unidades pequenas | A conexão é dividida em `SocketAttempt`, `SilenceWatch`, `ConnectionSuspension`, `SocketEndClassifier`, `PingLedger` e `AuthenticatedConnection`, que só orquestra; fila com leitor de recusa separado. Nomes novos sem colisão no projeto (conferidos: 0 ocorrências) | ✅ |
| VI. Núcleo independente | `Anathema.Net.Connection` com `noEngineReferences`, só `Core` e `Account`. Ticks, fábrica de socket, ciclo de vida e rede por interface. Troca de thread continua só na `MainThreadQueue`. Clientes de socket sem singleton para token, conexão ou despacho | ✅ (ver Complexity Tracking: `PlayerSession.Instance`) |
| VII. Fakes nomeados | `FakeAccessTokenSource`, `SpoiledFirstTokenSource`, `FakeWebSocketFactory`, `FakeFrameTicker` novos; tempo só pelo `FakeMonotonicClock`; `ReconnectPolicyTests` e `HeartbeatTests` preservados; LiveServer marcado | ✅ |
| Plataformas | Só Windows e Android. Modo de ciclo de vida escolhido por `RuntimePlatform.Android` e `Application.runInBackground`, sem `#if` novo | ✅ |
| Voltar ao primeiro plano zera a detecção de silêncio | `ForgivePause` na volta e em salto de tick (FR-020, research R3) | ✅ |
| Reconexão sozinha no Android, renovando token | Volta → reabre com `GetValidAsync`, que renova na margem (research R9) | ✅ |
| JSON pelo codec | Frames novos na união; `JsonUtility` e `ErrorPayloadDTO` saem dos clientes. `JsonConvert` fica só na ponte de `match_start` | ✅ (ver Complexity Tracking) |
| Log por interface | Conexão, fila e clientes antigos por `IClientLog`; `Debug.Log*` sai de `BaseClient` e `MatchmakingClient` | ✅ |
| Código anterior evolui no lugar | `BaseClient`, `MatchClient`, `MatchmakingClient`, `PlayerSession`, `ReconnectPolicy`, `Heartbeat` evoluem ou são movidos com os testes; nada paralelo (tabela abaixo) | ✅ |

**Pós-design (Phase 1)**: reavaliado depois de `data-model.md` e `contracts/`.
Nenhuma violação nova além das de Complexity Tracking.

- O `InternalsVisibleTo` para os fakes não abre o construtor de `AccessToken`
  ao código de produção: a assembly de fakes é só de editor e só com
  `UNITY_INCLUDE_TESTS`.
- Os clientes antigos em `Assembly-CSharp` ficam sem teste próprio, porque
  `Assembly-CSharp` não tem assembly de teste. Eles só traduzem eventos; todo
  comportamento está na conexão e na fila, que têm teste.

### Código anterior tocado

| Arquivo | Mudança | Por quê | Fica para depois |
|---|---|---|---|
| `Assets/Scripts/Core/Network/WebSocketClient/Base.cs` | Adaptador fino sobre `AuthenticatedConnection`; `ClientConnectionState`, NativeWebSocket, Newtonsoft, `JsonUtility`, `Debug`, dispatcher, ponte de token saem; eventos do overlay traduzidos | FR-038, FR-045, FR-046 | some quando a feature 5 der a fachada à apresentação |
| `Assets/Scripts/Core/Network/WebSocketClient/MatchmakingClient.cs` | Sobre `MatchQueue`; `Join(DeckId)`/`Leave()`; recebe `MatchClient` pelo construtor; recusas e falhas pelo log | FR-040, FR-041 | reação visual a recusa e `LeftQueue` (UI) |
| `Assets/Scripts/Core/Network/WebSocketClient/MatchClient.cs` | `Connect(MatchId)`; `match_denied` pela conexão; `match_start` pela ponte `RawTextReceived` | FR-039 | `match_start` tipado e espelho (feature 4) |
| `Assets/Scripts/Core/Session/PlayerSession.cs` | `ComposeNetwork`; expõe `Matchmaking` e `Match`; liga o overlay; sai `Token` | FR-044, FR-045 | `Instance` sai na feature 5 |
| `Assets/Scripts/UI/PlayButton.cs` (`MyButtonScript`) | Lista decks e entra com o primeiro | FR-040 | tela de escolha de deck |
| `Assets/Scripts/Core/Contexts/VersusContext.cs` | `SetContext(MatchPairing)`; sem `JsonConvert`/`Debug` | FR-030, FR-041 | feature 5 |
| `Assets/Scripts/Core/Network/Reconnect/ReconnectPolicy.cs`, `Heartbeat.cs` | Movidos para `Assets/Scripts/Net/Connection/Connection/`; 1000 reconectável; `ForgivePause`, `NotePingSent`; comentários preservados | FR-043, research R7, R8 | — |
| `Assets/Tests/EditMode/ReconnectPolicyTests.cs`, `HeartbeatTests.cs` | Movidos para `Assets/Tests/EditMode/Net.Connection/`; mudam só em 1000 e nos métodos novos | FR-043 | — |
| `Assets/Scripts/Core/Config/AppConfig.cs`, `Anathema.Config.asmdef` | `BuildConnectionRoutes()`; referência a `Anathema.Net.Connection` | data-model | — |
| `Assets/Scenes/BootstrapScene.unity` | Sai o componente `NetworkBootstrap` | research R13 | — |
| `ProjectSettings/ProjectSettings.asset` | `runInBackground: 0 → 1` | FR-026 | — |
| Portas e borda da 001/002 (`LifecycleSignalFilter`, `GenericServerFrames`, `NetworkLayerHost`, `UnityAppLifecycle`, `LiveNetworkAdapters`, `AccountAssemblyInfo`, asmdefs de `Unity` e `Fakes`) | Ver [contracts/legacy-bridge.md](./contracts/legacy-bridge.md) | research R1–R9 | — |

Removidos: tabela em [contracts/legacy-bridge.md](./contracts/legacy-bridge.md#removidos-com-meta).

Itens do `Game/TODO.md` (vault), escritos na implementação: lista em
[contracts/legacy-bridge.md](./contracts/legacy-bridge.md#gametodomd-vault).

`MatchSession`, `MiniPlayerProfile`, `LoginController`, `SelfProfileService`,
`VersusController` e `AppEnvManager` não mudam.

## Project Structure

### Documentation (this feature)

```text
specs/003-authenticated-socket-queue/
├── plan.md              # este arquivo
├── research.md          # Phase 0
├── data-model.md        # Phase 1
├── quickstart.md        # Phase 1
├── contracts/
│   ├── authenticated-connection.md   # portas novas, conexão, heartbeat, suspensão, fakes
│   ├── matchmaking-queue.md          # fila, frames de fila, recusas, LiveServer
│   └── legacy-bridge.md              # clientes antigos, composição, remoções, TODO
├── checklists/
│   └── requirements.md
└── tasks.md             # Phase 2 (/speckit-tasks)
```

### Source Code (repository root)

```text
Assets/Scripts/Net/
├── Core/                                        # Anathema.Net.Core (evolui)
│   ├── Time/          + IFrameTicker
│   ├── Socket/        + IWebSocketFactory
│   ├── Lifecycle/     + BackgroundSignalMode; LifecycleSignalFilter evolui
│   └── Protocol/Frames/ + MatchDeniedFrame; GenericServerFrames evolui
├── Connection/                                  # Anathema.Net.Connection (nova; noEngineReferences; Core, Account)
│   ├── Anathema.Net.Connection.asmdef
│   ├── ConnectionRoutes.cs
│   ├── Connection/    AuthenticatedConnection, ConnectionPorts, ConnectionSettings, ConnectionTiming,
│   │                  ConnectionTarget, ConnectionPhase, ConnectionStatus, SuspensionReason,
│   │                  GiveUpReason, GiveUpKind, MatchRefusalDetail, SocketEnd, SocketEndKind,
│   │                  SocketEndClassifier, SocketAttempt, SilenceWatch, PingLedger,
│   │                  ConnectionSuspension, ResumeCause,
│   │                  ReconnectPolicy, ReconnectPlan, ReconnectAction, Heartbeat, HeartbeatAction (movidos)
│   └── Matchmaking/   MatchQueue, QueuePhase, JoinOutcome, JoinQueueMessage, MatchFoundFrame,
│                      MatchmakingFailedFrame, MatchPairing, PairedPlayer, QueueRefusal,
│                      QueueRefusalKind, QueueRefusalReader, ConnectionFrames
├── Unity/                                       # Anathema.Net.Unity (evolui; + Connection)
│   ├── UnityFrameTicker.cs, DotNetWebSocketFactory.cs, LiveConnectionServices.cs
│   └── NetworkLayerHost.cs, UnityAppLifecycle.cs, LiveNetworkAdapters.cs (evoluem)
└── Account/AccountAssemblyInfo.cs               # + InternalsVisibleTo("Anathema.Net.Fakes")

Assets/Scripts/Core/Network/WebSocketClient/  Base.cs, MatchClient.cs, MatchmakingClient.cs  # evoluem
Assets/Scripts/Core/Session/PlayerSession.cs                                               # evolui
Assets/Scripts/Core/Contexts/VersusContext.cs, Assets/Scripts/UI/PlayButton.cs              # evoluem
Assets/Scripts/Core/Config/AppConfig.cs, Anathema.Config.asmdef                             # evoluem

Assets/Tests/EditMode/
├── Fakes/            + FakeAccessTokenSource, HeldRenewal, SpoiledFirstTokenSource,
│                       FakeWebSocketFactory, FakeFrameTicker; asmdef + Account
├── Net.Core/         Lifecycle/LifecycleSignalFilterTests evolui
├── Net.Json/         GenericServerFramesTests evolui (match_denied)
├── Net.Connection/   Anathema.Net.Connection.Tests.asmdef (Core, Json, Account, Connection, Fakes)
│                     ConnectionTestRig, ReconnectPolicyTests, HeartbeatTests (movidos),
│                     SocketEndClassifierTests, PingLedgerTests, ConnectionTargetTests,
│                     ConnectionTimingTests, ConnectionSettingsTests, GiveUpReasonTests,
│                     ConnectionTokenTests, ConnectionGateTests, ConnectionDropTests,
│                     ConnectionLeaveTests, ConnectionFrameDeliveryTests, ConnectionSilenceTests,
│                     ConnectionSuspensionTests, ConnectionAssemblyBoundaryTests,
│                     Matchmaking/ JoinQueueMessageTests, MatchFoundFrameTests,
│                     MatchmakingFailedFrameTests, QueueRefusalReaderTests, MatchQueueJoinTests,
│                     MatchQueueRefusalTests, MatchQueueReconnectTests, MatchQueueLeaveTests
├── Net.Unity/        + UnityFrameTickerTests, DotNetWebSocketFactoryTests, LiveConnectionServicesTests,
│                     LocalConnectionRoutes, LiveServer/LiveQueueTests, TextDeckJoinMessage;
│                     UnityAppLifecycleTests evolui
├── Net.Editor/       + RunInBackgroundSettingTests
└── Config/           AppConfigTests evolui (BuildConnectionRoutes)
```

**Structure Decision**: a nova `Anathema.Net.Connection` fica ao lado das
assemblies da 001 e da 002 em `Assets/Scripts/Net/`, entre a conta e a borda. As
portas genéricas (`IFrameTicker`, `IWebSocketFactory`, `match_denied`) ficam no
núcleo. O código de cena antigo continua em `Assembly-CSharp`, nos mesmos
arquivos. `Anathema.Reconnect` deixa de existir: o conteúdo dela vai para a
assembly nova, com os testes.

Dependências, só no sentido borda → núcleo:

```text
Assembly-CSharp (PlayerSession, clientes, PlayButton…) ─► Anathema.Config ─► Anathema.Net.Unity ─► Anathema.Net.Connection ─► Anathema.Net.Account ─► Anathema.Net.Core
                                                                            └──────────────────► Anathema.Net.Json ─────────────────────────► Anathema.Net.Core
Anathema.Net.Fakes ─► Anathema.Net.Account, Anathema.Net.Core
Anathema.Net.Connection.Tests ─► Connection, Account, Json, Core, Fakes
```

## Complexity Tracking

| Violation | Why Needed | Simpler Alternative Rejected Because |
|---|---|---|
| `PlayerSession.Instance` continua singleton e raiz de composição; `PlayButton` o lê | A descrição permite até a feature 5 (FR-045). As cenas não têm outro ponto de entrada, e a Home precisa achar a fila | Um hospedeiro sem `Instance` exige a fachada da feature 5. Os clientes de socket não leem o singleton: tudo entra pelo construtor. Registrado no `Game/TODO.md` |
| `AuthenticatedConnection.RawTextReceived` e `JsonConvert` no `MatchClient` para `match_start` | O tratamento de `match_start` fica como está até a feature 4 (FR-039), e o codec não devolve JSON cru | Tipar `match_start` agora é escopo da feature 4. `IPayloadReader.RawJson()` poria JSON no contrato do núcleo para sempre. `[Obsolete]` gera aviso (research R12) |
| `[assembly: InternalsVisibleTo("Anathema.Net.Fakes")]` na conta | O fake de `IAccessTokenSource` pedido na descrição precisa construir `AccessToken`, cujo construtor é interno | Construtor público deixaria código de produção criar token sem leitor. Porta paralela só de texto duplicaria `IAccessTokenSource` (research R5) |
| `VersusContext.Instance`, `MatchSession.Instance` e `SceneManager` dentro de `MatchmakingClient`/`MatchClient` | O fluxo `match_found` → `VersusScene` → `MatchClient` e o `match_start` precisam continuar como hoje (FR-039, FR-041) | Tirar os singletons de cena é a fachada da feature 5. São da apresentação, não de token, conexão ou despacho (FR-045) |
| Clientes antigos em `Assembly-CSharp` sem teste próprio | `Assembly-CSharp` não tem assembly de teste, e criar uma para ele puxaria todas as cenas | Os clientes só traduzem eventos. Conexão e fila, onde está o comportamento, têm teste; o fluxo inteiro é provado pelo quickstart §3 e pelo LiveServer |
