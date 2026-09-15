# Contrato: composição, cenas e código antigo

**Feature**: `005-presentation-facade`

---

## Composição (`Anathema.Net.Unity`)

```csharp
public sealed class ServerRoutes { /* 9 Uri: register, login, refresh, profile, cards, decks, matches, matchmaking, match */ }

public sealed class ClientCompositionOptions
{
    public ClientCompositionOptions(ServerRoutes routes, bool allowCleartext);
    public RefreshTokenVaultSlot? VaultSlot { get; init; }   // nulo: o slot da plataforma
    public IClientLog? Log { get; init; }                    // nulo: UnityConsoleLog
    public bool AllowClockJumps { get; init; }
}

public static class ClientComposition
{
    public static ComposedClient Compose(ClientCompositionOptions options);
}

public sealed class ComposedClient : IDisposable
{
    public AnathemaClient Client { get; }
    public void Pump();                       // alcançabilidade, fila da thread principal, tique
    public void AttachTo(GameObject host);    // NetworkLayerHost chamando Pump e repassando pausa/foco
    public void DropSockets();                // aborta os sockets vivos; a 003 trata como queda
    public void JumpClock(TimeSpan forward);  // só com AllowClockJumps; lança sem ele
    public void Dispose();
}
```

| Garantia | Teste |
|---|---|
| Rotas, log e slot nulos ou inválidos lançam com o valor e a forma esperada | `ClientCompositionTests` |
| `JumpClock` sem `AllowClockJumps` lança citando a opção; salto negativo lança | `ClientCompositionTests`, `SteppableMonotonicClockTests` |
| Salto soma ao relógio de dentro e nunca faz o relógio voltar | `SteppableMonotonicClockTests` |
| `DropSockets` aborta só os sockets ainda vivos e esquece os fechados | `DroppableWebSocketFactoryTests` |
| `Pump` faz a mesma sequência do `NetworkLayerHost.Update` (poll, drain, tique) | `ClientCompositionTests` |
| `AppConfig.BuildServerRoutes` monta as 9 rotas do host efetivo | `AppConfigRoutesTests` (evolui) |
| Dois `Compose` com slots diferentes não dividem a guarda | `ClientCompositionTests` (DPAPI em diretório temporário) |

---

## Borda de cena (`Anathema.Client.Scenes`)

```csharp
public interface ISceneClientConsumer { void BindClient(AnathemaClient client); }

public static class SceneRoute { public static string? For(ClientStage stage); }

public sealed class SceneSubscriptions { public void Add(IDisposable subscription); public void DisposeAll(); }

public sealed class ClientHost : MonoBehaviour     // PlayerSession.cs renomeado, mesmo .meta
{
    [SerializeField] AppConfig configDev, configProd; [SerializeField] bool isProd;
}
```

| Garantia | Requisito | Teste / prova |
|---|---|---|
| Mapa estágio → cena da research R9 | FR-028 | `SceneRouteTests` |
| O roteador não carrega cena já carregada | FR-028 | quickstart §4 |
| Cada consumidor é ligado uma vez por componente, nas cenas abertas antes do hospedeiro e nas carregadas depois | FR-026, FR-031 | quickstart §4 |
| `SceneManager` só em `SceneRouter.cs` e `SceneClientBinder.cs` (fora das ferramentas de editor) | FR-029, SC-005 | `SceneManagerUsageTests` |
| Nenhum `static ... Instance { get; private set; }` nem `static <Tipo>? instance` mutável em `Assets/Scripts/` | FR-027, SC-005 | `StaticInstanceUsageTests` |

`SceneClientBinder` usa `SceneManager.sceneLoaded`; por isso aparece na garantia
do `SceneManager` junto do roteador.

---

## Destino do código antigo

