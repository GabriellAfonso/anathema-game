---

description: "Task list for 002-player-account"
---

# Tasks: Conta e dados do jogador

**Input**: Design documents from `specs/002-player-account/`

**Prerequisites**: [plan.md](./plan.md), [spec.md](./spec.md), [research.md](./research.md), [data-model.md](./data-model.md), [contracts/](./contracts/), [quickstart.md](./quickstart.md)

**Tests**: obrigatórios (constituição, princípio VII). Todo método novo ganha
teste EditMode. Escreva o teste antes da implementação: em Unity, "falhar" pode
significar não compilar porque o tipo ainda não existe.

**Organization**: tarefas agrupadas por história da spec (US1 a US6).

## Format: `[ID] [P?] [Story] Description`

- **[P]**: pode rodar em paralelo com as outras [P] do mesmo bloco (arquivos
  diferentes, sem depender de tarefa incompleta do bloco)
- **[Story]**: US1 a US6 (histórias da spec)

## Convenções para todas as tarefas

Leia antes de executar qualquer tarefa:

- **Antes de escrever código**: leia `CLAUDE.md`, `.specify/memory/constitution.md` e o contrato
  citado na tarefa (`specs/002-player-account/contracts/`). Formas HTTP de
  catálogo, decks e histórico: nunca copie para código nem comentário; cite o
  caminho em `C:/Users/gabri/Projetos/dev_container/anathema/backend/specs/`
  (`011-deck-catalog-api/contracts/http_catalog.md`, `http_decks.md`,
  `012-match-result-history/contracts/http_match_history.md`).
- **Namespaces**: um por assembly, igual ao nome da asmdef
  (`Anathema.Net.Core`, `Anathema.Net.Account`, `Anathema.Net.Unity`,
  `Anathema.Config`, `Anathema.Net.Fakes`). Testes: namespace testado +
  `.Tests`. Subpastas **não** criam sub-namespace. `AppConfig` continua no
  namespace global; o código antigo (`PlayerSession`, `LoginController`…)
  também.
- **Todo arquivo novo**:
  - começa com `#nullable enable`;
  - tem um tipo público por arquivo;
  - métodos de 4 a 20 linhas, com no máximo 2 níveis de indentação;
  - `var` só quando o tipo aparece do lado direito.
- **Documentação e erros**:
  - membro público leva `/// <summary>` com a intenção e um `<example>`;
  - mensagem de exceção inclui o valor recebido e a forma esperada.
- **Assincronia** (research R2):
  - todo `await` em `Anathema.Net.Account` usa `ConfigureAwait(false)`;
  - não há lock nem `Task.Run`;
  - a sessão só é tocada na thread principal.
- **Credenciais** (research R12):
  - senha, acesso e refresh só saem por `RevealForRequest()`, e só para montar
    cabeçalho ou corpo;
  - nunca vão para `LogField`;
  - corpo de login e de renovação nunca é registrado.
- **Eventos de log** (snake_case, estáveis): `account_signed_in`,
  `account_sign_in_refused`, `account_signed_out`, `account_resumed`,
  `account_resume_refused`, `access_token_renewed`,
  `access_token_renewal_unavailable`, `access_token_renewal_on_foreground`,
  `session_expired`, `vault_save_failed`, `vault_unreadable`,
  `account_response_out_of_contract`, `catalog_card_skipped`,
  `catalog_card_duplicated`, `catalog_effect_value_unknown`,
  `history_end_reason_unknown`.
- **Testes**:
  - NUnit, em `Assets/Tests/EditMode/<pasta da asmdef>/`;
  - nomes de método em português, no estilo de `Assets/Tests/EditMode/HeartbeatTests.cs`;
  - assíncronos como `[Test] public async Task`;
  - fakes de `Anathema.Net.Fakes` e codec real
    (`NewtonsoftProtocolCodec(GenericServerFrames.CreateUnion(), log)`);
  - tempo só pelo `FakeMonotonicClock`, nunca `Thread.Sleep` (exceto LiveServer).
- **JWT de teste**: sempre por `JwtTestTokens` (T020), nunca texto montado à mão
  no teste.
- **`.meta`**: o Unity gera ao abrir o projeto; todo arquivo novo em `Assets/` vai
  para o commit com o seu `.meta`. Arquivos removidos saem com o `.meta`.
- **Comando da suíte** (editor fechado), chamado abaixo de "rodar a suíte":
  `"C:/Program Files/Unity/Hub/Editor/6000.2.8f1/Editor/Unity.exe" -batchmode -projectPath . -runTests -testPlatform EditMode -testResults Logs/editmode-results.xml`
  Se o `Unity.exe` não abrir a partir da sessão, compile com o Roslyn do Unity e
  rode NUnit no Mono como na 001 (registro da execução em
  `specs/001-server-connection/tasks.md`); o mantenedor roda a suíte real.

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: assembly nova e referências, conforme "Project Structure" do plan.md.

- [X] T001 [P] Create `Assets/Scripts/Net/Account/Anathema.Net.Account.asmdef`: `name` = `Anathema.Net.Account`, `rootNamespace` = `Anathema.Net.Account`, `references` = `["Anathema.Net.Core"]`, `autoReferenced` = true, `noEngineReferences` = true, `overrideReferences` = false.
- [X] T002 [P] Create `Assets/Scripts/Net/Account/AccountAssemblyInfo.cs` with `[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Anathema.Net.Account.Tests")]` and a comment saying why (the tests reach `TokenRenewal`, `UnrecognizedCatalogCard` and the readers).
- [X] T003 [P] Create `Assets/Tests/EditMode/Net.Account/Anathema.Net.Account.Tests.asmdef` with the same shape as `Assets/Tests/EditMode/Net.Json/Anathema.Net.Json.Tests.asmdef`:
  - `references` = `["UnityEngine.TestRunner", "UnityEditor.TestRunner", "Anathema.Net.Core", "Anathema.Net.Json", "Anathema.Net.Account", "Anathema.Net.Fakes"]`;
  - `includePlatforms` = `["Editor"]`;
  - `overrideReferences` = true, with `precompiledReferences` = `["nunit.framework.dll"]`;
  - `defineConstraints` = `["UNITY_INCLUDE_TESTS"]`;
  - `autoReferenced` = false.
- [X] T004 [P] Add `"Anathema.Net.Account"` to `references` in `Assets/Scripts/Net/Unity/Anathema.Net.Unity.asmdef`.
- [X] T005 [P] Add `"Anathema.Net.Account"` to `references` in `Assets/Scripts/Core/Config/Anathema.Config.asmdef`.
- [X] T006 [P] Add `"Anathema.Net.Account"` to `references` in `Assets/Tests/EditMode/Net.Unity/Anathema.Net.Unity.Tests.asmdef` and in `Assets/Tests/EditMode/Config/Anathema.Config.Tests.asmdef`.
- [X] T007 [P] Add `<assembly fullname="Anathema.Net.Account" preserve="all"/>` to `Assets/Scripts/Net/link.xml`, next to the Core and Json entries, keeping their comment.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: identidades, porta da guarda, fakes, leitura de token, rotas e desfechos comuns. Tudo que as seis histórias usam.

**⚠️ CRITICAL**: nenhuma história começa antes desta fase fechar.

### Tests

- [X] T008 [P] Create `Assets/Tests/EditMode/Net.Core/Identity/DeckIdTests.cs`, mirroring `UserIdTests.cs`: value 0 and -1 throw with the value in the message; equality by value; `ToString()` = `deck_id=4`.
- [X] T009 [P] Create `Assets/Tests/EditMode/Net.Core/Identity/CardIdTests.cs`, mirroring `UserIdTests.cs`: `ToString()` = `card_id=1004`.
- [X] T010 [P] Extend `Assets/Tests/EditMode/Net.Core/Identity/IdentityIsolationTests.cs` so both existing tests also cover `DeckId` and `CardId`: no conversion operator, no constructor taking another identifier.
- [X] T011 [P] Extend `Assets/Tests/EditMode/Net.Json/PayloadIdentityExtensionsTests.cs` with:
  - `ReadDeckId`/`ReadCardId` from an integer;
  - `"4"` → `PayloadShapeException` with `WrongFieldType`;
  - `0` → `InvalidValue`;
  - `ReadCardIdList` over `[1, 1, 1004]`;
  - `WriteDeckId` and `WriteCardIdList` round trip through `EncodeObject`/`DecodeObject`.

  Also extend `Assets/Tests/EditMode/Net.Json/JObjectPayloadReaderTests.cs` with `ReadTextList`:
  - `["a", "b"]` → both, in order;
  - `[]` → empty;
  - missing → `MissingField`;
  - `"a"` (not a list) and `["a", 1]` → `WrongFieldType`, with path `field[1]` for the item.
