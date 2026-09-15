---

description: "Task list for 005-presentation-facade"
---

# Tasks: Fachada da apresentação e prova final

**Input**: Design documents from `specs/005-presentation-facade/`

**Prerequisites**: [plan.md](./plan.md), [spec.md](./spec.md), [research.md](./research.md), [data-model.md](./data-model.md), [contracts/](./contracts/), [quickstart.md](./quickstart.md)

**Tests**: obrigatórios (constituição, princípio VII). Todo método novo ganha
teste EditMode. Escreva o teste antes da implementação: em Unity, "falhar" pode
significar não compilar porque o tipo ainda não existe.

**Organization**: tarefas agrupadas pelas histórias da spec (US1 a US9). A ordem
das fases segue as dependências, não só a prioridade:

- US1 (estado) → US2 (conta) → US4 (fila) → US5 (partida) → US3 (dados) → US6
  (saúde): a fachada, núcleo, testada sobre fakes;
- US7 (composição e cenas) vem antes de US8 (contrato e `internal`): o código de
  cena antigo usa tipos que vão virar internos, e só pode sair depois de a borda
  nova existir;
- US9 (prova) vem por último: ela só vale depois de a fronteira estar fechada.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: pode rodar em paralelo com as outras [P] do mesmo bloco (arquivos
  diferentes, sem depender de tarefa incompleta do bloco)
- **[Story]**: US1 a US9 (histórias da spec)

## Convenções para todas as tarefas

Leia antes de executar qualquer tarefa:

- **Antes de escrever código**:
  - leia `CLAUDE.md`, `.specify/memory/constitution.md` e o contrato citado na
    tarefa (`specs/005-presentation-facade/contracts/`);
  - formas do servidor: nunca copie para código nem para comentário; cite o
    caminho em `C:/Users/gabri/Projetos/dev_container/anathema/backend/specs/`
    (`011-deck-catalog-api/contracts/`, `012-match-result-history/contracts/http_match_history.md`,
    `009-match-protocol/contracts/`, `010-match-timers/contracts/`) ou
    `scripts/smoke_match.py`.
- **Namespaces**: um por assembly, igual ao nome da asmdef
  (`Anathema.Net.Facade`, `Anathema.Client.Scenes`, `Anathema.Client.Proof`).
  - Testes: namespace testado + `.Tests`.
  - Subpastas (`State/`, `Match/`, `Health/`, `Composition/`…) **não** criam
    sub-namespace.
  - Scripts de tela movidos para `Assets/Scripts/Presentation/` continuam no
    namespace global: as cenas os referenciam pelo nome da classe.
- **Todo arquivo novo**:
  - começa com `#nullable enable`;
  - tem um tipo público por arquivo;
  - métodos de 4 a 20 linhas, com no máximo 2 níveis de indentação;
  - `var` só quando o tipo aparece do lado direito.
- **Documentação e erros**:
  - membro público leva `/// <summary>` com a intenção e um `<example>`;
  - mensagem de exceção inclui o valor recebido e a forma esperada;
  - comentários existentes em arquivo evoluído, movido ou removido são
    preservados no destino indicado em
    `contracts/composition-and-scenes.md` ("Destino do código antigo").
- **Mover arquivo** em `Assets/`: sempre `git mv` do `.cs` **e** do `.meta`
  juntos. O GUID do `.meta` é o que mantém a referência das cenas.
- **Estado e assincronia** (research R4, R5):
  - estágio, resultado e saúde só mudam na thread principal;
  - continuação de `await` que muda estado ou publica passa pela `MainThreadQueue`
    das portas (`queue.Enqueue`), nunca direto;
  - nada de `Task.Run`, `async void` (exceto `Start` de `MonoBehaviour`),
    `Task.Delay`, timer ou trava;
  - espera do histórico só pelo `IFrameTicker` + relógio monotônico;
  - toda operação assíncrona confere a geração do `ClientStages` antes de aplicar.
- **Avisos**: toda assinatura pública é `EventFeed<T>.Subscribe`; nenhum `event`
  novo em tipo público.
- **Nenhuma regra de jogo** (FR-038): a fachada não decide jogada, dano, fase ou
  vez; a estratégia da prova escolhe o que mandar e aceita a recusa.
- **Eventos de log**: nomes e campos de `contracts/client-state.md` ("Log") e
  `contracts/match-proof.md` ("Log da prova"). Não invente outros sem acrescentar
  lá.
- **Testes**:
  - NUnit em `Assets/Tests/EditMode/<Assembly>/<subpasta>/`;
  - nomes de método em português, no estilo de `ReconnectPolicyTests`;
  - assíncronos como `[Test] public async Task`;
  - fakes de `Anathema.Net.Fakes`, criados no `[SetUp]`, nunca em campo
    inicializado;
  - `[TestCase]` nunca recebe tipo interno como parâmetro de método público:
    passe o nome (`"InMatch"`) e converta no corpo (memória do projeto);
  - não existe `Assert.Multiple` no NUnit do Unity;
  - tempo só por `FakeMonotonicClock.Advance` + `FakeFrameTicker.Tick()`, e
    drenagem por `MainThreadQueue.Drain()`.
- **`.meta`**: o Unity gera ao abrir o projeto; todo arquivo e pasta novos em
  `Assets/` vão para o commit com o `.meta`; removido sai com o `.meta`.
- **Rodar a suíte** (editor fechado):
  `"C:/Program Files/Unity/Hub/Editor/6000.2.8f1/Editor/Unity.exe" -batchmode -projectPath . -runTests -testPlatform EditMode -testResults Logs/editmode-results.xml`
  - Se o `Unity.exe` não abrir a partir da sessão, compile cada assembly com o
    Roslyn do Unity usando só as referências da própria asmdef e rode NUnit no
    Mono, como no registro de execução de `specs/004-match-session/tasks.md`.

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: assemblies novas vazias, sem mudar o comportamento de nada.

- [X] T001 Criar `Assets/Scripts/Net/Facade/Anathema.Net.Facade.asmdef` (`noEngineReferences: true`, `autoReferenced: true` por enquanto, referências `Anathema.Net.Core`, `Anathema.Net.Account`, `Anathema.Net.Connection`, `Anathema.Net.Match`) e `Assets/Scripts/Net/Facade/FacadeAssemblyInfo.cs` com `InternalsVisibleTo` para `Anathema.Net.Facade.Tests`, `Anathema.Net.Unity` e `Anathema.Net.Unity.Tests`, cada linha com o comentário do porquê (molde: `Assets/Scripts/Net/Match/MatchAssemblyInfo.cs`)
- [X] T002 [P] Criar `Assets/Tests/EditMode/Net.Facade/Anathema.Net.Facade.Tests.asmdef` no molde de `Assets/Tests/EditMode/Net.Match/Anathema.Net.Match.Tests.asmdef`, referenciando Core, Json, Account, Connection, Match, Facade e Fakes
- [X] T003 [P] Criar `Assets/Scripts/Client/Scenes/Anathema.Client.Scenes.asmdef` (motor, referências Facade, Unity, Config, Core, Account, Connection, Match) e `Assets/Tests/EditMode/Client.Scenes/Anathema.Client.Scenes.Tests.asmdef` (Client.Scenes, Facade, Core)
- [X] T004 [P] Criar `Assets/Scripts/Client/Proof/Anathema.Client.Proof.asmdef` (motor, `defineConstraints: ["UNITY_EDITOR || DEVELOPMENT_BUILD"]`, referências Facade, Unity, Config, Core, Account, Connection, Match) e `Assets/Tests/EditMode/Client.Proof/Anathema.Client.Proof.Tests.asmdef` (Proof, Facade, Unity, Core, Json, Account, Connection, Match, Fakes). Confira na documentação do Unity 6000.2 que `||` é aceito em `defineConstraints`; se não for, registre a alternativa escolhida em `research.md` R11
- [X] T005 [P] Criar `Assets/Scripts/Presentation/Anathema.Presentation.asmdef` (motor, referências Facade, Client.Scenes, Core, Account, Connection, Match, `Unity.TextMeshPro`, `UnityEngine.UI`, `Unity.Multiplayer.Playmode`), ainda sem scripts
- [X] T006 Acrescentar `InternalsVisibleTo("Anathema.Net.Facade")` e `("Anathema.Net.Facade.Tests")` em `Assets/Scripts/Net/Account/AccountAssemblyInfo.cs`, `Assets/Scripts/Net/Connection/ConnectionAssemblyInfo.cs` e `Assets/Scripts/Net/Match/MatchAssemblyInfo.cs`; criar `Assets/Scripts/Net/Core/CoreAssemblyInfo.cs` se não existir, com as mesmas duas amizades mais Account, Connection e Match (para `EventFeed.Publish`, T008)
- [X] T007 Compilar tudo (suíte ou verificação offline) e confirmar zero erro e zero aviso novo antes de seguir

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: `EventFeed`, eventos da 004 convertidos, composição sobre portas, tipos de estado e o dono das transições.

