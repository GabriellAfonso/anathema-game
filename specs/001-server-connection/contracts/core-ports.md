# Contrato: portas do núcleo

**Feature**: `001-server-connection` | Assembly: `Anathema.Net.Core`

O que as features 2 a 5 podem esperar das seis portas, dos fakes e da fila da
thread principal. Tipos de valor em [data-model.md](../data-model.md). As
assinaturas são a forma do contrato; o `/// <summary>` com `<example>` fica no
código.

Regra comum aos adaptadores reais: **todo aviso de evento chega na thread
principal**, via `MainThreadQueue`. Os fakes disparam avisos na hora, na thread
do teste.

---

## `IWebSocket`

```csharp
public interface IWebSocket
{
    event Action Opened;
    event Action<string> TextReceived;
    event Action<SocketClosure> Closed;
    event Action<string> Errored;

    void Open(Uri url);
    Task<SocketSendOutcome> SendTextAsync(string text);
    void Close();
}
```

| Garantia | Requisito |
|---|---|
| `TextReceived` na ordem de chegada; texto fragmentado entregue inteiro | FR-005, FR-009 |
| `SocketClosure.Code` é o número exato do close frame (4001, 4400, 4403, 4404, 1000…); `null` se não houve close frame | FR-006 |
| No máximo um `Closed`; nada depois dele | FR-007 |
| `SendTextAsync` fora de Open → `NotOpen`, sem exceção | FR-008 |
| `Close()` idempotente em qualquer estado | Edge cases |
| Esquema proibido pela `CleartextPolicy` → `Errored("cleartext_refused")` + `Closed(null, …)` | FR-045 |
| Uma instância = uma conexão; `Open` fora de Idle lança `InvalidOperationException` | data-model |

Adaptador real: `DotNetWebSocket(MainThreadQueue, CleartextPolicy, IClientLog)`.
Recebe fora da thread principal (research R4).

Fake: `FakeWebSocket`.
- Registra: `OpenedUrl`, `SentTexts` e `CloseRequested`.
- Roteiro: `SimulateOpened()`, `SimulateText(string)`,
  `SimulateClosed(int? code, string reason)`, `SimulateError(string)`.
- Resultado de envio configurável: `NextSendOutcome`.
- Respeita as mesmas garantias de estado. Um `SimulateText` depois de fechado
  lança `InvalidOperationException` no teste, para acusar roteiro errado.

---

## `IHttpTransport`

```csharp
public interface IHttpTransport
{
    Task<HttpOutcome> SendAsync(HttpRequestSpec request);
}
```

| Garantia | Requisito |
|---|---|
| 4xx/5xx → `HttpResponse`, nunca exceção | FR-011 |
| Sem rede, host não resolvido, conexão recusada, prazo → `TransportFailure` com `Kind` | FR-012 |
| Todo pedido tem prazo (padrão 10 s) | FR-013 |
| Corpo devolvido como texto, sem interpretação | Edge cases |
| Pode ser chamado de qualquer thread; a `Task` completa na thread principal | FR-026 |

Adaptador real: `UnityHttpTransport(MainThreadQueue, CleartextPolicy)` (research R5).

Fake: `FakeHttpTransport`.
- Registra os pedidos em `Requests`.
- Roteiro: `RespondNext(int status, string body)` e
  `FailNext(TransportFailureKind, string detail)`, consumidos em ordem.
- Pedido sem roteiro: falha o teste com mensagem que mostra método e URL.

---

## `IMonotonicClock`

```csharp
public interface IMonotonicClock
{
    MonotonicInstant Now { get; }
}
```

| Garantia | Requisito |
|---|---|
| Nunca diminui; imune a mudança da hora do sistema; sem hora absoluta | FR-014 |
| Conta o tempo com o aparelho dormindo | FR-016 |
| Seguro de qualquer thread | research R1 |

Adaptador real: `PlatformMonotonicClock.Create()`. No Windows,
`StopwatchMonotonicClock`; no Android, `BootTimeMonotonicClock`.

