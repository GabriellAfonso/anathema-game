# Research: Conta e dados do jogador

**Feature**: `002-player-account` | **Date**: 2026-09-13

Cada item: decisão, por quê, alternativas. As formas HTTP de catálogo, decks e
histórico são as dos contratos do backend (citados na spec) e não são repetidas.

---

## R1. Validade do token sem o relógio do aparelho

**Decisão**: `AccessTokenReader` lê o JWT sem biblioteca:

1. separa as três partes por `.`; qualquer outra quantidade é fora do contrato;
2. decodifica a segunda parte em base64url (troca `-`/`_`, completa `=`);
3. passa o texto por `IProtocolCodec.DecodeObject` e lê `exp` e `iat` como
   inteiros e `user_id` como texto que contém um inteiro positivo.

A vida útil é `exp - iat` segundos, contada a partir do `IMonotonicClock.Now`
lido quando a resposta chega. `AccessToken.NeedsRenewal(now, margin)` é
`now >= arrivedAt + lifetime - margin`. A hora do sistema nunca é lida.

**Verificado no backend** (`server/requirements.txt`: `djangorestframework_simplejwt==5.5.1`,
sem `SIMPLE_JWT` em `server/core/settings.py`):

- `Token.__init__` chama `set_exp` e `set_iat` para todo token novo (`tokens.py:79`).
- `RefreshToken.access_token` cria um `AccessToken` novo e não copia `exp`, `iat`,
  `jti` nem `token_type` (`no_copy_claims`): o acesso da renovação tem `iat` próprio.
- `RefreshToken.for_user` grava `user_id` como **texto**: `user_id = str(user_id)`
  (`tokens.py`, `for_user`). O claim chega como `"7"`, não `7`. Como o acesso
  herda os claims do refresh, o acesso da renovação traz o mesmo `user_id`.
- `ROTATE_REFRESH_TOKENS: False`: a renovação devolve só `access`.

A assinatura **não** é verificada: o cliente não tem a chave (HS256 com o
`SECRET_KEY` do servidor) e não é a autoridade. Ler `exp`, `iat` e `user_id` é
conveniência para agendar a renovação e saber quem é o jogador; quem recusa token
é o servidor.

**Margem**: 30 s (`AccountTiming.DefaultRenewalMargin`), injetável. A contagem a
partir da chegada superestima a vida pelo tempo de trânsito (tipicamente < 1 s
na rede local, poucos segundos em dados móveis ruins); 30 s cobre isso e o
intervalo entre pedir o token e abrir o socket na feature 3.

**Alternativas**:
- Comparar `exp` com `DateTime.UtcNow`: quebra com hora do aparelho errada e é
  proibido pela spec (FR-009).
- Assumir 5 minutos fixos: quebra no dia em que o backend configurar
  `ACCESS_TOKEN_LIFETIME`.
- Biblioteca JWT: pacote novo, e o cliente não valida assinatura.

---

## R2. Modelo de concorrência e testes assíncronos

**Decisão**:

- Toda a sessão de conta roda na thread principal. Não há lock: as tarefas do
  `UnityHttpTransport` completam na thread principal (`TaskCompletionSource`
  completado no callback `completed`, research R5 da 001), e os avisos do
  `UnityAppLifecycle` chegam pela `MainThreadQueue`.
- O código de `Anathema.Net.Account` usa `ConfigureAwait(false)` em todo `await`.
  Com o `TaskCompletionSource` sem `RunContinuationsAsynchronously`, a
  continuação roda onde a tarefa completou: thread principal no jogo, thread do
  teste nos fakes. Sem isso, a continuação iria para o
  `UnitySynchronizationContext`, que num `[Test]` só anda no próximo tick do
  editor.
- **Uma renovação por vez**: `TokenRenewal` guarda a `Task<RenewalOutcome>` em
  curso; quem chega durante ela recebe a mesma tarefa. A referência é limpa na
  conclusão, antes de entregar o resultado.