- [X] T012 [P] Extend `Assets/Tests/EditMode/Net.Json/DiscriminatedUnionTests.cs` for the new constructor (unknown arm receives kind text and object):
  - unknown `kind` with `message` → arm sees both;
  - unknown `kind` without `message` → `DecodeObject` fails with `MissingField` and path ending in `.message`;
  - old constructor still returns unknown arm with text only.
- [X] T013 [P] Create `Assets/Tests/EditMode/Net.Core/Storage/RefreshTokenTests.cs`:
  - empty or whitespace text throws with the expected-shape message, without echoing the text;
  - `ToString()` = `refresh_token=<redacted>`;
  - `RevealForRequest()` returns the text;
  - equality by value.
- [X] T014 [P] Create `Assets/Tests/EditMode/Net.Core/Fakes/FakeRefreshTokenVaultTests.cs`:
  - `Save` then `Read` → `Found`;
  - `Delete` twice is fine, then `Read` → `Empty`;
  - `FailNextSave` → `Failed` once, then `Saved`;
  - `MakeUnreadable` → `Unreadable`;
  - `Preload` → `Found`;
  - `SaveCount`/`DeleteCount` counted.
- [X] T015 [P] Extend `Assets/Tests/EditMode/Net.Core/Fakes/FakeHttpTransportTests.cs`:
  - `HoldNext` → request recorded at once, task not completed;
  - `Release(200, body)` completes with that response;
  - `ReleaseFailure` completes with `TransportFailure`;
  - releasing twice throws;
  - a held entry and a `RespondNext` entry are consumed in scripting order.
- [X] T016 [P] Create `Assets/Tests/EditMode/Net.Account/AccountAssemblyBoundaryTests.cs`:
  - `Anathema.Net.Account` references no assembly starting with `UnityEngine`, `UnityEditor` or `Newtonsoft`;
  - no signature type is in `System.Net.WebSockets` or `UnityEngine.Networking` (use `AssemblySignatureScanner` from T034);
  - the assembly does not reference `Anathema.Net.Json` or `Anathema.Net.Unity`.
- [X] T017 [P] Create `Assets/Tests/EditMode/Net.Account/Tokens/PasswordTests.cs`: empty password throws; `ToString()` = `password=<redacted>`; `RevealForRequest()` returns the text.
- [X] T018 [P] Create `Assets/Tests/EditMode/Net.Account/Tokens/Base64UrlTextTests.cs`:
  - decodes text with `-`, `_` and no padding (lengths %4 = 0, 2, 3);
  - length %4 = 1 → failure;
  - invalid character → failure;
  - output is UTF-8 text.
- [X] T019 [P] Create `Assets/Tests/EditMode/Net.Account/Tokens/AccessTokenReaderTests.cs`:
  - valid token with `user_id: "7"`, `iat` 1000, `exp` 1300 → `Owner` 7, `Lifetime` 300 s, `ArrivedAt` as given;
  - `user_id` as JSON integer 7 → `WrongFieldType`;
  - `"0"` and `"abc"` → `InvalidValue`;
  - `exp` missing → `MissingField`;
  - `exp == iat` → `InvalidValue`;
  - 2 parts and 4 parts → `NotJson`;
  - invalid base64 → `NotJson`;
  - `iat` in the year 2000 and in the year 2090 with the same difference → same `Lifetime`.
- [X] T020 [P] Create `Assets/Tests/EditMode/Net.Account/JwtTestTokens.cs` (test support, used by every token test):
  - `static string Create(long issuedAt, long expiresAt, string userIdClaim = "7")` builds header `{"alg":"HS256","typ":"JWT"}` + payload + signature `"test-signature"`, all base64url without padding;
  - `static string CreateRaw(string payloadJson)` builds a token with an arbitrary payload.
- [X] T021 [P] Create `Assets/Tests/EditMode/Net.Account/Tokens/AccessTokenTests.cs`:
  - `NeedsRenewal` with lifetime 300 s and margin 30 s: false at +269 s, true at +270 s and +271 s;
  - margin 0: true exactly at +300 s;
  - `ExpiresAt` = `ArrivedAt + Lifetime`;
  - `ToString()` contains `<redacted>` and `user_id=7`, and never the token text.
- [X] T022 [P] Create `Assets/Tests/EditMode/Net.Account/AccountTimingTests.cs`: default margin 30 s; -1 s and 121 s throw with the value; 0 and 120 accepted.
- [X] T023 [P] Create `Assets/Tests/EditMode/Net.Account/AccountRoutesTests.cs`:
  - relative URI throws naming the route;
  - `Deck(new DeckId(4))` = decks URL + `4/`;
  - `MatchesPage(new HistoryPageRequest(2, 50))` ends with `?page=2&page_size=50`.
- [X] T024 [P] Create `Assets/Tests/EditMode/Net.Account/History/HistoryPageRequestTests.cs`: page 0 and page size 0 throw with value and expected shape; default is page 1, size 20.
- [X] T025 [P] Create `Assets/Tests/EditMode/Net.Account/Http/AccountCallOutcomeTests.cs`:
  - `Success`, `Refused` and `Failed` factories set exactly one of `Value`/`Refusal`/`Failure`;
  - reading `Value` on a refusal throws, naming the actual state;
  - `UnrecognizedRefusal` truncates body to 500 characters.
- [X] T026 [P] Extend `Assets/Tests/EditMode/Config/AppConfigTests.cs`:
  - with all routes filled, `BuildAccountRoutes()` returns absolute URLs on `EffectiveHost` with the scheme from `useTls`;
  - each of `registerEndpoint`, `cardsEndpoint`, `decksEndpoint`, `matchesEndpoint`, `loginEndpoint`, `playerMe`, `tokenRefreshEndpoint` empty → throws naming the field and the asset name (use `[TestCase]` per field).

### Implementation

- [X] T027 [P] Create `Assets/Scripts/Net/Core/Identity/DeckId.cs` (`readonly struct`, `long Value` > 0, same shape and comments as `UserId.cs`).
- [X] T028 [P] Create `Assets/Scripts/Net/Core/Identity/CardId.cs`, same shape. Summary says `CardId` only looks up the catalog; plays cite `CardInstanceId` (constitution, principle III).
- [X] T029 Add to `Assets/Scripts/Net/Core/Protocol/PayloadIdentityReading.cs`: `ReadDeckId`, `ReadCardId`, `ReadCardIdList` (over `ReadIntegerList`; invalid value → `PayloadShapeException(InvalidValue)` with path `field[index]`). Add `WriteDeckId` and `WriteCardIdList` to `PayloadIdentityWriting.cs`. Follow the existing methods. Also add `IReadOnlyList<string> ReadTextList(string field)` to `Assets/Scripts/Net/Core/Protocol/IPayloadReader.cs` (summary + example) and implement it in `Assets/Scripts/Net/Json/JObjectPayloadReader.cs` following `ReadIntegerList`. Depends on T027, T028.
- [X] T030 [P] Add to `Assets/Scripts/Net/Core/Protocol/DiscriminatedUnion.cs` a constructor `(string discriminatorField, Func<string, IPayloadReader, TBase> unknownArm)`:
  - store that delegate;
  - the old constructor delegates with `(value, _) => unknownArm(value)`;
  - `Dispatch` passes the body;
  - keep all existing comments.
- [X] T031 [P] Create `Assets/Scripts/Net/Core/Storage/RefreshToken.cs` (sealed class; rules in data-model).
- [X] T032 [P] Create `Assets/Scripts/Net/Core/Storage/VaultReadOutcome.cs` and `Assets/Scripts/Net/Core/Storage/VaultWriteOutcome.cs` (sealed classes with static factories `Found`/`Empty`/`Unreadable` and `Saved`/`Failed`; `Kind` enum inside each file is not allowed; create `VaultReadKind.cs` and `VaultWriteKind.cs`).
- [X] T033 Create `Assets/Scripts/Net/Core/Storage/IRefreshTokenVault.cs` per `contracts/secure-storage-and-bridge.md`. Depends on T031, T032.
- [X] T034 [P] Extract the signature walk (`SignatureTypes`, `MethodTypes`, `Expand` and the `BindingFlags`) from `Assets/Tests/EditMode/Net.Core/CoreAssemblyBoundaryTests.cs` into public static `Assets/Tests/EditMode/Fakes/AssemblySignatureScanner.cs` (`IEnumerable<string> FindUsages(Assembly assembly, string namespaceFragment)`). `CoreAssemblyBoundaryTests` then calls it. Keep the existing summary and comments.
- [X] T035 Create `Assets/Tests/EditMode/Fakes/FakeRefreshTokenVault.cs` per contract. Depends on T033.
- [X] T036 [P] Create `Assets/Tests/EditMode/Fakes/HeldHttpResponse.cs` and add `HoldNext()` to `Assets/Tests/EditMode/Fakes/FakeHttpTransport.cs`:
  - the scripted queue holds either a ready outcome or a held entry backed by a `TaskCompletionSource<HttpOutcome>` without `RunContinuationsAsynchronously`;
  - update the class summary.
