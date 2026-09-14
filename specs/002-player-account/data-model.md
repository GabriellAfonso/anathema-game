# Data Model: Conta e dados do jogador

**Feature**: `002-player-account` | **Date**: 2026-09-13

Como o cliente representa conta, tokens, catálogo, decks e histórico. As formas
JSON são as do backend (spec, "Contratos") e não são repetidas. Todos os tipos
são imutáveis, salvo indicação; todo arquivo com `#nullable enable`; nenhum campo
ou parâmetro público chamado só `id`.

---

## Identidade (`Anathema.Net.Core/Identity`)

| Tipo | Valor | No JSON | Validação | Log |
|---|---|---|---|---|
| `DeckId` | `long` | inteiro cru (`deck_id`) | > 0 | `deck_id=4` |
| `CardId` | `long` | inteiro cru (`card_id`, itens de `card_ids`) | > 0 | `card_id=1004` |

Mesmo padrão de `UserId`: `readonly struct`, `IEquatable<T>`, sem conversão
implícita. Extensões novas em `PayloadIdentityReading`/`PayloadIdentityWriting`:

- `ReadDeckId`, `ReadCardId`, `ReadCardIdList`;
- `WriteDeckId`, `WriteCardIdList`.

`IdentityIsolationTests` ganha os dois tipos.

---

## Guarda segura (`Anathema.Net.Core/Storage`)

**`IRefreshTokenVault`**: `Read()`, `Save(RefreshToken)`, `Delete()`, síncronos.

**`RefreshToken`**: texto não vazio; `ToString()` → `refresh_token=<redacted>`;
`RevealForRequest()` devolve o texto.

**`VaultReadOutcome`**: `Found(RefreshToken)` | `Empty` | `Unreadable(detail)`.
O `detail` nunca contém o valor.

**`VaultWriteOutcome`**: `Saved` | `Failed(detail)`.

---

## Problema de lista de deck (`Anathema.Net.Core/Decks`)

Hierarquia fechada, lida por `DeckProblemUnion.Create()`, uma
`DiscriminatedUnion<DeckProblem>` por `kind`. Independe de HTTP (FR-035).

| Tipo | `kind` | Campos além de `Message` |
|---|---|---|
| `WrongDeckSize` | `wrong_deck_size` | `long Found`, `long Required` |
| `TooManyCopies` | `too_many_copies` | `CardId Card`, `long Count`, `long Limit` |
| `UnknownCard` | `unknown_card` | `CardId Card` |
| `UnrecognizedDeckProblem` | outro | `string Kind` |

`DeckProblem` é abstrata, com construtor `private protected`, e tem
`string Message`. Um `kind` desconhecido também exige `message`; se ela faltar,
a decodificação falha com `MissingField`. Para isso, `DiscriminatedUnion<TBase>`
ganha um construtor cujo braço desconhecido recebe o texto do discriminador
**e** o objeto (`Func<string, IPayloadReader, TBase>`); o construtor antigo
delega a ele.

**Recusas com tipo** (`ProfileRefusal`, `HistoryRefusal`): classe com `Kind`
(`ProfileMissing`/`Unrecognized`; `PastTheEnd`/`NoProfile`/`Unrecognized`) e
`UnrecognizedRefusal? Unrecognized`, preenchido só no último caso.

**Carta de tipo desconhecido**: `UnrecognizedCatalogCard(string CardTypeText)`,
`internal` a `Anathema.Net.Account`, é o braço desconhecido da união por
`card_type`. `CatalogReader` o descarta e registra, e ele nunca aparece em
`LoadedCatalog`.

**Lista de textos**: `IPayloadReader` ganha
`IReadOnlyList<string> ReadTextList(string field)`, para as mensagens por campo
do cadastro e as recusas `name`/`deck_limit` de deck.

---

## Tokens (`Anathema.Net.Account/Tokens`)

**`AccessToken`**
- `UserId Owner` — do claim `user_id` (texto com inteiro positivo).
- `TimeSpan Lifetime` — `exp - iat`, > 0.
- `MonotonicInstant ArrivedAt`.
- `MonotonicInstant ExpiresAt => ArrivedAt.Add(Lifetime)`.
- `bool NeedsRenewal(MonotonicInstant now, TimeSpan margin)` →
  `now >= ExpiresAt - margin`.
- `RevealForRequest()`; `ToString()` → `access_token=<redacted> user_id=7`.

**`AccessTokenReader`**: `DecodeOutcome<AccessToken> Read(string jwt,
MonotonicInstant arrivedAt)`. Falhas possíveis: `NotJson` (partes ou base64),
`MissingField`/`WrongFieldType` (`exp`, `iat`, `user_id`) e `InvalidValue`
(`exp <= iat`, `user_id` que não é inteiro positivo).

