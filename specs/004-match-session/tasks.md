---

description: "Task list for 004-match-session"
---

# Tasks: Partida

**Input**: Design documents from `specs/004-match-session/`

**Prerequisites**: [plan.md](./plan.md), [spec.md](./spec.md), [research.md](./research.md), [data-model.md](./data-model.md), [contracts/](./contracts/), [quickstart.md](./quickstart.md)

**Tests**: obrigatórios (constituição, princípio VII). Todo método novo ganha
teste EditMode. Escreva o teste antes da implementação: em Unity, "falhar" pode
significar não compilar porque o tipo ainda não existe.

**Organization**: tarefas agrupadas pelas histórias da spec (US1 a US7). A ordem
das fases segue as dependências: US5 (P2) vem antes de US7, porque o bot do marco
usa as dicas.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: pode rodar em paralelo com as outras [P] do mesmo bloco (arquivos
  diferentes, sem depender de tarefa incompleta do bloco)
- **[Story]**: US1 a US7 (histórias da spec)

## Convenções para todas as tarefas

Leia antes de executar qualquer tarefa:

- **Antes de escrever código**:
  - leia `CLAUDE.md`, `.specify/memory/constitution.md` e o contrato citado na
    tarefa (`specs/004-match-session/contracts/`);
  - formas do servidor: nunca copie para código nem para comentário; cite o
    caminho em `C:/Users/gabri/Projetos/dev_container/anathema/backend/specs/`
    (`009-match-protocol/contracts/client_messages.md`, `server_frames.md`,
    `refusal_codes.md`, `009-match-protocol/data-model.md`,
    `010-match-timers/contracts/server_frames.md`, `client_messages.md`) ou
    `server/apps/game/match/documents.py`.
- **Namespaces**: um por assembly, igual ao nome da asmdef
  (`Anathema.Net.Match`, `Anathema.Net.Connection`, `Anathema.Net.Account`,
  `Anathema.Net.Core`, `Anathema.Net.Unity`).
  - Testes: namespace testado + `.Tests`.
  - Subpastas (`Protocol/`, `Protocol/Events/`, `Mirror/`, …) **não** criam
    sub-namespace.
  - Código antigo (`MatchClient`, `MatchSession`, `PlayerSession`) continua no
    namespace global.
- **Todo arquivo novo**:
  - começa com `#nullable enable`;
  - tem um tipo público por arquivo;
  - métodos de 4 a 20 linhas, com no máximo 2 níveis de indentação;
  - `var` só quando o tipo aparece do lado direito.
- **Documentação e erros**:
  - membro público leva `/// <summary>` com a intenção e um `<example>`;
  - mensagem de exceção inclui o valor recebido e a forma esperada;
  - comentários existentes em arquivo evoluído são preservados.
- **Tipos**:
  - imutáveis, com `static Read(IPayloadReader)` no molde de
    `Assets/Scripts/Net/Connection/Matchmaking/MatchFoundFrame.cs`;
  - listas expostas como `IReadOnlyList<T>`;
  - identificadores sempre tipados (`UserId`, `CardInstanceId`, `CardId`,
    `MatchId`); nenhum membro chamado `id`.
- **Estado e assincronia** (research R4, R5):
  - espelho, relógio, pendente e sessão só na thread principal, sem trava,
    `Task.Run`, `async void`, `Task.Delay` ou timer;
  - `TurnClock` não tem temporizador;
  - `await` sem `ConfigureAwait(false)`.
- **Nenhuma regra de jogo** (FR-047):
  - nada compara custo com energia, conta cartas contra teto, decide se a fase
    aceita uma jogada, subtrai Nexus ou aplica dano;
  - `DisplayedUnitStats` e `HandCardHints` só informam.
- **Eventos de log**: nomes e campos da tabela em `contracts/live-match.md`
  ("Eventos de log"). Não invente outros sem acrescentar lá.
- **Testes**:
  - NUnit em `Assets/Tests/EditMode/Net.Match/<subpasta>/`;
  - nomes de método em português, no estilo de `ReconnectPolicyTests`;
  - assíncronos como `[Test] public async Task`;
  - fakes de `Anathema.Net.Fakes`, codec real
    (`NewtonsoftProtocolCodec(MatchFrames.CreateUnion(), log)`);
  - fakes criados no `[SetUp]`, nunca em campo inicializado (o NUnit reaproveita
    a instância do fixture);
  - tempo só pelo `FakeMonotonicClock.Advance` (+ `FakeFrameTicker.Tick()` quando
    a conexão precisa); nunca `Thread.Sleep`, exceto LiveServer.
- **`.meta`**: o Unity gera ao abrir o projeto; todo arquivo novo em `Assets/`
  (inclusive `.json` de fixture e pastas) vai para o commit com o seu `.meta`;
  arquivo removido sai com o `.meta`.
- **Comando da suíte** (editor fechado), chamado abaixo de "rodar a suíte":
  `"C:/Program Files/Unity/Hub/Editor/6000.2.8f1/Editor/Unity.exe" -batchmode -projectPath . -runTests -testPlatform EditMode -testResults Logs/editmode-results.xml`
  - Se o `Unity.exe` não abrir a partir da sessão, compile com o Roslyn do Unity e
    rode NUnit no Mono, como na 003 (registro em
    `specs/003-authenticated-socket-queue/tasks.md`).
  - Ao compilar por fora, passe a cada asmdef **só** as referências declaradas
    nela. A 003 escondeu um CS0012 passando referência a mais.
  - O mantenedor roda a suíte real.

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: assemblies e referências que tudo o resto usa.

- [X] T001 Criar `Assets/Scripts/Net/Match/Anathema.Net.Match.asmdef` (`rootNamespace` `Anathema.Net.Match`, referências `Anathema.Net.Core`, `Anathema.Net.Account`, `Anathema.Net.Connection`, `noEngineReferences: true`, `autoReferenced: true`) e `Assets/Scripts/Net/Match/MatchAssemblyInfo.cs` com `[assembly: InternalsVisibleTo("Anathema.Net.Match.Tests")]` e comentário no molde de `Assets/Scripts/Net/Connection/ConnectionAssemblyInfo.cs` (research R1)
- [X] T002 [P] Criar `Assets/Tests/EditMode/Net.Match/Anathema.Net.Match.Tests.asmdef` no molde de `Assets/Tests/EditMode/Net.Connection/Anathema.Net.Connection.Tests.asmdef`, com referências `UnityEngine.TestRunner`, `UnityEditor.TestRunner`, `Anathema.Net.Core`, `Anathema.Net.Json`, `Anathema.Net.Account`, `Anathema.Net.Connection`, `Anathema.Net.Match`, `Anathema.Net.Fakes`
- [X] T003 [P] Acrescentar `[assembly: InternalsVisibleTo("Anathema.Net.Match.Tests")]` em `Assets/Scripts/Net/Account/AccountAssemblyInfo.cs`, com comentário: os testes da partida montam `LoadedCatalog` pelo leitor interno (`specs/004-match-session/research.md` R13)
- [X] T004 [P] Acrescentar `Anathema.Net.Match` às referências de `Assets/Scripts/Net/Unity/Anathema.Net.Unity.asmdef` e de `Assets/Tests/EditMode/Net.Unity/Anathema.Net.Unity.Tests.asmdef`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: leitores compartilhados, base de comando, fixtures e rig que todas as histórias usam.

**⚠️ CRITICAL**: nenhuma história começa antes desta fase.

### Núcleo e conta

