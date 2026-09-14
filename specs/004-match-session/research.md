# Research: Partida

**Feature**: `004-match-session` | **Date**: 2026-09-14

Cada item: decisão, por quê, alternativas. As formas das mensagens são as dos
contratos do backend citados na spec e não são repetidas.

---

## R1. Onde mora a partida

**Decisão**: assembly nova `Anathema.Net.Match` (`noEngineReferences: true`) em
`Assets/Scripts/Net/Match/`, referenciando `Anathema.Net.Core`,
`Anathema.Net.Account` e `Anathema.Net.Connection`. Pastas por responsabilidade:

- `Protocol/`: visão, lados, carta, unidade, modificadores, combate, desfecho,
  relógio do frame, eventos, frames e `MatchFrames.CreateUnion()`;
- `Commands/`: as 11 mensagens, `MatchCommands`, `PendingPlay`, resultado de envio;
- `Refusals/`: `PlayRefusal`, `PlayRefusalCode`, `PlayRefusalReader`;
- `Mirror/`: `MatchMirror`, `VersionGate`, avisos e `DisplayedUnitStats`;
- `Clock/`: `TurnClock`;
- `Hints/`: `HandCardHints`;
- `Session/`: `LiveMatch`, estados da sessão e `MatchNarrator`.

`Anathema.Net.Unity` passa a referenciar `Anathema.Net.Match`, porque o codec
único é montado em `LiveNetworkAdapters`.

**Por quê**: a partida precisa do catálogo (conta) e da conexão autenticada
(conexão). Nem o núcleo nem a conexão podem depender dela. O princípio VI prevê
"estado espelhado da partida" como camada própria. A exigência "espelho sem
socket" é cumprida pelo desenho: `MatchMirror`, `TurnClock`, `HandCardHints`,
`DisplayedUnitStats` e os leitores não recebem nada da conexão no construtor, e
um teste de reflexão verifica isso (`MatchCoreIsolationTests`).

**Alternativas**:
- Duas assemblies (estado sem conexão / sessão com conexão): a fronteira seria
  verificada pelo compilador, mas a sessão é uma classe e meia, com um
  consumidor só. A 003 recusou a mesma separação para a fila pela mesma razão; a
  feature 5 separa se a fachada pedir.
- Partida dentro de `Anathema.Net.Connection`: mistura transporte com estado de
  jogo, e a conexão passaria a depender do catálogo.
- Formas do protocolo no núcleo: o núcleo passaria a conhecer `SpellDuration` e
  `MatchEndReason`, que moram na conta.

---

## R2. Formas do protocolo

**Decisão**: tipos imutáveis com `static Read(IPayloadReader)`, como os frames da
003. Uniões por `DiscriminatedUnion`:

| União | Campo | Braço desconhecido |
|---|---|---|
| `DiscriminatedUnion<ServerFrame>` (codec) | `type` | `UnknownServerFrame` (existente) |
| `DiscriminatedUnion<MatchEvent>` | `kind` | `UnrecognizedMatchEvent(kindText)` |
| `DiscriminatedUnion<UnitModifier>` | `modifier_kind` | `UnrecognizedModifier(kindText, duration)` |

Valores fechados sem corpo próprio viram enum com `Unknown` e o texto original ao
lado (`MatchPhase` + `PhaseText`, motivo do desfecho + `ReasonText`, duração +
`DurationText`), no molde de `SpellEffect.TargetKind`/`TargetKindText`.

Reaproveitados da 002, sem cópia: `MatchEndReason` (`nexus_depleted`, `forfeit`,
`Unknown`, hoje em `Account/History`) e `SpellDuration` (`permanent`,
`until_end_of_round`, `Unknown`, em `Account/Catalog`), que é a mesma
`EffectDuration` do backend. O mapeamento de texto hoje privado em
`MatchHistoryRow` e `SpellEffect` é extraído para leitores públicos pequenos
(`MatchEndReasonText.Parse`, `SpellDurationText.Parse`) em `Anathema.Net.Account`,
usados pela 002 e pela partida. Sem lógica duplicada (princípio V), e sem abrir
os internos da conta a uma assembly de produção.

