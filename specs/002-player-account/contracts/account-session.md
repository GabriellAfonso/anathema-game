# Contrato: sessão de conta, tokens e cliente autenticado

**Feature**: `002-player-account` | Assembly: `Anathema.Net.Account`

O que as features 3 a 5 e a apresentação podem esperar da sessão. Os tipos de
valor estão em [data-model.md](../data-model.md); as decisões, em
[research.md](../research.md). As assinaturas mostram a forma do contrato. O
`/// <summary>` com `<example>` fica no código.

As rotas de conta não têm contrato escrito no backend. As formas estão na
descrição da feature (spec, "Contexto") e foram verificadas em
`backend/server/apps/accounts/` e no SimpleJWT 5.5.1.

---

## `AccountSession`

```csharp
public sealed class AccountSession
{
    public AccountSession(IHttpTransport http, IProtocolCodec codec, IMonotonicClock clock,
        IRefreshTokenVault vault, IClientLog log, AccountRoutes routes);

    public AccountSessionState State { get; }
    public UserId? Self { get; }
    public int Generation { get; }
    public event Action? SessionExpired;

    public Task<SignInOutcome> SignInAsync(string username, Password password);
    public Task<ResumeOutcome> ResumeAsync();
    public void SignOut();
}
```

| Garantia | Requisito |
|---|---|
| `SignInAsync` com 200 legível → `SignedIn(UserId)`; tokens em memória; guarda gravada | FR-004, FR-005 |
| 401 → `CredentialsRefused`; outro status → `ServerRefused(status)`; transporte → `TransportFailed`; 200 ilegível → `OutOfContract`; nada é gravado | FR-004, FR-010 |
| Segundo `SignInAsync` durante um em curso → `AlreadyInProgress`, sem requisição | FR-008 |
| Falha ao gravar a guarda: login vale, `vault_save_failed` no log | FR-023 |
| `SignOut` apaga tokens e guarda, `Generation++`, sem `SessionExpired`, sem requisição | FR-007 |
| `ResumeAsync`: nada guardado → `NothingStored` sem requisição; `Unreadable` → apaga, registra, `NothingStored` | FR-019, FR-023 |
| `ResumeAsync`: renovação 200 → `Resumed(UserId)`; 401 → apaga, `Refused`, sem evento; outro → `Unavailable`, guarda intacta | FR-018, FR-019 |
| `SessionExpired` dispara uma vez por expiração, na thread principal | FR-014 |

Mapeamento das rotas:

| Rota | Corpo enviado | Lido na resposta |
|---|---|---|
| `POST Login` | `username`, `password` | `token` (acesso), `refresh` |
| `POST Refresh` | `refresh` | `access` |

---

## `IAccessTokenSource`

A porta "me dê um token de acesso válido". A feature 3 usa no `?token=` do socket.

```csharp
public interface IAccessTokenSource
{
    Task<AccessTokenOutcome> GetValidAsync();
    Task<RenewalOutcome> RenewNowAsync();
}
```

Implementação: `SessionAccessTokens(AccountSession, TokenRenewal, IMonotonicClock, AccountTiming)`.

| Garantia | Requisito |
|---|---|
| Sessão sem tokens → `SessionUnavailable(NoSession)` ou `(Expired)`, sem requisição | edge cases |
| `NeedsRenewal(now, margin)` falso → `Valid(token atual)` sem requisição | FR-012 |
| `NeedsRenewal` verdadeiro → renova antes; `Renewed` → `Valid(novo)` | FR-012 |
| `RenewNowAsync` renova sem olhar a margem (depois de um 4001 do socket) | research R7 |
| Chamadas concorrentes, dos dois métodos, compartilham a mesma renovação | FR-013 |

## `TokenRenewal`

Uso interno da sessão, visível só para os testes do assembly
(`InternalsVisibleTo`).

| Resposta do refresh | `RenewalOutcome` | Efeito na sessão |
|---|---|---|
| 200 com `access` legível | `Renewed` | troca o acesso; o refresh continua o mesmo |
| 401 | `SessionExpired` | `Expired`, guarda apagada, `Generation++`, evento |
| outro status | `Unavailable(ServerStatus)` | nenhum |
| falha de transporte | `Unavailable(Transport)` | nenhum |
| 200 ilegível | `Unavailable(OutOfContract)` | nenhum |
| qualquer um, com `Generation` mudada desde o início | o resultado vai para quem esperava | **nenhum**: descartado (FR-016) |

---

## `ForegroundRenewal`

```csharp
public sealed class ForegroundRenewal : IDisposable
{
    public ForegroundRenewal(IAppLifecycle lifecycle, AccountSession session,
        IAccessTokenSource tokens, IMonotonicClock clock, AccountTiming timing, IClientLog log);
}
```