- [X] T005 [P] Acrescentar a `Assets/Tests/EditMode/Net.Json/PayloadIdentityExtensionsTests.cs` os casos de lista de `CardInstanceId`: lista vazia; `[3, 7]`; item negativo ou texto → falha com caminho `campo[índice]`; escrita de `[3, 7]` e de `[]`. Implementar `ReadCardInstanceIdList` em `Assets/Scripts/Net/Core/Protocol/PayloadIdentityReading.cs` e `WriteCardInstanceIdList` em `Assets/Scripts/Net/Core/Protocol/PayloadIdentityWriting.cs`, no molde de `ReadCardIdList`/`WriteCardIdList` (data-model, "Núcleo")
- [X] T006 [P] Escrever `Assets/Tests/EditMode/Net.Account/History/MatchEndReasonTextTests.cs` (`nexus_depleted`, `forfeit`, texto desconhecido → `Unknown`). Criar o público `Assets/Scripts/Net/Account/History/MatchEndReasonText.cs` com `static MatchEndReason Parse(string)`, movendo o dicionário de `Assets/Scripts/Net/Account/History/MatchHistoryRow.cs`, que passa a chamá-lo. `MatchHistoryTests` continua passando sem mudança (research R2)
- [X] T007 [P] Escrever `Assets/Tests/EditMode/Net.Account/Catalog/SpellDurationTextTests.cs` (`permanent`, `until_end_of_round`, desconhecido → `Unknown`). Criar o público `Assets/Scripts/Net/Account/Catalog/SpellDurationText.cs` com `static SpellDuration Parse(string)`, movendo o dicionário de durações de `Assets/Scripts/Net/Account/Catalog/SpellEffect.cs`, que passa a chamá-lo. `CatalogReaderTests` continua passando sem mudança (research R2)

### Base do protocolo de partida

- [X] T008 [P] Criar `Assets/Scripts/Net/Match/Protocol/MatchFrames.cs` com `static DiscriminatedUnion<ServerFrame> CreateUnion()` devolvendo, por enquanto, `ConnectionFrames.CreateUnion()`; os braços de partida entram em T029 (contracts/protocol-shapes.md)
- [X] T009 [P] Criar `Assets/Scripts/Net/Match/Commands/PlayCommand.cs`: abstrato, implementa `IOutgoingMessage`, construtor protegido com o `type` (texto vazio lança) e `ToString()` com o `type`. As 11 mensagens entram em T032 (data-model, "Comandos")
- [X] T010 [P] Escrever `Assets/Tests/EditMode/Net.Match/MatchAssemblyBoundaryTests.cs` no molde de `Assets/Tests/EditMode/Net.Connection/ConnectionAssemblyBoundaryTests.cs`, com `AssemblySignatureScanner` sobre `typeof(PlayCommand).Assembly`:
  - sem referências começando com `UnityEngine`, `UnityEditor`, `Newtonsoft`;
  - nenhum tipo usa `System.Net.WebSockets`, `UnityEngine`, `Newtonsoft.Json`;
  - não referencia `Anathema.Net.Json` nem `Anathema.Net.Unity`.

  (FR-046, SC-004; depende de T009)

### Ferramentas de teste

- [X] T011 [P] Criar `Assets/Tests/EditMode/Net.Match/MatchTestCatalog.cs` (interno): monta `LoadedCatalog` por `new CatalogReader(log).Read(codec.DecodeObject(json).Value)` e `new LoadedCatalog(cards, 1)`, com construtores de item `Unit(cardId, name, energy, attack, health)` e `Spell(cardId, name, energy, targetKind, declarationOnly)` no formato de `Assets/Tests/EditMode/Net.Account/Catalog/CatalogJson.cs`. Mais um `Default()` com as cartas usadas nas fixtures. Teste de fumaça `Assets/Tests/EditMode/Net.Match/MatchTestCatalogTests.cs`: `Find` de unidade e de feitiço (depende de T002, T003)
- [X] T012 [P] Criar `Assets/Tests/EditMode/Net.Match/MatchFixtures.cs` (interno, `static string Text(string name)` lendo `Path.Combine(Application.dataPath, "Tests/EditMode/Net.Match/Fixtures", name)`; arquivo ausente lança com o caminho). Criar as fixtures de contrato em `Assets/Tests/EditMode/Net.Match/Fixtures/`, montadas a partir dos exemplos de `009-match-protocol/contracts/server_frames.md`, `010-match-timers/contracts/server_frames.md`, `server/apps/game/match/player_view.py` e `documents.py`:
  - `contract-match-start-mulligan.json`: fase `mulligan`, prioridade e token nulos, `clock.turn` nulo, `mulligan_remaining_ms` 30000;
  - `contract-match-update-action.json`: fase `action`, eventos `passed`, `round_started`, `cards_drawn` própria e do oponente (`cards` vazia);
  - `contract-match-update-declaration.json`: `combat` com dois atacantes e `blocks` vazia; unidade com os três modificadores;
  - `contract-match-update-combat-blocked.json`: um par em `blocks`;
  - `contract-match-update-finished.json`: `outcome` `nexus_depleted`, `clock.turn` nulo;
  - `contract-match-update-all-events.json`: um evento de cada um dos 19 `kind`, na ordem do `data-model.md` da 009 e do contrato 010;
  - `contract-match-update-no-clock.json`: sem o campo `clock`;
  - `contract-turn-warning.json`.

  Teste `Assets/Tests/EditMode/Net.Match/MatchFixturesTests.cs`: cada arquivo é JSON objeto pelo codec (depende de T002)
- [X] T013 Criar `Assets/Tests/EditMode/Net.Match/MatchTestRig.cs` (interno), no molde de `Assets/Tests/EditMode/Net.Connection/ConnectionTestRig.cs`:
  - fakes: `FakeWebSocketFactory`, `FakeMonotonicClock`, `FakeFrameTicker`, `FakeNetworkReachability(LocalArea)`, `FakeClientLog`, `FakeAppLifecycle`, `MainThreadQueue`, `FakeAccessTokenSource`;
  - codec `NewtonsoftProtocolCodec(MatchFrames.CreateUnion(), Log)`, `ConnectionPorts` e `Catalog` de `MatchTestCatalog.Default()`;
  - `AuthenticatedConnection Connection()` com `ConnectionSettings` determinístico;
  - `MatchBase`, `OpenLatest()`, `Receive(string)`, `ReceiveFixture(string name)`, `Advance(TimeSpan)`, `SentTexts()`, `LogValue(event, field)`.

  (depende de T008, T011, T012)

**Checkpoint**: base pronta; a suíte roda com os testes das tarefas T005–T013.

---

## Phase 3: User Story 1 - Ler tudo o que o servidor manda na partida (Priority: P1) 🎯 MVP

**Goal**: cada frame do socket de partida vira tipo fechado e completo, com braço desconhecido em cada família; os comandos viram o JSON do contrato.

**Independent Test**: decodificar as fixtures de contrato pelo codec real e verificar cada campo, evento, modificador, fase e código; codificar cada comando e comparar com o JSON de `contracts/protocol-shapes.md`.

### Valores e visão

- [X] T014 [P] [US1] Escrever `Assets/Tests/EditMode/Net.Match/Protocol/MatchPhaseReadingTests.cs`: os 7 textos → enum e texto preservado; `"setup"` → `Unknown` com `PhaseText` `"setup"`. Criar `Assets/Scripts/Net/Match/Protocol/MatchPhase.cs` e o interno `Assets/Scripts/Net/Match/Protocol/MatchPhaseText.cs` (`Parse`) (FR-003, SC-002)
- [X] T015 [P] [US1] Criar `Assets/Scripts/Net/Match/Protocol/MatchProfile.cs` (`user_id`, `nickname`, `icon`, `level`) e `Assets/Scripts/Net/Match/Protocol/MatchCard.cs` (`card_instance_id`, `card_id`), com `Read`. Teste `Assets/Tests/EditMode/Net.Match/Protocol/MatchCardReadingTests.cs`: leitura; `card_id` faltando → `PayloadShapeException` com caminho (FR-004)
- [X] T016 [P] [US1] Escrever `Assets/Tests/EditMode/Net.Match/Protocol/UnitModifierUnionTests.cs`:
  - `attack` e `health` com `amount`;
  - `damage_immunity` sem `amount`;
  - `modifier_kind` `"poison"` → `UnrecognizedModifier` com `KindText` e duração;
  - duração `"until_end_of_turn"` → `SpellDuration.Unknown` com `DurationText`;
  - `attack` sem `amount` → falha com caminho.

  Criar em `Assets/Scripts/Net/Match/Protocol/`: `UnitModifier.cs` (abstrato), `AttackModifier.cs`, `HealthModifier.cs`, `DamageImmunityModifier.cs`, `UnrecognizedModifier.cs` e o interno `UnitModifiers.cs` (união por `modifier_kind`, duração por `SpellDurationText.Parse`). (US1-4, US1-5, FR-004; depende de T007)