Outras decisões de forma:

- `version` é `long`. Identificadores por `PayloadIdentityReading`; entram no
  núcleo `ReadCardInstanceIdList` e `WriteCardInstanceIdList` (hoje só existe a
  lista de `CardId`).
- O perfil de um lado é tipo novo `MatchProfile` (`UserId`, apelido, ícone,
  nível). `PairedPlayer` tem os mesmos campos, mas nome de outro momento
  ("pareado") e mora na fila; acoplar a partida a ele trocaria clareza por uma
  classe de quatro propriedades.
- Campo obrigatório faltando ou de tipo errado em qualquer ponto do frame lança
  `PayloadShapeException` com o caminho: o codec já registra
  `connection_frame_invalid` e descarta o frame inteiro (FR-009). Valor
  desconhecido de fase, `kind`, `modifier_kind`, duração ou motivo **não** é
  falha.
- `clock` ausente (formato anterior à 010) lê como `ClockView.Empty`.
- `cards_drawn` com `cards` vazia é caso normal, não falha.

**Alternativas**:
- `enum` para eventos: evento tem corpo diferente por `kind`; um enum levaria de
  volta a dicionário de campos.
- Propriedade `PhaseText` só no desconhecido: duas formas de ler a fase conforme o
  valor. O texto ao lado sempre é o que o catálogo já faz.

---

## R3. União de frames do codec

**Decisão**: `MatchFrames.CreateUnion()` = `ConnectionFrames.CreateUnion()` +
`match_start` + `match_update` + `turn_warning`. `LiveNetworkAdapters` troca a
chamada. `message_refused` continua o `MessageRefusedFrame` genérico da 001; a
partida só lê o `code` dele com `PlayRefusalReader`, como `QueueRefusalReader`
faz na fila.

**Por quê**: um codec só para conta e os dois sockets (research R1 da 003). O
`match_start` passa a sair tipado da própria conexão, o que remove a ponte
`RawTextReceived` (R12).

**Alternativas**: codec separado para a conexão de partida — duas instâncias do
mesmo codec só para registrar três braços, e `LiveConnectionServices` teria de
saber qual socket usa qual.

---

## R4. Regra de versão e ordem dos avisos

**Decisão**: `VersionGate` decide antes de qualquer mudança:

| Frame | Versão frente à última aplicada | Veredito |
|---|---|---|
| `match_start` ou `match_update`, espelho vazio | qualquer | `Accept` |
| `match_start` ou `match_update` | maior | `Accept` |
| `match_start` | igual | `ResyncSameVersion` |
| `match_update` | igual ou menor | `Discard` |
| `match_start` | menor | `Discard` |

`LiveMatch` aplica cada veredito nesta ordem:

1. `Discard`: registra `match_frame_discarded` (debug) e para. Espelho, relógio e
   pendente não mudam.
2. `ResyncSameVersion`: reancora o relógio no instante de chegada; a sessão volta
   a ao vivo; nenhum aviso do espelho.
3. `Accept`:
   - o relógio guarda a nova âncora sem avisar;
   - o pendente é limpo, antes de qualquer aviso: quem responde ao estado novo
     mandando um comando (a apresentação ou o bot do marco) não pode tê-lo apagado
     logo depois (ajuste da implementação);
   - o espelho substitui o estado e emite, em ordem: estado substituído, cada
     evento, fase mudou, prioridade mudou, partida terminou;
   - o relógio emite os seus avisos (vez nova, tempo acabando);
   - a sessão atualiza o próprio estado.

Assim quem assina "estado substituído" já lê o relógio novo, e os avisos do
relógio chegam depois do estado que os explica.

`turn_warning` não tem versão e não passa pelo `VersionGate` (R6).

Cada aviso é chamado dentro de um `try/catch` que registra
`match_subscriber_failed` e segue (FR-015). O estado já foi substituído antes do
primeiro aviso, então uma exceção nunca deixa o espelho meio aplicado.

**Por quê**: FR-010, FR-011, FR-014, FR-021. Decidir antes de mudar é o que
permite "frame descartado não mexe no relógio" sem desfazer nada.

