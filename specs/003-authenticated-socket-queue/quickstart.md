# Quickstart: Socket autenticado e fila

**Feature**: `003-authenticated-socket-queue`

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
- os `LiveServer` aparecem como não executados (`[Explicit]`);
- `ReconnectPolicyTests` e `HeartbeatTests` rodam dentro de
  `Anathema.Net.Connection.Tests`, não mais em `Anathema.Reconnect.Tests`;
- `ConnectionAssemblyBoundaryTests` e `CoreAssemblyBoundaryTests` passam (SC-003);
- `RunInBackgroundSettingTests` passa (FR-026);
- nenhum aviso de compilação vindo de `Assets/Scripts/` (SC-001);
- testes da feature somados abaixo de 5 s no XML (SC-012).

---

## 2. Testes LiveServer (backend local)

1. Suba o backend:

   ```bash
   cd C:/Users/gabri/Projetos/dev_container/anathema/backend
   docker compose up
   ```

2. Confira que `http://127.0.0.1:8000/game/cards/` responde 401 sem token.
3. No editor: **Window > General > Test Runner > EditMode**, categoria
   `LiveServer`, rode `LiveQueueTests`. Pela linha de comando, editor fechado:

   ```bash
   "C:/Program Files/Unity/Hub/Editor/6000.2.8f1/Editor/Unity.exe" -batchmode -projectPath . -runTests -testPlatform EditMode -testCategory LiveServer -testFilter "Anathema.Net.Unity.Tests.LiveQueueTests" -testResults Logs/liveserver-queue-results.xml
   ```

Esperado: os seis casos de [contracts/matchmaking-queue.md](./contracts/matchmaking-queue.md)
passam (SC-007). `LiveAccountTests` e `LiveServerProbeTests` continuam passando.

---

## 3. Dois jogadores do Multiplayer Play Mode (SC-008)

1. Backend no ar. **Window > Multiplayer > Multiplayer Play Mode**, dois
   jogadores virtuais com contas diferentes, cada uma com o deck inicial.
2. Abra `Assets/Scenes/BootstrapScene.unity` e dê Play.
3. Nos dois, na Home, toque em **Jogar**.

Esperado:

- os dois chegam à `VersusScene` com o apelido e o ícone do oponente certo;
- em seguida os dois recebem `match_start` da mesma partida (Console:
  `connection_proven` no socket de partida e a `MatchScene` carregada);
- nenhum `auth_denied`, nenhum aviso de reconexão, nenhum token no Console;
- a `BootstrapScene` não tem componente de script ausente.

4. Com os dois na partida, desligue o Wi-Fi do computador por 5 s e religue.

Esperado: o aviso de reconexão aparece com a tentativa, some na volta, e o
`match_start` recebido de novo não recarrega a cena.

---

## 4. Android: Wi-Fi → dados procurando partida (SC-009)

Pré-requisitos: build de desenvolvimento, backend acessível pelos dados móveis
(ex.: túnel ou IP público de teste) e host configurado como na quickstart da 001
(extra `serverHost`). Com o backend só na rede local, faça o passo com Wi-Fi →
outra rede Wi-Fi que alcance o computador; o caminho de reciclagem é o mesmo.

1. `adb logcat -s Unity` em outro terminal.
2. Entre, toque em **Jogar** com só um jogador na fila (fica procurando).
3. Desligue o Wi-Fi com os dados móveis ligados.

Esperado, em até 10 s, sem tocar na tela:

- `network_kind_changed previous=LocalArea current=CarrierData`;
- `connection_socket_recycled`;
- `connection_proven` e `queue_join_sent deck_id=…`;
- a fila de volta em procurando; um segundo jogador que entrar agora é pareado.

---

## 5. Android: mais de 5 minutos minimizado procurando (SC-010)

1. Procurando partida, aperte o botão Home do aparelho.
2. Espere 6 minutos (tela apagada vale).
3. Volte ao app.

Esperado no log, nesta ordem, em até 10 s depois da volta:

- retorno ao primeiro plano;
- `access_token_renewal_on_foreground` e `access_token_renewed`;
- `connection_reopening cause=Foreground` (ou `connection_ping_on_foreground`, se
  o socket sobreviveu);
- `connection_proven` e `queue_join_sent deck_id=…` (se o socket caiu);
- nenhum `session_expired`, nenhuma tela de login.

---

## 6. Windows: minimizar procurando (SC-011)

1. Build Windows de desenvolvimento, backend no ar, entre e toque em **Jogar**.
2. Minimize a janela por 2 minutos.
3. Restaure.

Esperado no `Player.log` (`%USERPROFILE%/AppData/LocalLow/<empresa>/<produto>/Player.log`):

- nenhum `socket_closed` e nenhum `connection_suspended` durante os 2 minutos;
- `pong` continuando a cada 10 s (log de depuração `connection_latency`);
- a fila ainda procurando; um segundo jogador que entrar é pareado.

---

## 7. Remoções (SC-002)

Na raiz do projeto:

```bash
grep -rn "using NativeWebSocket\|endel\.nativewebsocket\|WebSocketDispatcher\|ConnectionClient\|TokenRefreshService\|PlayerSession.Instance.Token\|NetworkBootstrap" Assets/Scripts Assets/Tests Assets/Scenes --include=*.cs --include=*.asmdef --include=*.unity
ls Assets/WebSocket
```

Esperado: nenhuma linha no `grep` e `ls` dizendo que a pasta não existe. Comentários
que citam a NativeWebSocket como histórico (`SocketClosure`, `DotNetWebSocket`,
`SocketEndClassifier`) não são referência. `Assets/_Recovery/` é cópia de recuperação
do editor e fica fora da busca.

Abra a `BootstrapScene` no editor e confira que nenhum objeto mostra script ausente.

---

## Critérios de pronto (resumo)

| Critério | Seção |
|---|---|
| SC-001, SC-003, SC-004, SC-005, SC-006, SC-012 | 1 |
| SC-007 | 2 |
| SC-008 | 3 |
| SC-009 | 4 |
| SC-010 | 5 |
| SC-011 | 6 |
| SC-002 | 7 |
