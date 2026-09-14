---

description: "Task list for 003-authenticated-socket-queue"
---

# Tasks: Socket autenticado e fila

**Input**: Design documents from `specs/003-authenticated-socket-queue/`

**Prerequisites**: [plan.md](./plan.md), [spec.md](./spec.md), [research.md](./research.md), [data-model.md](./data-model.md), [contracts/](./contracts/), [quickstart.md](./quickstart.md)

**Tests**: obrigatórios (constituição, princípio VII). Todo método novo ganha
teste EditMode. Escreva o teste antes da implementação: em Unity, "falhar" pode
significar não compilar porque o tipo ainda não existe.

**Organization**: tarefas agrupadas pelas histórias da spec (US1 a US5).

## Format: `[ID] [P?] [Story] Description`

- **[P]**: pode rodar em paralelo com as outras [P] do mesmo bloco (arquivos
  diferentes, sem depender de tarefa incompleta do bloco)
- **[Story]**: US1 a US5 (histórias da spec)

## Convenções para todas as tarefas

Leia antes de executar qualquer tarefa:

- **Antes de escrever código**:
  - leia `CLAUDE.md`, `.specify/memory/constitution.md` e o contrato citado na
    tarefa (`specs/003-authenticated-socket-queue/contracts/`);
  - mensagens do servidor: nunca copie formas para código nem comentário; cite o
    caminho em `C:/Users/gabri/Projetos/dev_container/anathema/backend/specs/`
    (`011-deck-catalog-api/contracts/matchmaking_messages.md`,
    `013-socket-heartbeat/contracts/heartbeat_messages.md`,
    `009-match-protocol/contracts/server_frames.md`, `refusal_codes.md`).
- **Namespaces**: um por assembly, igual ao nome da asmdef
  (`Anathema.Net.Core`, `Anathema.Net.Account`, `Anathema.Net.Connection`,
  `Anathema.Net.Unity`, `Anathema.Config`, `Anathema.Net.Fakes`).
  - Testes: namespace testado + `.Tests`.
  - Subpastas **não** criam sub-namespace.
  - Código antigo (`BaseClient`, `PlayerSession`, `VersusContext`…) continua no
    namespace global.
- **Todo arquivo novo ou movido**:
  - começa com `#nullable enable`;
  - tem um tipo público por arquivo;
  - métodos de 4 a 20 linhas, com no máximo 2 níveis de indentação;
  - `var` só quando o tipo aparece do lado direito.
- **Documentação e erros**:
  - membro público leva `/// <summary>` com a intenção e um `<example>`;
  - mensagem de exceção inclui o valor recebido e a forma esperada;
  - comentários existentes em arquivo movido ou reescrito são preservados.
- **Estado e assincronia** (research R2, R4):
  - conexão e fila só na thread principal; sem trava, sem `Task.Run`, sem
    `async void`, sem `Task.Delay`, sem timer;
  - espera = instante no `IMonotonicClock`, avaliado a cada `IFrameTicker.Ticked`;
  - `await` de token na conexão **sem** `ConfigureAwait(false)`; resultado de
    geração antiga é descartado.
- **Token**: só sai por `AccessToken.RevealForRequest()`, e só dentro de
  `ConnectionTarget.WithToken`. Nunca em `LogField`; URL registrada sem query
  (FR-016).
- **Eventos de log**: nomes e campos da tabela em
  `contracts/authenticated-connection.md` ("Eventos de log"). Não invente outros
  sem acrescentar lá.
- **Testes**:
  - NUnit, em `Assets/Tests/EditMode/<pasta da asmdef>/`;
  - nomes de método em português, no estilo de `ReconnectPolicyTests`;
  - assíncronos como `[Test] public async Task`;
  - fakes de `Anathema.Net.Fakes`, codec real
    (`NewtonsoftProtocolCodec(ConnectionFrames.CreateUnion(), log)`);
  - fakes criados no `[SetUp]`, nunca em campo inicializado (o NUnit reaproveita
    a instância do fixture; registro da 002);
  - tempo só pelo `FakeMonotonicClock` + `FakeFrameTicker.Tick()`; entrega
    "atrás da fila" por `MainThreadQueue.Enqueue` + `Drain()`; nunca
    `Thread.Sleep` (exceto LiveServer).
- **`.meta`**: o Unity gera ao abrir o projeto; todo arquivo novo em `Assets/` vai
  para o commit com o seu `.meta`. Arquivo movido leva o `.meta` junto (`git mv`
  dos dois, para manter o GUID). Arquivo removido sai com o `.meta`.
- **Comando da suíte** (editor fechado), chamado abaixo de "rodar a suíte":
  `"C:/Program Files/Unity/Hub/Editor/6000.2.8f1/Editor/Unity.exe" -batchmode -projectPath . -runTests -testPlatform EditMode -testResults Logs/editmode-results.xml`
  Se o `Unity.exe` não abrir a partir da sessão, compile com o Roslyn do Unity e
  rode NUnit no Mono como na 001 e na 002 (registros em
  `specs/001-server-connection/tasks.md` e `specs/002-player-account/tasks.md`);
  o mantenedor roda a suíte real.

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: assemblies e referências que tudo o resto usa.

- [X] T001 Criar `Assets/Scripts/Net/Connection/Anathema.Net.Connection.asmdef` (`rootNamespace` `Anathema.Net.Connection`, referências `Anathema.Net.Core` e `Anathema.Net.Account`, `noEngineReferences: true`, `autoReferenced: true`) e `Assets/Scripts/Net/Connection/ConnectionAssemblyInfo.cs` com `[assembly: InternalsVisibleTo("Anathema.Net.Connection.Tests")]`
- [X] T002 [P] Criar `Assets/Tests/EditMode/Net.Connection/Anathema.Net.Connection.Tests.asmdef` no molde de `Assets/Tests/EditMode/Net.Account/Anathema.Net.Account.Tests.asmdef`, com referências `UnityEngine.TestRunner`, `UnityEditor.TestRunner`, `Anathema.Net.Core`, `Anathema.Net.Json`, `Anathema.Net.Account`, `Anathema.Net.Connection`, `Anathema.Net.Fakes`
- [X] T003 [P] Acrescentar `Anathema.Net.Account` às referências de `Assets/Tests/EditMode/Fakes/Anathema.Net.Fakes.asmdef` e `[assembly: InternalsVisibleTo("Anathema.Net.Fakes")]` em `Assets/Scripts/Net/Account/AccountAssemblyInfo.cs`, com comentário citando `specs/003-authenticated-socket-queue/research.md` R5 (research R5)
- [X] T004 [P] Acrescentar `Anathema.Net.Connection` às referências de `Assets/Scripts/Net/Unity/Anathema.Net.Unity.asmdef` e `Assets/Scripts/Core/Config/Anathema.Config.asmdef`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: portas, fakes, código movido e rig de teste que todas as histórias usam.

**⚠️ CRITICAL**: nenhuma história começa antes desta fase.

### Código movido (sem mudar comportamento)

