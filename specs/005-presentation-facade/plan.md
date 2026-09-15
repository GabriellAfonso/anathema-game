# Implementation Plan: Fachada da apresentação e prova final

**Branch**: nenhum (spec no `main`) | **Date**: 2026-09-14 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/005-presentation-facade/spec.md`

## Summary

Última feature da camada de rede. Junta as peças da 001–004 atrás de uma
superfície única, tira os singletons de composição e de cena, e prova com dois
clientes headless que a superfície basta.

- **Fachada** (`Anathema.Net.Facade`, núcleo):
  - `AnathemaClient` com conta, catálogo, decks, histórico, fila, partida
    corrente (`LiveMatch`) e saúde da conexão;
  - estado do app fechado em sete estágios, trocado inteiro e avisado na thread
    principal;
  - linha do histórico buscada depois do fim, em 4 leituras em 7 s, pelo tique e
    pelo relógio injetado;
  - sessão expirada de qualquer origem leva ao login;
  - catálogo indisponível, partida recusada e conexão desistida viram
    `MatchUnavailable`, com tentar de novo e voltar.
- **Assinatura descartável**: `EventFeed<T>` no núcleo. Os eventos públicos de
  `LiveMatch`, `MatchMirror`, `TurnClock` e `PendingPlay` viram feeds, no lugar.
- **Regra de consumo**:
  - o contrato [presentation-surface.md](./contracts/presentation-surface.md)
    lista o que fica público;
  - todo o resto das assemblies de rede vira `internal`, com
    `InternalsVisibleTo` só entre as assemblies da camada e os testes;
  - apresentação, cenas e prova nunca são amigas, então usar tipo de máquina é
    erro de compilação;
  - `SurfaceContractTests`, `SurfaceHasNoEventTests` e `FriendAssemblyTests`
    fecham a fronteira.
- **Composição** (`Anathema.Net.Unity`): `ClientComposition.Compose(options)` →
  `ComposedClient`, com `Pump`, `AttachTo`, e `DropSockets` e `JumpClock` para a
  prova. `LiveAccountServices` e `LiveConnectionServices` descem para a fachada.
- **Cenas** (`Anathema.Client.Scenes`):
  - `ClientHost` (o `PlayerSession.cs` renomeado) compõe;
  - `SceneRouter` segue o estágio;
  - `SceneClientBinder` injeta a fachada em `ISceneClientConsumer`;
  - os scripts de cena vão para `Anathema.Presentation`;
  - `BaseClient`, `MatchClient`, `MatchmakingClient`, `MatchSession`,
    `VersusContext`, `SelfProfileService` e `AppEnvManager` saem.
- **Prova** (`Anathema.Client.Proof`, só desenvolvimento): o roteiro da US9 com
  os bots do `smoke_match.py` estendidos para cobrir os 11 comandos. Roda no
  editor por um teste `LiveServer` e no Android pela `MatchProofScene`.

Contratos do backend, citados e não copiados, em
`C:/Users/gabri/Projetos/dev_container/anathema/backend/specs/`:
`009-match-protocol/contracts/`, `010-match-timers/contracts/`,
`011-deck-catalog-api/contracts/`,
`012-match-result-history/contracts/http_match_history.md`; e
`scripts/smoke_match.py`.

Achados do research que completam a spec:

- **Não transitividade das asmdefs**: a apresentação referencia a fachada e as 4
  assemblies de modelo. "Só a superfície" vem de `internal` + amizades, não da
  lista de referências (R1, R2).
- **Evento do C# não é descartável**: por isso os eventos dos tipos da superfície
  da 004 viram `EventFeed<T>` (R3).
- **Guarda do Android sem slot**: dois clientes no aparelho dividiriam a guarda;
  o slot entra na composição e na guarda do Android (R8).
- **Aviso de fim de vez vindo do `turn_warning`**: durante a vez parada não chega
  `match_update`, então o aviso só pode ter vindo do `turn_warning` (R11).
- **Renovação antes da reabertura**: a conexão da 003 sempre pede o token antes
  de abrir, então a ordem das linhas de log é a prova (R11).
- **Roteador por "cena carregada"**: o critério não pode ser "cena ativa", porque
  o `ForceBootstrapOnPlay` abre a `LoginScene` como aditiva (R9).

## Technical Context

**Language/Version**: C# 9 (Unity 6000.2.8f1), `#nullable enable` em todo arquivo novo; .NET Standard 2.1

