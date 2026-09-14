# Contrato: fila

**Feature**: `003-authenticated-socket-queue`

Mensagens e recusas do socket de fila:
`C:/Users/gabri/Projetos/dev_container/anathema/backend/specs/011-deck-catalog-api/contracts/matchmaking_messages.md`.
Não copiadas aqui.

---

## `MatchQueue` (`Anathema.Net.Connection`)

```csharp
public sealed class MatchQueue : IDisposable
{
    public MatchQueue(AuthenticatedConnection connection, ConnectionTarget target, IClientLog log);

    public QueuePhase Phase { get; }
    public DeckId? SearchDeck { get; }

    public event Action<QueuePhase>? PhaseChanged;
    public event Action<MatchPairing>? Paired;
    public event Action<QueueRefusal>? Refused;
    public event Action<string>? MatchmakingFailed;
    public event Action<GiveUpReason>? LeftQueue;

    public JoinOutcome Join(DeckId deck);
    public void Leave();
    public void Dispose();
}
```

| Garantia | Requisito |
|---|---|
| `Join` fora da fila → `Started`, fase `Connecting`; conexão aberta ou abrindo com o alvo de fila | FR-027 |
| A cada `Connected` com deck guardado, exatamente um `join_queue` com o `deck_id`; `Sent` → `Searching` | FR-027, FR-034 |
| `Join` em `Connecting` ou `Searching` → `AlreadyQueued`, nada enviado | FR-028 |
| `Leave` → `connection.Leave()`, fase `OutOfQueue`, sem `LeftQueue` nem `Refused` | FR-029 |
| `match_found` → `Paired` com `MatchPairing` tipado, fase `Paired`, `connection.Leave()` | FR-030 |
| `message_refused` → `Refused(QueueRefusal)` por `code`, fase `OutOfQueue`, socket aberto | FR-031, FR-033 |
| `matchmaking_failed` → `MatchmakingFailed(texto)`, fase `OutOfQueue`, socket aberto | FR-032, FR-033 |
| Queda, renovação ou suspensão com deck guardado → fase `Connecting`; reabertura reenvia | FR-034 |
| Conexão `GaveUp` com deck guardado → `LeftQueue(motivo)`, fase `OutOfQueue` | FR-035 |
| Queda sem deck guardado → `connection.Leave()`, sem reconexão | FR-036 |
| Frames depois de `Paired` ou `Leave` → descartados | Edge cases |
| `match_found` que não decodifica → log, fase inalterada | Edge cases |
| Reenvio recusado → `Refused`, fase `OutOfQueue`; não conta tentativa | Edge cases |

Transições completas: research R10 e data-model.

---

## Mensagem e frames

| Tipo | Leitura / escrita |
|---|---|
| `JoinQueueMessage` | `type` `join_queue`; `deck_id` inteiro por `WriteDeckId` |
| `MatchFoundFrame` | `match_id` por `ReadMatchId`; `self` e `opponent` com `user_id`, `nickname`, `icon`, `level` |
| `MatchmakingFailedFrame` | `error` texto |
| `ConnectionFrames.CreateUnion()` | genéricos da 001 + `match_denied` + os dois acima |

Campo obrigatório ausente ou de tipo errado → decodificação inválida com o
caminho (infraestrutura da 001).

---

## `QueueRefusal`

| `code` recebido | `QueueRefusalKind` | Campos extras |
|---|---|---|
| `deck_not_specified` | `DeckNotSpecified` | — |
| `deck_not_found` | `DeckNotFound` | `Deck` (`deck_id` ecoado) |
| `invalid_deck` | `InvalidDeck` | `Problems` pela `DeckProblemUnion`, na ordem; `Deck` se vier |
| `malformed_message` | `MalformedMessage` | — |
| `unknown_message_type` | `UnknownMessageType` | — |
| qualquer outro | `Unrecognized` | — |
| conhecido com forma quebrada | `Unrecognized` + log `queue_refusal_out_of_contract` | — |

`Code` sempre traz o texto recebido; `Error` só serve para leitura humana. Nenhum
ramo compara `Error`.

---

## Limitações do backend

Queda entre o pareamento e o `match_found`, e saída tardia do socket antigo:
research R11. Sem contorno no cliente.

---

## Testes (`Anathema.Net.Connection.Tests`)

| Arquivo | Cobre |
|---|---|
| `JoinQueueMessageTests` | payload escrito |
| `MatchFoundFrameTests` | leitura tipada, campo ausente, `user_id` em texto |
| `MatchmakingFailedFrameTests` | leitura |
| `QueueRefusalReaderTests` | cada linha da tabela, `invalid_deck` com dois problemas, forma quebrada |
| `MatchQueueJoinTests` | US2-1, US2-2, `AlreadyQueued`, envio `NotOpen` |
| `MatchQueueRefusalTests` | US2-3 a US2-6 |
| `MatchQueueReconnectTests` | US2-7, US2-8, reenvio recusado, queda ociosa |
| `MatchQueueLeaveTests` | US2-9, frames depois de sair e de parear |

LiveServer (`Anathema.Net.Unity.Tests/LiveServer/LiveQueueTests`, research R14):

| Caso | SC-007 |
|---|---|
| `DuasContasNovasSaoPareadasNaMesmaPartida` | pareamento, mesmo `MatchId` |
| `DeckInexistenteETextoSaoRecusadosESocketContinua` | `deck_not_found`, `deck_not_specified`, `join_queue` válido depois |
| `TrocaDeRedeProcurandoVoltaParaAFilaEPareia` | reentrada automática e pareamento |
| `SocketDePartidaComMatchIdRecebidoRecebeMatchStart` | `match_start` |
| `SocketDePartidaComMatchIdInventadoDesisteCom4404` | `match_denied` + 4404 |
| `TokenInvalidoRenovaEConecta` | renovação e prova |