**⚠️ CRITICAL**: nenhuma história começa antes desta fase.

### Tests (write first)

- [X] T008 [P] `EventFeedTests` em `Assets/Tests/EditMode/Net.Core/Notices/EventFeedTests.cs`: entrega em ordem de assinatura; descartado não recebe; descartar dentro do próprio aviso cala os seguintes; descartar duas vezes não falha; exceção de um ouvinte vira `feed_listener_failed` (campos `feed`, `exception`) e os outros recebem; ouvinte nulo lança citando o nome do feed (research R3)
- [X] T009 [P] `ClientStateTests` em `Assets/Tests/EditMode/Net.Facade/State/ClientStateTests.cs`: cada fábrica preenche só os campos do seu estágio (tabela `ClientState` de `data-model.md`); `ClientStageChange` e `StageRequestResult` recusam nulo
- [X] T010 [P] `MatchResultTests` e `MatchUnavailableTests` de forma em `Assets/Tests/EditMode/Net.Facade/Match/MatchResultShapeTests.cs`: `Row` só em `Resolved`, `Attempts` entre 0 e 4, `GiveUp` só em `MatchRefused`/`ConnectionGaveUp`, `PlayerText` nunca vazio
- [X] T011 [P] `ClientStagesTests` em `Assets/Tests/EditMode/Net.Facade/State/ClientStagesTests.cs`: começa em `SignedOut(Startup)`; `Move` troca o estado inteiro, publica `StageChanged` uma vez com anterior e atual e registra `client_stage`; `Move` para estado igual não publica; `Generation` sobe em `Invalidate()`
- [X] T012 [P] `ClientPortsTests` em `Assets/Tests/EditMode/Net.Facade/Composition/ClientPortsTests.cs`: cada porta nula lança com o nome e a forma esperada

### Implementation

- [X] T013 `EventFeed<T>` em `Assets/Scripts/Net/Core/Notices/EventFeed.cs` (construtor e `Publish` internos, `Subscribe` público devolvendo `IDisposable`, assinatura privada aninhada), conforme `data-model.md` ("Núcleo: `EventFeed<T>`")
- [X] T014 Converter os cinco eventos de `Assets/Scripts/Net/Match/Mirror/MatchMirror.cs` para `EventFeed<T>` com os mesmos nomes, mantendo a ordem de publicação; atualizar `Assets/Scripts/Net/Match/Session/MatchNarrator.cs`, `Assets/Scripts/Net/Match/Session/LiveMatch.cs` e os testes em `Assets/Tests/EditMode/Net.Match/Mirror/` e `Net.Match/Session/` de `+=` para `Subscribe`
- [X] T015 Converter `TurnStarted` e `TurnRunningOut` de `Assets/Scripts/Net/Match/Clock/TurnClock.cs` para `EventFeed<T>`; atualizar `Assets/Scripts/Net/Match/Clock/ClockAnnouncements.cs` e os testes em `Assets/Tests/EditMode/Net.Match/Clock/`
- [X] T016 Converter `CurrentChanged` de `Assets/Scripts/Net/Match/Commands/PendingPlay.cs` para `EventFeed<T>`, com `IClientLog` no construtor interno; atualizar `LiveMatch.cs` e `Assets/Tests/EditMode/Net.Match/Commands/`
- [X] T017 Converter `StatusChanged` e `Refused` de `Assets/Scripts/Net/Match/Session/LiveMatch.cs` para `EventFeed<T>`; atualizar os consumidores antigos `Assets/Scripts/Core/Network/WebSocketClient/MatchClient.cs`, `Assets/Scripts/Core/Session/MatchSession.cs`, `Assets/Tests/EditMode/Net.Unity/LiveServer/SmokeBot.cs`, `LiveMatchTests.cs` e os testes de sessão da partida; rodar a suíte da partida (mesmos 248 casos verdes da 004)
- [X] T018 [P] Acrescentar `AlreadySignedIn` em `Assets/Scripts/Net/Account/Session/SignInOutcomeKind.cs` e a fábrica correspondente em `Assets/Scripts/Net/Account/Session/SignInOutcome.cs`, com teste no arquivo de testes de `SignInOutcome` existente em `Assets/Tests/EditMode/Net.Account/` (ou `SignInOutcomeKindTests.cs` novo se não houver)
- [X] T019 `git mv` de `Assets/Scripts/Net/Unity/LiveAccountServices.cs` (+`.meta`) para `Assets/Scripts/Net/Facade/Composition/AccountServices.cs` e de `LiveConnectionServices.cs` para `Assets/Scripts/Net/Facade/Composition/ConnectionServices.cs`; renomear as classes, trocar o namespace para `Anathema.Net.Facade`, tirar os `FromAdapters` (vão para T021); `git mv` dos testes `Assets/Tests/EditMode/Net.Unity/LiveAccountServicesTests.cs` e `LiveConnectionServicesTests.cs` para `Assets/Tests/EditMode/Net.Facade/Composition/AccountServicesTests.cs` e `ConnectionServicesTests.cs`
- [X] T020 `ClientPorts` em `Assets/Scripts/Net/Facade/Composition/ClientPorts.cs` com as portas de `data-model.md` ("A fachada"); `AccountServices` e `ConnectionServices` passam a ter um construtor por `ClientPorts`
- [X] T021 `LivePorts` em `Assets/Scripts/Net/Unity/LivePorts.cs`: monta `ClientPorts` a partir de `LiveNetworkAdapters`, ciclo de vida, alcançabilidade, ticker, guarda e rotas (substitui os dois `FromAdapters`); acrescentar `Anathema.Net.Facade` em `Assets/Scripts/Net/Unity/Anathema.Net.Unity.asmdef`; atualizar `Assets/Scripts/Core/Session/PlayerSession.cs` e `Assets/Tests/EditMode/Net.Unity/LiveServer/LivePlayer.cs`; teste `Assets/Tests/EditMode/Net.Unity/LivePortsTests.cs`
- [X] T022 [P] Tipos de estado em `Assets/Scripts/Net/Facade/State/`: `ClientStage.cs`, `SignedOutReason.cs`, `ClientStageChange.cs`, `StageRequestResult.cs`, `ClientState.cs` (fábricas internas por estágio)
- [X] T023 [P] Tipos de resultado em `Assets/Scripts/Net/Facade/Match/`: `MatchResult.cs`, `HistoryRowStatus.cs`, `MatchUnavailable.cs`, `MatchUnavailableKind.cs`
- [X] T024 `ClientStages` em `Assets/Scripts/Net/Facade/State/ClientStages.cs`: estado atual, `Generation`, `Invalidate()`, `Move(ClientState next, string cause)` publicando `StageChanged` e `client_stage`
- [X] T025 `FacadeTestRig` em `Assets/Tests/EditMode/Net.Facade/FacadeTestRig.cs`:
  - `ClientPorts` sobre `FakeHttpTransport` (respostas de `FakeAccountResponses`), `FakeWebSocketFactory`, `FakeMonotonicClock`, `FakeFrameTicker`, `FakeAppLifecycle`, `FakeNetworkReachability`, `FakeRefreshTokenVault`, `FakeClientLog`, `MainThreadQueue` e codec real (`NewtonsoftProtocolCodec(MatchFrames.CreateUnion(), log)`);
  - ajudantes `SignInAsync`, `OpenQueueSocket`, `ReceiveMatchFound`, `OpenMatchSocket`, `ReceiveMatchFixture(nome)` e `Pump()` (drena e dá tique);
  - fixtures de partida lidas de `Assets/Tests/EditMode/Net.Match/Fixtures/` pelo `[CallerFilePath]`, sem cópia (molde: `MatchFixtures.cs`)
- [X] T026 `AnathemaClient` em `Assets/Scripts/Net/Facade/AnathemaClient.cs`: construtor interno por `ClientPorts`, compõe `AccountServices`, `ConnectionServices` e `ClientStages`; expõe `State`, `StageChanged`, `Catalog`, `Decks`, `History`, `Log`, `Dispose`; teste `Assets/Tests/EditMode/Net.Facade/AnathemaClientCompositionTests.cs` (começa `SignedOut(Startup)`, serviços expostos, `Dispose` fecha as duas conexões)