| Arquivo | Destino | Responsabilidade vai para | Comentários |
|---|---|---|---|
| `Core/Session/PlayerSession.cs` | renomeado e movido para `Client/Scenes/ClientHost.cs` (mesmo `.meta`) | composição → `ClientComposition`; perfil copiado → `ClientAccount.ReadProfileAsync` | resumo reescrito; nota do `RuntimeInitializeOnLoadMethod` preservada |
| `Bootstrap/AppEnvManager.cs` | removido | seleção dev/prod e checagens de TLS e host vazio → `ClientHost` | mensagens de erro preservadas como log |
| `Core/Network/WebSocketClient/Base.cs` (`BaseClient`) | removido | tradução de estado → `HealthRelay` | comentário da última tentativa preservado |
| `Core/Network/WebSocketClient/MatchClient.cs` | removido | abrir partida → `MatchOpening`; cena → `SceneRouter` | comentários do cache de catálogo e da idempotência de cena preservados |
| `Core/Network/WebSocketClient/MatchmakingClient.cs` | removido | fila → `ClientQueue` + `ClientStages`; cena → `SceneRouter` | comentário de `matchmaking_failed` preservado |
| `Core/Session/MatchSession.cs` | removido | partida corrente → `AnathemaClient.CurrentMatch` | resumo vai para o `<summary>` de `CurrentMatch` |
| `Core/Contexts/VersusContext.cs`, `DTO/Player/PlayerPublicDTO.cs` | removidos | pareamento → `ClientState.Pairing` | — |
| `Core/Network/SelfProfileService.cs` | removido | perfil → `ClientAccount.ReadProfileAsync` | — |
| `Login/LoginController.cs` | movido para `Presentation/` | `BindClient`: retoma; entrar pela fachada; sem `SceneManager` nem `IsEnvironmentReady` | login automático do Multiplayer Play Mode mantido |
| `UI/PlayButton.cs` (`MyButtonScript`) | movido para `Presentation/` | `client.Decks.ListAsync` → `client.Queue.Join` | nome da classe mantido (cena liga por ele) |
| `UI/VersusController.cs` | movido para `Presentation/` | `BindClient`: `State.Pairing` | — |
| `UI/MiniPlayerProfile.cs` | movido para `Presentation/` | `BindClient`: `Account.ReadProfileAsync` | — |
| `UI/ReconnectOverlay.cs` | movido para `Presentation/`; componente na `BootstrapScene` | `client.Health`; sai `static instance` e `Attach` | comentários do overlay preservados |
| `Net/Unity/LiveAccountServices.cs`, `LiveConnectionServices.cs` | movidos para `Net/Facade/Composition/` como `AccountServices`, `ConnectionServices` (internos) | composição sobre portas | — |
| `Net/Unity/NetworkLayerHost.cs` | evolui: sem `Attach` público; ligado por `ComposedClient.AttachTo` | — | comentário da ordem drain/tique preservado |
| `Net/Unity/Storage/PlatformRefreshTokenVault.cs`, `AndroidKeystoreRefreshTokenVault.cs` | evoluem: recebem slot | — | — |
| `Core/Config/AppConfig.cs` | evolui: `BuildServerRoutes()` no lugar dos dois construtores | — | nota do `[NonSerialized]` preservada |
| `Scenes/BootstrapScene.unity` | objetos de `AppEnvManager`, `VersusContext` e `SelfProfileService` removidos; `ClientHost` com as configs; objeto `ReconnectOverlay` novo | — | — |

Não mudam: `InputNavigator`, `Domain/Match/MatchController.cs` (comentado),
`DTO/Network/WsEventDTO.cs`, `Editor/ForceBootstrapOnPlay.cs`.

---

## Amizades (`InternalsVisibleTo`)

| Assembly | Amigas de produção | Amigas de teste |
|---|---|---|
| `Anathema.Net.Core` | Json, Account, Connection, Match, Facade, Unity, Config, Net.Editor, Fakes | Core, Json, Account, Connection, Match, Facade, Unity, Config, Editor, Proof `.Tests` |
| `Anathema.Net.Json` | Unity | Json, Account, Connection, Match, Facade, Unity, Proof `.Tests` |
| `Anathema.Net.Account` | Connection, Match, Facade, Unity, Config, Fakes | Account, Connection, Match, Facade, Unity, Proof `.Tests` |
| `Anathema.Net.Connection` | Match, Facade, Unity, Config, Fakes | Connection, Match, Facade, Unity `.Tests` |
| `Anathema.Net.Match` | Facade, Unity | Match, Facade, Unity, Proof `.Tests` |
| `Anathema.Net.Facade` | Unity | Facade, Unity `.Tests` |
| `Anathema.Net.Unity` | Config, Net.Editor | Unity, Config `.Tests` |
| `Anathema.Net.Fakes` | — | todas as `.Tests` |

Nenhuma linha cita `Anathema.Presentation`, `Anathema.Client.Scenes` ou
`Anathema.Client.Proof` (`FriendAssemblyTests`).

---

## `Game/TODO.md` e `CLAUDE.md`

Itens em [research.md R13](../research.md#r13-gametodomd-e-claudemd).