- [X] T017 [US1] Criar em `Assets/Scripts/Net/Match/Protocol/`:
  - `BankUnit.cs` (`card`, `damage_taken`, `modifiers` pela união);
  - `SideView.cs` (abstrato), `OwnSideView.cs` (+ `hand`), `OpponentSideView.cs` (+ `hand_size`);
  - `BlockPair.cs`, `CombatView.cs`;
  - `MatchOutcome.cs` (motivo por `MatchEndReasonText.Parse`, com `ReasonText`);
  - `PlayerView.cs` (`match_id` por `ReadMatchId`; `priority_user_id`, `token_holder_user_id` por `ReadOptionalUserId`; `outcome` e `combat` por `ReadOptionalObject`).

  Seguem `data-model.md`, "Protocolo". (FR-002, FR-003; depende de T005, T006, T014–T016)
- [X] T018 [US1] Escrever `Assets/Tests/EditMode/Net.Match/Protocol/PlayerViewReadingTests.cs` sobre as fixtures `contract-match-start-mulligan.json`, `contract-match-update-declaration.json`, `contract-match-update-combat-blocked.json` e `contract-match-update-finished.json`, lendo `payload.view`:
  - US1-1: mão própria tipada, `hand_size`, banco, cemitério, `deck_size`, energia, Nexus, perfil e `mulligan_taken` dos dois lados; prioridade, token, combate e desfecho nulos no mulligan;
  - US1-6: combate com atacantes e pares; desfecho com `UserId` e motivo; motivo `"timeout"` → `Unknown` com `ReasonText`;
  - campo a mais ignorado; `you.nexus` faltando → falha com caminho `view.you.nexus`.

  (depende de T012, T017)
- [X] T019 [P] [US1] Escrever `Assets/Tests/EditMode/Net.Match/Protocol/ClockViewReadingTests.cs`: vez preenchida com os quatro campos; vez nula com mulligan numérico; os dois nulos; os dois preenchidos (lidos como vieram); objeto ausente → `ClockView.Empty`. Criar `Assets/Scripts/Net/Match/Protocol/TurnView.cs` e `Assets/Scripts/Net/Match/Protocol/ClockView.cs` com `static ClockView ReadOptional(IPayloadReader payload)` (FR-005)

### Eventos

- [X] T020 [US1] Criar em `Assets/Scripts/Net/Match/Protocol/`:
  - `EventDetail.cs`: construtores estáticos `OfUser`, `OfCard`, `OfInstance`, `OfInstances`, `OfCards`, `OfNumber`, `OfText`; exatamente um valor preenchido (research R10);
  - `MatchEvent.cs`: abstrato, com `KindText` e `IReadOnlyList<EventDetail> Details`;
  - `UnrecognizedMatchEvent.cs`: `Details` vazio;
  - o interno `MatchEvents.cs`: `DiscriminatedUnion<MatchEvent>` por `kind`, com o desconhecido e, por enquanto, nenhum braço.

  (depende de T015)
- [X] T021 [P] [US1] Criar os 11 eventos de jogada em `Assets/Scripts/Net/Match/Protocol/Events/`, cada um com `Read` e `Details` na ordem dos campos do contrato, registrados em `MatchEvents.cs`: `MulliganTakenEvent.cs`, `UnitPlayedEvent.cs`, `SpellCastEvent.cs` (alvo por `ReadOptionalCardInstanceId`), `PassedEvent.cs`, `AttackersSentEvent.cs` (lista por `ReadCardInstanceIdList`), `AttackerWithdrawnEvent.cs`, `AttackConfirmedEvent.cs`, `BlockerAssignedEvent.cs`, `BlockerRemovedEvent.cs`, `DefenseEndedEvent.cs`, `ForfeitedEvent.cs` (data-model, tabela de eventos; depende de T020)
- [X] T022 [P] [US1] Criar os 6 eventos de consequência e os 2 do relógio em `Assets/Scripts/Net/Match/Protocol/Events/`, registrados em `MatchEvents.cs`: `UnitDamagedEvent.cs`, `UnitDiedEvent.cs`, `NexusChangedEvent.cs`, `RoundStartedEvent.cs`, `CardsDrawnEvent.cs` (lista de `MatchCard` que pode vir vazia), `MatchFinishedEvent.cs` (lê `defeated_user_id` e `reason` direto no evento para um `MatchOutcome`), `TurnTimedOutEvent.cs`, `MulliganTimedOutEvent.cs` (depende de T020, T017)
- [X] T023 [US1] Escrever `Assets/Tests/EditMode/Net.Match/Protocol/MatchEventUnionTests.cs` sobre `contract-match-update-all-events.json`:
  - US1-2: 19 tipos na ordem, cada campo tipado;
  - `Details` de cada tipo com nomes de campo do contrato;
  - US1-3: `cards_drawn` do oponente com `Count` 1 e `Cards` vazia;
  - `kind` `"card_discarded"` → `UnrecognizedMatchEvent` com `KindText`;
  - `unit_played` sem `card` → falha com caminho.

  (SC-002; depende de T021, T022)

### Frames e união

- [X] T024 [US1] Criar em `Assets/Scripts/Net/Match/Protocol/`:
  - `MatchStartFrame.cs` (`match_start`: `version`, `view`, `clock` opcional);
  - `MatchUpdateFrame.cs` (`match_update`: + `events` pela união de `MatchEvents`, na ordem);
  - `TurnWarningFrame.cs` (`turn_warning`).

  Registrar os três em `Assets/Scripts/Net/Match/Protocol/MatchFrames.cs`. (FR-001; depende de T017, T019, T023)
- [X] T025 [US1] Escrever `Assets/Tests/EditMode/Net.Match/Protocol/MatchFramesTests.cs` e `Assets/Tests/EditMode/Net.Match/Protocol/TurnWarningFrameTests.cs` pelo codec real:
  - cada fixture de contrato decodifica no tipo certo;
  - `contract-match-update-no-clock.json` → `ClockView.Empty`;
  - `match_update` com `view.phase` faltando → `DecodeOutcome` inválido com caminho `payload.view.phase`, e nenhum frame parcial (FR-009);
  - US1-8: `turn_warning` tipado.

  (depende de T012, T024)
- [X] T026 [US1] Trocar em `Assets/Scripts/Net/Unity/LiveNetworkAdapters.cs` o codec para `new NewtonsoftProtocolCodec(MatchFrames.CreateUnion(), log)`, atualizando o comentário que cita a research R1 da 003 para citar também `specs/004-match-session/research.md` R3. Acrescentar a `Assets/Tests/EditMode/Net.Unity/LiveNetworkAdaptersTests.cs` o caso "codec decodifica `match_start`" (depende de T004, T024)

### Recusas e mensagens