**Alternativas**:
- O espelho dono do relógio: o espelho passaria a conhecer tempo, e a spec os
  separa (histórias 2 e 3).
- Avisar o relógio antes do espelho: "vez nova" chegaria antes do estado que
  mostra de quem é a vez.

---

## R5. Instante de chegada do frame

**Decisão**: o instante de chegada é `IMonotonicClock.Now` lido por `LiveMatch`
no momento em que a conexão entrega o frame na thread principal, antes de
qualquer outro trabalho com ele.

**Por quê**: o `DotNetWebSocket` monta o texto fora da thread principal e o
enfileira na `MainThreadQueue`, que o `NetworkLayerHost` drena no início de cada
`Update`. A diferença entre recepção e entrega é de um quadro, e só cresce se a
thread principal travar. O único travamento previsível do fluxo é o
`SceneManager.LoadScene` da `MatchScene`, e ele roda **dentro** do tratamento do
primeiro `match_start`, depois de o relógio já estar ancorado. A `VersusScene`
carrega no `match_found`, antes de o socket de partida abrir. No Android, em
segundo plano, o socket não recebe nada. O erro possível é mostrar um quadro a
mais de tempo, nunca a menos, e o servidor continua sendo quem estoura a vez
(FR-022).

**Alternativas**:
- Carimbar o instante no adaptador de socket: exige mudar `IWebSocket.TextReceived`
  (porta da 001), o `FakeWebSocket`, o `SocketAttempt` e a assinatura de
  `FrameReceived` de todos os consumidores, para ganhar um quadro. Fica anotado no
  `Game/TODO.md` como caminho, caso um travamento novo apareça entre recepção e
  entrega.
- Carimbar no `MainThreadQueue.Enqueue`: a fila é genérica e não conhece relógio.

---

## R6. Relógio da vez

**Decisão**: `TurnClock(IMonotonicClock)` guarda a vez desenhada (`TurnView` e a
âncora), o último `turn_number` avisado e o prazo do mulligan com a própria âncora.

- `Anchor(ClockView, chegada)`: vez nula apaga a vez; `turn_number` diferente do
  desenhado marca "vez nova"; `warning` verdadeiro com `turn_number` ainda não
  avisado marca "tempo acabando". Prazo de mulligan nulo apaga o relógio de
  mulligan.
- `NoteWarning(TurnWarningFrame, chegada)`: `turn_number` diferente do desenhado,
  ou sem vez desenhada, registra `turn_warning_ignored` e para. Igual: reancora
  com o `remaining_ms` do aviso e marca "tempo acabando" se ainda não saiu.
- `TurnRemaining` e `MulliganRemaining`: `remaining_ms − (agora − âncora)`, preso
  em zero, ou nulo sem relógio.
- Nenhum temporizador: o relógio só calcula quando alguém lê. Nada acontece em
  zero.

**Por quê**:
- `turn_warning` só vai ao dono da vez, e quem reconecta depois do aviso não o
  recebe de novo; o `warning` do frame cobre os dois casos (spec, Assumptions).
- O `turn_warning` da vez desenhada traz um `remaining_ms` mais recente que o do
  último frame.
- Sem temporizador não há o que estourar no cliente (FR-022), e o teste só
  avança o `FakeMonotonicClock`.

**Alternativas**: um evento "chegou a zero" movido pelo `IFrameTicker` — é
exatamente a regra local que a spec proíbe, e a apresentação desenha lendo o
restante a cada quadro de qualquer jeito.

---

## R7. Comandos, pendente e recusa

**Decisão**:

- Uma classe por mensagem (`MulliganCommand`, `PlayUnitCommand`, …), todas
  derivadas de `PlayCommand : IOutgoingMessage`, com construtor que só aceita
  `CardInstanceId` (ou lista dele). `CastSpellCommand` sem alvo **omite**
  `target_card_instance_id`: `IPayloadWriter` não escreve `null`, e o exemplo do
  contrato 010 omite.
