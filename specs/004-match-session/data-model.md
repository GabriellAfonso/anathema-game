# Data Model: Partida

**Feature**: `004-match-session` | **Date**: 2026-09-14

Tipos novos ou que mudam. Todo arquivo novo com `#nullable enable`; nenhum membro
chamado `id`. Formas das mensagens: contratos do backend citados na spec. Todos
imutáveis, salvo onde diz "estado".

---

## Núcleo (`Anathema.Net.Core`)

**`PayloadIdentityReading`** (evolui)
- `+ IReadOnlyList<CardInstanceId> ReadCardInstanceIdList(string field)`: falha com o
  caminho do item (`attacker_card_instance_ids[2]`).

**`PayloadIdentityWriting`** (evolui)
- `+ void WriteCardInstanceIdList(string field, IReadOnlyList<CardInstanceId> cards)`.

---

## Conta (`Anathema.Net.Account`)

- `MatchEndReasonText` (público, novo): `static MatchEndReason Parse(string)`.
  Extraído de `MatchHistoryRow`, que passa a usá-lo.
- `SpellDurationText` (público, novo): `static SpellDuration Parse(string)`. Extraído
  de `SpellEffect`, que passa a usá-lo.
- `AccountAssemblyInfo`: `+ InternalsVisibleTo("Anathema.Net.Match.Tests")`
  (research R13).

---

## Protocolo (`Anathema.Net.Match/Protocol`)

**`MatchPhase`**: `Mulligan`, `Upkeep`, `Action`, `Declaration`, `Combat`,
`RoundEnd`, `Finished`, `Unknown`.

**`MatchProfile`**: `UserId User`, `string Nickname`, `string Icon`, `long Level`.

**`MatchCard`**: `CardInstanceId Instance`, `CardId Card`.

**`UnitModifier`** (abstrato): `string KindText`, `SpellDuration Duration`,
`string DurationText`.
- `AttackModifier`: `long Amount`
- `HealthModifier`: `long Amount`
- `DamageImmunityModifier`: sem quantidade
- `UnrecognizedModifier`: só o que é comum

**`BankUnit`**: `MatchCard Card`, `long DamageTaken`,
`IReadOnlyList<UnitModifier> Modifiers`.

**`SideView`** (abstrato)
- `MatchProfile Profile`
- `long Nexus`, `long EnergyCurrent`
- `IReadOnlyList<BankUnit> Bank`
- `IReadOnlyList<MatchCard> Graveyard`
- `long DeckSize`
- `bool MulliganTaken`

Derivados:
- `OwnSideView : SideView`: `+ IReadOnlyList<MatchCard> Hand`.
- `OpponentSideView : SideView`: `+ long HandSize`.

**`BlockPair`**: `CardInstanceId Blocker`, `CardInstanceId Attacker`.

**`CombatView`**: `IReadOnlyList<CardInstanceId> Attackers`,
`IReadOnlyList<BlockPair> Blocks`.

**`MatchOutcome`**: `UserId DefeatedUser`, `MatchEndReason Reason`,
`string ReasonText`.

**`PlayerView`**
- `MatchId Match`
- `long RoundNumber`
- `MatchPhase Phase`, `string PhaseText`
- `UserId? PriorityUser`, `UserId? TokenHolder` — nulos no mulligan
- `bool TokenConsumed`, `long ConsecutivePasses`
- `MatchOutcome? Outcome`
- `CombatView? Combat`
- `OwnSideView You`, `OpponentSideView Opponent`

**`TurnView`**: `long TurnNumber`, `UserId Holder`, `long RemainingMs`,
`bool Warning`.

**`ClockView`**: `TurnView? Turn`, `long? MulliganRemainingMs`;
`static ClockView Empty` (frame sem `clock`).

**`EventDetail`**: `string Field` e um só destes preenchido:
- `UserId? User`
- `MatchCard? Card`
- `CardInstanceId? Instance`
- `IReadOnlyList<CardInstanceId>? Instances`
- `IReadOnlyList<MatchCard>? Cards`
- `long? Number`
- `string? Text`

