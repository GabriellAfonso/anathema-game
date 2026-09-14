# Contrato: código anterior, composição e remoções

**Feature**: `004-match-session`

O que muda nos arquivos antigos para o jogo seguir de Jogar até a `MatchScene`
sobre a sessão de partida nova (FR-042 a FR-046).

---

## Composição (`PlayerSession`)

`ComposeSocketClients` passa a criar:

```csharp
Match = new MatchClient(composed.MatchConnection, composed.Routes.Match, Account.Catalog, adapters.Clock, Log);
```

O resto de `PlayerSession` não muda. `Instance` segue como raiz até a feature 5.

---

## `MatchClient`

```csharp
public class MatchClient : BaseClient
{
    public MatchClient(AuthenticatedConnection connection, Uri matchBase, CardCatalog catalog, IMonotonicClock clock, IClientLog log);
    public LiveMatch? Live { get; }
    public void Connect(MatchId match);
}
```

`Connect` dispara `ConnectAsync`:

1. `catalog.LoadAsync()`; falha → log `match_catalog_unavailable` (`kind`) e para;
2. `Live?.Dispose()`;
3. nova `LiveMatch(connection, matchBase, match, catalog, clock, log)`;
4. `MatchSession.Instance.Attach(live)`;
5. no primeiro `Mirror.ViewReplaced`: carrega `MatchScene` se não for a ativa;
   os seguintes não recarregam;
6. `live.Start()`.

Saem: `RawTextReceived`, `nextTextIsMatchStart`, `JsonConvert`,
`MatchStartEnvelope`, `HandleStartMatch(MatchStateDTO)`, `using Newtonsoft.Json`.
O comentário "precisa ser idempotente: recarregar a cena numa reconexão apagaria a
partida" fica, junto do passo 5.

---

## `MatchSession`

```csharp
public class MatchSession : MonoBehaviour
{
    public static MatchSession Instance { get; }        // inalterado
    public LiveMatch? Live { get; }
    public PlayerView? State { get; }                   // Live?.Mirror.Current
    public bool HasState { get; }
    public event Action<PlayerView>? OnStateChanged;    // repassa Mirror.ViewReplaced
    public void Attach(LiveMatch live);
    public void Clear();
}
```

- `Attach` desfaz a ligação anterior antes de assinar a nova.
- `Clear` desfaz a ligação, sem descartar a sessão: o dono dela é o `MatchClient`.
- Sai `ApplyState(MatchStateDTO)`.
- Os comentários de classe ("fica fora da MatchScene porque…", "o cliente nunca
  mescla…") ficam.

---

## Conexão (003) e borda

| Arquivo | Mudança | Research |
|---|---|---|
| `Net/Connection/Connection/AuthenticatedConnection.cs` | sai `RawTextReceived` e o repasse do texto em `OnFrame` | R12 |
| `Net/Connection/Connection/SocketAttempt.cs` | `FrameArrived` sem o texto | R12 |
| `Tests/EditMode/Net.Connection/ConnectionFrameDeliveryTests.cs` | sai `TextoCruAcompanhaCadaFrameAceito` | R12 |
| `Net/Unity/LiveNetworkAdapters.cs` | codec com `MatchFrames.CreateUnion()`; comentário de R1 atualizado | R3 |
| `Net/Unity/Anathema.Net.Unity.asmdef` | + `Anathema.Net.Match` | R1 |
| `Tests/EditMode/Net.Unity/Anathema.Net.Unity.Tests.asmdef` | + `Anathema.Net.Match` | R14 |
| `Tests/EditMode/Net.Unity/LiveServer/LivePlayer.cs` | + deck com feitiços, + `LiveMatch` | R14 |
| `Net/Core/Protocol/PayloadIdentityReading.cs`, `PayloadIdentityWriting.cs` | + listas de `CardInstanceId` | R2 |
| `Net/Account/AccountAssemblyInfo.cs` | + `InternalsVisibleTo("Anathema.Net.Match.Tests")` | R13 |
| `Net/Account/History/MatchHistoryRow.cs`, `Catalog/SpellEffect.cs` | usam `MatchEndReasonText` e `SpellDurationText` extraídos (públicos, novos) | R2 |

---

## Removidos (com `.meta`)

| Caminho | Por quê |
|---|---|
| `Assets/Scripts/DTO/Match/MatchStateDTO.cs` e a pasta `DTO/Match` | forma obsoleta (FR-042) |

Verificação: quickstart §5 (SC-003).

`Domain/Match/MatchController.cs` (todo comentado) e `DTO/Player/PlayerPublicDTO.cs`
(usado por `VersusContext`) não são tocados.

---

## `Game/TODO.md` (vault)

Na implementação:

- **Marcar feito** (seção da 003): "Ponte de `match_start` no `MatchClient`".
- **Reescrever** "Singletons de cena nos clientes de socket": `MatchClient` usa
  `MatchSession.Instance` só para `Attach`, e `SceneManager` para o primeiro
  `match_start`. Caminho: feature 5.
- **Novos**, seção da feature 004:
  - instante de chegada lido na entrega à thread principal; caminho se aparecer
    travamento entre recepção e entrega: carimbar no adaptador de socket
    (research R5);
  - catálogo indisponível ao abrir a partida só vai para o log
    (`match_catalog_unavailable`); caminho: tela de erro de partida (feature 5);
  - `MatchScene` não desenha espelho, relógio nem dicas; caminho: feature 5.