- [X] T037 [P] Create `Assets/Scripts/Net/Account/Tokens/Password.cs` (sealed class; `ToString` redacted).
- [X] T038 [P] Create `Assets/Scripts/Net/Account/Tokens/Base64UrlText.cs` (`static bool TryDecode(string text, out string decoded, out string problem)`, via `Convert.FromBase64String` after replacing and padding).
- [X] T039 [P] Create `Assets/Scripts/Net/Account/Tokens/AccessToken.cs` per data-model. The constructor is `internal`; only `AccessTokenReader` builds it.
- [X] T040 Create `Assets/Scripts/Net/Account/Tokens/AccessTokenReader.cs`:
  - constructor takes `IProtocolCodec`;
  - `DecodeOutcome<AccessToken> Read(string jwt, MonotonicInstant arrivedAt)`;
  - split, then `Base64UrlText`, then `codec.DecodeObject`, then read `exp` and `iat` with `ReadInteger` and `user_id` with `ReadText` + `long.TryParse` (invariant, positive);
  - every failure path uses `DecodeFailure` without the token text;
  - summary cites research R1 (`str(user_id)` in SimpleJWT 5.5.1).
  Depends on T038, T039.
- [X] T041 [P] Create `Assets/Scripts/Net/Account/AccountTiming.cs`.
- [X] T042 [P] Create `Assets/Scripts/Net/Account/History/HistoryPageRequest.cs`.
- [X] T043 Create `Assets/Scripts/Net/Account/AccountRoutes.cs` per data-model. Depends on T027, T042.
- [X] T044 [P] Create in `Assets/Scripts/Net/Account/Http/`: `SessionUnavailableKind.cs`, `AccountCallFailure.cs` (sealed class with factories `SessionUnavailable`, `TransportFailed`, `OutOfContract` and a `AccountCallFailureKind` in its own file `AccountCallFailureKind.cs`), `UnrecognizedRefusal.cs`, `AccountCallOutcome.cs`.
- [X] T045 Add to `Assets/Scripts/Core/Config/AppConfig.cs`:
  - fields `registerEndpoint`, `cardsEndpoint`, `decksEndpoint`, `matchesEndpoint` under the `[Header("Endpoints")]` block, in the same style;
  - `public AccountRoutes BuildAccountRoutes()`, using `HttpUrl` per field and throwing with field and `name` when a route is empty.
  Depends on T043.
- [X] T046 Add the four keys to `Assets/Config/AppConfig_Dev.asset` (`/accounts/register/`, `/game/cards/`, `/players/decks/`, `/game/matches/`) and to `Assets/Config/AppConfig_Prod.asset` (empty, like its other routes). Depends on T045.
- [X] T047 Run the suite. Phase 2 tests pass; nothing else regresses.

**Checkpoint**: fundação pronta; histórias podem começar.

---

## Phase 3: User Story 1 - Entrar e continuar autenticado sem perceber o token (Priority: P1) 🎯 MVP

**Goal**: login, perfil, porta de token válido com renovação por margem, reativa, única e na volta ao primeiro plano; expiração só por refresh 401; guarda real no Windows e no Android; `LoginScene` → Home pela sessão nova.

**Independent Test**: `Anathema.Net.Account.Tests` com fakes cobre os cenários 1 a 11 da história; o cenário 12 é a seção 3 (passos 1–2) do quickstart.

### Tests for User Story 1 (MANDATORY) ⚠️

- [X] T048 [P] [US1] Create `Assets/Tests/EditMode/Net.Account/Session/AccountRequestBodiesTests.cs`:
  - login body has exactly `username` and `password`;
  - refresh body has exactly `refresh`;
  - login response → access text + refresh text; `token` or `refresh` missing → failure with field;
  - refresh response → `access`; missing → failure.
- [X] T049 [P] [US1] Create `Assets/Tests/EditMode/Net.Account/Session/AccountSessionSignInTests.cs` (contracts/account-session.md, tests 1, 2 and 8):
  - 200 → `SignedIn(7)`, `State` `SignedIn`, `Self` 7, vault stored refresh, `Generation` +1;
  - 401 → `CredentialsRefused`; 500 → `ServerRefused(500)`; transport → `TransportFailed`; token `"x.y"` → `OutOfContract`; in all four the vault is empty and the state `SignedOut`;
  - second `SignInAsync` while the first is held → `AlreadyInProgress` with one request;
  - vault `FailNextSave` → still `SignedIn`, `vault_save_failed` logged once;
  - `SignOut` → `SignedOut`, vault deleted, no `SessionExpired`, zero new requests.
- [X] T050 [P] [US1] Create `Assets/Tests/EditMode/Net.Account/Session/TokenRenewalTests.cs`, one test per row of the `TokenRenewal` table in contracts/account-session.md:
  - 200 → `Renewed` with new access, refresh unchanged;
  - 401 → `SessionExpired`, state `Expired`, vault empty, `SessionExpired` event count 1;
  - 400 and 503 → `Unavailable(ServerStatus)`, state `SignedIn`;
  - transport → `Unavailable(Transport)`;
  - 200 without `access` → `Unavailable(OutOfContract)`;
  - held renewal + `SignOut` + `Release(200)` → caller gets `Renewed` but session stays `SignedOut` with no tokens;
  - two calls while held → one request, same outcome object.
- [X] T051 [P] [US1] Create `Assets/Tests/EditMode/Net.Account/Session/SessionAccessTokensTests.cs`:
  - `SignedOut` → `SessionUnavailable(NoSession)`, zero requests; `Expired` → `SessionUnavailable(Expired)`;
  - lifetime 300 s: at +60 s `Valid` with zero requests; at +271 s one renewal then `Valid(new)`;
  - `RenewNowAsync` at +10 s → one renewal;
  - ten `GetValidAsync` at +280 s with `HoldNext` → one request, and after `Release` all ten `Valid` with the same token;
  - renewal `Unavailable` → `AccessTokenOutcome.Unavailable`.
- [X] T052 [P] [US1] Create `Assets/Tests/EditMode/Net.Account/Session/ForegroundRenewalTests.cs` (`FakeAppLifecycle` + `FakeMonotonicClock`):
  - signed in, background, advance 7 min, foreground → one renewal request before any `GetValidAsync`, `access_token_renewal_on_foreground` logged;
  - advance 1 min → zero requests;
  - signed out → zero requests;
  - after `Dispose`, foreground → zero requests.
- [X] T053 [P] [US1] Create `Assets/Tests/EditMode/Net.Account/Http/AuthenticatedHttpClientTests.cs`:
  - GET → headers `Authorization: Bearer <access>` and `Accept: application/json`, no `Content-Type`; with body, `Content-Type: application/json`;
  - 401 then 200 → 3 requests (call, refresh, retry), the retry carries the new token, result 200;
  - 401, refresh 200, retry 401 → result response 401 with exactly 3 requests;
  - two held calls both 401 with the same old token → one refresh, two retries;
  - call sent with token A, meanwhile the session renewed to B, then 401 → retry with B and no refresh;
  - 401 + refresh 401 → `SessionUnavailable(Expired)`;
  - transport failure → `TransportFailed` without refresh;
  - not signed in → `SessionUnavailable(NoSession)` with zero requests;
  - no log field contains the access token.
- [X] T054 [P] [US1] Create `Assets/Tests/EditMode/Net.Account/Profile/OwnProfileQueryTests.cs`:
  - 200 → all six fields;
  - 404 → `ProfileRefusal.Kind == ProfileMissing`;
  - 500 → `Unrecognized` with status;
  - 200 with `level` as text → `OutOfContract` with path `level`.
- [X] T055 [P] [US1] Create `Assets/Tests/EditMode/Net.Unity/Storage/RefreshTokenVaultSlotTests.cs`:
  - `ForPlayer()` = `player`;
  - `ForEditorProject` → same `dataPath` gives the same slot, two paths give different slots, format `editor-` + 12 lowercase hex;
  - `Named("")` throws.
- [X] T056 [P] [US1] Create `Assets/Tests/EditMode/Net.Unity/Storage/DpapiRefreshTokenVaultTests.cs` (whole file under `#if UNITY_EDITOR_WIN`; temp directory created in `SetUp`, deleted in `TearDown`):
  - save + read round trip;
  - a second instance on the same slot reads it;
  - a different slot → `Empty`;
  - overwritten file bytes → `Unreadable`, no exception;
  - file bytes do not contain the UTF-8 or UTF-16 token text;
  - `Delete` twice is fine.