**`MatchEvent`** (abstrato): `string KindText`, `IReadOnlyList<EventDetail> Details`.

| Tipo | `kind` | Propriedades |
|---|---|---|
| `MulliganTakenEvent` | `mulligan_taken` | `UserId User`, `long SwappedCount` |
| `UnitPlayedEvent` | `unit_played` | `UserId User`, `MatchCard Card` |
| `SpellCastEvent` | `spell_cast` | `UserId User`, `MatchCard Card`, `CardInstanceId? Target` |
| `PassedEvent` | `passed` | `UserId User` |
| `AttackersSentEvent` | `attackers_sent` | `UserId User`, `IReadOnlyList<CardInstanceId> Attackers` |
| `AttackerWithdrawnEvent` | `attacker_withdrawn` | `UserId User`, `CardInstanceId Attacker` |
| `AttackConfirmedEvent` | `attack_confirmed` | `UserId User` |
| `BlockerAssignedEvent` | `blocker_assigned` | `UserId User`, `CardInstanceId Blocker`, `CardInstanceId Attacker` |
| `BlockerRemovedEvent` | `blocker_removed` | `UserId User`, `CardInstanceId Blocker` |
| `DefenseEndedEvent` | `defense_ended` | `UserId User` |
| `ForfeitedEvent` | `forfeited` | `UserId User` |
| `UnitDamagedEvent` | `unit_damaged` | `CardInstanceId Unit`, `long Amount` |
| `UnitDiedEvent` | `unit_died` | `UserId Owner`, `MatchCard Card` |
| `NexusChangedEvent` | `nexus_changed` | `UserId User`, `long Amount` |
| `RoundStartedEvent` | `round_started` | `long RoundNumber`, `UserId TokenHolder` |
| `CardsDrawnEvent` | `cards_drawn` | `UserId User`, `long Count`, `IReadOnlyList<MatchCard> Cards` (vazia para o oponente) |
| `MatchFinishedEvent` | `match_finished` | `MatchOutcome Outcome` |
| `TurnTimedOutEvent` | `turn_timed_out` | `UserId User`, `long TurnNumber` |
| `MulliganTimedOutEvent` | `mulligan_timed_out` | `UserId User` |
| `UnrecognizedMatchEvent` | qualquer outro | — |

**Frames** (`: ServerFrame`)
- `MatchStartFrame` (`match_start`): `long Version`, `PlayerView View`,
  `ClockView Clock`.
- `MatchUpdateFrame` (`match_update`): `long Version`, `PlayerView View`,
  `IReadOnlyList<MatchEvent> Events`, `ClockView Clock`.
- `TurnWarningFrame` (`turn_warning`): `long TurnNumber`, `UserId Holder`,
  `long RemainingMs`.

**`MatchFrames`**: `static DiscriminatedUnion<ServerFrame> CreateUnion()` —
`ConnectionFrames.CreateUnion()` + os três acima.

**`MatchEvents`** (interno): união por `kind`. **`UnitModifiers`** (interno): união
por `modifier_kind`.

---

## Comandos (`Anathema.Net.Match/Commands`)

**`PlayCommand : IOutgoingMessage`** (abstrato): `string MessageType`.

| Tipo | `type` | Construtor |
|---|---|---|
| `MulliganCommand` | `mulligan` | `(IReadOnlyList<CardInstanceId> swapped)` |
| `PlayUnitCommand` | `play_unit` | `(CardInstanceId card)` |
| `CastSpellCommand` | `cast_spell` | `(CardInstanceId card, CardInstanceId? target)` — sem alvo omite o campo |
| `PassCommand` | `pass` | `()` |
| `DeclareAttackCommand` | `declare_attack` | `(IReadOnlyList<CardInstanceId> attackers)` |
| `WithdrawAttackerCommand` | `withdraw_attacker` | `(CardInstanceId attacker)` |
| `ConfirmAttackCommand` | `confirm_attack` | `()` |
| `AssignBlockerCommand` | `assign_blocker` | `(CardInstanceId blocker, CardInstanceId attacker)` |
| `RemoveBlockerCommand` | `remove_blocker` | `(CardInstanceId blocker)` |
| `EndDefenseWindowCommand` | `end_defense_window` | `()` |
| `ForfeitCommand` | `forfeit` | `()` |

