# Contrato: ambiente e build

**Feature**: `001-server-connection` | `AppConfig` (`Anathema.Config`),
`Anathema.Net.Editor`, `CleartextPolicy`/`ServerHost`/`ReleaseTlsRule` (núcleo)

Por que cada camada existe: [research.md](../research.md) R3, R6 e R10.

---

## Endereço do servidor

| Situação | Host efetivo | Esquemas |
|---|---|---|
| Editor | `apiBaseUrl` do `AppConfig` selecionado (ou `-serverHost` se passado) | conforme `useTls` |
| Build de desenvolvimento, sem parâmetro | `apiBaseUrl` | conforme `useTls` |
| Build de desenvolvimento, com parâmetro de inicialização válido | o do parâmetro | conforme `useTls` |
| Build de desenvolvimento, parâmetro inválido | `apiBaseUrl`; log `server_host_rejected raw=… expected=host[:porta]` | conforme `useTls` |
| Build de produção | `apiBaseUrl`; parâmetro ignorado; `OverrideHost` recusa | só `https`/`wss` (garantido pelo gate) |

Parâmetro de inicialização:

- **Android**:
  `adb shell am start -n <applicationIdentifier>/com.unity3d.player.UnityPlayerGameActivity -e serverHost <ip>:<porta>`
- **Windows**: `Anathema.exe -serverHost <ip>:<porta>`

Normalização (`ServerHost.TryParse`, também usada no `OnValidate`):

| Entrada | Resultado |
|---|---|
| `192.168.0.10:8000` | `192.168.0.10:8000` |
| `http://192.168.0.10:8000/` | `192.168.0.10:8000` |
| `wss://api.anathema.com` | `api.anathema.com` |
| `""`, `"   "` | recusa: vazio |
| `192.168.0.10:8000/api` | recusa: caminho não é aceito |
| `192.168.0.10:99999` | recusa: porta fora de 1..65535 |

Rotas que o `AppConfig` mantém: `loginEndpoint`, `playerMe`,
`tokenRefreshEndpoint`, `matchmakingConsumerUrl` e `matchConsumerUrl`.
`connectionConsumerUrl` sai: a rota `ws/connection/` não existe no backend.

---

## Tráfego sem TLS

| Build | `UnityWebRequest` `http://` | `ClientWebSocket` `ws://` | Quem garante |
|---|---|---|---|
| Editor | permitido | permitido | `insecureHttpOption=DevelopmentOnly`; `CleartextPolicy(true)` |
| Desenvolvimento, Windows | permitido | permitido | idem |
| Desenvolvimento, Android | permitido | permitido | idem, mais `usesCleartextTraffic="true"` pelo `DevCleartextManifest` |
| Produção, Windows | bloqueado (`CleartextRefused`) | bloqueado (`cleartext_refused`) | `insecureHttpOption`; `CleartextPolicy(false)` |
| Produção, Android | bloqueado | bloqueado | `insecureHttpOption`; `usesCleartextTraffic="false"`; `CleartextPolicy(false)` |

`CleartextPolicy` é construída na composição com
`new CleartextPolicy(allowsCleartext: Debug.isDebugBuild)`. Nenhum outro código
decide isso.

---

## Gate de build de produção

`ReleaseBuildTlsGate : IProcessSceneWithReport`

| Condição | Resultado |
|---|---|
| Entrada no Play Mode (`report == null`) | nada |
| Build com **Development Build** | nada |
| Build sem Development Build; cena sem seletor de ambiente | nada |
| Build sem Development Build; seletor aponta para `AppConfig` com `useTls=true` | passa, sem aviso |
| Build sem Development Build; seletor aponta para `AppConfig` com `useTls=false` | `BuildFailedException` com a mensagem abaixo |
| Build sem Development Build; seletor com o config selecionado vazio | `BuildFailedException`: config não atribuído, com o nome do campo |

Mensagem (forma):

```text
Build de produção bloqueado: a cena <caminho da cena> seleciona o AppConfig '<nome>'
(isProd=<valor>) com useTls=false. Um build sem "Development Build" exige TLS: marque
isProd e use um AppConfig com useTls ligado, ou gere com "Development Build".
```

Seletor de ambiente: qualquer componente com os campos serializados `isProd`
(bool), `configDev` e `configProd` (referência a `AppConfig`). Hoje, só o
`AppEnvManager` da `BootstrapScene`. Os nomes ficam em constantes em
`EnvironmentSelectionReader`, com referência a
`Assets/Scripts/Bootstrap/AppEnvManager.cs`.

Testes (`Anathema.Net.Editor.Tests`, EditMode):
- `ReleaseTlsRule` nas quatro combinações de build × TLS.
- `EnvironmentSelectionReader` sobre um componente de teste com os mesmos campos.
- Um teste que abre `AppEnvManager` por `MonoScript` e falha se algum dos três
  campos deixar de existir.

---

## Manifesto Android

`DevCleartextManifest` (`IPreprocessBuildWithReport` + `IPostGenerateGradleAndroidProject`) grava em
`<application>`:

- build de desenvolvimento: `android:usesCleartextTraffic="true"`;
- build de produção: `android:usesCleartextTraffic="false"` (explícito por causa
  do `minSdk 23`).

---

## Código antigo

Continua compilando. Muda só o que o [plano](../plan.md#código-anterior-tocado)
lista: `AppConfig`, os dois assets, `NetworkBootstrap` (sem `ConnectionClient`),
`LoginController` (vai para `HomeScene` depois do login) e `insecureHttpOption`.