- [X] T057 [P] [US1] Create `Assets/Tests/EditMode/Net.Unity/LiveAccountServicesTests.cs`, composing with fakes (`FakeHttpTransport`, `FakeMonotonicClock`, `FakeAppLifecycle`, `FakeRefreshTokenVault`, `FakeClientLog`, real codec):
  - `Session`, `Tokens` and `Profile` are not null and share one session (sign in through `Session`, `Tokens.GetValidAsync` returns `Valid`);
  - foreground after 7 min triggers one renewal request.

### Implementation for User Story 1

- [X] T058 [P] [US1] Create `Assets/Scripts/Net/Account/Session/AccountSessionState.cs` (`SignedOut`, `SigningIn`, `SignedIn`, `Expired`), `SignInOutcomeKind.cs` and `SignInOutcome.cs` (factories per data-model).
- [X] T059 [P] [US1] Create `Assets/Scripts/Net/Account/Session/AccountRequestBodies.cs` (internal static):
  - `WriteLogin(IPayloadWriter, string username, Password)`;
  - `WriteRefresh(IPayloadWriter, RefreshToken)`;
  - `ReadLoginTokens(IPayloadReader)` → `(string access, string refresh)`;
  - `ReadRefreshedAccess(IPayloadReader)`.
  Summary cites `backend/server/apps/accounts/views.py` (`token`, not `access`) and SimpleJWT `TokenRefreshView` (`access`).
- [X] T060 [P] [US1] Create `Assets/Scripts/Net/Account/Session/SessionTokens.cs` (internal; `AccessToken Access`, `RefreshToken Refresh`, `WithAccess(AccessToken)`).
- [X] T061 [P] [US1] Create `Assets/Scripts/Net/Account/Session/RenewalUnavailableReason.cs`, `RenewalOutcomeKind.cs`, `RenewalOutcome.cs`.
- [X] T062 [US1] Create `Assets/Scripts/Net/Account/Session/AccountSession.cs` per contracts/account-session.md:
  - constructor stores dependencies and builds `internal TokenRenewal Renewal`;
  - `SignInAsync` guards `SigningIn` → POST login → map outcome → `ApplySignIn` (tokens, `Self`, vault save, `Generation++`, log);
  - `SignOut`;
  - internal `CurrentTokens`;
  - internal `ApplyRenewedAccess(int generation, AccessToken)` returns whether it applied;
  - internal `Expire(int generation)` (vault delete, `Generation++`, `State = Expired`, raise `SessionExpired` once, log `session_expired`);
  - split into private methods of at most 20 lines.
  Depends on T040, T043, T044, T058, T059, T060, T061.
- [X] T063 [US1] Create `Assets/Scripts/Net/Account/Session/TokenRenewal.cs` (internal):
  - `Task<RenewalOutcome> RenewAsync()` returns the in-flight task when present, otherwise starts one;
  - the start captures `Generation` and refresh → POST refresh → map per research R5 → apply only if the generation still matches;
  - clears the in-flight reference before returning;
  - logs `access_token_renewed` / `access_token_renewal_unavailable`.
  Depends on T062.
- [X] T064 [P] [US1] Create `Assets/Scripts/Net/Account/Session/AccessTokenOutcomeKind.cs`, `AccessTokenOutcome.cs` and `IAccessTokenSource.cs` (summary says feature 3 uses it for `?token=`).
- [X] T065 [US1] Create `Assets/Scripts/Net/Account/Session/SessionAccessTokens.cs` (`IAccessTokenSource`; `GetValidAsync` checks state, then `NeedsRenewal(clock.Now, timing.RenewalMargin)`, then `Renewal.RenewAsync`; `RenewNowAsync` always renews). Depends on T063, T064.
- [X] T066 [US1] Create `Assets/Scripts/Net/Account/Session/ForegroundRenewal.cs` (`IDisposable`; subscribes `ReturnedToForeground`; when `SignedIn` and `NeedsRenewal`, logs and starts `GetValidAsync` without awaiting, observing the task so no exception goes unobserved). Depends on T065.
- [X] T067 [P] [US1] Create `Assets/Scripts/Net/Account/Http/AuthenticatedRequest.cs` and `AuthenticatedCallResult.cs` (response `Status` + `BodyText`, or `AccountCallFailure`).
- [X] T068 [US1] Create `Assets/Scripts/Net/Account/Http/AuthenticatedHttpClient.cs` per contract:
  - get token → send with headers;
  - on 401 compare the token used with `session.CurrentTokens`: same → `RenewAsync` then retry once; different → retry once;
  - map failures;
  - never log bodies.
  Depends on T065, T067.
- [X] T069 [P] [US1] Create `Assets/Scripts/Net/Account/Profile/OwnProfile.cs`, `ProfileRefusalKind.cs` and `ProfileRefusal.cs`.
- [X] T070 [US1] Create `Assets/Scripts/Net/Account/Profile/OwnProfileQuery.cs` (GET `routes.OwnProfile` through `AuthenticatedHttpClient`; decode with the codec; out-of-contract logs `account_response_out_of_contract` with status, kind and path). Depends on T068, T069.
- [X] T071 [P] [US1] Create `Assets/Scripts/Net/Unity/Storage/RefreshTokenVaultSlot.cs` (research R3; SHA-256 via `System.Security.Cryptography.SHA256`).
- [X] T072 [P] [US1] Create `Assets/Scripts/Net/Unity/Storage/DpapiNative.cs` (internal static, under `#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN`):
  - `DATA_BLOB` struct;
  - `[DllImport("crypt32.dll", SetLastError = true)]` for `CryptProtectData` and `CryptUnprotectData`, and `[DllImport("kernel32.dll")]` for `LocalFree`;
  - `static bool TryProtect(byte[] plain, byte[] entropy, out byte[] cipher, out int error)` and the matching `TryUnprotect`, with `Marshal.AllocHGlobal`/`FreeHGlobal` in `finally` and `LocalFree` on output;
  - flag `CRYPTPROTECT_UI_FORBIDDEN`.
  Comment cites research R3 (`ProtectedData` missing from .NET Standard 2.1).
- [X] T073 [US1] Create `Assets/Scripts/Net/Unity/Storage/DpapiRefreshTokenVault.cs` (same `#if`):
  - `(string directory, RefreshTokenVaultSlot slot)`;
  - entropy `anathema.refresh_token.v1`;
  - `Save` writes `<file>.tmp`, then `File.Replace` or `File.Move`;
  - `Read` missing file → `Empty`, failure → `Unreadable("dpapi_error=<code>")`;
  - `Delete` removes file and temp;
  - every IO exception becomes an outcome.
  Depends on T071, T072.
- [X] T074 [P] [US1] Create `Assets/Plugins/Android/RefreshTokenCipher.java` (package `com.anathema.net`), per research R4:
  - `public static void save(Context, String)`, `public static String read(Context)` (null when no file), `public static void delete(Context)`;
  - AES-256-GCM key in `AndroidKeyStore`, alias `anathema_refresh_token_v1`;
  - file `getNoBackupFilesDir()/refresh_token.bin` holding `iv || ciphertext`;
  - exceptions propagate.
  In the `.meta`, confirm the importer is Android-only.
- [X] T075 [P] [US1] Create `Assets/Plugins/Android/proguard-user.txt` with `-keep class com.anathema.net.RefreshTokenCipher { public static *; }` and a comment line citing research R4. Set `useCustomProguardFile: 1` in `ProjectSettings/ProjectSettings.asset`.
- [X] T076 [US1] Create `Assets/Scripts/Net/Unity/Storage/AndroidKeystoreRefreshTokenVault.cs` (under `#if UNITY_ANDROID && !UNITY_EDITOR`):
  - `AndroidJavaClass("com.anathema.net.RefreshTokenCipher")` with `AndroidApplication.currentContext`;
  - `AndroidJavaException` → `Unreadable`/`Failed` with the exception class name only.
  Depends on T074.
- [X] T077 [US1] Create `Assets/Scripts/Net/Unity/Storage/PlatformRefreshTokenVault.cs`:
  - `static IRefreshTokenVault Create(IClientLog log)` → Android adapter on device, otherwise DPAPI in `Path.Combine(Application.persistentDataPath, "account")`;
  - slot `ForEditorProject(Application.dataPath)` in the editor, `ForPlayer()` in the player;
  - logs the chosen slot, never a path with the user name.
  Depends on T073, T076.