- `MatchCommands` expõe os 11 comandos, com dois métodos para feitiço
  (`CastSpell`, `CastSpellAt`), e devolve `Task<PlaySendResult>`:
  1. conexão fora de `Connected` → `NotConnected` com a fase, sem tocar no socket
     (FR-024);
  2. senão marca o pendente e chama `SendAsync`;
  3. `SocketSendOutcome` diferente de `Sent` → `SocketFailed`, pendente
     desmarcado;
  4. `Sent` → `Sent`.
- `PendingPlay` guarda duas coisas diferentes:
  - `Current`, o pendente para a apresentação, limpo por atualização aceita,
    recusa ou `Recovered` (FR-025);
  - `LastSentSinceUpdate`, o último comando enviado desde a última atualização
    aceita, limpo só por atualização aceita.
  A recusa é associada a `LastSentSinceUpdate`: uma recusa que chega depois de
  um `Recovered` ainda aponta para o comando que a causou.
- `PlayRefusalReader` lê `MessageRefusedFrame.Code` numa tabela fechada dos 31
  códigos do contrato 009; código fora dela vira `PlayRefusalCode.Unknown` com o
  texto preservado. `Error` só vai para `PlayRefusal.Error` e para o log.
- "`CardId` não compila onde se espera `CardInstanceId`" é garantido pelos tipos
  da 001 (sem conversão entre eles, `IdentityIsolationTests`). O teste desta
  feature (`CommandIdentityTests`) verifica por reflexão que nenhum parâmetro
  público de `MatchCommands` nem construtor de `PlayCommand` é `CardId`, `long`
  ou `int`.

**Por quê**: FR-023 a FR-027. Separar pendente de "último enviado" resolve o caso
da recusa depois da reconexão sem inventar eco que o servidor não manda.

**Alternativas**:
- Uma classe genérica `PlayCommand(type, Action<IPayloadWriter>)`: onze formas num
  lambda, sem tipo para testar nem para a apresentação reconhecer.
- Bloquear envio com pendente: a spec diz que o pendente não bloqueia.
- Teste de compilação negativa com Roslyn na suíte: o Unity Test Framework não
  compila código em teste. A reflexão cobre a mesma garantia na superfície
  pública.

---

## R8. Dicas de mira e atributos exibidos

**Decisão**:

- `HandCardHints.For(CardInstanceId, PlayerView, LoadedCatalog)` → `HandCardHint`
  com:
  - `Kind`: `Unit`, `Spell`, `UnknownCard` ou `NotInHand`;
  - custo (`CatalogCard.Energy`) e energia atual;
  - para feitiço: `SpellTargetKind` do catálogo, candidatos (cópias do banco
    daquele lado, na ordem do banco), `DeclarationOnly` e a fase atual.
- `DisplayedUnitStats.Of(BankUnit, LoadedCatalog)`: ataque e vida base da
  `UnitCard`, somando os modificadores de ataque e vida e descontando
  `DamageTaken` da vida. Nulo quando o `CardId` não é unidade no catálogo. O
  `/// <summary>` diz "conveniência de exibição; nada decide por ele".
- `MatchCommands` não recebe catálogo, espelho nem dicas no construtor
  (`CommandsIgnoreHintsTests` verifica os tipos dos campos por reflexão).

**Por quê**: FR-016, FR-028 a FR-030, SC-008. `NotInHand` existe para uma cópia
que saiu da mão entre o clique e a consulta não virar exceção.

**Alternativas**: uma dica "pode jogar?" que compara custo e energia — é regra
de jogo (legalidade), e a spec pede os dois números lado a lado.

---

## R9. Sessão de partida

**Decisão**: `LiveMatch` é criada por partida e assina a `AuthenticatedConnection`
de partida (uma por app, da 003). Estados:

| Situação | Estado | Desatualizado |
|---|---|---|
| `Start()` | `Connecting` | não |
| `Connected` antes do primeiro `match_start` | `Connecting` | não |
| `match_start` aceito ou `ResyncSameVersion` | `Live` | não |
| `Connecting`, `WaitingRetry`, `RenewingToken` ou `Suspended` depois de `Live` | `Reconnecting` | sim |
| `GaveUp` com `GiveUpKind.MatchRefused` | `Refused` (com o `GiveUpReason`) | mantém |
| `GaveUp` com outro motivo | `GaveUp` (com o `GiveUpReason`) | sim |
| frame aceito com fase `finished` | `Finished` (com desfecho) | não |

