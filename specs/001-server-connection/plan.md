# Implementation Plan: Conexão com o servidor

**Branch**: nenhum (spec no `main`) | **Date**: 2026-09-13 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/001-server-connection/spec.md`

## Summary

Esta feature constrói a base da camada de rede do cliente:

- **Portas do núcleo**: seis (socket, HTTP, relógio monotônico, ciclo de vida,
  alcançabilidade, log), cada uma com um fake nomeado.
- **Adaptadores reais**: `ClientWebSocket`, `UnityWebRequest`, relógio de alta
  resolução, callbacks de pausa e foco, `Application.internetReachability` e
  `Debug`.
- **Fila única** que entrega os eventos na thread principal.
- **Codec do protocolo** sobre Newtonsoft. Lê cada campo explicitamente, sem
  desserialização por reflexão, e traz a união fechada por discriminador
  reutilizável.
- **Identificadores tipados**.
- **Ambiente do Android**: host trocável só em desenvolvimento; cleartext
  liberado só no build de desenvolvimento; build de produção sem TLS bloqueado
  na hora do build.

Contratos do protocolo em `C:/Users/gabri/Projetos/dev_container/anathema/backend/specs/`
(`009-match-protocol`, `011-deck-catalog-api`, `013-socket-heartbeat`). Não são
copiados aqui.

Duas decisões do research mudam o que a descrição da feature dizia:

- **O relógio do Android não é `Stopwatch`.** No IL2CPP, o `Stopwatch` lê
  `CLOCK_MONOTONIC`, que para enquanto o aparelho dorme. A duração fora do app
  sairia menor que a real, e o token de 5 minutos venceria sem o cliente saber
  (FR-016). Por isso o Android lê `CLOCK_BOOTTIME`. O Windows continua com
  `Stopwatch`, que já conta o sono (research R1).
- **A plataforma não bloqueia o socket no Android.** A liberação de cleartext do
  Unity e a Network Security Config do Android só alcançam o HTTP. O
  `ClientWebSocket` abre socket nativo e passa por fora das duas. Em produção, o
  bloqueio de `ws://` é uma política do projeto aplicada no próprio adaptador
  (research R3).

## Technical Context

**Language/Version**: C# 9 (Unity 6000.2.8f1), `#nullable enable` em todo arquivo novo; .NET Standard 2.1 (`apiCompatibilityLevel: 6`)

**Primary Dependencies**: Newtonsoft JSON 3.2.1 (só em `Anathema.Net.Json`); `System.Net.WebSockets.ClientWebSocket`; `UnityWebRequest`; Unity Test Framework 1.6.0; `Unity.Android.Gradle` (só no editor, para o manifesto)

**Storage**: N/A (nada persistido; o host de desenvolvimento vale só para a execução)

**Testing**: Unity Test Framework em EditMode, pelo comando único da constituição; testes contra o backend com `[Explicit]` + `[Category("LiveServer")]`

**Target Platform**: Windows desktop e Android (IL2CPP, ARM64, `minSdk 23`, entrada GameActivity)

**Project Type**: biblioteca interna do cliente Unity (camada de rede), consumida pelas features 2 a 5

**Performance Goals**: testes EditMode da feature somam < 5 s (SC-009); fila da thread principal entrega 10.000 itens de 4 produtores sem perda nem troca de ordem (SC-005)

**Constraints**: núcleo sem referência a UnityEngine, Newtonsoft ou `ClientWebSocket` (SC-002); nenhum evento real fora da thread principal (SC-004); nenhuma exceção escapa do codec (SC-006); código antigo compila sem mudança de comportamento, exceto o listado em "Código anterior tocado"

**Scale/Scope**: 5 assemblies de produção, 1 de fakes e 5 de teste; ~45 tipos pequenos; 3 testes LiveServer

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Princípio / regra | Como o plano cumpre | Estado |
|---|---|---|
| I. Contratos são a fonte | Codec implementa envelope, `message_refused`, `auth_denied`, `ping`/`pong` lendo os contratos 009/011/013 por caminho; nenhum contrato copiado | ✅ |
| II. Servidor é a autoridade | Nenhuma regra de jogo; recusa exposta por `code`; `error` só como texto | ✅ |
| III. Identidade tipada | `UserId`, `MatchId`, `CardInstanceId` como structs distintas; leitura e escrita só por métodos do reader/writer com nome de campo explícito | ✅ |
| IV. Tipos explícitos | União fechada com braço desconhecido; `JObject`/`JToken` confinados em `Anathema.Net.Json`; `IPayloadReader` é tipo do projeto | ✅ (ver Complexity Tracking) |
| V. Unidades pequenas | Um tipo público por arquivo; nomes conferidos contra o projeto (`MainThreadQueue`, `NetworkLayerHost`; sem `Manager`/`Helper`/`Dispatcher`, que já existe) | ✅ |
| VI. Núcleo independente | `Anathema.Net.Core` e `Anathema.Net.Json` com `noEngineReferences: true`; único `MonoBehaviour` novo é `NetworkLayerHost`; troca de thread só em `MainThreadQueue`; injeção por construtor | ✅ |
| VII. Fakes nomeados | `Anathema.Net.Fakes` com os seis fakes; tempo injetado; LiveServer marcado | ✅ |
| Plataformas | Só Windows e Android; o `#if UNITY_ANDROID` do relógio escolhe entre os dois alvos | ✅ |
| Cleartext só em desenvolvimento | `insecureHttpOption = DevelopmentOnly`; `usesCleartextTraffic` só em build de desenvolvimento; `CleartextPolicy` no socket e no HTTP; gate de build | ✅ |
| JSON pelo codec do projeto | Newtonsoft só atrás de `IProtocolCodec` | ✅ |
| Log por interface | `IClientLog`; `Debug` só em `UnityConsoleLog` | ✅ |
| Código anterior evolui no lugar | `AppConfig` evolui no mesmo arquivo; `NetworkBootstrap` e `LoginController` recebem só a remoção da rota morta (abaixo) | ✅ |