**Checkpoint**: suíte verde; a 004 continua funcionando com feeds; a fachada compõe sobre fakes e fica em `SignedOut`.

---

## Phase 3: User Story 1 - Saber em que ponto o jogador está, sem cena (Priority: P1) 🎯 MVP

**Goal**: o caminho principal do estado do app (entrar, procurar, parear, jogar, terminar, voltar) avisado na thread principal.

**Independent Test**: `FacadeTestRig` roteiriza login → `join_queue` → `match_found` → `match_start` → queda e volta → frame terminado → voltar, verificando cada `ClientStageChange` (contracts/client-state.md, "Transições").

### Tests for User Story 1 (MANDATORY) ⚠️

- [X] T027 [P] [US1] `SignInStageTests` em `Assets/Tests/EditMode/Net.Facade/State/SignInStageTests.cs` (US1 cenários 1 e 2; login e retomada)
- [X] T028 [P] [US1] `QueueStageTests` em `Assets/Tests/EditMode/Net.Facade/State/QueueStageTests.cs` (US1-3: `Searching`, depois `Paired` com `MatchId` e os dois `PairedPlayer`)
- [X] T029 [P] [US1] `MatchStageTests` em `Assets/Tests/EditMode/Net.Facade/State/MatchStageTests.cs`:
  - US1-4: `InMatch` com o espelho já preenchido;
  - US1-5: queda e `match_start` de volta sem aviso e mesma instância;
  - US1-6: `MatchFinished` com desfecho e `RowStatus.Fetching`;
  - primeiro frame já terminado indo de `Paired` a `MatchFinished`
- [X] T030 [P] [US1] `ReturnStageTests` em `Assets/Tests/EditMode/Net.Facade/State/ReturnStageTests.cs` (US1-7: `ReturnToLobby` → `SignedIn`, `CurrentMatch` nulo, socket de partida fechado de propósito)
- [X] T031 [P] [US1] `StageChangeDeliveryTests` em `Assets/Tests/EditMode/Net.Facade/State/StageChangeDeliveryTests.cs`: aviso de continuação assíncrona só sai no `Drain`; assinatura descartada entre o `Enqueue` e o `Drain` não recebe (US1-8); cada transição sai uma vez

### Implementation for User Story 1

- [X] T032 [US1] `ClientAccount` em `Assets/Scripts/Net/Facade/ClientAccount.cs` com `SessionState`, `Self`, `SignInAsync` e `ResumeAsync`: sucesso aplica `SignedIn(Self)` pela `MainThreadQueue` com conferência de geração
- [X] T033 [P] [US1] `QueueJoinResult` e `QueueJoinKind` em `Assets/Scripts/Net/Facade/Queue/QueueJoinResult.cs` e `QueueJoinKind.cs`
- [X] T034 [US1] `ClientQueue` em `Assets/Scripts/Net/Facade/ClientQueue.cs` com `Phase`, `Join` (de `SignedIn`: `Started` → `Searching`) e o feed `Paired`, que leva a `Paired`; ouve a `MatchQueue` interna
- [X] T035 [US1] `MatchOpening` em `Assets/Scripts/Net/Facade/Match/MatchOpening.cs`:
  - no `Paired`: carrega o catálogo, cria a `LiveMatch` sobre `ConnectionServices.MatchConnection` e `Start`;
  - primeiro `Live` → `InMatch`;
  - `Finished` → `MatchFinished` com `MatchResult` em `Fetching`;
  - aplicações pela fila com conferência de geração;
  - comentários de cache de catálogo e de "antes de carregar" do `MatchClient` preservados aqui
- [X] T036 [US1] Ligar `Account`, `Queue`, `CurrentMatch` e `ReturnToLobby()` em `Assets/Scripts/Net/Facade/AnathemaClient.cs`; `ReturnToLobby` só em `MatchFinished`/`MatchUnavailable`, descarta a `LiveMatch` e chama `Invalidate()`; `<summary>` de `CurrentMatch` com os comentários de intenção do `MatchSession`

**Checkpoint**: US1 passa sozinha sobre fakes.

---

## Phase 4: User Story 2 - Conta pela fachada, e sessão expirada leva ao login (Priority: P1)

**Goal**: conta completa e expiração de sessão convergindo para `SignedOut(SessionExpired)` a partir de qualquer estágio.

**Independent Test**: `FacadeTestRig` com refresh recusado em cada estágio logado e com a conexão desistindo antes do aviso da sessão (contracts/client-state.md, "Conta" e "Sessão expirada").

### Tests for User Story 2 (MANDATORY) ⚠️

- [X] T037 [P] [US2] `ClientAccountTests` em `Assets/Tests/EditMode/Net.Facade/Account/ClientAccountTests.cs`:
  - cadastro sem mudar o estágio;
  - login recusado mantém `SignedOut`;
  - `AlreadySignedIn` sem requisição;
  - perfil;
  - segunda `AnathemaClient` sobre o mesmo `FakeRefreshTokenVault` retoma com o mesmo `UserId` (US2 cenários 1, 2, 3 e 6)
- [X] T038 [P] [US2] `SignOutStageTests` em `Assets/Tests/EditMode/Net.Facade/State/SignOutStageTests.cs`: de cada estágio logado → `SignedOut(SignedOut)`, sem `session_expired`, fila deixada e socket de partida fechado de propósito (US2-5)
- [X] T039 [P] [US2] `SessionExpiryStageTests` em `Assets/Tests/EditMode/Net.Facade/State/SessionExpiryStageTests.cs`: `[TestCase("SignedIn")]` … `[TestCase("MatchUnavailable")]` (6 casos) com refresh recusado → `SignedOut(SessionExpired)`, `CurrentMatch` nulo; catálogo ou histórico que termina depois não muda nada (US2-4, SC-002)
- [X] T040 [P] [US2] `SessionExpiryOrderTests` em `Assets/Tests/EditMode/Net.Facade/State/SessionExpiryOrderTests.cs`: desistência `SessionExpired` da conexão de partida antes do aviso da sessão dá o mesmo estado e uma publicação só; `NoSession` com `SignedOut` é ignorado (research R7)

### Implementation for User Story 2

- [X] T041 [US2] Completar `Assets/Scripts/Net/Facade/ClientAccount.cs`: `RegisterAsync`, `ReadProfileAsync`, `SignOut` (`StageRequestResult`) e `AlreadySignedIn` fora de `SignedOut`
- [X] T042 [US2] `SessionExpiryWatch` em `Assets/Scripts/Net/Facade/State/SessionExpiryWatch.cs`: assina `AccountSession.SessionExpired`, a desistência da fila e o `StatusChanged` da `LiveMatch` corrente, e chama uma única expiração idempotente. Acrescentar o nome à lista da research R14
- [X] T043 [US2] Desmontagem compartilhada por sair e expirar em `Assets/Scripts/Net/Facade/AnathemaClient.cs` (método privado): `Invalidate()`, deixar a fila, descartar a `LiveMatch`, e `Move` para `SignedOut` com o motivo; `SignOut` chama também `AccountSession.SignOut`

**Checkpoint**: US1 e US2 passam.

---

## Phase 5: User Story 4 - Fila pela fachada, com recusas e saída visíveis (Priority: P1)

**Goal**: sair da fila, recusa tipada, saída da fila e chamadas fora de hora.

**Independent Test**: `FacadeTestRig` com `message_refused` de fila, `matchmaking_failed`, desistência da conexão de fila e `Join` em cada estágio (contracts/client-state.md, "Fila").

### Tests for User Story 4 (MANDATORY) ⚠️

- [X] T044 [P] [US4] `ClientQueueTests` em `Assets/Tests/EditMode/Net.Facade/Queue/ClientQueueTests.cs`:
  - `deck_not_found` → `Refused` + `SignedIn`;
  - `Leave` → `SignedIn` sem recusa;
  - desistência → `Left(ConnectionGaveUp)`;
  - `matchmaking_failed` → `Left(MatchmakingFailed)`;
  - `Join` fora de `SignedIn` → `NotApplicable`, e em `Searching` → `AlreadyQueued`, nada enviado (US4 cenários 1 a 4)
- [X] T045 [P] [US4] `StageMismatchTests` em `Assets/Tests/EditMode/Net.Facade/State/StageMismatchTests.cs`: `Leave`, `ReturnToLobby`, `RetryMatch` e `SignOut` fora de hora devolvem `Applied` falso com o estágio, sem exceção e sem requisição (FR-008)