- **Geração da sessão**: `AccountSession.Generation` (inteiro) aumenta a cada
  entrar, sair, expirar e retomar. Resultado de renovação cuja geração de partida
  difere da atual é descartado (FR-016). O catálogo guarda a geração em que foi
  carregado e se invalida sozinho (FR-028), sem assinar evento.
- **Testes**: UTF 1.6 executa `[Test] public async Task` (`TaskTestMethodCommand`
  no pacote). Os fakes completam na hora, então o teste é determinístico.
- **Concorrência testável**: `FakeHttpTransport` ganha `HoldNext()`, que devolve
  um `HeldHttpResponse` com `Release(status, body)` e `ReleaseFailure(kind, detail)`.
  O pedido fica pendente até o teste soltar, e o teste verifica que dez chamadas
  geraram um só pedido de renovação antes de soltar.

**Alternativas**:
- `SemaphoreSlim` para a renovação: resolve uma corrida entre threads que não
  existe e acrescenta `await` sem prazo.
- Evento "sessão encerrada" para limpar o catálogo: mais acoplamento e uma ordem
  de assinatura a testar.
- Stub inline que devolve `Task` pendente: proibido pelo princípio VII.

---

## R3. Guarda no Windows: DPAPI sem `ProtectedData`

**Achado**: com `apiCompatibilityLevel: 6` (.NET Standard 2.1), o
`netstandard.dll` de referência do Unity 6000.2.8f1 não contém
`ProtectedData` (0 ocorrências). Existe um
`System.Security.Cryptography.ProtectedData.dll` em
`Editor/Data/NetStandard/EditorExtensions/` (só editor) e em
`MonoBleedingEdge/lib/mono/unityaot-win32/Facades/`, mas nenhum dos dois é
referência de compilação do perfil do projeto.

**Decisão**: `DpapiRefreshTokenVault` chama a DPAPI direto por P/Invoke:

- `crypt32.dll`: `CryptProtectData` e `CryptUnprotectData`, com `DATA_BLOB`
  (`int cbData`, `IntPtr pbData`) montado por `Marshal.AllocHGlobal`/`Copy`, sem
  `unsafe`.
- `kernel32.dll`: `LocalFree` no blob de saída.
- Flag `CRYPTPROTECT_UI_FORBIDDEN` (0x1); escopo do usuário atual (sem
  `CRYPTPROTECT_LOCAL_MACHINE`).
- Entropia opcional fixa do app (`"anathema.refresh_token.v1"` em UTF-8): impede
  que outro programa do mesmo usuário decifre o arquivo só chamando a DPAPI.
- O texto cifrado vai para
  `Application.persistentDataPath/account/refresh_token.<slot>.bin`, gravado num
  arquivo temporário ao lado e movido por cima (`File.Replace` quando já existe,
  `File.Move` quando não), para um app morto no meio não deixar arquivo pela metade.
- `CryptUnprotectData` falhando (arquivo de outro usuário do Windows, corrompido,
  entropia diferente) → `VaultReadOutcome.Unreadable` com o código do
  `Marshal.GetLastWin32Error()`; a sessão apaga e segue sem guarda (FR-023).

Funciona no editor (Mono) e no player Windows (IL2CPP), que aceitam `DllImport`
de DLL do sistema. O adaptador só é compilado para Windows e editor
(`#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN`).

**Slot por jogador virtual (FR-024)**: o Multiplayer Play Mode roda cada jogador
virtual como um editor clonado com projeto próprio
(`CurrentPlayer.IsMainEditor => !VirtualProjectsEditor.IsClone`), então o
`Application.dataPath` difere entre eles, mas o `persistentDataPath` é o mesmo
(depende só de empresa e produto). O slot é:

- player: `player`;
- editor: `editor-` + 12 primeiros hex do SHA-256 de `Application.dataPath`.

Não depende de API interna do pacote nem das tags, que podem estar vazias.

