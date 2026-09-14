# Implementation Plan: Conta e dados do jogador

**Branch**: nenhum (spec no `main`) | **Date**: 2026-09-13 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/002-player-account/spec.md`

## Summary

Esta feature constrói a conta do jogador sobre as portas da 001:

- **Sessão de conta**: entrar, sair, retomar. Validade do token por `exp - iat`
  contada no relógio monotônico. Renovação por margem, reativa (401) e na volta
  ao primeiro plano, uma por vez.
- **Porta `IAccessTokenSource`**: "me dê um token válido", que a feature 3 usa
  no socket.
- **Cliente HTTP autenticado**, e sobre ele cadastro, perfil, catálogo (com cache
  por sessão), decks (com recusas tipadas) e histórico.
- **Guarda segura do refresh token**: DPAPI por P/Invoke no Windows e Android
  Keystore por um plugin Java pequeno.
- **Código antigo evoluindo no lugar**:
  - `PlayerSession` vira a raiz de composição e a ponte;
  - `TokenRefreshService` e `SelfProfileService` delegam;
  - `LoginController` usa a sessão e retoma sem senha;
  - DTOs de `JsonUtility` saem.

Contratos HTTP em `C:/Users/gabri/Projetos/dev_container/anathema/backend/specs/`
(`011-deck-catalog-api/contracts/http_catalog.md`, `http_decks.md`,
`012-match-result-history/contracts/http_match_history.md`). Não são copiados
aqui.

Três achados do research mudam o que a descrição dizia:

- **`user_id` chega como texto.** O SimpleJWT 5.5.1 grava `str(user_id)` no
  claim. O leitor aceita só texto com inteiro positivo (research R1; spec
  corrigida).
- **`ProtectedData` não existe no perfil do projeto.** Só há fachadas internas da
  instalação do Unity. A DPAPI é chamada por P/Invoke em `crypt32` (research R3).
- **Jogadores virtuais dividem o `persistentDataPath`.** A guarda do editor usa um
  slot derivado do `Application.dataPath` de cada clone (research R3, FR-024).

## Technical Context

**Language/Version**: C# 9 (Unity 6000.2.8f1), `#nullable enable` em todo arquivo novo; .NET Standard 2.1 (`apiCompatibilityLevel: 6`); Java (plugin Android de um arquivo)

**Primary Dependencies**: portas e codec da 001 (`IHttpTransport`, `IMonotonicClock`, `IAppLifecycle`, `IClientLog`, `IProtocolCodec`); `crypt32.dll`/`kernel32.dll` (P/Invoke); `android.security.keystore` (API 23+); Unity Test Framework 1.6.0. Nenhum pacote novo.

**Storage**: refresh token cifrado em arquivo (DPAPI no Windows; AES-GCM com chave no Android Keystore, em `noBackupFilesDir`); tokens de acesso e catálogo só em memória

**Testing**: EditMode pelo comando único da constituição, `[Test] async Task` com fakes que completam na hora; `[Explicit]` + `[Category("LiveServer")]` contra o backend local

**Target Platform**: Windows desktop (player e editor) e Android (IL2CPP, ARM64, `minSdk 23`, GameActivity)

**Project Type**: biblioteca interna do cliente Unity (camada de rede), consumida pela apresentação e pelas features 3 a 5

**Performance Goals**: testes EditMode da feature < 5 s (SC-011); 10 pedidos concorrentes → 1 renovação (SC-004)

**Constraints**:
- núcleo e `Anathema.Net.Account` sem UnityEngine, Newtonsoft ou `UnityWebRequest` (SC-002);
- nenhuma credencial em log ou `PlayerPrefs` (SC-003);
- hora do sistema nunca lida;
- `BaseClient`/`MatchClient`/`MatchmakingClient` sem mudança de comportamento.