- `Finished` chama `connection.Leave()` depois dos avisos do espelho e ignora
  qualquer mudança de conexão seguinte.
- `Refused`, `GaveUp` e `Finished` são terminais: frame que chegue depois é
  ignorado.
- `Recovered` limpa o pendente.
- `Dispose()` sai da conexão, cancela as assinaturas e cala todos os avisos.
- `match_denied` já vira `GaveUp` com `MatchRefused` dentro da conexão (003).
- `message_refused` → `Refused(PlayRefusal)` e log.
- `turn_warning` → `TurnClock.NoteWarning`.
- Outro frame → `match_frame_unexpected` (debug).
- Um `match_start` em fase `finished` depois de reconectar leva direto a
  `Finished`.

**Por quê**: FR-031 a FR-035. Uma `LiveMatch` por partida faz o espelho nascer
vazio a cada partida, sem `Clear()` para esquecer.

**Alternativas**:
- Uma sessão por app com `Reset`: estado de uma partida vazaria para a próxima se
  um `Reset` faltasse.
- Conexão própria dentro da sessão: duplicaria a composição da 003.

---

## R10. Narrador

**Decisão**: cada `MatchEvent` expõe `Details` (lista de `EventDetail`: nome do
campo do contrato e um valor tipado — `UserId`, `MatchCard`, `CardInstanceId`,
lista de `CardInstanceId`, número, texto). `MatchNarrator` assina
`MatchMirror.EventReceived` e escreve `IClientLog.Info("match_event", …)` com:

- `round`: o `round_number` do estado já substituído;
- `kind`: o texto do `kind`;
- um campo por detalhe, com o nome do contrato:
  - `UserId` vira o apelido lido nos perfis da visão atual;
  - carta vira `nome#card_instance_id`, com o nome do catálogo;
  - lista vira os identificadores separados por vírgula;
  - número e texto saem como vieram.
- sem nome conhecido, sai o `ToString()` do identificador tipado.

Evento desconhecido sai com `kind` e `unrecognized=true`.

**Por quê**: FR-036. Os detalhes pertencem ao evento, que conhece os próprios
campos. O narrador só resolve nomes, sem um `switch` de 19 braços, e evento novo
ganha narração ao declarar os detalhes.

**Alternativas**: `switch` por tipo no narrador — método de 19 braços, quebra o
limite de tamanho, e esquecer um braço não dá erro.

---

## R11. Nomes sem colisão

**Decisão**:

| Conceito da spec | Nome | Ocorrências antes (`grep -rw`) |
|---|---|---|
| Sessão de partida | `LiveMatch`, `LiveMatchPhase`, `LiveMatchStatus` | 0 |
| Espelho | `MatchMirror`, `VersionGate` | 0 |
| Relógio | `TurnClock`, `ClockView`, `TurnView` | 0 |
| Visão | `PlayerView`, `OwnSideView`, `OpponentSideView`, `MatchProfile`, `MatchCard`, `BankUnit`, `CombatView`, `MatchOutcome` | 0 |
| Recusa de jogada | `PlayRefusal`, `PlayRefusalCode` | 0 (`MatchRefusal` é evitado: `MatchRefusalDetail` da 003 é recusa de **gate**) |
| Comando | `PlayCommand`, `MatchCommands`, `PendingPlay`, `PlaySendResult` | 0 |

A classe existente `MatchSession` (MonoBehaviour) mantém o nome e passa a
delegar para a `LiveMatch` atual (R12).

**Por quê**: a spec pede nomes que não colidam (FR-043), e a constituição prefere
nomes com poucas ocorrências.

---

## R12. Código anterior: `MatchClient`, `MatchSession` e a ponte

**Decisão**:

- **`AuthenticatedConnection`**: sai `RawTextReceived`; o `SocketAttempt.FrameArrived`
  perde o texto. Sai o teste `TextoCruAcompanhaCadaFrameAceito`.
