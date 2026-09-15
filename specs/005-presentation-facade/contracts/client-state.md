# Contrato: estado do app, resultado, sessão e saúde

**Feature**: `005-presentation-facade`

Garantias de comportamento da `AnathemaClient`. Formas em
[../data-model.md](../data-model.md); decisões em [../research.md](../research.md).

---

## Transições

A tabela completa está em [data-model.md](../data-model.md#transições-dono-clientstages-interno).

| Garantia | Requisito | Teste |
|---|---|---|
| Composição sem sessão guardada começa em `SignedOut(Startup)` | FR-009, US1-1 | `SignInStageTests` |
| Login ou retomada levam a `SignedIn` com `Self` | FR-010, US1-2 | `SignInStageTests` |
| `Join` em `SignedIn` → `Searching`; `match_found` → `Paired` com o pareamento | FR-010, US1-3 | `QueueStageTests` |
| Primeiro `Live` → `InMatch` com a `LiveMatch` já com estado | FR-010, US1-4 | `MatchStageTests` |
| Queda e volta com `match_start` não mudam estágio nem instância | FR-012, US1-5 | `MatchStageTests` |
| Frame terminado → `MatchFinished` com o desfecho e a linha `Fetching` | FR-015, US1-6 | `MatchStageTests` |
| Primeiro frame já terminado leva de `Paired` direto a `MatchFinished` | spec, casos de borda | `MatchStageTests` |
| `ReturnToLobby` → `SignedIn`, `CurrentMatch` nulo | FR-010, US1-7 | `ReturnStageTests` |
| Operação fora do estágio: `Applied` falso, nada enviado, sem exceção | FR-008 | `StageMismatchTests` |
| Cada transição sai uma vez, na thread principal, com anterior e atual | FR-011, FR-024 | `StageChangeDeliveryTests` |

## Conta

| Garantia | Requisito | Teste |
|---|---|---|
| Cadastro não muda o estágio | US2-1 | `ClientAccountTests` |
| Login recusado mantém `SignedOut` | US2-2 | `ClientAccountTests` |
| `SignInAsync` fora de `SignedOut` → `AlreadySignedIn`, nada enviado | FR-008 | `ClientAccountTests` |
| Perfil devolve o outcome da 002 | US2-3 | `ClientAccountTests` |
| `SignOut` em qualquer estágio → `SignedOut(SignedOut)`, sem aviso de expiração | US2-5 | `SignOutStageTests` |
| Segunda fachada sobre a mesma guarda retoma com o mesmo `UserId` | US2-6 | `ClientAccountTests` |

## Sessão expirada (R7)

| Garantia | Requisito | Teste |
|---|---|---|
| Refresh recusado em cada um dos 6 estágios logados → `SignedOut(SessionExpired)` | FR-013, SC-002 | `SessionExpiryStageTests` (`[TestCase]` por nome do `ClientStage`) |
| Fila deixada, socket de partida fechado de propósito, `CurrentMatch` nulo | FR-013 | idem |
| Conexão desistindo por `SessionExpired`/`NoSession` antes do aviso da sessão dá o mesmo estado, e o aviso seguinte não publica de novo | R7 | `SessionExpiryOrderTests` |
| Catálogo/histórico em curso terminando depois não mudam nada | spec, casos de borda | `SessionExpiryStageTests` |

## Fila

| Garantia | Requisito | Teste |
|---|---|---|
| `deck_not_found` → `Queue.Refused` tipado e `SignedIn` | FR-005, US4-1 | `ClientQueueTests` |
| `Leave` em `Searching` → `SignedIn`, sem recusa | US4-2 | `ClientQueueTests` |
| Conexão de fila desiste → `Queue.Left(ConnectionGaveUp)` e `SignedIn` | US4-3 | `ClientQueueTests` |
| `matchmaking_failed` → `Queue.Left(MatchmakingFailed)` e `SignedIn` | FR-005 | `ClientQueueTests` |
| `Join` fora de `SignedIn` → `NotApplicable` com o estágio, nada enviado | US4-4 | `ClientQueueTests` |

## Partida indisponível

| Garantia | Requisito | Teste |
|---|---|---|
| Catálogo falha → `MatchUnavailable(CatalogUnavailable)`, nenhum socket aberto, log `match_catalog_unavailable` | FR-014, US5-1 | `MatchUnavailableTests` |
| `RetryMatch` → `Paired`, catálogo pedido de novo, `Start` da sessão nova | US5-2 | `MatchUnavailableTests` |
| `match_denied` → `MatchUnavailable(MatchRefused)` com o motivo | US5-3 | `MatchUnavailableTests` |
| Tentativas esgotadas → `MatchUnavailable(ConnectionGaveUp)` | FR-010 | `MatchUnavailableTests` |

## Linha do histórico (R6)

| Garantia | Requisito | Teste |
|---|---|---|
| Leituras em 0, 1, 3 e 7 s de relógio injetado, no máximo 4 | FR-016, SC-003 | `HistoryRowLookupTests` |
| Linha na 1ª e na 3ª leitura → `Resolved` com a linha, `ResultUpdated` uma vez | FR-017, US5-5 | `HistoryRowLookupTests` |
| Nunca a linha, ou falha e recusa → `Unavailable` depois da 4ª, sem exceção | FR-017, US5-6 | `HistoryRowLookupTests` |
| `ReturnToLobby`, `SignOut`, expiração e `Dispose` cancelam; nenhum aviso depois | FR-018, US5-7 | `HistoryRowLookupTests` |
| `won` diferente de `DidIWin` → `match_history_disagrees`, nada corrigido | spec, casos de borda | `HistoryRowLookupTests` |
| Oponente nulo na linha → `Resolved` com oponente ausente | spec, casos de borda | `HistoryRowLookupTests` |

## Saúde da conexão

| Garantia | Requisito | Teste |
|---|---|---|
| Queda do socket de partida → `Reconnecting(tentativa, espera)`; volta → `Recovered` | FR-007, US6-1 | `ConnectionHealthTests` |
| Suspensão sem rede em `Searching` → `Reconnecting` com a tentativa anterior | US6-2 | `ConnectionHealthTests` |
| Desistência → `GaveUp` com o texto para o jogador | US6-3 | `ConnectionHealthTests` |
| `LatencyMeasured` e `LastLatency` são da conexão ativa | US6-4 | `ConnectionHealthTests` |
| Sem conexão ativa, ou conexão que não é a ativa, nada sai | US6-5 | `ConnectionHealthTests` |

## Descarte

| Garantia | Requisito | Teste |
|---|---|---|
| Cada feed da fachada, da `LiveMatch`, do espelho, do relógio e do pendente não entrega depois de descartado | FR-020, SC-004 | `FacadeFeedDisposalTests` |
| `AnathemaClient.Dispose` fecha fila e partida, cancela, e nenhum feed entrega depois | spec, casos de borda | `FacadeDisposeTests` |

## Log

A fachada registra, com campos:
- `client_stage` (anterior, atual, motivo);
- `match_catalog_unavailable` (`match_id`, `kind`), nome mantido do `MatchClient`;
- `match_history_row` (`match_id`, `status`, `attempts`);
- `match_history_disagrees` (`match_id`, `won`, `did_i_win`);
- `feed_listener_failed` (`feed`, `exception`).
