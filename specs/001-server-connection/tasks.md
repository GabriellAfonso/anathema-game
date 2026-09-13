---

description: "Task list for 001-server-connection"
---

# Tasks: Conexão com o servidor

**Input**: Design documents from `specs/001-server-connection/`

**Prerequisites**: [plan.md](./plan.md), [spec.md](./spec.md), [research.md](./research.md), [data-model.md](./data-model.md), [contracts/](./contracts/), [quickstart.md](./quickstart.md)

**Tests**: obrigatórios (constituição, princípio VII). Todo método novo ganha
teste EditMode. Escreva o teste antes da implementação: em Unity, "falhar" pode
significar não compilar porque o tipo ainda não existe.

**Organization**: tarefas agrupadas por história da spec (US1 a US4).

## Format: `[ID] [P?] [Story] Description`

- **[P]**: pode rodar em paralelo com as outras [P] do mesmo bloco (arquivos
  diferentes, sem depender de tarefa incompleta do bloco)
- **[Story]**: US1, US2, US3, US4 (histórias da spec)

## Convenções para todas as tarefas

Leia antes de executar qualquer tarefa:

- **Antes de escrever código**: `CLAUDE.md` e `.specify/memory/constitution.md`.
- **Namespaces**: um por assembly, igual ao nome da asmdef: `Anathema.Net.Core`,
  `Anathema.Net.Json`, `Anathema.Net.Unity`, `Anathema.Net.Editor`,
  `Anathema.Config`, `Anathema.Net.Fakes`. Os testes usam o namespace testado
  com o sufixo `.Tests`. Subpastas **não** criam sub-namespace.
- **Todo arquivo novo**:
  - começa com `#nullable enable`;
  - tem um tipo público por arquivo;
  - métodos de 4 a 20 linhas e no máximo 2 níveis de indentação;
  - `var` só quando o tipo aparece do lado direito da atribuição.
- **Membro público**: leva `/// <summary>` com a intenção e um `<example>`.
- **Mensagem de exceção**: inclui o valor recebido e a forma esperada.
- **Testes**:
  - NUnit, em `Assets/Tests/EditMode/<pasta da asmdef>/`;
  - nomes em português, no estilo de `Assets/Tests/EditMode/HeartbeatTests.cs`;
  - tempo sempre injetado (`FakeMonotonicClock`), nunca `Thread.Sleep` nem espera
    real, exceto nos testes LiveServer.
- **Contratos do protocolo** (formas das mensagens): nunca copie para o código
  nem para os comentários; cite o caminho em
  `C:/Users/gabri/Projetos/dev_container/anathema/backend/specs/`.
- **Arquivos `.meta`**: o Unity gera ao abrir o projeto ou ao rodar o comando de
  teste. Todo arquivo novo em `Assets/` vai para o commit com o seu `.meta`.
- **Comando da suíte** (editor fechado), chamado abaixo de "rodar a suíte":
  `"C:/Program Files/Unity/Hub/Editor/6000.2.8f1/Editor/Unity.exe" -batchmode -projectPath . -runTests -testPlatform EditMode -testResults Logs/editmode-results.xml`

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: assemblies, pastas e configuração de stripping, conforme a seção "Project Structure" do plan.md.

- [X] T001 [P] Create `Assets/Scripts/Net/Core/Anathema.Net.Core.asmdef`: `name` = `Anathema.Net.Core`, `rootNamespace` = `Anathema.Net.Core`, `references` = `[]`, `autoReferenced` = true, `noEngineReferences` = true, `overrideReferences` = false.
- [X] T002 [P] Create `Assets/Scripts/Net/Json/Anathema.Net.Json.asmdef`: `references` = `["Anathema.Net.Core"]`, `noEngineReferences` = true, `overrideReferences` = true, `precompiledReferences` = `["Newtonsoft.Json.dll"]`, `autoReferenced` = true.
- [X] T003 [P] Create `Assets/Scripts/Net/Unity/Anathema.Net.Unity.asmdef`: `references` = `["Anathema.Net.Core", "Anathema.Net.Json"]`, `noEngineReferences` = false, `autoReferenced` = true.
- [X] T004 [P] Create `Assets/Scripts/Core/Config/Anathema.Config.asmdef` na pasta onde `AppConfig.cs` já está, sem mover o arquivo: `references` = `["Anathema.Net.Core", "Anathema.Net.Unity"]`, `autoReferenced` = true (o `Assembly-CSharp` antigo continua enxergando `AppConfig`).
- [X] T005 [P] Create `Assets/Scripts/Net/Editor/Anathema.Net.Editor.asmdef`: `includePlatforms` = `["Editor"]`, `references` = `["Anathema.Net.Core", "Anathema.Config", "Unity.Android.Gradle"]`. Se `Unity.Android.Gradle` não resolver como referência de assembly, remova da lista e registre em research.md R3 que `DevCleartextManifest` (T101) usará o caminho alternativo.
- [X] T006 [P] Create `Assets/Tests/EditMode/Fakes/Anathema.Net.Fakes.asmdef`: `references` = `["Anathema.Net.Core"]`, `includePlatforms` = `["Editor"]`, `defineConstraints` = `["UNITY_INCLUDE_TESTS"]`, `autoReferenced` = false, `noEngineReferences` = true.
- [X] T007 [P] Create as cinco asmdefs de teste, no molde de `Assets/Tests/EditMode/Anathema.Reconnect.Tests.asmdef` (`UnityEngine.TestRunner`, `UnityEditor.TestRunner`, `nunit.framework.dll`, `UNITY_INCLUDE_TESTS`, Editor only):
  - `Assets/Tests/EditMode/Net.Core/Anathema.Net.Core.Tests.asmdef` (+ `Anathema.Net.Core`, `Anathema.Net.Fakes`);
  - `Assets/Tests/EditMode/Net.Json/Anathema.Net.Json.Tests.asmdef` (+ `Anathema.Net.Core`, `Anathema.Net.Json`, `Anathema.Net.Fakes`);
  - `Assets/Tests/EditMode/Net.Unity/Anathema.Net.Unity.Tests.asmdef` (+ `Anathema.Net.Core`, `Anathema.Net.Json`, `Anathema.Net.Unity`, `Anathema.Net.Fakes`);
  - `Assets/Tests/EditMode/Net.Editor/Anathema.Net.Editor.Tests.asmdef` (+ `Anathema.Net.Core`, `Anathema.Config`, `Anathema.Net.Editor`, `Anathema.Net.Fakes`);
  - `Assets/Tests/EditMode/Config/Anathema.Config.Tests.asmdef` (+ `Anathema.Net.Core`, `Anathema.Config`, `Anathema.Net.Fakes`).
- [X] T008 [P] Create `Assets/Scripts/Net/link.xml` preservando inteiros os assemblies `Anathema.Net.Core` e `Anathema.Net.Json`, com comentário XML citando research.md R7.
- [X] T009 Rodar a suíte (resultado em `Logs/editmode-results.xml`). Esperado: compila sem erro; `HeartbeatTests` e `ReconnectPolicyTests` passam; `NetworkBootstrap`, `LoginController` e `AppEnvManager` compilam com `AppConfig` dentro de `Anathema.Config`; os `.meta` de tudo o que foi criado foram gerados.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: tempo e log, que todas as histórias usam, e a verificação da fronteira do núcleo.

**⚠️ CRITICAL**: nenhuma história começa antes desta fase.

### Tests