- **`MatchClient(AuthenticatedConnection, Uri matchBase, CardCatalog, IMonotonicClock, IClientLog)`**:
  - `Connect(MatchId)` segue síncrono para quem chama e dispara
    `ConnectAsync`:
    1. `await catalog.LoadAsync()`, que devolve na hora o catálogo em cache da
       mesma geração de sessão;
    2. descarta a `LiveMatch` anterior;
    3. cria a nova e chama `MatchSession.Instance.Attach(live)`;
    4. assina o primeiro "estado substituído" para carregar a `MatchScene` se ela
       não for a cena ativa;
    5. chama `live.Start()`.
  - Catálogo indisponível → `match_catalog_unavailable` (erro) e nenhuma sessão.
  - Expõe `LiveMatch? Live`.
  - Saem `JsonConvert`, `MatchStartEnvelope`, `HandleStartMatch` com
    `MatchStateDTO` e o `using Newtonsoft.Json`. O comentário sobre idempotência
    do `match_start` fica, agora no ponto em que a cena só carrega uma vez.
- **`MatchSession`** (MonoBehaviour, singleton sob demanda) mantém o papel, o nome
  e os comentários: a partida em vigor, fora da `MatchScene`.
  - `Attach(LiveMatch)`, `LiveMatch? Live`, `PlayerView? State`, `HasState`;
  - `event Action<PlayerView> OnStateChanged` repassado de
    `MatchMirror.ViewReplaced`;
  - `Clear()` desfaz a ligação.
  - Sai `ApplyState(MatchStateDTO)`.
- **`MatchStateDTO.cs`** (e a pasta `DTO/Match`) sai com `.meta`.
- **`PlayerSession.ComposeSocketClients`** passa `Account.Catalog` e
  `adapters.Clock` ao `MatchClient`.

**Por quê**:
- FR-042 a FR-045.
- O catálogo é pré-condição das dicas e do narrador. A conta já o guarda em
  cache, então esperar por ele no `Connect` não atrasa a abertura quando a Home
  já carregou o catálogo.
- Sem catálogo, a partida não teria nome de carta nem dica; é melhor registrar
  do que abrir uma sessão manca.

**Alternativas**:
- `LiveMatch` aceitando catálogo nulo: todo consumidor de dica e de narração
  passaria a tratar nulo.
- Apagar `MatchSession` e fazer as cenas lerem `PlayerSession.Instance.Match.Live`:
  muda a entrada de cena que a feature 5 vai substituir de qualquer forma, e a
  constituição pede evolução no lugar.
- Carregar o catálogo no botão Jogar: acopla a Home à partida por uma
  pré-condição invisível.

---

## R13. Testes EditMode, fixtures e catálogo de teste

**Decisão**:

- Assembly `Anathema.Net.Match.Tests` (`Core`, `Json`, `Account`, `Connection`,
  `Match`, `Fakes`) em `Assets/Tests/EditMode/Net.Match/`.
- `MatchTestRig` monta a conexão com os fakes da 003 (`FakeWebSocketFactory`,
  `FakeAccessTokenSource`, `FakeFrameTicker`, `FakeMonotonicClock`,
  `FakeAppLifecycle`, `FakeNetworkReachability`, `FakeClientLog`, `MainThreadQueue`)
  e o codec real com `MatchFrames.CreateUnion()`. O `ConnectionTestRig` da 003 é
  interno de outra assembly de teste; os ~20 linhas de montagem se repetem, sem
  lógica.
- **Fixtures**: arquivos `.json` em `Assets/Tests/EditMode/Net.Match/Fixtures/`,
  lidos por `MatchFixtures.Text(nome)` (caminho a partir de `Application.dataPath`,
  permitido na assembly de teste). Duas famílias:
  - `contract-*.json`: montadas a partir dos exemplos dos contratos 009/010 e de
    `documents.py`;
  - `recorded-*.json`: frames reais gravados pelo marco LiveServer (R14),
    escolhidos à mão.
- **Catálogo**: `LoadedCatalog` e `CatalogReader` têm construtor interno. A conta
  ganha `InternalsVisibleTo("Anathema.Net.Match.Tests")`, e `MatchTestCatalog`
  monta um `LoadedCatalog` a partir do JSON do catálogo (mesmo formato do
  `CatalogJson` da 002).
