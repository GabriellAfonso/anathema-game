# Contrato: conexão autenticada

**Feature**: `003-authenticated-socket-queue`

Superfície que a fila, a partida (feature 4) e a fachada (feature 5) consomem.
Protocolo do servidor: contratos do backend citados na spec, não copiados aqui.

---

## Portas novas (`Anathema.Net.Core`)

```csharp
public interface IFrameTicker
{
    event Action? Ticked;
}

public interface IWebSocketFactory
{
    IWebSocket Create();
}
```

| Garantia | Requisito |
|---|---|
| `Ticked` na thread principal, uma vez por quadro, depois de `MainThreadQueue.Drain()` | research R2 |
| `Create()` devolve uma instância nova e ociosa a cada chamada | contrato `IWebSocket` da 001 |

Adaptadores: `UnityFrameTicker` (`Raise()` chamado pelo `NetworkLayerHost`) e
`DotNetWebSocketFactory`. Fakes: `FakeFrameTicker` e `FakeWebSocketFactory`.

---

## `AuthenticatedConnection` (`Anathema.Net.Connection`)

```csharp
public sealed class AuthenticatedConnection : IDisposable
{
    public AuthenticatedConnection(ConnectionPorts ports, IAccessTokenSource tokens, IProtocolCodec codec, ConnectionSettings settings);

    public ConnectionStatus Status { get; }
    public TimeSpan? LastLatency { get; }

    public event Action<ConnectionStatus>? StatusChanged;
    public event Action<ServerFrame>? FrameReceived;
    public event Action<string>? RawTextReceived;   // ponte até a feature 4 (research R12)
    public event Action? Recovered;
    public event Action<TimeSpan>? LatencyMeasured;

    public void Connect(ConnectionTarget target);
    public void Leave();
    public Task<SocketSendOutcome> SendAsync(IOutgoingMessage message);
    public void Dispose();
}
```

### Token e gates

| Garantia | Requisito |
|---|---|
| Cada abertura é precedida de exatamente um `GetValidAsync()`, chamado depois de qualquer espera | FR-002 |
| O token vai só em `token=` da query; nenhum log contém a query | FR-002, FR-016 |
| `SessionUnavailable(Expired)` → `GaveUp(SessionExpired)`; `SessionUnavailable(NoSession)` → `GaveUp(NoSession)`; `Unavailable` → `WaitingRetry` sem contar recusa de token | FR-003 |
| `auth_denied` e/ou 4001 no mesmo socket → uma recusa, `RenewingToken`, um `RenewNowAsync()`, nova abertura | FR-004 |
| Recusas de token seguidas acima do limite → `GaveUp(TokenRefusedRepeatedly)` | FR-005 |
| Renovação `SessionExpired`/`NoSession` → `GaveUp`; `Unavailable` → `WaitingRetry` sem contar recusa | FR-006 |
| `match_denied` e/ou 4400/4403/4404 → `GaveUp(MatchRefused, detalhe)`, sem renovar e sem tentar | FR-007 |
| Qualquer outro fim, inclusive 1000 e 1001 → `WaitingRetry` com espera exponencial, teto e jitter | FR-008 |
| Classificação pela tabela de research R7 | FR-004, FR-007, FR-008 |

### Prova, estados e eventos

| Garantia | Requisito |
|---|---|
| O primeiro frame decodificado do socket atual que não é `auth_denied` nem `match_denied` — inclusive `pong` — zera recusas de token, tentativas e backoff | FR-009 |
| Abrir o socket não zera nada | FR-009 |
| `StatusChanged` a cada mudança de `ConnectionStatus`, nunca repetido sem mudança | FR-011 |
| `Recovered` uma vez por recuperação, na prova do socket reaberto; nunca na primeira conexão | FR-012 |
| `GiveUpReason.PlayerText()` para todo `GiveUpKind` e detalhe | FR-013 |
| `FrameReceived` na ordem de chegada, na thread principal; frame inválido → log `connection_frame_invalid` e descarte, sem fechar | FR-014 |
| `pong`, `auth_denied` e `match_denied` também chegam por `FrameReceived` (quem consome pode ignorar) | FR-014 |
| `SendAsync` sem socket aberto → `NotOpen`, log `connection_send_not_open`, sem exceção | FR-015 |
| Depois de `Leave()`: nenhuma abertura, nenhum `FrameReceived`, nenhum `Recovered`, só o `StatusChanged(Disconnected)` do próprio `Leave` | FR-010 |
| Resultado de token ou renovação de uma tentativa antiga é descartado | FR-010, research R4 |
| `Connect` em `Connecting`/`Connected`/`WaitingRetry`/`RenewingToken`/`Suspended` com o mesmo alvo não faz nada; com alvo diferente lança `InvalidOperationException` com os dois alvos | Edge cases |