- [X] T010 [P] Write `Assets/Tests/EditMode/Net.Core/Time/MonotonicInstantTests.cs`: subtração devolve `TimeSpan` exato em ticks; comparação e igualdade; não existe conversão para `DateTime` (reflexão sobre membros públicos).
- [X] T011 [P] Write `Assets/Tests/EditMode/Net.Core/Logging/ClientLogExtensionsTests.cs` usando `FakeClientLog`: `Info`/`Warning`/`Error`/`Debug` gravam o nível, o `EventName` e os campos na ordem.
- [X] T012 [P] Write `Assets/Tests/EditMode/Net.Core/Logging/ClientLogLineFormatTests.cs`: `ClientLogLineFormat.Format(entry)` produz `event_name key=value key=value`; valor com espaço sai entre aspas; entrada sem campos produz só o nome do evento.
- [X] T013 [P] Write `Assets/Tests/EditMode/Net.Core/CoreAssemblyBoundaryTests.cs` (SC-002), conforme a seção "Verificação da fronteira" de `contracts/core-ports.md`:
  1. `typeof(MonotonicInstant).Assembly.GetReferencedAssemblies()` não tem nome começando com `UnityEngine`, `UnityEditor` ou `Newtonsoft`;
  2. nenhum campo, propriedade, evento, parâmetro ou retorno de qualquer tipo do assembly usa tipo do namespace `System.Net.WebSockets`.
- [X] T014 [P] Write `Assets/Tests/EditMode/Net.Core/Fakes/FakeMonotonicClockTests.cs` e `Assets/Tests/EditMode/Net.Core/Fakes/FakeClientLogTests.cs`:
  - relógio começa diferente de zero e `Advance` soma exatamente;
  - `Advance` negativo lança;
  - `FakeClientLog.Single` falha com 0 ou 2 entradas do evento (US1, cenário 6).

### Implementation

- [X] T015 [P] Implement `Assets/Scripts/Net/Core/Time/MonotonicInstant.cs` e `Assets/Scripts/Net/Core/Time/IMonotonicClock.cs`, conforme data-model.md "Tempo": `readonly struct`, `long Ticks`, `operator -`, comparável, sem `DateTime`.
- [X] T016 [P] Implement `Assets/Scripts/Net/Core/Logging/`: `ClientLogLevel.cs`, `LogField.cs`, `ClientLogEntry.cs`, `IClientLog.cs`, `ClientLogExtensions.cs` e `ClientLogLineFormat.cs`, conforme data-model.md "Log" e a seção `IClientLog` de `contracts/core-ports.md`.
- [X] T017 [P] Implement `Assets/Tests/EditMode/Fakes/FakeMonotonicClock.cs` (depende de T015): `Now` e `Advance(TimeSpan)`; thread-safe com `Interlocked`, porque T062 lê de várias threads.
- [X] T018 [P] Implement `Assets/Tests/EditMode/Fakes/FakeClientLog.cs` (depende de T016): `Entries` (cópia só leitura, thread-safe) e `Single(string eventName)`.
- [X] T019 Rodar a suíte (resultado em `Logs/editmode-results.xml`); T010–T014 passam.

**Checkpoint**: fundação pronta. US1 e US2 podem começar em paralelo.

---

## Phase 3: User Story 1 - Testar comportamento de rede sem cena e sem servidor (Priority: P1) 🎯 MVP

**Goal**: as portas de socket, HTTP, ciclo de vida e alcançabilidade existem no núcleo, com fakes nomeados que as features seguintes usam (FR-004 a FR-022, exceto os adaptadores reais).

**Independent Test**: `Anathema.Net.Core.Tests` roteiriza cada fake e verifica avisos e pedidos, sem nenhum adaptador real (spec, US1, cenários 1-6).

### Tests for User Story 1 (MANDATORY) ⚠️

- [X] T020 [P] [US1] Write `Assets/Tests/EditMode/Net.Core/Socket/CleartextPolicyTests.cs`:
  - `https`/`wss` são sempre permitidos;
  - `http`/`ws` só com `AllowsCleartext = true`;
  - `ftp` e `file` nunca.
- [X] T021 [P] [US1] Write `Assets/Tests/EditMode/Net.Core/Socket/SocketClosureTests.cs`:
  - códigos 4001, 4400, 4403, 4404 e 1000 preservados;
  - `HasServerCode` é falso com `Code = null`;
  - motivo `null` lança.
- [X] T022 [P] [US1] Write `Assets/Tests/EditMode/Net.Core/Http/HttpRequestSpecTests.cs` e `Assets/Tests/EditMode/Net.Core/Http/HttpOutcomeTests.cs`:
  - prazo padrão 10 e fora de 1..120 lança;
  - URL relativa lança;
  - método fora de GET/POST/PUT/PATCH/DELETE lança;
  - `HttpResponse` guarda 401 e 503 com corpo;
  - `TransportFailure` guarda `Kind` e `Detail`;
  - os dois casos são distinguíveis sem cast inseguro.
- [X] T023 [P] [US1] Write `Assets/Tests/EditMode/Net.Core/Lifecycle/LifecycleSignalFilterTests.cs` com `FakeMonotonicClock`, cobrindo a tabela de research.md R2 e data-model.md "Ciclo de vida":
  - `pause(true)` → `WentToBackground`;
  - `pause(false)` → `ReturnedToForeground` com `AwayFor` igual ao avanço do relógio (7 min);
  - `focus(true)` depois de "foi" também conta como volta;
  - `focus(false)` é ignorado com `focusLossStopsPlayer = false` e vira "foi" com `true`;
  - sinais duplicados não repetem avisos;
  - volta sem ida não avisa.
- [X] T024 [P] [US1] Write `Assets/Tests/EditMode/Net.Core/Reachability/NetworkKindChangedTests.cs`: construir com `Previous == Current` lança; `LocalArea → CarrierData` é aceito.
- [X] T025 [P] [US1] Write `Assets/Tests/EditMode/Net.Core/Fakes/FakeWebSocketTests.cs` (US1, cenário 1; garantias de `IWebSocket` em `contracts/core-ports.md`):
  - `Open` registra `OpenedUrl`;
  - `SimulateClosed(4001, "auth")` produz um único `Closed` com 4001;
  - `SimulateText` depois de fechado lança no teste;
  - `SendTextAsync` antes de abrir → `NotOpen`;
  - `SentTexts` guarda os envios na ordem;
  - `Close()` duas vezes não repete aviso.
- [X] T026 [P] [US1] Write `Assets/Tests/EditMode/Net.Core/Fakes/FakeHttpTransportTests.cs` (US1, cenários 2-3):
  - `RespondNext(401, "…")` → `HttpResponse` 401;
  - `FailNext(Timeout, "…")` → `TransportFailure`;
  - roteiros consumidos em ordem;
  - pedido sem roteiro falha com método e URL na mensagem;
  - `Requests` registra método, URL, cabeçalhos e corpo.
- [X] T027 [P] [US1] Write `Assets/Tests/EditMode/Net.Core/Fakes/FakeAppLifecycleTests.cs` (US1, cenário 4): `SimulateBackground`, avançar 7 min e `SimulateForeground` → `AwayFor` de 7 min; chamada fora de ordem lança.
- [X] T028 [P] [US1] Write `Assets/Tests/EditMode/Net.Core/Fakes/FakeNetworkReachabilityTests.cs` (US1, cenário 5): `LocalArea → CarrierData` avisa uma vez com anterior e novo; repetir o mesmo valor não avisa; `Current` acompanha.

### Implementation for User Story 1