### Implementation for User Story 4

- [X] T046 [P] [US4] `QueueExit` e `QueueExitKind` em `Assets/Scripts/Net/Facade/Queue/QueueExit.cs` e `QueueExitKind.cs`
- [X] T047 [US4] Completar `Assets/Scripts/Net/Facade/ClientQueue.cs`:
  - `Leave` (`StageRequestResult`);
  - feeds `Refused` e `Left`;
  - `AlreadyQueued`/`NotApplicable`;
  - volta a `SignedIn` só a partir de `Searching`;
  - desistência por motivo de sessão vai para a `SessionExpiryWatch`;
  - comentário de `matchmaking_failed` do `MatchmakingClient` preservado

**Checkpoint**: US1, US2 e US4 passam.

---

## Phase 6: User Story 5 - Partida corrente, erro explícito e resultado com histórico (Priority: P1)

**Goal**: `MatchUnavailable` com tentar de novo, e a linha do histórico depois do fim.

**Independent Test**: `FacadeTestRig` com catálogo recusado, `match_denied`, tentativas esgotadas e respostas de histórico sem a linha, com a linha na terceira leitura e nunca (contracts/client-state.md, "Partida indisponível" e "Linha do histórico").

### Tests for User Story 5 (MANDATORY) ⚠️

- [X] T048 [P] [US5] `MatchUnavailableTests` em `Assets/Tests/EditMode/Net.Facade/Match/MatchUnavailableTests.cs`:
  - catálogo falha → `CatalogUnavailable`, nenhum socket, `match_catalog_unavailable`;
  - `RetryMatch` → `Paired` e `Start` novo;
  - `match_denied` → `MatchRefused`;
  - tentativas esgotadas → `ConnectionGaveUp` (US5 cenários 1 a 3)
- [X] T049 [P] [US5] `HistoryRowLookupTests` em `Assets/Tests/EditMode/Net.Facade/Match/HistoryRowLookupTests.cs`:
  - leituras em 0, 1, 3 e 7 s de `FakeMonotonicClock` com `Tick()`, nunca uma quinta;
  - linha na 1ª e na 3ª → `Resolved` e `ResultUpdated` uma vez;
  - nunca, falha ou recusa → `Unavailable`;
  - cancelado por voltar, sair, expirar e `Dispose` sem aviso;
  - `won` divergente → `match_history_disagrees`;
  - oponente nulo (US5 cenários 5 a 7, SC-003)
- [X] T050 [P] [US5] `CurrentMatchTests` em `Assets/Tests/EditMode/Net.Facade/Match/CurrentMatchTests.cs`: em `InMatch`, `CurrentMatch` é a mesma sessão cujos espelho, relógio, comandos, pendente, `HintFor`, `StatsOf` e `Refused` estão ativos; em `SignedIn`, nulo (US5-4)

### Implementation for User Story 5

- [X] T051 [US5] Completar `Assets/Scripts/Net/Facade/Match/MatchOpening.cs`:
  - falha de catálogo → `MatchUnavailable(CatalogUnavailable)` com `match_catalog_unavailable` (`match_id`, `kind`);
  - `SessionUnavailable` → expiração;
  - `Refused` → `MatchRefused`;
  - `GaveUp` que não é de sessão → `ConnectionGaveUp`;
  - a `LiveMatch` é descartada ao entrar em `MatchUnavailable`
- [X] T052 [US5] `RetryMatch()` em `Assets/Scripts/Net/Facade/AnathemaClient.cs`: só em `MatchUnavailable`, volta a `Paired` com o mesmo pareamento e reabre
- [X] T053 [US5] `HistoryRowLookup` em `Assets/Scripts/Net/Facade/Match/HistoryRowLookup.cs`: assina `IFrameTicker`, esperas `[1 s, 2 s, 4 s]`, `HistoryPageRequest(1)`, casa por `MatchId`, conta falha e recusa como tentativa, registra `match_history_row` e `match_history_disagrees`, e confere a geração (research R6)
- [X] T054 [US5] `ResultUpdated` em `Assets/Scripts/Net/Facade/AnathemaClient.cs`; iniciar `HistoryRowLookup` na entrada em `MatchFinished` e trocar o `ClientState` (mesmo estágio) a cada atualização

**Checkpoint**: US1, US2, US4 e US5 passam.

---

## Phase 7: User Story 3 - Catálogo, decks e histórico pela fachada (Priority: P2)

**Goal**: os serviços de dados da 002 pela fachada, com o catálogo uma vez por sessão.

**Independent Test**: `FacadeTestRig` contando requisições do `FakeHttpTransport` (contracts/presentation-surface.md, "Catálogo, decks e histórico").

### Tests for User Story 3 (MANDATORY) ⚠️

- [X] T055 [P] [US3] `ClientDataTests` em `Assets/Tests/EditMode/Net.Facade/Account/ClientDataTests.cs`:
  - duas cargas → um pedido;
  - `Find` de unidade, feitiço e inexistente;
  - deck de 12 → `WrongDeckSize`;
  - sair e entrar com outra conta → catálogo pedido de novo (US3 cenários 1 a 4)

### Implementation for User Story 3

- [X] T056 [US3] Ajustar `Assets/Scripts/Net/Facade/AnathemaClient.cs` e `Assets/Scripts/Net/Facade/Composition/AccountServices.cs` só no que `ClientDataTests` apontar (esperado: nada além de expor os serviços já compostos em T026); registrar em `research.md` se precisar de mudança

**Checkpoint**: US3 passa.

---

## Phase 8: User Story 6 - Saúde da conexão para o overlay (Priority: P2)

**Goal**: avisos de reconexão, volta, desistência e latência da conexão ativa.

**Independent Test**: `FacadeTestRig` derrubando o `FakeWebSocket` da fila e da partida e suspendendo por rede (contracts/client-state.md, "Saúde da conexão").

### Tests for User Story 6 (MANDATORY) ⚠️

- [X] T057 [P] [US6] `ConnectionHealthTests` em `Assets/Tests/EditMode/Net.Facade/Health/ConnectionHealthTests.cs`:
  - queda e volta da partida → `Reconnecting(tentativa, espera)` e `Recovered`;
  - sem rede em `Searching` → `Reconnecting` com a tentativa anterior;
  - desistência → `GaveUp` com texto;
  - latência da conexão ativa;
  - conexão que não é a ativa não emite (US6 cenários 1 a 5)

### Implementation for User Story 6

- [X] T058 [P] [US6] `ReconnectingNotice`, `RecoveredNotice` e `GaveUpNotice` em `Assets/Scripts/Net/Facade/Health/`
- [X] T059 [US6] `HealthRelay` em `Assets/Scripts/Net/Facade/Health/HealthRelay.cs`: escolhe a conexão ativa pelo estágio e traduz `ConnectionStatus`, `Recovered` e `LatencyMeasured`; a lógica e o comentário de "última tentativa anunciada" vêm de `Assets/Scripts/Core/Network/WebSocketClient/Base.cs`
- [X] T060 [US6] `ConnectionHealth` em `Assets/Scripts/Net/Facade/ConnectionHealth.cs` (`LastLatency` e os quatro feeds), ligado em `AnathemaClient.Health`

**Checkpoint**: a fachada inteira (US1–US6) passa sobre fakes.

---

## Phase 9: User Story 7 - Composição sem singletons e cenas guiadas pelo estado (Priority: P1)

**Goal**: `ClientComposition`, `ClientHost`, roteador e binder; telas só pela fachada; código antigo removido.

**Independent Test**: `SceneRouteTests`, `ClientCompositionTests` e as guardas de fonte em EditMode, mais o quickstart §4 com dois jogadores (contracts/composition-and-scenes.md).

### Tests for User Story 7 (MANDATORY) ⚠️

- [X] T061 [P] [US7] `SteppableMonotonicClockTests` em `Assets/Tests/EditMode/Net.Unity/SteppableMonotonicClockTests.cs`: soma o salto, nunca volta, salto negativo lança
- [X] T062 [P] [US7] `DroppableWebSocketFactoryTests` em `Assets/Tests/EditMode/Net.Unity/DroppableWebSocketFactoryTests.cs` sobre `FakeWebSocketFactory`: `DropAll` aborta só os vivos e esquece os fechados
- [X] T063 [P] [US7] `ClientCompositionTests` em `Assets/Tests/EditMode/Net.Unity/ClientCompositionTests.cs`:
  - opções inválidas lançam;
  - `JumpClock` sem `AllowClockJumps` lança citando a opção;
  - `Pump` faz poll, drain e tique nessa ordem;
  - dois `Compose` com slots diferentes gravam guardas separadas (DPAPI em diretório temporário)