- Tempo só pelo `FakeMonotonicClock`; nenhuma espera real (SC-011).

**Por quê**: princípio VII. Frames reais pegam campo que os exemplos dos contratos
não mostram.

**Alternativas**:
- Frames como `const string` em C#: um frame real de partida passa de 3 KB, e
  arquivos C# com JSON escapado são ilegíveis e estouram o limite de linhas.
- Construir `LoadedCatalog` por `CardCatalog` + `FakeHttpTransport`: sessão de
  conta, cliente HTTP e roteiro de resposta só para ter uma tabela de cartas.
- Construtor público em `LoadedCatalog`: código de produção poderia montar
  catálogo sem leitor (mesmo argumento do research R5 da 003).

---

## R14. O marco LiveServer

**Decisão**: `Assets/Tests/EditMode/Net.Unity/LiveServer/LiveMatchTests.cs`,
`[Explicit]`, `[Category("LiveServer")]`, `[UnityTest]`, no molde de
`LiveQueueTests`.

- **Jogadores**: dois `LivePlayer` (existente) no mesmo processo, com
  `DotNetWebSocket`, `UnityHttpTransport`, relógio e codec reais. Ciclo de vida e
  rede são os fakes da 003.
- **Deck**: `LivePlayer` ganha a criação do deck com feitiços do `smoke_match.py`
  (`card_id` 1–8 ×3, 9, e 1001–1005 ×3), pela criação de deck da 002.
- **Fila**: `MatchQueue` como na 003. Pareados, cada jogador cria a sua
  `LiveMatch` sobre a própria `MatchConnection`, com o catálogo carregado.
- **Bot** (`SmokeBot`, só de teste), a cada estado substituído ou recusa:
  - chama `SmokeStrategy.Next(mirror, hints, catalog, tried)`, que devolve o
    próximo `PlayCommand` na ordem do `smoke_match.py`, e o manda por
    `MatchCommands`;
  - guarda `(versão, tipo, payload)` já tentados, para não repetir na mesma
    versão;
  - "unidade mais barata" ordena a mão pelo custo do catálogo;
  - "Nexus abaixo de 12" para a poção lê o Nexus da visão.
  É estratégia do teste, não regra do cliente.
- **Queda**: na rodada 3, `FakeNetworkReachability.SimulateKind(CarrierData)` no
  P2 recicla o socket real (mesmo caminho da 003); o teste espera
  `LiveMatchPhase.Reconnecting` e depois `Live`.
- **Gravação**: `RecordingWebSocketFactory` (só de teste) embrulha o
  `DotNetWebSocketFactory` do P1 e grava cada texto recebido em
  `Logs/match-recording/<match_id>/NNNN-<type>.json`. Fora de `Assets/`: nada é
  importado nem ganha `.meta` sem escolha humana (quickstart §3).
- **Verificações**:
  - `Finished` nos dois, com desfechos iguais;
  - no máximo 20 recusas por bot e nenhuma `unknown_message_type`;
  - uma linha `match_event` no log de P1 por evento aceito;
  - `connection_gave_up` ausente;
  - menos de 600 s.

**Por quê**: FR-038 a FR-041, SC-009. Embrulhar a fábrica de socket grava o texto
real sem devolver à produção a ponte de texto cru que esta feature remove.

**Alternativas**:
- Gravar pela conexão: exigiria manter `RawTextReceived`.
- Derrubar o socket com `Abort` no adaptador: operação nova em `IWebSocket` só
  para teste (mesma recusa do R14 da 003).

---

## R15. Valores padrão

| Onde | Valor | Origem |
|---|---|---|
| marco | limite de recusas por bot | 20 | `REFUSAL_LIMIT` do `smoke_match.py` |
| marco | rodada da desistência | 30 | `ROUND_CAP` |
| marco | tempo limite | 600 s | `MATCH_TIMEOUT_S` |
| marco | rodada da queda provocada | 3 | escolha: depois do mulligan e de ao menos um combate possível |
| relógio | aviso | `warning` do servidor (≤ 15 s) | contrato 010; o cliente não compara limiar |
| narrador | nível de log | `Info` | `IClientLog` |