- [X] T029 [P] [US1] Implement `Assets/Scripts/Net/Core/Socket/`: `SocketClosure.cs`, `SocketSendOutcome.cs` (`Sent`, `NotOpen`, `Failed(detail)`), `CleartextPolicy.cs` e `IWebSocket.cs`, conforme data-model.md "Socket" e `contracts/core-ports.md`.
- [X] T030 [P] [US1] Implement `Assets/Scripts/Net/Core/Http/`: `HttpRequestSpec.cs`, `HttpOutcome.cs` (abstrata selada por construtor privado), `HttpResponse.cs`, `TransportFailure.cs`, `TransportFailureKind.cs` e `IHttpTransport.cs`, conforme data-model.md "HTTP".
- [X] T031 [P] [US1] Implement `Assets/Scripts/Net/Core/Lifecycle/`: `WentToBackground.cs`, `ReturnedToForeground.cs`, `IAppLifecycle.cs` e `LifecycleSignalFilter.cs`. O filtro recebe no construtor `(IMonotonicClock clock, bool focusLossStopsPlayer)`, expõe `OnPause(bool)` e `OnFocus(bool)`, e os eventos `WentToBackground` e `ReturnedToForeground`.
- [X] T032 [P] [US1] Implement `Assets/Scripts/Net/Core/Reachability/`: `NetworkKind.cs`, `NetworkKindChanged.cs` e `INetworkReachability.cs`.
- [X] T033 [P] [US1] Implement `Assets/Tests/EditMode/Fakes/FakeWebSocket.cs` (depende de T029), conforme a descrição do fake em `contracts/core-ports.md` `IWebSocket`, com a mesma máquina de estados de data-model.md.
- [X] T034 [P] [US1] Implement `Assets/Tests/EditMode/Fakes/FakeHttpTransport.cs` (depende de T030).
- [X] T035 [P] [US1] Implement `Assets/Tests/EditMode/Fakes/FakeAppLifecycle.cs` (depende de T031 e T017).
- [X] T036 [P] [US1] Implement `Assets/Tests/EditMode/Fakes/FakeNetworkReachability.cs` (depende de T032).
- [X] T037 [US1] Rodar a suíte (resultado em `Logs/editmode-results.xml`); T020–T028 passam.

**Checkpoint**: uma feature seguinte já consegue testar socket, HTTP, ciclo de vida e troca de rede só com fakes.

---

## Phase 4: User Story 2 - Traduzir frames do protocolo em tipos do projeto (Priority: P1)

**Goal**: identificadores tipados e o codec (envelope, união fechada com desconhecido, frames genéricos, decodificação inválida sem exceção). FR-029 a FR-040.

**Independent Test**: `Anathema.Net.Json.Tests` alimenta o codec com textos e verifica o valor tipado ou a falha, sem socket (spec, US2, cenários 1-7; "Casos de teste obrigatórios" de `contracts/protocol-codec.md`).

### Tests for User Story 2 (MANDATORY) ⚠️

- [X] T038 [P] [US2] Write `Assets/Tests/EditMode/Net.Core/Identity/UserIdTests.cs`, `CardInstanceIdTests.cs`, `MatchIdTests.cs` e `IdentityIsolationTests.cs`:
  - igualdade por valor;
  - validação: `UserId` > 0, `CardInstanceId` ≥ 0, `MatchId` não vazio e sem espaço nas pontas;
  - `ToString` no formato `user_id=7`;
  - por reflexão, nenhum dos três tem operador de conversão nem construtor que aceite outro identificador (cenário 7: trocar um pelo outro não compila).
- [X] T039 [P] [US2] Write `Assets/Tests/EditMode/Net.Core/Protocol/DecodeOutcomeTests.cs`: `Value` em falha lança; `Failure` em sucesso lança; `DecodeFailure` guarda `Kind`, `Path` e `Detail`.
- [X] T040 [P] [US2] Write `Assets/Tests/EditMode/Net.Json/JObjectPayloadReaderTests.cs`, cobrindo a seção `IPayloadReader` de `contracts/protocol-codec.md`:
  - campo ausente → `PayloadShapeException` com `MissingField` e caminho;
  - `4.0` e `"4"` em `ReadInteger` → `WrongFieldType`;
  - opcionais devolvem `null` com ausente ou `null`, e `WrongFieldType` com tipo errado;
  - `Path` aninhado `payload.deck_problems[1]`;
  - `FieldNames` e `Has`.
- [X] T041 [P] [US2] Write `Assets/Tests/EditMode/Net.Json/JObjectPayloadWriterTests.cs`: escreve texto, inteiro, booleano, objeto aninhado e lista de inteiros; campo repetido lança `ArgumentException` com o nome.
- [X] T042 [P] [US2] Write `Assets/Tests/EditMode/Net.Json/DiscriminatedUnionTests.cs`. Fica em `Net.Json` porque precisa de um `IPayloadReader` real; um reader de dicionário só para o teste duplicaria o `JObjectPayloadReader`.
  - `Register` repetido lança;
  - valor sem braço chama `unknownArm` com o texto exato;
  - `DecodeObject` sem discriminador → `MissingType`; discriminador não texto → `TypeNotText`;
  - `PayloadShapeException` do braço vira falha com o caminho;
  - união aninhada por `kind` devolve o braço desconhecido no item;
  - falha aninhada sai com o caminho completo.
- [X] T043 [P] [US2] Write `Assets/Tests/EditMode/Net.Json/NewtonsoftProtocolCodecDecodeTests.cs` (seção "Decodificação, em ordem" e caso 5 de `contracts/protocol-codec.md`):
  - cada passo 1-7 produz o `DecodeFailureKind` da tabela;
  - o conjunto inválido (`null`, `""`, `"not json"`, `"[]"`, `"42"`, `"null"`, `"{}"`, `{"type": 5}`, `{"type": "pong", "payload": []}`, JSON truncado, 1000 níveis) devolve falha, sem exceção (SC-006);
  - `payload` ausente é tratado como `{}`;
  - `"Pong"` é desconhecido;
  - campos a mais no envelope e no payload são ignorados;
  - `Detail` é cortado em 200 caracteres.
- [X] T044 [P] [US2] Write `Assets/Tests/EditMode/Net.Json/NewtonsoftProtocolCodecEncodeTests.cs`:
  - `PingMessage` com marcador → uma linha com `type` `ping` e `payload` com `sent_at_ms` e `ping_seq`;
  - `PingMessage` sem marcador → `"payload": {}`;
  - a saída não tem quebra de linha.
- [X] T045 [P] [US2] Write `Assets/Tests/EditMode/Net.Json/GenericServerFramesTests.cs` (US2, cenários 1-6; casos 1-4 e 9 do contrato):
  - `message_refused` com `deck_id` extra → `Code`, `Error` e `Details.ReadInteger("deck_id") == 4`;
  - sem `code` → `MissingField` em `payload.code`;
  - código desconhecido é preservado;
  - `auth_denied` → `Error`;
  - `pong` com o payload do ping codificado → marcador igual;
  - `pong` com `{}` → `Marker == null`;
  - `match_found` → `UnknownServerFrame`.
- [X] T046 [P] [US2] Write `Assets/Tests/EditMode/Net.Json/PayloadIdentityExtensionsTests.cs`:
  - `ReadUserId` de `"7"` → `WrongFieldType`;
  - `ReadMatchId` de `""` → `InvalidValue`;
  - `ReadUserId` de `0` → `InvalidValue`;
  - ida e volta com `WriteUserId`, `WriteCardInstanceId` e `WriteMatchId` pelo writer e pelo reader.

### Implementation for User Story 2

