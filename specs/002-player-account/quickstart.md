# Quickstart: Conta e dados do jogador

**Feature**: `002-player-account`

Como provar que a feature funciona. Formas e garantias em
[contracts/](./contracts/); decisões em [research.md](./research.md).

---

## 1. Suíte EditMode (sem servidor)

Editor fechado, na raiz do projeto:

```bash
"C:/Program Files/Unity/Hub/Editor/6000.2.8f1/Editor/Unity.exe" -batchmode -projectPath . -runTests -testPlatform EditMode -testResults Logs/editmode-results.xml
```

Esperado:

- `Logs/editmode-results.xml` com `result="Passed"` e zero falhas;
- os testes `LiveServer` aparecem como não executados (`[Explicit]`);
- `Logs/` sem aviso de compilação vindo de `Assets/Scripts/Net/` ou dos arquivos
  antigos tocados (SC-001);
- os testes de fronteira passam para `Anathema.Net.Core` e `Anathema.Net.Account`
  (SC-002), e o de credencial no log também (SC-003).

---

## 2. Testes LiveServer (backend local)

1. Suba o backend:

   ```bash
   cd C:/Users/gabri/Projetos/dev_container/anathema/backend
   docker compose up
   ```

2. Confira que `http://127.0.0.1:8000/game/cards/` responde 401 sem token.
3. No editor, abra **Window > General > Test Runner > EditMode**, filtre pela
   categoria `LiveServer` e rode `LiveAccountTests` (os `[Explicit]` só rodam
   quando selecionados).

   Pela linha de comando, com o editor fechado:

   ```bash
   "C:/Program Files/Unity/Hub/Editor/6000.2.8f1/Editor/Unity.exe" -batchmode -projectPath . -runTests -testPlatform EditMode -testCategory LiveServer -testFilter "Anathema.Net.Unity.Tests.LiveAccountTests" -testResults Logs/liveserver-account-results.xml
   ```

Esperado: os sete casos de research R11 passam (SC-006). A guarda de teste fica
numa pasta temporária, apagada ao fim.

---

## 3. Login pela cena (editor)

1. Backend no ar. Abra `Assets/Scenes/BootstrapScene.unity` e dê Play.
2. Na `LoginScene`, entre com um usuário existente.

Esperado:

- a `HomeScene` carrega com apelido, nível e moedas no mini perfil (SC-007);
- o Console mostra `account_signed_in user_id=…` e nenhum token.

3. Pare o Play e dê Play de novo.

Esperado: a `LoginScene` passa direto para a Home, com `account_resumed` no
Console, sem pedir senha.

4. Com o Multiplayer Play Mode e dois jogadores virtuais (tags `Player1` e
   `Player2`), dê Play.

Esperado: cada um entra com o próprio usuário. No Play seguinte, cada um retoma
a própria sessão, nunca a do outro (FR-024).

---

## 4. Android: fechar e abrir continua logado (SC-008)

Pré-requisitos: build de desenvolvimento instalado, backend no computador e
host do servidor configurado como na quickstart da 001
(`specs/001-server-connection/quickstart.md`, extra `serverHost`).

1. Em outro terminal, siga o log:

   ```bash
   adb logcat -s Unity
   ```

2. Abra o app e entre com um usuário.
3. Espere a Home e mate o app pelo sistema:

   ```bash
   adb shell am force-stop <applicationId>
   ```

4. Abra o app de novo, com o mesmo extra `serverHost`.

Esperado:

- a Home aparece sem a tela de login pedir senha;
- o log tem `account_resumed user_id=…`;
- nenhuma linha de log contém token.

---

## 5. Android: mais de 5 minutos minimizado (SC-009)

1. Logado na Home, aperte o botão Home do aparelho.
2. Espere 6 minutos. Deixar a tela apagar vale: o relógio conta o sono, research
   R1 da 001.
3. Volte ao app.

Esperado no log, nesta ordem:

- o retorno ao primeiro plano;
- `access_token_renewal_on_foreground`;
- `access_token_renewed`;
- nenhum `session_expired`.

4. Toque em **Jogar**. O `PlayButton` abre a fila pelo `MatchmakingClient`, que lê
   o token pela ponte.

Esperado: a fila abre sem `auth_denied` e sem fechamento 4001 no log, e nenhum
erro visível.

---

## 6. Sessão expirada (opcional)

1. No banco local, desative o usuário logado (`is_active = False`) pelo admin do
   Django.
2. Minimize o app por 6 minutos e volte.

Esperado:

- `session_expired` uma vez;
- o arquivo da guarda apagado;
- no próximo lançamento, a tela de login pede senha.

---

## Critérios de pronto (resumo)

| Critério | Seção |
|---|---|
| SC-001, SC-002, SC-003, SC-004, SC-005, SC-010, SC-011 | 1 |
| SC-006 | 2 |
| SC-007 | 3 |
| SC-008 | 4 |
| SC-009 | 5 |