- [X] T078 [US1] Create `Assets/Scripts/Net/Unity/LiveAccountServices.cs`:
  - public constructor `(IHttpTransport, IProtocolCodec, IMonotonicClock, IAppLifecycle, IClientLog, AccountRoutes, IRefreshTokenVault, AccountTiming)` builds `Session`, `Tokens` (`SessionAccessTokens`), the internal `AuthenticatedHttpClient`, `Profile` and `ForegroundRenewal`;
  - `static LiveAccountServices FromAdapters(LiveNetworkAdapters, IAppLifecycle, AccountRoutes, IRefreshTokenVault)`;
  - `IDisposable` (disposes `ForegroundRenewal`).
  Depends on T066, T070, T077.
- [X] T079 [US1] Evolve `Assets/Scripts/Core/Session/PlayerSession.cs` per contracts/secure-storage-and-bridge.md:
  - in `Awake` after the singleton check: `UnityConsoleLog`, `MainThreadQueue`, `LiveNetworkAdapters.Create(queue, log, new CleartextPolicy(Debug.isDebugBuild))`, `UnityAppLifecycle` and `UnityNetworkReachability` (as in `DevConnectionProbe`), `AddComponent<NetworkLayerHost>().Attach(...)`, `Account = LiveAccountServices.FromAdapters(..., AppEnvManager.Settings.BuildAccountRoutes(), PlatformRefreshTokenVault.Create(log))`;
  - `Token => Account.Session.CurrentAccessTokenText` (add `public string? CurrentAccessTokenText` to `AccountSession`, with cases "null when signed out, token text when signed in" added to `Assets/Tests/EditMode/Net.Account/Session/AccountSessionSignInTests.cs`);
  - remove `RefreshToken`, `SetTokens`, `SetAccessToken`;
  - `SetProfile(OwnProfile)`;
  - `OnDestroy` disposes `Account`;
  - keep the existing summaries and add one citing the debt (plan, Complexity Tracking).
  Depends on T078, T045.
- [X] T080 [US1] Evolve `Assets/Scripts/Core/Network/TokenRefreshService.cs`:
  - remove `MonoBehaviour`, coroutine, `UnityWebRequest`, `JsonUtility`, `Debug`;
  - `Instance` becomes a lazily created plain instance;
  - `Refresh(Action<TokenRefreshResult>)` awaits `PlayerSession.Instance.Account.Tokens.RenewNowAsync()` and maps per contract, invoking the callback on the main thread (the task already completes there);
  - keep the enum, its summaries and the class summary about 4001, updated to say it now delegates.
  Depends on T079.
- [X] T081 [US1] Evolve `Assets/Scripts/Core/Network/SelfProfileService.cs`: replace `LoadProfile(string)` and the coroutine with `public async Task<bool> LoadProfileAsync()` using `PlayerSession.Instance.Account.Profile.ReadAsync()` and `SetProfile`; log failures through `IClientLog` (expose `Log` on `PlayerSession`). Depends on T079.
- [X] T082 [US1] Evolve `Assets/Scripts/Login/LoginController.cs`:
  - remove `using UnityEditor`, `UnityEngine.Networking`, `System.Text`, `SendLoginRequest`, `BuildLoginUrl`, `BuildLoginRequestDto`, `CreatePostRequest`, `HandleLoginSuccess` and every `Debug.Log*`;
  - `HandleLogin` is the only `async void` (try/catch → log) and calls `SignInAsync(username, new Password(password))`;
  - on `SignedIn`: `LoadProfileAsync()` (failure logged, does not block), then `SceneManager.LoadScene("HomeScene")`;
  - keep the comment about the removed presence socket;
  - keep `#region Auto Login Gambiarra`, calling the new sign-in.
  Depends on T081.
- [X] T083 [US1] Delete `Assets/Scripts/DTO/Network/LoginRequestDTO.cs`, `LoginResponseDTO.cs`, `TokenRefreshDTO.cs` and `Assets/Scripts/DTO/Player/SelfPlayerProfileDTO.cs` with their `.meta`. Grep `Assets/Scripts` for the four type names and for `SetTokens`, `SetAccessToken` and `.RefreshToken`: zero hits. Depends on T080, T081, T082.
- [ ] T084 [US1] Run the suite. Then quickstart.md section 3, steps 1–2 (Play → login → Home with mini profile; Console with `account_signed_in` and no token).

**Checkpoint**: jogador entra pela cena de sempre; toda feature seguinte já pode pedir `IAccessTokenSource`.

---

## Phase 4: User Story 2 - Abrir o app e continuar logado (Priority: P1)

**Goal**: retomada sem senha a partir da guarda, com os quatro desfechos; `LoginController` retoma no `Start`.

**Independent Test**: `AccountSessionResumeTests` com `FakeRefreshTokenVault.Preload`; quickstart seção 3 passos 3–4.

### Tests for User Story 2 (MANDATORY) ⚠️

- [X] T085 [P] [US2] Create `Assets/Tests/EditMode/Net.Account/Session/AccountSessionResumeTests.cs` (contract test 10):
  - preload + refresh 200 → `Resumed(7)`, `SignedIn`, `Self` 7, one request with the preloaded refresh in the body, `account_resumed` logged;
  - refresh 401 → `Refused`, vault deleted, `SignedOut`, `SessionExpired` count 0;
  - transport → `Unavailable(Transport)`, vault intact, `SignedOut`;
  - 503 → `Unavailable(ServerStatus)`, vault intact;
  - empty vault → `NothingStored`, zero requests;
  - `MakeUnreadable` → `NothingStored`, vault deleted, `vault_unreadable` logged;
  - already `SignedIn` → `Resumed(Self)` with zero requests;
  - after resume, `GetValidAsync` returns the resumed token without a new request.
- [X] T086 [P] [US2] Extend `Assets/Tests/EditMode/Net.Unity/LiveAccountServicesTests.cs`: two `LiveAccountServices` sharing one `FakeRefreshTokenVault`; the first signs in; the second `ResumeAsync` → `Resumed` with the same `UserId`.

### Implementation for User Story 2

- [X] T087 [P] [US2] Create `Assets/Scripts/Net/Account/Session/ResumeOutcomeKind.cs` and `ResumeOutcome.cs`.
- [X] T088 [US2] Add `public Task<ResumeOutcome> ResumeAsync()` to `Assets/Scripts/Net/Account/Session/AccountSession.cs`:
  - `SignedIn` short-circuit;
  - vault read: `Empty` → `NothingStored`; `Unreadable` → delete, log, `NothingStored`;
  - `Found` → POST refresh through the same request/mapping code as `TokenRenewal` (extract a shared internal `RefreshCall` in `Assets/Scripts/Net/Account/Session/RefreshCall.cs` used by both; update `TokenRenewal`);
  - 200 → set tokens + `Self`, `Generation++`, `SignedIn`; 401 → delete, `Refused`, no event; other → `Unavailable`.
  Depends on T087.
- [X] T089 [US2] Evolve `Assets/Scripts/Login/LoginController.cs`:
  - `Start` becomes `async void` (try/catch → log): `SetLoginInteractable(false)`, then `ResumeAsync()`;
  - `Resumed` → `LoadProfileAsync()`, then `HomeScene`;
  - otherwise `SetLoginInteractable(true)`, then the existing dev auto-login block (keep `#if UNITY_EDITOR || DEVELOPMENT_BUILD` and the tag logic).
  Depends on T088.
- [ ] T090 [US2] Run the suite. Then quickstart.md section 3, steps 3–4 (Play again resumes without password; two virtual players each resume their own session).

**Checkpoint**: fechar e abrir não pede senha; US1 e US2 formam o MVP.

---

## Phase 5: User Story 3 - Criar conta (Priority: P2)

**Goal**: cadastro com recusa por campo, sem autenticar.

**Independent Test**: `AccountRegistrationTests` com transporte fake.

### Tests for User Story 3 (MANDATORY) ⚠️

- [X] T091 [P] [US3] Create `Assets/Tests/EditMode/Net.Account/Registration/AccountRegistrationTests.cs`:
  - body has exactly `username`, `email`, `password`, `password_confirmation`;
  - 201 → success, session `SignedOut`, vault untouched;
  - 400 `{"email": ["Este e-mail já está sendo utilizado."]}` → one field `Email` with that message;
  - 400 with `password` (two messages), `password_confirmation` and `non_field_errors` → three fields in body order, the last as `Other` with name `non_field_errors`;
  - 400 with a list body → `UnrecognizedBody`;
  - 500 → `UnrecognizedBody`;
  - transport → `TransportFailed`;
  - no log field contains the password.

### Implementation for User Story 3