- [X] T047 [P] [US2] Implement `Assets/Scripts/Net/Core/Identity/`: `UserId.cs`, `CardInstanceId.cs` e `MatchId.cs`, conforme data-model.md "Identidade". Cada um é `readonly struct`, `IEquatable<T>`, sem conversões.
- [X] T048 [P] [US2] Implement `Assets/Scripts/Net/Core/Protocol/`: `DecodeFailureKind.cs`, `DecodeFailure.cs`, `DecodeOutcome.cs` e `PayloadShapeException.cs`. A mensagem da exceção inclui caminho, valor recebido e forma esperada.
- [X] T049 [US2] Implement `Assets/Scripts/Net/Core/Protocol/`: `IPayloadReader.cs`, `IPayloadWriter.cs`, `PayloadIdentityReading.cs` e `PayloadIdentityWriting.cs` (extensões de identidade), com as assinaturas de `contracts/protocol-codec.md`. Depende de T047 e T048.
- [X] T050 [US2] Implement `Assets/Scripts/Net/Core/Protocol/DiscriminatedUnion.cs`, conforme a seção `DiscriminatedUnion<TBase>` do contrato: captura só `PayloadShapeException`; aninhamento relança com o caminho. Depende de T049.
- [X] T051 [US2] Implement `Assets/Scripts/Net/Core/Protocol/`: `IOutgoingMessage.cs`, `PingMarker.cs` e `PingMessage.cs` (`type` `ping`; com marcador escreve `sent_at_ms` e `ping_seq`; sem marcador não escreve nada). Depende de T049.
- [X] T052 [US2] Implement `Assets/Scripts/Net/Core/Protocol/Frames/`: `ServerFrame.cs` (abstrata, construtor `protected`), `MessageRefusedFrame.cs`, `AuthDeniedFrame.cs`, `PongFrame.cs`, `UnknownServerFrame.cs` e `GenericServerFrames.cs` (`CreateUnion()` registra os três braços com discriminador `type`). Implement também `Assets/Scripts/Net/Core/Protocol/IProtocolCodec.cs`. Tabela "Frames genéricos" do contrato; comentário cita os contratos 009/011/013 do backend por caminho. Depende de T050 e T051.
- [X] T053 [P] [US2] Implement `Assets/Scripts/Net/Json/JObjectPayloadReader.cs`: envolve `JObject`, mantém o `Path` e lança `PayloadShapeException`. Nenhum membro público expõe `JToken`/`JObject`: o construtor é `internal` e a criação passa pelo codec. Depende de T049.
- [X] T054 [P] [US2] Implement `Assets/Scripts/Net/Json/JObjectPayloadWriter.cs`: monta um `JObject`, com construtor `internal`. Depende de T049.
- [X] T055 [US2] Implement `Assets/Scripts/Net/Json/NewtonsoftProtocolCodec.cs`, com construtor `(DiscriminatedUnion<ServerFrame> frames, IClientLog log)`:
  - `Decode` segue os passos 1-7 do contrato, parseando com `JsonTextReader` com `MaxDepth = 64` (acima disso: `NotJson`);
  - `Decode` captura qualquer exceção restante como `InvalidValue` e registra `codec_unexpected_failure`;
  - `Encode` sempre escreve `payload`, `{}` quando vazio, com `Formatting.None`.

  Depende de T052, T053 e T054.
- [X] T056 [US2] Rodar a suíte (resultado em `Logs/editmode-results.xml`); T038–T046 passam, e `CoreAssemblyBoundaryTests` (T013) continua passando.

**Checkpoint**: frames genéricos tipados; as features seguintes só registram braços.

---

## Phase 5: User Story 3 - Falar com o servidor local de verdade (Priority: P2)

**Goal**: adaptadores reais, fila da thread principal e hospedeiro; os três testes LiveServer passam (FR-023 a FR-028, SC-003 a SC-005).

**Independent Test**: suíte EditMode (fila, montagem de mensagem, classificador, adaptadores sem servidor) e os três testes `[Explicit]` `[Category("LiveServer")]` com `docker compose up` (quickstart.md §2).

**Depends on**: US1 (portas) e US2 (codec, para os passos LiveServer).

### Tests for User Story 3 (MANDATORY) ⚠️

- [X] T057 [P] [US3] Write `Assets/Tests/EditMode/Net.Core/Threading/MainThreadQueueTests.cs` (seção `MainThreadQueue` de `contracts/core-ports.md`):
  - ordem de um produtor;
  - 4 threads × 2.500 itens: nenhum perdido e a ordem de cada produtor preservada (SC-005);
  - nada entregue antes de `Drain`;
  - `Close` descarta pendentes;
  - `Enqueue` depois de `Close` não lança nem entrega;
  - exceção de um item gera `main_thread_item_failed` no `FakeClientLog` e os seguintes são entregues;
  - item enfileirado durante `Drain` só sai no próximo;
  - `Close` é idempotente.
- [X] T058 [P] [US3] Write `Assets/Tests/EditMode/Net.Core/Reachability/NetworkKindTrackerTests.cs` com `FakeMonotonicClock`:
  - `Observe(kind)` antes de 1 s desde a última leitura é ignorado;
  - mudança avisa com anterior e novo;
  - mesmo valor não avisa;
  - `LocalArea → CarrierData` direto avisa uma vez.
- [X] T059 [P] [US3] Write `Assets/Tests/EditMode/Net.Unity/WebSocketMessageAssemblerTests.cs`:
  - três fragmentos → um texto;
  - caractere UTF-8 multibyte cortado entre fragmentos é remontado;
  - mensagem de 64 KiB em pedaços de 8 KiB sai inteira;
  - `Reset` limpa.
- [X] T060 [P] [US3] Write `Assets/Tests/EditMode/Net.Unity/DotNetWebSocketTests.cs` (sem servidor):
  - `Open("ws://…")` com `CleartextPolicy(false)`, depois `Drain` → `Errored("cleartext_refused")` e um `Closed(null, "cleartext_refused")`;
  - `Close()` antes de `Open` → um `Closed(null, "closed_before_open")`;
  - `SendTextAsync` antes de abrir → `NotOpen`;
  - nenhum aviso chega antes de `Drain`;
  - `Open` duas vezes lança `InvalidOperationException`.