Fake: `FakeMonotonicClock`, com `Advance(TimeSpan)` e `Now`. Começa num instante
fixo, diferente de zero.

---

## `IAppLifecycle`

```csharp
public interface IAppLifecycle
{
    event Action<WentToBackground> WentToBackground;
    event Action<ReturnedToForeground> ReturnedToForeground;
}
```

| Garantia | Requisito |
|---|---|
| `ReturnedToForeground.AwayFor` medido no `IMonotonicClock` | FR-015 |
| No máximo um "foi" por "voltou"; nunca começa com "voltou" | FR-017 |

Adaptador real: `UnityAppLifecycle(LifecycleSignalFilter, MainThreadQueue)`.
Recebe `OnApplicationPause`/`OnApplicationFocus` do `NetworkLayerHost`
(research R2).

Fake: `FakeAppLifecycle`.
- Construído com um `FakeMonotonicClock`.
- `SimulateBackground()` e `SimulateForeground()` produzem os avisos com a duração
  lida do relógio.
- Chamada fora de ordem lança no teste.

---

## `INetworkReachability`

```csharp
public interface INetworkReachability
{
    NetworkKind Current { get; }
    event Action<NetworkKindChanged> Changed;
}
```

| Garantia | Requisito |
|---|---|
| `Current` ∈ {`None`, `LocalArea`, `CarrierData`} | FR-018 |
| `Changed` a cada mudança, inclusive `LocalArea → CarrierData` direto; nunca sem mudança | FR-019 |
| Descreve tipo de rede, não alcance ao servidor | Assumptions |

Adaptador real: `UnityNetworkReachability(IMonotonicClock, MainThreadQueue)`.
Consulta a cada 1 s, chamado pelo `Update` do hospedeiro (research R9).

Fake: `FakeNetworkReachability(NetworkKind initial)`, com
`SimulateKind(NetworkKind)`. Se o valor for igual ao atual, não avisa.

---

## `IClientLog`

```csharp
public interface IClientLog
{
    void Write(ClientLogEntry entry);
}
```

Extensões no núcleo: `Info(eventName, params LogField[])`, `Warning(...)`,
`Error(...)`, `Debug(...)`.

| Garantia | Requisito |
|---|---|
| Nível, nome de evento e campos nomeados | FR-020 |
| Seguro de qualquer thread | — |
| `Debug.Log*` só em `UnityConsoleLog` | constituição |

Fake: `FakeClientLog`, com `Entries` e
`Single(string eventName)` (falha se houver 0 ou mais de 1).

---

## `MainThreadQueue`

```csharp
public sealed class MainThreadQueue
{
    public MainThreadQueue(IClientLog log);
    public void Enqueue(Action item);   // qualquer thread
    public void Drain();                // thread principal
    public void Close();                // idempotente
    public bool IsClosed { get; }
}
```

| Garantia | Requisito |
|---|---|
| Entrega na ordem de `Enqueue` | FR-024 |
| Depois de `Close()`, nada é entregue; `Enqueue` descarta sem exceção | FR-025 |
| Exceção de um item vai para o log (`main_thread_item_failed`) e não interrompe os seguintes | FR-027 |
| Item enfileirado durante `Drain()` fica para a próxima chamada | Edge cases |

Hospedeiro: `NetworkLayerHost : MonoBehaviour`.
- `Update` → `queue.Drain()` e `reachability.Poll()`.
- `OnApplicationPause`/`OnApplicationFocus` → `UnityAppLifecycle`.
- `OnDestroy` → `queue.Close()`.
- É o único componente de cena da feature (FR-028). Recebe as dependências por um
  método `Attach(...)` chamado pela composição, sem singleton.

---

## Verificação da fronteira (SC-002)

`Anathema.Net.Core.Tests` tem um teste que:

1. confere que `Anathema.Net.Core` não referencia assembly cujo nome começa com
   `UnityEngine`, `UnityEditor` ou `Newtonsoft`;
2. percorre os tipos do assembly (campos, propriedades, parâmetros e retornos de
   métodos, eventos) e falha se algum tipo usado estiver em
   `System.Net.WebSockets`.
