# Contrato: código anterior, composição e remoções

**Feature**: `003-authenticated-socket-queue`

O que muda nos arquivos antigos para o jogo seguir de Login a `match_start` sobre
a conexão nova (FR-038 a FR-046).

---

## Composição (`PlayerSession`)

```csharp
public class PlayerSession : MonoBehaviour
{
    public static PlayerSession Instance { get; private set; }   // dívida até a feature 5
    public LiveAccountServices Account { get; private set; }
    public MatchmakingClient Matchmaking { get; private set; }
    public MatchClient Match { get; private set; }
    public IClientLog Log { get; private set; }
    // sai: string? Token
}
```

`Awake` → `ComposeNetwork()`:

1. `UnityConsoleLog`, `MainThreadQueue`, `LiveNetworkAdapters`, `UnityAppLifecycle`,
   `UnityNetworkReachability`, `UnityFrameTicker`;
2. `NetworkLayerHost.Attach(queue, lifecycle, reachability, ticker)`;
3. `LiveAccountServices.FromAdapters(...)`;
4. `LiveConnectionServices.FromAdapters(adapters, lifecycle, reachability, ticker, Account.Tokens, AppEnvManager.Settings.BuildConnectionRoutes())`;
5. `Match = new MatchClient(connections.MatchConnection, connections.Routes.Match)`;
   `Matchmaking = new MatchmakingClient(connections.Queue, Match, log)`;
6. `ReconnectOverlay.Attach(Matchmaking, Match)`.

`OnDestroy` descarta conexões e conta.

---

## Clientes de socket

| Arquivo | Depois | Requisito |
|---|---|---|
| `WebSocketClient/Base.cs` (`BaseClient`) | adaptador fino sobre `AuthenticatedConnection`; sem NativeWebSocket, Newtonsoft, `JsonUtility`, `Debug`, `WebSocketDispatcher`, `PlayerSession`, `TokenRefreshService`; eventos do overlay traduzidos | FR-038, FR-045, FR-046 |
| `WebSocketClient/MatchmakingClient.cs` | `Join(DeckId)`, `Leave()` sobre `MatchQueue`; `Paired` → `VersusContext.Instance.SetContext(pairing)`, `SceneManager.LoadScene("VersusScene")`, `match.Connect(pairing.Match)`; `Refused`/`MatchmakingFailed`/`LeftQueue` → `IClientLog` | FR-040, FR-041 |
| `WebSocketClient/MatchClient.cs` | `Connect(MatchId)` com `ConnectionTarget.Match`; `match_denied` tratado pela conexão; `match_start` pela ponte `RawTextReceived` + `JsonConvert` + `MatchSession.Instance.ApplyState`, como hoje | FR-039 |
| `UI/PlayButton.cs` (`MyButtonScript`) | `async`: `Account.Decks.ListAsync()`; primeiro deck → `Matchmaking.Join`; sem deck ou falha → log `play_without_deck` / `play_decks_unavailable` | FR-040 |
| `UI/ReconnectOverlay.cs` | mesma interface; `Attach(params BaseClient[])` inalterado | FR-042 |
| `Core/Contexts/VersusContext.cs` | `SetContext(MatchPairing)`; `Player`/`Opponent` preenchidos a partir de `PairedPlayer` | FR-041 |

Tradução do `BaseClient` para o overlay:

| Conexão | Evento antigo |
|---|---|
| `StatusChanged(WaitingRetry)` | `OnReconnecting(attempt, wait em segundos)` |
| `StatusChanged(Suspended(NoNetwork))` | `OnReconnecting(attempt atual, 0)` |
| `Recovered` | `OnReconnected` |
| `StatusChanged(GaveUp)` | `OnGaveUp(reason.PlayerText())` |
| `Leave`, fila pareada | nenhum |

Assinaturas implementadas: `MatchClient(AuthenticatedConnection, Uri matchBase, IClientLog)`,
`MatchmakingClient(AuthenticatedConnection, MatchQueue, MatchClient, IClientLog)`.
`BaseClient` perdeu `Connect()`, `OnConnected` e `OnConnectionError`, que não tinham uso.
O `MatchClient` marca o `match_start` por `FrameReceived` e desserializa o texto seguinte
de `RawTextReceived` num envelope com `JsonConvert`, sem `JObject`.

Eventos de log do código de cena: `play_without_deck`, `play_decks_unavailable`,
`matchmaking_join_refused` (`code`, `problems`), `matchmaking_failed` (`error`),
`matchmaking_left_queue` (`kind`), `match_start_without_state`.