- [X] T027 [P] [US1] Escrever `Assets/Tests/EditMode/Net.Match/Protocol/PlayRefusalReaderTests.cs`: US1-7, `[TestCase]` para os 31 códigos de `refusal_codes.md` → valor do enum; `"brand_new_code"` → `Unknown` com `CodeText`; `Error` preservado e nunca usado na decisão (mesmo código com dois textos diferentes → mesmo `Code`); `ProbableCommand` repassado. Criar em `Assets/Scripts/Net/Match/Refusals/`: `PlayRefusalCode.cs`, `PlayRefusal.cs`, o interno `PlayRefusalReader.cs` com `Read(MessageRefusedFrame refusal, PlayCommand? probable)` (FR-007, SC-002; depende de T009)
- [X] T028 [P] [US1] Criar as 11 mensagens em `Assets/Scripts/Net/Match/Commands/`, derivadas de `PlayCommand`, com construtores de `data-model.md` ("Comandos") e escrita por `WriteCardInstanceId`/`WriteCardInstanceIdList`: `MulliganCommand.cs`, `PlayUnitCommand.cs`, `CastSpellCommand.cs` (alvo nulo **omite** o campo, research R7), `PassCommand.cs`, `DeclareAttackCommand.cs`, `WithdrawAttackerCommand.cs`, `ConfirmAttackCommand.cs`, `AssignBlockerCommand.cs`, `RemoveBlockerCommand.cs`, `EndDefenseWindowCommand.cs`, `ForfeitCommand.cs` (FR-008; depende de T005, T009)
- [X] T029 [US1] Escrever `Assets/Tests/EditMode/Net.Match/Protocol/PlayCommandEncodingTests.cs`, com `codec.Encode` comparado a cada linha da tabela "Escrita" de `contracts/protocol-shapes.md` (US1/US4-1, SC-007). Escrever `Assets/Tests/EditMode/Net.Match/Protocol/CommandIdentityTests.cs`: por reflexão, nenhum construtor público de tipo derivado de `PlayCommand` tem parâmetro `CardId`, `long`, `int` ou lista deles (US4-7, research R7). Depende de T028

**Checkpoint**: US1 completa — todo frame de partida e toda mensagem de comando têm forma tipada e teste; a suíte passa.

---

## Phase 4: User Story 2 - Um espelho da partida que nunca volta no tempo (Priority: P1)

**Goal**: estado espelhado substituído por frame aceito, fatos prontos, avisos em ordem, atributos exibidos.

**Independent Test**: alimentar `VersionGate` e `MatchMirror` com visões decodificadas das fixtures e verificar estado, fatos e ordem dos avisos, sem conexão.

- [X] T030 [P] [US2] Escrever `Assets/Tests/EditMode/Net.Match/Mirror/VersionGateTests.cs`, uma linha da tabela de research R4 por caso: vazio aceita qualquer versão; update maior aceita; update igual e menor descartam; start igual → `ResyncSameVersion`; start menor descarta; `Judge` não muda `AppliedVersion`; `Record` muda. Criar `Assets/Scripts/Net/Match/Mirror/FrameVerdict.cs` e `Assets/Scripts/Net/Match/Mirror/VersionGate.cs` (FR-010)
- [X] T031 [P] [US2] Criar em `Assets/Scripts/Net/Match/Mirror/` os tipos de aviso `ViewReplaced.cs`, `PhaseChange.cs`, `PriorityChange.cs` e `MatchEnding.cs` (data-model, "Espelho")
- [X] T032 [US2] Escrever `Assets/Tests/EditMode/Net.Match/Mirror/MatchMirrorApplyTests.cs`:
  - US2-1: primeiro `Apply` → `Current`, `Version`, `Self` do perfil; `ViewReplaced` com anterior nulo;
  - US2-2: versão 7 depois da 4 sem intermediárias;
  - substituição inteira: banco do segundo frame sem a unidade do primeiro não mantém a unidade;
  - `Self` diferente entre frames → `match_self_changed` e fica o novo.

  Criar `Assets/Scripts/Net/Match/Mirror/MatchMirror.cs` com `Current`, `Version`, `Self` e `Apply` que substitui e emite `ViewReplaced`. (FR-011, FR-012; depende de T018, T031)
- [X] T033 [US2] Escrever `Assets/Tests/EditMode/Net.Match/Mirror/MatchMirrorAnnouncementTests.cs`:
  - US2-5: ordem `ViewReplaced` → 3 `EventReceived` na ordem → `PhaseChanged` → `PriorityChanged`;
  - sem mudança de fase ou prioridade → esses avisos não saem;
  - US2-7: fase `finished` → `MatchEnded` com desfecho e `Won`, uma vez; outro `Apply` em `finished` não repete;
  - assinante de `ViewReplaced` que lança → `match_subscriber_failed` registrado, os `EventReceived` seguintes saem e `Current` já é o novo.

  Completar `Assets/Scripts/Net/Match/Mirror/MatchMirror.cs` com os avisos, cada chamada protegida por um método privado de aviso seguro. (FR-014, FR-015; depende de T032)
- [X] T034 [US2] Escrever `Assets/Tests/EditMode/Net.Match/Mirror/MatchMirrorFactsTests.cs`:
  - US2-6: mulligan com `you.mulligan_taken` falso e `opponent` verdadeiro;
  - `IsMyPriority`/`AmTokenHolder` verdadeiros e falsos, e falsos com nulos;
  - `Phase` nulo antes do primeiro `Apply`;
  - `IsFinished`;
  - `DidIWin` nulo sem desfecho, verdadeiro com o oponente derrotado, falso com o próprio.

  Completar os fatos em `Assets/Scripts/Net/Match/Mirror/MatchMirror.cs`. (FR-013; depende de T033)
- [X] T035 [P] [US2] Escrever `Assets/Tests/EditMode/Net.Match/Mirror/DisplayedUnitStatsTests.cs`:
  - US2-8: base 3/4, +3 ataque, +1 vida, 2 de dano → 6 e 3;
  - imunidade não altera número;
  - dois modificadores de ataque somam;
  - modificador desconhecido ignorado;
  - `CardId` de feitiço ou ausente → nulo.

  Criar `Assets/Scripts/Net/Match/Mirror/DisplayedUnitStats.cs`, com `/// <summary>` dizendo "conveniência de exibição; nada decide por ele". (FR-016; depende de T011, T017)

**Checkpoint**: US2 completa — espelho testado sem socket.

---

## Phase 5: User Story 3 - Um relógio da vez que se desenha sozinho e não decide nada (Priority: P1)

**Goal**: restante da vez e do mulligan a partir de `remaining_ms` e da chegada; vez nova e aviso único; nada em zero.

**Independent Test**: `TurnClock` sobre `FakeMonotonicClock`, ancorado com `ClockView` e `TurnWarningFrame` decodificados, verificando restante e avisos.

- [X] T036 [US3] Escrever `Assets/Tests/EditMode/Net.Match/Clock/TurnClockTests.cs`:
  - US3-1: âncora aos 10.000 ms com 25.000 → restante 22.000 aos 13.000 e 0 aos 40.000, sem nenhum aviso;
  - US3-2: vez 12 → 13 emite `TurnStarted` com número e dono; vez 12 → 12 com restante menor só reancora;
  - US3-7: vez nula apaga `Turn` e `TurnRemaining`, sem aviso;
  - avisos só saem em `ClockAnnouncements.Raise()`, não em `Anchor`.

  Criar `Assets/Scripts/Net/Match/Clock/ClockAnnouncements.cs` (interno) e `Assets/Scripts/Net/Match/Clock/TurnClock.cs`, com `Turn`, `TurnRemaining`, `TurnStarted` e `Anchor`. (FR-017, FR-018, FR-022; depende de T019)
- [X] T037 [US3] Escrever `Assets/Tests/EditMode/Net.Match/Clock/TurnWarningTests.cs`:
  - US3-3: `turn_warning` da vez 12 → `TurnRunningOut(12)` uma vez; repetido → nada; vez 11 → `turn_warning_ignored`, nada muda;
  - sem vez desenhada → ignorado;
  - `turn_warning` da vez reancora com o `remaining_ms` dele;
  - frame com `warning` verdadeiro → `TurnRunningOut` em `Raise`; seguido de `turn_warning` da mesma vez → não repete.

  Completar `Assets/Scripts/Net/Match/Clock/TurnClock.cs` com `TurnRunningOut` e `NoteWarning`. (FR-019; depende de T024, T036)