**Scale/Scope**:
- assemblies: 1 nova de produção (`Anathema.Net.Account`) e 1 nova de teste; 5 existentes evoluem (`Core`, `Unity`, `Config`, `Fakes`, `Net.Json.Tests`);
- ~60 tipos pequenos;
- 1 arquivo Java;
- 7 casos LiveServer.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Princípio / regra | Como o plano cumpre | Estado |
|---|---|---|
| I. Contratos são a fonte | Serviços mapeiam status e chaves dos contratos 011/012 citados por caminho; rotas de conta verificadas no código do backend e no SimpleJWT 5.5.1 (research R1, R5) | ✅ |
| II. Servidor é a autoridade | Deck não é validado no cliente (FR-036); `requires_target` exposto como veio; JWT lido sem validar assinatura, só para agendar renovação; recusa por chave/status, nunca por texto | ✅ |
| III. Identidade tipada | `DeckId`, `CardId` novos; `UserId` do claim; `MatchId` no histórico; nenhum membro `id` | ✅ |
| IV. Tipos explícitos | `card_type` e `kind` por `DiscriminatedUnion` com braço desconhecido; `target_kind`/`duration`/`end_reason` com `Unknown` + texto; `IPayloadReader` em vez de `JObject` | ✅ |
| V. Unidades pequenas | Serviço por recurso; leitores separados (`AccessTokenReader`, `CatalogReader`, `DeckRefusalReader`); nomes conferidos contra o projeto (sem `Manager`/`Helper`/`Handler`; `PlayerDecks`, `CardCatalog`, `MatchHistory` com < 5 ocorrências) | ✅ |
| VI. Núcleo independente | `Anathema.Net.Account` com `noEngineReferences`, só `Core`; injeção por construtor; guarda por interface; troca de thread continua só na `MainThreadQueue` (research R2) | ✅ (ver Complexity Tracking) |
| VII. Fakes nomeados | `FakeRefreshTokenVault` novo; `FakeHttpTransport.HoldNext` para concorrência; tempo pelo `FakeMonotonicClock`; LiveServer marcado | ✅ |
| Plataformas | DPAPI sob `UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN`; Keystore sob `UNITY_ANDROID && !UNITY_EDITOR`; nada para outros alvos | ✅ |
| Refresh em armazenamento protegido | DPAPI e Android Keystore; `PlayerPrefs` barrado por teste (research R12) | ✅ |
| JSON pelo codec | Corpos por `IProtocolCodec.EncodeObject`/`DecodeObject`; DTOs de `JsonUtility` tocados removidos | ✅ |
| Log por interface | `LoginController` perde `Debug.Log*`; eventos estruturados; tipos de credencial com `ToString` mascarado | ✅ |
| Código anterior evolui no lugar | `PlayerSession`, `TokenRefreshService`, `SelfProfileService`, `LoginController`, `AppConfig` evoluem nos mesmos arquivos (tabela abaixo) | ✅ |

**Pós-design (Phase 1)**: reavaliado depois de `data-model.md` e `contracts/`.
Nenhuma violação nova; as exceções são só as de Complexity Tracking. Os testes de
adaptador DPAPI tocam disco real numa pasta temporária. Não é violação do
princípio VII: quem consome a guarda usa o fake, e o teste do próprio adaptador é
o mesmo caso dos testes de adaptador da 001.

### Código anterior tocado

| Arquivo | Mudança | Por quê | Fica para depois |
|---|---|---|---|
| `Assets/Scripts/Core/Session/PlayerSession.cs` | Vira raiz de composição (`Account`, `NetworkLayerHost`); `Token` lê a sessão; saem `RefreshToken`, `SetTokens` e `SetAccessToken`; `SetProfile(OwnProfile)` | FR-041, FR-043, FR-044 | `Instance` e `Token` síncrono saem quando o `BaseClient` usar `IAccessTokenSource` (feature 3) |
| `Assets/Scripts/Core/Network/TokenRefreshService.cs` | Sai de `MonoBehaviour`; `Refresh` delega a `RenewNowAsync`; sem `UnityWebRequest`/`JsonUtility`/`Debug` | FR-041, FR-043 | classe inteira sai na feature 3 |
| `Assets/Scripts/Core/Network/SelfProfileService.cs` | `LoadProfileAsync()` sobre `OwnProfileQuery` | FR-041 | virar leitura direta da apresentação quando houver tela de perfil |
| `Assets/Scripts/Login/LoginController.cs` | Retomada no `Start`; login pela sessão; sem `UnityEditor`, `UnityWebRequest`, `JsonUtility`, `Debug.Log*` | FR-042 | reagir a `SessionExpired` voltando à `LoginScene` (UI, fora do escopo) |
| `Assets/Scripts/Core/Config/AppConfig.cs` e `Anathema.Config.asmdef` | 4 rotas novas; `BuildAccountRoutes()`; asmdef referencia `Anathema.Net.Account` | FR-045 | — |
| `Assets/Config/AppConfig_Dev.asset`, `AppConfig_Prod.asset` | Chaves novas (dev preenchido, prod vazio como as demais) | FR-045 | preencher prod quando houver servidor |
| `Assets/Scripts/DTO/Network/LoginRequestDTO.cs`, `LoginResponseDTO.cs`, `TokenRefreshDTO.cs`, `Assets/Scripts/DTO/Player/SelfPlayerProfileDTO.cs` | Removidos com os `.meta` | FR-046 | — |
| `Assets/Tests/EditMode/Fakes/FakeHttpTransport.cs` | Acrescenta `HoldNext` | research R2 | — |
| `Assets/Scripts/Net/Core/Protocol/DiscriminatedUnion.cs` | Construtor novo com braço desconhecido que recebe também o objeto (`Func<string, IPayloadReader, TBase>`); o antigo delega a ele | `UnrecognizedDeckProblem` precisa ler `message` (FR-034) | — |
| `Assets/Scripts/Net/Core/Protocol/IPayloadReader.cs`, `Assets/Scripts/Net/Json/JObjectPayloadReader.cs` | Acrescenta `ReadTextList(string field)` | mensagens por campo do cadastro e recusas `name`/`deck_limit` (FR-003, FR-033) | — |
| `Assets/Tests/EditMode/Net.Core/CoreAssemblyBoundaryTests.cs` → `Assets/Tests/EditMode/Fakes/AssemblySignatureScanner.cs` | A varredura de assinaturas sai do teste para um utilitário reutilizável; comentários preservados | a fronteira de `Anathema.Net.Account` usa a mesma varredura, sem duplicar | — |
| `Assets/Tests/EditMode/Net.Core/CoreAssemblyBoundaryTests.cs` | Mesma verificação para `Anathema.Net.Account`, mais `UnityEngine.Networking` e `PlayerPrefs` | SC-002, SC-003 | — |
| `ProjectSettings/ProjectSettings.asset` | `useCustomProguardFile: 0 → 1` | research R4 | — |

