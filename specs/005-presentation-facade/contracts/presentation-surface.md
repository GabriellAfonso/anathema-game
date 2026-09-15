# Contrato: superfície da apresentação

**Feature**: `005-presentation-facade`

Esta é a porta de entrada da parte visual. Tudo o que uma cena pode assinar e
chamar está aqui, e nada fora daqui faz parte da superfície (FR-019). A lista de
tipos é verificada por `SurfaceContractTests`. Se o teste e este documento
divergirem, os dois são corrigidos na mesma mudança.

---

## Regras de consumo

1. **Entrada**: o script de cena implementa `ISceneClientConsumer` e recebe a
   `AnathemaClient` em `BindClient`. Não procure, não guarde em campo estático e
   não crie outra.
2. **Assinatura**: toda assinatura é `feed.Subscribe(...)`, que devolve um
   `IDisposable`. Guarde-o numa `SceneSubscriptions` e chame `DisposeAll()` no
   `OnDestroy`. A superfície não tem `event` do C#.
3. **Thread**: todo aviso chega na thread principal. Não há nada a despachar.
4. **Estado antes de aviso**: no `BindClient`, leia `client.State` e desenhe. Os
   avisos só trazem o que muda depois.
5. **Cena**: não carregue cena. O `SceneRouter` segue `client.State.Stage`.
6. **Chamada fora de hora**: devolve `StageRequestResult` com `Applied` falso,
   `QueueJoinResult.NotApplicable` ou `SignInOutcomeKind.AlreadySignedIn`. Nunca
   lança exceção por estágio.
7. **Regra de jogo**: nenhuma. Dicas (`HintFor`) e atributos exibidos
   (`StatsOf`) são para desenhar. Um comando que o servidor vai recusar ainda
   pode ser mandado, e a recusa volta tipada.
8. **Recusa**: compare pelo `Code` (`PlayRefusalCode`, `QueueRefusalKind`,
   `DeckRefusalReason`), nunca pelo texto.
9. **Identidade**: jogada cita `CardInstanceId`; `CardId` só serve para
   `client.Catalog`.
10. **Log**: pelo `IClientLog` que a cena recebe do hospedeiro, com `LogField`.
    Texto simples só no que o jogador lê.

---

## `AnathemaClient`

```csharp
public sealed class AnathemaClient : IDisposable
{
    public ClientState State { get; }
    public EventFeed<ClientStageChange> StageChanged { get; }
    public EventFeed<MatchResult> ResultUpdated { get; }
    public ClientAccount Account { get; }
    public CardCatalog Catalog { get; }
    public PlayerDecks Decks { get; }
    public MatchHistory History { get; }
    public ClientQueue Queue { get; }
    public LiveMatch? CurrentMatch { get; }
    public ConnectionHealth Health { get; }
    public IClientLog Log { get; }
    public StageRequestResult RetryMatch();
    public StageRequestResult ReturnToLobby();
    public void Dispose();   // só o hospedeiro chama
}
```

| O que a tela precisa | Onde |
|---|---|
| Em que ponto o jogador está | `State.Stage`, `StageChanged` |
| Quem sou | `State.Self`, `Account.Self` |
| Por que voltou ao login | `State.SignedOutReason` |
| Com quem fui pareado | `State.Pairing` (`Self`, `Opponent`: apelido, ícone, nível) |
| A partida | `CurrentMatch` (abaixo) |
| O resultado | `State.Result`, `ResultUpdated` (linha do histórico chegando) |
| Por que a partida não abriu | `State.Unavailable` (`Kind`, `PlayerText`); `RetryMatch()`, `ReturnToLobby()` |
| Overlay de conexão | `Health.Reconnecting`, `Recovered`, `GaveUp`, `LatencyMeasured`, `LastLatency` |

## `ClientAccount`

