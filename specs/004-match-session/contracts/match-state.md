# Contrato: espelho, relógio, dicas e atributos exibidos

**Feature**: `004-match-session`

Peças sem socket da partida. Nenhuma recebe `AuthenticatedConnection`
(`MatchCoreIsolationTests`). Regra de versão e ordem dos avisos: research R4.

---

## `MatchMirror` (`Anathema.Net.Match/Mirror`)

```csharp
public sealed class MatchMirror
{
    public PlayerView? Current { get; }
    public long? Version { get; }
    public UserId? Self { get; }

    public MatchPhase? Phase { get; }
    public bool IsMyPriority { get; }
    public bool AmTokenHolder { get; }
    public bool MyMulliganPending { get; }
    public bool OpponentMulliganAnswered { get; }
    public bool IsFinished { get; }
    public bool? DidIWin { get; }

    public event Action<ViewReplaced>? ViewReplaced;
    public event Action<MatchEvent>? EventReceived;
    public event Action<PhaseChange>? PhaseChanged;
    public event Action<PriorityChange>? PriorityChanged;
    public event Action<MatchEnding>? MatchEnded;

    internal MatchMirror(IClientLog log);
    internal void Apply(PlayerView view, long version, IReadOnlyList<MatchEvent> events);
}
```

| Garantia | Requisito |
|---|---|
| `Apply` substitui `Current` e `Version` inteiros antes de qualquer aviso | FR-011, FR-015 |
| `Self` = `view.You.Profile.User`; mudança entre frames → `match_self_changed` (erro), fica o novo | FR-012 |
| Ordem: `ViewReplaced` → cada `EventReceived` na ordem da lista → `PhaseChanged` (se mudou) → `PriorityChanged` (se mudou) → `MatchEnded` (primeira fase `Finished` aceita, uma vez) | FR-014 |
| Aviso que lança → `match_subscriber_failed` (`notice`, `exception`); os seguintes saem | FR-015 |
| `IsMyPriority`/`AmTokenHolder` falsos com `PriorityUser`/`TokenHolder` nulos | FR-013 |
| `MyMulliganPending` só em fase `Mulligan` com `You.MulliganTaken` falso | FR-013 |
| `DidIWin` nulo sem `Outcome`; senão `DefeatedUser != Self` | FR-013, spec Assumptions |
| Nada no espelho escolhe, bloqueia ou valida jogada | FR-047 |

## `VersionGate`

Tabela de vereditos: research R4. `Judge` não muda nada; `Record` só é chamado
depois de `Accept`.

---

## `TurnClock` (`Anathema.Net.Match/Clock`)

```csharp
public sealed class TurnClock
{
    public TurnView? Turn { get; }
    public TimeSpan? TurnRemaining { get; }
    public TimeSpan? MulliganRemaining { get; }

    public event Action<TurnView>? TurnStarted;
    public event Action<long>? TurnRunningOut;

    internal TurnClock(IMonotonicClock clock);
    internal ClockAnnouncements Anchor(ClockView view, MonotonicInstant arrival);
    internal void NoteWarning(TurnWarningFrame warning, MonotonicInstant arrival);
}
```

| Garantia | Requisito |
|---|---|
| Restante = `remaining_ms − (Now − âncora)`, preso em zero; nulo sem relógio | FR-017, SC-006 |
| `turn_number` diferente do desenhado → `TurnStarted`; igual → só reancora | FR-018 |
| `TurnRunningOut` no máximo uma vez por `turn_number`, vindo de `warning` do frame ou de `turn_warning` | FR-019 |
| `turn_warning` de outro `turn_number` ou sem vez desenhada → `turn_warning_ignored` (debug), nada muda | FR-019 |
| Prazo de mulligan nulo → `MulliganRemaining` nulo | FR-020 |
| Nunca chamado para frame descartado; chamado para `match_start` de mesma versão | FR-021 |
| Nenhum temporizador; zero não emite nada | FR-022 |

---

## `HandCardHints` (`Anathema.Net.Match/Hints`)

```csharp
public static class HandCardHints
{
    public static HandCardHint For(CardInstanceId card, PlayerView view, LoadedCatalog catalog);
}
```

| Caso | `Kind` | Outros campos |
|---|---|---|
| Cópia fora de `view.You.Hand` | `NotInHand` | `Card` nulo, `Cost` nulo, candidatos vazios |
| `CardId` sem entrada no catálogo | `UnknownCard` | `Card`, log `hint_card_unknown` |
| Unidade | `Unit` | `Cost`, `EnergyCurrent`, `Target` nulo, candidatos vazios |
| Feitiço `none` | `Spell` | `Target = None`, candidatos vazios |
| Feitiço `allied_unit` | `Spell` | candidatos = `You.Bank`, na ordem |
| Feitiço `enemy_unit` | `Spell` | candidatos = `Opponent.Bank`, na ordem |
| Feitiço `Unknown` | `Spell` | candidatos vazios |
| Qualquer feitiço | `Spell` | `DeclarationOnly` do catálogo, `Phase` atual |

Nunca lança por estado de partida; nunca é lido por `MatchCommands` (FR-030,
`CommandsIgnoreHintsTests`).

---

## `DisplayedUnitStats`

`Of(unit, catalog)`: `Attack = UnitCard.Attack + Σ AttackModifier.Amount`;
`Health = UnitCard.Health + Σ HealthModifier.Amount − DamageTaken`. Nulo se o
`CardId` não é unidade no catálogo. Documentado como conveniência de exibição
(FR-016).

---

## Testes (`Anathema.Net.Match.Tests`)

| Arquivo | Cobre |
|---|---|
| `Mirror/VersionGateTests` | todas as linhas da tabela de R4 |
| `Mirror/MatchMirrorApplyTests` | US2-1, US2-2, substituição sem mescla |
| `Mirror/MatchMirrorAnnouncementTests` | US2-5, US2-7, assinante que lança |
| `Mirror/MatchMirrorFactsTests` | US2-6, fatos com nulos, `DidIWin` |
| `Mirror/DisplayedUnitStatsTests` | US2-8, imunidade não altera número, carta não unidade |
| `Clock/TurnClockTests` | US3-1, US3-2, US3-7 |
| `Clock/TurnWarningTests` | US3-3, `warning` do frame, aviso único por vez |
| `Clock/MulliganClockTests` | US3-4 |
| `Hints/HandCardHintsTests` | US5-1 a US5-6, `NotInHand` |
| `MatchCoreIsolationTests` | nenhum construtor de espelho, relógio, dicas ou leitores recebe tipo de `Anathema.Net.Connection` |
| `CommandsIgnoreHintsTests` | `MatchCommands` não tem campo de `MatchMirror`, `LoadedCatalog`, `HandCardHint` nem `DisplayedUnitStats` (SC-008) |

Os cenários com frame descartado e reconexão (US3-5, US3-6, US2-3, US2-4) são
provados pela sessão: [live-match.md](./live-match.md).
