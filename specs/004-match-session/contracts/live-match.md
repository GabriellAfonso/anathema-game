# Contrato: sessão de partida, comandos, recusas e narrador

**Feature**: `004-match-session`

---

## `LiveMatch` (`Anathema.Net.Match/Session`)

```csharp
public sealed class LiveMatch : IDisposable
{
    public LiveMatch(AuthenticatedConnection connection, Uri matchBase, MatchId match,
        LoadedCatalog catalog, IMonotonicClock clock, IClientLog log);

    public MatchId Match { get; }
    public LiveMatchStatus Status { get; }
    public MatchMirror Mirror { get; }
    public TurnClock Clock { get; }
    public MatchCommands Commands { get; }
    public PendingPlay Pending { get; }

    public event Action<LiveMatchStatus>? StatusChanged;
    public event Action<PlayRefusal>? Refused;

    public void Start();
    public HandCardHint HintFor(CardInstanceId card);
    public DisplayedUnitStats? StatsOf(BankUnit unit);
    public void Dispose();
}
```

| Garantia | Requisito |
|---|---|
| `Start` → `connection.Connect(ConnectionTarget.Match(matchBase, match))`, estado `Connecting`; segundo `Start` lança | FR-031 |
| Frame de partida: instante lido em `clock.Now` na entrega, antes de tudo | FR-017, research R5 |
| Veredito e ordem de aplicação | research R4 |
| Transições de estado | research R9, data-model |
| `Reconnecting` mantém `Mirror.Current` e marca `IsStale` | FR-032, FR-033 |
| `match_start` depois de `Reconnecting` (aceito ou mesma versão) → `Live`, `IsStale` falso | FR-033 |
| Fase `finished` aceita → avisos do espelho → `Finished` com desfecho → `connection.Leave()`; mudança de conexão seguinte ignorada | FR-034 |
| `Dispose` → `connection.Leave()`, cancela assinaturas; nenhum aviso depois | FR-035 |
| `message_refused` → `PlayRefusal` com `Pending.LastSentSinceUpdate`, `Refused`, `Pending.ClearCurrent()`; espelho, relógio e estado inalterados | FR-026, FR-027 |
| `turn_warning` → `Clock.NoteWarning` | FR-019 |
| `Recovered` → `Pending.ClearCurrent()` | FR-025 |
| Cada mudança de estado e cada recusa registradas | FR-037 |
| Terminal (`Refused`, `GaveUp`, `Finished`) ignora frames seguintes | research R9 |

---

## `MatchCommands` (`Anathema.Net.Match/Commands`)

```csharp
public sealed class MatchCommands
{
    public Task<PlaySendResult> Mulligan(IReadOnlyList<CardInstanceId> swapped);
    public Task<PlaySendResult> PlayUnit(CardInstanceId card);
    public Task<PlaySendResult> CastSpell(CardInstanceId card);
    public Task<PlaySendResult> CastSpellAt(CardInstanceId card, CardInstanceId target);
    public Task<PlaySendResult> Pass();
    public Task<PlaySendResult> DeclareAttack(IReadOnlyList<CardInstanceId> attackers);
    public Task<PlaySendResult> WithdrawAttacker(CardInstanceId attacker);
    public Task<PlaySendResult> ConfirmAttack();
    public Task<PlaySendResult> AssignBlocker(CardInstanceId blocker, CardInstanceId attacker);
    public Task<PlaySendResult> RemoveBlocker(CardInstanceId blocker);
    public Task<PlaySendResult> EndDefenseWindow();
    public Task<PlaySendResult> Forfeit();
    public Task<PlaySendResult> Send(PlayCommand command);

    internal MatchCommands(AuthenticatedConnection connection, PendingPlay pending, IClientLog log);
}
```

| Garantia | Requisito |
|---|---|
| Fase da conexão ≠ `Connected` → `NotConnected` com a fase; nada enviado, nada pendente, nada guardado para depois | FR-024, SC-007 |
| `Connected` → `Pending.MarkSent`, `SendAsync`; `Sent` → `Sent` | FR-024, FR-025 |
| `SocketSendOutcome` ≠ `Sent` → `SocketFailed`, `Pending.Unmark` | FR-024 |
| Pendente nunca bloqueia; novo envio substitui `Current` e `LastSentSinceUpdate` | FR-025 |
| JSON de cada comando: [protocol-shapes.md](./protocol-shapes.md) | FR-008 |
| Sem acesso a espelho, catálogo ou dicas | FR-030 |