- [X] T005 Mover `Assets/Scripts/Core/Network/Reconnect/ReconnectPolicy.cs` e `Heartbeat.cs` (com `.meta`) para `Assets/Scripts/Net/Connection/Connection/`, com `#nullable enable` e namespace `Anathema.Net.Connection`; separar um tipo público por arquivo em `ReconnectPolicy.cs`, `ReconnectPlan.cs`, `ReconnectAction.cs`, `Heartbeat.cs`, `HeartbeatAction.cs`; `Func<double>? random`; preservar todos os comentários; apagar `Assets/Scripts/Core/Network/Reconnect/Anathema.Reconnect.asmdef` e a pasta vazia (FR-043)
- [X] T006 Mover `Assets/Tests/EditMode/ReconnectPolicyTests.cs` e `HeartbeatTests.cs` (com `.meta`) para `Assets/Tests/EditMode/Net.Connection/`, com namespace `Anathema.Net.Connection.Tests` e `#nullable enable`, sem mudar nenhuma asserção; apagar `Assets/Tests/EditMode/Anathema.Reconnect.Tests.asmdef`; rodar a suíte e confirmar os 24 testes passando no novo lugar (depende de T002, T005)

### Portas e frame genérico

- [X] T007 [P] Escrever `Assets/Tests/EditMode/Net.Core/Fakes/FakeFrameTickerTests.cs` (cada `Tick()` dispara `Ticked` uma vez; sem assinante não lança) e criar `Assets/Scripts/Net/Core/Time/IFrameTicker.cs` e `Assets/Tests/EditMode/Fakes/FakeFrameTicker.cs` (contracts/authenticated-connection.md, "Portas novas")
- [X] T008 [P] Escrever `Assets/Tests/EditMode/Net.Core/Fakes/FakeWebSocketFactoryTests.cs` (cada `Create()` devolve `FakeWebSocket` novo e ocioso; `Created` na ordem; `Latest` sem criação lança com mensagem) e criar `Assets/Scripts/Net/Core/Socket/IWebSocketFactory.cs` e `Assets/Tests/EditMode/Fakes/FakeWebSocketFactory.cs`
- [X] T009 [P] Acrescentar a `Assets/Tests/EditMode/Net.Json/GenericServerFramesTests.cs` os casos de `match_denied` (com `error`; sem `error` → decodificação inválida com o campo) e criar `Assets/Scripts/Net/Core/Protocol/Frames/MatchDeniedFrame.cs`, registrado em `Assets/Scripts/Net/Core/Protocol/Frames/GenericServerFrames.cs` (research R1)

### Fake de token e rig

- [X] T010 [P] Escrever `Assets/Tests/EditMode/Net.Connection/Fakes/FakeAccessTokenSourceTests.cs` (roteiro em fila para `GetValidAsync` e `RenewNowAsync`; sem roteiro, `GetValidAsync` repete o último válido e `RenewNowAsync` lança dizendo o que roteirizar; `HoldNextRenewal` só completa no `Release`; contagens `ValidRequests`/`RenewRequests`) e criar `Assets/Tests/EditMode/Fakes/FakeAccessTokenSource.cs` e `Assets/Tests/EditMode/Fakes/HeldRenewal.cs`, criando `AccessToken` pelo construtor interno com `UserId(1)`, vida de 5 min e o instante do `FakeMonotonicClock` recebido no construtor (depende de T002, T003; contracts/authenticated-connection.md, "Fakes")
- [X] T011 [P] Escrever `Assets/Tests/EditMode/Net.Connection/ConnectionTimingTests.cs` (padrões 10 s/30 s/5 s; `SilenceLimit <= PingInterval`, `PauseThreshold >= PingInterval` ou valor ≤ 0 lançam com os valores) e criar `Assets/Scripts/Net/Connection/Connection/ConnectionTiming.cs` (research R8)
- [X] T012 [P] Escrever `Assets/Tests/EditMode/Net.Connection/ConnectionAssemblyBoundaryTests.cs` no molde de `Assets/Tests/EditMode/Net.Account/AccountAssemblyBoundaryTests.cs`, com `AssemblySignatureScanner`: sem referências começando com `UnityEngine`, `UnityEditor`, `Newtonsoft`; nenhum tipo usa `System.Net.WebSockets`, `UnityEngine`, `Newtonsoft.Json` (FR-047, SC-003)
- [X] T013 Criar `Assets/Scripts/Net/Connection/Matchmaking/ConnectionFrames.cs` com `CreateUnion()` devolvendo, por enquanto, `GenericServerFrames.CreateUnion()` (os braços de fila entram em T047), e teste `Assets/Tests/EditMode/Net.Connection/Matchmaking/ConnectionFramesTests.cs` decodificando `pong`, `auth_denied`, `match_denied` e `message_refused` (depende de T009)
- [X] T014 Criar `Assets/Scripts/Net/Connection/Connection/ConnectionPorts.cs` (agrupa `IWebSocketFactory`, `IMonotonicClock`, `IFrameTicker`, `IAppLifecycle`, `INetworkReachability`, `MainThreadQueue`, `IClientLog`; nulo lança nomeando o campo) com `Assets/Tests/EditMode/Net.Connection/ConnectionPortsTests.cs`, e `Assets/Tests/EditMode/Net.Connection/ConnectionTestRig.cs` montando `FakeWebSocketFactory`, `FakeMonotonicClock`, `FakeFrameTicker`, `FakeAppLifecycle`, `FakeNetworkReachability(LocalArea)`, `MainThreadQueue`, `FakeClientLog`, `FakeAccessTokenSource` e o codec de T013, com atalhos `Advance(TimeSpan)` (avança relógio + `Tick()`), `OpenLatest()`, `Receive(string frameJson)` e `Drain()` (depende de T007, T008, T010, T013)

**Checkpoint**: suíte verde, `Anathema.Reconnect` não existe mais, rig pronto.

---

## Phase 3: User Story 1 - Conexão que se recupera sozinha e sabe quando desistir (Priority: P1) 🎯 MVP

**Goal**: `AuthenticatedConnection` abre com token, renova em `auth_denied`/4001, desiste em `match_denied`/44xx ou sessão expirada, reconecta com backoff em qualquer outra queda, zera só na prova e entrega frames em ordem.

**Independent Test**: roteiros EditMode com `FakeWebSocketFactory`, `FakeAccessTokenSource`, `FakeMonotonicClock` e jitter determinístico verificam URLs abertas, fases, eventos e motivos (spec US1-1 a US1-8), sem cena e sem servidor.

### Tests for User Story 1 (MANDATORY) ⚠️