- [X] T038 [US3] Escrever `Assets/Tests/EditMode/Net.Match/Clock/MulliganClockTests.cs`:
  - US3-4: 30.000 ms e +12.000 → 18.000, com `Turn` nulo;
  - prazo nulo em âncora seguinte → `MulliganRemaining` nulo;
  - nunca negativo.

  Completar `MulliganRemaining` em `Assets/Scripts/Net/Match/Clock/TurnClock.cs`. (FR-020; depende de T037)

**Checkpoint**: US3 completa — relógio testado só com tempo falso.

---

## Phase 6: User Story 4 - Jogar por comandos e entender as recusas (Priority: P1)

**Goal**: comandos que só saem conectados, pendente para a apresentação, recusa associada ao último comando.

**Independent Test**: `MatchCommands` sobre a conexão do `MatchTestRig`, verificando textos enviados, resultado fora de "conectado" e o ciclo do `PendingPlay`.

- [X] T039 [P] [US4] Escrever `Assets/Tests/EditMode/Net.Match/Commands/PendingPlayTests.cs`, um teste por linha da tabela `PendingPlay` de `contracts/live-match.md`, inclusive `CurrentChanged` a cada mudança e `Unmark` de um comando que já não é o atual (não mexe). Criar `Assets/Scripts/Net/Match/Commands/PendingPlay.cs` (US4-3, US4-4, FR-025; depende de T009)
- [X] T040 [P] [US4] Criar `Assets/Scripts/Net/Match/Commands/PlaySendStatus.cs` e `Assets/Scripts/Net/Match/Commands/PlaySendResult.cs` (data-model, "Comandos")
- [X] T041 [US4] Escrever `Assets/Tests/EditMode/Net.Match/Commands/MatchCommandsTests.cs` sobre a conexão do rig:
  - US4-1: com socket aberto, cada um dos 12 métodos manda o texto da tabela de `protocol-shapes.md` e devolve `Sent`;
  - US4-2 (`[TestCase]`): `Disconnected`, `Connecting`, `WaitingRetry`, `RenewingToken`, `Suspended` e `GaveUp` → `NotConnected` com a fase; nada em `SentTexts`; `Pending.Current` nulo; `match_command_not_sent`; ao reconectar, nada é mandado;
  - `NextSendOutcome = Failed` → `SocketFailed` e pendente desmarcado;
  - `Send(PlayCommand)` igual ao método nomeado.

  Criar `Assets/Scripts/Net/Match/Commands/MatchCommands.cs` com o construtor interno de `contracts/live-match.md`. (FR-023, FR-024, SC-007; depende de T013, T028, T039, T040)
- [X] T042 [US4] Acrescentar a `Assets/Tests/EditMode/Net.Match/Protocol/CommandIdentityTests.cs` o caso "nenhum parâmetro público de `MatchCommands` é `CardId`, `long`, `int` ou lista deles" (US4-7; depende de T041)

A associação recusa → comando (US4-5, US4-6) é provada pela sessão, em T047.

**Checkpoint**: US4 completa — comandos e pendente testados; forma da recusa testada em US1.

---

## Phase 7: User Story 6 - Uma sessão de partida que atravessa quedas e termina sozinha (Priority: P1)

**Goal**: `LiveMatch` junta conexão, espelho, relógio, comandos e narrador; estados conectando → ao vivo → reconectando → ao vivo, recusada, desistiu, terminada; o código de cena passa a usá-la.

**Independent Test**: `LiveMatch` sobre o `MatchTestRig`, com fixtures entrando pelo `FakeWebSocket`, verificando estados, avisos, fechamento do socket e linhas do narrador.

### Sessão

- [X] T043 [P] [US6] Criar `Assets/Scripts/Net/Match/Session/LiveMatchPhase.cs` e `Assets/Scripts/Net/Match/Session/LiveMatchStatus.cs` (igualdade por valor; `ToString` com fase, `stale` e motivo) (data-model, "Sessão")
- [X] T044 [US6] Escrever `Assets/Tests/EditMode/Net.Match/Session/LiveMatchConnectionTests.cs`:
  - US6-1: `Start` abre a URL com `matchId` e fica `Connecting`; `Connected` sem `match_start` continua `Connecting`; `match_start` → `Live` e espelho preenchido;
  - US6-2: queda → `Reconnecting` com `IsStale`, `Mirror.Current` ainda legível, comando devolve `NotConnected`; nova abertura + `match_start` versão maior → `Live` sem `IsStale`;
  - US6-3: `match_denied` + 4404 → `Refused` com `GiveUpReason.MatchRefused(MatchNotFound)` e nenhuma abertura depois;
  - US6-4: renovação que responde sessão expirada → `GaveUp` com o motivo, espelho legível;
  - segundo `Start` lança;
  - `match_status` registrado a cada mudança.

  Criar `Assets/Scripts/Net/Match/Session/LiveMatch.cs` com o construtor de `contracts/live-match.md`, `Start`, a tradução de `StatusChanged` da conexão (research R9) e a entrada de `MatchStartFrame` pelo `VersionGate`. (FR-031 a FR-033, FR-037; depende de T013, T030, T034, T038, T041, T043)
- [X] T045 [US6] Escrever `Assets/Tests/EditMode/Net.Match/Session/LiveMatchVersionTests.cs`:
  - US2-3: `match_update` 6 e 7 depois do 7 → nada, nenhum aviso, `match_frame_discarded`;
  - US2-4: `match_start` 7 de reconexão → estado não trocado, sem `ViewReplaced`, sessão `Live`; `match_start` 9 → trocado;
  - US3-5: frame descartado não muda `Clock.TurnRemaining`;
  - US3-6: `match_start` 7 com 17.000 depois de 8 s → reancora nos 17.000 a partir da chegada;
  - buraco de versão (`turn_warning` + `match_update` 9 depois do 7) aceito;
  - ordem de research R4: dentro de `ViewReplaced`, `Clock.Turn` já é a vez nova; `TurnStarted` sai depois do último aviso do espelho;
  - atualização aceita limpa `Pending.Current` e `LastSentSinceUpdate`;
  - SC-005: sequência embaralhada de versões nunca expõe versão menor que uma já exposta.

  Completar `Assets/Scripts/Net/Match/Session/LiveMatch.cs` com `MatchUpdateFrame` e `TurnWarningFrame`. (FR-010, FR-021, SC-005, SC-006; depende de T044)
- [X] T046 [US6] Escrever `Assets/Tests/EditMode/Net.Match/Session/LiveMatchFinishTests.cs`:
  - US6-5: `contract-match-update-finished.json` → `MatchEnded`, `Finished` com desfecho, `CloseRequests` 1 e nenhuma nova abertura nem `Reconnecting`;
  - queda e `match_start` já em `finished` → `Finished` direto;
  - frame depois de `Finished`, `Refused` ou `GaveUp` → ignorado;
  - `Dispose` em `Live` fecha o socket, e nenhum aviso sai depois (nem de frame já na fila);
  - `Recovered` limpa `Pending.Current`.

  Completar `Assets/Scripts/Net/Match/Session/LiveMatch.cs`. (FR-034, FR-035; depende de T045)
- [X] T047 [US6] Escrever `Assets/Tests/EditMode/Net.Match/Session/LiveMatchRefusalTests.cs`:
  - US4-5: `play_unit` enviado + `not_enough_energy` → `Refused` com código, `Error` e o `PlayUnitCommand` como provável; `Pending.Current` nulo; `match_play_refused` com os campos;
  - US4-6: recusa sem comando desde a última atualização → provável nulo;
  - recusa depois de `Recovered` ainda aponta o comando;
  - FR-027: recusa não muda `Mirror.Current`, `Clock.Turn` nem `Status`.

  Completar `Assets/Scripts/Net/Match/Session/LiveMatch.cs` com `MessageRefusedFrame` e `match_frame_unexpected` para outro frame. (FR-026, FR-027; depende de T027, T046)
