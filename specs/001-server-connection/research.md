# Research: Conexão com o servidor

**Feature**: `001-server-connection` | **Date**: 2026-09-13

Cada item: decisão, por quê, alternativas. "Verificar no aparelho" marca o que
só o quickstart prova; o resto foi confirmado no código-fonte do IL2CPP
instalado, na documentação do Unity 6000.2 ou em issue pública.

---

## R1. Relógio monotônico e tempo com o aparelho dormindo

**Decisão**: `IMonotonicClock` tem dois adaptadores escolhidos pela plataforma:

- **Windows**: `StopwatchMonotonicClock` (`Stopwatch.GetTimestamp`).
- **Android**: `BootTimeMonotonicClock`, que lê `clock_gettime(CLOCK_BOOTTIME)`
  por P/Invoke em `libc`. `CLOCK_BOOTTIME` é monotônico e conta o tempo de
  suspensão. Pode ser chamado de qualquer thread, sem JNI.

`PlatformMonotonicClock.Create()` escolhe entre os dois com
`#if UNITY_ANDROID && !UNITY_EDITOR`. As duas plataformas são alvos, então a
regra de "nada de `#if` para outras plataformas" continua valendo.

**Por quê**:

- O IL2CPP 6000.2.8f1 implementa o tick monotônico com
  `clock_gettime(CLOCK_MONOTONIC, ...)`
  (`Editor/Data/il2cpp/libil2cpp/os/Posix/Time.cpp`, linhas 75 e 103-113).
  `CLOCK_MONOTONIC` para durante a suspensão no Linux/Android. A mesma diferença
  entre plataformas aparece no .NET ([dotnet/runtime#77945](https://github.com/dotnet/runtime/issues/77945)).
- No Windows, `QueryPerformanceCounter` (base do `Stopwatch`) inclui o tempo de
  sono, standby e hibernação ([Microsoft Learn](https://learn.microsoft.com/en-us/windows/win32/sysinfo/acquiring-high-resolution-time-stamps)).
- Sem isso, com o celular bloqueado por 10 minutos, a duração fora pode sair em
  segundos. A feature 2 não renovaria o token (5 minutos), e a feature 3
  reconectaria com o token vencido.

**Detalhes**: `timespec` no ARM64 são dois `long`. O único ABI Android do projeto
é ARM64 (`AndroidTargetArchitectures: 2`). A struct usa `nint` nos dois campos,
para não depender disso. `CLOCK_BOOTTIME = 7`. Se a chamada falhar (retorno
diferente de 0), o adaptador cai para `Stopwatch` e registra
`clock_boottime_unavailable` uma vez.

**Verificar no aparelho**: bloquear a tela por 6 minutos. O log de volta deve
mostrar `away_ms` ≥ 360000 (quickstart §5).

**Alternativas**:
- `Stopwatch` em todo lugar (o que a descrição pedia): subestima a duração fora
  no Android. Rejeitada por FR-016.
- `SystemClock.elapsedRealtimeNanos()` por JNI: correto, mas exige
  `AttachCurrentThread` para leituras fora da thread principal e custa uma
  chamada JNI por leitura.
- `DateTime.UtcNow` para a duração: é hora de parede e salta com ajuste de
  relógio. A constituição proíbe.

---

## R2. Ciclo de vida: pausa, foco, Windows e Android

**Decisão**: o `NetworkLayerHost` repassa `OnApplicationPause(bool)` e
`OnApplicationFocus(bool)` ao `UnityAppLifecycle`. A decisão fica em
`LifecycleSignalFilter` (núcleo, testável), com duas entradas de configuração:

- **Android**: `pause(true)` = foi para segundo plano. `pause(false)` = voltou.
  `focus(true)` depois de um "foi" sem "voltou" também conta como volta.
  `focus(false)` sozinho é ignorado (teclado, aba de notificações, diálogo do
  sistema).
- **Windows**: `focusLossStopsPlayer = !Application.runInBackground`. No projeto,
  `runInBackground: 0`. Quando o player para ao perder o foco, `focus(false)`
  também é "foi". Caso contrário, só `pause`.

O filtro suprime repetições: no máximo um "foi" por "voltou", e nunca começa com
"voltou". A duração fora é `agora - instante do "foi"` no `IMonotonicClock`.

**Por quê**: pela documentação do Unity, o player desktop sem Run In Background
pausa ao perder o foco e chama `OnApplicationPause(true)` só se a janela for
menor que a tela. No Android, apertar Home com o teclado aberto chama
`OnApplicationPause` sem `OnApplicationFocus`
([OnApplicationPause](https://docs.unity3d.com/6000.2/Documentation/ScriptReference/MonoBehaviour.OnApplicationPause.html)).
Nenhum dos dois sinais sozinho basta nas duas plataformas.

**Alternativas**: só `OnApplicationPause` perde o alt-tab em tela cheia no
Windows. Só `OnApplicationFocus` trata o teclado do Android como segundo plano.

---

## R3. Qual camada bloqueia tráfego sem TLS no Android (e no Windows)

Resposta registrada por adaptador, como FR-047 pede:

| Camada | Alcança `UnityWebRequest` (HTTP) | Alcança `ClientWebSocket` (socket) |
|---|---|---|
| **1. `PlayerSettings.insecureHttpOption`** (Unity) — `NotAllowed` / `DevelopmentOnly` / `AlwaysAllowed` | **Sim.** A documentação o define só para `UnityWebRequest` ([InsecureHttpOption](https://docs.unity3d.com/6000.2/Documentation/ScriptReference/InsecureHttpOption.html)). Vale em Windows e Android. | **Não.** `System.Net` não passa por ele. |
| **2. Network Security Config / `android:usesCleartextTraffic`** (Android) | **Sim**, no Android: o erro "Cleartext HTTP traffic to … not permitted" relatado com `UnityWebRequest` é mensagem da pilha HTTP do Java ([Unity Discussions](https://discussions.unity.com/t/android-9-cleartext-http-traffic-to-my-ip-not-permitted/724166)). Padrão `false` a partir da API 28, `true` abaixo dela. | **Não.** A NSC só governa as pilhas Java. O `ClientWebSocket` do Mono/IL2CPP abre socket BSD nativo. |
| **3. `CleartextPolicy` do projeto** | Sim (defesa em profundidade; categoria `CleartextRefused`) | **Sim — é a única camada** |

O Android Player do 6000.2.8f1 não liga `insecureHttpOption` ao manifesto. O
único lugar do módulo Android com "cleartext" é a API de manifesto
`Unity.Android.Gradle` (`UsesCleartextTraffic`). Nenhuma ferramenta de build
referencia `InsecureHttpOption`. Busca feita nos binários de
`PlaybackEngines/AndroidPlayer`.

**Decisão**:

1. `insecureHttpOption = DevelopmentOnly` (hoje é `NotAllowed`, e por isso nem o
   HTTP de desenvolvimento funciona).
2. `DevCleartextManifest` (ganchos `IPreprocessBuildWithReport` e
   `IPostGenerateGradleAndroidProject`, editando o XML do manifesto da unityLibrary) grava
   `android:usesCleartextTraffic="true"` em build de desenvolvimento e `"false"`
   em build de produção. O `false` explícito importa porque o `minSdk` é 23: em
   aparelho com API 23-27, o padrão do Android libera cleartext.
3. `CleartextPolicy` é construída na composição a partir de `Debug.isDebugBuild`
   (verdadeiro no editor e no build de desenvolvimento). É injetada no
   `DotNetWebSocket` e no `UnityHttpTransport`. Com a política fechada,
   `ws://`/`http://` não abre: o socket avisa erro `cleartext_refused` seguido de
   fechado sem código, e o HTTP devolve falha `CleartextRefused`.
4. O gate de build (R6) impede que um build de produção sequer use configuração
   sem TLS.

**Verificar no aparelho**: o build de desenvolvimento faz HTTP e socket em `http://`/`ws://` com o IP do computador (quickstart §4). Se o HTTP falhar com "Cleartext HTTP traffic not permitted", o modificador de manifesto não rodou.

**Registro da implementação (2026-09-13)**:

- A primeira versão usou `AndroidProjectFilesModifier`, como o plano previa.
- Compilada contra as DLLs do próprio editor 6000.2.8f1 (`UnityEditor.Android.Extensions.dll`
  e `Unity.Android.Gradle.dll`), falhou com CS1061: `UnityLibraryManifest` não é alcançável em
  `AndroidProjectFiles` a partir de código de projeto.
- Ficou o caminho alternativo de T101. A asmdef de editor não precisa de referência
  explícita a `Unity.Android.Gradle` (T005).
- O tipo do build vem de `report.summary.options` no pré-build, e não de
  `EditorUserBuildSettings.development`, que pode divergir de um `BuildPlayer` com
  `BuildOptions.Development`.
- `UnityEngine.Android.AndroidApplication.currentActivity` existe em
  `UnityEngine.AndroidJNIModule.dll` desta versão (R10).

**Alternativas**: `network_security_config.xml` com domínios liberados foi
rejeitado: o IP do computador muda, e o arquivo teria de existir só no build de
desenvolvimento, que é o mesmo trabalho do atributo. Confiar só no gate de build
foi rejeitado: não impede código de produção de montar uma URL `ws://` à mão.

---

## R4. `ClientWebSocket` no Unity: close code, recepção, envio concorrente

**Decisão**: `DotNetWebSocket` usa um `ClientWebSocket` por conexão:

- **Recepção**: laço em `Task.Run` com buffer de 8 KiB, acumulando num
  `MemoryStream` até `EndOfMessage` (FR-009).
  - `WebSocketMessageType.Close`: `(int)result.CloseStatus` e
    `CloseStatusDescription` viram `SocketClosure`. O enum
    `WebSocketCloseStatus` carrega qualquer inteiro, então 4001 chega como 4001.
  - `Binary`: vira aviso de erro `binary_frame_ignored`, e a recepção continua.
  - `WebSocketException`/`IOException`: aviso de erro, depois fechado com
    `Code = null`.
- **Envio**: serializado por `SemaphoreSlim(1)`. O `ClientWebSocket` não aceita
  dois `SendAsync` simultâneos.
- **Fechar de propósito**: `CloseOutputAsync(NormalClosure)`. O fechado com 1000
  chega pelo laço de recepção quando o servidor ecoa. Se o servidor não ecoar em
  5 s, `Abort()` e fechado com o código enviado. Fechar antes de abrir cancela o
  `ConnectAsync` e produz um único fechado sem código.
- **Entrega**: todo aviso passa por `MainThreadQueue.Enqueue`. Uma trava de
  estado (`Interlocked`) garante um único fechado (FR-007).

**Por quê**: a NativeWebSocket achata o close code, e é por isso que `BaseClient`
guarda `rejectionReason`. O `ClientWebSocket` expõe o número. Há relatos antigos
de falha de conexão com `ClientWebSocket` em Android ARMv7/ARM64. O tracker não
tem mais a issue, e o projeto é só ARM64 no Unity 6.

**Verificar no aparelho**: quickstart §4 (ping/pong) e o LiveServer 2 no editor
(4001 exato).

**Alternativas**: manter a NativeWebSocket perde o close code. Escrever um
cliente WebSocket próprio sobre `TcpClient` é complexidade sem ganho.

---

## R5. `UnityWebRequest`: thread, prazo e classificação de falha

**Decisão**: `UnityHttpTransport.SendAsync` pode ser chamado de qualquer thread:

- Cria e dispara o `UnityWebRequest` dentro de um item da `MainThreadQueue`.
- Completa o `TaskCompletionSource<HttpOutcome>` no callback `completed`, que roda
  na thread principal.
- Prazo padrão de **10 s** (`HttpRequestSpec.TimeoutSeconds`), igual ao do
  `smoke_match.py`.

`Result`:

- `Success` e `ProtocolError`: `HttpOutcome.Response(status, body)`. 4xx e 5xx
  são resposta (FR-011). O corpo é lido como texto UTF-8 sem interpretação.
- `ConnectionError` e `DataProcessingError`: `HttpOutcome.Failure(kind, detail)`,
  e `UnityWebRequestErrorClassifier` mapeia o texto de `error` do Unity:
  - "Request timeout" → `Timeout`
  - "Cannot resolve destination host" → `HostNotResolved`
  - "Cannot connect to destination host" → `CannotConnect`
  - o resto → `Other`, com o texto original em `detail`
- `InvalidOperationException` de cleartext bloqueado pelo Unity → `CleartextRefused`.

**Por quê**: `UnityWebRequest` só roda na thread principal e não expõe código
numérico de falha; o texto de `error` é o único sinal. Esse texto é do Unity, não
do servidor, então a regra "recusa pelo `code`" não se aplica. O classificador
fica isolado e tem teste.

**Alternativas**: `HttpClient` do .NET rejeitado: a constituição fixa
`UnityWebRequest`, e o `HttpClient` passaria por fora do `insecureHttpOption`.

---

## R6. Gate de build: produção sem TLS falha na hora do build

**Decisão**: `ReleaseBuildTlsGate : IProcessSceneWithReport`, em asmdef de editor.

- Com `report == null` (entrar no Play Mode), não faz nada.
- Com `report.summary.options` sem `BuildOptions.Development`, procura na cena
  componentes que tenham os campos serializados `isProd`, `configDev` e
  `configProd` (`EnvironmentSelectionReader`, via `SerializedObject`). Lê o
  `AppConfig` selecionado e aplica `ReleaseTlsRule` (núcleo, função pura).
- Violação: `BuildFailedException` com cena, nome do asset, valor de `isProd`,
  `useTls=false` e a correção.

  Exemplo: `Build de produção bloqueado: a cena Assets/Scenes/BootstrapScene.unity seleciona o AppConfig 'AppConfig_Dev' (isProd=false) com useTls=false. Um build sem "Development Build" exige TLS: marque isProd e use um AppConfig com useTls ligado, ou gere com "Development Build".`

**Por quê**:

- Cenas processam antes de o pacote ser gerado (FR-046).
- A seleção de ambiente mora no `AppEnvManager`, em `Assembly-CSharp`, que asmdef
  não referencia. Ler por nome de campo evita mudar código antigo (Complexity
  Tracking do plano).
- Validar todo `AppConfig` do projeto bloquearia todo build de produção enquanto
  o asset de desenvolvimento existisse.

**Alternativas**: `IPreprocessBuildWithReport` roda antes das cenas e teria de
abrir cada cena à mão. O `Debug.LogError` atual do `AppEnvManager` só aparece em
tempo de execução, no aparelho.

---

## R7. Codec sem reflexão e o IL2CPP

**Decisão**: o codec não desserializa por reflexão (nada de
`JsonConvert.DeserializeObject<T>`).

- `NewtonsoftProtocolCodec` faz `JToken.Parse` e embrulha objetos em
  `JObjectPayloadReader`.
- Cada braço de união lê campo por campo por `IPayloadReader`
  (`ReadInteger("user_id")`, …).
- Para envio, `JObjectPayloadWriter` monta um `JObject` e serializa com
  `Formatting.None`.
- `Assets/Scripts/Net/link.xml` preserva `Anathema.Net.Core` e
  `Anathema.Net.Json` inteiros. Newtonsoft já traz o próprio `link.xml` no pacote
  (conversores de `System.ComponentModel`). `managedStrippingLevel` fica no
  padrão do projeto.

**Por quê**:

- Sem reflexão, o stripping não tem o que remover por engano: todo tipo de frame
  é construído por delegate referenciado estaticamente no registro da união.
- A leitura explícita também dá o nome do campo na falha (FR-033) e garante que o
  campo a mais é ignorado (FR-032).
- O `link.xml` cobre o que as features seguintes registrarem nas mesmas
  assemblies.

**Verificar no aparelho**: o probe decodifica `pong` e `auth_denied` no build IL2CPP (quickstart §4).

**Alternativas**:
- `JsonConvert` com `[Preserve]` em cada DTO: depende de cada feature lembrar do
  atributo, e desconhecido/inválido vira exceção a traduzir.
- `JsonUtility`: proibido pela constituição.

---

## R8. Entrega na thread principal

**Decisão**: `MainThreadQueue` no núcleo.

- `ConcurrentQueue<Action>` com estado aberto/fechado.
- `Enqueue` de qualquer thread. Com a fila fechada, descarta sem exceção.
- `Drain()`, chamado pelo `NetworkLayerHost.Update`, retira só os itens que
  existiam no início da chamada. Um item enfileirado durante a entrega vai para o
  próximo quadro: sem reentrância e sem laço infinito.
- Exceção de um item → `IClientLog` (`main_thread_item_failed`), e segue para o
  próximo.
- `Close()`, chamado em `NetworkLayerHost.OnDestroy`, fecha e esvazia.

A ordem é a de linearização do `Enqueue`: itens de um mesmo produtor saem na
ordem em que ele os pôs, e o total segue a ordem global de chegada à fila.

Os avisos de ciclo de vida e de alcançabilidade, que já nascem na thread
principal, também passam pela fila. Assim, um "voltou" nunca chega antes de um
fechamento de socket que o antecedeu.

**Por quê**: é o único ponto de troca de thread (princípio VI). Fica no núcleo
para os testes de ordem e de destruição rodarem com threads reais em EditMode,
sem cena.

**Alternativas**: `SynchronizationContext` do Unity. `Post` não oferece
"descartar depois de destruído", e o núcleo teria de conhecer o contexto do
motor.

---

## R9. Alcançabilidade

**Decisão**: `UnityNetworkReachability` lê `Application.internetReachability` a
cada 1 s (medido no `IMonotonicClock`), chamado do `Update` do hospedeiro:

- `NotReachable` → `None`
- `ReachableViaLocalAreaNetwork` → `LocalArea`
- `ReachableViaCarrierDataNetwork` → `CarrierData`

Mudança gera `NetworkKindChanged(previous, current)`. Sem mudança, nada.

**Por quê**: a API só pode ser lida na thread principal. No Android, cada leitura
consulta o sistema, e uma vez por segundo basta para perceber Wi-Fi → dados. A
documentação do Unity avisa que ela não prova alcance ao servidor; o
heartbeat da feature 3 cuida disso.

**Alternativas**: `ConnectivityManager.NetworkCallback` por JNI é instantâneo,
mas exige classe Java e thread de callback. Fica para quando 1 s se mostrar
lento.

---

## R10. Host de desenvolvimento sem interface visual

**Decisão**:

- `LaunchServerHostReader`, só em build de desenvolvimento
  (`Debug.isDebugBuild`), lê:
  - **Android**: extra de intent `serverHost`, via
    `AndroidApplication.currentActivity` → `getIntent().getStringExtra("serverHost")`.
    Comando:
    `adb shell am start -n <pacote>/com.unity3d.player.UnityPlayerGameActivity -e serverHost 192.168.0.10:8000`
    (entrada GameActivity, `androidApplicationEntry: 2`).
  - **Windows**: argumento `-serverHost 192.168.0.10:8000`
    (`Environment.GetCommandLineArgs`).
- `AppConfig` usa esse host, normalizado e validado por `ServerHost.TryParse`,
  quando presente. Também oferece `OverrideHost(string)` para uma futura tela de
  depuração.
- Em build de produção, nenhum dos dois é consultado, e `OverrideHost` devolve
  recusa.
- Nada é persistido: sem o extra, vale o host do asset.

**Por quê**: a feature não tem interface visual. O extra de intent é o jeito
padrão de passar parâmetro a um app Android de desenvolvimento. Não persistir
evita um IP velho de outra rede ficar grudado.

**Alternativas**:
- `adb reverse tcp:8000 tcp:8000` com `localhost`: funciona só por USB e não
  prova o caminho pela rede, que a spec pede.
- Persistir em `PlayerPrefs`: permitido (não é credencial), mas rejeitado pelo
  motivo acima.

---

## R11. Probe de ponta a ponta e testes LiveServer

**Decisão**: `LiveServerProbeSteps` (em `Anathema.Net.Unity`) tem três passos,
usados pelos testes LiveServer e pelo probe do aparelho, sem duplicar:

1. `GET /game/cards/` sem token → espera 401.
2. `ws/matchmaking/?token=invalido` → espera `AuthDeniedFrame` e fechado com 4001.
3. `POST /accounts/register/` com usuário `probe_<ticks>` e depois
   `POST /accounts/login/` → `token`. A seguir `ws/matchmaking/?token=…`, `ping`
   com `PingMarker` e espera `PongFrame` com o mesmo marcador.

Formato do cadastro e do login: o de `backend/scripts/smoke_match.py`
(`sign_up`, `log_in`). O login devolve `token` e `refresh`.

`DevConnectionProbe` é um `[RuntimeInitializeOnLoadMethod]`:

- Só em build de desenvolvimento e só com o extra/argumento `connectionProbe`.
- Cria o `GameObject` com `NetworkLayerHost`, compõe os adaptadores e roda os três
  passos.
- Registra `connection_probe_step` e `connection_probe_passed` ou
  `connection_probe_failed` pelo `IClientLog`, que chega ao `adb logcat -s Unity`.

**Testes LiveServer no editor**: `[UnityTest]` que, a cada tick do editor, chama
`MainThreadQueue.Drain()`. O `Update` do `NetworkLayerHost` não roda em EditMode.
Cada assinante guarda `Thread.CurrentThread.ManagedThreadId` e compara com o da
thread principal capturado no início (SC-004).

**Por quê**: o cadastro antes do login evita depender de um usuário que exista
no banco local. O probe reaproveita os mesmos passos, então o que passa no
editor é o mesmo que roda no aparelho.

**Alternativas**: usuário fixo semeado no backend exige passo manual e
credencial no quickstart.

---

## R12. Rota `ws/connection/` e o login atual

**Decisão**: tirar `connectionConsumerUrl` do `AppConfig` e dos dois assets; tirar
`ConnectionClient` do `NetworkBootstrap`; e fazer o `LoginController` carregar
`HomeScene` logo depois de guardar os tokens.

**Por quê**: o backend não tem mais essa rota. Hoje o `LoginController` só sai da
tela quando o socket de presença abre, o que nunca acontece. É a menor mudança
que mantém o código antigo compilando (FR-048) e restaura o fluxo que já
existia. Premissa registrada na spec (Assumptions), ainda sem confirmação do
mantenedor.

**Alternativas**: manter o campo como `[Obsolete]` até a feature 3 deixa aviso
de compilação (SC-001) e uma rota morta na configuração, contra FR-042.