### Heartbeat

| Garantia | Requisito |
|---|---|
| Ping logo depois de abrir e a cada `PingInterval`, com `PingMarker` do instante monotônico | FR-017 |
| `pong` casado por marcador → `LatencyMeasured` e `LastLatency`; sem marcador pendente → sem latência | FR-018 |
| Qualquer frame zera o silêncio; só depois do primeiro `pong` do socket atual, silêncio ≥ `SilenceLimit` derruba (com confirmação atrás da fila) | FR-019, FR-021 |
| Delta entre ticks > `PauseThreshold` e volta ao primeiro plano zeram o silêncio antes de avaliar | FR-020 |

### Segundo plano e rede

| Garantia | Requisito |
|---|---|
| Em segundo plano sem socket aberto: `Suspended(Background)`, nenhuma abertura, nenhuma tentativa consumida | FR-022 |
| Em segundo plano com socket aberto: nada é fechado | FR-022 |
| Volta: silêncio zerado; aberto → ping já; senão `ResetBackoff` e abertura já (com `GetValidAsync`) | FR-023 |
| `GaveUp` ou `Disconnected` não reagem a volta, rede ou tick | FR-023 |
| Mudança entre dois tipos com rede → socket descartado e outro aberto já; sem `WaitingRetry`, sem tentativa | FR-024 |
| `NetworkKind.None` sem socket aberto → `Suspended(NoNetwork)`; rede de volta → abertura já | FR-025 |
| Rede de volta com o socket ainda aberto → reciclado, como na troca de tipo: o socket da interface perdida não é confiável, e o heartbeat levaria o limite de silêncio para perceber | FR-024, FR-025 |
| Troca de rede em segundo plano → nada; na volta, reciclado | Edge cases |

Tipos internos da implementação (visíveis só aos testes): `SocketAttempt`, `SocketEnd`,
`SocketEndKind`, `SocketEndClassifier`, `PingLedger`, `SilenceWatch`,
`ConnectionSuspension`, `ResumeCause`, `QueueRefusalReader`, `SocketUrl`.

### Eventos de log

Nomes estáveis, usados pelo quickstart. Campos nunca incluem token nem query.

| Evento | Nível | Campos |
|---|---|---|
| `connection_opening` | Info | `target` (sem query), `attempt` |
| `connection_proven` | Info | `target`, `recovered` |
| `connection_waiting_retry` | Info | `attempt`, `wait_ms`, `close_code` |
| `connection_renewing_token` | Info | `refusals` |
| `connection_gave_up` | Warning | `kind`, `match_detail` |
| `connection_suspended` | Info | `reason` |
| `connection_reopening` | Info | `cause` (`Foreground`, `NetworkBack`, `NetworkKindChanged`) |
| `connection_socket_recycled` | Info | `previous`, `current` |
| `connection_ping_on_foreground` | Debug | — |
| `connection_latency` | Debug | `latency_ms` |
| `connection_silence_confirmed` | Warning | `silence_ms` |
| `connection_frame_invalid` | Warning | `kind`, `path` |
| `connection_send_not_open` | Warning | `message_type`, `phase` |
| `connection_stale_token_result` | Debug | `attempt_generation` |
| `connection_attempt_failed` | Error | `exception` |
| `queue_join_sent` | Info | `deck_id` |
| `queue_refused` | Info | `code` |
| `queue_refusal_out_of_contract` | Warning | `code`, `path` |
| `queue_left` | Warning | `kind` |

---

## `ConnectionSettings` e `ConnectionTiming`

