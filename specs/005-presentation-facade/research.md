# Research: Fachada da apresentação e prova final

**Feature**: `005-presentation-facade` | **Date**: 2026-09-14

Cada item: decisão, por quê, alternativas. Formas de mensagem são as dos
contratos do backend citados na spec e não são repetidas.

---

## R1. Assemblies e sentido das dependências

**Decisão**: quatro assemblies novas de produção, uma evolução de fronteira nas
existentes.

| Assembly | Pasta | Motor | Referencia | Papel |
|---|---|---|---|---|
| `Anathema.Net.Facade` | `Assets/Scripts/Net/Facade/` | não | Core, Account, Connection, Match | `AnathemaClient`, estado do app, histórico pós-partida, saúde, composição sobre portas |
| `Anathema.Client.Scenes` | `Assets/Scripts/Client/Scenes/` | sim | Facade, Unity, Config, Core, Account, Connection, Match | `ClientHost`, `SceneRouter`, `SceneRoute`, `SceneClientBinder`, `ISceneClientConsumer`, `SceneSubscriptions` |
| `Anathema.Presentation` | `Assets/Scripts/Presentation/` | sim | Facade, Client.Scenes, Core, Account, Connection, Match, TextMeshPro, UGUI, Multiplayer Play Mode | os scripts de cena que hoje estão em `Assembly-CSharp` |
| `Anathema.Client.Proof` | `Assets/Scripts/Client/Proof/` | sim | Facade, Unity, Config, Core, Account, Connection, Match | roteiro, bots e runner da prova final; `defineConstraints: UNITY_EDITOR \|\| DEVELOPMENT_BUILD` |

Testes novos: `Anathema.Net.Facade.Tests`, `Anathema.Client.Scenes.Tests`,
`Anathema.Client.Proof.Tests`.

Todas as asmdefs de `Assets/Scripts/Net/`, `Client/`, `Presentation/` e
`Anathema.Config` passam a `autoReferenced: false`. Quem precisa de uma assembly
a referencia pelo nome, e `Assembly-CSharp` (o que sobra nele: `InputNavigator`,
o `MatchController` comentado, `WsEventDto`) não vê a camada.

```text
Anathema.Presentation ─► Anathema.Client.Scenes ─► Anathema.Net.Unity ─► Anathema.Net.Facade ─► Match ─► Connection ─► Account ─► Core
        │                        │                         └──► Anathema.Net.Json ─────────────────────────────────────────► Core
        └─► Facade, Match, Connection, Account, Core      └──► Anathema.Config ─► Anathema.Net.Unity
Anathema.Client.Proof ─► Facade, Unity, Config, Match, Connection, Account, Core
```

**Por quê**:
- a fachada é núcleo (princípio VI): orquestra e não toca motor;
- as referências de asmdef no Unity não são transitivas: a apresentação usa
  `PlayerView`, `DeckId`, `QueueRefusal`, então referencia Match, Core, Account e
  Connection além da fachada (Assumptions da spec);
- a composição real precisa dos adaptadores, então mora na borda
  (`Anathema.Net.Unity`); o hospedeiro de cena e o roteador precisam de
  `SceneManager` e `MonoBehaviour`, então moram numa assembly de borda própria,
  que a apresentação referencia só para a interface de ligação;
- a prova precisa rodar no editor e no aparelho, então é código de execução só
  de desenvolvimento, e não assembly de teste.

**Alternativas**:
- Fachada dentro de `Anathema.Net.Unity`: a fachada passaria a ter motor e não
  poderia ser testada sem adaptadores.
- Scripts de cena continuando em `Assembly-CSharp`: `Assembly-CSharp` enxerga
  toda asmdef com `autoReferenced`, inclusive o codec e os adaptadores.
- Roteiro da prova dentro da assembly de teste: não roda no Android.

---

## R2. Como "fora da superfície" vira erro de compilação

**Decisão**: tipo que não faz parte do contrato vira `internal`. As assemblies
que precisam dele entre si recebem `[assembly: InternalsVisibleTo]`. As
assemblies de apresentação, de cena e de prova **nunca** são amigas.

