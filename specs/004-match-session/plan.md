# Implementation Plan: Partida

**Branch**: nenhum (spec no `main`) | **Date**: 2026-09-14 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/004-match-session/spec.md`

## Summary

Esta feature constrói, sobre a conexão e a fila da 003, a conta da 002 e as portas
da 001, a assembly nova `Anathema.Net.Match`:

- **Formas do protocolo**:
  - `match_start`, `match_update` e `turn_warning` tipados, registrados na união
    do codec;
  - `PlayerView` completo, 19 eventos e 3 modificadores, cada família com braço
    desconhecido;
  - os 31 códigos de recusa;
  - as 11 mensagens de comando, só com `CardInstanceId`.
- **Espelho** (`MatchMirror` + `VersionGate`):
  - aplica ou descarta frames pela versão e substitui o estado inteiro;
  - expõe fatos prontos e avisos em ordem;
  - calcula atributos exibidos, só para exibição.
- **Relógio da vez** (`TurnClock`):
  - ancorado na chegada do frame e identificado por `turn_number`;
  - aviso único por vez;
  - relógio de mulligan;
  - nenhum temporizador.
- **Comandos, pendente e recusa** (`MatchCommands`, `PendingPlay`, `PlayRefusal`):
  - não envia fora de "conectado";
  - pendente só para a apresentação;
  - recusa associada ao último comando por melhor esforço.
- **Dicas** (`HandCardHints`): tipo da carta, alvo, candidatos, só na declaração,
  custo e energia, sem decidir.
- **Sessão** (`LiveMatch`):
  - estados conectando, ao vivo, reconectando (desatualizado), terminada,
    recusada e desistiu;
  - ressincroniza pelo `match_start`;
  - fecha o socket no fim;
  - narrador no log.
- **Código antigo evoluindo no lugar**:
  - `MatchClient` cria e hospeda a `LiveMatch`;
  - `MatchSession` delega para ela;
  - saem `MatchStateDTO`, `RawTextReceived` e o `JsonConvert` fora do codec.
- **O marco**: dois bots headless jogam uma partida inteira contra o backend
  local, com uma queda no meio, e o log conta a partida.

Contratos, citados e não copiados:
`C:/Users/gabri/Projetos/dev_container/anathema/backend/specs/009-match-protocol/contracts/`
(`client_messages.md`, `server_frames.md`, `refusal_codes.md`),
`.../009-match-protocol/data-model.md` (eventos),
`.../010-match-timers/contracts/` (`client_messages.md`, `server_frames.md`),
`.../server/apps/game/match/player_view.py` e `documents.py`.

Achados do research que completam a spec:

- **Instante de chegada** é lido na entrega à thread principal. O único
  travamento previsível do fluxo (carregar a `MatchScene`) acontece depois da
  âncora (research R5).
- **O mapeamento de `reason` e de duração já existe na 002**, privado em
  `MatchHistoryRow` e `SpellEffect`. Ele é extraído para leitores públicos e
  reaproveitado, sem cópia (research R2).
- **"Tempo acabando" sai também pelo `warning` do frame**, porque o
  `turn_warning` só vai ao dono da vez (research R6).
- **Pendente e "último enviado" são dois campos**: a recusa que chega depois de
  uma reconexão ainda aponta o comando certo (research R7).
- **Gravação de frames reais** por um decorador de fábrica de socket, só de
  teste: nada de ponte de texto cru na produção (research R14).

## Technical Context

**Language/Version**: C# 9 (Unity 6000.2.8f1), `#nullable enable` em todo arquivo novo; .NET Standard 2.1

**Primary Dependencies**:
- da 001: `IProtocolCodec`, `DiscriminatedUnion`, `IOutgoingMessage`, `IPayloadReader`/`IPayloadWriter`, `PayloadIdentityReading`/`Writing`, `MessageRefusedFrame`, `IMonotonicClock`, `IClientLog`, `MainThreadQueue` e os fakes;
- da 002: `UserId`, `CardId`, `LoadedCatalog`/`CardLookup`/`UnitCard`/`SpellCard`/`SpellEffect`/`SpellTargetKind`/`SpellDuration`, `MatchEndReason`, `CardCatalog`, `PlayerDecks`;
- da 003: `AuthenticatedConnection` (`FrameReceived`, `SendAsync`, `StatusChanged`, `Recovered`, `Leave`), `ConnectionTarget.Match`, `ConnectionFrames`, `GiveUpReason`, `MatchId`, `CardInstanceId`, `MatchQueue`, `LiveConnectionServices`, `LivePlayer`;
- Unity Test Framework 1.6.0.