**`PlaySendStatus`**: `Sent`, `NotConnected`, `SocketFailed`.

**`PlaySendResult`**: `PlaySendStatus Status`, `PlayCommand Command`,
`ConnectionPhase ConnectionPhase`.

**`PendingPlay`** (estado)
- `PlayCommand? Current`
- `PlayCommand? LastSentSinceUpdate`
- `event Action<PlayCommand?> CurrentChanged`
- internos: `MarkSent(PlayCommand)`, `Unmark(PlayCommand)`, `ClearCurrent()`,
  `ClearOnUpdate()`.

**`MatchCommands`**: os 11 comandos (feitiço em dois métodos), cada um
`Task<PlaySendResult>`. Recebe só `AuthenticatedConnection`, `PendingPlay` e
`IClientLog`.

---

## Recusas (`Anathema.Net.Match/Refusals`)

**`PlayRefusalCode`**:
- forma/transporte: `MalformedMessage`, `UnknownMessageType`, `MatchNotFound`,
  `ConcurrentMatchWrite`, `InternalError`;
- motor, os 26 de `refusal_codes.md` na ordem da tabela: `NotYourPriority` …
  `MulliganAlreadyTaken`;
- `Unknown`.

**`PlayRefusal`**: `PlayRefusalCode Code`, `string CodeText`, `string Error`,
`PlayCommand? ProbableCommand`.

**`PlayRefusalReader`** (interno): `PlayRefusal Read(MessageRefusedFrame, PlayCommand?)`.

---

## Espelho (`Anathema.Net.Match/Mirror`)

**`FrameVerdict`**: `Accept`, `ResyncSameVersion`, `Discard`.

**`VersionGate`** (estado): `long? AppliedVersion`;
`FrameVerdict Judge(long version, bool isStart)`; `void Record(long version)`.

**`ViewReplaced`**: `PlayerView? Previous`, `PlayerView Current`.
**`PhaseChange`**: `MatchPhase Previous`, `MatchPhase Current`.
**`PriorityChange`**: `UserId? Previous`, `UserId? Current`.
**`MatchEnding`**: `MatchOutcome Outcome`, `bool Won`.

**`MatchMirror`** (estado)
- Estado: `PlayerView? Current`, `long? Version`, `UserId? Self`.
- Fatos, lidos do estado atual:
  - `MatchPhase? Phase`
  - `bool IsMyPriority`, `bool AmTokenHolder`
  - `bool MyMulliganPending` — fase mulligan e `You.MulliganTaken` falso
  - `bool OpponentMulliganAnswered` — `Opponent.MulliganTaken`
  - `bool IsFinished`
  - `bool? DidIWin` — nulo sem desfecho
- Eventos: `ViewReplaced`, `EventReceived(MatchEvent)`, `PhaseChanged`,
  `PriorityChanged`, `MatchEnded`.
- Interno: `Apply(PlayerView view, long version, IReadOnlyList<MatchEvent> events)`.

**`DisplayedUnitStats`**: `long Attack`, `long Health`;
`static DisplayedUnitStats? Of(BankUnit unit, LoadedCatalog catalog)`.

---

## Relógio (`Anathema.Net.Match/Clock`)