Consequências mecânicas, todas verificadas pelo compilador:
- Em Core, Account, Connection, Match e Json, os tipos de máquina viram
  `internal`: transporte, socket, codec, uniões, leitores, frames, fila da thread
  principal, ciclo de vida, alcançabilidade, guarda, relógio, conexão
  autenticada, fila de partida, sessão de conta, cliente HTTP autenticado. A
  lista exata do que **fica público** está em
  [contracts/presentation-surface.md](./contracts/presentation-surface.md).
- Membros públicos de tipos públicos que citam tipo interno viram `internal`:
  `static Read(IPayloadReader)` dos modelos, construtores de `LiveMatch`,
  `CardCatalog`, `PlayerDecks`, `MatchHistory`, `MatchCommands`.
- `PlayCommand` continua público e implementa `IOutgoingMessage`, que fica
  interno: a escrita do payload passa a implementação explícita de interface,
  que chama um `internal abstract` (C# não aceita membro público com parâmetro
  interno; implementar interface interna numa classe pública é permitido).
- Os fakes de `Anathema.Net.Fakes` implementam portas internas com membros que
  citam tipos internos, então viram `internal`, e a assembly de fakes ganha
  `InternalsVisibleTo` para cada assembly de teste.
- Teste público com `[TestCase]` de parâmetro interno não compila (memória do
  projeto): esses casos passam a receber o nome ou o número e converter no
  corpo.
- Nos adaptadores Unity, tudo vira `internal`, exceto a composição (R8).

Verificação automática (FR-022):
- `SurfaceContractTests`: os tipos exportados de Core, Account, Connection,
  Match e Facade são exatamente a lista do contrato; os de `Anathema.Net.Unity`
  são a lista de composição; os de `Anathema.Client.Scenes` são a lista da borda
  de cena;
- `SurfaceHasNoEventTests`: nenhum tipo exportado da superfície declara `event`
  (R3);
- `FriendAssemblyTests`: nenhum `InternalsVisibleTo` do projeto cita
  `Anathema.Presentation`, `Anathema.Client.Scenes` ou `Anathema.Client.Proof`.

Com isso, "a prova só usa a superfície" é garantido pelo compilador para a
assembly da prova, e não por varredura de IL.

**Por quê**: é o único arranjo em que usar `AuthenticatedConnection` no código de
cena é erro de compilação (FR-021) sem duplicar os modelos da 002–004.

**Alternativas**:
- Fachada com tipos próprios espelhando `PlayerView`, eventos, cartas, decks e
  recusas: a apresentação referenciaria só a fachada, mas cada modelo existiria
  duas vezes, com mapeamento a manter (princípio V, sem duplicação).
- Separar cada assembly em "modelo" e "máquina": move ~150 arquivos, e os
  modelos precisam dos leitores de payload, que ficariam do lado da máquina.
- Analisador Roslyn de API proibida: pacote novo, e o suporte a arquivo
  adicional por asmdef no Unity 6000.2 não está garantido.
- Só teste de reflexão sobre o IL da apresentação: pega o uso, mas como falha de
  teste, não de compilação.

**Custo registrado**: é a maior mudança mecânica da feature e toca arquivos das
features 001–004 fora do que a fachada usa (Complexity Tracking).

---

## R3. Assinatura descartável: `EventFeed<T>`

**Decisão**: tipo novo em `Anathema.Net.Core` (`Core/Notices/EventFeed.cs`):

- `IDisposable Subscribe(Action<T> listener)` é o único membro público;
- `Publish(T)` e o construtor ficam `internal`;
- a publicação percorre uma cópia dos ouvintes e confere, para cada um, se a
  assinatura já foi descartada;
- a exceção de um ouvinte vai para o log e não impede os seguintes;
- descartar duas vezes não faz nada.

Os eventos C# públicos dos tipos de superfície da 004 viram `EventFeed<T>`, com o
mesmo nome, evoluindo no lugar:

