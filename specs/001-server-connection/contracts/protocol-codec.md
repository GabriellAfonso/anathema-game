# Contrato: codec do protocolo

**Feature**: `001-server-connection` | Interface em `Anathema.Net.Core`,
implementação em `Anathema.Net.Json`

Como o cliente transforma texto em frame tipado e mensagem em texto. As formas
das mensagens **não estão aqui**; são as dos contratos do backend:

- envelope, `message_refused`, códigos de recusa:
  `backend/specs/009-match-protocol/contracts/`
- recusas com campos extras (`deck_id`, `deck_problems` com `kind`):
  `backend/specs/011-deck-catalog-api/contracts/matchmaking_messages.md`
- `ping`/`pong`, eco do payload, `auth_denied` + 4001:
  `backend/specs/013-socket-heartbeat/contracts/heartbeat_messages.md`

---

## `IProtocolCodec`

```csharp
public interface IProtocolCodec
{
    string Encode(IOutgoingMessage message);
    DecodeOutcome<ServerFrame> Decode(string frameText);
    string EncodeObject(Action<IPayloadWriter> writeFields);
    DecodeOutcome<IPayloadReader> DecodeObject(string jsonText);
}
```

Implementação: `NewtonsoftProtocolCodec(DiscriminatedUnion<ServerFrame> frames)`.

### Decodificação, em ordem

| Passo | Falha | `DecodeFailureKind` |
|---|---|---|
| 1. `frameText` é JSON | não é | `NotJson` |
| 2. raiz é objeto | lista, número, texto, `null` | `NotObject` |
| 3. `type` presente | ausente | `MissingType` |
| 4. `type` é texto | número, objeto, `null`… | `TypeNotText` |
| 5. `payload` ausente → objeto vazio; presente → objeto | presente e não objeto | `PayloadNotObject` |
| 6. braço de `type` lê o payload | campo obrigatório ausente / tipo errado / valor inválido | `MissingField` / `WrongFieldType` / `InvalidValue` |
| 7. `type` sem braço | — (não é falha) | → `UnknownServerFrame(type)` |

Garantias:

- **Nenhuma exceção sai de `Decode`**, qualquer que seja o texto (FR-033). Isso
  inclui `null`, texto vazio e JSON profundo.
- Campos a mais, em qualquer nível, são ignorados (FR-032).
- `type` é comparado exatamente, com diferença de maiúsculas: `"Pong"` é
  desconhecido, como no servidor.
- `DecodeFailure.Detail` traz o valor recebido (cortado em 200 caracteres) e a
  forma esperada.

### Codificação

`Encode` produz `{"type": <MessageType>, "payload": {...}}` em uma linha, sempre
com `payload` (`{}` quando `WritePayload` não escreve nada). O contrato 013
aceita qualquer JSON no payload do ping, e o servidor ecoa `{}`.

`EncodeObject(Action<IPayloadWriter>)` e `DecodeObject(string)` servem aos corpos
HTTP (login e cadastro do probe): mesmo writer e mesmo reader, sem envelope.
`DecodeObject` segue os passos 1-2 da tabela acima e devolve
`DecodeOutcome<IPayloadReader>`.

---

## `IPayloadReader`

Vista só-leitura de um objeto JSON. É o único jeito de o resto do código ler
payload; `JObject`/`JToken` não saem de `Anathema.Net.Json`.

```csharp
public interface IPayloadReader
{
    string Path { get; }
    bool Has(string field);
    IReadOnlyCollection<string> FieldNames { get; }

    string ReadText(string field);
    long ReadInteger(string field);
    bool ReadBoolean(string field);
    IPayloadReader ReadObject(string field);
    IReadOnlyList<IPayloadReader> ReadObjectList(string field);
    IReadOnlyList<long> ReadIntegerList(string field);

    string? ReadOptionalText(string field);
    long? ReadOptionalInteger(string field);
    IPayloadReader? ReadOptionalObject(string field);
}
```

- `Read*` com campo ausente → `PayloadShapeException(MissingField)`. Com tipo
  errado → `WrongFieldType`. Opcional com `null` ou ausente → `null`. Opcional
  com tipo errado → `WrongFieldType`.
