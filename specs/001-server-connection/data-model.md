# Data Model: Conexão com o servidor

**Feature**: `001-server-connection` | **Date**: 2026-09-13

Tipos de valor e estados da camada. As formas das mensagens vêm dos contratos do
backend (`backend/specs/009-match-protocol`, `011-deck-catalog-api`,
`013-socket-heartbeat`) e não são repetidas aqui; este arquivo só descreve como o
cliente as representa.

Todos os tipos são imutáveis, a não ser que se diga o contrário. Todo arquivo com
`#nullable enable`.

---

## Identidade (`Anathema.Net.Core/Identity`)

| Tipo | Valor | No JSON | Validação | Igualdade / log |
|---|---|---|---|---|
| `UserId` | `long` | inteiro cru (`user_id`) | > 0 | por valor; `ToString()` → `user_id=7` |
| `CardInstanceId` | `long` | inteiro cru (`card_instance_id`) | ≥ 0 | por valor; `card_instance_id=3` |
| `MatchId` | `string` | texto cru (`match_id`) | não vazio, sem espaço nas pontas | ordinal; `match_id=…` |

- `readonly struct`, `IEquatable<T>`, sem conversão implícita entre si nem de/para
  `long`/`string`: trocar um pelo outro não compila (FR-038).
- Valor inválido no construtor: `ArgumentOutOfRangeException` com valor recebido e
  forma esperada. Pelo codec, vira `DecodeFailure` (`InvalidValue`).
- Leitura e escrita só por extensões do reader/writer que recebem o nome do
  campo: `ReadUserId("user_id")`, `WriteMatchId("match_id", id)`.

---

## Socket (`Anathema.Net.Core/Socket`)

**`SocketClosure`**
- `int? Code` — número exato do close frame; `null` = queda sem close frame.
- `string Reason` — motivo do servidor, ou descrição local (`abnormal`,
  `closed_before_open`, `cleartext_refused`).
- `bool HasServerCode => Code.HasValue`.

**`SocketSendOutcome`**: `Sent` | `NotOpen` | `Failed(detail)`.

**`CleartextPolicy`**
- `bool AllowsCleartext`.
- `bool Permits(Uri uri)`: `https`/`wss` sempre; `http`/`ws` só se
  `AllowsCleartext`; outro esquema, nunca.

**Estados de uma conexão** (um `IWebSocket` = uma conexão; reabrir = instância nova):

```text
Idle ──Open(url)──► Opening ──handshake ok──► Open ──Close()──► Closing ──► Closed
  │                    │                        │                               ▲
  │                    ├── falha / Close() ─────┼── queda / close do servidor ──┤
  └── Close() ─────────┴────────────────────────┴───────────────────────────────┘
```

| Transição | Avisos, em ordem |
|---|---|
| Opening → Open | `Opened` |
| Open, texto chega | `TextReceived(text)` (0..n) |
| Open, frame binário | `Errored("binary_frame_ignored")`; continua Open |
| qualquer → Closed por close frame | `Closed(code, reason)` |
| qualquer → Closed por falha | `Errored(detail)`, `Closed(null, "abnormal")` |
| Idle/Opening → Closed por `Close()` | `Closed(null, "closed_before_open")` |
| `Open(url)` com esquema proibido pela política | `Errored("cleartext_refused")`, `Closed(null, "cleartext_refused")` |
| Closed | nenhum aviso depois (FR-007) |

`Open` numa conexão que não está em Idle é erro de uso:
`InvalidOperationException` com estado atual e esperado.

---

## HTTP (`Anathema.Net.Core/Http`)

**`HttpRequestSpec`**
- `string Method` (`GET`, `POST`, `PUT`, `PATCH`, `DELETE`)
- `Uri Url` (absoluta)
- `IReadOnlyDictionary<string, string> Headers`
- `string? Body`
- `int TimeoutSeconds` (padrão 10; 1..120)

**`HttpOutcome`**: classe selada com dois casos mutuamente exclusivos.
- `HttpResponse`: `int Status`, `string Body`. Qualquer status que o servidor
  mandou, inclusive 4xx/5xx.
- `TransportFailure`: `TransportFailureKind Kind`, `string Detail`.

**`TransportFailureKind`**: `Timeout`, `HostNotResolved`, `CannotConnect`,
`CleartextRefused`, `Other`.

---

## Tempo (`Anathema.Net.Core/Time`)

**`MonotonicInstant`**
- `long Ticks` (100 ns, origem arbitrária e fixa por execução).
- `TimeSpan operator -(MonotonicInstant later, MonotonicInstant earlier)`.
- Sem conversão para `DateTime`. Comparável.

Invariante do adaptador: leituras sucessivas nunca diminuem, e o tempo com o
aparelho dormindo conta (research R1).

---

## Ciclo de vida (`Anathema.Net.Core/Lifecycle`)

**Avisos**: `WentToBackground(MonotonicInstant at)` e
`ReturnedToForeground(MonotonicInstant at, TimeSpan awayFor)`.

**`LifecycleSignalFilter`** (estado mutável interno)
- Configuração: `bool focusLossStopsPlayer`.
- Entradas: `OnPause(bool paused)`, `OnFocus(bool focused)`.

```text
Foreground ──pause(true) | [focusLossStopsPlayer] focus(false)──► Background
Background ──pause(false) | focus(true)──────────────────────────► Foreground (awayFor = now - at)
```

Sinal que não muda o estado é ignorado. O estado inicial é Foreground. Nunca sai
`ReturnedToForeground` sem um `WentToBackground` antes (FR-017).

---