- [X] T061 [P] [US3] Write `Assets/Tests/EditMode/Net.Unity/UnityWebRequestErrorClassifierTests.cs`, com os textos de research.md R5: "Request timeout" → `Timeout`; "Cannot resolve destination host" → `HostNotResolved`; "Cannot connect to destination host" → `CannotConnect`; outro → `Other` com o texto original.
- [X] T062 [P] [US3] Write `Assets/Tests/EditMode/Net.Unity/UnityHttpTransportTests.cs` como `[UnityTest]`, drenando a fila a cada tick: `SendAsync` chamado de uma thread do pool para `http://…` com `CleartextPolicy(false)` completa com `TransportFailure(CleartextRefused)`, e a continuação observada roda na thread principal.
- [X] T063 [P] [US3] Write `Assets/Tests/EditMode/Net.Unity/MonotonicClockAdapterTests.cs`: 10.000 leituras seguidas de `StopwatchMonotonicClock` nunca diminuem; no editor, `PlatformMonotonicClock.Create()` devolve `StopwatchMonotonicClock`.
- [X] T064 [P] [US3] Write `Assets/Tests/EditMode/Net.Unity/UnityAppLifecycleTests.cs`: `OnPause(true)` não avisa antes de `Drain`; depois de `Drain`, avisa `WentToBackground`; a volta com `FakeMonotonicClock` avançado traz `AwayFor` correto.
- [X] T065 [P] [US3] Write `Assets/Tests/EditMode/Net.Unity/NetworkKindMappingTests.cs`: os três valores de `UnityEngine.NetworkReachability` mapeiam para `None`, `LocalArea` e `CarrierData`.
- [X] T066 [P] [US3] Write `Assets/Tests/EditMode/Net.Unity/UnityConsoleLogTests.cs` com `LogAssert.Expect`: `Info` → `LogType.Log`, `Warning` → `LogType.Warning`, `Error` → `LogType.Error`, e texto igual a `ClientLogLineFormat.Format`.
- [X] T067 [P] [US3] Write `Assets/Tests/EditMode/Net.Unity/LiveNetworkAdaptersTests.cs`: `LiveNetworkAdapters.Create(queue, log, policy)` entrega a mesma `CleartextPolicy` ao socket e ao HTTP, e cada chamada de `CreateSocket()` devolve instância nova.
- [X] T068 [P] [US3] Write `Assets/Tests/EditMode/Net.Unity/LiveServer/LiveServerProbeTests.cs`: três `[UnityTest]` com `[Explicit]`, `[Category("LiveServer")]`, base `http://127.0.0.1:8000` e `ws://127.0.0.1:8000`, timeout de 15 s. Cada teste compõe `LiveNetworkAdapters`, roda um passo de `LiveServerProbeSteps` drenando a fila a cada tick e verifica `Passed`. Todo assinante grava `ManagedThreadId`, e o teste compara com o da thread principal capturado no início (SC-004).
  1. `CheckUnauthorizedHttpAsync` → 401;
  2. `CheckAuthDeniedAsync` → `AuthDeniedFrame` e close 4001;
  3. `CheckPingPongAsync` → marcador igual.

### Implementation for User Story 3

- [X] T069 [P] [US3] Implement `Assets/Scripts/Net/Core/Threading/MainThreadQueue.cs`, conforme research.md R8: `ConcurrentQueue<Action>`; `Drain` conta os itens no início; `Close` com `Interlocked`.
- [X] T070 [P] [US3] Implement `Assets/Scripts/Net/Core/Reachability/NetworkKindTracker.cs`: construtor `(IMonotonicClock clock, NetworkKind initial)`, `Observe(NetworkKind)` e evento `Changed`, com intervalo de 1 s.
- [X] T071 [P] [US3] Implement `Assets/Scripts/Net/Unity/WebSocketMessageAssembler.cs`: acumula bytes num `MemoryStream` e decodifica UTF-8 só no `EndOfMessage`.
- [X] T072 [US3] Implement `Assets/Scripts/Net/Unity/DotNetWebSocket.cs`, conforme research.md R4 e a tabela de transições de data-model.md "Socket":
  - construtor `(MainThreadQueue queue, CleartextPolicy policy, IClientLog log)`;
  - laço de recepção em `Task.Run`, com `WebSocketMessageAssembler`;
  - `SemaphoreSlim(1)` no envio;
  - `CloseOutputAsync(NormalClosure)` com `Abort` após 5 s;
  - trava de fechado único com `Interlocked`;
  - todo aviso via `queue.Enqueue`.

  Métodos de 4-20 linhas: separe abrir, receber, tratar close, tratar falha e publicar aviso. Depende de T069 e T071.
- [X] T073 [P] [US3] Implement `Assets/Scripts/Net/Unity/UnityWebRequestErrorClassifier.cs`.
- [X] T074 [US3] Implement `Assets/Scripts/Net/Unity/UnityHttpTransport.cs`, conforme research.md R5:
  - construtor `(MainThreadQueue queue, CleartextPolicy policy)`;
  - cria o `UnityWebRequest` dentro de `queue.Enqueue` e completa o `TaskCompletionSource<HttpOutcome>` em `completed`;
  - `Success` e `ProtocolError` → `HttpResponse`;
  - `ConnectionError` e `DataProcessingError` → classificador;
  - `InvalidOperationException` de cleartext → `CleartextRefused`;
  - política fechada → `CleartextRefused` sem criar o request;
  - `Dispose` do request sempre.

  Depende de T069 e T073.
- [X] T075 [P] [US3] Implement `Assets/Scripts/Net/Unity/StopwatchMonotonicClock.cs`, `BootTimeMonotonicClock.cs` e `PlatformMonotonicClock.cs`, conforme research.md R1:
  - P/Invoke `clock_gettime` em `libc` com `CLOCK_BOOTTIME = 7` e `timespec` de dois `nint`;
  - fallback para `Stopwatch` com um único log `clock_boottime_unavailable`;
  - `#if UNITY_ANDROID && !UNITY_EDITOR` só em `PlatformMonotonicClock`.
- [X] T076 [P] [US3] Implement `Assets/Scripts/Net/Unity/UnityAppLifecycle.cs`: construtor `(LifecycleSignalFilter filter, MainThreadQueue queue)`; `OnPause`/`OnFocus` repassam ao filtro; os eventos do filtro são republicados via fila; `focusLossStopsPlayer` vem de `!Application.runInBackground` (sempre falso no Android), decidido pela composição. Depende de T069.
- [X] T077 [P] [US3] Implement `Assets/Scripts/Net/Unity/NetworkKindMapping.cs` e `Assets/Scripts/Net/Unity/UnityNetworkReachability.cs`: `Poll()` lê `Application.internetReachability`, passa ao `NetworkKindTracker` e republica `Changed` via fila. Depende de T069 e T070.
- [X] T078 [P] [US3] Implement `Assets/Scripts/Net/Unity/UnityConsoleLog.cs`: o único lugar com `Debug.Log`, `Debug.LogWarning` e `Debug.LogError` da camada; formata com `ClientLogLineFormat`.
- [X] T079 [US3] Implement `Assets/Scripts/Net/Unity/NetworkLayerHost.cs` (`MonoBehaviour`):
  - `Attach(MainThreadQueue, UnityAppLifecycle, UnityNetworkReachability)`;
  - `Update` → `Drain()` e `Poll()`;
  - `OnApplicationPause`/`OnApplicationFocus` → lifecycle;
  - `OnDestroy` → `Close()`;
  - antes do `Attach`, não faz nada.

  Sem singleton e sem `static`. Depende de T076 e T077.
- [X] T080 [US3] Implement `Assets/Scripts/Net/Unity/LiveNetworkAdapters.cs`: `Create(MainThreadQueue, IClientLog, CleartextPolicy)` expõe `Http`, `Clock`, `CreateSocket()` e `Codec` (`NewtonsoftProtocolCodec` com `GenericServerFrames.CreateUnion()`). Depende de T072, T074, T075 e T055.
- [X] T081 [US3] Implement `Assets/Scripts/Net/Unity/Probe/ProbeStepResult.cs` e `Assets/Scripts/Net/Unity/Probe/LiveServerProbeSteps.cs`, conforme research.md R11:
  - construtor `(LiveNetworkAdapters adapters, IClientLog log, string httpBase, string wsBase)`;
  - `CheckUnauthorizedHttpAsync` faz `GET /game/cards/`;
  - `CheckAuthDeniedAsync` abre `ws/matchmaking/?token=invalid`;
  - `CheckPingPongAsync` cadastra `probe_<ticks>` em `POST /accounts/register/` e faz `POST /accounts/login/` (corpos como em `backend/scripts/smoke_match.py`, `sign_up`/`log_in`), lê `token`, abre `ws/matchmaking/?token=…`, manda `PingMessage` e espera `PongFrame` com o mesmo marcador;
  - cada passo registra `connection_probe_step` com os campos de quickstart.md §4.

  Os JSON de login e cadastro são escritos com `IPayloadWriter`, por uma mensagem HTTP pequena, e lidos com `IPayloadReader`: acrescente a `IProtocolCodec` os métodos `EncodeObject(Action<IPayloadWriter>)` e `DecodeObject(string)` e cubra-os com um teste em `NewtonsoftProtocolCodecEncodeTests.cs`. Depende de T080.