- [X] T015 [P] [US1] Escrever `Assets/Tests/EditMode/Net.Connection/SocketEndClassifierTests.cs` cobrindo cada linha da tabela de research R7, inclusive `auth_denied` com 4404 → `TokenRefused` e `match_denied` sem código → `MatchRefused(Unspecified)`
- [X] T016 [P] [US1] Escrever `Assets/Tests/EditMode/Net.Connection/ConnectionTargetTests.cs`: `Matchmaking(uri)` e `Match(uri, MatchId)`; `WithToken` acrescenta `token=` escapado (texto com `+`, `/`, `=`) preservando `matchId`; URL relativa ou esquema `http` lançam com o valor; `ToString()` sem query e sem token
- [X] T017 [P] [US1] Escrever `Assets/Tests/EditMode/Net.Connection/GiveUpReasonTests.cs`: `PlayerText()` não vazio e distinto para cada `GiveUpKind` e cada `MatchRefusalDetail`; `Attempts` só nos braços que o carregam
- [X] T018 [P] [US1] Escrever `Assets/Tests/EditMode/Net.Connection/ConnectionSettingsTests.cs`: `ForMatchmaking()` com base 0,5 s, teto 5 s, 5 tentativas; `ForMatch()` com base 0,5 s, teto 15 s, sem limite; ambos com `ConnectionTiming.Default` e 2 recusas de token (research R15)
- [X] T019 [P] [US1] Alterar `Assets/Tests/EditMode/Net.Connection/ReconnectPolicyTests.cs`: tirar `NormalClosure` de `CodigosTerminaisDesistemSemGastarTentativa` e acrescentar `FechamentoNormalDoServidorEhReconectavel` (1000 → `Retry`, conta tentativa); nenhuma outra asserção muda (research R7)
- [X] T020 [P] [US1] Escrever `Assets/Tests/EditMode/Net.Connection/ConnectionTokenTests.cs` com a rig: US1-1 (URL `?token=A`, `auth_denied` + 4001, fase `RenewingToken`, `RenewRequests == 1`, próxima URL `?token=B`, `Connected` e prova no primeiro frame), US1-2 (três recusas seguidas → `GaveUp(TokenRefusedRepeatedly)`, `Created.Count == 3`), US1-3 (renovação `Unavailable` → `WaitingRetry`, recusas não contadas, próxima abertura chama `GetValidAsync` de novo), US1-4 (renovação `SessionExpired` → `GaveUp(SessionExpired)`), `GetValidAsync` com `SessionUnavailable(NoSession)` → `GaveUp(NoSession)` sem criar socket, `GetValidAsync` `Unavailable` → `WaitingRetry`, e renovação segura por `HoldNextRenewal` + `Leave()` + `Release()` → nenhum socket novo e log `connection_stale_token_result`
- [X] T021 [P] [US1] Escrever `Assets/Tests/EditMode/Net.Connection/ConnectionGateTests.cs`: US1-5 para 4404, 4400 e 4403 com `match_denied` antes (motivo e detalhe certos, `RenewRequests == 0`, nenhum socket novo depois de avançar 1 min), `match_denied` + fechamento sem código → `Unspecified`, 4404 sem frame → `MatchNotFound`, e `match_denied` no alvo de fila também desiste com log
- [X] T022 [P] [US1] Escrever `Assets/Tests/EditMode/Net.Connection/ConnectionDropTests.cs` com jitter determinístico (`random: () => 0.5`): US1-6 (três quedas sem código → esperas 0,5/1/2 s anunciadas em `StatusChanged` com a tentativa; abertura só quando o relógio passa `retryAt`; `Recovered` uma vez na prova do socket reaberto; nenhum `Recovered` na primeira conexão), 1000 e 1001 do servidor → `WaitingRetry`, socket que abre e cai antes da prova não zera a tentativa, `ForMatchmaking` com 6 quedas seguidas sem prova → `GaveUp(AttemptsExhausted)`, erro + fechamento do mesmo socket → uma tentativa só
- [X] T023 [P] [US1] Escrever `Assets/Tests/EditMode/Net.Connection/ConnectionLeaveTests.cs`: US1-7 com `Leave()` em `Connecting`, `Connected`, `WaitingRetry` e `RenewingToken` (socket fechado, fase `Disconnected`, nenhum socket criado depois de avançar 1 min e soltar a renovação, nenhum `Recovered`/`FrameReceived` depois); `Connect` depois de `GaveUp` recomeça com política zerada; `Connect` com alvo diferente durante `Connecting` lança com os dois alvos; `Connect` com o mesmo alvo não faz nada
- [X] T024 [P] [US1] Escrever `Assets/Tests/EditMode/Net.Connection/ConnectionFrameDeliveryTests.cs`: US1-8 (três frames válidos em ordem por `FrameReceived`, texto inválido → log `connection_frame_invalid` e conexão aberta), `RawTextReceived` com o texto de cada frame aceito, `SendAsync` sem socket → `NotOpen` e log `connection_send_not_open`, `SendAsync` aberto → texto codificado em `SentTexts`, e nenhum registro do `FakeClientLog` contém o texto do token em nenhum teste deste arquivo (FR-016)

### Implementation for User Story 1

- [X] T025 [P] [US1] Criar `ConnectionPhase.cs`, `SuspensionReason.cs` e `ConnectionStatus.cs` (fábricas por fase; `Attempt`/`Wait` só em `WaitingRetry`, `Suspension` só em `Suspended`, `GiveUp` só em `GaveUp`; igualdade por valor) em `Assets/Scripts/Net/Connection/Connection/` (data-model)
- [X] T026 [P] [US1] Criar `GiveUpKind.cs`, `MatchRefusalDetail.cs` e `GiveUpReason.cs` com `PlayerText()` em português em `Assets/Scripts/Net/Connection/Connection/` (FR-013; faz T017 passar)
- [X] T027 [P] [US1] Criar `SocketEndKind.cs`, `SocketEnd.cs` e `SocketEndClassifier.cs` em `Assets/Scripts/Net/Connection/Connection/`, com as constantes 4001/4400/4403/4404 lidas de `ReconnectPolicy` (faz T015 passar)
- [X] T028 [P] [US1] Criar `Assets/Scripts/Net/Connection/Connection/ConnectionTarget.cs` (faz T016 passar)
- [X] T029 [P] [US1] Em `Assets/Scripts/Net/Connection/Connection/ReconnectPolicy.cs`, tirar `NormalClosure` de `IsTerminal`, mantendo a constante; atualizar o comentário do `case` citando `specs/003-authenticated-socket-queue/spec.md` (Assumptions: 1000 reconectável) (faz T019 passar)
- [X] T030 [P] [US1] Criar `Assets/Scripts/Net/Connection/Connection/ConnectionSettings.cs` com `ForMatchmaking()` e `ForMatch()`, trazendo os comentários de por quê de `QueuePolicy` e `MatchPolicy` de `Assets/Scripts/Bootstrap/NetworkBootstrap..cs` (faz T018 passar)
- [X] T031 [US1] Criar `Assets/Scripts/Net/Connection/Connection/SocketAttempt.cs`: um socket físico com geração; `Open(ConnectionTarget, AccessToken)` cria pela fábrica e abre; handlers de `Opened`, `TextReceived`, `Closed`, `Errored`; decodifica pelo codec; marca `auth_denied`/`match_denied` vistos; avisa `Opened`, `Proven` (primeiro frame que não é gate), `FrameArrived(ServerFrame, string)`, `Ended(SocketEnd)` uma vez; `Discard()` desliga handlers antes de `Close()` (research R6) (depende de T025–T028)
- [X] T032 [US1] Criar `Assets/Scripts/Net/Connection/Connection/AuthenticatedConnection.cs` com `Connect`, `Leave`, `SendAsync`, `Status`, `Dispose` e os eventos do contrato: tentativa com geração e `GetValidAsync`; mapeamento `SocketEnd` → política (research R7); `retryAt` avaliado em `Ticked`; `RenewingToken` com `RenewNowAsync`; prova → `policy.Reset()` e `Recovered`; `Leave` descarta e nunca reabre; eventos de log do contrato. Se passar de ~300 linhas, extrair a agenda de tentativas para `Assets/Scripts/Net/Connection/Connection/RetrySchedule.cs` com teste próprio (depende de T029–T031; faz T020–T024 passar)
- [X] T033 [US1] Criar `Assets/Scripts/Net/Connection/ConnectionRoutes.cs` (`Uri Matchmaking`, `Uri Match`, validados como `ws`/`wss` absolutos) e `BuildConnectionRoutes()` em `Assets/Scripts/Core/Config/AppConfig.cs` a partir de `WsUrl(matchmakingConsumerUrl)` e `WsUrl(matchConsumerUrl)`, com casos em `Assets/Tests/EditMode/Net.Connection/ConnectionRoutesTests.cs` e `Assets/Tests/EditMode/Config/AppConfigTests.cs`
- [X] T034 [US1] Rodar a suíte; todos os testes de US1 e da fundação verdes, sem aviso novo