**Alternativas**:
- Referenciar a fachada `ProtectedData.dll` por `csc.rsp` ou
  `precompiledReferences`: depende de arquivo interno da instalação do Unity, não
  existe no perfil do Android e quebraria a compilação de lá.
- Trocar para o perfil .NET Framework: mudança de stack sem emenda.
- `PlayerPrefs` ou arquivo em texto puro: proibido (constituição).
- Credential Manager (`CredWriteW`): também P/Invoke, mas guarda em cofre roaming
  e não separa por slot sem nomear alvos; DPAPI + arquivo é mais simples.

---

## R4. Guarda no Android: Android Keystore

**Decisão**: um plugin Java de fonte, `Assets/Plugins/Android/RefreshTokenCipher.java`
(pacote `com.anathema.net`), com três métodos estáticos chamados pelo adaptador
C# `AndroidKeystoreRefreshTokenVault` via `AndroidJavaClass`:

- `save(Context, String)`, `read(Context) → String|null`, `delete(Context)`.
- Chave AES-256 no provedor `AndroidKeyStore`, alias
  `anathema_refresh_token_v1`, criada na primeira gravação com
  `KeyGenParameterSpec` (`PURPOSE_ENCRYPT | PURPOSE_DECRYPT`, `BLOCK_MODE_GCM`,
  `ENCRYPTION_PADDING_NONE`, sem exigir autenticação do usuário). Disponível
  desde a API 23, o `minSdk` do projeto.
- `AES/GCM/NoPadding`, IV de 12 bytes gerado pelo provedor, gravado junto:
  `iv || ciphertext`.
- Arquivo em `context.getNoBackupFilesDir()/refresh_token.bin`. O Auto Backup do
  Android (ligado por padrão) restauraria um arquivo de `filesDir` num aparelho
  novo sem a chave do Keystore, que não sai do aparelho; fora do backup, o caso
  nem acontece. Se acontecer por outro caminho, a leitura falha e vira
  `Unreadable` (FR-023).
- Exceção Java (`AndroidJavaException` no C#) → `Unreadable`/`Failed` com o nome
  da exceção, nunca com o valor.
- O contexto vem de `UnityEngine.Android.AndroidApplication.currentContext`
  (existe no 6000.2.8f1), que funciona com a entrada GameActivity.
- As chamadas saem da thread principal, que já está anexada à JVM.

**Minificação**: `ProjectSettings.asset` não tem `minify` ligado hoje. Mesmo
assim entra `Assets/Plugins/Android/proguard-user.txt` com
`-keep class com.anathema.net.RefreshTokenCipher { public static *; }`, e
`useCustomProguardFile` passa a 1: a classe só é alcançada por JNI, e o R8 a
removeria no dia em que alguém ligar a minificação do release.

**Alternativas**:
- JNI inteiro em C# com `AndroidJavaObject` (KeyStore, KeyGenerator, Cipher,
  arrays de `sbyte`): cerca de quinze travessias por operação, com tipos Java
  soltos em C#.
- `EncryptedSharedPreferences` (androidx.security-crypto): dependência Gradle
  nova, e a biblioteca foi descontinuada pelo Google.
- `SharedPreferences` em texto puro: proibido.

---

## R5. Renovação reativa e semântica das recusas

**Decisão**:

| Situação | O que acontece |
|---|---|
| Rota autenticada responde 401 com o token T que a sessão ainda tem | renova (compartilhando renovação em curso); `Renewed` → repete uma vez; `SessionExpired`/`NoSession` → `SessionUnavailable`; `Unavailable` → falha correspondente |
| 401 com token T, mas a sessão já tem outro token | repete uma vez com o atual, sem renovar |
| A repetição responde 401 | devolve como resposta 401; o serviço mapeia para recusa não reconhecida (spec Assumptions) |
| Renovação: 200 com `access` legível | `Renewed` |
| Renovação: 401 | `SessionExpired`: apaga guarda, aumenta geração, emite `SessionExpired` uma vez |
| Renovação: outro status (400 por corpo ruim, 5xx) | `Unavailable(ServerStatus)` sem expirar |
| Renovação: falha de transporte | `Unavailable(Transport)` sem expirar |
| Renovação: 200 ilegível | `Unavailable(OutOfContract)` sem expirar |