| Garantia | Requisito |
|---|---|
| `ReturnedToForeground` com `SignedIn` e token vencido ou na margem → `GetValidAsync()` imediato; registra `access_token_renewal_on_foreground` | FR-017 |
| Sessão não `SignedIn`, ou token fora da margem → nada | FR-017 |
| Renovação já em curso → compartilha a mesma | FR-013 |
| `Dispose` cancela a assinatura | — |

---

## `AuthenticatedHttpClient`

```csharp
public sealed class AuthenticatedHttpClient
{
    public AuthenticatedHttpClient(IHttpTransport http, AccountSession session,
        IAccessTokenSource tokens, IClientLog log);

    public Task<AuthenticatedCallResult> SendAsync(AuthenticatedRequest request);
}
```

`AuthenticatedCallResult` é uma resposta (`int Status`, `string BodyText`) ou um
`AccountCallFailure`. Os serviços transformam a resposta em
`AccountCallOutcome<TValue, TRefusal>`.

| Garantia | Requisito |
|---|---|
| Cabeçalhos `Authorization: Bearer <acesso>`, `Accept: application/json`; `Content-Type: application/json` quando há corpo | FR-025 |
| Pega o token por `GetValidAsync`; indisponível → falha correspondente, sem requisição | FR-011 |
| 401 com o token que a sessão ainda tem → uma renovação (compartilhada) → uma repetição | FR-026 |
| 401 com token já trocado por outra chamada → uma repetição com o atual, sem renovar | FR-026 |
| Repetição com 401 → resposta 401 devolvida, sem nova renovação | FR-026 |
| Renovação `SessionExpired` durante o 401 → `SessionUnavailable(Expired)` | FR-027 |
| Renovação necessária que não sai: por transporte → `TransportFailed`; por status ou resposta fora do contrato → `RenewalUnavailable` (a sessão continua) | FR-015, FR-027 |
| Falha de transporte → `TransportFailed`, sem renovar | FR-027 |
| Nenhuma exceção sai; o corpo nunca vai para o log | FR-027, FR-040 |

---

## `AccountRegistration`

```csharp
public Task<AccountCallOutcome<AccountCreated, RegistrationRefusal>> RegisterAsync(RegistrationForm form);
```

Não passa pelo cliente autenticado: a rota é pública.

| Resposta | Resultado |
|---|---|
| 201 | sucesso `AccountCreated`; sessão intocada |
| 400 com objeto de listas de texto | `RegistrationRefusal.Fields`, na ordem do corpo; `username`, `email`, `password` e `password_confirmation` tipados; outros como `Other` com o nome |
| 400 com outra forma, ou outro status | `RegistrationRefusal.UnrecognizedBody` |
| transporte | `TransportFailed` |

---

## Testes obrigatórios (EditMode, `Anathema.Net.Account.Tests`)

Todos com `FakeHttpTransport`, `FakeMonotonicClock`, `FakeAppLifecycle`,
`FakeRefreshTokenVault`, `FakeClientLog` e o codec real.

1. Login 200: `SignedIn` com o `UserId` do claim texto `"7"`; guarda com o refresh.
2. Login 401, 500, transporte e token ilegível: cada desfecho; guarda vazia.
3. Validade: `exp - iat = 300`, margem 30 s. Em `T + 269 s`, nenhum pedido; em
   `T + 271 s`, um pedido de renovação. `iat` do ano 2000 e do ano 2090 dão o
   mesmo resultado.
4. 401 numa rota → 1 renovação + 1 repetição; repetição 401 → recusa, total de
   pedidos = 3.
5. Dez `GetValidAsync` com `HoldNext` na renovação → 1 pedido; após `Release`,
   as dez recebem o mesmo token.
6. Renovação 401 → `Expired`, guarda vazia, `SessionExpired` contado 1, todos os
   que esperavam com `SessionUnavailable(Expired)`.
7. Renovação por transporte e por 503 → sessão `SignedIn`, guarda intacta.
8. `SignOut` com renovação pendente; `Release` → estado continua `SignedOut`.
9. Primeiro plano com o relógio avançado 7 min → um pedido de renovação antes de
   qualquer `GetValidAsync`; com 1 min → nenhum.
10. Retomada: `Resumed`, `Refused` (guarda apagada, sem evento), `Unavailable`
    (guarda intacta), `NothingStored` (zero pedidos), `Unreadable` (apagada).
11. Cadastro 201 e 400 com `email`, `password` e `non_field_errors`.
12. Nenhuma entrada do log contém senha, acesso ou refresh (roda os cenários 1 a 11).