**`SessionTokens`**: `AccessToken Access`, `RefreshToken Refresh`. Só em memória.

**`AccountTiming`**: `TimeSpan RenewalMargin` (padrão 30 s, de 0 a 120 s).

---

## Sessão (`Anathema.Net.Account/Session`)

**`AccountSession`** (estado mutável, só na thread principal)

- `AccountSessionState State`
- `UserId? Self`
- `int Generation`
- `event Action? SessionExpired`

Estados:

```text
SignedOut ──SignIn ok──────────► SignedIn ──SignOut─────────► SignedOut
    │                               │
    ├──Resume ok──────────────────► │
    │                               └──renovação 401─► Expired ──SignIn / Resume ok──► SignedIn
    └──SignIn em curso: SigningIn (segundo SignIn → AlreadyInProgress)
```

| Transição | Efeitos |
|---|---|
| → `SignedIn` | tokens em memória, `Self` definido, guarda gravada (no login), `Generation++` |
| `SignedIn` → `SignedOut` | tokens nulos, `Self` nulo, guarda apagada, `Generation++`, sem evento |
| `SignedIn` → `Expired` | tokens nulos, guarda apagada, `Generation++`, `SessionExpired` uma vez |
| `Expired` → `SignedOut` por `SignOut` | nada além do estado |

**`SignInOutcome`** (`Kind` + dados)
- `SignedIn(UserId)`
- `CredentialsRefused`
- `ServerRefused(int status)`
- `TransportFailed(TransportFailure)`
- `OutOfContract(DecodeFailure)`
- `AlreadyInProgress`

**`ResumeOutcome`**
- `Resumed(UserId)`
- `NothingStored`
- `Refused`
- `Unavailable(RenewalUnavailableReason, string detail)`

**`RenewalOutcome`**
- `Renewed(AccessToken)`
- `SessionExpired`
- `NoSession`
- `Unavailable(RenewalUnavailableReason, string detail)`

`RenewalUnavailableReason`: `Transport`, `ServerStatus`, `OutOfContract`.

**`AccessTokenOutcome`** (porta `IAccessTokenSource`)
- `Valid(AccessToken)`
- `SessionUnavailable(SessionUnavailableKind)`, com `Expired` ou `NoSession`
- `Unavailable(RenewalUnavailableReason, detail)`

**`RegistrationForm`**: `string Username`, `string Email`, `Password Password`,
`Password Confirmation`. `Password.ToString()` → `password=<redacted>`.

**`RegistrationOutcome`**: `AccountCallOutcome<AccountCreated, RegistrationRefusal>`.

**`RegistrationRefusal`**
- `IReadOnlyList<RegistrationFieldError> Fields`, na ordem do corpo.
- `string? UnrecognizedBody`, quando o 400 não é um dicionário de listas.

**`RegistrationFieldError`**
- `RegistrationField Field`: `Username`, `Email`, `Password`,
  `PasswordConfirmation` ou `Other`.
- `string FieldName`, o nome como veio.
- `IReadOnlyList<string> Messages`.

---

## Chamada autenticada (`Anathema.Net.Account/Http`)

**`AuthenticatedRequest`**: `string Method`, `Uri Url`, `string? JsonBody`.

**`AuthenticatedResponse`** (interno ao cliente e aos serviços): `int Status`,
`string BodyText`.

**`AccountCallFailure`**
- `SessionUnavailable(SessionUnavailableKind)`
- `TransportFailed(TransportFailure)`
- `OutOfContract(int status, DecodeFailure)`

**`AccountCallOutcome<TValue, TRefusal>`**: exatamente um dos três preenchido.
- `bool IsSuccess`
- `TValue Value`
- `TRefusal? Refusal`
- `AccountCallFailure? Failure`

Acesso a `Value` fora do sucesso lança `InvalidOperationException` com o estado
real.

**`UnrecognizedRefusal`**: `int Status`, `string BodyText` (cortado em 500
caracteres). É o `TRefusal` de quem não tem recusa tipada.

---

## Perfil (`Anathema.Net.Account/Profile`)

**`OwnProfile`**: `string Nickname`, `string Icon`, `long Level`,
`long ExperiencePoints`, `long Coins`, `long Credits`.

**`ProfileRefusal`**: `ProfileMissing` | `UnrecognizedRefusal`.

---

## Catálogo (`Anathema.Net.Account/Catalog`)

**`CatalogCard`** (abstrata, construtor `private protected`): `CardId Card`,
`string Name`, `long Energy`, `string ImageKey`.

- **`UnitCard`**: `long Attack`, `long Health`.
- **`SpellCard`**: `string Description`, `SpellEffect Effect`.