**Checkpoint**: conexão autenticada completa e testada sozinha.

---

## Phase 4: User Story 2 - Entrar na fila com um deck e ser pareado, mesmo caindo (Priority: P1)

**Goal**: `MatchQueue` manda `join_queue`, entrega `match_found` tipado e fecha o socket, entrega recusas por `code`, reentra sozinha ao cair procurando e sai com motivo ao esgotar.

**Independent Test**: roteiros EditMode sobre a rig com `ConnectionSettings.ForMatchmaking()` (spec US2-1 a US2-9) e, depois da US5, `LiveQueueTests`.

### Tests for User Story 2 (MANDATORY) ⚠️

- [X] T035 [P] [US2] Escrever `Assets/Tests/EditMode/Net.Connection/Matchmaking/JoinQueueMessageTests.cs`: codificado pelo codec real sai com `type` `join_queue` e `deck_id` inteiro
- [X] T036 [P] [US2] Escrever `Assets/Tests/EditMode/Net.Connection/Matchmaking/MatchFoundFrameTests.cs`: frame completo → `MatchPairing` com `MatchId`, `Self` e `Opponent` (`UserId`, apelido, ícone, nível); `opponent` ausente, `user_id` em texto ou `match_id` inteiro → decodificação inválida com o caminho; campos a mais ignorados
- [X] T037 [P] [US2] Escrever `Assets/Tests/EditMode/Net.Connection/Matchmaking/MatchmakingFailedFrameTests.cs`: `error` lido; sem `error` → inválido
- [X] T038 [P] [US2] Escrever `Assets/Tests/EditMode/Net.Connection/Matchmaking/QueueRefusalReaderTests.cs`: cada linha da tabela de `contracts/matchmaking-queue.md` ("QueueRefusal"), `invalid_deck` com `wrong_deck_size` e `too_many_copies` na ordem, `deck_not_found` sem `deck_id` → `Unrecognized` com `Code` preservado e log `queue_refusal_out_of_contract`, e `Error` diferente com o mesmo `code` → mesmo `Kind`
- [X] T039 [P] [US2] Escrever `Assets/Tests/EditMode/Net.Connection/Matchmaking/MatchQueueJoinTests.cs`: US2-1 (primeiro texto enviado é o `join_queue` com o deck; `Searching` só depois do envio), US2-2 (`match_found` → `Paired`, fase `Paired`, socket fechado e nenhum socket novo depois de avançar 1 min), `Join` em `Connecting`/`Searching` → `AlreadyQueued` sem envio, envio `NotOpen` mantém `Connecting` e a próxima abertura reenvia
- [X] T040 [P] [US2] Escrever `Assets/Tests/EditMode/Net.Connection/Matchmaking/MatchQueueRefusalTests.cs`: US2-3 a US2-6 (fase `OutOfQueue`, socket aberto, `Refused`/`MatchmakingFailed` com os dados certos, novo `Join` pelo mesmo socket envia outro `join_queue`)
- [X] T041 [P] [US2] Escrever `Assets/Tests/EditMode/Net.Connection/Matchmaking/MatchQueueReconnectTests.cs`: US2-7 (queda procurando → `Connecting` → reabre → primeiro texto do socket novo é `join_queue` com o mesmo deck → `Searching`), US2-8 (tentativas esgotadas → `LeftQueue(AttemptsExhausted)` e `OutOfQueue`), reenvio recusado → `Refused` sem consumir tentativa, queda com fila fora de busca → nenhum socket novo, `match_found` inválido → log e fase `Searching`
- [X] T042 [P] [US2] Escrever `Assets/Tests/EditMode/Net.Connection/Matchmaking/MatchQueueLeaveTests.cs`: US2-9 (`Leave` procurando → socket fechado, `OutOfQueue`, nenhum `LeftQueue`/`Refused`, nenhum socket novo), frame chegando depois de `Paired` e depois de `Leave` → descartado

### Implementation for User Story 2

- [X] T043 [P] [US2] Criar `Assets/Scripts/Net/Connection/Matchmaking/JoinQueueMessage.cs` com `WriteDeckId("deck_id", deck)` (faz T035 passar)
- [X] T044 [P] [US2] Criar `PairedPlayer.cs`, `MatchPairing.cs` e `MatchFoundFrame.cs` em `Assets/Scripts/Net/Connection/Matchmaking/`, lendo por `ReadMatchId`, `ReadUserId`, `ReadText`, `ReadInteger`
- [X] T045 [P] [US2] Criar `Assets/Scripts/Net/Connection/Matchmaking/MatchmakingFailedFrame.cs`
- [X] T046 [P] [US2] Criar `QueueRefusalKind.cs`, `QueueRefusal.cs` e `QueueRefusalReader.cs` em `Assets/Scripts/Net/Connection/Matchmaking/`, com `DeckProblemUnion.Create().ReadNested` para `deck_problems` e captura de `PayloadShapeException` só no leitor (faz T038 passar)
- [X] T047 [US2] Registrar `match_found` e `matchmaking_failed` em `Assets/Scripts/Net/Connection/Matchmaking/ConnectionFrames.cs` e acrescentar os dois casos em `ConnectionFramesTests.cs` (depende de T044, T045; faz T036, T037 passar)
- [X] T048 [P] [US2] Criar `QueuePhase.cs` e `JoinOutcome.cs` em `Assets/Scripts/Net/Connection/Matchmaking/`
- [X] T049 [US2] Criar `Assets/Scripts/Net/Connection/Matchmaking/MatchQueue.cs` seguindo a tabela de research R10, com eventos `queue_join_sent`, `queue_refused`, `queue_left` (depende de T032, T043, T046–T048; faz T039–T042 passar)
- [X] T050 [US2] Rodar a suíte; US1 e US2 verdes, sem aviso novo

**Checkpoint**: fila completa sobre a conexão, testada sem servidor.

---

