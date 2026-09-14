# Contrato: perfil, catálogo, decks e histórico

**Feature**: `002-player-account` | Assembly: `Anathema.Net.Account` (e `DeckProblem` em `Anathema.Net.Core`)

As formas HTTP **não estão aqui**. São as dos contratos do backend:

- catálogo: `C:/Users/gabri/Projetos/dev_container/anathema/backend/specs/011-deck-catalog-api/contracts/http_catalog.md`
- decks: `C:/Users/gabri/Projetos/dev_container/anathema/backend/specs/011-deck-catalog-api/contracts/http_decks.md`
- histórico: `C:/Users/gabri/Projetos/dev_container/anathema/backend/specs/012-match-result-history/contracts/http_match_history.md`
- perfil (`GET /players/me/`): sem contrato escrito; forma na descrição da feature

Este arquivo diz o que o cliente devolve para cada resposta. Todo serviço usa
`AuthenticatedHttpClient` e devolve `AccountCallOutcome<TValue, TRefusal>`. As
falhas comuns (`SessionUnavailable`, `TransportFailed`, `OutOfContract`) valem
para todos e não são repetidas nas tabelas.

---

## `OwnProfileQuery`

```csharp
public Task<AccountCallOutcome<OwnProfile, ProfileRefusal>> ReadAsync();
```

| Resposta | Resultado |
|---|---|
| 200 | `OwnProfile` |
| 404 | `ProfileMissing` |
| outro | `UnrecognizedRefusal` |

---

## `CardCatalog`

```csharp
public Task<AccountCallOutcome<LoadedCatalog, UnrecognizedRefusal>> LoadAsync();
```

```csharp
public sealed class LoadedCatalog
{
    public IReadOnlyList<CatalogCard> Cards { get; }
    public CardLookup Find(CardId card);
}
```

| Garantia | Requisito |
|---|---|
| Na geração atual, a segunda `LoadAsync` devolve o guardado, sem pedido | FR-028 |
| Chamadas durante a busca compartilham o mesmo pedido | FR-028 |
| Falha não é guardada: a próxima `LoadAsync` pede de novo | FR-028 |
| `Generation` da sessão mudou → o guardado é descartado | FR-028 |
| `Find` devolve `Unit`, `Spell` ou `NotFound` com o `CardId` pedido, sem exceção | FR-029 |
| `unit` sem `description`/`effect` e `spell` sem `attack`/`health`: nenhum dos dois é exigido | contrato 011 |
| Valores de `target_kind`/`duration` fora do conjunto → `Unknown` + texto + log | FR-030 |
| Carta com `card_type` desconhecido, campo ruim ou `card_id` repetido → omitida + log | FR-031 |
| `cards` ausente ou que não é lista → `OutOfContract` | edge cases |

Eventos de log: `catalog_card_skipped` (`card_id`, `reason`, `field` ou
`card_type`), `catalog_card_duplicated` (`card_id`) e
`catalog_effect_value_unknown` (`card_id`, `field`, `value`).

---

## `PlayerDecks`

```csharp
public Task<AccountCallOutcome<IReadOnlyList<PlayerDeck>, DeckRefusal>> ListAsync();
public Task<AccountCallOutcome<PlayerDeck, DeckRefusal>> ReadAsync(DeckId deck);
public Task<AccountCallOutcome<PlayerDeck, DeckRefusal>> CreateAsync(DeckDraft draft);
public Task<AccountCallOutcome<PlayerDeck, DeckRefusal>> ChangeAsync(DeckId deck, DeckChange change);
public Task<AccountCallOutcome<DeckDeleted, DeckRefusal>> DeleteAsync(DeckId deck);
```

| Operação | Sucesso | 400 | 404 |
|---|---|---|---|
| `ListAsync` | 200 (lista vazia é sucesso) | não reconhecida | não reconhecida |
| `ReadAsync` | 200 | não reconhecida | `DeckNotFound` |
| `CreateAsync` | 201 | razões por chave (abaixo) | não reconhecida |
| `ChangeAsync` | 200; corpo só com os campos presentes em `DeckChange` | razões por chave | `DeckNotFound` |
| `DeleteAsync` | 204, corpo vazio | não reconhecida | `DeckNotFound` |

Razões num 400, juntas e na ordem da tabela:

| Chave do corpo | Razão |
|---|---|
| `name` | `InvalidDeckName(messages)` |
| `deck_problems` | `DeckListRejected(problems)` |
| `deck_limit` | `DeckLimitReached(messages)` |
| `detail` | `MissingDeckField(detail)` |
| nenhuma delas, ou o corpo não é JSON | `UnrecognizedDeckRefusal(status, corpo)` |

Qualquer outro status → `UnrecognizedDeckRefusal`. O cliente não valida a lista
antes de enviar (FR-036).

## `DeckProblemUnion` (`Anathema.Net.Core`)

```csharp
public static class DeckProblemUnion
{
    public static DiscriminatedUnion<DeckProblem> Create();
}
```

- Lê um item de `deck_problems`: `kind` e `message` obrigatórios, mais os campos
  do braço (data-model).
- `kind` desconhecido → `UnrecognizedDeckProblem(kind, message)`.
- Não referencia nada de HTTP nem de `Anathema.Net.Account`. A feature 3 a usa na
  recusa `invalid_deck` de `matchmaking_messages.md`.

---

## `MatchHistory`

```csharp
public Task<AccountCallOutcome<MatchHistoryPage, HistoryRefusal>> ReadPageAsync(HistoryPageRequest request);
```

| Resposta | Resultado |
|---|---|
| 200 | `MatchHistoryPage` (`HasNext`/`HasPrevious` = campo não nulo) |
| 404 com `Page > 1` | `HistoryPastTheEnd` |
| 404 com `Page == 1` | `HistoryNoProfile` |
| outro | `UnrecognizedRefusal` |

- `opponent: null` → `Opponent == null`, linha mantida.
- `end_reason` fora do conjunto → `Unknown` + texto + `history_end_reason_unknown`.
- `ended_at` → `EndedAtText`, só para exibir.

---

## Testes obrigatórios

`Anathema.Net.Account.Tests`, com transporte fake e o codec real:

1. Perfil: 200, 404 e 500.
2. Catálogo: 1 unidade e 1 feitiço corretos; `Find` de cada um e de um
   inexistente.
3. Catálogo: feitiço com `target_kind: "any_unit"`, unidade sem `health`, carta
   com `card_type: "relic"` e `card_id` repetido. As demais cartas continuam e o
   log tem uma entrada por caso.
4. Catálogo: duas `LoadAsync` concorrentes → 1 pedido; falha e depois sucesso → 2
   pedidos; `SignOut` e novo login → novo pedido.
5. Decks: cada linha das duas tabelas, incluindo um corpo com `name` e
   `deck_problems` juntos e um 502 com HTML.
6. `DeckChange` sem nome e sem lista lança; só com nome envia só `name`.
7. Histórico: página com oponente nulo, `end_reason` desconhecido, `count: 0` e
   404 na página 1 e na 3.

`Anathema.Net.Json.Tests`: `DeckProblemUnion` com os três `kind` do contrato, um
desconhecido e um sem `message`.

`Anathema.Net.Core.Tests`: `DeckId`/`CardId` (validação, igualdade, log,
isolamento de tipo).
