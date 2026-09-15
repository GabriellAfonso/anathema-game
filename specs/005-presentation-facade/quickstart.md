# Quickstart: Fachada da apresentação e prova final

**Feature**: `005-presentation-facade`

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
- presentes `Anathema.Net.Facade.Tests`, `Anathema.Client.Scenes.Tests` e
  `Anathema.Client.Proof.Tests`, com os testes das tabelas de
  [client-state.md](./contracts/client-state.md) e
  [composition-and-scenes.md](./contracts/composition-and-scenes.md);
- `SurfaceContractTests`, `SurfaceHasNoEventTests`, `FriendAssemblyTests`,
  `FacadeAssemblyBoundaryTests`, `SceneManagerUsageTests` e
  `StaticInstanceUsageTests` passando (SC-005, SC-006);
- os testes `LiveServer` aparecendo como não executados;
- nenhum aviso de compilação vindo de `Assets/Scripts/` (SC-001);
- os testes de `Anathema.Net.Facade.Tests` somando menos de 5 s (SC-010).

## 2. Conferência de fronteira à mão

Num script qualquer de `Assets/Scripts/Presentation/`, escreva
`Anathema.Net.Connection.AuthenticatedConnection x;` e salve.

Esperado: erro de compilação de acessibilidade (`CS0122`). Desfaça a linha
(US8, cenário 4).

## 3. Prova final (SC-007)

1. Suba o backend:

   ```bash
   cd C:/Users/gabri/Projetos/dev_container/anathema/backend
   docker compose up
   ```

2. Confira que `http://127.0.0.1:8000/game/cards/` responde 401 sem token.
3. Editor fechado, rode só a prova:

   ```bash
   "C:/Program Files/Unity/Hub/Editor/6000.2.8f1/Editor/Unity.exe" -batchmode -projectPath . -runTests -testPlatform EditMode -testCategory LiveServer -testFilter "Anathema.Client.Proof.Tests.MatchProofTests" -testResults Logs/liveserver-proof-results.xml -logFile Logs/liveserver-proof.log
   ```

Esperado:
- o teste passa em menos de 15 min;
- em `Logs/liveserver-proof.log`:
  - 18 linhas `proof_step` com `passed=true`, na ordem de
    [match-proof.md](./contracts/match-proof.md#passos);
  - `proof_coverage` com `missing` vazio;
  - `client_stage` de `P2` sem troca durante a queda;
  - depois de `proof_clock_jump`, `access_token_renewed` antes de
    `connection_opening` para `P2`;
  - `match_history_row` com `status=Resolved` para a partida 1 e a partida 2;
- `LiveAccountTests`, `LiveQueueTests` e `LiveServerProbeTests` continuam
  passando com o mesmo comando e `-testFilter` de cada um.

## 4. Multiplayer Play Mode (SC-008)

1. Backend no ar. Abra o projeto e habilite dois jogadores com as tags `Player1`
   e `Player2` no Multiplayer Play Mode.
2. Dê Play.

Esperado, nos dois jogadores:
- a `LoginScene` retoma ou entra sozinha (login automático de desenvolvimento) e
  o roteador carrega a `HomeScene`, com o mini perfil preenchido;
- apertar **Jogar** nos dois carrega a `VersusScene` com os dois apelidos e
  ícones, e em seguida a `MatchScene`;
- no Console de um jogador, `client_stage` `Paired → InMatch` uma vez;
- derrubar o servidor por 5 s (`docker compose stop web`, depois `start`) faz o
  overlay aparecer e sumir, sem nova linha de carga de `MatchScene` e com a
  hierarquia intacta;
- parar o Play não deixa exceção de objeto destruído no Console.

## 5. Android, build de desenvolvimento (SC-009)

1. Em **Build Profiles** (Android), marque **Development Build** e inclua
   `Assets/Scenes/Dev/MatchProofScene.unity` no índice 0, temporariamente.
2. Descubra o IP do computador na rede local e passe como host de lançamento,
   como o probe da 001:

   ```bash
   adb shell am force-stop com.UnityTechnologies.com.unity.template.urpblank
   adb shell am start -n com.UnityTechnologies.com.unity.template.urpblank/com.unity3d.player.UnityPlayerGameActivity -e serverHost 192.168.0.10:8000
   ```

3. Acompanhe `adb logcat -s Unity`.
4. No meio da partida 1 (após `proof_step` do passo 9), aperte Home, espere 20 s
   e volte ao app.

Esperado:
- `proof_finished passed=true`;
- entre a volta e o fim, a `LiveMatch` de algum cliente passa por
  `Reconnecting → Live`, e a partida segue até o fim.

Depois: remova `MatchProofScene` da lista de cenas antes de commitar (research
R12).