Itens novos em `Game/TODO.md` (vault), escritos na implementação:

- ponte `PlayerSession.Token`/`TokenRefreshService` (feature 3);
- `SessionExpired` sem reação visual;
- `PlayerSession.Instance` como singleton de composição.

`BaseClient`, `MatchClient`, `MatchmakingClient`, `NetworkBootstrap`,
`MiniPlayerProfile` e `AppEnvManager` não mudam.

## Project Structure

### Documentation (this feature)

```text
specs/002-player-account/
├── plan.md              # este arquivo
├── research.md          # Phase 0
├── data-model.md        # Phase 1
├── quickstart.md        # Phase 1
├── contracts/
│   ├── account-session.md               # sessão, tokens, renovação, cliente autenticado, cadastro
│   ├── player-data.md                   # perfil, catálogo, decks, DeckProblem, histórico
│   └── secure-storage-and-bridge.md     # guarda, adaptadores, fakes, ponte com o código antigo
├── checklists/
│   └── requirements.md
└── tasks.md             # Phase 2 (/speckit-tasks)
```

### Source Code (repository root)

```text
Assets/Scripts/Net/
├── Core/                                        # Anathema.Net.Core (evolui)
│   ├── Identity/      + DeckId, CardId
│   ├── Protocol/      PayloadIdentityReading/Writing (+ DeckId, CardId, lista de CardId)
│   ├── Storage/       IRefreshTokenVault, RefreshToken, VaultReadOutcome, VaultWriteOutcome
│   └── Decks/         DeckProblem, WrongDeckSize, TooManyCopies, UnknownCard,
│                      UnrecognizedDeckProblem, DeckProblemUnion
├── Account/                                     # Anathema.Net.Account (nova; noEngineReferences; Core)
│   ├── Anathema.Net.Account.asmdef
│   ├── AccountRoutes.cs, AccountTiming.cs
│   ├── Tokens/        AccessToken, AccessTokenReader, Base64UrlText, SessionTokens, Password
│   ├── Session/       AccountSession, AccountSessionState, SignInOutcome, ResumeOutcome,
│   │                  TokenRenewal, RenewalOutcome, RenewalUnavailableReason,
│   │                  IAccessTokenSource, SessionAccessTokens, AccessTokenOutcome,
│   │                  SessionUnavailableKind, ForegroundRenewal, AccountRequestBodies
│   ├── Http/          AuthenticatedHttpClient, AuthenticatedRequest, AuthenticatedCallResult,
│   │                  AccountCallOutcome, AccountCallFailure, UnrecognizedRefusal
│   ├── Registration/  AccountRegistration, RegistrationForm, AccountCreated,
│   │                  RegistrationRefusal, RegistrationFieldError, RegistrationField
│   ├── Profile/       OwnProfileQuery, OwnProfile, ProfileRefusal
│   ├── Catalog/       CardCatalog, CatalogReader, LoadedCatalog, CatalogCard, UnitCard, SpellCard,
│   │                  SpellEffect, SpellTargetKind, SpellDuration, CardLookup, CardLookupKind
│   ├── Decks/         PlayerDecks, PlayerDeck, DeckDraft, DeckChange, DeckDeleted, DeckRefusal,
│   │                  DeckRefusalReason (+ 6 braços), DeckRefusalReader
│   └── History/       MatchHistory, HistoryPageRequest, MatchHistoryPage, MatchHistoryRow,
│                      HistoryOpponent, MatchEndReason, HistoryRefusal
├── Unity/                                       # Anathema.Net.Unity (evolui; + referência a Account)
│   ├── Storage/       DpapiRefreshTokenVault, DpapiNative, AndroidKeystoreRefreshTokenVault,
│   │                  PlatformRefreshTokenVault, RefreshTokenVaultSlot
│   └── LiveAccountServices.cs
Assets/Plugins/Android/
├── RefreshTokenCipher.java                      # com.anathema.net, só Android
└── proguard-user.txt

Assets/Scripts/Core/Config/  AppConfig.cs, Anathema.Config.asmdef       # evoluem
Assets/Scripts/Core/Session/PlayerSession.cs                           # evolui (composição + ponte)
Assets/Scripts/Core/Network/ TokenRefreshService.cs, SelfProfileService.cs  # evoluem
Assets/Scripts/Login/LoginController.cs                                # evolui

Assets/Tests/EditMode/
├── Fakes/        + FakeRefreshTokenVault, HeldHttpResponse; FakeHttpTransport evolui
├── Net.Core/     + DeckIdTests, CardIdTests; IdentityIsolationTests e CoreAssemblyBoundaryTests evoluem
├── Net.Json/     + DeckProblemUnionTests, PayloadIdentityExtensionsTests evolui
├── Net.Account/  Anathema.Net.Account.Tests.asmdef (Core, Json, Account, Fakes)
│                 AccessTokenReaderTests, AccountSessionSignInTests, AccountSessionResumeTests,
│                 TokenRenewalTests, SessionAccessTokensTests, ForegroundRenewalTests,
│                 AuthenticatedHttpClientTests, AccountRegistrationTests, OwnProfileQueryTests,
│                 CardCatalogTests, CatalogReaderTests, PlayerDecksTests, DeckRefusalReaderTests,
│                 MatchHistoryTests, AccountCredentialLeakTests, JwtTestTokens (construtor de JWT de teste)
├── Net.Unity/    + DpapiRefreshTokenVaultTests, RefreshTokenVaultSlotTests, LiveAccountServicesTests,
│                 LiveServer/LiveAccountTests
└── Config/       AppConfigTests evolui (rotas novas, BuildAccountRoutes)
```