---

## Núcleo e borda da 001/002 tocados

| Arquivo | Mudança | Research |
|---|---|---|
| `Net/Core/Lifecycle/LifecycleSignalFilter.cs` (+ `BackgroundSignalMode.cs`) | modo explícito; construtor com `bool` delega | R9 |
| `Net/Core/Protocol/Frames/GenericServerFrames.cs` (+ `MatchDeniedFrame.cs`) | registra `match_denied` | R1 |
| `Net/Core/Time/IFrameTicker.cs`, `Net/Core/Socket/IWebSocketFactory.cs` | novos | R2, R6 |
| `Net/Unity/NetworkLayerHost.cs` | `Attach` recebe o ticker; `Update`: `Poll` → `Drain` → `Raise` | R2 |
| `Net/Unity/UnityAppLifecycle.cs` | `Create` escolhe `DesktopKeepsRunning` com Run In Background fora do Android | R9 |
| `Net/Unity/LiveNetworkAdapters.cs` | codec com `ConnectionFrames.CreateUnion()`; expõe `Sockets` (`DotNetWebSocketFactory`) | R1, R6 |
| `Net/Unity/Anathema.Net.Unity.asmdef` | + `Anathema.Net.Connection` | R1 |
| `Net/Account/AccountAssemblyInfo.cs` | + `InternalsVisibleTo("Anathema.Net.Fakes")` | R5 |
| `Core/Config/AppConfig.cs`, `Anathema.Config.asmdef` | `BuildConnectionRoutes()`; + `Anathema.Net.Connection` | data-model |
| `Tests/EditMode/Fakes/Anathema.Net.Fakes.asmdef` | + `Anathema.Net.Account` | R5 |
| `ProjectSettings/ProjectSettings.asset` | `runInBackground: 0 → 1` | R9 |

---

## Removidos (com `.meta`)

| Caminho | Por quê |
|---|---|
| `Assets/WebSocket/` (`WebSocket.cs`, `WebSocket.jslib`, `endel.nativewebsocket.asmdef`) | NativeWebSocket (FR-044) |
| `Assets/Scripts/Core/Network/WebSocketClient/Dispatcher.cs` | `WebSocketDispatcher` (FR-044) |
| `Assets/Scripts/Core/Network/WebSocketClient/ConnectionClient.cs` | rota inexistente (FR-044) |
| `Assets/Scripts/Core/Network/TokenRefreshService.cs` | ponte da 002 (FR-044) |
| `Assets/Scripts/Core/Network/Reconnect/` (`Heartbeat.cs`, `ReconnectPolicy.cs`, `Anathema.Reconnect.asmdef`) | movidos para `Anathema.Net.Connection` (FR-043) |
| `Assets/Tests/EditMode/HeartbeatTests.cs`, `ReconnectPolicyTests.cs`, `Anathema.Reconnect.Tests.asmdef` | movidos para `Anathema.Net.Connection.Tests` (FR-043) |
| `Assets/Scripts/Bootstrap/NetworkBootstrap..cs` e o GameObject `WebSocketClient` na `BootstrapScene` (o componente já estava como script ausente) | composição no `PlayerSession` (R13) |
| GameObject `WebSocketDispatcher` na `BootstrapScene` | a classe saiu; ficaria como script ausente |
| `Assets/Scripts/DTO/Network/ErrorPayloadDTO.cs` | sem uso depois dos clientes (FR-046) |
| `Assets/Scripts/DTO/VersusDTO.cs` | `match_found` tipado (FR-030) |

Verificação: SC-002 (quickstart §7).

---

## `Game/TODO.md` (vault)

Na implementação:

- **Marcar feitos** os itens da 001 (remover `ConnectionClient`/NativeWebSocket;
  religar `BaseClient`, `MatchClient`, `MatchmakingClient`) e da 002 (ponte
  `PlayerSession.Token`/`TokenRefreshService`).
- **Reescrever** "`PlayerSession.Instance` como raiz de composição": caminho
  passa a ser a feature 5.
- **Novos**, seção da feature 003:
  - ponte `RawTextReceived` + `JsonConvert` para `match_start` no `MatchClient`
    (feature 4);
  - pedido ao backend: `match_found` perdido na queda entre pareamento e entrega
    (research R11);
  - pedido ao backend: `leave` por `user_id` remove a entrada do socket novo
    (research R11);
  - `PlayButton` entra com o primeiro deck até existir tela de escolha;
  - `VersusContext.Instance`, `MatchSession.Instance` e `SceneManager` nos
    clientes de socket (feature 5).