**Primary Dependencies**:
- da 001: `IHttpTransport`, `IWebSocketFactory`, `IMonotonicClock`, `IFrameTicker`, `IAppLifecycle`, `INetworkReachability`, `MainThreadQueue`, `IClientLog`, `IProtocolCodec`, `IRefreshTokenVault`, adaptadores Unity e fakes;
- da 002: `AccountSession`, `SessionAccessTokens`, `AuthenticatedHttpClient`, `OwnProfileQuery`, `AccountRegistration`, `CardCatalog`, `PlayerDecks`, `MatchHistory`, `ForegroundRenewal`;
- da 003: `AuthenticatedConnection`, `MatchQueue`, `ConnectionStatus`, `GiveUpReason`;
- da 004: `LiveMatch`, `MatchMirror`, `TurnClock`, `MatchCommands`, `PendingPlay`, `MatchNarrator`;
- Unity: TextMeshPro, UGUI, Multiplayer Play Mode 1.6.3 (já no manifesto);
- Unity Test Framework 1.6.0.

Nenhum pacote novo.

**Storage**: guarda de refresh por slot nomeado (DPAPI no Windows, Android Keystore no Android); nada novo persistido

**Testing**:
- EditMode pelo comando único da constituição, com os fakes nomeados da 001–004 e a `MainThreadQueue` drenada pelo teste;
- fronteira por reflexão (`AssemblySignatureScanner` e tipos exportados);
- guardas de fonte (`SceneManager`, `static Instance`) lendo `Assets/Scripts/`;
- `[Explicit]` + `[Category("LiveServer")]` para a prova.

**Target Platform**: Windows desktop (player, editor, Multiplayer Play Mode) e Android

**Project Type**: biblioteca interna do cliente Unity (camada de rede) com a borda de cena que a consome

**Performance Goals**:
- testes EditMode da fachada < 5 s (SC-010);
- prova LiveServer < 15 min (SC-007);
- transição de estágio sem alocação além do `ClientState` novo e do aviso.

**Constraints**:
- fachada sem UnityEngine, Newtonsoft ou `System.Net.WebSockets`;
- nenhuma regra de jogo (FR-038);
- nenhum contorno de backend (FR-039);
- nenhuma espera real nos testes EditMode;
- nenhum aviso de compilação novo;
- nenhum `static Instance` e `SceneManager` só no roteador e no binder.