- [X] T082 [US3] Rodar a suíte (T057–T067 passam); com `docker compose up` no backend, rodar os testes LiveServer conforme quickstart.md §2 (T068 passa: SC-003 e SC-004).

**Checkpoint**: adaptadores reais provados contra o backend local, com entrega na thread principal.

---

## Phase 6: User Story 4 - Rodar o build de desenvolvimento no celular apontando para o computador (Priority: P3)

**Goal**: host trocável só em desenvolvimento, cleartext só em desenvolvimento, gate de build de produção e probe no aparelho (FR-041 a FR-048, SC-007, SC-008).

**Independent Test**: testes EditMode do gate, do `ServerHost`, do `AppConfig` e do leitor de inicialização; quickstart.md §3 (gate) e §4 (aparelho).

**Depends on**: US3 só para o probe (T104). O resto depende só da fundação.

### Tests for User Story 4 (MANDATORY) ⚠️

- [X] T083 [P] [US4] Write `Assets/Tests/EditMode/Net.Core/Environment/ServerHostTests.cs` com a tabela "Normalização" de `contracts/environment-build.md`; o problema devolvido contém o valor recebido e `host[:porta]`.
- [X] T084 [P] [US4] Write `Assets/Tests/EditMode/Net.Core/Environment/ReleaseTlsRuleTests.cs`: nas quatro combinações de build de desenvolvimento × `useTls`, só produção com TLS desligado viola; a mensagem contém o caminho da cena, o nome do config, `isProd=<valor>`, `useTls=false` e "Development Build".
- [X] T085 [P] [US4] Write `Assets/Tests/EditMode/Config/AppConfigTests.cs` (instância com `ScriptableObject.CreateInstance<AppConfig>()`):
  - sem override, `EffectiveHost == apiBaseUrl`, e `HttpUrl`/`WsUrl` usam esse host e esquema conforme `useTls`;
  - `OverrideHost("http://192.168.0.10:8000/", developmentBuild: true)` troca as URLs para `192.168.0.10:8000`;
  - host inválido devolve o motivo e mantém o anterior;
  - `developmentBuild: false` recusa;
  - por reflexão, `connectionConsumerUrl` não existe.
- [X] T086 [P] [US4] Write `Assets/Tests/EditMode/Net.Unity/LaunchServerHostReaderTests.cs`: `FindCommandLineValue(new[] {"app.exe", "-serverHost", "192.168.0.10:8000"}, "-serverHost")` devolve o valor; flag sem valor ou ausente devolve `null`; `ShouldRead(developmentBuild: false)` é falso.
- [X] T087 [P] [US4] Write `Assets/Tests/EditMode/Net.Editor/EnvironmentSelectionReaderTests.cs`, com o componente de teste `Assets/Tests/EditMode/Net.Editor/EnvironmentSelectorTestComponent.cs` (`MonoBehaviour` com campos privados serializados `isProd`, `configDev` e `configProd`):
  - lê o config selecionado conforme `isProd`;
  - componente sem os campos é ignorado;
  - config selecionado vazio vira problema com o nome do campo.
- [X] T088 [P] [US4] Write `Assets/Tests/EditMode/Net.Editor/AppEnvManagerFieldsTests.cs`: carrega `Assets/Scripts/Bootstrap/AppEnvManager.cs` com `AssetDatabase.LoadAssetAtPath<MonoScript>`, pega a classe e falha se `isProd`, `configDev` ou `configProd` não existirem como campos de instância (proteção da exceção em Complexity Tracking).
- [X] T089 [P] [US4] Write `Assets/Tests/EditMode/Net.Editor/ReleaseBuildTlsGateTests.cs`: `ShouldValidate(hasReport: false, options)` é falso; `ShouldValidate(true, BuildOptions.Development)` é falso; `ShouldValidate(true, BuildOptions.None)` é verdadeiro; `Validate` sobre `EnvironmentSelectorTestComponent` com config `useTls=false` lança `BuildFailedException` com a mensagem de `ReleaseTlsRule`, e com `useTls=true` não lança.
- [X] T090 [P] [US4] Write `Assets/Tests/EditMode/Net.Editor/DevCleartextManifestTests.cs`: `CleartextValueFor(developmentBuild: true)` é `true`, `false` é `false`.
- [X] T091 [P] [US4] Write `Assets/Tests/EditMode/Net.Unity/DevConnectionProbeTests.cs`: `ShouldRun(developmentBuild, probeValue)` só é verdadeiro com build de desenvolvimento e valor `true`; `ResolveBases(host, useTls)` monta `http://`/`ws://` ou `https://`/`wss://`.

### Implementation for User Story 4

- [X] T092 [P] [US4] Implement `Assets/Scripts/Net/Core/Environment/ServerHost.cs` (`TryParse(string raw, out ServerHost host, out string problem)`).
- [X] T093 [P] [US4] Implement `Assets/Scripts/Net/Core/Environment/ReleaseTlsRule.cs` (`Check(...)` devolve `string?`, com a mensagem de `contracts/environment-build.md`).
- [X] T094 [P] [US4] Implement `Assets/Scripts/Net/Unity/LaunchServerHostReader.cs`, conforme research.md R10: `ShouldRead`, `FindCommandLineValue` e `ReadLaunchValue(string name)`. No Android lê `AndroidApplication.currentActivity.Call<AndroidJavaObject>("getIntent").Call<string>("getStringExtra", name)`; no Windows e no editor lê `Environment.GetCommandLineArgs()` com prefixo `-`. Serve para `serverHost` e `connectionProbe`.
- [X] T095 [US4] Evolve `Assets/Scripts/Core/Config/AppConfig.cs` no lugar, conforme data-model.md "AppConfig", sem apagar os `Tooltip`/`Header` existentes:
  - remover `connectionConsumerUrl`;
  - adicionar `EffectiveHost`, `OverrideHost(string raw)` (público, usa `Debug.isDebugBuild`) e `internal OverrideHost(string raw, bool developmentBuild)`;
  - no primeiro acesso a `EffectiveHost` em build de desenvolvimento, aplicar `LaunchServerHostReader.ReadLaunchValue("serverHost")`; valor inválido registra `server_host_rejected`;
  - `HttpUrl`/`WsUrl` usam o host efetivo;
  - `OnValidate` delega a normalização a `ServerHost`.

  Criar `Assets/Scripts/Core/Config/AssemblyInfo.cs` com `[assembly: InternalsVisibleTo("Anathema.Config.Tests")]`. Depende de T092 e T094.