Nenhum pacote novo.

**Storage**: N/A (estado da partida só em memória; fixtures de teste em arquivos `.json`)

**Testing**: EditMode pelo comando único da constituição, com `FakeWebSocketFactory`, `FakeAccessTokenSource`, `FakeFrameTicker`, `FakeMonotonicClock`, `FakeAppLifecycle`, `FakeNetworkReachability`, `FakeClientLog` e `MainThreadQueue` drenada pelo teste; fixtures dos contratos e gravadas; `[Explicit]` + `[Category("LiveServer")]` para o marco

**Target Platform**: Windows desktop (player, editor, Multiplayer Play Mode) e Android

**Project Type**: biblioteca interna do cliente Unity (camada de rede), consumida pelo código de cena antigo agora e pela fachada da feature 5

**Performance Goals**:
- testes EditMode da feature < 5 s (SC-011);
- marco LiveServer < 600 s (SC-009);
- aplicar um `match_update` não aloca além do frame decodificado e dos avisos.

**Constraints**:
- `Anathema.Net.Match` sem UnityEngine, Newtonsoft ou `System.Net.WebSockets` (SC-004);
- nenhuma regra de jogo (FR-047, SC-008);
- hora do sistema nunca lida;
- nenhuma espera real nos testes;
- nenhum aviso de compilação novo;
- sem contorno para limitação do backend.

**Scale/Scope**:
- assemblies: 1 nova de produção (`Anathema.Net.Match`) e 1 nova de teste
  (`Anathema.Net.Match.Tests`);
- 5 existentes evoluem: `Core`, `Account`, `Connection`, `Unity`,
  `Net.Unity.Tests`;
- ~75 tipos pequenos, 19 deles eventos;
- ~8 arquivos antigos evoluem e 1 sai;
- 1 caso LiveServer novo (o marco).

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Princípio / regra | Como o plano cumpre | Estado |
|---|---|---|
| I. Contratos são a fonte | Formas lidas dos contratos 009/010 e de `documents.py`, citados por caminho; estratégia do bot do `smoke_match.py`; `Fluxo de Partida.md` lido para o que cada fase espera. Nenhum contorno de backend (R11 da 003 continua) | ✅ |
| II. Servidor é a autoridade | Espelho substitui sem mesclar; versão descarta ≤; relógio por `remaining_ms` + monotônico; zero não faz nada; recusa por `code`; dicas e atributos exibidos documentados como conveniência e fora de `MatchCommands` (`CommandsIgnoreHintsTests`) | ✅ |
| III. Identidade tipada | `CardInstanceId` em todo comando; `CardId` só em `MatchCard.Card` e na busca de catálogo; `UserId`, `MatchId` tipados; nenhum membro `id`; `CommandIdentityTests` | ✅ |
| IV. Tipos explícitos | Eventos e modificadores como hierarquias fechadas com braço desconhecido; fase, motivo e duração com `Unknown` e texto; nada de `JObject` fora do codec; `ClockView.Empty` em vez de nulo solto | ✅ |
| V. Unidades pequenas | Uma responsabilidade por tipo (`VersionGate`, `TurnClock`, `PendingPlay`, `PlayRefusalReader`, `HandCardHints`, `MatchNarrator`, `LiveMatch` só orquestra); narrador sem `switch` de 19 braços (R10); leitores de `reason`/duração extraídos em vez de copiados; nomes novos com 0 ocorrências (R11) | ✅ |
| VI. Núcleo independente | `Anathema.Net.Match` com `noEngineReferences`; relógio, catálogo e conexão pelo construtor; eventos na thread principal porque a conexão já entrega nela; nenhum singleton no núcleo | ✅ (ver Complexity Tracking: `MatchSession.Instance`) |
| VII. Fakes nomeados | Só os fakes existentes; tempo pelo `FakeMonotonicClock`; LiveServer marcado; um arquivo de teste por responsabilidade, uma asmdef de teste para a assembly nova | ✅ (ver Complexity Tracking: `InternalsVisibleTo`) |
| Plataformas | Nada específico de plataforma; nenhum `#if` novo | ✅ |
| JSON pelo codec | `JsonConvert` sai do `MatchClient`; `match_start` tipado pela união | ✅ |
| Log por interface | Sessão, narrador, comandos e `MatchClient` por `IClientLog`, com campos | ✅ |
| Código anterior evolui no lugar | `MatchClient`, `MatchSession`, `PlayerSession`, `AuthenticatedConnection`, `LiveNetworkAdapters`, `LivePlayer` evoluem; nada paralelo (tabela abaixo) | ✅ |