## Phase 5: User Story 3 - Perceber conexão morta sem confundir pausa com silêncio (Priority: P2)

**Goal**: ping na abertura e a cada intervalo com marcador, latência exposta, silêncio real derruba e reconecta, pausa e frames recém-chegados nunca derrubam, `pong` prova a sessão.

**Independent Test**: `ConnectionSilenceTests` com relógio avançando entre ticks e frames enfileirados na `MainThreadQueue` (spec US3-1 a US3-6).

### Tests for User Story 3 (MANDATORY) ⚠️

- [X] T051 [P] [US3] Acrescentar a `Assets/Tests/EditMode/Net.Connection/HeartbeatTests.cs`: `ForgivePauseZeraOSilencioEMantemOTimeoutArmado` e `NotePingSentAdiaOProximoPing` (research R8)
- [X] T052 [P] [US3] Escrever `Assets/Tests/EditMode/Net.Connection/PingLedgerTests.cs`: `Next` gera `sent_at_ms` do instante e sequência crescente; `Match` do marcador pendente devolve a latência e o retira; marcador desconhecido ou nulo → nulo; nono marcador descarta o mais velho
- [X] T053 [P] [US3] Escrever `Assets/Tests/EditMode/Net.Connection/ConnectionSilenceTests.cs` com a rig: ping logo depois de abrir e depois a cada 10 s (marcador lido do texto enviado), US3-1 (latência 80 ms em `LatencyMeasured` e `LastLatency`), US3-2 (depois de um `pong`, 31 s em ticks de 1 s sem frame → após `Drain`, socket descartado e `WaitingRetry`; log `connection_silence_confirmed`), US3-3 (sem `pong`, 60 s → nada), US3-4 (salto de 60 s num tick com `pong` enfileirado antes → nada), confirmação enfileirada seguida de frame que já estava na fila → nada, US3-5 (socket de fila só com `pong` → prova, recusas e tentativas zeradas), US3-6 (volta do segundo plano com socket aberto → silêncio zerado, ping imediato, e só derruba 30 s depois da volta)

### Implementation for User Story 3

- [X] T054 [P] [US3] Acrescentar `ForgivePause()` e `NotePingSent()` a `Assets/Scripts/Net/Connection/Connection/Heartbeat.cs`, com `<summary>`/`<example>` e comentário de por quê citando research R3 (faz T051 passar)
- [X] T055 [P] [US3] Criar `Assets/Scripts/Net/Connection/Connection/PingLedger.cs` (faz T052 passar)
- [X] T056 [US3] Criar `Assets/Scripts/Net/Connection/Connection/SilenceWatch.cs`: guarda o instante do último tick; delta > `PauseThreshold` → `ForgivePause` antes de `Tick`; `SendPing` → aviso `PingDue`; `DeclareDead` → enfileira confirmação com a geração do socket na `MainThreadQueue` e avisa `SilenceConfirmed` só se a geração é a mesma e o silêncio continua acima do limite; `pong` → `NotePong` + `LatencyMeasured` pelo `PingLedger`; outro frame → `NoteInbound`; `Forgive()` para a volta ao primeiro plano (depende de T054, T055)
- [X] T057 [US3] Ligar `SilenceWatch` em `Assets/Scripts/Net/Connection/Connection/AuthenticatedConnection.cs`: ping na abertura por `PingMessage` com o marcador, `Ticked` → `SilenceWatch`, `PingDue` → envio, `SilenceConfirmed` → descarte + caminho de `Dropped(null)`, `ReturnedToForeground` com socket aberto → `Forgive()` + ping + log `connection_ping_on_foreground`, `LatencyMeasured` repassado com log `connection_latency` (depende de T056; faz T053 passar)
- [X] T058 [US3] Rodar a suíte; US1 a US3 verdes, sem aviso novo

**Checkpoint**: conexão morta percebida, pausa perdoada.

---

## Phase 6: User Story 4 - Sobreviver ao segundo plano e à troca de rede (Priority: P2)

**Goal**: tentativas suspensas em segundo plano e sem rede, reabertura imediata na volta e na rede de volta, reciclagem na troca Wi-Fi ↔ dados, Windows sem suspender ao minimizar.

**Independent Test**: `ConnectionSuspensionTests` com `FakeAppLifecycle` e `FakeNetworkReachability` (spec US4-1 a US4-5), `LifecycleSignalFilterTests` e `RunInBackgroundSettingTests`; US4-6 e US4-7 pelo quickstart.

### Tests for User Story 4 (MANDATORY) ⚠️

- [X] T059 [P] [US4] Escrever `Assets/Tests/EditMode/Net.Connection/ConnectionSuspensionTests.cs` com a rig: US4-1 (esperando, segundo plano, relógio além da espera → nenhum socket, `Suspended(Background)`, tentativa não consumida), US4-2 (volta → `GetValidAsync` e abertura no mesmo passo, backoff zerado, log `connection_reopening cause=Foreground`), US4-3 (volta com socket aberto → ping, nenhum socket novo), US4-4 (aberta em `LocalArea`, `SimulateKind(CarrierData)` → socket antigo descartado, socket novo aberto sem tick, nenhuma tentativa consumida, log `connection_socket_recycled`, `Closed` tardio do antigo ignorado), US4-5 (caída com `None` → sem abertura por 10 min, `Suspended(NoNetwork)`; `SimulateKind(LocalArea)` → abertura já), segundo plano com socket aberto não fecha, `LocalArea → None → CarrierData` recicla uma vez só, volta com `GaveUp` ou `Disconnected` não faz nada, troca de rede em segundo plano só abre na volta
- [X] T060 [P] [US4] Acrescentar a `Assets/Tests/EditMode/Net.Core/Lifecycle/LifecycleSignalFilterTests.cs` os casos de `BackgroundSignalMode`: `AndroidPause` igual ao comportamento atual com `false`; `DesktopStopsOnFocusLoss` igual ao atual com `true`; `DesktopKeepsRunning` ignora `OnPause(true)` e `OnFocus(false)` e nunca avisa
- [X] T061 [P] [US4] Escrever `Assets/Tests/EditMode/Net.Editor/RunInBackgroundSettingTests.cs`: `UnityEditor.PlayerSettings.runInBackground` verdadeiro, com mensagem dizendo onde ligar (FR-026)

### Implementation for User Story 4