- [X] T096 [P] [US4] Remover a linha `connectionConsumerUrl:` de `Assets/Config/AppConfig_Dev.asset` e `Assets/Config/AppConfig_Prod.asset`, sem tocar nas outras chaves.
- [X] T097 [US4] Edit `Assets/Scripts/Bootstrap/NetworkBootstrap..cs`: remover a propriedade `ConnectionClient`, sua construção e o método `PresencePolicy()` se ficar sem uso. Manter todos os comentários restantes. Depende de T095.
- [X] T098 [US4] Edit `Assets/Scripts/Login/LoginController.cs`: em `HandleLoginSuccess`, remover a assinatura e a chamada do `ConnectionClient` e carregar `SceneManager.LoadScene("HomeScene")` depois de `SetTokens` e `LoadProfile`. Remover `HandleSocketConnected`/`HandleSocketError` se ficarem sem uso. Não mudar o resto do login (feature 2). Depende de T097.
- [X] T099 [P] [US4] Edit `ProjectSettings/ProjectSettings.asset`: `insecureHttpOption: 0` → `insecureHttpOption: 1` (DevelopmentOnly), research.md R3.
- [X] T100 [US4] Implement `Assets/Scripts/Net/Editor/EnvironmentSelectionReader.cs`: nomes `isProd`, `configDev` e `configProd` em constantes com comentário citando `Assets/Scripts/Bootstrap/AppEnvManager.cs` e o Complexity Tracking do plan.md; lê por `SerializedObject` e devolve config selecionado, `isProd` e problema. Depende de T004 e T095.
- [X] T101 [US4] Implement `Assets/Scripts/Net/Editor/DevCleartextManifest.cs`: `AndroidProjectFilesModifier` que grava `android:usesCleartextTraffic` com `CleartextValueFor(development)` a partir das opções do build. Se `Unity.Android.Gradle` não estiver referenciável (T005), implementar como `IPostGenerateGradleAndroidProject`, editando `unityLibrary/src/main/AndroidManifest.xml` com `System.Xml`, e registrar a escolha em research.md R3.
- [X] T102 [US4] Implement `Assets/Scripts/Net/Editor/ReleaseBuildTlsGate.cs`: `IProcessSceneWithReport`, `callbackOrder` 0; `ShouldValidate(bool hasReport, BuildOptions options)`; `Validate(Scene scene)` percorre os `MonoBehaviour` da cena com `EnvironmentSelectionReader`, aplica `ReleaseTlsRule` e lança `BuildFailedException`. Depende de T093 e T100.
- [X] T103 [US4] Rodar a suíte (resultado em `Logs/editmode-results.xml`); T083–T090 passam.
- [X] T104 [US4] Implement `Assets/Scripts/Net/Unity/Probe/DevConnectionProbe.cs`:
  - `[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]`, com `ShouldRun` e `ResolveBases`;
  - lê `connectionProbe` e `serverHost` com `LaunchServerHostReader`, validando o host por `ServerHost`; sem `serverHost`, usa `127.0.0.1:8000`;
  - cria um `GameObject` com `DontDestroyOnLoad` e `NetworkLayerHost`, compõe `MainThreadQueue`, `UnityConsoleLog`, `CleartextPolicy(Debug.isDebugBuild)`, `LiveNetworkAdapters`, `UnityAppLifecycle` e `UnityNetworkReachability`;
  - registra `app_background`, `app_foreground away_ms=…` e `network_kind_changed previous=… current=…` (quickstart.md §5-6);
  - roda os três passos de `LiveServerProbeSteps` e registra `connection_probe_passed host=…` ou `connection_probe_failed step=…`.

  Não lê `AppConfig`, porque `Anathema.Net.Unity` não referencia `Anathema.Config`. Depende de T081, T091 e T094.
- [X] T105 [US4] Validar o gate conforme quickstart.md §3 (SC-007): build sem Development Build com `isProd` desligado falha com a mensagem; com Development Build passa.

**Checkpoint**: build de desenvolvimento pronto para o aparelho, e produção protegida.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: portas de qualidade da constituição e validação manual.