- [X] T092 [P] [US3] Create in `Assets/Scripts/Net/Account/Registration/`: `RegistrationField.cs`, `RegistrationFieldError.cs`, `RegistrationRefusal.cs`, `AccountCreated.cs`, `RegistrationForm.cs`.
- [X] T093 [US3] Add `WriteRegistration(IPayloadWriter, RegistrationForm)` to `Assets/Scripts/Net/Account/Session/AccountRequestBodies.cs`, with a case in `AccountRequestBodiesTests.cs`. Depends on T092.
- [X] T094 [US3] Create `Assets/Scripts/Net/Account/Registration/AccountRegistration.cs` (`(IHttpTransport, IProtocolCodec, IClientLog, AccountRoutes)`; POST without `Authorization`; on 400, `DecodeObject` the body and, for each name in `FieldNames` in order, `ReadTextList(name)` into a `RegistrationFieldError`; any `PayloadShapeException` or non-object body → `UnrecognizedBody`). Depends on T093, T029.
- [X] T095 [US3] Add `Registration` to `Assets/Scripts/Net/Unity/LiveAccountServices.cs` and a not-null assertion to `LiveAccountServicesTests.cs`. Depends on T094.
- [X] T096 [US3] Run the suite.

**Checkpoint**: conta nova pode ser criada pelo cliente.

---

## Phase 6: User Story 4 - Consultar o catálogo de cartas (Priority: P2)

**Goal**: catálogo carregado uma vez por sessão, tipos distintos de unidade e feitiço, efeito tipado, tolerante a carta ruim.

**Independent Test**: `CatalogReaderTests` e `CardCatalogTests` com transporte fake.

### Tests for User Story 4 (MANDATORY) ⚠️

- [X] T097 [P] [US4] Create `Assets/Tests/EditMode/Net.Account/Catalog/CatalogReaderTests.cs` (contracts/player-data.md, tests 2 and 3):
  - `card_id` 1 unit and 1004 spell → `Find` gives `Unit` with attack/health and `Spell` with description and effect (`requires_target` false, `None`, `Permanent`, `declaration_only` false);
  - `Find(new CardId(9999))` → `NotFound` with `Requested` 9999;
  - `target_kind: "any_unit"` → `Unknown` + text + `catalog_effect_value_unknown`;
  - `duration: "forever"` → `Unknown`;
  - unit without `health` → skipped with `field=health`;
  - `card_type: "relic"` → skipped with `reason=unknown_card_type`;
  - duplicated `card_id` → first kept, `catalog_card_duplicated`;
  - order of `Cards` = server order minus skipped;
  - body without `cards` → failure;
  - `cards` as object → failure.
- [X] T098 [P] [US4] Create `Assets/Tests/EditMode/Net.Account/Catalog/CardCatalogTests.cs` (test 4):
  - two concurrent `LoadAsync` with `HoldNext` → one request;
  - second `LoadAsync` after success → zero requests;
  - transport failure then success → two requests;
  - `SignOut` + sign in → a new request;
  - 500 → `UnrecognizedRefusal`;
  - 200 without `cards` → `OutOfContract`.

### Implementation for User Story 4

- [X] T099 [P] [US4] Create in `Assets/Scripts/Net/Account/Catalog/`: `SpellTargetKind.cs`, `SpellDuration.cs`, `SpellEffect.cs`, `CardLookupKind.cs`.
- [X] T100 [P] [US4] Create in `Assets/Scripts/Net/Account/Catalog/`: `CatalogCard.cs` (abstract, `private protected` constructor), `UnitCard.cs`, `SpellCard.cs`, `UnrecognizedCatalogCard.cs` (internal).
- [X] T101 [US4] Create `Assets/Scripts/Net/Account/Catalog/CardLookup.cs` and `LoadedCatalog.cs` (dictionary by `CardId` plus ordered list; `Generation`). Depends on T100.
- [X] T102 [US4] Create `Assets/Scripts/Net/Account/Catalog/CatalogReader.cs` (internal):
  - `DiscriminatedUnion<CatalogCard>` on `card_type` with `unit` and `spell` arms and an unknown arm returning `UnrecognizedCatalogCard`;
  - per-item `DecodeObject` with skip and log;
  - effect enums mapped from exact contract text;
  - summary cites `http_catalog.md` by path.
  Depends on T099, T101.
- [X] T103 [US4] Create `Assets/Scripts/Net/Account/Catalog/CardCatalog.cs` (cache of the `LoadedCatalog` + in-flight task; discards when `session.Generation` differs; failures not cached). Depends on T102.
- [X] T104 [US4] Add `Catalog` to `Assets/Scripts/Net/Unity/LiveAccountServices.cs` and to `LiveAccountServicesTests.cs`. Depends on T103.
- [X] T105 [US4] Run the suite.

**Checkpoint**: qualquer `CardId` resolve em carta tipada.

---

## Phase 7: User Story 5 - Gerenciar os próprios decks (Priority: P2)

**Goal**: listar, ler, criar, alterar e apagar decks, com recusas tipadas; `DeckProblem` reutilizável pelo socket.

**Independent Test**: `DeckProblemUnionTests`, `DeckRefusalReaderTests` e `PlayerDecksTests` com transporte fake.

### Tests for User Story 5 (MANDATORY) ⚠️

- [X] T106 [P] [US5] Create `Assets/Tests/EditMode/Net.Json/Decks/DeckProblemUnionTests.cs`:
  - `wrong_deck_size` (found 39, required 40), `too_many_copies` (card 12, count 4, limit 3) and `unknown_card` (9999) with `message` each;
  - `kind: "banned_card"` with `message` → `UnrecognizedDeckProblem` keeping kind and message;
  - unknown without `message` → `MissingField`;
  - `card_id` as text → `WrongFieldType`;
  - a test asserting `typeof(DeckProblem).Assembly` is `Anathema.Net.Core` (FR-035).
- [X] T107 [P] [US5] Create `Assets/Tests/EditMode/Net.Account/Decks/DeckChangeTests.cs`: neither name nor cards → `ArgumentException` naming both; name only, cards only and both accepted. Also `DeckDraft` with null name or null list throws.
- [X] T108 [P] [US5] Create `Assets/Tests/EditMode/Net.Account/Decks/DeckRefusalReaderTests.cs`, one test per row of the key table in contracts/player-data.md:
  - `name`, `deck_problems` (three problems in order), `deck_limit`, `detail`;
  - `name` + `deck_problems` together in table order;
  - `{}` → `UnrecognizedDeckRefusal`;
  - HTML body with 502 → `UnrecognizedDeckRefusal` with status 502.
- [X] T109 [P] [US5] Create `Assets/Tests/EditMode/Net.Account/Decks/PlayerDecksTests.cs`, one test per row of the operations table:
  - `ListAsync` 200 `{"decks": []}` → empty;
  - `ReadAsync` 200 / 404;
  - `CreateAsync` 201 with body `name` + `card_ids` exactly as given (12 cards sent unchanged, no client validation) and 400;
  - `ChangeAsync` with name only → body has only `name`, then 200 / 404;
  - `DeleteAsync` 204 with empty body → `DeckDeleted`, then 404;
  - URL of `ReadAsync(new DeckId(4))` ends with `/players/decks/4/`.

### Implementation for User Story 5

- [X] T110 [P] [US5] Create in `Assets/Scripts/Net/Core/Decks/`: `DeckProblem.cs` (abstract, `Message`), `WrongDeckSize.cs`, `TooManyCopies.cs`, `UnknownCard.cs`, `UnrecognizedDeckProblem.cs` (each with `internal static ... Read(IPayloadReader)`).
- [X] T111 [US5] Create `Assets/Scripts/Net/Core/Decks/DeckProblemUnion.cs` (`static DiscriminatedUnion<DeckProblem> Create()` using the new constructor from T030; summary cites `http_decks.md` and `matchmaking_messages.md` by path and says feature 3 reuses it). Depends on T110.
- [X] T112 [P] [US5] Create in `Assets/Scripts/Net/Account/Decks/`: `PlayerDeck.cs` (with internal `Read(IPayloadReader)`), `DeckDraft.cs`, `DeckChange.cs`, `DeckDeleted.cs`.
- [X] T113 [P] [US5] Create in `Assets/Scripts/Net/Account/Decks/`: `DeckRefusalReason.cs` (abstract, `private protected` constructor), `InvalidDeckName.cs`, `DeckListRejected.cs`, `MissingDeckField.cs`, `DeckLimitReached.cs`, `DeckNotFound.cs`, `UnrecognizedDeckRefusal.cs`, `DeckRefusal.cs`.
- [X] T114 [US5] Create `Assets/Scripts/Net/Account/Decks/DeckRefusalReader.cs` (internal; 404 → `DeckNotFound`; 400 by keys in table order; otherwise unrecognized). Depends on T111, T113.
- [X] T115 [US5] Create `Assets/Scripts/Net/Account/Decks/PlayerDecks.cs` (five operations over `AuthenticatedHttpClient`; bodies with `WriteText` and `WriteCardIdList`). Depends on T112, T114.
- [X] T116 [US5] Add `Decks` to `Assets/Scripts/Net/Unity/LiveAccountServices.cs` and to `LiveAccountServicesTests.cs`. Depends on T115.
- [X] T117 [US5] Run the suite.