**Pós-design (Phase 1)**: reavaliado depois de `data-model.md` e `contracts/`. Nenhuma violação nova. As duas exceções continuam só as da tabela Complexity Tracking.

### Código anterior tocado

| Arquivo | Mudança | Por quê | Fica para depois |
|---|---|---|---|
| `Assets/Scripts/Core/Config/AppConfig.cs` | Sai `connectionConsumerUrl`. O host efetivo passa a ser o configurado, ou o da inicialização em build de desenvolvimento. `HttpUrl`/`WsUrl` usam o host efetivo | FR-041 a FR-044 | revisar `tokenRefreshEndpoint`, `playerMe` e rotas na feature 2 |
| `Assets/Scripts/Core/Config/Anathema.Config.asmdef` (novo, mesma pasta) | `AppConfig` passa a compilar numa asmdef, sem mover o arquivo (GUID e assets preservados) | o gate de build e os testes precisam referenciar `AppConfig`, e asmdef não referencia `Assembly-CSharp` | — |
| `Assets/Config/AppConfig_Dev.asset`, `AppConfig_Prod.asset` | some a chave `connectionConsumerUrl` | FR-042 | — |
| `Assets/Scripts/Bootstrap/NetworkBootstrap..cs` | saem a propriedade e a construção de `ConnectionClient` | sem a rota, o arquivo não compila | religar nas portas novas: feature 3 |
| `Assets/Scripts/Login/LoginController.cs` | depois do login, carrega `HomeScene` direto, sem esperar o socket de presença | o socket de presença não existe mais no backend; hoje o login nunca sai da tela | login nas portas novas: feature 2 |
| `ProjectSettings/ProjectSettings.asset` | `insecureHttpOption: 0 → 1` (DevelopmentOnly) | FR-045 | — |

`ConnectionClient.cs` fica no projeto, sem uso, e sai com a NativeWebSocket na
feature 3. `AppEnvManager` não muda.

## Project Structure

### Documentation (this feature)

```text
specs/001-server-connection/
├── plan.md              # este arquivo
├── research.md          # Phase 0
├── data-model.md        # Phase 1
├── quickstart.md        # Phase 1
├── contracts/
│   ├── core-ports.md        # portas do núcleo e garantias dos adaptadores
│   ├── protocol-codec.md    # codec, reader/writer, união, frames genéricos
│   └── environment-build.md # AppConfig, host de desenvolvimento, cleartext, gate de build
├── checklists/
│   └── requirements.md
└── tasks.md             # Phase 2 (/speckit-tasks)
```

### Source Code (repository root)