| Tipo | Membros |
|---|---|
| `LiveMatch` | `StatusChanged`, `Refused` |
| `MatchMirror` | `ViewReplaced`, `EventReceived`, `PhaseChanged`, `PriorityChanged`, `MatchEnded` |
| `TurnClock` | `TurnStarted`, `TurnRunningOut` |
| `PendingPlay` | `CurrentChanged` (o construtor passa a receber o log) |

Os eventos dos tipos de máquina (`AuthenticatedConnection`, `MatchQueue`,
`AccountSession`, adaptadores) continuam `event`, porque ficam internos.

**Por quê**: a descrição pede que a partida corrente seja a `LiveMatch` e que
toda assinatura seja descartável; `event` do C# não devolve nada. Convertendo os
eventos dos tipos da superfície, as duas coisas valem ao mesmo tempo.

**Alternativas**:
- Embrulhar `LiveMatch` num objeto da fachada que reexporta tudo: duplica ~20
  membros e esconde a sessão que a descrição manda expor.
- `IObservable<T>`: sem implementação no .NET Standard 2.1 do Unity sem pacote,
  e o contrato de erro e conclusão não se aplica a avisos.

---

## R4. Estado do app

**Decisão**: `ClientStage` fechado com sete valores: `SignedOut`, `SignedIn`,
`Searching`, `Paired`, `InMatch`, `MatchFinished`, `MatchUnavailable`. O
`ClientState` é imutável e é trocado inteiro a cada transição. Um único dono
interno (`ClientStages`) aplica as transições da tabela de
[data-model.md](./data-model.md) e publica `StageChanged` com o anterior e o
novo.

Regras que completam a spec:
- `Paired` já dispara a abertura da partida (catálogo, depois `LiveMatch.Start`),
  como o `MatchmakingClient` faz hoje;
- `Paired → InMatch` acontece no primeiro `LiveMatchPhase.Live`, depois de o
  espelho já ter o estado. `Paired → MatchFinished` também é aceito, quando o
  primeiro frame já vem terminado;
- `Reconnecting` e a volta a `Live` não mudam o estado do app;
- operação fora do estado devolve `StageRequestResult` com `Applied` falso e o
  estado atual. Entrar quando já logado devolve `SignInOutcomeKind.AlreadySignedIn`,
  um valor novo no enum da 002;
- sair, voltar, sessão expirada e descarte incrementam uma geração interna;
  continuação assíncrona de geração velha é ignorada.

**Por quê**: um valor fechado e imutável é o que o roteador, as telas e a prova
leem; geração é o mesmo recurso que a `AccountSession` já usa contra resposta
atrasada.

**Alternativas**: estado derivado na hora das fases da fila, da sessão e da
partida. Cada leitor combinaria três máquinas, e as transições não teriam aviso
único.

---

## R5. Thread principal

**Decisão**: tudo o que a fachada publica sai na thread principal:
- os avisos da conexão, da fila e da partida já chegam nela, porque a conexão da
  003 entrega pela `MainThreadQueue`, e a fachada reage de forma síncrona;
- continuações de `await` (login, catálogo, histórico) não publicam direto:
  enfileiram a aplicação na `MainThreadQueue` injetada;
- `EventFeed` confere o descarte no momento da entrega. Assim, um aviso
  enfileirado para um ouvinte que descartou antes da drenagem não é entregue
  (spec, US1 cenário 8).

**Por quê**: é o ponto único de troca de thread da constituição, e o
`UnityHttpTransport` completa as tarefas por essa mesma fila.

**Alternativas**: `SynchronizationContext` do Unity. Não existe no EditMode sem
cena, e a prova headless não teria garantia.

---

## R6. Linha do histórico depois do fim

**Decisão**: `HistoryRowLookup` interno, dirigido por `IFrameTicker` e pelo
relógio monotônico injetado, sem `Task.Delay`.

- Leituras da página 1 (tamanho padrão 20) nos instantes 0, 1, 3 e 7 s depois do
  fim: 4 leituras, esperas de 1, 2 e 4 s, todas antes de 8 s (FR-016).