**Checkpoint**: decks do jogador completos; `DeckProblem` pronto para a recusa `invalid_deck` da feature 3.

---

## Phase 8: User Story 6 - Ler o histórico de partidas (Priority: P3)

**Goal**: histórico paginado com oponente opcional e motivo do fim tipado.

**Independent Test**: `MatchHistoryTests` com transporte fake.

### Tests for User Story 6 (MANDATORY) ⚠️

- [X] T118 [P] [US6] Create `Assets/Tests/EditMode/Net.Account/History/MatchHistoryTests.cs` (contracts/player-data.md, test 7):
  - page 1 `count: 0`, `results: []`, `next`/`previous` null → empty, both flags false;
  - two rows like the contract example → `MatchId`, `Won`, `NexusDepleted`/`Forfeit`, opponent `UserId` 9 and `null`, duration, round, `EndedAtText` unchanged;
  - `next` not null → `HasNext`;
  - `end_reason: "draw"` → `Unknown` + `history_end_reason_unknown`;
  - 404 on page 3 → `PastTheEnd`; 404 on page 1 → `NoProfile`;
  - 500 → `Unrecognized`;
  - request URL has `page` and `page_size`.

### Implementation for User Story 6

- [X] T119 [P] [US6] Create in `Assets/Scripts/Net/Account/History/`: `MatchEndReason.cs`, `HistoryOpponent.cs`, `MatchHistoryRow.cs`, `MatchHistoryPage.cs`, `HistoryRefusalKind.cs`, `HistoryRefusal.cs`.
- [X] T120 [US6] Create `Assets/Scripts/Net/Account/History/MatchHistory.cs` (GET `routes.MatchesPage(request)`; row reading with `ReadMatchId`, `ReadOptionalObject("opponent")`, `ReadUserId` inside it; 404 mapping by `request.Page`). Depends on T119.
- [X] T121 [US6] Add `History` to `Assets/Scripts/Net/Unity/LiveAccountServices.cs` and to `LiveAccountServicesTests.cs`. Depends on T120.
- [X] T122 [US6] Run the suite.

**Checkpoint**: todas as histórias entregues.

---

## Phase 9: Polish & Cross-Cutting Concerns

**Purpose**: portas de qualidade que cruzam histórias (SC-002, SC-003, SC-006 a SC-009), dívida registrada e validação manual.

- [X] T123 [P] Create `Assets/Tests/EditMode/Net.Account/AccountCredentialLeakTests.cs` (SC-003):
  - one password, one access token and one refresh token with distinctive texts;
  - runs sign-in success and refused, renewal 200/401/503/transport/out-of-contract, 401 retry, resume in all four outcomes, registration 400, profile 200 and a login response that is out of contract;
  - asserts no `LogField.Value` and no `EventName` of any `FakeClientLog` entry contains any of the three texts;
  - build scenarios from small private methods, not copied setups.
- [X] T124 [P] Create `Assets/Tests/EditMode/Net.Unity/NoCredentialInPlayerPrefsTests.cs` (FR-022): read every `*.cs` under `Path.Combine(Application.dataPath, "Scripts")` and fail listing any file that contains `PlayerPrefs`.
- [X] T125 Create `Assets/Tests/EditMode/Net.Unity/LiveServer/LiveAccountTests.cs` (`[Explicit, Category("LiveServer")]`, same `RunStep`-style drain loop as `LiveServerProbeTests.cs`, `http://127.0.0.1:8000`, DPAPI vault in a temp directory with slot `Named("live-test-" + guid)`, deleted in `TearDown`), one `[UnityTest]` per case in research R11:
  1. register `acct_<12 hex>`, sign in, profile nickname equals username;
  2. catalog 29/24/5;
  3. starter deck listed and none of its cards is a `SpellCard`;
  4. spell deck 201, rename 200, 12-card create → `WrongDeckSize(12, 40)`, delete, read → `DeckNotFound`;
  5. history `Count == 0`;
  6. `RenewNowAsync` → `Renewed`, then profile 200;
  7. second `LiveAccountServices` on the same vault → `ResumeAsync` → `Resumed` with the same `UserId`.
  Depends on US1–US6.
- [ ] T126 Run quickstart.md section 2 with `docker compose up` in the backend: all `LiveAccountTests` pass (SC-006). Record the result in the execution log below.
- [X] T127 Add to `C:/Users/gabri/Obsidian/Projetos/Anathema/Game/TODO.md`, under a new heading `## Cliente — conta e dados do jogador (feature 002)`, three items, each with diagnosis and path (plan.md, Código anterior tocado):
  - ponte `PlayerSession.Token`/`TokenRefreshService` até a feature 3;
  - `SessionExpired` sem reação visual;
  - `PlayerSession.Instance` como raiz de composição.
  Also mark the existing `LoginController` item as done by feature 002 and correct its `PlayerPrefs` mention.
- [ ] T128 Build a development APK and run quickstart.md sections 4 and 5 on an Android device (SC-008, SC-009). Record results.
- [ ] T129 Run quickstart.md section 3 in full, including step 4 with two virtual players (SC-007, FR-024).
- [ ] T130 Run the suite a final time with the editor closed and confirm:
  - zero failures and zero new compiler warnings (SC-001);
  - EditMode time of the `Anathema.Net.Account.Tests` fixture under 5 s (SC-011);
  - every new file under `Assets/` has a `.meta` and every deleted file lost its `.meta`.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: sem dependências.
- **Foundational (Phase 2)**: depende do Setup; bloqueia todas as histórias.
- **US1 (Phase 3)**: depende só da fundação.
- **US2 (Phase 4)**: depende de US1 (sessão, `TokenRenewal`, `LoginController` religado).
- **US3 (Phase 5)**: depende da fundação; T093 e T095 editam arquivos de US1 (`AccountRequestBodies`, `LiveAccountServices`), então entra depois de T059 e T078.
- **US4, US5, US6 (Phases 6–8)**: dependem de US1 (`AuthenticatedHttpClient`, `LiveAccountServices`). Entre si são independentes, exceto que as tarefas que editam `LiveAccountServices.cs` (T104, T116, T121) rodam em sequência.
- **Polish (Phase 9)**: T123 e T124 depois de US1–US3; T125–T130 depois de todas.

### User Story Dependencies

```text
Setup ─► Foundational ─► US1 ─┬─► US2 ─────────────┐
                              ├─► US3 ─────────────┤
                              ├─► US4 ─────────────┼─► Polish
                              ├─► US5 ─────────────┤
                              └─► US6 ─────────────┘
```

### Within Each User Story

- Testes antes da implementação: primeiro o teste falha ou não compila, depois passa.
- Tipos de valor antes dos serviços; serviços antes da composição (`LiveAccountServices`); composição antes do código antigo.
- Núcleo (`Anathema.Net.Core`) antes de `Anathema.Net.Account`; `Account` antes de `Anathema.Net.Unity`.
- Rodar a suíte no fim de cada fase.

### Dependências dentro das fases (as [P] que esperam outra tarefa)

- Fundação: T029 ← T027, T028; T033 ← T031, T032; T035 ← T033; T016 compila só depois de T034; T040 ← T038, T039; T043 ← T027, T042; T045 ← T043; T046 ← T045.
- US1: T062 ← T040, T043, T044, T058–T061; T063 ← T062; T065 ← T063, T064; T066 ← T065; T068 ← T065, T067; T070 ← T068, T069; T073 ← T071, T072; T076 ← T074; T077 ← T073, T076; T078 ← T066, T070, T077; T079 ← T078, T045; T080, T081 ← T079; T082 ← T081; T083 ← T080–T082.
- US2: T088 ← T087, T063; T089 ← T088, T082.
- US3: T093 ← T092, T059; T094 ← T093; T095 ← T094, T078.
- US4: T101 ← T100; T102 ← T099, T101; T103 ← T102, T068; T104 ← T103.
- US5: T111 ← T110, T030; T114 ← T111, T113; T115 ← T112, T114, T068; T116 ← T115.
- US6: T120 ← T119, T068, T042; T121 ← T120.

---

## Parallel Example: Foundational