**`TurnClock`** (estado, com `IMonotonicClock`)
- `TurnView? Turn` — a vez desenhada, com o `RemainingMs` da âncora
- `TimeSpan? TurnRemaining`, `TimeSpan? MulliganRemaining` — calculados agora
- `event Action<TurnView> TurnStarted`
- `event Action<long> TurnRunningOut` — o `turn_number`
- internos: `ClockAnnouncements Anchor(ClockView view, MonotonicInstant arrival)`,
  `void NoteWarning(TurnWarningFrame warning, MonotonicInstant arrival)`.
  `ClockAnnouncements.Raise()` emite o que o `Anchor` marcou (research R4).

---

## Dicas (`Anathema.Net.Match/Hints`)

**`HintCardKind`**: `Unit`, `Spell`, `UnknownCard`, `NotInHand`.

**`HandCardHint`**
- `CardInstanceId Instance`, `CardId? Card`, `HintCardKind Kind`
- `long? Cost`, `long EnergyCurrent`
- `SpellTargetKind? Target` — só feitiço
- `IReadOnlyList<CardInstanceId> Candidates`
- `bool DeclarationOnly`
- `MatchPhase Phase`

**`HandCardHints`**: `static HandCardHint For(CardInstanceId card, PlayerView view, LoadedCatalog catalog)`.

---

## Sessão (`Anathema.Net.Match/Session`)

**`LiveMatchPhase`**: `Idle`, `Connecting`, `Live`, `Reconnecting`, `Finished`,
`Refused`, `GaveUp`.

**`LiveMatchStatus`**: `LiveMatchPhase Phase`, `bool IsStale`,
`MatchOutcome? Outcome` (em `Finished`), `GiveUpReason? GiveUp` (em `Refused` e
`GaveUp`).

**`LiveMatch`** (estado; `IDisposable`)
- Construtor: `(AuthenticatedConnection connection, Uri matchBase, MatchId match, LoadedCatalog catalog, IMonotonicClock clock, IClientLog log)`
- `MatchId Match`, `LiveMatchStatus Status`
- `MatchMirror Mirror`, `TurnClock Clock`, `MatchCommands Commands`, `PendingPlay Pending`
- `event Action<LiveMatchStatus> StatusChanged`, `event Action<PlayRefusal> Refused`
- `void Start()`
- `HandCardHint HintFor(CardInstanceId card)`
- `DisplayedUnitStats? StatsOf(BankUnit unit)`
- `void Dispose()`

**`MatchNarrator`** (interno a `LiveMatch`): `(MatchMirror, LoadedCatalog, IClientLog)`.

### Estados da sessão

```text
Idle ──Start──► Connecting ──match_start aceito──► Live
                    │                                │  ▲
                    │                  queda/renovação│  │match_start (aceito ou mesma versão)
                    │                                ▼  │
                    │                           Reconnecting
                    │                                │
  GaveUp(MatchRefused) ─► Refused ◄──────────────────┤
  GaveUp(outro)        ─► GaveUp  ◄──────────────────┘
  Live/Reconnecting ──frame aceito em finished──► Finished (connection.Leave())
```

`Refused`, `GaveUp` e `Finished` são terminais. `Dispose()` em qualquer estado sai
da conexão e cala os avisos.

---

## Borda Unity (`Anathema.Net.Unity`)

- `LiveNetworkAdapters`: codec com `MatchFrames.CreateUnion()`.
- `Anathema.Net.Unity.asmdef`: `+ Anathema.Net.Match`.

## Conexão (`Anathema.Net.Connection`)

- `AuthenticatedConnection`: `- event Action<string>? RawTextReceived`.
- `SocketAttempt.FrameArrived`: `Action<ServerFrame>` (sem o texto).

## Código anterior (`Assembly-CSharp`)

- `MatchClient`: ver [contracts/legacy-bridge.md](./contracts/legacy-bridge.md).
- `MatchSession`: `Attach(LiveMatch)`, `LiveMatch? Live`, `PlayerView? State`,
  `bool HasState`, `event Action<PlayerView> OnStateChanged`, `Clear()`.
- `PlayerSession`: compõe `MatchClient` com catálogo e relógio.
- Sai: `DTO/Match/MatchStateDTO.cs`.