- [X] T106 [P] Revisar todo arquivo novo em `Assets/Scripts/Net/`, `Assets/Scripts/Core/Config/` e `Assets/Tests/EditMode/Fakes/`: membro público com `/// <summary>` e `<example>`; comentário diz o porquê; linha que existe por restrição externa cita a origem (research.md R1/R3/R4, issue dotnet/runtime#77945).
- [X] T107 [P] Revisar estilo em todos os arquivos novos de `Assets/Scripts/Net/`, `Assets/Scripts/Core/Config/` e `Assets/Tests/EditMode/`:
  - métodos de 4-20 linhas e no máximo 2 níveis de indentação;
  - um tipo público por arquivo;
  - `#nullable enable`;
  - `var` só com o tipo à direita;
  - sem `dynamic`;
  - mensagens de exceção com valor e forma esperada;
  - nomes sem `Manager`/`Helper`/`Handler`/`data`.

  Dividir o que estourar.
- [X] T108 Verificar por busca:
  - `Debug.Log` só em `Assets/Scripts/Net/Unity/UnityConsoleLog.cs` entre os arquivos novos;
  - `JObject`/`JToken`/`Newtonsoft` só em `Assets/Scripts/Net/Json/`;
  - `ClientWebSocket` só em `DotNetWebSocket.cs`;
  - nenhum campo ou parâmetro chamado só `id`.
- [X] T109 Rodar a suíte conforme quickstart.md §1: 100% passa; nenhum aviso de compilação originado em arquivos novos (SC-001); duração somada dos testes da feature em `Logs/editmode-results.xml` < 5 s (SC-009).
- [X] T110 Conferir `git status`: todo arquivo novo ou alterado em `Assets/` tem o `.meta` correspondente; `ConnectionClient.cs` continua no projeto.
- [ ] T111 Validar no aparelho Android conforme quickstart.md §4, §5 e §6 (SC-008; R1, R3, R4, R7 marcados "verificar no aparelho"). Registrar em research.md o resultado de cada verificação.
- [X] T112 Registrar o trabalho adiado em `C:/Users/gabri/Obsidian/Projetos/Anathema/Game/TODO.md`, com diagnóstico e caminho, conforme a constituição:
  - remover `ConnectionClient.cs` e a NativeWebSocket;
  - religar `BaseClient`/`MatchClient`/`MatchmakingClient` nas portas (feature 3);
  - login do `LoginController` nas portas (feature 2);
  - alcançabilidade por callback do Android se 1 s se mostrar lento (R9).

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: sem dependências.
- **Foundational (Phase 2)**: depende do Setup; bloqueia todas as histórias.
- **US1 (Phase 3)** e **US2 (Phase 4)**: dependem só da fundação; podem correr em paralelo.
- **US3 (Phase 5)**: depende de US1 (portas e fakes) e US2 (codec).
- **US4 (Phase 6)**: T083–T103 dependem só da fundação; T104 (probe) depende de US3.
- **Polish (Phase 7)**: depois das histórias desejadas; T111 exige US3 e US4.

### User Story Dependencies

```text
Setup ─► Foundational ─┬─► US1 ─┬─► US3 ─► US4 (T104) ─► Polish
                       ├─► US2 ─┘            ▲
                       └─► US4 (T083–T103) ───┘
```

### Within Each User Story

- Testes antes da implementação: o teste falha ou não compila, depois passa.
- Tipos de valor antes das interfaces que os usam; interfaces antes dos fakes e adaptadores.
- Núcleo antes de `Json`; `Json` antes de `Unity`.
- Rodar a suíte no fim de cada fase antes de seguir.

### Dependências dentro das fases (as [P] que esperam outra tarefa)

- T017 ← T015; T018 ← T016
- T033 ← T029; T034 ← T030; T035 ← T031, T017; T036 ← T032
- T049 ← T047, T048; T050, T051 ← T049; T052 ← T050, T051; T053, T054 ← T049; T055 ← T052, T053, T054
- T072 ← T069, T071; T074 ← T069, T073; T076, T077 ← T069 (T077 também ← T070); T079 ← T076, T077; T080 ← T072, T074, T075, T055; T081 ← T080
- T095 ← T092, T094; T097 ← T095; T098 ← T097; T100 ← T095; T102 ← T093, T100; T104 ← T081, T091, T094

---

## Parallel Example: User Story 1

```bash
# Testes de US1, todos juntos:
Task: "CleartextPolicyTests em Assets/Tests/EditMode/Net.Core/Socket/CleartextPolicyTests.cs"
Task: "LifecycleSignalFilterTests em Assets/Tests/EditMode/Net.Core/Lifecycle/LifecycleSignalFilterTests.cs"
Task: "FakeWebSocketTests em Assets/Tests/EditMode/Net.Core/Fakes/FakeWebSocketTests.cs"
Task: "FakeHttpTransportTests em Assets/Tests/EditMode/Net.Core/Fakes/FakeHttpTransportTests.cs"

# Tipos das quatro portas, juntos:
Task: "Socket em Assets/Scripts/Net/Core/Socket/"
Task: "Http em Assets/Scripts/Net/Core/Http/"
Task: "Lifecycle em Assets/Scripts/Net/Core/Lifecycle/"
Task: "Reachability em Assets/Scripts/Net/Core/Reachability/"
```

## Parallel Example: User Story 2

```bash
Task: "JObjectPayloadReaderTests em Assets/Tests/EditMode/Net.Json/JObjectPayloadReaderTests.cs"
Task: "NewtonsoftProtocolCodecDecodeTests em Assets/Tests/EditMode/Net.Json/NewtonsoftProtocolCodecDecodeTests.cs"
Task: "GenericServerFramesTests em Assets/Tests/EditMode/Net.Json/GenericServerFramesTests.cs"
Task: "Identity em Assets/Scripts/Net/Core/Identity/"
Task: "DecodeOutcome e falhas em Assets/Scripts/Net/Core/Protocol/"
```

## Parallel Example: User Story 3

```bash
Task: "MainThreadQueueTests em Assets/Tests/EditMode/Net.Core/Threading/MainThreadQueueTests.cs"
Task: "WebSocketMessageAssemblerTests em Assets/Tests/EditMode/Net.Unity/WebSocketMessageAssemblerTests.cs"
Task: "UnityWebRequestErrorClassifierTests em Assets/Tests/EditMode/Net.Unity/UnityWebRequestErrorClassifierTests.cs"
Task: "Relógios em Assets/Scripts/Net/Unity/StopwatchMonotonicClock.cs, BootTimeMonotonicClock.cs, PlatformMonotonicClock.cs"
Task: "UnityConsoleLog em Assets/Scripts/Net/Unity/UnityConsoleLog.cs"
```

## Parallel Example: User Story 4

```bash
Task: "ServerHostTests em Assets/Tests/EditMode/Net.Core/Environment/ServerHostTests.cs"
Task: "AppConfigTests em Assets/Tests/EditMode/Config/AppConfigTests.cs"
Task: "ReleaseBuildTlsGateTests em Assets/Tests/EditMode/Net.Editor/ReleaseBuildTlsGateTests.cs"
Task: "insecureHttpOption em ProjectSettings/ProjectSettings.asset"
```

---

## Implementation Strategy

### MVP First (User Story 1)

1. Phase 1 (Setup) e Phase 2 (Foundational).
2. Phase 3 (US1): portas e fakes.
3. **Parar e validar**: rodar a suíte. A feature 2 já pode escrever testes de login com `FakeHttpTransport`.

### Incremental Delivery

1. Setup e fundação.
2. US1: portas e fakes testáveis.
3. US2: codec e identificadores (junto com US1, se houver duas frentes).
4. US3: adaptadores reais e LiveServer. Destrava a feature 3.
5. US4: ambiente e aparelho. Destrava o teste manual no Android.
6. Polish: portas de qualidade e validação no aparelho.

### Parallel Team Strategy

- Depois da fundação: uma frente em US1, outra em US2, e uma terceira em
  T083–T103 de US4.
- US3 começa quando US1 e US2 fecham; o probe (T104) entra por último.

---

## Notes

- Fakes não têm asmdef de teste própria. São verificados em
  `Anathema.Net.Core.Tests` (T014, T025–T028), que é quem os usa.
- `ConnectionClient.cs` e `AppEnvManager.cs` não são editados.
- Commit ao fim de cada tarefa ou grupo lógico, sempre com os `.meta`.
- Se um contrato do backend e esta lista discordarem, vale o contrato.

## Registro da execução (2026-09-13)

- O `Unity.exe` aberto por esta sessão falha na inicialização (`wakeup_pipes_init: bind () failed`, dentro e fora do sandbox), então a suíte oficial não rodou aqui. As tarefas que exigem o Unity continuam abertas: T009, T019, T037, T056, T082, T103, T105, T109, T110 e T111.
- Substituto usado: todas as assemblies da feature (Core, Json, Fakes, Unity, Config, Editor e as cinco de teste) compilaram com o Roslyn do próprio Unity 6000.2.8f1 contra as DLLs do editor, sem erro nem aviso, também com `UNITY_ANDROID`. Os testes de `Anathema.Net.Core.Tests` e `Anathema.Net.Json.Tests` rodaram no Mono do Unity pelo NUnit do pacote: 187 passaram, 0 falharam. Os de `Net.Unity`, `Net.Editor` e `Config` só compilaram.
- T005: a asmdef de editor não referencia `Unity.Android.Gradle`. T101 seguiu o caminho alternativo (`IPostGenerateGradleAndroidProject`); o motivo está em research.md, R3.
- `AppConfig` continua no namespace global, e não em `Anathema.Config`, para o código antigo que o usa sem `using` compilar sem mudança.
- Os arquivos `.meta` dos arquivos novos só existem depois que o Unity abrir o projeto.
- Código antigo (`Assembly-CSharp`, 29 arquivos) compilado com as mudanças de `AppConfig`, `NetworkBootstrap` e `LoginController`: 0 erros; os avisos que aparecem nesses arquivos (CS0649 dos campos `[SerializeField]` do `LoginController`) já existiam.
- Primeira rodada no Test Runner do editor (2026-09-13): 265 passaram, 8 falharam, 3 pulados (LiveServer). As 8 falhas eram de `Anathema.Net.Editor.Tests`: o `EnvironmentSelectorTestComponent` (T087) era um MonoBehaviour dentro de asmdef só de editor, e o Unity recusa `AddComponent` nele. Corrigido: os testes usam o próprio `AppEnvManager`, achado pelo `MonoScript`, e o componente substituto foi removido.
- Antes disso, o Play quebrou o login com `UriFormatException`: a recarga de scripts devolvia `""` para o campo privado `AppConfig.overrideHost`. Corrigido com `[NonSerialized]` e teste de regressão em `AppConfigTests`.
- LiveServer no Test Runner do editor (2026-09-13), backend local no ar: os 3 testes passaram (401, `auth_denied` + 4001, `ping`/`pong` com marcador), com todos os eventos na thread principal (T082, SC-003, SC-004).
- T110: os 151 arquivos novos em `Assets/` têm `.meta`; `ConnectionClient.cs` continua no projeto.
- T105 (2026-09-13): build sem Development Build com `isProd` desligado falhou no `ReleaseBuildTlsGate` com a mensagem citando a cena, `AppConfig_Dev`, `isProd=false` e `useTls=false` (SC-007).
- Suíte EditMode (2026-09-13): o mantenedor rodou o Run All no Test Runner depois da correção dos testes do editor e confirmou a suíte verde (T009, T019, T037, T056, T103, T109). Fica aberto só o T111, a validação no aparelho Android.