- [X] T062 [P] [US4] Criar `Assets/Scripts/Net/Core/Lifecycle/BackgroundSignalMode.cs` e evoluir `Assets/Scripts/Net/Core/Lifecycle/LifecycleSignalFilter.cs` para receber o modo, com o construtor de `bool` delegando e os comentários preservados (faz T060 passar)
- [X] T063 [P] [US4] Criar `ResumeCause.cs` e `ConnectionSuspension.cs` em `Assets/Scripts/Net/Connection/Connection/` (flags `InBackground`/`WithoutNetwork` a partir de `IAppLifecycle` e `INetworkReachability.Current`/`Changed`; avisos `Suspended` e `Resumed(ResumeCause)`; `IDisposable` desassina), com `Assets/Tests/EditMode/Net.Connection/ConnectionSuspensionSignalsTests.cs` para as combinações de flags (research R9)
- [X] T064 [US4] Ligar `ConnectionSuspension` em `Assets/Scripts/Net/Connection/Connection/AuthenticatedConnection.cs`: sem socket aberto e suspenso → `Suspended(motivo)` e `retryAt` congelado; `Resumed` → `policy.ResetBackoff()` e nova tentativa já; `NetworkKindChanged` → descarte, geração++, tentativa já; abrir com `Current == None` → `Suspended(NoNetwork)`; `GaveUp`/`Disconnected` ignoram; log `connection_suspended`/`connection_reopening`/`connection_socket_recycled` (depende de T057, T063; faz T059 passar)
- [X] T065 [US4] Em `Assets/Scripts/Net/Unity/UnityAppLifecycle.cs`, `Create` escolhe `AndroidPause` no Android, `DesktopKeepsRunning` fora dele com `Application.runInBackground`, e `DesktopStopsOnFocusLoss` no resto; ajustar `Assets/Tests/EditMode/Net.Unity/UnityAppLifecycleTests.cs` se ele verificar a escolha (depende de T062)
- [X] T066 [US4] Em `ProjectSettings/ProjectSettings.asset`, `runInBackground: 0` → `1` (faz T061 passar)
- [X] T067 [US4] Rodar a suíte; US1 a US4 verdes, sem aviso novo

**Checkpoint**: núcleo de rede completo; falta religar o jogo.

---

## Phase 7: User Story 5 - Jogar do botão até a partida, pelo código de hoje (Priority: P2)

**Goal**: o jogo usa a conexão e a fila novas de Login a `match_start`; o aviso de reconexão continua; saem NativeWebSocket, dispatcher, `ConnectionClient`, ponte de token e `NetworkBootstrap`.

**Independent Test**: quickstart §2 (LiveServer), §3 (dois jogadores do Multiplayer Play Mode) e §7 (remoções).

### Tests for User Story 5 (MANDATORY) ⚠️

- [X] T068 [P] [US5] Escrever `Assets/Tests/EditMode/Net.Unity/UnityFrameTickerTests.cs`: `Raise()` dispara `Ticked` uma vez por chamada
- [X] T069 [P] [US5] Escrever `Assets/Tests/EditMode/Net.Unity/DotNetWebSocketFactoryTests.cs`: cada `Create()` devolve `DotNetWebSocket` distinto; argumentos nulos lançam nomeando o campo; e acrescentar a `Assets/Tests/EditMode/Net.Unity/LiveNetworkAdaptersTests.cs` que o codec decodifica `match_found` e que `Sockets` não é nulo
- [X] T070 [P] [US5] Escrever `Assets/Tests/EditMode/Net.Unity/LiveConnectionServicesTests.cs` com fakes nos adaptadores: `MatchmakingConnection` e `MatchConnection` distintas, `Queue` sobre a de fila, `Dispose` desassina ciclo de vida, rede e ticker (um `Tick` depois não cria socket)
- [X] T071 [P] [US5] Criar `Assets/Tests/EditMode/Fakes/SpoiledFirstTokenSource.cs` e `Assets/Tests/EditMode/Net.Connection/Fakes/SpoiledFirstTokenSourceTests.cs` (primeiro `Valid` sai com texto diferente, os seguintes e as renovações passam iguais) (research R14)

### Implementation for User Story 5

- [X] T072 [P] [US5] Criar `Assets/Scripts/Net/Unity/UnityFrameTicker.cs` (faz T068 passar)
- [X] T073 [P] [US5] Criar `Assets/Scripts/Net/Unity/DotNetWebSocketFactory.cs`; em `Assets/Scripts/Net/Unity/LiveNetworkAdapters.cs`, codec com `ConnectionFrames.CreateUnion()` e propriedade `IWebSocketFactory Sockets` (faz T069 passar)
- [X] T074 [US5] Em `Assets/Scripts/Net/Unity/NetworkLayerHost.cs`, `Attach` recebe `UnityFrameTicker` e `Update` faz `Poll` → `Drain` → `Raise`, com comentário citando research R2 e R3; ajustar chamadas de `Attach` existentes (depende de T072)
- [X] T075 [US5] Criar `Assets/Scripts/Net/Unity/LiveConnectionServices.cs` com `FromAdapters(...)` montando as duas conexões (`ForMatchmaking`, `ForMatch`) e a `MatchQueue` sobre `ConnectionTarget.Matchmaking(routes.Matchmaking)` (depende de T049, T064, T073; faz T070 passar)
- [X] T076 [US5] Reescrever `Assets/Scripts/Core/Network/WebSocketClient/Base.cs` como adaptador de `AuthenticatedConnection` pela tabela de `contracts/legacy-bridge.md` ("Tradução do `BaseClient` para o overlay"), com `#nullable enable`, sem NativeWebSocket, Newtonsoft, `JsonUtility`, `Debug`, `WebSocketDispatcher`, `PlayerSession`, `TokenRefreshService` e sem `ClientConnectionState`; preservar os comentários que ainda valem (FR-038, FR-042)
- [X] T077 [US5] Reescrever `Assets/Scripts/Core/Network/WebSocketClient/MatchClient.cs`: construtor `(AuthenticatedConnection, Uri matchBase)`, `Connect(MatchId)` por `ConnectionTarget.Match`, `match_start` pela ponte `RawTextReceived` + `JsonConvert` + `MatchSession.Instance.ApplyState` sem recarregar a `MatchScene`, como hoje; `match_denied` e `ReadError` saem; comentário da ponte citando research R12 (depende de T076)
- [X] T078 [US5] Em `Assets/Scripts/Core/Contexts/VersusContext.cs`, `SetContext(MatchPairing)` preenchendo `Player`, `Opponent` e `MatchId` a partir de `PairedPlayer`; apagar `Assets/Scripts/DTO/VersusDTO.cs` (com `.meta`); `VersusController` não muda
- [X] T079 [US5] Reescrever `Assets/Scripts/Core/Network/WebSocketClient/MatchmakingClient.cs`: construtor `(MatchQueue, MatchClient, AuthenticatedConnection, IClientLog)`, `Join(DeckId)`, `Leave()`; `Paired` → `VersusContext.Instance.SetContext`, `SceneManager.LoadScene("VersusScene")`, `matchClient.Connect(pairing.Match)`; `Refused`, `MatchmakingFailed` e `LeftQueue` pelo log; sem `NetworkBootstrap`, `JsonUtility`, `Debug` (depende de T076–T078)
- [X] T080 [US5] Em `Assets/Scripts/Core/Session/PlayerSession.cs`, `ComposeAccount` → `ComposeNetwork` pela ordem de `contracts/legacy-bridge.md` ("Composição"); expor `Matchmaking` e `Match`; chamar `ReconnectOverlay.Attach(Matchmaking, Match)`; descartar conexões no `OnDestroy`; remover `Token`; atualizar o `<summary>` da classe (dívida até a feature 5) (depende de T074, T075, T079)
- [X] T081 [US5] Reescrever `Assets/Scripts/UI/PlayButton.cs` (`MyButtonScript`, mesmo nome de classe para não quebrar a `HomeScene`): `OnClickAction` inicia uma tarefa observada que lista decks por `PlayerSession.Instance.Account.Decks.ListAsync()` e entra com o primeiro por `PlayerSession.Instance.Matchmaking.Join`; sem deck → log `play_without_deck`; falha → log `play_decks_unavailable` (depende de T080)
- [X] T082 [US5] Apagar `Assets/Scripts/Bootstrap/NetworkBootstrap..cs` (com `.meta`) e o `GameObject`/componente dele em `Assets/Scenes/BootstrapScene.unity` (procurar o GUID `3c6edc48ab5fad046b7001d2b7e35a00`; remover o bloco `MonoBehaviour`, a referência em `m_Component` e, se ficar vazio, o `GameObject` e o `Transform`); se o componente já estava como script ausente, registrar isso no registro da execução (depende de T080)
- [X] T083 [US5] Apagar, com `.meta`: `Assets/WebSocket/` inteira, `Assets/Scripts/Core/Network/WebSocketClient/Dispatcher.cs`, `Assets/Scripts/Core/Network/WebSocketClient/ConnectionClient.cs`, `Assets/Scripts/Core/Network/TokenRefreshService.cs` e `Assets/Scripts/DTO/Network/ErrorPayloadDTO.cs`; conferir que `Assets/Scripts/Core/Network/SelfProfileService.cs` e `LoginController.cs` compilam (depende de T076–T081)
- [X] T084 [US5] Criar `Assets/Tests/EditMode/Net.Unity/LocalConnectionRoutes.cs` (`ws://127.0.0.1:8000/ws/matchmaking/` e `/ws/match/`) e `Assets/Tests/EditMode/Net.Unity/LiveServer/TextDeckJoinMessage.cs` (`join_queue` com `deck_id` em texto)
- [X] T085 [US5] Escrever `Assets/Tests/EditMode/Net.Unity/LiveServer/LiveQueueTests.cs` (`[Explicit]`, `[Category("LiveServer")]`, `[UnityTest]`, molde de `LiveAccountTests`) com os seis casos de `contracts/matchmaking-queue.md` e research R14; laço de espera drena a fila e chama `FakeFrameTicker.Tick()` a cada `yield`, com prazo de 30 s por cenário (depende de T071, T075, T084)
- [X] T086 [US5] Rodar a suíte; todos verdes, sem aviso novo; rodar a verificação de remoções do quickstart §7 (SC-002)