**Scale/Scope**:
- assemblies novas: 4 de produção e 3 de teste;
- assemblies existentes: 8 mudam de visibilidade e de `autoReferenced`;
- ~35 tipos novos;
- ~100 tipos passam a `internal`;
- 7 arquivos antigos removidos e 7 movidos;
- 3 cenas editadas e 1 cena nova de desenvolvimento;
- 1 prova LiveServer, que substitui o marco da 004.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Princípio / regra | Como o plano cumpre | Estado |
|---|---|---|
| I. Contratos são a fonte | Histórico lido pelo contrato 012 (`match_id`, `won`, `final_round`); fila e decks pelo 011; tempos do relógio conferidos em `turn_clock.py`; bots do `smoke_match.py`; `Decisões/` lida (só a 0001, perfil por usuário, sem efeito aqui) | ✅ |
| II. Servidor é a autoridade | Fachada só espelha estágio de fatos do servidor; nenhum estouro, legalidade ou dano; `won` do histórico e `DidIWin` não se corrigem entre si; estratégia da prova é escolha de teste e manda jogadas que o servidor recusa | ✅ |
| III. Identidade tipada | `MatchId`, `UserId`, `DeckId`, `CardInstanceId` em toda assinatura nova; nenhum membro `id`; a linha do histórico é casada por `MatchId` | ✅ |
| IV. Tipos explícitos | Estágio, motivos e status fechados em enum; `ClientState` com fábricas por estágio; nada de `object` fora do codec | ✅ |
| V. Unidades pequenas | Uma responsabilidade por tipo (`ClientStages`, `MatchOpening`, `HistoryRowLookup`, `HealthRelay`, `SceneRoute`, `SceneRouter`, `SceneClientBinder`); `AnathemaClient` só compõe e delega; nomes novos com 0 ocorrência (R14) | ✅ |
| VI. Núcleo independente | `Anathema.Net.Facade` com `noEngineReferences`; `MonoBehaviour` só em `Client.Scenes`, `Presentation`, `Proof` e `Net.Unity`; injeção pelo construtor e por `BindClient`; singletons removidos; troca de thread só pela `MainThreadQueue` | ✅ (ver Complexity Tracking: amizades) |
| VII. Fakes nomeados | Fakes existentes, que passam a internos com amizade para os testes; tempo pelo `FakeMonotonicClock` e pelo `FakeFrameTicker`; LiveServer marcado; uma asmdef de teste por asmdef nova com lógica | ✅ (ver Complexity Tracking: `Presentation` sem teste, `Proof.Tests` amiga) |
| Plataformas | Só Windows e Android; `#if UNITY_ANDROID` já existente na guarda; `defineConstraints` de desenvolvimento, não de plataforma | ✅ |
| Cleartext só em dev | `AllowCleartext` vem de `Debug.isDebugBuild` no hospedeiro; a prova só existe em build de desenvolvimento | ✅ |
| Refresh token protegido | Guarda por slot na plataforma; `SignOut` da prova apaga | ✅ |
| JSON pelo codec | Nada novo lê JSON; codec continua em `Anathema.Net.Json`, agora interno | ✅ |
| Log por interface | Fachada, composição, cenas e prova por `IClientLog` com campos; `Debug.Log` só no `UnityConsoleLog`; os `Debug.LogWarning` de `VersusController` e `MiniPlayerProfile` passam ao log recebido | ✅ |
| Código anterior evolui no lugar | `PlayerSession` → `ClientHost` com o mesmo `.meta`; `LiveAccountServices`/`LiveConnectionServices` movidos; eventos da 004 convertidos no lugar; removidos só os que já delegavam (tabela abaixo) | ✅ |
| Comentários preservados | Tabela "Destino do código antigo" diz para onde vai cada comentário | ✅ |

**Pós-design (Phase 1)**: reavaliado depois de `data-model.md` e `contracts/`.
Nenhuma violação nova além das de Complexity Tracking.

- A troca de `event` por `EventFeed<T>` muda as chamadas dos testes da 004 de
  `+=` para `Subscribe`, sem mudar a ordem nem o conteúdo dos avisos.
  `MatchMirrorAnnouncementTests` continua verificando a mesma sequência.
- `SignInOutcomeKind.AlreadySignedIn` é valor novo num enum da 002. Nenhum
  `switch` existente fica sem braço, porque os consumidores atuais
  (`LoginController`) comparam por igualdade.

### Código anterior tocado