**Pós-design (Phase 1)**: reavaliado depois de `data-model.md` e `contracts/`.
Nenhuma violação nova além das de Complexity Tracking.

- A troca dos leitores privados de `reason` e duração por leitores públicos na
  conta não muda comportamento da 002. Os testes de `MatchHistoryTests` e
  `CatalogReaderTests` continuam valendo sem alteração.
- `MatchSession` continua em `Assembly-CSharp`, sem teste próprio. Ela só repassa
  eventos da `LiveMatch`, que tem teste.

### Código anterior tocado

| Arquivo | Mudança | Por quê | Fica para depois |
|---|---|---|---|
| `Assets/Scripts/Core/Network/WebSocketClient/MatchClient.cs` | Cria e hospeda a `LiveMatch`; carrega catálogo antes; primeira troca de estado carrega `MatchScene`; saem ponte, `JsonConvert`, `MatchStateDTO` | FR-042, FR-044 | `SceneManager` e `MatchSession.Instance` saem na feature 5 |
| `Assets/Scripts/Core/Session/MatchSession.cs` | `Attach(LiveMatch)`, `State` vira `PlayerView`, evento repassa `ViewReplaced`; sai `ApplyState`; comentários preservados | FR-043 | singleton sai na feature 5 |
| `Assets/Scripts/Core/Session/PlayerSession.cs` | `MatchClient` recebe `Account.Catalog` e o relógio | FR-045 | `Instance` sai na feature 5 |
| `Assets/Scripts/Net/Connection/Connection/AuthenticatedConnection.cs`, `SocketAttempt.cs` | Sai `RawTextReceived`; `FrameArrived` sem texto | FR-042 | — |
| `Assets/Tests/EditMode/Net.Connection/ConnectionFrameDeliveryTests.cs` | Sai o teste do texto cru | FR-042 | — |
| `Assets/Scripts/Net/Unity/LiveNetworkAdapters.cs`, `Anathema.Net.Unity.asmdef` | Codec com `MatchFrames.CreateUnion()`; + `Anathema.Net.Match` | research R3 | — |
| `Assets/Scripts/Net/Core/Protocol/PayloadIdentityReading.cs`, `PayloadIdentityWriting.cs` | + listas de `CardInstanceId` | research R2 | — |
| `Assets/Scripts/Net/Account/History/MatchHistoryRow.cs`, `Catalog/SpellEffect.cs`, `AccountAssemblyInfo.cs` | Leitores de texto extraídos; `InternalsVisibleTo` para os testes da partida | research R2, R13 | — |
| `Assets/Tests/EditMode/Net.Unity/LiveServer/LivePlayer.cs`, `Anathema.Net.Unity.Tests.asmdef` | + deck com feitiços e `LiveMatch`; + `Anathema.Net.Match` | research R14 | — |