**Checkpoint**: jogo religado; falta prova com servidor, editor e aparelho.

---

## Phase 8: Polish & Cross-Cutting Concerns

**Purpose**: prova de ponta a ponta, dívida registrada e registro da execução.

- [X] T087 [P] Atualizar `C:/Users/gabri/Obsidian/Projetos/Anathema/Game/TODO.md` pela lista de `contracts/legacy-bridge.md` ("`Game/TODO.md` (vault)"): marcar feitos os itens da 001 e da 002, reescrever o de `PlayerSession.Instance` (caminho: feature 5) e criar a seção da feature 003 com os cinco itens, incluindo os dois pedidos ao backend de research R11
- [X] T088 [P] Atualizar `specs/003-authenticated-socket-queue/contracts/*.md` e `data-model.md` com qualquer desvio de nome ou assinatura feito na implementação
- [ ] T089 Rodar os LiveServer pelo quickstart §2 com `docker compose up` do backend; `LiveQueueTests`, `LiveAccountTests` e `LiveServerProbeTests` verdes (SC-007)
- [ ] T090 Validar o quickstart §3 no editor com dois jogadores do Multiplayer Play Mode (SC-008)
- [ ] T091 Validar o quickstart §4 e §5 no aparelho Android com build de desenvolvimento (SC-009, SC-010)
- [ ] T092 Validar o quickstart §6 no build Windows (SC-011)
- [ ] T093 Rodar a suíte oficial com o editor fechado, conferir os `.meta` gerados de todo arquivo novo e movido, e acrescentar "Registro da execução" ao fim deste arquivo com o que rodou, o que só compilou, desvios e o que ficou em aberto (SC-001, SC-012)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: sem dependências.
- **Foundational (Phase 2)**: depende da Setup; bloqueia todas as histórias.
- **US1 (Phase 3)**: depende da fundação.
- **US2 (Phase 4)**: depende da US1 (`MatchQueue` usa `AuthenticatedConnection`). Os testes e tipos de frame e recusa (T035–T038, T043–T046, T048) podem começar junto com a US1.
- **US3 (Phase 5)**: depende da US1. Pode correr em paralelo com a US2, exceto T057, que edita `AuthenticatedConnection.cs`.
- **US4 (Phase 6)**: depende da US3 (T064 usa a volta ao primeiro plano de T057). T060–T063, T065 e T066 podem começar junto com a US3.
- **US5 (Phase 7)**: depende da US2 e da US4.
- **Polish (Phase 8)**: depende da US5.

### Arquivos compartilhados (serializar)

- `AuthenticatedConnection.cs`: T032 → T057 → T064.
- `ConnectionFrames.cs`: T013 → T047.
- `ReconnectPolicy.cs`: T005 → T029. `ReconnectPolicyTests.cs`: T006 → T019.
- `Heartbeat.cs`: T005 → T054. `HeartbeatTests.cs`: T006 → T051.
- `PlayerSession.cs`, `Base.cs`, `MatchClient.cs`, `MatchmakingClient.cs`: T076 → T081, nesta ordem.

### Within Each User Story

- Testes escritos antes e falhando (ou sem compilar) antes da implementação.
- Tipos de valor antes de orquestradores.
- Rodar a suíte no fim de cada fase.

---

## Parallel Example: User Story 1

```bash
# Testes de US1, juntos:
Task: "SocketEndClassifierTests em Assets/Tests/EditMode/Net.Connection/SocketEndClassifierTests.cs"
Task: "ConnectionTargetTests em Assets/Tests/EditMode/Net.Connection/ConnectionTargetTests.cs"
Task: "ConnectionTokenTests em Assets/Tests/EditMode/Net.Connection/ConnectionTokenTests.cs"
Task: "ConnectionDropTests em Assets/Tests/EditMode/Net.Connection/ConnectionDropTests.cs"

# Tipos de valor de US1, juntos:
Task: "ConnectionPhase/ConnectionStatus em Assets/Scripts/Net/Connection/Connection/"
Task: "GiveUpReason em Assets/Scripts/Net/Connection/Connection/GiveUpReason.cs"
Task: "SocketEndClassifier em Assets/Scripts/Net/Connection/Connection/SocketEndClassifier.cs"
Task: "ConnectionTarget em Assets/Scripts/Net/Connection/Connection/ConnectionTarget.cs"
```

## Parallel Example: User Story 2

```bash
Task: "MatchFoundFrameTests + MatchFoundFrame em Assets/.../Matchmaking/"
Task: "QueueRefusalReaderTests + QueueRefusalReader em Assets/.../Matchmaking/"
Task: "JoinQueueMessageTests + JoinQueueMessage em Assets/.../Matchmaking/"
```

---

## Implementation Strategy