- [X] T064 [P] [US7] Evoluir o teste de rotas do `AppConfig` em `Assets/Tests/EditMode/Config/` (arquivo existente que cobre `BuildAccountRoutes`/`BuildConnectionRoutes`) para `BuildServerRoutes`, com as 9 rotas e o host efetivo
- [X] T065 [P] [US7] `SceneRouteTests` em `Assets/Tests/EditMode/Client.Scenes/SceneRouteTests.cs`: os sete estágios → cena da research R9
- [X] T066 [P] [US7] `SceneManagerUsageTests` e `StaticInstanceUsageTests` em `Assets/Tests/EditMode/Client.Scenes/` (dois arquivos):
  - leem os `.cs` de `Assets/Scripts/`;
  - `SceneManager` só em `Client/Scenes/SceneRouter.cs` e `Client/Scenes/SceneClientBinder.cs`;
  - nenhum `static <Tipo> Instance { get; private set; }` nem campo `static <Tipo>? instance` mutável;
  - `static ... Instance { get; } = new` imutável é permitido (FR-027, FR-029, SC-005);
  - falham até T084

### Implementation for User Story 7

- [X] T067 [P] [US7] `ServerRoutes` em `Assets/Scripts/Net/Unity/ServerRoutes.cs` (9 `Uri`, nenhum nulo, conversão interna para `AccountRoutes` e `ConnectionRoutes`) e `BuildServerRoutes()` em `Assets/Scripts/Core/Config/AppConfig.cs`, preservando o comentário do `[NonSerialized]`
- [X] T068 [P] [US7] `SteppableMonotonicClock` em `Assets/Scripts/Net/Unity/SteppableMonotonicClock.cs` (interno)
- [X] T069 [P] [US7] `DroppableWebSocketFactory` em `Assets/Scripts/Net/Unity/DroppableWebSocketFactory.cs` (interno)
- [X] T070 [US7] Slot da guarda:
  - `PlatformRefreshTokenVault.Create(IClientLog, RefreshTokenVaultSlot?)` em `Assets/Scripts/Net/Unity/Storage/PlatformRefreshTokenVault.cs`;
  - slot em `Assets/Scripts/Net/Unity/Storage/AndroidKeystoreRefreshTokenVault.cs`, no alias da chave e no nome do arquivo;
  - o slot `ForPlayer()` mantém o alias e o arquivo de hoje, para não perder a sessão de quem já instalou (leia o arquivo antes);
  - teste do nome derivado em `Assets/Tests/EditMode/Net.Unity/Storage/`
- [X] T071 [US7] `ClientCompositionOptions`, `ClientComposition` e `ComposedClient` em `Assets/Scripts/Net/Unity/` (três arquivos), conforme `contracts/composition-and-scenes.md`:
  - `Compose` usa `LivePorts`, `SteppableMonotonicClock` (salto só com a opção) e `DroppableWebSocketFactory`;
  - log padrão `UnityConsoleLog`;
  - cleartext pela opção
- [X] T072 [US7] Evoluir `Assets/Scripts/Net/Unity/NetworkLayerHost.cs`: `Attach` interno recebendo o `ComposedClient` (`Update` chama `Pump`; pausa e foco pelo ciclo de vida); `ComposedClient.AttachTo` adiciona o componente; comentário da ordem drain/tique preservado
- [X] T073 [P] [US7] `ISceneClientConsumer`, `SceneSubscriptions` e `SceneRoute` em `Assets/Scripts/Client/Scenes/` (três arquivos)
- [X] T074 [US7] `SceneClientBinder` em `Assets/Scripts/Client/Scenes/SceneClientBinder.cs`: liga consumidores das raízes de cada cena carregada, uma vez por componente, no `sceneLoaded` e sob pedido do hospedeiro
- [X] T075 [US7] `SceneRouter` em `Assets/Scripts/Client/Scenes/SceneRouter.cs`: assina `StageChanged`, usa `SceneRoute.For` e só carrega se `SceneManager.GetSceneByName(destino).isLoaded` for falso (research R9)
- [X] T076 [US7] `git mv` de `Assets/Scripts/Core/Session/PlayerSession.cs` (+`.meta`) para `Assets/Scripts/Client/Scenes/ClientHost.cs`:
  - renomear a classe;
  - absorver `configDev`, `configProd`, `isProd` e as checagens de `Assets/Scripts/Bootstrap/AppEnvManager.cs`, agora como log;
  - `Awake` compõe (`BuildServerRoutes`, cleartext por `Debug.isDebugBuild`) e chama `AttachTo` e `DontDestroyOnLoad`;
  - `Start` liga roteador e binder e liga as cenas já abertas;
  - `OnDestroy` descarta;
  - sem `Instance`
- [X] T077 [US7] `git mv` de `Assets/Scripts/Login/LoginController.cs`, `Assets/Scripts/UI/PlayButton.cs`, `VersusController.cs`, `MiniPlayerProfile.cs` e `ReconnectOverlay.cs` (cada um com `.meta`) para `Assets/Scripts/Presentation/`
- [X] T078 [P] [US7] `Assets/Scripts/Presentation/LoginController.cs` como `ISceneClientConsumer`:
  - `BindClient` retoma;
  - entrar por `client.Account`;
  - sem `SceneManager`, `PlayerSession`, `SelfProfileService` nem `IsEnvironmentReady`;
  - login automático do Multiplayer Play Mode mantido;
  - assinaturas numa `SceneSubscriptions` descartada no `OnDestroy`
- [X] T079 [P] [US7] `Assets/Scripts/Presentation/PlayButton.cs` (`MyButtonScript`): `client.Decks.ListAsync` e depois `client.Queue.Join` com o primeiro deck; logs `play_decks_unavailable` e `play_without_deck` pelo `client.Log`
- [X] T080 [P] [US7] `Assets/Scripts/Presentation/VersusController.cs`: `BindClient` lê `State.Pairing` (apelido e ícone dos dois); `Debug.LogWarning` passa a `client.Log`
- [X] T081 [P] [US7] `Assets/Scripts/Presentation/MiniPlayerProfile.cs`: `BindClient` chama `Account.ReadProfileAsync` e preenche; `Debug.LogWarning` passa a `client.Log`
- [X] T082 [P] [US7] `Assets/Scripts/Presentation/ReconnectOverlay.cs`: `BindClient` assina `client.Health` (`Reconnecting`, `Recovered`, `GaveUp`); saem `static instance`, `Attach` e o `HashSet<BaseClient>`; comentários do overlay preservados
- [X] T083 [US7] `git rm` (com `.meta` e pastas vazias):
  - `Assets/Scripts/Core/Network/WebSocketClient/Base.cs`, `MatchClient.cs`, `MatchmakingClient.cs`;
  - `Assets/Scripts/Core/Session/MatchSession.cs`;
  - `Assets/Scripts/Core/Contexts/VersusContext.cs`;
  - `Assets/Scripts/Core/Network/SelfProfileService.cs`;
  - `Assets/Scripts/DTO/Player/PlayerPublicDTO.cs`;
  - `Assets/Scripts/Bootstrap/AppEnvManager.cs`.

  Antes, confira que cada comentário foi para o destino da tabela em `contracts/composition-and-scenes.md`
- [X] T084 [US7] Editar o YAML das cenas:
  - `Assets/Scenes/BootstrapScene.unity`:
    - remover os objetos/componentes de `AppEnvManager`, `VersusContext` e `SelfProfileService`;
    - no componente do `ClientHost` (GUID do antigo `PlayerSession`), gravar `configDev`/`configProd`/`isProd` com os valores do `AppEnvManager` e `m_EditorClassIdentifier: Anathema.Client.Scenes::ClientHost`;
    - acrescentar um objeto com `ReconnectOverlay`;
  - `LoginScene.unity`, `HomeScene.unity` e `VersusScene.unity`: atualizar o `m_EditorClassIdentifier` dos scripts movidos
- [X] T085 [US7] `autoReferenced: false` em todas as asmdefs de `Assets/Scripts/Net/`, `Assets/Scripts/Client/`, `Assets/Scripts/Presentation/` e em `Assets/Scripts/Core/Config/Anathema.Config.asmdef`, acrescentando as referências explícitas que faltarem (ex.: `Anathema.Net.Editor` → `Anathema.Config`); `Assembly-CSharp` fica só com `InputNavigator`, `MatchController` (comentado) e `WsEventDto`
- [X] T086 [US7] Rodar a suíte: T061–T066 verdes, zero aviso novo