- Linha encontrada: `MatchResult.RowStatus = Resolved` com a
  `MatchHistoryRow`. Falha de transporte, recusa ou página sem a linha contam
  como tentativa. Esgotadas as quatro, `Unavailable`.
- A atualização troca o `ClientState` (mesmo estágio) e sai por
  `AnathemaClient.ResultUpdated`, não por `StageChanged`.
- `won` diferente de `Mirror.DidIWin`: `log.Error("match_history_disagrees")`, e
  as duas fontes ficam como vieram.
- Voltar, sair, expirar ou descartar cancela pela geração (R4).

**Por quê**: o servidor grava a linha depois do último frame. Quatro leituras em
7 s cobrem a gravação sem prender a tela, e o tique permite testar sem espera
real (princípio VII).

**Alternativas**: esperar a linha antes de entrar em `MatchFinished` (rejeitado
na spec); temporizador de `System.Threading`, que dispara fora da thread
principal e não é substituível por fake.

---

## R7. Sessão expirada

**Decisão**: quatro fontes convergem para uma transição idempotente,
`SignedOut(SessionExpired)`:
- `AccountSession.SessionExpired`;
- desistência da conexão de fila com `GiveUpKind.SessionExpired` ou `NoSession`;
- `LiveMatch` em `GaveUp` com um desses dois motivos;
- a abertura de partida recebendo `SessionUnavailable` do catálogo.

A transição deixa a fila, descarta a `LiveMatch` (fecha o socket de propósito),
cancela abertura e histórico, e esquece pareamento e partida. `NoSession` com o
estágio já `SignedOut` é ignorado.

**Por quê**: a desistência da conexão pode chegar antes ou depois do aviso da
sessão, dependendo de quem pediu token primeiro; as duas ordens precisam dar o
mesmo estado.

**Alternativas**: ouvir só o `SessionExpired` da sessão. A partida ficaria em
`MatchUnavailable` se a desistência da conexão chegasse primeiro.

---

## R8. Composição

**Decisão**: em `Anathema.Net.Unity`, só três tipos novos são públicos, mais
`RefreshTokenVaultSlot` e `NetworkLayerHost`:

- `ClientCompositionOptions`: rotas (`ServerRoutes`), slot da guarda (nulo = o da
  plataforma), log (nulo = `UnityConsoleLog`), política de cleartext e
  `AllowClockJumps`;
- `ClientComposition.Compose(options)`: monta fila da thread principal,
  adaptadores, ciclo de vida, alcançabilidade, ticker e a fachada;
- `ComposedClient`: expõe `Client`, `Pump()` (mesmo passo do `Update` do
  hospedeiro), `AttachTo(GameObject)`, `DropSockets()`, `JumpClock(TimeSpan)`
  e `Dispose()`.

Detalhes:
- **Onde a fachada é montada**: `LiveAccountServices` e `LiveConnectionServices`
  saem de `Anathema.Net.Unity` e vão para `Anathema.Net.Facade/Composition/` como
  `AccountServices` e `ConnectionServices` internos, com o `.meta` preservado. A
  fachada se compõe a partir de `ClientPorts` (portas da 001 + codec + guarda +
  rotas); a borda só cria as portas reais. Os testes da fachada compõem as mesmas
  peças sobre fakes.
- **Relógio com salto**: `SteppableMonotonicClock`, interno, decora o relógio da
  plataforma com um deslocamento. `JumpClock` só funciona com `AllowClockJumps`
  e, sem ele, lança exceção citando a opção. O salto é controlado pelo teste
  (FR-036).
- **Queda de socket**: `DroppableWebSocketFactory`, interno, decora a fábrica
  real e guarda os sockets vivos. `DropSockets()` aborta todos, o que a conexão
  da 003 trata como queda de rede. Em partida só o socket de partida está aberto,
  porque a fila sai no pareamento.
- **Rotas**: `ServerRoutes`, público, junta as rotas HTTP e WS. `AccountRoutes` e
  `ConnectionRoutes` ficam internos, e `AppConfig.BuildServerRoutes()` substitui
  os dois construtores antigos.