### MVP First (User Story 1)

1. Phase 1 e Phase 2.
2. Phase 3 (US1): conexão autenticada testada sozinha.
3. **Parar e validar**: suíte verde; a política e o heartbeat movidos continuam passando.

### Incremental Delivery

1. Setup + fundação → base pronta.
2. US1 → conexão (MVP do núcleo).
3. US2 → fila sobre a conexão.
4. US3 → heartbeat com latência e pausa.
5. US4 → segundo plano, rede e Windows.
6. US5 → jogo religado e remoções (primeira entrega visível ao jogador).
7. Polish → LiveServer, editor, aparelho, vault.

### Parallel Team Strategy

- Depois da fundação: uma frente em US1; outra nos tipos de frame e recusa da US2 (T035–T038, T043–T046, T048) e nos testes/filtro da US4 (T060–T062, T065, T066).
- Depois da US1: uma frente em US2 (T049), outra em US3.
- Tarefas que editam `AuthenticatedConnection.cs` ficam numa frente só.

---

## Notes

- **O código antigo não tem teste próprio.** `BaseClient`, `MatchClient`,
  `MatchmakingClient`, `PlayerSession`, `VersusContext` e `MyButtonScript` ficam
  em `Assembly-CSharp`, que nenhuma asmdef de teste referencia. Eles só traduzem
  eventos; o comportamento está em `Anathema.Net.Connection` (Phases 3 a 6), e o
  fluxo é validado por LiveServer e quickstart (T089, T090).
- **Android e Windows** são validados só por quickstart (T091, T092).
- `MatchSession`, `MiniPlayerProfile`, `LoginController`, `SelfProfileService`,
  `VersusController`, `ReconnectOverlay` e `AppEnvManager` não são editados
  (o overlay só volta a compilar contra o `BaseClient` novo).
- Limitações do backend (research R11) não ganham contorno no cliente em
  nenhuma tarefa.
- Commit ao fim de cada tarefa ou grupo lógico, sempre com os `.meta`.
- Se um contrato do backend e esta lista discordarem, vale o contrato.

## Registro da execução (2026-09-14)

- **Verificação offline.** O `Unity.exe` não abre a partir desta sessão, então a suíte oficial não rodou. No lugar dela:
  - as assemblies foram compiladas com o Roslyn do Unity 6000.2.8f1: Core, Json, Account, Fakes, Connection, Unity, Config, Editor, `Assembly-CSharp` e as de teste (Core, Json, Account, Connection, Unity, Config, Editor);
  - os testes rodaram no Mono do Unity pelo NUnit do pacote.
- **Compilação.** Nenhum erro e nenhum aviso novo. Os avisos CS0649 de `AppEnvManager`, `VersusController` e `MiniPlayerProfile` já existiam.
- **Testes executados.**
  - `Anathema.Net.Core.Tests`, `Anathema.Net.Json.Tests`, `Anathema.Net.Account.Tests` e `Anathema.Net.Connection.Tests`: 620 passaram, 0 falharam (eram 402 antes da feature).
  - De `Anathema.Net.Unity.Tests`, compilados à parte e rodados: `UnityFrameTickerTests`, `DotNetWebSocketFactoryTests` e `LiveConnectionServicesTests`, 7 passaram. A assembly inteira trava fora do editor (testes de `UnityWebRequest` e de socket real).
- **Testes só compilados.** O resto de `Anathema.Net.Unity.Tests` (inclusive `UnityAppLifecycleTests` e `LiveNetworkAdaptersTests` com os casos novos), `RunInBackgroundSettingTests` (usa `PlayerSettings`), `AppConfigTests` (`ScriptableObject`) e `LiveQueueTests` (`[Explicit]`, precisa do backend).
- **Bugs de teste corrigidos.**
  - O rig tinha a propriedade `Queue` e um método `Queue(...)`; o método virou `QueueOver`.
  - `SocketQueSoRecebePongContaComoAutenticado` chamava `OpenLatest()` num socket já aberto.
- **Desvios do plano e das tarefas.**
  - `SocketAttempt`, `SocketEnd`, `SocketEndKind`, `SocketEndClassifier`, `PingLedger`, `SilenceWatch`, `ConnectionSuspension`, `ResumeCause`, `QueueRefusalReader` e o novo `SocketUrl` são internos.
  - `ConnectionStatus` tem as fábricas `Of`, `Waiting`, `SuspendedBy` e `GivenUp`. `ReconnectPolicy` ganhou `BaseDelaySeconds`, `MaxDelaySeconds`, `MaxAttempts`, `MaxAuthRetries` e `JitterRatio`, e o construtor e `OnClosed` foram divididos em métodos menores, sem mudar comportamento.
  - Rede de volta depois de "sem rede" também recicla um socket que ainda pareça aberto, e troca de rede recicla mesmo com abertura ou renovação em curso (research R9 e contrato atualizados).
  - Depois de um salto de pausa entre quadros sai um ping extra, para confirmar a conexão.
  - `BaseClient` perdeu `Connect()`, `OnConnected` e `OnConnectionError`, sem uso. Assinaturas: `MatchClient(connection, matchBase, log)` e `MatchmakingClient(connection, queue, match, log)`. O `MatchClient` lê `match_start` por um envelope com `JsonConvert`, sem `JObject`.
  - Eventos de log novos no código de cena, registrados em `contracts/legacy-bridge.md`.
  - A `BootstrapScene` perdeu também o GameObject `WebSocketDispatcher`, porque a classe saiu. O componente `NetworkBootstrap` já estava como script ausente (`m_EditorClassIdentifier: '::'`), confirmando o nome de arquivo com dois pontos.
  - Os LiveServer usam o ajudante `LivePlayer` e ficam sob `#if UNITY_EDITOR_WIN`, como `LiveAccountTests` (guarda DPAPI).
  - `VersusContext` continua sem `#nullable`: os campos serializados do Unity gerariam aviso.
- **Arquivos `.meta`.** Os dos arquivos e pastas novos em `Assets/` só existem depois que o Unity abrir o projeto. Os arquivos movidos levaram o `.meta` por `git mv`.
- **A conferir no editor.**
  - A `BootstrapScene` abre sem script ausente.
  - O botão Jogar da `HomeScene` continua ligado: a classe `MyButtonScript` mora em `PlayButton.cs`, nome que já não batia antes desta feature.
  - `Assets/_Recovery/0 (1).unity`, cópia de recuperação do editor, ainda cita o `WebSocketDispatcher`; não foi tocada.
- **Em aberto.** Precisam do editor, do backend ou do aparelho:
  - T089: LiveServer com `docker compose up`;
  - T090: quickstart §3 com dois jogadores do Multiplayer Play Mode;
  - T091: Android (quickstart §4 e §5);
  - T092: build Windows (quickstart §6);
  - T093: suíte oficial e `.meta`.
- **Correção depois da execução offline.** Ao abrir o projeto, o Unity acusou CS0012 em `AppConfigTests`: `Anathema.Config.Tests.asmdef` não referenciava `Anathema.Net.Connection`. O build offline passava a referência a mais e escondeu a falha. A asmdef ganhou a referência.