**Checkpoint**: nenhum singleton de composição; telas só pela fachada; roteador no comando.

---

## Phase 10: User Story 8 - A regra de consumo escrita e verificada (Priority: P1)

**Goal**: tudo fora de `contracts/presentation-surface.md` vira `internal`; fronteira testada; `CLAUDE.md` aponta para o contrato.

**Independent Test**: `SurfaceContractTests`, `SurfaceHasNoEventTests`, `FriendAssemblyTests` e `FacadeAssemblyBoundaryTests` verdes, mais o quickstart §2 (erro `CS0122`).

### Tests for User Story 8 (MANDATORY) ⚠️

- [X] T087 [P] [US8] `SurfaceContractTests` em `Assets/Tests/EditMode/Net.Facade/SurfaceContractTests.cs`: listas literais de `contracts/presentation-surface.md` ("Tipos públicos, por assembly"), a lista de composição de `Anathema.Net.Unity` (`ClientComposition`, `ClientCompositionOptions`, `ComposedClient`, `ServerRoutes`, `RefreshTokenVaultSlot`, `NetworkLayerHost`) e a de `Anathema.Client.Scenes`, comparadas com `Assembly.GetExportedTypes()`. A mensagem lista os tipos a mais e a menos
- [X] T088 [P] [US8] `SurfaceHasNoEventTests` em `Assets/Tests/EditMode/Net.Facade/SurfaceHasNoEventTests.cs`: nenhum tipo exportado de Core, Account, Connection, Match e Facade declara `event`
- [X] T089 [P] [US8] `FriendAssemblyTests` em `Assets/Tests/EditMode/Net.Facade/FriendAssemblyTests.cs`: o `InternalsVisibleToAttribute` de cada assembly carregada do projeto nunca cita `Anathema.Presentation`, `Anathema.Client.Scenes` nem `Anathema.Client.Proof`; `Anathema.Client.Proof.Tests` só aparece em Core, Json, Account e Match
- [X] T090 [P] [US8] `FacadeAssemblyBoundaryTests` em `Assets/Tests/EditMode/Net.Facade/FacadeAssemblyBoundaryTests.cs` no molde de `MatchAssemblyBoundaryTests`: sem `UnityEngine`, `UnityEditor`, `Newtonsoft`, `System.Net.WebSockets`, `Anathema.Net.Json` nem `Anathema.Net.Unity`
- [X] T091 [P] [US8] `FacadeFeedDisposalTests` e `FacadeDisposeTests` em `Assets/Tests/EditMode/Net.Facade/` (dois arquivos):
  - cada feed da fachada, da `LiveMatch`, do espelho, do relógio e do pendente não entrega depois de descartado (SC-004);
  - `AnathemaClient.Dispose` fecha tudo e nenhum feed entrega depois

### Implementation for User Story 8

- [X] T092 [US8] Tornar `internal` em `Assets/Scripts/Net/Core/` todo tipo fora da lista de Core; completar as amizades de `CoreAssemblyInfo.cs` pela tabela "Amizades" de `contracts/composition-and-scenes.md`; membros públicos de tipos públicos que citam tipo interno passam a `internal`
- [X] T093 [US8] Idem em `Assets/Scripts/Net/Json/` (criar `JsonAssemblyInfo.cs` com as amizades)
- [X] T094 [US8] Idem em `Assets/Scripts/Net/Account/`: construtores de `CardCatalog`, `PlayerDecks` e `MatchHistory` internos; `AccountSession`, `SessionAccessTokens`, `AuthenticatedHttpClient`, `OwnProfileQuery`, `AccountRegistration`, `AccountRoutes`, `AccountTiming`, `ForegroundRenewal`, `IAccessTokenSource`, tokens e leitores internos
- [X] T095 [US8] Idem em `Assets/Scripts/Net/Connection/`: `AuthenticatedConnection`, `MatchQueue`, `ConnectionPorts`, `ConnectionRoutes`, `ConnectionSettings`, `ConnectionStatus`, `ConnectionTarget`, política, heartbeat e frames internos
- [X] T096 [US8] Idem em `Assets/Scripts/Net/Match/`:
  - `MatchFrames`, `MatchStartFrame`, `MatchUpdateFrame`, `TurnWarningFrame`, `HandCardHints` e os `static Read(IPayloadReader)` internos;
  - construtores de `LiveMatch` e `MatchCommands` internos;
  - `PlayCommand` com `IOutgoingMessage.WritePayload` explícito chamando `internal abstract` (research R2)
- [X] T097 [US8] Idem em `Assets/Scripts/Net/Facade/`: `ClientStages`, `SessionExpiryWatch`, `MatchOpening`, `HistoryRowLookup`, `HealthRelay`, `ClientPorts`, `AccountServices` e `ConnectionServices` internos
- [X] T098 [US8] Idem em `Assets/Scripts/Net/Unity/`: tudo interno menos a lista de composição; criar `UnityAssemblyInfo.cs` com as amizades
- [X] T099 [US8] Tornar `internal` os fakes de `Assets/Tests/EditMode/Fakes/` e criar `Assets/Tests/EditMode/Fakes/FakesAssemblyInfo.cs` com amizade para cada assembly de teste
- [X] T100 [US8] Corrigir as assemblies de teste:
  - `[TestCase]` com tipo interno → nome ou número;
  - ajudantes públicos que citam tipo interno → `internal`;
  - `LivePlayer`, `LiveQueueTests` e `LiveAccountTests` compondo pelas peças internas da fachada;
  - rodar a suíte até T087–T091 ficarem verdes
- [X] T101 [US8] Conferir `specs/005-presentation-facade/contracts/presentation-surface.md` contra a lista final de `SurfaceContractTests` e corrigir o documento se divergir; acrescentar em `CLAUDE.md`, seção "Base de conhecimento", uma linha apontando `specs/005-presentation-facade/contracts/presentation-surface.md` como porta de entrada da parte visual (FR-023)

**Checkpoint**: usar tipo de máquina na apresentação não compila; fronteira testada.

---

## Phase 11: User Story 9 - Dois clientes headless jogam partidas inteiras usando só a fachada (Priority: P1)

**Goal**: o roteiro de `contracts/match-proof.md` passando contra o backend local, com a assembly da prova sem amizade nenhuma.

**Independent Test**: `MatchProofTests` (`[Explicit]`, `LiveServer`) pelo quickstart §3; `ProofStrategyTests` e `CommandCoverageTests` na suíte normal.

### Tests for User Story 9 (MANDATORY) ⚠️

- [X] T102 [P] [US9] `git mv` de `Assets/Tests/EditMode/Net.Unity/SmokeStrategyTests.cs` e `SmokeCatalog.cs` (+`.meta`) para `Assets/Tests/EditMode/Client.Proof/ProofStrategyTests.cs` e `ProofCatalog.cs`; acrescentar os casos das extensões:
  - remover o bloqueador uma vez depois de o bloqueio ser aceito;
  - `LIFE POTION` com Nexus ≥ 12 enquanto "feitiço sem alvo" não foi coberto, e ignorada depois;
  - `P2` sem agir na vez parada;
  - `Forfeit` no mulligan no modo partida 2
- [X] T103 [P] [US9] `CommandCoverageTests` em `Assets/Tests/EditMode/Client.Proof/CommandCoverageTests.cs`: os 11 itens; envio recusado não conta; `Missing` com nomes
- [X] T104 [P] [US9] `ProofLogTests` em `Assets/Tests/EditMode/Client.Proof/ProofLogTests.cs`: entradas em ordem, marca de salto, busca da primeira `access_token_renewed` e da primeira `connection_opening` depois de uma marca

### Implementation for User Story 9

- [X] T105 [P] [US9] `ProofSetup`, `ProofStep`, `ProofFailure`, `ProofWait` e `LocalServerRoutes` em `Assets/Scripts/Client/Proof/` (um arquivo cada)
- [X] T106 [P] [US9] `ProofLog` em `Assets/Scripts/Client/Proof/ProofLog.cs` (`IClientLog` que grava e repassa a um log interno de console recebido)
- [X] T107 [P] [US9] `CommandCoverage` em `Assets/Scripts/Client/Proof/CommandCoverage.cs`
- [X] T108 [US9] `git mv` de `Assets/Tests/EditMode/Net.Unity/LiveServer/SmokeStrategy.cs` (+`.meta`) para `Assets/Scripts/Client/Proof/ProofStrategy.cs`:
  - usa `Func<CardInstanceId, HandCardHint>` (`LiveMatch.HintFor`) no lugar de `HandCardHints.For`;
  - acrescenta as extensões de `contracts/match-proof.md`;
  - comentário de "estratégia de teste, não regra do cliente" preservado