- **Guarda por slot**: `PlatformRefreshTokenVault.Create(log, slot)` aceita o
  slot. No Android, `AndroidKeystoreRefreshTokenVault` passa a receber o slot no
  alias da chave e no nome do arquivo: dois clientes headless no mesmo aparelho
  não podem dividir a guarda.
- `NetworkLayerHost` perde o `Attach` público: `ComposedClient.AttachTo` o
  adiciona e liga ao `Pump` e ao ciclo de vida.

**Por quê**: o hospedeiro de cena, a prova no editor e a prova no aparelho montam
a camada pelo mesmo caminho (FR-025), e o que só a prova usa (salto, queda) não é
alcançável pela apresentação, que não referencia `Anathema.Net.Unity`.

**Alternativas**:
- Decorador de relógio público recebendo `IMonotonicClock`: exigiria deixar
  `IMonotonicClock` e `MonotonicInstant` públicos, visíveis à apresentação.
- Derrubar a conexão por reachability falsa, como a 004: exige fakes de
  ciclo de vida na composição real.

---

## R9. Hospedeiro, ponto de acesso e roteador de cena

**Decisão**:
- `ClientHost` (MonoBehaviour, `Anathema.Client.Scenes`) é o `PlayerSession.cs`
  renomeado e movido com o mesmo `.meta`. Assim, a referência da `BootstrapScene`
  continua válida, e o `m_EditorClassIdentifier` é atualizado. Ele absorve o
  `AppEnvManager`: recebe `configDev`, `configProd` e `isProd` por
  `SerializeField` (valores copiados no YAML da cena). No `Awake` compõe,
  `AttachTo(gameObject)` e `DontDestroyOnLoad`; no `Start` liga o roteador e o
  binder e liga os consumidores já carregados; no `OnDestroy` descarta.
- **Ponto de acesso**: injeção, não localizador. Scripts de cena implementam
  `ISceneClientConsumer.BindClient(AnathemaClient client)`. O `SceneClientBinder`
  percorre as raízes de cada cena carregada (`scene.GetRootGameObjects()` e
  `GetComponentsInChildren<ISceneClientConsumer>(true)`) no `sceneLoaded`, e no
  `Start` do hospedeiro para as cenas abertas antes dele. Nenhum `static`, nenhum
  `Find`.
- `SceneRoute.For(ClientStage)` é puro e testado:
  - `SignedOut` → `LoginScene`;
  - `SignedIn` e `Searching` → `HomeScene`;
  - `Paired` → `VersusScene`;
  - `InMatch` e `MatchFinished` → `MatchScene`;
  - `MatchUnavailable` → nenhuma.
- `SceneRouter` assina `StageChanged`. Ele carrega a cena de destino em modo
  único só se `SceneManager.GetSceneByName(destino).isLoaded` for falso. O critério
  não é a cena ativa, porque o `ForceBootstrapOnPlay` abre a `LoginScene` como
  aditiva e a ativa é a `BootstrapScene`.
- `SceneSubscriptions` (lista de `IDisposable`, `DisposeAll`) é o que cada
  consumidor chama no `OnDestroy`.
- O `ReconnectOverlay` vira componente de um objeto da `BootstrapScene`, ligado
  pelo binder. O `static instance` e o `Attach(params BaseClient[])` saem.