```bash
Task: "DeckIdTests em Assets/Tests/EditMode/Net.Core/Identity/DeckIdTests.cs"
Task: "RefreshTokenTests em Assets/Tests/EditMode/Net.Core/Storage/RefreshTokenTests.cs"
Task: "AccessTokenReaderTests em Assets/Tests/EditMode/Net.Account/Tokens/AccessTokenReaderTests.cs"
Task: "JwtTestTokens em Assets/Tests/EditMode/Net.Account/JwtTestTokens.cs"
Task: "AccountRoutesTests em Assets/Tests/EditMode/Net.Account/AccountRoutesTests.cs"
Task: "DeckId em Assets/Scripts/Net/Core/Identity/DeckId.cs"
Task: "DiscriminatedUnion (construtor novo) em Assets/Scripts/Net/Core/Protocol/DiscriminatedUnion.cs"
```

## Parallel Example: User Story 1

```bash
# Testes, todos juntos:
Task: "AccountSessionSignInTests em Assets/Tests/EditMode/Net.Account/Session/AccountSessionSignInTests.cs"
Task: "TokenRenewalTests em Assets/Tests/EditMode/Net.Account/Session/TokenRenewalTests.cs"
Task: "AuthenticatedHttpClientTests em Assets/Tests/EditMode/Net.Account/Http/AuthenticatedHttpClientTests.cs"
Task: "DpapiRefreshTokenVaultTests em Assets/Tests/EditMode/Net.Unity/Storage/DpapiRefreshTokenVaultTests.cs"

# Tipos e guardas de plataforma, juntos:
Task: "AccountSessionState/SignInOutcome em Assets/Scripts/Net/Account/Session/"
Task: "RefreshTokenVaultSlot em Assets/Scripts/Net/Unity/Storage/RefreshTokenVaultSlot.cs"
Task: "DpapiNative em Assets/Scripts/Net/Unity/Storage/DpapiNative.cs"
Task: "RefreshTokenCipher.java em Assets/Plugins/Android/RefreshTokenCipher.java"
```

## Parallel Example: User Stories 3 a 6 (depois de US1)

```bash
Task: "AccountRegistrationTests em Assets/Tests/EditMode/Net.Account/Registration/AccountRegistrationTests.cs"
Task: "CatalogReaderTests em Assets/Tests/EditMode/Net.Account/Catalog/CatalogReaderTests.cs"
Task: "DeckProblemUnionTests em Assets/Tests/EditMode/Net.Json/Decks/DeckProblemUnionTests.cs"
Task: "MatchHistoryTests em Assets/Tests/EditMode/Net.Account/History/MatchHistoryTests.cs"
Task: "Tipos de catálogo em Assets/Scripts/Net/Account/Catalog/"
Task: "Tipos de DeckProblem em Assets/Scripts/Net/Core/Decks/"
Task: "Tipos de histórico em Assets/Scripts/Net/Account/History/"
```

---

## Implementation Strategy

### MVP First (US1 + US2)

1. Phase 1 (Setup) e Phase 2 (Foundational).
2. Phase 3 (US1): login, tokens, guarda, cena religada.
3. Phase 4 (US2): retomada.
4. **Parar e validar**: suíte, quickstart seção 3. A feature 3 já pode começar sobre `IAccessTokenSource`.

### Incremental Delivery

1. Setup e fundação.
2. US1: entrar e ficar autenticado. Destrava a feature 3.
3. US2: retomar sem senha. Destrava o uso real no Android.
4. US5: decks (a fila da feature 3 precisa de `DeckId` e `DeckProblem`).
5. US4: catálogo (as features 4 e 5 precisam para mostrar e mirar cartas).
6. US3 e US6: cadastro e histórico.
7. Polish: LiveServer, credenciais, aparelho, dívida no vault.

### Parallel Team Strategy

- Depois da fundação, uma frente só em US1 (é gargalo).
- Depois de US1: uma frente em US2, outra em US4 e US5 (núcleo de decks primeiro), e uma terceira em US3 e US6.
- Tarefas que editam `LiveAccountServices.cs` ou `AccountRequestBodies.cs` são serializadas entre as frentes.

---

## Notes

- **O código antigo não tem teste.** `PlayerSession`, `TokenRefreshService`,
  `SelfProfileService` e `LoginController` ficam em `Assembly-CSharp`, que
  nenhuma asmdef de teste pode referenciar. Por isso a ponte é só delegação de
  uma linha. O comportamento dela (renovar sem margem, compartilhar,
  mapear) está testado em `Anathema.Net.Account` (T050, T051), e a cena é
  validada pelo quickstart (T084, T090, T129).
- **O adaptador do Android não roda no editor.** É validado só no aparelho (T128).
- `BaseClient`, `MatchClient`, `MatchmakingClient`, `NetworkBootstrap`,
  `MiniPlayerProfile` e `AppEnvManager` não são editados.
- Commit ao fim de cada tarefa ou grupo lógico, sempre com os `.meta`.
- Se um contrato do backend e esta lista discordarem, vale o contrato.

## Registro da execução (2026-09-13)

- **Verificação offline.** O `Unity.exe` não abre a partir desta sessão, então a suíte oficial não rodou. No lugar dela:
  - as assemblies foram compiladas com o Roslyn do Unity 6000.2.8f1: Core, Json, Fakes, Account, Unity, Config, `Assembly-CSharp` e as de teste;
  - os testes rodaram no Mono do Unity pelo NUnit do pacote.
- **Compilação.** Nenhum erro e nenhum aviso novo. `Anathema.Net.Unity` também compilou com `UNITY_ANDROID`, que é o caminho do Keystore. Os avisos CS0649 de `AppEnvManager`, `VersusController` e `MiniPlayerProfile` já existiam.
- **Testes executados.**
  - `Anathema.Net.Core.Tests`, `Anathema.Net.Json.Tests` e `Anathema.Net.Account.Tests`: 402 passaram, 0 falharam.
  - Em `Anathema.Net.Unity.Tests` rodaram só os fixtures sem motor, 15 passaram:
    - `DpapiRefreshTokenVaultTests`, com DPAPI real numa pasta temporária;
    - `RefreshTokenVaultSlotTests`;
    - `LiveAccountServicesTests`.
- **Testes só compilados.** O restante de `Anathema.Net.Unity.Tests`, `NoCredentialInPlayerPrefsTests` (usa `Application.dataPath`), `Anathema.Config.Tests` (`AppConfig` é `ScriptableObject`) e `LiveAccountTests` (`[Explicit]`, precisa do backend).
- **Bug de teste corrigido.** `LiveAccountServicesTests` criava os fakes em campos, e o NUnit reaproveita a instância do fixture, então os pedidos de um teste vazavam para o seguinte. Os fakes passaram para o `[SetUp]`.
- **Desvios do plano e das tarefas.**
  - T020: `JwtTestTokens` virou `FakeAccessJwt`, e os corpos de login e renovação viraram `FakeAccountResponses`. Os dois ficam em `Anathema.Net.Fakes`, porque `Anathema.Net.Unity.Tests` também precisa deles. `AccountTestRig.LoginBody` e `RefreshBody` só delegam.
  - `AccountSession` não recebe `AccountTiming`.
  - `ForegroundRenewal` recebe `IMonotonicClock` e `AccountTiming`.
  - `AccountCallFailure` ganhou `RenewalUnavailable`, para renovação que não saiu por status ou por resposta fora do contrato.
  - `contracts/account-session.md` foi atualizado com essas três mudanças.
  - `RefreshCall`, o pedido de renovação compartilhado entre renovação e retomada, entrou já na US1.
  - `ResumeAsync` não aplica nem apaga a guarda se um login mudar a geração durante a retomada.
  - A união por `card_type` é de um tipo interno, `CatalogEntry`, com os braços `ListedCatalogCard` e `UnrecognizedCatalogCard`. Assim, carta de tipo desconhecido não finge os campos de `CatalogCard`.
  - T082 e T089: o `LoginController` foi escrito uma vez só, com login e retomada.
  - As rotas do backend local dos testes de `Net.Unity` ficam em `LocalAccountRoutes`.
  - `ProjectSettings.asset`: `useCustomProguardFile` passou para 1 (T075).
- **Plugin Java.** `RefreshTokenCipher.java` não foi compilado aqui, porque não há Gradle na sessão. A validação é a T128, no aparelho.
- **Arquivos `.meta`.** Os `.meta` dos arquivos e pastas novos em `Assets/` só existem depois que o Unity abrir o projeto.
- **Em aberto.** Precisam do editor, do backend ou do aparelho:
  - T084, T090 e T129: quickstart seção 3, no editor;
  - T126: LiveServer com `docker compose up`;
  - T128: aparelho Android;
  - T130: suíte oficial e `.meta`.