- [X] T109 [US9] `git mv` de `Assets/Tests/EditMode/Net.Unity/LiveServer/SmokeBot.cs` para `Assets/Scripts/Client/Proof/ProofBot.cs`:
  - assina os feeds da `LiveMatch` e guarda os `IDisposable`;
  - registra `TurnRunningOut`;
  - confirma a cobertura pelo evento do `match_update` seguinte;
  - comentário do "match_start de mesma versão" preservado
- [X] T110 [US9] `ProofPlayer` em `Assets/Scripts/Client/Proof/ProofPlayer.cs` (compõe com slot, `ProofLog` e as opções de `contracts/match-proof.md`, "Montagem")
- [X] T111 [US9] `MatchProofScript` em `Assets/Scripts/Client/Proof/MatchProofScript.cs`, com os passos 1–18 de `contracts/match-proof.md`:
  - divida em classes internas por bloco (`AccountProofSteps.cs`, `QueueProofSteps.cs`, `FirstMatchProofSteps.cs`, `SecondMatchProofSteps.cs`) para ficar abaixo de 500 linhas por arquivo;
  - cada passo registra `proof_step`;
  - falha por `ProofFailure` com passo, esperado e recebido
- [X] T112 [US9] `MatchProofTests` em `Assets/Tests/EditMode/Client.Proof/LiveServer/MatchProofTests.cs` (`#if UNITY_EDITOR_WIN`, `[Explicit]`, `[Category("LiveServer")]`, `[UnityTest]`): bombeia os dois `ComposedClient` a cada quadro, espera o roteiro e usa só `MatchProofScript` e os tipos públicos
- [X] T113 [US9] `git rm` de `Assets/Tests/EditMode/Net.Unity/LiveServer/LiveMatchTests.cs` (o marco da 004, substituído pela prova); `RecordingWebSocketFactory` e o teste dele ficam
- [X] T114 [US9] `MatchProofRunner` em `Assets/Scripts/Client/Proof/MatchProofRunner.cs` e a cena `Assets/Scenes/Dev/MatchProofScene.unity` (um objeto com o runner e o `AppConfig` de desenvolvimento), fora de `EditorBuildSettings` (research R12)
- [X] T115 [US9] Rodar a prova pelo quickstart §3 com `docker compose up`; anotar tempo, recusas por bot e cobertura no registro de execução

**Checkpoint**: a camada de rede provada de ponta a ponta usando só a superfície.

---

## Phase 12: Polish & Cross-Cutting Concerns

**Purpose**: vault, revisão, suíte oficial e quickstarts manuais.

- [X] T116 Atualizar `C:/Users/gabri/Obsidian/Projetos/Anathema/Game/TODO.md` pela research R13: itens marcados "[feito na 005]", itens reapontados para "Caminho: parte visual" e item novo da cena de prova incluída à mão
- [X] T117 [P] Revisão por `grep` em `Assets/Scripts/Net/Facade/`, `Assets/Scripts/Client/` e `Assets/Scripts/Presentation/`: nada compara custo com energia, decide fase, calcula dano ou estoura vez (SC-011); a estratégia da prova só escolhe candidato
- [X] T118 [P] Conferir `.meta` de todo arquivo e pasta novos ou movidos em `Assets/` e ausência de `.meta` órfão dos removidos (`git status`)
- [ ] T119 Rodar a suíte oficial pelo comando da constituição: tudo verde, zero aviso novo, `Anathema.Net.Facade.Tests` abaixo de 5 s (SC-001, SC-010); quickstart §2 (erro `CS0122`)
- [ ] T120 Quickstart §4 com dois jogadores do Multiplayer Play Mode (SC-008)
- [ ] T121 Quickstart §5 no Android, build de desenvolvimento (SC-009); remover `MatchProofScene` da lista de cenas depois
- [X] T122 Acrescentar "Registro da execução" ao fim deste arquivo, no molde de `specs/004-match-session/tasks.md`: verificação feita, desvios do plano e o que ficou em aberto

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: sem dependência.
- **Foundational (Phase 2)**: depende do Setup; bloqueia todas as histórias.
- **US1 (Phase 3)**: depende da fundação.
- **US2 (Phase 4)**: depende de US1 (`ClientAccount`, `MatchOpening` e desmontagem sobre o estágio).
- **US4 (Phase 5)**: depende de US1 (`ClientQueue`) e de US2 (`SessionExpiryWatch`, para a desistência por sessão).
- **US5 (Phase 6)**: depende de US1 (`MatchOpening`) e de US2 (expiração durante abertura e histórico).
- **US3 (Phase 7)**: depende da fundação (T026); pode rodar em paralelo com US1–US5.
- **US6 (Phase 8)**: depende de US1 (estágio para escolher a conexão ativa).
- **US7 (Phase 9)**: depende de US1–US6 (as telas usam a fachada inteira).
- **US8 (Phase 10)**: depende de US7 (o código antigo que usa tipos de máquina já saiu).
- **US9 (Phase 11)**: depende de US7 (composição) e de US8 (fronteira fechada; a prova precisa compilar sem amizade).
- **Polish (Phase 12)**: depende de todas.

### Within Each User Story

- Testes antes da implementação; em Unity, "falhar" pode ser não compilar.
- Tipos simples ([P]) antes de quem os usa.
- `AnathemaClient.cs` é arquivo central: tarefas que o editam (T026, T036, T043, T052, T054, T056, T060) nunca são [P] entre si.
- T092–T100 são sequenciais: cada `internal` propaga erro de compilação para a assembly seguinte.

### Parallel Opportunities

- Setup: T002–T005.
- Fundação: testes T008–T012; tipos T022 e T023; T018 em qualquer momento.
- Cada história: todos os testes [P] juntos.
- US3 em paralelo com US1–US5 depois de T026.
- US7: T061–T066 juntos; T067–T069 juntos; T078–T082 juntos depois de T077.
- US8: T087–T091 juntos.
- US9: T102–T104 juntos; T105–T107 juntos.

---

## Parallel Example: User Story 7

```bash
# Testes da composição e das cenas:
Task: "SteppableMonotonicClockTests em Assets/Tests/EditMode/Net.Unity/SteppableMonotonicClockTests.cs"
Task: "DroppableWebSocketFactoryTests em Assets/Tests/EditMode/Net.Unity/DroppableWebSocketFactoryTests.cs"
Task: "ClientCompositionTests em Assets/Tests/EditMode/Net.Unity/ClientCompositionTests.cs"
Task: "SceneRouteTests em Assets/Tests/EditMode/Client.Scenes/SceneRouteTests.cs"

# Telas, depois do git mv (T077):
Task: "LoginController em Assets/Scripts/Presentation/LoginController.cs"
Task: "MyButtonScript em Assets/Scripts/Presentation/PlayButton.cs"
Task: "VersusController em Assets/Scripts/Presentation/VersusController.cs"
Task: "MiniPlayerProfile em Assets/Scripts/Presentation/MiniPlayerProfile.cs"
Task: "ReconnectOverlay em Assets/Scripts/Presentation/ReconnectOverlay.cs"
```

## Parallel Example: User Story 8