**Por quê**: injeção deixa claro de onde vem a fachada e cumpre "sem `static
Instance`". O teste do mapa estágio → cena não precisa de cena.

**Alternativas**:
- Propriedade estática na borda (`ClientHost.Current`): é o singleton de novo,
  com outro nome.
- `FindFirstObjectByType<ClientHost>()` em cada cena: busca escondida, e a ordem
  de `Awake` entre cenas não é garantida.

---

## R10. Destino do código antigo

Tabela completa em
[contracts/composition-and-scenes.md](./contracts/composition-and-scenes.md#destino-do-código-antigo).
Resumo:
- `BaseClient`: sai; a tradução de estado da conexão para o overlay vai para
  `HealthRelay`, com o comentário da tentativa preservada;
- `MatchClient`: sai; a abertura vai para `MatchOpening` e a troca de cena para o
  `SceneRouter`;
- `MatchmakingClient`: sai; a fila vai para `ClientQueue` + `ClientStages`;
- `MatchSession`: sai; a partida corrente é `AnathemaClient.CurrentMatch`, e os
  comentários de intenção vão para o `<summary>` dela;
- `VersusContext` e `PlayerPublicDTO`: saem; o pareamento é
  `ClientState.Pairing`;
- `SelfProfileService`: sai; o perfil é `ClientAccount.ReadProfileAsync`;
- `PlayerSession`: vira `ClientHost`;
- `AppEnvManager`: absorvido pelo `ClientHost`.

**Por quê**: cada um só repassava para uma peça que já existe na fachada;
manter o arquivo seria responsabilidade duplicada (FR-030).

---

## R11. Prova final

**Decisão**: `Anathema.Client.Proof`, não amiga de ninguém (R2):

- `MatchProofScript.RunAsync(ProofSetup)`: o roteiro da US9. Cada passo registra
  um `ProofStep` e falha por `ProofFailure` com a expectativa e o valor
  recebido.
- `ProofPlayer`: um `ComposedClient` com rótulo, slot `proof-<rótulo>-<sufixo>` e
  `ProofLog`.
- `ProofBot` (evolui de `SmokeBot`) e `ProofStrategy` (evolui de
  `SmokeStrategy`). Eles assinam os `EventFeed` da `LiveMatch` e usam
  `LiveMatch.HintFor` no lugar de `HandCardHints.For`, que fica interno.
- `CommandCoverage`: conjunto dos 11 comandos cobertos, com o nome dos que
  faltam.
- `ProofWait.UntilAsync(condição, limite)`: `Stopwatch` + `Task.Yield`, como o
  `UntilAsync` da 004. Quem bombeia é o chamador: a corrotina do teste ou o
  `Update` do runner.
- `LocalServerRoutes`: `127.0.0.1:8000` com os caminhos do backend.
- `MatchProofRunner` (MonoBehaviour) na cena `Assets/Scenes/Dev/MatchProofScene.unity`:
  lê o `AppConfig` com o host de lançamento de desenvolvimento, compõe os dois
  clientes com `AttachTo` e roda o roteiro.

Extensões da estratégia do `smoke_match.py`, só as necessárias para a cobertura:

- **Remover bloqueador**: na defesa, se ainda não cobriu, depois de atribuir e
  com o bloqueio aceito, remove o mesmo bloqueador uma vez; em seguida segue a
  regra normal (atribui de novo, encerra).
- **Feitiço sem alvo**: `LIFE POTION` (1004, energia 4) ignora o teto de Nexus 12
  enquanto "feitiço sem alvo" não foi coberto. `SACRIFICIAL FIRE` (1003, só na
  declaração, energia 8) conta quando sai pela regra normal.
- **Vez estourada**: o bot P2 fica parado na primeira Fase de Ação dele a partir
  da rodada 2 (`--stall`).
- **Partida 2**: P1 manda `forfeit` no mulligan.

Verificações que dependem de observação:

- **`turn_warning`**: o bot parado assina `Clock.TurnRunningOut` e registra o
  número da vez. Durante a vez parada nenhum `match_update` chega (ninguém joga),
  então o aviso só pode ter vindo do `turn_warning` (o `warning` do frame
  precisaria de um frame). Depois, os dois espelhos recebem `turn_timed_out` e,
  no mesmo `match_update`, `passed`.
- **Renovação antes da reabertura**: o `ProofLog` marca o instante do salto.
  Depois da marca, a primeira linha `access_token_renewed` do cliente saltado
  precisa vir antes da primeira `connection_opening`. A conexão da 003 sempre
  pede o token antes de abrir (`AuthenticatedConnection`, linha do
  `GetValidAsync` antes do log de abertura). O salto é de 5 min 30 s, acima dos
  5 min do token menos a margem de 30 s, e o teste chama `DropSockets()` logo
  depois.
- **Queda**: rodada 3, P2, `DropSockets()`. `Health.Reconnecting` e depois
  `Health.Recovered` precisam sair, o estágio precisa continuar `InMatch` e a
  `CurrentMatch` precisa continuar a mesma instância.

O teste `[Explicit] [Category("LiveServer")]` em
`Anathema.Client.Proof.Tests` só bombeia os dois clientes e chama o roteiro.
`ProofStrategyTests` e `CommandCoverageTests` usam catálogo e visões montados
pelos leitores internos, por isso `Anathema.Client.Proof.Tests` é amiga de
Core, Json, Account e Match (Complexity Tracking). A prova (`Anathema.Client.Proof`)
continua sem amizade nenhuma.

**Por quê**: o mesmo roteiro serve para o editor e para o aparelho (quickstart
§5), e a regra "se precisar de outro tipo, a superfície está incompleta" passa a
ser um erro de compilação da assembly da prova.

**Alternativas**:
- Roteiro no teste e um runner Android separado: dois roteiros.
- Runner por flag de lançamento, como o `DevConnectionProbe`: a descrição pede
  uma cena de prova.

---

## R12. Cena de prova fora do build de produção

**Decisão**: `MatchProofScene` fica fora de `EditorBuildSettings`. O quickstart
§5 manda incluí-la manualmente, no índice 0, num build de desenvolvimento, e
tirá-la antes de commitar. A assembly da prova não compila fora de
`UNITY_EDITOR || DEVELOPMENT_BUILD`. Se a cena entrasse num build de produção por
engano, ela teria só um script ausente e nunca seria carregada.

**Por quê**: a lista de cenas é fixada antes do pré-processamento de build, e não
dá para filtrá-la por tipo de build sem um script de build novo.

**Alternativas**: filtro de build em `Anathema.Net.Editor`. Ele mudaria o fluxo de
build de produção por causa de uma prova manual; fica registrado no TODO se o
esquecimento acontecer.

---

## R13. `Game/TODO.md` e `CLAUDE.md`

**Decisão**:
- **Marcados "[feito na 005]"**:
  - `SessionExpired` sem reação visual (vira estágio e roteador);
  - `PlayerSession.Instance` como raiz de composição;
  - singletons de cena nos clientes de socket;
  - recusa de fila e saída da fila sem reação visual (viram avisos da fachada);
    a tela de fila continua para a parte visual;
  - catálogo indisponível ao abrir a partida só no log (vira `MatchUnavailable`).
- **Reapontados para "Caminho: parte visual"**:
  - `MatchScene` não desenha nada;
  - botão Jogar entra com o primeiro deck (tela de deck);
  - `MatchUnavailable` sem tela;
  - resultado da partida sem tela;
  - `VersusScene` pisca antes da `MatchScene`.
- **Novo item de dívida**: cena de prova incluída manualmente no build (R12).
- **`CLAUDE.md`**: uma linha em "Base de conhecimento" apontando
  `specs/005-presentation-facade/contracts/presentation-surface.md` como a porta
  de entrada da parte visual.

---

## R14. Nomes novos

Os nomes novos têm zero ocorrências hoje, conferido por busca: `AnathemaClient`,
`ClientStage`, `ClientState`, `ClientStages`, `StageRequestResult`,
`ClientAccount`, `ClientQueue`, `QueueExit`, `ConnectionHealth`,
`HealthRelay`, `MatchOpening`, `HistoryRowLookup`, `MatchResult`,
`MatchUnavailable`, `EventFeed`, `ClientComposition`, `ComposedClient`,
`ServerRoutes`, `SteppableMonotonicClock`, `DroppableWebSocketFactory`,
`ClientHost`, `SceneRouter`, `SceneRoute`, `SceneClientBinder`,
`ISceneClientConsumer`, `SceneSubscriptions`, `MatchProofScript`, `ProofPlayer`,
`ProofBot`, `ProofStrategy`, `CommandCoverage`, `ProofLog`, `MatchProofRunner`.