Removido: `Assets/Scripts/DTO/Match/MatchStateDTO.cs` e a pasta `DTO/Match`, com
`.meta`. Itens do `Game/TODO.md`:
[contracts/legacy-bridge.md](./contracts/legacy-bridge.md#gametodomd-vault).

`BaseClient`, `MatchmakingClient`, `ReconnectOverlay`, `VersusContext`,
`PlayButton`, `Domain/Match/MatchController.cs` e `PlayerPublicDTO` não mudam.

## Project Structure

### Documentation (this feature)

```text
specs/004-match-session/
├── plan.md              # este arquivo
├── research.md          # Phase 0
├── data-model.md        # Phase 1
├── quickstart.md        # Phase 1
├── contracts/
│   ├── protocol-shapes.md   # leitura dos frames, escrita dos comandos, recusas
│   ├── match-state.md       # espelho, relógio, dicas, atributos exibidos
│   ├── live-match.md        # sessão, comandos, pendente, narrador, log, marco
│   └── legacy-bridge.md     # MatchClient, MatchSession, composição, remoções, TODO
├── checklists/
│   └── requirements.md
└── tasks.md             # Phase 2 (/speckit-tasks)
```

### Source Code (repository root)

```text
Assets/Scripts/Net/
├── Core/Protocol/       PayloadIdentityReading, PayloadIdentityWriting        # evoluem (listas de CardInstanceId)
├── Account/             AccountAssemblyInfo, History/MatchHistoryRow, Catalog/SpellEffect  # evoluem
│                        + History/MatchEndReasonText, Catalog/SpellDurationText
├── Connection/Connection/ AuthenticatedConnection, SocketAttempt              # evoluem (sem texto cru)
├── Match/                                        # Anathema.Net.Match (nova; noEngineReferences; Core, Account, Connection)
│   ├── Anathema.Net.Match.asmdef, MatchAssemblyInfo.cs
│   ├── Protocol/   MatchPhase, MatchPhaseText, MatchProfile, MatchCard, BankUnit, SideView,
│   │               OwnSideView, OpponentSideView, BlockPair, CombatView, MatchOutcome, PlayerView,
│   │               TurnView, ClockView, UnitModifier, AttackModifier, HealthModifier,
│   │               DamageImmunityModifier, UnrecognizedModifier, UnitModifiers,
│   │               EventDetail, MatchEvent, MatchEvents, UnrecognizedMatchEvent,
│   │               Events/ (19 tipos de evento),
│   │               MatchStartFrame, MatchUpdateFrame, TurnWarningFrame, MatchFrames
│   ├── Commands/   PlayCommand, MulliganCommand, PlayUnitCommand, CastSpellCommand, PassCommand,
│   │               DeclareAttackCommand, WithdrawAttackerCommand, ConfirmAttackCommand,
│   │               AssignBlockerCommand, RemoveBlockerCommand, EndDefenseWindowCommand,
│   │               ForfeitCommand, MatchCommands, PendingPlay, PlaySendResult, PlaySendStatus
│   ├── Refusals/   PlayRefusal, PlayRefusalCode, PlayRefusalReader
│   ├── Mirror/     MatchMirror, VersionGate, FrameVerdict, ViewReplaced, PhaseChange,
│   │               PriorityChange, MatchEnding, DisplayedUnitStats
│   ├── Clock/      TurnClock, ClockAnnouncements
│   ├── Hints/      HandCardHints, HandCardHint, HintCardKind
│   └── Session/    LiveMatch, LiveMatchPhase, LiveMatchStatus, MatchNarrator
└── Unity/          LiveNetworkAdapters, Anathema.Net.Unity.asmdef                # evoluem

Assets/Scripts/Core/Network/WebSocketClient/MatchClient.cs                          # evolui
Assets/Scripts/Core/Session/MatchSession.cs, PlayerSession.cs                         # evoluem
Assets/Scripts/DTO/Match/MatchStateDTO.cs                                             # sai

Assets/Tests/EditMode/
├── Net.Match/        Anathema.Net.Match.Tests.asmdef (Core, Json, Account, Connection, Match, Fakes)
│   ├── MatchTestRig, MatchTestCatalog, MatchFixtures
│   ├── Fixtures/     contract-*.json, recorded-*.json
│   ├── Protocol/     MatchFramesTests, PlayerViewReadingTests, MatchPhaseReadingTests,
│   │                 UnitModifierUnionTests, MatchEventUnionTests, ClockViewReadingTests,
│   │                 TurnWarningFrameTests, PlayRefusalReaderTests, PlayCommandEncodingTests,
│   │                 CommandIdentityTests, RecordedFramesTests
│   ├── Mirror/       VersionGateTests, MatchMirrorApplyTests, MatchMirrorAnnouncementTests,
│   │                 MatchMirrorFactsTests, DisplayedUnitStatsTests
│   ├── Clock/        TurnClockTests, TurnWarningTests, MulliganClockTests
│   ├── Hints/        HandCardHintsTests
│   ├── Commands/     MatchCommandsTests, PendingPlayTests
│   ├── Session/      LiveMatchConnectionTests, LiveMatchVersionTests, LiveMatchFinishTests,
│   │                 LiveMatchRefusalTests, MatchNarratorTests
│   └── MatchAssemblyBoundaryTests, MatchCoreIsolationTests, CommandsIgnoreHintsTests
├── Net.Connection/   ConnectionFrameDeliveryTests                                # evolui (sai o texto cru)
├── Net.Json/         PayloadIdentityExtensionsTests                              # evolui (listas de CardInstanceId)
└── Net.Unity/        Anathema.Net.Unity.Tests.asmdef (+ Match);
                      LiveServer/ LiveMatchTests, SmokeBot, SmokeStrategy, RecordingWebSocketFactory;
                      LivePlayer evolui
```

**Structure Decision**: a nova `Anathema.Net.Match` fica ao lado das assemblies
da 001–003 em `Assets/Scripts/Net/`, acima da conexão e abaixo da borda Unity.
Protocolo, estado e sessão da partida ficam numa assembly só, em pastas por
responsabilidade (research R1). O código de cena antigo continua em
`Assembly-CSharp`, nos mesmos arquivos.

Dependências, só no sentido borda → núcleo:

```text
Assembly-CSharp (PlayerSession, MatchClient, MatchSession…) ─► Anathema.Config ─► Anathema.Net.Unity ─► Anathema.Net.Match ─► Anathema.Net.Connection ─► Anathema.Net.Account ─► Anathema.Net.Core
                                                                                  └──► Anathema.Net.Json ─────────────────────────────────────────────────────────────► Anathema.Net.Core
Anathema.Net.Match.Tests ─► Match, Connection, Account, Json, Core, Fakes
Anathema.Net.Unity.Tests ─► Unity, Match, Connection, Account, Json, Core, Fakes
```

## Complexity Tracking

| Violation | Why Needed | Simpler Alternative Rejected Because |
|---|---|---|
| `MatchSession.Instance` e `SceneManager` continuam no `MatchClient` | O fluxo `VersusScene` → `match_start` → `MatchScene` sem recarregar na reconexão precisa continuar (FR-044), e a `MatchScene` só tem o singleton como entrada | Tirar os singletons de cena é a fachada da feature 5. O núcleo (`LiveMatch` e o resto) não lê singleton nenhum; tudo entra pelo construtor. Registrado no `Game/TODO.md` |
| `[assembly: InternalsVisibleTo("Anathema.Net.Match.Tests")]` e `("Anathema.Net.Unity.Tests")` na conta | Os testes da partida e os da estratégia do bot do marco precisam de um `LoadedCatalog`, cujo construtor e leitor são internos | Construtor público deixaria código de produção montar catálogo sem leitor; montar por `CardCatalog` + `FakeHttpTransport` põe sessão de conta e HTTP em todo teste de dica e narrador (research R13) |
| `MatchClient` e `MatchSession` sem teste próprio | `Assembly-CSharp` não tem assembly de teste (mesma situação da 003) | Os dois só criam, ligam e repassam. Espelho, relógio, comandos, dicas e sessão têm teste; o fluxo inteiro é provado pelo quickstart §4 e pelo marco |
| Montagem de conexão sobre fakes repetida em `MatchTestRig` | `ConnectionTestRig` da 003 é interno de `Anathema.Net.Connection.Tests` | Mover o rig para `Anathema.Net.Fakes` faria os fakes dependerem de `Anathema.Net.Connection` e do codec; a repetição é montagem de ~20 linhas, sem lógica |