**`SpellEffect`**
- `bool RequiresTarget`
- `SpellTargetKind TargetKind`: `None`, `AlliedUnit`, `EnemyUnit` ou `Unknown`
- `string TargetKindText`
- `SpellDuration Duration`: `Permanent`, `UntilEndOfRound` ou `Unknown`
- `string DurationText`
- `bool DeclarationOnly`

**`LoadedCatalog`**
- `IReadOnlyList<CatalogCard> Cards`, na ordem do servidor.
- `CardLookup Find(CardId)`.
- `int Generation`, a geração da sessão em que foi carregado.

**`CardLookup`**
- `CardLookupKind Kind`: `Unit`, `Spell` ou `NotFound`.
- `UnitCard? Unit`, `SpellCard? Spell`.
- `CardId Requested`.

**`CardCatalog`** (estado mutável)
- `Task<AccountCallOutcome<LoadedCatalog, UnrecognizedRefusal>> LoadAsync()`.
- Guarda o último `LoadedCatalog` válido da geração atual e compartilha a busca
  em curso.

---

## Decks (`Anathema.Net.Account/Decks`)

**`PlayerDeck`**: `DeckId Deck`, `string Name`, `IReadOnlyList<CardId> Cards`.

**`DeckDraft`**: `string Name`, `IReadOnlyList<CardId> Cards`, para criar.

**`DeckChange`**: `string? Name`, `IReadOnlyList<CardId>? Cards`, com ao menos um
dos dois.

**`DeckRefusal`**: `IReadOnlyList<DeckRefusalReason> Reasons`, nunca vazia.

**`DeckRefusalReason`** (hierarquia fechada)

| Tipo | Campos |
|---|---|
| `InvalidDeckName` | `IReadOnlyList<string> Messages` |
| `DeckListRejected` | `IReadOnlyList<DeckProblem> Problems` |
| `MissingDeckField` | `string Detail` |
| `DeckLimitReached` | `IReadOnlyList<string> Messages` |
| `DeckNotFound` | — |
| `UnrecognizedDeckRefusal` | `int Status`, `string BodyText` |

**`DeckDeleted`**: marcador de sucesso do `DELETE`.

---

## Histórico (`Anathema.Net.Account/History`)

**`HistoryPageRequest`**: `int Page` ≥ 1, `int PageSize` ≥ 1. O padrão é página 1
com 20 itens.

**`MatchHistoryPage`**
- `long Count`
- `bool HasNext`, `bool HasPrevious`
- `IReadOnlyList<MatchHistoryRow> Rows`

**`MatchHistoryRow`**
- `MatchId Match`
- `bool Won`
- `MatchEndReason EndReason`: `NexusDepleted`, `Forfeit` ou `Unknown`
- `string EndReasonText`
- `HistoryOpponent? Opponent`
- `long DurationSeconds`
- `long FinalRound`
- `string EndedAtText`

**`HistoryOpponent`**: `UserId User`, `string Nickname`, `string Icon`,
`long Level`.

**`HistoryRefusal`**: `HistoryPastTheEnd` | `HistoryNoProfile` |
`UnrecognizedRefusal`.

---

## Rotas (`Anathema.Net.Account/AccountRoutes`)

**`AccountRoutes`**
- `Uri Register`, `Uri Login`, `Uri Refresh`, `Uri OwnProfile`, `Uri Cards`,
  `Uri Decks`, `Uri Matches`.
- `Uri Deck(DeckId)` → `Decks` + `"{deck_id}/"`.
- `Uri MatchesPage(HistoryPageRequest)`.

Todas as URLs são absolutas, e as de coleção terminam em `/`, como o Django exige.

**`AppConfig`** (evolui no lugar)

| Campo | Dev | Prod |
|---|---|---|
| `registerEndpoint` (novo) | `/accounts/register/` | vazio, como os demais |
| `cardsEndpoint` (novo) | `/game/cards/` | vazio |
| `decksEndpoint` (novo) | `/players/decks/` | vazio |
| `matchesEndpoint` (novo) | `/game/matches/` | vazio |
| `loginEndpoint`, `playerMe`, `tokenRefreshEndpoint` | mantidos | mantidos |

Membro novo: `AccountRoutes BuildAccountRoutes()`. Rota vazia lança com o nome do
campo e o nome do asset.

---

## Composição (`Anathema.Net.Unity/LiveAccountServices`)

Monta sobre `LiveNetworkAdapters`, `AccountRoutes`, `IRefreshTokenVault` e
`AccountTiming`:

- `AccountSession Session`
- `IAccessTokenSource Tokens`
- `AccountRegistration Registration`
- `OwnProfileQuery Profile`
- `CardCatalog Catalog`
- `PlayerDecks Decks`
- `MatchHistory History`

Assina o `IAppLifecycle` com `ForegroundRenewal`.