- `ReadInteger` aceita só inteiro JSON. `4.0` e `"4"` são `WrongFieldType`.
- `Path` compõe o caminho para a mensagem: `payload.deck_problems[1]`.
- Extensões de identidade no núcleo: `ReadUserId`, `ReadCardInstanceId`,
  `ReadMatchId` (e as `Optional`).

## `IPayloadWriter`

```csharp
public interface IPayloadWriter
{
    void WriteText(string field, string value);
    void WriteInteger(string field, long value);
    void WriteBoolean(string field, bool value);
    void WriteObject(string field, Action<IPayloadWriter> writeFields);
    void WriteIntegerList(string field, IReadOnlyList<long> values);
}
```

Extensões de identidade: `WriteUserId`, `WriteCardInstanceId`, `WriteMatchId`.
Campo repetido → `ArgumentException` com o nome.

---

## `DiscriminatedUnion<TBase>`

Infraestrutura de união fechada (FR-030, FR-031). As features seguintes só
registram braços.

```csharp
public sealed class DiscriminatedUnion<TBase> where TBase : class
{
    public DiscriminatedUnion(string discriminatorField, Func<string, TBase> unknownArm);
    public DiscriminatedUnion<TBase> Register(string value, Func<IPayloadReader, TBase> arm);
    public DecodeOutcome<TBase> DecodeBody(string value, IPayloadReader body);
    public DecodeOutcome<TBase> DecodeObject(IPayloadReader objectWithDiscriminator);
}
```

- `DecodeBody`: para o envelope, com `type` fora do `payload`.
- `DecodeObject`: para `kind`/`modifier_kind` dentro do próprio objeto.
- Captura `PayloadShapeException` do braço e devolve a falha. Não captura outras
  exceções: um `NullReferenceException` num braço é bug e deve aparecer no teste.
  O `NewtonsoftProtocolCodec` é quem garante, na borda, que nada escapa de
  `Decode`: qualquer outra exceção vira `InvalidValue` e é registrada.
- Dentro de um braço, união aninhada: `problemUnion.DecodeObject(item)`. Uma falha
  aninhada é relançada como `PayloadShapeException`, com o caminho completo.

Exemplo de registro, por uma feature seguinte (forma, não código final):

```csharp
GenericServerFrames.CreateUnion()
    .Register("match_found", MatchFoundFrame.Read);   // feature 3
```

---

## Frames genéricos desta feature

| `type` | Tipo | Leitura | Observação |
|---|---|---|---|
| `message_refused` | `MessageRefusedFrame` | `code` e `error` obrigatórios, texto; `Details` = o payload inteiro | código desconhecido é preservado. Decisão sempre por `Code` (princípio II) |
| `auth_denied` | `AuthDeniedFrame` | `error` obrigatório, texto | chega antes do fechamento 4001 |
| `pong` | `PongFrame` | `Marker` só se `sent_at_ms` e `ping_seq` são inteiros; senão `null` | o servidor devolve `{}` quando o ping não tinha objeto |
| outro | `UnknownServerFrame` | `MessageType` | inclui `match_found`, `match_update` etc. até as features registrarem |

Mensagem que sai nesta feature: `PingMessage(PingMarker? marker)`, com
`type = "ping"`.

---

## Casos de teste obrigatórios (EditMode, `Anathema.Net.Json.Tests`)

1. Ida e volta do envelope: `Encode(PingMessage(marker))`, depois o texto
   equivalente de um `pong` com o mesmo payload, e `Decode` → marcador igual.
2. `message_refused` com `deck_id` extra → `Code`, `Error` e
   `Details.ReadInteger("deck_id") == 4`.
3. `message_refused` sem `code` → `MissingField`, caminho `payload.code`.
4. `type` desconhecido → `UnknownServerFrame` com o texto exato.
5. Conjunto inválido → falha com o `Kind` certo e zero exceções: `""`,
   `"not json"`, `"[]"`, `"42"`, `"null"`, `"{}"`, `{"type": 5}`,
   `{"type": "pong", "payload": []}`, JSON truncado, JSON com 1000 níveis.
6. Campos a mais no envelope e no payload → mesmo valor do frame sem eles.
7. União aninhada por `kind` com braço desconhecido → valor desconhecido no item,
   não falha.
8. `UserId` lido de `"7"` (texto) → `WrongFieldType`.
9. `pong` com payload `{}` → `Marker == null`.