```csharp
public sealed class ConnectionSettings
{
    public ConnectionSettings(ReconnectPolicy policy, ConnectionTiming timing);
    public ReconnectPolicy Policy { get; }
    public ConnectionTiming Timing { get; }
    public static ConnectionSettings ForMatchmaking();
    public static ConnectionSettings ForMatch();
}

public sealed class ConnectionTiming
{
    public ConnectionTiming(TimeSpan pingInterval, TimeSpan silenceLimit, TimeSpan pauseThreshold);
    public static ConnectionTiming Default { get; }   // 10 s, 30 s, 5 s
}
```

Valores e origem: research R15.

---

## `ReconnectPolicy` e `Heartbeat` (movidos)

API atual preservada. Diferenças:

| Tipo | Mudança | Research |
|---|---|---|
| `ReconnectPolicy` | 1000 não é terminal | R7 |
| `ReconnectPolicy` | `ResetBackoff` passa a ser usado nas reaberturas imediatas, não na abertura | R7 |
| `Heartbeat` | `ForgivePause()` | R3, R8 |
| `Heartbeat` | `NotePingSent()` | R8 |

---

## Fakes (`Anathema.Net.Fakes`)

**`FakeAccessTokenSource : IAccessTokenSource`**
- `EnqueueValid(string text)`, `EnqueueSessionUnavailable(SessionUnavailableKind)`,
  `EnqueueUnavailable(RenewalUnavailableReason)` — próximos `GetValidAsync`.
- `EnqueueRenewed(string text)`, `EnqueueRenewalExpired()`, `EnqueueRenewalNoSession()`,
  `EnqueueRenewalUnavailable(RenewalUnavailableReason)` — próximos `RenewNowAsync`.
- `HoldNextRenewal()` → `HeldRenewal` com `Release()`.
- Sem roteiro: `GetValidAsync` repete o último válido; `RenewNowAsync` lança no
  teste com mensagem dizendo o que roteirizar.
- `ValidRequests`, `RenewRequests`.

**`SpoiledFirstTokenSource : IAccessTokenSource`** — embrulha outro; o primeiro
`Valid` sai com texto inválido; o resto passa direto.

**`FakeWebSocketFactory : IWebSocketFactory`** — `IReadOnlyList<FakeWebSocket> Created`,
`FakeWebSocket Latest` (lança no teste se nenhum foi criado).

**`FakeFrameTicker : IFrameTicker`** — `Tick()`.

A assembly de fakes referencia `Anathema.Net.Account` e enxerga o construtor
interno de `AccessToken` (research R5).

---

## Testes (`Anathema.Net.Connection.Tests`)

Referências: `Core`, `Json`, `Account`, `Connection`, `Fakes`.
`ConnectionTestRig` monta conexão, fakes e codec real (`ConnectionFrames.CreateUnion()`).

| Arquivo | Cobre |
|---|---|
| `ReconnectPolicyTests` (movido) | backoff, jitter, terminais, auth, limite |
| `HeartbeatTests` (movido) | intervalo, timeout armado pelo pong, `ForgivePause`, `NotePingSent` |
| `SocketEndClassifierTests` | tabela de R7 |
| `PingLedgerTests` | marcador, casamento, limite de pendentes |
| `ConnectionTargetTests` | query, escape, `ToString` sem token |
| `ConnectionTimingTests`, `ConnectionSettingsTests` | validação e presets |
| `GiveUpReasonTests` | `PlayerText` para todo braço |
| `ConnectionTokenTests` | US1-1 a US1-4, sessão indisponível, resultado velho descartado |
| `ConnectionGateTests` | US1-5, `match_denied` sem código, 44xx sem frame |
| `ConnectionDropTests` | US1-6, 1000/1001, queda antes da prova, `Recovered` |
| `ConnectionLeaveTests` | US1-7, `Leave` em cada fase |
| `ConnectionFrameDeliveryTests` | US1-8, `SendAsync` sem socket, `RawTextReceived` |
| `ConnectionSilenceTests` | US3-1 a US3-6 |
| `ConnectionSuspensionTests` | US4-1 a US4-5 |
| `ConnectionAssemblyBoundaryTests` | FR-047, com `AssemblySignatureScanner` |