## Alcançabilidade (`Anathema.Net.Core/Reachability`)

- **`NetworkKind`**: `None`, `LocalArea`, `CarrierData`.
- **`NetworkKindChanged`**: `NetworkKind Previous`, `NetworkKind Current`.
  Sempre `Previous != Current`.

---

## Log (`Anathema.Net.Core/Logging`)

- **`ClientLogLevel`**: `Debug`, `Info`, `Warning`, `Error`.
- **`LogField`**: `string Name` (snake_case), `string Value`.
- **`ClientLogEntry`**: `ClientLogLevel Level`, `string EventName` (snake_case,
  estável), `IReadOnlyList<LogField> Fields`.

O adaptador real formata `event_name key=value key=value`.

---

## Fila da thread principal (`Anathema.Net.Core/Threading`)

**`MainThreadQueue`** (estado mutável, thread-safe)

```text
Open ──Close()──► Closed
```

| Estado | `Enqueue(action)` | `Drain()` |
|---|---|---|
| Open | aceita | entrega os itens presentes no início da chamada, em ordem; exceção → log, segue |
| Closed | descarta, sem exceção | não faz nada |

`Close()` descarta os pendentes. É idempotente.

---

## Protocolo (`Anathema.Net.Core/Protocol`)

**`DecodeOutcome<T>`**: `IsValid`, `T Value` (só se válido),
`DecodeFailure Failure` (só se inválido).

**`DecodeFailure`**
- `DecodeFailureKind Kind`
- `string Path` — caminho do campo, ex.: `payload.deck_problems[0].kind`
- `string Detail` — valor recebido e forma esperada

**`DecodeFailureKind`**: `NotJson`, `NotObject`, `MissingType`, `TypeNotText`,
`PayloadNotObject`, `MissingField`, `WrongFieldType`, `InvalidValue`.

**`PayloadShapeException`**: carrega um `DecodeFailure`. Lançada pelos métodos
`Read*` do reader e capturada em `DiscriminatedUnion`. Nunca sai do codec.

**`DiscriminatedUnion<TBase>`** (mutável só durante o registro)
- Construída com `string discriminatorField` e
  `Func<string, TBase> unknownArm`.
- `Register(string value, Func<IPayloadReader, TBase> arm)`. Valor repetido:
  `ArgumentException`.
- `DecodeBody(string value, IPayloadReader body)` — discriminador fora do corpo
  (o `type` do envelope).
- `DecodeObject(IPayloadReader objectWithDiscriminator)` — discriminador dentro do
  objeto (`kind`, `modifier_kind`).
- Valor sem braço → `unknownArm(value)`. Discriminador ausente ou que não é texto
  → falha.

**Mensagens que saem**
- `IOutgoingMessage`: `string MessageType`, `void WritePayload(IPayloadWriter)`.
- `PingMessage`: `PingMarker? Marker`. Sem marcador, sai com `payload` vazio (`{}`). Com
  marcador, sai com `{"sent_at_ms": …, "ping_seq": …}`.
- `PingMarker`: `long SentAtMs`, `long Sequence`.

**Frames que chegam** (hierarquia fechada; `ServerFrame` é abstrata com construtor `internal`/protegido)

| Tipo | `type` | Campos | Origem |
|---|---|---|---|
| `MessageRefusedFrame` | `message_refused` | `string Code`, `string Error`, `IPayloadReader Details` (payload inteiro, só leitura) | 009 `refusal_codes.md`; extras em 011 `matchmaking_messages.md` |
| `AuthDeniedFrame` | `auth_denied` | `string Error` | 013 `heartbeat_messages.md` (gate 4001) |
| `PongFrame` | `pong` | `PingMarker? Marker` — presente só se o eco tem `sent_at_ms` e `ping_seq` inteiros | 013 `heartbeat_messages.md` |
| `UnknownServerFrame` | qualquer outro | `string MessageType` | — |

`GenericServerFrames.CreateUnion()` devolve uma `DiscriminatedUnion<ServerFrame>`
com esses três braços registrados. As features seguintes registram os seus nela,
antes de construir o codec.

---

## Ambiente (`Anathema.Net.Core/Environment` e `AppConfig`)

**`ServerHost`**
- `string HostAndPort`.
- `TryParse(string raw, out ServerHost host, out string problem)`:
  - tira `http://`, `https://`, `ws://`, `wss://` e a barra final;
  - rejeita vazio, espaço, caminho (`/x`) e porta fora de 1..65535;
  - a mensagem do problema traz o valor recebido e o formato
    `host[:porta]`.

**`ReleaseTlsRule`**
- `Check(bool developmentBuild, string sceneName, string configName, bool isProd, bool useTls)`
  → `string?` (mensagem de violação ou `null`).
- Viola só se `!developmentBuild && !useTls`.

**`AppConfig`** (evolui no lugar; `ScriptableObject`)

| Campo / membro | Mudança |
|---|---|
| `apiBaseUrl`, `useTls`, `loginEndpoint`, `playerMe`, `tokenRefreshEndpoint`, `matchmakingConsumerUrl`, `matchConsumerUrl` | mantidos |
| `connectionConsumerUrl` | removido |
| `EffectiveHost` | novo: host da inicialização (só dev) ou `apiBaseUrl` |
| `OverrideHost(string raw)` | novo: aceita só em build de desenvolvimento e com `ServerHost.TryParse` válido; senão devolve o motivo e mantém o host |
| `HttpUrl(path)`, `WsUrl(path)` | passam a usar `EffectiveHost` |
| `OnValidate` | normalização passa a delegar para `ServerHost` |