```text
Assets/Scripts/Net/
├── link.xml                                  # preserva Anathema.Net.Core e Anathema.Net.Json no IL2CPP
├── Core/                                     # Anathema.Net.Core (noEngineReferences, sem referências)
│   ├── Anathema.Net.Core.asmdef
│   ├── Identity/        UserId, MatchId, CardInstanceId
│   ├── Socket/          IWebSocket, SocketClosure, SocketSendOutcome, CleartextPolicy
│   ├── Http/            IHttpTransport, HttpRequestSpec, HttpOutcome, TransportFailureKind
│   ├── Time/            IMonotonicClock, MonotonicInstant
│   ├── Lifecycle/       IAppLifecycle, WentToBackground, ReturnedToForeground, LifecycleSignalFilter
│   ├── Reachability/    INetworkReachability, NetworkKind, NetworkKindChanged
│   ├── Logging/         IClientLog, ClientLogEntry, LogField, ClientLogLevel
│   ├── Threading/       MainThreadQueue
│   ├── Protocol/        IProtocolCodec, IPayloadReader, IPayloadWriter, PayloadShapeException,
│   │                    DecodeOutcome, DecodeFailure, DecodeFailureKind, DiscriminatedUnion,
│   │                    IOutgoingMessage, PingMessage, PingMarker
│   ├── Protocol/Frames/ ServerFrame, MessageRefusedFrame, AuthDeniedFrame, PongFrame,
│   │                    UnknownServerFrame, GenericServerFrames
│   └── Environment/     ServerHost, ReleaseTlsRule
├── Json/                                     # Anathema.Net.Json (noEngineReferences; Core + Newtonsoft.Json.dll)
│   ├── Anathema.Net.Json.asmdef
│   ├── NewtonsoftProtocolCodec.cs
│   ├── JObjectPayloadReader.cs
│   └── JObjectPayloadWriter.cs
├── Unity/                                    # Anathema.Net.Unity (Core, Json)
│   ├── Anathema.Net.Unity.asmdef
│   ├── DotNetWebSocket.cs
│   ├── UnityHttpTransport.cs, UnityWebRequestErrorClassifier.cs
│   ├── StopwatchMonotonicClock.cs, BootTimeMonotonicClock.cs, PlatformMonotonicClock.cs
│   ├── UnityAppLifecycle.cs
│   ├── UnityNetworkReachability.cs
│   ├── UnityConsoleLog.cs
│   ├── NetworkLayerHost.cs                   # único MonoBehaviour novo
│   ├── LaunchServerHostReader.cs
│   └── Probe/           LiveServerProbeSteps, DevConnectionProbe
└── Editor/                                   # Anathema.Net.Editor (Editor only; Core, Config)
    ├── Anathema.Net.Editor.asmdef
    ├── ReleaseBuildTlsGate.cs                # IProcessSceneWithReport
    ├── EnvironmentSelectionReader.cs
    └── DevCleartextManifest.cs               # IPostGenerateGradleAndroidProject (research R3)

Assets/Scripts/Core/Config/
├── Anathema.Config.asmdef                    # novo (Core, Anathema.Net.Unity)
└── AppConfig.cs                              # evolui no lugar

Assets/Tests/EditMode/
├── Fakes/     Anathema.Net.Fakes.asmdef — FakeWebSocket, FakeHttpTransport, FakeMonotonicClock,
│              FakeAppLifecycle, FakeNetworkReachability, FakeClientLog
├── Net.Core/  Anathema.Net.Core.Tests.asmdef
├── Net.Json/  Anathema.Net.Json.Tests.asmdef
├── Net.Unity/ Anathema.Net.Unity.Tests.asmdef (adaptadores; LiveServer)
├── Net.Editor/ Anathema.Net.Editor.Tests.asmdef
└── Config/    Anathema.Config.Tests.asmdef
```

**Structure Decision**: pasta nova `Assets/Scripts/Net/`, com uma asmdef por
camada, ao lado do código antigo (`Assets/Scripts/Core/Network/`), que não é
movido.

- **Codec**: fica fora do núcleo porque referencia Newtonsoft (SC-002), mas
  também não referencia o motor.
- **Adaptadores e hospedeiro**: em `Anathema.Net.Unity`.
- **Gate de build**: numa asmdef de editor.
- **Fakes**: asmdef própria, para as features seguintes reutilizarem (FR-022).

O `Anathema.Reconnect` antigo não é tocado.

Dependências, só no sentido borda → núcleo:

```text
Anathema.Net.Editor ─► Anathema.Config ─► Anathema.Net.Unity ─► Anathema.Net.Json ─► Anathema.Net.Core
Anathema.Net.Editor ─► Anathema.Net.Core
Anathema.Net.Fakes  ─► Anathema.Net.Core
Assembly-CSharp (código antigo) ─► Anathema.Config (autoReferenced)
```

## Complexity Tracking

| Violation | Why Needed | Simpler Alternative Rejected Because |
|---|---|---|
| `EnvironmentSelectionReader` lê `isProd`, `configDev` e `configProd` do `AppEnvManager` por nome de campo serializado (texto), não pelo tipo | O gate precisa saber qual `AppConfig` a cena seleciona. `AppEnvManager` está em `Assembly-CSharp`, que nenhuma asmdef referencia, e a feature não pode mudá-lo | Mover `AppEnvManager` para asmdef mexe em código antigo fora do escopo. Checar todo `AppConfig` do projeto bloquearia todo build de produção enquanto o de desenvolvimento existir. Os nomes ficam em constantes com referência ao arquivo, e um teste falha se um campo sumir |
| Braços da união leem campos por métodos que lançam `PayloadShapeException`, capturada em `DiscriminatedUnion` | Mantém cada braço linear, com 4-20 linhas; sem isso cada campo vira um `if` de falha | Encadear `DecodeOutcome` campo a campo em C# 9 estoura o limite de indentação e de linhas por método. A exceção nunca sai do codec (FR-033) e carrega campo, valor e forma esperada |