- [X] T048 [US6] Escrever `Assets/Tests/EditMode/Net.Match/Session/MatchNarratorTests.cs`:
  - US6-6: `unit_played` da carta 5 do jogador `"gabriel"` → uma entrada `match_event` com `match_id`, `round`, `kind`, `user_id=gabriel`, `card=NOME#instância`;
  - `contract-match-update-all-events.json` → uma entrada por evento, na ordem;
  - lista de atacantes separada por vírgula;
  - `UserId` fora dos perfis e `CardId` fora do catálogo → `ToString()` tipado;
  - evento desconhecido → `kind` e `unrecognized=true`.

  Criar o interno `Assets/Scripts/Net/Match/Session/MatchNarrator.cs` e ligá-lo em `LiveMatch.cs`. (FR-036; depende de T047)
- [X] T049 [US6] Acrescentar `DisplayedUnitStats? StatsOf(BankUnit unit)` a `Assets/Scripts/Net/Match/Session/LiveMatch.cs`, com teste em `Assets/Tests/EditMode/Net.Match/Session/LiveMatchConnectionTests.cs` (delegação com o catálogo da sessão) (FR-016; depende de T035, T048)

### Código anterior (research R12, contracts/legacy-bridge.md)

- [X] T050 [US6] Remover `RawTextReceived` de `Assets/Scripts/Net/Connection/Connection/AuthenticatedConnection.cs` (evento, comentário de ponte e repasse em `OnFrame`) e o texto de `FrameArrived` em `Assets/Scripts/Net/Connection/Connection/SocketAttempt.cs`. Remover `TextoCruAcompanhaCadaFrameAceito` de `Assets/Tests/EditMode/Net.Connection/ConnectionFrameDeliveryTests.cs`. O comentário "quem assina pode ter saído dentro do aviso" vai para o ponto que continua fazendo sentido, ou sai se não restar ponto. `Anathema.Net.Connection.Tests` passa (FR-042)
- [X] T051 [US6] Reescrever `Assets/Scripts/Core/Network/WebSocketClient/MatchClient.cs` conforme `contracts/legacy-bridge.md` ("`MatchClient`"):
  - construtor com `CardCatalog` e `IMonotonicClock`; `LiveMatch? Live`;
  - `Connect(MatchId)` dispara `ConnectAsync`: catálogo → descarta a anterior → `MatchSession.Instance.Attach` → primeiro `ViewReplaced` carrega `MatchScene` se não ativa → `Start`;
  - `match_catalog_unavailable` em falha de catálogo;
  - saem `RawTextReceived`, `JsonConvert`, `MatchStartEnvelope`, `HandleStartMatch` e `using Newtonsoft.Json`;
  - o comentário de idempotência do `match_start` é preservado junto do carregamento de cena.

  (FR-042, FR-044; depende de T048, T050)
- [X] T052 [US6] Evoluir `Assets/Scripts/Core/Session/MatchSession.cs` conforme `contracts/legacy-bridge.md` ("`MatchSession`"): `Attach(LiveMatch)`, `Live`, `State` como `PlayerView?`, `HasState`, `OnStateChanged(PlayerView)` repassando `Mirror.ViewReplaced`, `Clear()` desfazendo a ligação; sai `ApplyState`; `#nullable enable`; comentários de classe preservados (FR-043; depende de T051)
- [X] T053 [US6] Em `Assets/Scripts/Core/Session/PlayerSession.cs`, `ComposeSocketClients` passa `Account.Catalog` e `adapters.Clock` ao `MatchClient` (FR-045; depende de T051)
- [X] T054 [US6] Apagar `Assets/Scripts/DTO/Match/MatchStateDTO.cs`, o `.meta` e a pasta `Assets/Scripts/DTO/Match` com o `.meta`. Conferir com `grep -rn "MatchStateDTO\|RawTextReceived" Assets --include=*.cs` (sem saída) (FR-042, SC-003; depende de T051, T052)

**Checkpoint**: US6 completa — sessão testada com fakes; cenas compilam sobre a sessão nova; a ponte saiu.

---

## Phase 8: User Story 5 - Dicas de mira sem decidir por ninguém (Priority: P2)

**Goal**: dica por cópia da mão (tipo, custo e energia, alvo, candidatos, só na declaração) sem esconder nem bloquear jogada.

**Independent Test**: `HandCardHints.For` com `MatchTestCatalog` e visões conhecidas, e verificação por reflexão de que comandos não leem dicas.

- [X] T055 [US5] Escrever `Assets/Tests/EditMode/Net.Match/Hints/HandCardHintsTests.cs`:
  - US5-1: unidade custo 3, energia 2 → `Unit`, 3 e 2, sem alvo;
  - US5-2: feitiço sem alvo → `Spell`, `None`, candidatos vazios;
  - US5-3: aliado com duas unidades próprias → as duas na ordem do banco;
  - US5-4: inimigo com banco vazio → lista vazia;
  - US5-5: só declaração fora da declaração → `DeclarationOnly` verdadeiro e `Phase` `Action`;
  - US5-6: `CardId` fora do catálogo → `UnknownCard` e `hint_card_unknown`;
  - cópia fora da mão → `NotInHand`;
  - alvo desconhecido → candidatos vazios.

  Criar em `Assets/Scripts/Net/Match/Hints/`: `HintCardKind.cs`, `HandCardHint.cs`, `HandCardHints.cs` (log recebido como parâmetro opcional de `For` ou por sobrecarga interna usada pela sessão). (FR-028, FR-029; depende de T011, T017)
- [X] T056 [US5] Acrescentar `HandCardHint HintFor(CardInstanceId card)` a `Assets/Scripts/Net/Match/Session/LiveMatch.cs`, com teste em `Assets/Tests/EditMode/Net.Match/Session/LiveMatchConnectionTests.cs`: antes do primeiro `match_start` → `NotInHand`; depois → dica da visão atual (depende de T049, T055)
- [X] T057 [US5] Escrever `Assets/Tests/EditMode/Net.Match/CommandsIgnoreHintsTests.cs` (por reflexão, nenhum campo de `MatchCommands` e de `PendingPlay` tem tipo `MatchMirror`, `LoadedCatalog`, `HandCardHint`, `DisplayedUnitStats` ou `TurnClock`) e `Assets/Tests/EditMode/Net.Match/MatchCoreIsolationTests.cs` (nenhum construtor de `MatchMirror`, `TurnClock`, `VersionGate`, `HandCardHints`, `DisplayedUnitStats`, `PlayRefusalReader` e dos tipos de `Protocol/` recebe tipo do assembly `Anathema.Net.Connection`) (FR-030, SC-008; depende de T041, T055)

**Checkpoint**: US5 completa — dicas testadas; garantia de que comando não consulta dica.

---

## Phase 9: User Story 7 - Dois clientes headless jogam uma partida inteira (Priority: P1)

**Goal**: o marco jogável contra o backend local, com queda no meio, desfechos iguais e o log contando a partida.

**Independent Test**: `LiveMatchTests` com `docker compose up` do backend (quickstart §2).