O `TokenRefreshView` do SimpleJWT responde 401 (`InvalidToken`) para refresh
vencido, malformado ou de usuário inativo, e 400 só para corpo sem `refresh`
(erro do cliente). Por isso só 401 expira.

**Alternativas**: expirar em qualquer 4xx da renovação: um 400 por bug do cliente
deslogaria todo mundo.

---

## R6. Onde mora cada coisa

**Decisão**:

| Assembly | O que ganha | Por quê |
|---|---|---|
| `Anathema.Net.Core` | `DeckId`, `CardId` e extensões de leitura/escrita; `IRefreshTokenVault` e desfechos; `DeckProblem` e `DeckProblemUnion` | identidades e portas moram no núcleo desde a 001; `DeckProblem` precisa ser lido pelo socket de fila da feature 3 sem depender de HTTP (FR-035) |
| `Anathema.Net.Account` (nova, `noEngineReferences`, só `Core`) | sessão, tokens, renovação, cliente autenticado, cadastro, perfil, catálogo, decks, histórico | orquestração da sessão é uma camada própria (princípio VI); não conhece Newtonsoft (lê por `IPayloadReader`) |
| `Anathema.Net.Unity` | `DpapiRefreshTokenVault`, `AndroidKeystoreRefreshTokenVault`, `PlatformRefreshTokenVault`, `RefreshTokenVaultSlot`, `LiveAccountServices` | adaptadores e composição real |
| `Anathema.Config` | rotas novas e `AccountRoutes` | `AppConfig` evolui no lugar |
| `Anathema.Net.Fakes` | `FakeRefreshTokenVault`; `HoldNext` no `FakeHttpTransport` | fakes reutilizáveis (FR-020) |
| `Assembly-CSharp` | ponte em `PlayerSession`, `TokenRefreshService`, `SelfProfileService`; `LoginController` religado | código anterior evolui no lugar |

O teste de fronteira da 001 (`CoreAssemblyBoundaryTests`) ganha o mesmo teste para
`Anathema.Net.Account`, incluindo proibir `UnityEngine.Networking`.

**Alternativas**: pôr a sessão em `Anathema.Net.Core` mistura porta com
orquestração e faria o núcleo crescer para as features 3 a 5.

---

## R7. Ponte de compatibilidade com o código antigo

**Decisão**:

- `PlayerSession` (na `BootstrapScene`, `executionOrder 300`, depois do
  `AppEnvManager` em 100) passa a ser a raiz de composição do jogo:
  - no `Awake`, cria `MainThreadQueue`, `UnityConsoleLog`, `LiveNetworkAdapters`,
    o `NetworkLayerHost` no próprio `GameObject` e `LiveAccountServices` com as
    `AccountRoutes` de `AppEnvManager.Settings`;
  - expõe `Account` (`LiveAccountServices`);
  - mantém `Instance`, `Nickname`, `Icon`, `Level`, `Experience_points`, `Coins` e
    `Credits` para o `MiniPlayerProfile`;
  - `Token` devolve o texto do token de acesso atual, ou nulo;
  - saem `RefreshToken`, `SetTokens` e `SetAccessToken`;
  - `SetProfile` passa a receber `OwnProfile`.
- `TokenRefreshService` vira classe comum (sai de `MonoBehaviour`, não está em
  cena), com o mesmo `Instance` e o mesmo `Refresh(Action<TokenRefreshResult>)`.
  Ela chama `IAccessTokenSource.RenewNowAsync()`, que renova **sem** olhar a
  margem, porque o `BaseClient` só pede renovação depois de um 4001. O
  mapeamento é:
  - `Renewed` → `Success`;
  - `SessionExpired`/`NoSession` → `Expired`;
  - `Unavailable` → `NetworkError`.
  O enum `TokenRefreshResult` não muda.