```bash
Task: "SurfaceContractTests em Assets/Tests/EditMode/Net.Facade/SurfaceContractTests.cs"
Task: "SurfaceHasNoEventTests em Assets/Tests/EditMode/Net.Facade/SurfaceHasNoEventTests.cs"
Task: "FriendAssemblyTests em Assets/Tests/EditMode/Net.Facade/FriendAssemblyTests.cs"
Task: "FacadeAssemblyBoundaryTests em Assets/Tests/EditMode/Net.Facade/FacadeAssemblyBoundaryTests.cs"
Task: "FacadeFeedDisposalTests e FacadeDisposeTests em Assets/Tests/EditMode/Net.Facade/"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Setup + fundação.
2. US1: o estado do app sobre fakes.
3. **Parar e validar**: `SignInStageTests`, `QueueStageTests`, `MatchStageTests`, `ReturnStageTests` e `StageChangeDeliveryTests` verdes.

### Incremental Delivery

1. Setup + fundação: feeds na 004, fachada composta em `SignedOut`.
2. US1: caminho principal do estado (MVP).
3. US2, US4, US5: conta e expiração, fila, indisponível e histórico.
4. US3, US6: dados e saúde; fachada completa sobre fakes.
5. US7: jogo rodando pela fachada, sem singletons (primeira entrega visível: quickstart §4).
6. US8: fronteira fechada pelo compilador.
7. US9: a prova contra o servidor.
8. Polish: vault, suíte oficial, Multiplayer Play Mode, Android.

### Parallel Team Strategy

- Depois da fundação: uma frente em US1 → US2 → US4 → US5, outra em US3 e depois US6.
- US7 numa frente para composição e cenas (T061–T076) e outra para telas e remoções (T077–T085).
- US8 numa frente só (propagação de `internal`).
- US9: estratégia e cobertura (T102–T108) em paralelo com runner e cena (T114).

---

## Notes

- **`Anathema.Presentation` não tem teste próprio.** São telas que só leem o estado e chamam a fachada. O comportamento está em `Anathema.Net.Facade.Tests` e `Anathema.Client.Scenes.Tests`, e o fluxo é validado pelo quickstart §4 (T120).
- **`Anathema.Client.Proof.Tests` é amiga de Core, Json, Account e Match** só para montar catálogo e visões nos testes de estratégia. `MatchProofTests` usa apenas o público, e `Anathema.Client.Proof` não é amiga de ninguém (plan, Complexity Tracking).
- A `MatchScene` não ganha desenho nenhum nesta feature.
- Limitações do backend (`match_found` perdido, `leave` por `user_id`) não ganham contorno.
- Se um contrato do backend e esta lista discordarem, vale o contrato.

---

## Registro da execução (2026-09-14)

- **Verificação offline.** O `Unity.exe` não abre a partir desta sessão, então a suíte oficial não rodou. No lugar dela:
  - todas as assemblies foram compiladas com o Roslyn do Unity 6000.2.8f1, cada uma só com as referências declaradas na própria asmdef: Core, Json, Account, Fakes, Connection, Match, Facade, Unity, Config, Editor, Client.Scenes, Presentation, Client.Proof, `Assembly-CSharp` e as de teste;
  - `Anathema.Net.Unity` também compilou com o define `UNITY_ANDROID`;
  - os testes sem motor rodaram no Mono do Unity pelo NUnit do pacote.
- **Compilação.** Nenhum erro e nenhum aviso novo. `Anathema.Presentation` e `Anathema.Client.Proof` compilam só com o público (nenhuma é amiga de ninguém).
- **Testes executados (offline).**
  - Core 164, Json 87, Account 185, Connection 208, Match 248, Facade 157 e Client.Proof 25: todos passaram;
  - Client.Scenes: 14 passaram; `SceneEdgeSurfaceTests` falha só fora do editor (`ReflectionTypeLoadException` de `UnityEngine.CoreModule`);
  - de `Anathema.Net.Unity.Tests`, rodados à parte: `SteppableMonotonicClockTests`, `DroppableWebSocketFactoryTests` e `ServerRoutesTests`. A assembly inteira trava fora do editor, como na 004; `CompositionSurfaceTests` também depende do motor.
- **Testes só compilados.** `Anathema.Net.Editor.Tests` e `Anathema.Config.Tests` (dependem de `UnityEditor`/`ScriptableObject`), `ClientCompositionTests` (`#if UNITY_EDITOR_WIN`), `LivePortsTests` e `MatchProofTests` (`[Explicit]`, precisa do backend).
- **Nenhuma regra de jogo (SC-011).** Revisão por `grep` em `Net/Facade`, `Client/` e `Presentation`: só há comparação de fase de conexão e de `LiveMatchStatus`. A `ProofStrategy` compara custo com energia apenas para escolher candidato, como a `SmokeStrategy` da 004; quem decide é o servidor.
- **Regressão corrigida no fim.** O gate de TLS do build de release (`EnvironmentSelectionReader`) e os testes dele procuravam `Assets/Scripts/Bootstrap/AppEnvManager.cs`, removido na US7. `EditorTestObjects` agora acha o `ClientHost` (mesmos campos `configDev`, `configProd`, `isProd`) e `AppEnvManagerFieldsTests` virou `ClientHostFieldsTests` (`git mv`). Exemplos de doc que citavam `AppEnvManager.Settings` em `AppConfig`, `AccountRoutes` e `ConnectionRoutes` foram atualizados.
- **Desvios do plano e das tarefas.**
  - `feed_listener_failed` (do `EventFeed`) substituiu `match_subscriber_failed`.
  - `TransportFailure` e `TransportFailureKind` ficaram internos (herdam da união interna de resultado HTTP); as propriedades `Transport` e `Renewal` também. O contrato da superfície foi corrigido junto.
  - `AppConfig.BuildAccountRoutes` e `BuildConnectionRoutes` ficaram internos; a borda usa `BuildServerRoutes`.
  - Nomes novos: `SessionExpiryWatch` (fachada) e `ClientComposition.CreateConsoleLog` (log antes de compor).
  - A cobertura tem 12 itens para os 11 comandos: o mulligan conta trocando e sem trocar.
  - Além das tarefas: `ProofFixtures` (visões e eventos das fixtures), `ProofWaitTests`, `ProofRun` (estado compartilhado pelos blocos de passos) e `ProofAssemblyInfo`, que abre a prova só para `Anathema.Client.Proof.Tests`.
  - `ProofPlayer.Dispose` chama `SignOut` para apagar a guarda do slot mesmo quando o roteiro falha no meio.
  - O `MatchProofRunner` liga cada cliente a um objeto filho e o destrói quando o passo 2 recompõe, para o passo do quadro não seguir no cliente descartado.
  - `Anathema.Client.Proof` tem `noEngineReferences: false` (runner e `ComposedClient.AttachTo`).
- **Arquivos `.meta`.** Os que faltavam (140, entre arquivos e pastas novos da 005) foram gerados por script com GUID novo; nenhum `.meta` órfão. A `MatchProofScene.unity` foi escrita à mão (runner com GUID do script e `AppConfig_Dev`) e fica fora de `EditorBuildSettings`. Conferir que o editor abre a cena sem aviso.
- **Em aberto.** Precisam do editor, do backend, do Multiplayer Play Mode ou do aparelho:
  - T119: suíte oficial pelo comando da constituição, tempo de `Anathema.Net.Facade.Tests` e o erro `CS0122` do quickstart §2;
  - T120: quickstart §4 com dois jogadores;
  - T121: quickstart §5 no Android.
- **Prova final (T115).** Passou no Test Runner do editor, com o backend local de `docker compose up` (2026-09-14). Tempo, envios e recusas por bot saem no log como `proof_finished` e `proof_bot`; não foram copiados para cá. Antes de passar, a prova contra o servidor achou três defeitos da estratégia dos bots, nenhum da fachada:
  - na volta da queda do passo 11, o bot é chamado de novo na mesma versão (`match_start` sem troca de versão, volta a ao vivo, recusa atrasada de um envio de antes da queda) e acusava travamento. Agora o último candidato (passar, confirmar, encerrar defesa) pode ser repetido 3 vezes na mesma versão, e a volta a ao vivo esquece os candidatos já tentados;
  - a partida 1 acabou por Nexus na rodada 7 sem nenhum feitiço sem alvo. Nova extensão em `contracts/match-proof.md`: com os seis itens de combate cobertos e "feitiço sem alvo" ainda não, ninguém declara ataque;
  - cada caso ganhou teste em `ProofStrategyTests` (27 testes na assembly da prova).
- **Multiplayer Play Mode (T120, parcial).** Os dois jogadores chegaram à partida (2026-09-15). Antes, dois problemas do ambiente:
  - o clone do Player 2 em `Library/VP/mppmbb3df59b` era de janeiro, com `Assets` e `ProjectSettings` apontando para o caminho antigo do projeto (`C:/Users/gabri/Projetos/Anathema/game`); o editor acusava versão desconhecida. O clone foi apagado e recriado;
  - o clone novo não achou a `HomeScene` na lista de cenas, embora leia o mesmo `ProjectSettings`. Contorno só de editor no `SceneRouter`: cena fora da lista carrega por `EditorSceneManager.LoadSceneInPlayMode` pelo caminho e registra `scene_route_outside_scene_list`; no build a lista vale sempre. A causa no clone não foi achada.
  - Falta a queda do servidor (`docker compose stop web` / `start`) sem recarregar a cena.