- [X] T058 [P] [US7] Criar `Assets/Tests/EditMode/Net.Unity/LiveServer/RecordingWebSocketFactory.cs` (interno, `#if UNITY_EDITOR_WIN` como `LivePlayer`). Embrulha um `IWebSocketFactory`; cada socket criado grava cada texto recebido em `Logs/match-recording/<rótulo>/NNNN-<type>.json`, com `type` lido por busca de texto simples, sem Newtonsoft. Teste `Assets/Tests/EditMode/Net.Unity/RecordingWebSocketFactoryTests.cs` sobre `FakeWebSocketFactory`, com pasta temporária (research R14)
- [X] T059 [US7] Evoluir `Assets/Tests/EditMode/Net.Unity/LiveServer/LivePlayer.cs`:
  - fábrica de socket opcional para gravação;
  - `Task<DeckId> CreateSpellDeckAsync()` com os `card_id` de `spell_deck_id` do `scripts/smoke_match.py` (1–8 ×3, 9, 1001–1005 ×3), pela criação de deck de `PlayerDecks`;
  - `Task<LoadedCatalog> LoadCatalogAsync()`;
  - `LiveMatch OpenMatch(MatchId, LoadedCatalog)` sobre `Connections.MatchConnection` e `Connections.Routes.Match`.

  (depende de T004, T056, T058)
- [X] T060 [P] [US7] Criar `Assets/Tests/EditMode/Net.Unity/LiveServer/SmokeStrategy.cs` (interno). `PlayCommand? Next(LiveMatch match, string label, ISet<string> tried)` segue `act` e os candidatos de `scripts/smoke_match.py`, usando só `Mirror`, `HintFor` e o catálogo:
  - **mulligan**: P1 troca a primeira carta, P2 nenhuma, uma vez, respeitando `MyMulliganPending`;
  - **fora do mulligan**: sem prioridade, nada; rodada ≥ 30, `ForfeitCommand`;
  - **`action`**: feitiços não só de declaração com alvo quando há candidato (poção só com Nexus < 12) → declarar com o banco inteiro se dono do token não consumido → unidade mais barata pelo custo do catálogo → passar;
  - **`declaration`**: puxar o último uma vez com ≥ 2 atacantes → feitiço só de declaração → confirmar;
  - **`combat`**: bloquear o primeiro atacante com a primeira unidade se não há bloqueio → encerrar defesa;
  - chave `versão|type|texto codificado` em `tried`; nenhum candidato novo → lança com fase e versão.

  Testes `Assets/Tests/EditMode/Net.Unity/SmokeStrategyTests.cs` sobre fixtures lidas por caminho (um por fase e o de rodada 30). (research R14; depende de T056)
- [X] T061 [US7] Criar `Assets/Tests/EditMode/Net.Unity/LiveServer/SmokeBot.cs` (interno): assina `LiveMatch.Mirror.ViewReplaced` e `LiveMatch.Refused`, chama `SmokeStrategy.Next` e manda por `Commands.Send`; conta envios por tipo e recusas; guarda o desfecho (depende de T059, T060)
- [X] T062 [US7] Criar `Assets/Tests/EditMode/Net.Unity/LiveServer/LiveMatchTests.cs` (`[Explicit]`, `[Category("LiveServer")]`, `#if UNITY_EDITOR_WIN`, `[UnityTest]`, laço `Drain` + `Tick` como `LiveQueueTests`, limite de 600 s):
  - US7-1: duas contas, catálogo, deck com feitiços, fila, mesmo `MatchId`, uma `LiveMatch` cada;
  - US7-2 a US7-5: bots jogam;
  - US7-6: na rodada 3, `Reachability.SimulateKind(CarrierData)` no P2 → espera `Reconnecting` e depois `Live`;
  - US7-7: os dois `Finished` com desfechos iguais; ≤ 20 recusas por bot, nenhuma `UnknownMessageType`; `CloseRequests` pela saída de propósito; no log de P1, uma `match_event` por evento aceito; nenhum `connection_gave_up`;
  - US7-8: P1 grava por `RecordingWebSocketFactory`.

  (FR-038 a FR-041, SC-009; depende de T061)
- [ ] T063 [US7] Rodar o marco pelo quickstart §2 com o backend no ar e anotar o resultado em "Registro da execução" no fim deste arquivo. Se o `Unity.exe` não abrir a partir da sessão, marcar como pendente para o mantenedor (depende de T062)
- [ ] T064 [US7] Copiar frames gravados por uma rodada do marco para `Assets/Tests/EditMode/Net.Match/Fixtures/recorded-*.json`, pela lista do quickstart §3. Escrever `Assets/Tests/EditMode/Net.Match/Protocol/RecordedFramesTests.cs`: cada `recorded-*.json` decodifica como frame válido e, se é `match_update`, sem `UnrecognizedMatchEvent`. Sem gravação disponível, o teste passa sem casos e a tarefa fica pendente em "Registro da execução" (depende de T063)

**Checkpoint**: US7 completa — o marco jogável provado contra o servidor real.

---

## Phase 10: Polish & Cross-Cutting Concerns

**Purpose**: verificação final, vault, fronteiras e documentação.

- [X] T065 [P] Conferir fronteira e remoções pelo quickstart §5: `grep -rln "JsonConvert\|using Newtonsoft" Assets/Scripts --include=*.cs | grep -v "^Assets/Scripts/Net/Json/"` e `grep -rn "MatchStateDTO\|RawTextReceived" Assets --include=*.cs` sem saída. Conferir também que `Anathema.Config.asmdef` e `Anathema.Config.Tests.asmdef` compilam sem CS0012; se pedirem, acrescentar `Anathema.Net.Match` com o motivo no commit (SC-003, SC-004)
- [X] T066 [P] Revisar `Assets/Scripts/Net/Match/` pelo quickstart §6 (nenhuma regra de jogo) e registrar o resultado em "Registro da execução" (SC-008)
- [X] T067 [P] Atualizar `C:/Users/gabri/Obsidian/Projetos/Anathema/Game/TODO.md` conforme `contracts/legacy-bridge.md` ("`Game/TODO.md`"): marcar feita a ponte de `match_start`, reescrever os singletons de cena e criar a seção da feature 004 com os três itens
- [ ] T068 Rodar a suíte (quickstart §1): tudo verde, nenhum aviso novo de `Assets/Scripts/`, `Anathema.Net.Match.Tests` abaixo de 5 s; conferir `.meta` de todo arquivo e pasta novos em `Assets/` (SC-001, SC-011; depende de todas as fases)
- [ ] T069 Validar com o Multiplayer Play Mode pelo quickstart §4 (Jogar → `VersusScene` → `MatchScene`, queda sem recarregar cena) e anotar em "Registro da execução"; pendente para o mantenedor se o editor não abrir a partir da sessão (SC-010; depende de T068)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: sem dependências.
- **Foundational (Phase 2)**: depende do Setup; bloqueia todas as histórias.
- **US1 (Phase 3)**: depende da fundação.
- **US2 (Phase 4)**: depende de US1 (visão decodificada).
- **US3 (Phase 5)**: depende de US1 (`ClockView`, `TurnWarningFrame`); independente de US2.
- **US4 (Phase 6)**: depende de US1 (mensagens); independente de US2 e US3.
- **US6 (Phase 7)**: depende de US2, US3 e US4.
- **US5 (Phase 8)**: `HandCardHints` depende só de US1; `HintFor` e os testes de reflexão dependem de US6 e US4.
- **US7 (Phase 9)**: depende de US5 e US6.
- **Polish (Phase 10)**: depende de todas.

### User Story Dependencies

```text
Setup → Foundational → US1 ─┬─► US2 ─┐
                            ├─► US3 ─┼─► US6 ─► US5 (HintFor) ─► US7 ─► Polish
                            └─► US4 ─┘            ▲
                            └─► US5 (HandCardHints, T055) ─┘
```

### Within Each User Story

- Teste antes da implementação; o teste falha (ou não compila) antes.
- Tipos de valor antes de quem os usa.
- Tarefas que editam `LiveMatch.cs` (T044–T049, T056) em sequência, numa frente só.
- Commit ao fim de cada tarefa ou grupo lógico, com os `.meta`.

### Parallel Opportunities