- `SelfProfileService` mantém o componente na cena. `LoadProfile(string token)`
  vira `LoadProfileAsync()`, que usa `OwnProfileQuery` e grava na `PlayerSession`.
- `BaseClient` não muda: lê `PlayerSession.Instance.Token` e chama
  `TokenRefreshService.Instance.Refresh` como hoje.

**Dívida registrada** (plano e `Game/TODO.md`): os singletons `Instance`, a leitura
síncrona de `Token` e o `TokenRefreshService` saem quando a feature 3 religar
`BaseClient` em `IAccessTokenSource`.

**Alternativas**: um `MonoBehaviour` novo de composição: dois objetos donos da
mesma sessão na mesma cena, contra "evolui no lugar".

---

## R8. Catálogo tolerante a carta ruim

**Decisão**: `CatalogReader` lê `cards` como lista de objetos e decodifica cada
item isoladamente, com captura de `PayloadShapeException` por item:

- `card_type` fora de `unit`/`spell` → omite e registra `catalog_card_skipped`
  (`card_id` se legível, `reason=unknown_card_type`, `card_type`).
- Campo obrigatório ausente ou de tipo errado → omite e registra com `field`.
- `card_id` repetido → mantém o primeiro e registra `catalog_card_duplicated`.
- `target_kind`/`duration` fora do conjunto → carta mantida com
  `SpellTargetKind.Unknown`/`SpellDuration.Unknown` e o texto original; registra
  `catalog_effect_value_unknown`.
- `cards` ausente ou que não é lista → a busca inteira é `OutOfContract`.

`card_type` é discriminador de forma e usa `DiscriminatedUnion<CatalogCard>` com
braço desconhecido que vira "omitir". `target_kind` e `duration` não mudam a
forma, então são enum com `Unknown` e texto preservado (princípio IV).

**Alternativas**: falhar o catálogo inteiro por uma carta: um feitiço novo no
backend derrubaria todo cliente antigo.

---

## R9. Recusas de deck e rotas

**Decisão**: `DeckRefusalReader` inspeciona o corpo 400 por chave, sem olhar
texto, e junta tudo que reconhece:

| Chave presente | Motivo |
|---|---|
| `name` (lista de textos) | `InvalidDeckName` |
| `deck_problems` (lista de objetos) | `DeckListRejected` com `DeckProblemUnion` |
| `deck_limit` (lista de textos) | `DeckLimitReached` |
| `detail` (texto), só em criação | `MissingDeckField` |
| nenhuma das acima, ou corpo não JSON | `UnrecognizedDeckRefusal(status, corpo)` |

- 404 em qualquer operação → `DeckNotFound`.
- `DELETE` 204 tem corpo vazio: sucesso sem decodificar.
- `DeckChange` só é construído com nome, lista ou os dois
  (`ArgumentException` com os valores recebidos): o `PATCH` vazio é pedido sem
  sentido, não validação de deck.
- A lista vai como veio, sem checar tamanho, cópias ou catálogo (FR-036).

---

## R10. Histórico

**Decisão**:

- `HistoryPageRequest(page, pageSize)` rejeita valores < 1 com
  `ArgumentOutOfRangeException` (valor e forma); monta
  `?page=N&page_size=M`.
- `HasNext`/`HasPrevious` = campo `next`/`previous` não nulo; a URL não é usada,
  porque o host dela é o que o servidor enxerga (atrás de proxy, o do container).
- 404 com página > 1 → `HistoryPastTheEnd`; com página 1 → `HistoryNoProfile`.
- `opponent` nulo → `Opponent == null`; objeto → `HistoryOpponent(UserId,
  nickname, icon, level)`. Aqui `user_id` é inteiro, não texto (contrato 012).