A tabela completa está em
[contracts/composition-and-scenes.md](./contracts/composition-and-scenes.md#destino-do-código-antigo).
Além dela:

| Arquivo | Mudança | Por quê | Fica para depois |
|---|---|---|---|
| Todas as asmdefs de `Assets/Scripts/Net/` e `Anathema.Config.asmdef` | `autoReferenced: false`; `Anathema.Net.Unity` + `Anathema.Net.Facade` | R1 | — |
| `*AssemblyInfo.cs` de Core, Json, Account, Connection, Match, Unity, Fakes | amizades da tabela | R2 | — |
| Tipos de máquina de Core, Json, Account, Connection, Match, Unity e Fakes | `public` → `internal`; membros que citam tipo interno → `internal` | R2, FR-021 | — |
| `Match/Commands/PlayCommand.cs` | escrita do payload por implementação explícita + `internal abstract` | R2 | — |
| `Match/Session/LiveMatch.cs`, `Mirror/MatchMirror.cs`, `Clock/TurnClock.cs`, `Commands/PendingPlay.cs` | `event` → `EventFeed<T>`; construtores internos | R3, FR-020 | — |
| `Account/Session/SignInOutcomeKind.cs` | + `AlreadySignedIn` | R4, FR-008 | — |
| Testes de Account, Connection, Match, Unity, Json, Config | `+=` → `Subscribe`; `[TestCase]` com tipo interno → nome/número; composição pela fachada onde usavam `LiveAccountServices`/`LiveConnectionServices` | R2, R3 | — |
| `Tests/EditMode/Net.Unity/LiveServer/LiveMatchTests.cs`, `SmokeBot.cs`, `SmokeStrategy.cs`, `SmokeStrategyTests.cs`, `SmokeCatalog.cs` | removidos; evoluem para `Anathema.Client.Proof` e `Anathema.Client.Proof.Tests` | R11 | — |
| `Tests/EditMode/Net.Unity/LiveServer/LivePlayer.cs`, `LiveQueueTests.cs`, `LiveAccountTests.cs` | compõem pelas peças internas da fachada | R8 | — |
| `Tests/EditMode/Net.Unity/LiveAccountServicesTests.cs`, `LiveConnectionServicesTests.cs` | movidos para `Anathema.Net.Facade.Tests/Composition/` | R8 | — |
| `Scenes/LoginScene.unity`, `HomeScene.unity`, `VersusScene.unity` | `m_EditorClassIdentifier` dos scripts movidos atualizado (referência por GUID não muda) | R9 | — |
| `CLAUDE.md` | linha apontando o contrato da superfície | FR-023 | — |
| Vault `Game/TODO.md` | itens da R13 | FR-032 | — |

## Project Structure

### Documentation (this feature)

```text
specs/005-presentation-facade/
├── plan.md              # este arquivo
├── research.md          # Phase 0
├── data-model.md        # Phase 1
├── quickstart.md        # Phase 1
├── contracts/
│   ├── presentation-surface.md   # regra de consumo e lista de tipos públicos (porta de entrada da parte visual)
│   ├── client-state.md           # estágios, conta, fila, indisponível, histórico, saúde, descarte, log
│   ├── composition-and-scenes.md # composição, hospedeiro, roteador, destino do código antigo, amizades
│   └── match-proof.md            # roteiro, bots, execução no editor e no aparelho
├── checklists/
│   └── requirements.md
└── tasks.md             # Phase 2 (/speckit-tasks)
```

### Source Code (repository root)

```text
Assets/Scripts/Net/
├── Core/Notices/          EventFeed                                               # novo
├── Core, Json, Account, Connection, Match, Unity   *AssemblyInfo, asmdef, visibilidade   # evoluem
├── Account/Session/       SignInOutcomeKind                                        # evolui
├── Match/Session, Mirror, Clock, Commands   LiveMatch, MatchMirror, TurnClock, PendingPlay, PlayCommand  # evoluem
├── Facade/                                         # Anathema.Net.Facade (nova; noEngineReferences; Core, Account, Connection, Match)
│   ├── Anathema.Net.Facade.asmdef, FacadeAssemblyInfo.cs
│   ├── AnathemaClient, ClientAccount, ClientQueue, ConnectionHealth
│   ├── State/        ClientStage, ClientState, ClientStageChange, SignedOutReason, StageRequestResult, ClientStages
│   ├── Queue/        QueueJoinResult, QueueJoinKind, QueueExit, QueueExitKind
│   ├── Health/       ReconnectingNotice, RecoveredNotice, GaveUpNotice, HealthRelay
│   ├── Match/        MatchOpening, MatchResult, HistoryRowStatus, HistoryRowLookup, MatchUnavailable, MatchUnavailableKind
│   └── Composition/  ClientPorts, AccountServices (ex-LiveAccountServices), ConnectionServices (ex-LiveConnectionServices)
└── Unity/             ClientComposition, ClientCompositionOptions, ComposedClient, ServerRoutes,
                       SteppableMonotonicClock, DroppableWebSocketFactory                # novos
                       NetworkLayerHost, Storage/PlatformRefreshTokenVault, Storage/AndroidKeystoreRefreshTokenVault  # evoluem

Assets/Scripts/Core/Config/AppConfig.cs                                           # evolui (BuildServerRoutes)
Assets/Scripts/Client/
├── Scenes/            Anathema.Client.Scenes.asmdef; ClientHost (ex-PlayerSession.cs), SceneRouter, SceneRoute,
│                      SceneClientBinder, ISceneClientConsumer, SceneSubscriptions
└── Proof/             Anathema.Client.Proof.asmdef (UNITY_EDITOR || DEVELOPMENT_BUILD); MatchProofScript, ProofSetup,
                       ProofPlayer, ProofBot, ProofStrategy, CommandCoverage, ProofLog, ProofStep, ProofFailure,
                       ProofWait, LocalServerRoutes, MatchProofRunner
Assets/Scripts/Presentation/   Anathema.Presentation.asmdef; LoginController, PlayButton (MyButtonScript),
                               VersusController, MiniPlayerProfile, ReconnectOverlay                # movidos
Assets/Scenes/                 BootstrapScene, LoginScene, HomeScene, VersusScene (editadas); Dev/MatchProofScene (nova)

Removidos: Bootstrap/AppEnvManager.cs, Core/Network/WebSocketClient/{Base,MatchClient,MatchmakingClient}.cs,
           Core/Session/MatchSession.cs, Core/Contexts/VersusContext.cs, Core/Network/SelfProfileService.cs,
           DTO/Player/PlayerPublicDTO.cs (com .meta e pastas vazias)

Assets/Tests/EditMode/
├── Net.Core/          EventFeedTests                                              # novo
├── Net.Facade/        Anathema.Net.Facade.Tests.asmdef (Core, Json, Account, Connection, Match, Facade, Fakes)
│   ├── FacadeTestRig
│   ├── State/         SignInStageTests, QueueStageTests, MatchStageTests, ReturnStageTests, StageMismatchTests,
│   │                  StageChangeDeliveryTests, SignOutStageTests, SessionExpiryStageTests, SessionExpiryOrderTests
│   ├── Account/       ClientAccountTests
│   ├── Queue/         ClientQueueTests
│   ├── Match/         MatchUnavailableTests, HistoryRowLookupTests
│   ├── Health/        ConnectionHealthTests
│   ├── Composition/   AccountServicesTests, ConnectionServicesTests                 # movidos
│   └── FacadeFeedDisposalTests, FacadeDisposeTests, FacadeAssemblyBoundaryTests,
│       SurfaceContractTests, SurfaceHasNoEventTests, FriendAssemblyTests
├── Client.Scenes/     Anathema.Client.Scenes.Tests.asmdef; SceneRouteTests, SceneManagerUsageTests, StaticInstanceUsageTests
├── Client.Proof/      Anathema.Client.Proof.Tests.asmdef (Proof, Facade, Unity, Core, Json, Account, Connection, Match, Fakes)
│                      ProofStrategyTests, CommandCoverageTests, ProofCatalog; LiveServer/MatchProofTests
├── Net.Unity/         ClientCompositionTests, SteppableMonotonicClockTests, DroppableWebSocketFactoryTests  # novos
│                      LiveServer/LivePlayer, LiveQueueTests, LiveAccountTests evoluem; marco da 004 sai
├── Config/            AppConfigRoutesTests                                        # evolui
└── Fakes/             FakesAssemblyInfo (amizades), fakes → internal              # evoluem
```

**Structure Decision**:
- A fachada fica ao lado das assemblies da 001–004 em `Assets/Scripts/Net/`,
  acima da partida e abaixo da borda Unity.
- A borda de cena e a prova ficam em `Assets/Scripts/Client/`, separadas da
  camada de rede.
- Os scripts de tela ficam em `Assets/Scripts/Presentation/`.
- `Assembly-CSharp` fica só com o que não fala com a rede.

Dependências, só no sentido borda → núcleo:

```text
Anathema.Presentation ─► Anathema.Client.Scenes ─► Anathema.Net.Unity ─► Anathema.Net.Facade ─► Anathema.Net.Match ─► Anathema.Net.Connection ─► Anathema.Net.Account ─► Anathema.Net.Core
     └─► Facade, Match, Connection, Account, Core      │                    └─► Anathema.Net.Json ─────────────────────────────────────────────────────────────► Core
                                                        └─► Anathema.Config ─► Anathema.Net.Unity
Anathema.Client.Proof ─► Facade, Unity, Config, Match, Connection, Account, Core
Anathema.Net.Facade.Tests ─► Facade, Match, Connection, Account, Json, Core, Fakes
Anathema.Client.Proof.Tests ─► Proof, Facade, Unity, Match, Connection, Account, Json, Core, Fakes
Anathema.Client.Scenes.Tests ─► Client.Scenes, Facade, Core
```

## Complexity Tracking

| Violation | Why Needed | Simpler Alternative Rejected Because |
|---|---|---|
| ~40 declarações de `InternalsVisibleTo` em 8 assemblies, e fakes internos | FR-021 exige que usar tipo fora da superfície seja erro de compilação, e a apresentação precisa referenciar as assemblies de modelo, porque as asmdefs não são transitivas (R1, R2) | Espelhar os modelos na fachada duplicaria `PlayerView`, 19 eventos, cartas, decks e recusas. Separar modelo e máquina em assemblies move ~150 arquivos e esbarra nos leitores de payload. Analisador Roslyn é pacote novo sem suporte garantido. Teste de IL só falha em teste, não na compilação |
| Mudança de visibilidade em arquivos da 001–004 que a fachada não usa diretamente | A fronteira é por assembly: um tipo público esquecido em qualquer arquivo abre a superfície | Deixar parte pública e listar como "tolerada" tornaria o contrato incompleto e o `SurfaceContractTests` sem sentido |
| `Anathema.Client.Proof.Tests` amiga de Core, Json, Account e Match | `ProofStrategyTests` e `CommandCoverageTests` precisam de `LoadedCatalog` e `PlayerView` montados pelos leitores internos, como a `SmokeStrategyTests` da 004 | A garantia da prova vale para `Anathema.Client.Proof`, que não é amiga de ninguém. O `MatchProofTests` só bombeia e chama `MatchProofScript.RunAsync`. Uma asmdef de teste a mais contraria "uma asmdef de teste por asmdef testada" |
| `Anathema.Presentation` sem assembly de teste | São `MonoBehaviour` de tela que só leem estado e chamam a fachada. Lógica de estágio, fila, conta e cena está em `Facade.Tests` e `Scenes.Tests` | Testar tela exige cena (fora do EditMode sem cena); o fluxo é provado pelo quickstart §4 |
| `ComposedClient.DropSockets` e `JumpClock` em código de produção | A prova precisa derrubar socket e vencer o token com adaptadores reais (US9, cenários 5 e 6), e a composição da prova é a mesma do jogo (FR-025) | Composição separada para a prova deixaria de provar a composição do jogo. A apresentação não alcança `ComposedClient`, porque não referencia `Anathema.Net.Unity`, e `JumpClock` só funciona com `AllowClockJumps` |
| `MatchProofScene` incluída à mão no build de desenvolvimento | A lista de cenas do build é fixada antes de qualquer pré-processamento (R12) | Filtro de build novo mudaria o fluxo de produção por causa de uma prova manual; registrado no `Game/TODO.md` |