```csharp
public AccountSessionState SessionState { get; }
public UserId? Self { get; }
public Task<AccountCallOutcome<AccountCreated, RegistrationRefusal>> RegisterAsync(RegistrationForm form);
public Task<SignInOutcome> SignInAsync(string username, Password password);
public Task<ResumeOutcome> ResumeAsync();
public StageRequestResult SignOut();
public Task<AccountCallOutcome<OwnProfile, ProfileRefusal>> ReadProfileAsync();
```

## Catálogo, decks e histórico

Os serviços da 002, expostos como estão. Os construtores ficam internos.

```csharp
CardCatalog.LoadAsync() -> Task<AccountCallOutcome<LoadedCatalog, UnrecognizedRefusal>>   // uma vez por sessão
LoadedCatalog.Find(CardId) -> CardLookup                                                    // unidade, feitiço, não encontrada
PlayerDecks.ListAsync / ReadAsync(DeckId) / CreateAsync(DeckDraft) / ChangeAsync(DeckId, DeckChange) / DeleteAsync(DeckId)
MatchHistory.ReadPageAsync(HistoryPageRequest)
```

## `ClientQueue`

```csharp
public QueuePhase Phase { get; }
public QueueJoinResult Join(DeckId deck);
public StageRequestResult Leave();
public EventFeed<MatchPairing> Paired { get; }
public EventFeed<QueueRefusal> Refused { get; }
public EventFeed<QueueExit> Left { get; }
```

## `LiveMatch` (partida corrente)

```csharp
public MatchId Match { get; }
public LiveMatchStatus Status { get; }          // Connecting, Live, Reconnecting (IsStale), Finished, Refused, GaveUp
public MatchMirror Mirror { get; }              // Current, Version, Self, Phase, IsMyPriority, AmTokenHolder,
                                                // MyMulliganPending, OpponentMulliganAnswered, IsFinished, DidIWin
public TurnClock Clock { get; }                 // Turn, TurnRemaining, MulliganRemaining
public MatchCommands Commands { get; }          // os 12 comandos → Task<PlaySendResult>
public PendingPlay Pending { get; }             // Current, LastSentSinceUpdate
public EventFeed<LiveMatchStatus> StatusChanged { get; }
public EventFeed<PlayRefusal> Refused { get; }
public HandCardHint HintFor(CardInstanceId card);
public DisplayedUnitStats? StatsOf(BankUnit unit);
```

Feeds do espelho: `ViewReplaced`, `EventReceived`, `PhaseChanged`,
`PriorityChanged`, `MatchEnded`. Do relógio: `TurnStarted`, `TurnRunningOut`. Do
pendente: `CurrentChanged`. A ordem dos avisos de um frame é a da 004
(`specs/004-match-session/contracts/match-state.md`).

A partida corrente fica fora da `MatchScene` porque o `match_start` chega antes
de a cena existir, e chega de novo a cada reconexão, com a cena já montada. A
cena sempre lê `CurrentMatch.Mirror.Current` no `BindClient` e se redesenha em
`ViewReplaced`. O cliente nunca mescla: cada estado substitui o anterior inteiro
(comentários preservados do antigo `MatchSession`).

---

## Tipos públicos, por assembly

Tudo o que não está listado é `internal`.

**`Anathema.Net.Core`**
- identidade: `UserId`, `MatchId`, `DeckId`, `CardId`, `CardInstanceId`;
- problemas de deck: `DeckProblem`, `WrongDeckSize`, `TooManyCopies`,
  `UnknownCard`, `UnrecognizedDeckProblem`;
- log: `IClientLog`, `ClientLogEntry` (o que `IClientLog.Write` recebe), `LogField`, `ClientLogLevel`,
  `ClientLogExtensions`;
- avisos: `EventFeed<T>`;
- falha de leitura: `DecodeFailure`, `DecodeFailureKind` (expostos por `SignInOutcome.Decode` e
  `AccountCallFailure.Decode`).

`TransportFailure` e `TransportFailureKind` ficam internos: `TransportFailure` herda da união interna de
resultado HTTP. As propriedades `Transport` de `SignInOutcome` e `AccountCallFailure` e a `Renewal` de
`AccountCallFailure` e `ResumeOutcome` são internas; a apresentação lê o `Kind` de cada resultado.