`Send(PlayCommand)` existe para o bot do marco mandar o candidato escolhido sem um
`switch` por tipo.

---

## `PendingPlay`

| Momento | `Current` | `LastSentSinceUpdate` |
|---|---|---|
| comando enviado | o comando | o comando |
| envio falhou no socket | nulo, se era ele | anterior |
| atualização aceita (`Accept`) | nulo | nulo |
| recusa | nulo | inalterado (usado na recusa) |
| `Recovered` | nulo | inalterado |

`CurrentChanged` avisa cada mudança de `Current`.

---

## `MatchNarrator`

Assina `Mirror.EventReceived`. Uma entrada `IClientLog.Info("match_event", …)`
por evento:

| Campo | Valor |
|---|---|
| `match_id` | `MatchId` |
| `round` | `round_number` do estado já substituído |
| `kind` | texto do `kind` |
| um por `EventDetail` | nome do campo do contrato; `UserId` → apelido do perfil; carta → `NOME#card_instance_id`; lista → identificadores separados por vírgula; número/texto como vieram |
| `unrecognized` | `true`, só em evento desconhecido |

Sem apelido ou nome de carta conhecido: `ToString()` do identificador tipado.

---

## Eventos de log

| Evento | Nível | Campos |
|---|---|---|
| `match_status` | Info | `match_id`, `phase`, `stale`, `give_up_kind` |
| `match_frame_discarded` | Debug | `type`, `version`, `applied_version` |
| `match_frame_unexpected` | Debug | `type` |
| `match_play_refused` | Warning | `code`, `error`, `probable_command` |
| `match_command_not_sent` | Warning | `command`, `connection_phase`, `status` |
| `match_subscriber_failed` | Error | `notice`, `exception`, `message` |
| `match_self_changed` | Error | `previous`, `current` |
| `turn_warning_ignored` | Debug | `turn_number`, `drawn_turn_number` |
| `hint_card_unknown` | Warning | `card_id` |
| `match_event` | Info | tabela do narrador |

Nenhum campo leva token.

---

## Testes (`Anathema.Net.Match.Tests/Session` e `/Commands`)

`MatchTestRig`: conexão da 003 sobre fakes, codec com `MatchFrames.CreateUnion()`,
`MatchTestCatalog`, `MatchFixtures`.

| Arquivo | Cobre |
|---|---|
| `Commands/MatchCommandsTests` | US4-1 pelo socket fake, US4-2 em cada fase fora de `Connected`, `SocketFailed` |
| `Commands/PendingPlayTests` | US4-3, US4-4, tabela do pendente |
| `Session/LiveMatchConnectionTests` | US6-1, US6-2 (conectando → ao vivo → queda → reconectando → ao vivo), US6-3, US6-4 |
| `Session/LiveMatchVersionTests` | US2-3, US2-4, US3-5, US3-6 pela sessão; frames fora de ordem e duplicados; buraco de versão por `turn_warning` (SC-005) |
| `Session/LiveMatchFinishTests` | US6-5, `match_start` em `finished` depois de queda, `Dispose` |
| `Session/LiveMatchRefusalTests` | US4-5, US4-6, recusa depois de `Recovered`, recusa não muda estado |
| `Session/MatchNarratorTests` | US6-6, cada campo, nome desconhecido, evento desconhecido |
| `MatchAssemblyBoundaryTests` | FR-046: sem `UnityEngine`, `UnityEditor`, `Newtonsoft`, `System.Net.WebSockets`; sem referência a `Anathema.Net.Json`/`Anathema.Net.Unity` |

---

## Marco LiveServer (`Anathema.Net.Unity.Tests/LiveServer`)

| Arquivo | Papel |
|---|---|
| `LiveMatchTests` | `[Explicit]`, `[Category("LiveServer")]`; roteiro de research R14 |
| `SmokeBot` | assina `LiveMatch`; escolhe e manda; conta recusas e envios |
| `SmokeStrategy` | ordem de candidatos do `smoke_match.py` sobre espelho e dicas |
| `RecordingWebSocketFactory` | embrulha a fábrica real e grava textos em `Logs/match-recording/` |
| `LivePlayer` (evolui) | + deck com feitiços; + `LiveMatch` sobre `MatchConnection` |

Verificações de pronto: US7-1 a US7-8, SC-009.