**Structure Decision**: a nova `Anathema.Net.Account` fica ao lado das
assemblies da 001 em `Assets/Scripts/Net/`. `DeckProblem` e a porta da guarda
ficam em `Anathema.Net.Core`, para a feature 3 usá-los sem depender da conta. Os
adaptadores ficam em `Anathema.Net.Unity`, e o plugin Java no lugar padrão do
Unity para Android. O código antigo continua em `Assembly-CSharp`, nos mesmos
arquivos.

Dependências, só no sentido borda → núcleo:

```text
Assembly-CSharp (PlayerSession, LoginController…) ─► Anathema.Config ─► Anathema.Net.Unity ─► Anathema.Net.Account ─► Anathema.Net.Core
                                                                          └──────────────► Anathema.Net.Json ────► Anathema.Net.Core
Anathema.Config ─► Anathema.Net.Account
Anathema.Net.Fakes ─► Anathema.Net.Core
Anathema.Net.Account.Tests ─► Account, Json, Core, Fakes
```

## Complexity Tracking

| Violation | Why Needed | Simpler Alternative Rejected Because |
|---|---|---|
| `PlayerSession.Instance` e `TokenRefreshService.Instance` continuam como singletons estáticos, e `PlayerSession` compõe a camada | `BaseClient` (fora do escopo até a feature 3) lê `PlayerSession.Instance.Token` e chama `TokenRefreshService.Instance.Refresh`; a spec exige que continue funcionando (FR-043) | Religar `BaseClient` agora é escopo da feature 3. Um segundo objeto de composição criaria duas donas da sessão. Os singletons ficam em `Assembly-CSharp`, fora do núcleo, e estão registrados como dívida |
| Leitura do JWT sem validar assinatura | O cliente precisa de `exp`, `iat` e `user_id` para agendar a renovação e saber quem é | Validar exige o segredo do servidor no cliente. Confiar no conteúdo não dá poder: toda decisão continua no servidor, que recusa token inválido |
| Plugin Java (`RefreshTokenCipher.java`) fora do C# | Keystore só é acessível pela API Java; em C# seriam cerca de quinze travessias JNI por operação | `EncryptedSharedPreferences` traz dependência Gradle descontinuada; JNI puro em C# espalha tipos Java soltos e é mais difícil de ler e de manter |