- `end_reason` fora do conjunto → `MatchEndReason.Unknown` com o texto e log
  `history_end_reason_unknown`.
- `ended_at` fica como `EndedAtText` (texto ISO do servidor), só para exibir.

---

## R11. Testes LiveServer

**Decisão**: `LiveAccountTests` (`[Explicit]`, `[Category("LiveServer")]`) em
`Anathema.Net.Unity.Tests/LiveServer`, no padrão da 001 (`[UnityTest]` que drena
a `MainThreadQueue` a cada tick), com `LiveNetworkAdapters` e `LiveAccountServices`
reais apontando para `http://127.0.0.1:8000`:

1. cadastra `acct_<12 hex>`, entra, lê perfil (apelido = usuário);
2. catálogo: 29 cartas, 24 unidades, 5 feitiços;
3. decks: o inicial existe e nenhum `CardId` dele é feitiço no catálogo;
4. cria o deck de feitiços de `scripts/smoke_match.py` (`spell_deck_id`: 1 a 8
   com 3 cópias, 9 uma vez, 1001 a 1005 com 3 cópias) → sucesso; renomeia; cria
   com 12 cartas → `WrongDeckSize(12, 40)`; apaga; lê → `DeckNotFound`;
5. histórico da conta nova: `Count == 0`;
6. `RenewNowAsync` → `Renewed`; perfil lido com o token novo;
7. segunda `LiveAccountServices` com a mesma guarda DPAPI (slot de teste
   `live-test-<guid>` em pasta temporária) → `ResumeAsync` → `Resumed` com o
   mesmo `UserId`.

A guarda de teste usa pasta temporária apagada no `TearDown`, para não tocar a
guarda real do editor.

---

## R12. Credencial fora do log

**Decisão**:

- `AccessToken`, `RefreshToken` e `Password` são tipos com `ToString()` que
  devolve `access_token=<redacted>`, `refresh_token=<redacted>` e
  `password=<redacted>`. O texto só sai por `RevealForRequest()`, usado apenas
  por quem monta o cabeçalho ou o corpo HTTP.
- Resposta fora do contrato registra status, `DecodeFailure.Kind` e `Path`, nunca
  o corpo de login ou de renovação (que contém tokens).
- `AccountCredentialLeakTests` roda todos os cenários com o `FakeClientLog` e
  falha se algum campo de alguma entrada contiver a senha, o acesso ou o refresh
  do cenário (SC-003). Um teste de arquitetura falha se algum tipo de
  `Anathema.Net.Account` ou de `Assembly-CSharp` citar `PlayerPrefs`.

---

## R13. Retomada e login pelo `LoginController`

**Decisão**:

- `LoginController.Start` desliga o botão e chama `ResumeAsync`:
  - `Resumed` → `SelfProfileService.LoadProfileAsync()` → `HomeScene`;
  - `NothingStored`, `Refused` ou `Unavailable` → liga o botão e, no editor ou
    em build de desenvolvimento, faz o login automático do jogador virtual por
    tag, como hoje.
- `HandleLogin` chama `SignInAsync`. No sucesso lê o perfil (falha é registrada e
  não impede) e carrega `HomeScene`.
- Um único `async void` por evento de UI, com `try/catch` que registra no
  `IClientLog`; o resto é `async Task`.
- Saem `using UnityEditor`, `UnityWebRequest`, `System.Text` e os `Debug.Log*`
  (princípio "log por interface"). Os textos com codificação quebrada
  (`Usu�rio`) somem junto com os `Debug.Log` que os continham.
- **Não** entra reação visual a `SessionExpired`, como voltar para a
  `LoginScene`: é UI fora do escopo. O evento é registrado no log, e o item vai
  para `Game/TODO.md`.

**Alternativas**: retomar no `PlayerSession.Awake` e pular a `LoginScene`: muda o
fluxo de cenas, e a retomada com falha de rede precisaria de tela própria.