- Setup: T002, T003, T004 em paralelo depois de T001.
- Fundação: T005–T012 em paralelo; T013 depois de T008, T011, T012.
- US1: T014, T015, T016, T019, T027, T028 em paralelo; T021 e T022 em paralelo depois de T020.
- Depois de US1: US2 (T030–T035), US3 (T036–T038), US4 (T039–T042) e T055 (US5) em frentes paralelas.
- US7: T058 e T060 em paralelo.
- Polish: T065, T066, T067 em paralelo.

---

## Parallel Example: User Story 1

```bash
# Valores e mensagens de US1, juntos:
Task: "MatchPhaseReadingTests + MatchPhase em Assets/Scripts/Net/Match/Protocol/"
Task: "UnitModifierUnionTests + modificadores em Assets/Scripts/Net/Match/Protocol/"
Task: "ClockViewReadingTests + TurnView/ClockView em Assets/Scripts/Net/Match/Protocol/"
Task: "PlayRefusalReaderTests + recusas em Assets/Scripts/Net/Match/Refusals/"
Task: "11 mensagens em Assets/Scripts/Net/Match/Commands/"

# Eventos, depois de T020:
Task: "11 eventos de jogada em Assets/Scripts/Net/Match/Protocol/Events/"
Task: "6 consequências + 2 do relógio em Assets/Scripts/Net/Match/Protocol/Events/"
```

## Parallel Example: depois de US1

```bash
Task: "US2 — VersionGate e MatchMirror em Assets/Scripts/Net/Match/Mirror/"
Task: "US3 — TurnClock em Assets/Scripts/Net/Match/Clock/"
Task: "US4 — PendingPlay e MatchCommands em Assets/Scripts/Net/Match/Commands/"
Task: "US5 — HandCardHints em Assets/Scripts/Net/Match/Hints/ (T055)"
```

---

## Implementation Strategy

### MVP First (User Story 1)

1. Phase 1 e Phase 2.
2. Phase 3 (US1): todo frame de partida tipado, todo comando com o JSON do contrato.
3. **Parar e validar**: suíte verde; fixtures de contrato decodificam.

### Incremental Delivery

1. Setup + fundação → base pronta.
2. US1 → formas (MVP).
3. US2, US3, US4 → espelho, relógio e comandos, cada um testado sozinho.
4. US6 → sessão e código de cena religado; a ponte sai (primeira entrega visível: `MatchScene` pela sessão nova).
5. US5 → dicas.
6. US7 → o marco jogável contra o servidor.
7. Polish → fronteiras, vault, suíte, Multiplayer Play Mode.

### Parallel Team Strategy

- Depois de US1: uma frente em US2, outra em US3, outra em US4 + T055.
- US6 numa frente só (um arquivo central).
- US7 depois de US6: estratégia (T060) e gravador (T058) em paralelo.

---

## Notes

- **O código de cena não tem teste próprio.** `MatchClient`, `MatchSession` e
  `PlayerSession` ficam em `Assembly-CSharp`, que nenhuma asmdef de teste
  referencia. O comportamento está em `Anathema.Net.Match`, e o fluxo é validado
  pelo marco (T062) e pelo quickstart §4 (T069).
- `BaseClient`, `MatchmakingClient`, `ReconnectOverlay`, `VersusContext`,
  `PlayButton`, `Domain/Match/MatchController.cs` e `PlayerPublicDTO` não são
  editados.
- A `MatchScene` não ganha desenho nenhum nesta feature.
- Limitações do backend (research R11 da 003) não ganham contorno em nenhuma
  tarefa.
- Se um contrato do backend e esta lista discordarem, vale o contrato.

## Registro da execução (2026-09-14)

- **Verificação offline.** O `Unity.exe` não abre a partir desta sessão, então a suíte oficial não rodou. No lugar dela:
  - todas as assemblies foram compiladas com o Roslyn do Unity 6000.2.8f1, cada uma só com as referências declaradas na própria asmdef: Core, Json, Account, Fakes, Connection, Match, Unity, Config, Editor, `Assembly-CSharp` e as de teste (Json, Account, Connection, Match, Unity, Config, Editor);
  - os testes rodaram no Mono do Unity pelo NUnit do pacote.
- **Compilação.** Nenhum erro e nenhum aviso novo. Os avisos CS0649 de `AppEnvManager`, `VersusController` e `MiniPlayerProfile` já existiam. `Anathema.Config` e `Anathema.Config.Tests` compilaram sem `Anathema.Net.Match` (sem CS0012).
- **Testes executados.**
  - `Anathema.Net.Json.Tests`, `Anathema.Net.Account.Tests`, `Anathema.Net.Connection.Tests` e `Anathema.Net.Match.Tests`: 727 passaram, 0 falharam (248 deles da partida).
  - De `Anathema.Net.Unity.Tests`, compilados à parte e rodados: `RecordingWebSocketFactoryTests` e `SmokeStrategyTests`, 11 passaram. A assembly inteira trava fora do editor (testes de `UnityWebRequest` e de socket real).
- **Testes só compilados.** O caso novo de `LiveNetworkAdaptersTests` (codec com `turn_warning`) e `LiveMatchTests` (`[Explicit]`, precisa do backend).
- **Remoções e fronteira (quickstart §5).** `grep` sem saída para `MatchStateDTO`, `RawTextReceived` e `JsonConvert`/`using Newtonsoft` fora de `Assets/Scripts/Net/Json/`. `MatchStateDTO.cs`, o `.meta` e a pasta `DTO/Match` saíram por `git rm` (a remoção já está no índice).
- **Nenhuma regra de jogo (quickstart §6).** Revisão por `grep` em `Assets/Scripts/Net/Match/`: nada compara custo com energia, confere teto de banco ou mão, altera Nexus ou decide fase. A única conta é a de `DisplayedUnitStats`, documentada como conveniência de exibição.
- **Desvios do plano e das tarefas.**
  - `LiveMatch` limpa o pendente **antes** dos avisos do espelho, e não depois: quem responde ao estado novo mandando um comando (o bot do marco, a apresentação) teria o comando apagado logo em seguida. Research R4 atualizada.
  - `MatchFixtures` acha a pasta pelo `[CallerFilePath]`, e não por `Application.dataPath`: assim a leitura não depende do motor e roda na verificação offline.
  - `TurnClock` recebe `IClientLog` no construtor interno, por causa de `turn_warning_ignored`.
  - `VersionGate` e `FrameVerdict` são internos. `PhaseChanged` e `PriorityChanged` não saem no primeiro frame aceito (não há fase anterior). `LiveMatchPhase` tem `Idle` antes do `Start`.
  - A conta também abre os internos para `Anathema.Net.Unity.Tests`, para os testes da estratégia do bot montarem `LoadedCatalog` (plan, Complexity Tracking).
  - `SmokeStrategy.Next` recebe `(PlayerView, versão)` em vez da sessão, para ser testável sem conexão. `SmokeBot` também age quando a sessão volta a `Live`, porque um `match_start` de mesma versão não troca o estado.
  - `MatchCommandsTests` cobre `NotConnected` em `Disconnected`, `Connecting`, `WaitingRetry`, `RenewingToken` e `GaveUp`; `Suspended` não foi roteirizado.
  - `RecordedFramesTests` é um teste só com laço, porque o NUnit do Unity não tem `Assert.Multiple`.
- **Arquivos `.meta`.** Os dos arquivos e pastas novos em `Assets/` (inclusive os `.json` de fixture) só existem depois que o Unity abrir o projeto.
- **Em aberto.** Precisam do editor, do backend ou do Multiplayer Play Mode:
  - T063: o marco LiveServer com `docker compose up` (quickstart §2);
  - T064: copiar frames gravados para `Fixtures/recorded-*.json` (quickstart §3). O teste já existe e passa sem casos;
  - T068: suíte oficial pelo comando da constituição e `.meta`;
  - T069: quickstart §4 com dois jogadores do Multiplayer Play Mode.