**`Anathema.Net.Account`**
- chamadas: `AccountCallOutcome<TValue, TRefusal>`, `AccountCallFailure`,
  `AccountCallFailureKind`, `UnrecognizedRefusal`;
- sessão: `AccountSessionState`, `SignInOutcome`, `SignInOutcomeKind`,
  `ResumeOutcome`, `ResumeOutcomeKind`, `Password`;
- cadastro: `RegistrationForm`, `RegistrationField`, `RegistrationFieldError`,
  `RegistrationRefusal`, `AccountCreated`;
- perfil: `OwnProfile`, `ProfileRefusal`, `ProfileRefusalKind`;
- catálogo: `CardCatalog`, `LoadedCatalog`, `CardLookup`, `CardLookupKind`,
  `CatalogCard`, `UnitCard`, `SpellCard`, `SpellEffect`, `SpellTargetKind`,
  `SpellDuration`;
- decks: `PlayerDecks`, `PlayerDeck`, `DeckDraft`, `DeckChange`, `DeckDeleted`,
  `DeckRefusal`, `DeckRefusalReason`, `DeckNotFound`, `DeckLimitReached`,
  `DeckListRejected`, `InvalidDeckName`, `MissingDeckField`,
  `UnrecognizedDeckRefusal`;
- histórico: `MatchHistory`, `MatchHistoryPage`, `MatchHistoryRow`,
  `HistoryPageRequest`, `HistoryOpponent`, `HistoryRefusal`,
  `HistoryRefusalKind`, `MatchEndReason`.

**`Anathema.Net.Connection`**
- fila: `QueuePhase`, `QueueRefusal`, `QueueRefusalKind`, `MatchPairing`,
  `PairedPlayer`;
- desistência: `GiveUpReason`, `GiveUpKind`, `MatchRefusalDetail`;
- `ConnectionPhase`, que aparece em `PlaySendResult`.

**`Anathema.Net.Match`**: todos os tipos públicos de hoje, exceto `MatchFrames`,
`MatchStartFrame`, `MatchUpdateFrame`, `TurnWarningFrame` e `HandCardHints`, que
passam a internos.

**`Anathema.Net.Facade`**
- `AnathemaClient`;
- estado: `ClientState`, `ClientStage`, `ClientStageChange`, `SignedOutReason`,
  `StageRequestResult`;
- conta e fila: `ClientAccount`, `ClientQueue`, `QueueJoinResult`,
  `QueueJoinKind`, `QueueExit`, `QueueExitKind`;
- saúde: `ConnectionHealth`, `ReconnectingNotice`, `RecoveredNotice`,
  `GaveUpNotice`;
- partida: `MatchResult`, `HistoryRowStatus`, `MatchUnavailable`,
  `MatchUnavailableKind`.

**`Anathema.Client.Scenes`** (borda que a apresentação usa): `ISceneClientConsumer`
e `SceneSubscriptions`. `ClientHost`, `SceneRouter`, `SceneRoute` e
`SceneClientBinder` também são públicos, mas são do hospedeiro, não da tela.

---

## Garantias verificadas

| Garantia | Teste | Requisito |
|---|---|---|
| Os tipos exportados das 5 assemblies acima são exatamente os listados | `SurfaceContractTests` | FR-019, FR-022, SC-006 |
| Nenhum tipo exportado da superfície declara `event` | `SurfaceHasNoEventTests` | FR-020 |
| Nenhum `InternalsVisibleTo` cita `Anathema.Presentation`, `Anathema.Client.Scenes` ou `Anathema.Client.Proof` | `FriendAssemblyTests` | FR-021, FR-022 |
| A fachada não referencia motor, Newtonsoft, socket concreto, `Anathema.Net.Json` nem `Anathema.Net.Unity` | `FacadeAssemblyBoundaryTests` | FR-001, FR-022 |
| Feed descartado não entrega, inclusive aviso enfileirado | `EventFeedTests`, `FacadeFeedDisposalTests` | FR-020, SC-004 |
