# Contrato: guarda segura e ponte com o código antigo

**Feature**: `002-player-account`

---

## `IRefreshTokenVault` (`Anathema.Net.Core`)

```csharp
public interface IRefreshTokenVault
{
    VaultReadOutcome Read();
    VaultWriteOutcome Save(RefreshToken token);
    void Delete();
}
```

| Garantia | Requisito |
|---|---|
| `Save` seguido de `Read` na mesma guarda (ou em outra instância no mesmo slot) → `Found` com o mesmo texto | US2 |
| `Delete` idempotente; depois dele `Read` → `Empty` | FR-020 |
| Valor ilegível → `Unreadable(detail)`; nunca exceção, nunca o valor no `detail` | FR-023 |
| Nada em texto puro, nada em `PlayerPrefs` | FR-022 |
| Chamada síncrona, na thread principal | research R2 |

### Adaptadores (`Anathema.Net.Unity`)

| Tipo | Plataforma | Proteção | Onde | Research |
|---|---|---|---|---|
| `DpapiRefreshTokenVault(string directory, RefreshTokenVaultSlot slot)` | Windows player e editor | `CryptProtectData` do usuário atual + entropia do app | `<directory>/refresh_token.<slot>.bin`, gravação atômica | R3 |
| `AndroidKeystoreRefreshTokenVault()` | Android | AES-256-GCM, chave no `AndroidKeyStore` | `noBackupFilesDir/refresh_token.bin`, via `RefreshTokenCipher.java` | R4 |

`PlatformRefreshTokenVault.Create(IClientLog)` escolhe: Android no Android
(fora do editor); DPAPI com `Application.persistentDataPath/account` no resto.

`RefreshTokenVaultSlot`:

- `ForPlayer()` → `player`;
- `ForEditorProject(string dataPath)` → `editor-<12 hex do SHA-256>`;
- `Named(string)` para testes.

Dois `dataPath` diferentes sempre geram slots diferentes (FR-024).

### Fake (`Anathema.Net.Fakes`)

`FakeRefreshTokenVault`:

- guarda em memória;
- `Stored` (texto ou nulo), `SaveCount` e `DeleteCount`;
- roteiro: `FailNextSave(detail)`, `MakeUnreadable(detail)`;
- `Preload(string)` para testes de retomada.

### Testes

- `Anathema.Net.Unity.Tests`: DPAPI em pasta temporária.
  - ida e volta;
  - segunda instância no mesmo slot lê o valor;
  - slots diferentes não se enxergam;
  - arquivo corrompido → `Unreadable`;
  - o arquivo no disco não contém o texto.
- Android: só pelo quickstart (não roda no editor).

---

## `FakeHttpTransport` (evolui, `Anathema.Net.Fakes`)

Acréscimo, sem mudar o que existe:

```csharp
public HeldHttpResponse HoldNext();
```

```csharp
public sealed class HeldHttpResponse
{
    public bool IsPending { get; }
    public void Release(int status, string body);
    public void ReleaseFailure(TransportFailureKind kind, string detail);
}
```

- O pedido entra em `Requests` na hora e a tarefa fica pendente até o teste soltar.
- Soltar duas vezes lança no teste.

---

## Ponte com o código anterior (`Assembly-CSharp`)

É dívida: sai na feature 3, quando o `BaseClient` passar a usar
`IAccessTokenSource`. Registrada no plano e em `Game/TODO.md`.

### `PlayerSession` (`MonoBehaviour`, `BootstrapScene`)

| Membro | Antes | Depois |
|---|---|---|
| `static Instance` | singleton de cena | mantido (dívida) |
| `Account` | — | `LiveAccountServices`, composto no `Awake` |
| `Token` | texto guardado por `SetTokens` | texto do acesso atual da sessão, ou `null` |
| `RefreshToken`, `SetTokens`, `SetAccessToken` | públicos | removidos |
| `Nickname`, `Icon`, `Level`, `Experience_points`, `Coins`, `Credits` | preenchidos por `SetProfile(SelfPlayerProfileDTO)` | preenchidos por `SetProfile(OwnProfile)` |
| `NetworkLayerHost` | — | adicionado ao mesmo `GameObject` no `Awake` |

### `TokenRefreshService`

- Classe comum, sem `MonoBehaviour`.
- `Instance` e `Refresh(Action<TokenRefreshResult>)` mantidos; chamam
  `Account.Tokens.RenewNowAsync()`.
- Mapeamento para o enum `TokenRefreshResult`, que não muda:
  - `Renewed` → `Success`;
  - `SessionExpired`/`NoSession` → `Expired`;
  - `Unavailable` → `NetworkError`.
- Chamadas concorrentes seguem compartilhando (agora pela `TokenRenewal`).

### `SelfProfileService`

`LoadProfile(string token)` vira `Task<bool> LoadProfileAsync()`: lê por
`OwnProfileQuery`, grava na `PlayerSession` e devolve se conseguiu.

### `LoginController`

- `Start`: `ResumeAsync`.
  - `Resumed` → perfil → `HomeScene`.
  - Senão libera o botão e segue o login automático de desenvolvimento por tag.
- `HandleLogin`: `SignInAsync` → perfil (falha não bloqueia) → `HomeScene`.
- Sem `UnityEditor`, `UnityWebRequest`, `JsonUtility` nem `Debug.Log*`; log por
  `IClientLog`.

### Removidos

`LoginRequestDTO`, `LoginResponseDTO`, `TokenRefreshDTO` (as duas classes) e
`SelfPlayerProfileDTO`, com os `.meta`.

### Intocados

`BaseClient`, `MatchClient`, `MatchmakingClient`, `NetworkBootstrap`,
`MiniPlayerProfile`, `AppEnvManager`, `ErrorPayloadDTO`, `WsEventDto`.
