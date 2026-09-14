# Quickstart: Partida

**Feature**: `004-match-session`

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
- `Anathema.Net.Match.Tests` presente, com os arquivos das tabelas de
  [protocol-shapes.md](./contracts/protocol-shapes.md),
  [match-state.md](./contracts/match-state.md) e
  [live-match.md](./contracts/live-match.md);
- `MatchAssemblyBoundaryTests`, `ConnectionAssemblyBoundaryTests` e
  `CoreAssemblyBoundaryTests` passam (SC-004);
- `LiveMatchTests` e os outros `LiveServer` aparecem como não executados;
- nenhum aviso de compilação vindo de `Assets/Scripts/` (SC-001);
- testes de `Anathema.Net.Match.Tests` somados abaixo de 5 s (SC-011).

---

## 2. O marco: dois bots jogam uma partida (SC-009)

1. Suba o backend:

   ```bash
   cd C:/Users/gabri/Projetos/dev_container/anathema/backend
   docker compose up
   ```

2. Confira que `http://127.0.0.1:8000/game/cards/` responde 401 sem token.
3. Rode só o marco, editor fechado:

   ```bash
   "C:/Program Files/Unity/Hub/Editor/6000.2.8f1/Editor/Unity.exe" -batchmode -projectPath . -runTests -testPlatform EditMode -testCategory LiveServer -testFilter "Anathema.Net.Unity.Tests.LiveMatchTests" -testResults Logs/liveserver-match-results.xml -logFile Logs/liveserver-match.log
   ```

Esperado:

- o teste passa em menos de 600 s;
- em `Logs/liveserver-match.log`:
  - linhas `match_event`, da primeira `mulligan_taken` à `match_finished`, com
    apelidos e nomes de carta;
  - `match_status` de P2 passando por `Reconnecting` e voltando a `Live` perto
    da rodada 3;
  - `match_status` `Finished` nos dois;
  - no máximo 20 `match_play_refused` por bot e nenhum com
    `code=unknown_message_type`;
- `LiveQueueTests`, `LiveAccountTests` e `LiveServerProbeTests` continuam
  passando.

---

## 3. Gravar fixtures reais

Depois de uma rodada do §2:

1. Abra `Logs/match-recording/<match_id>/`: um arquivo por frame recebido pelo
   P1, numerado na ordem.
2. Copie para `Assets/Tests/EditMode/Net.Match/Fixtures/`, renomeando para
   `recorded-<o-que-mostra>.json`, pelo menos:
   - o primeiro `match_start` (mulligan);
   - um `match_update` com `round_started` e `cards_drawn` dos dois lados;
   - um em `declaration` com `combat`;
   - um com `blocker_assigned`;
   - um com modificador de ataque até o fim da rodada;
   - um `turn_warning`, se houver;
   - o `match_start` de reconexão;
   - o `match_update` em `finished`.
3. Abra o editor para o Unity gerar os `.meta`; faça commit dos `.json` com os
   `.meta`.
4. Rode o §1: `RecordedFramesTests` decodifica cada um.

Os frames não levam token. Apelidos são das contas descartáveis do teste.

---

## 4. Dois jogadores do Multiplayer Play Mode (SC-010)

1. Backend no ar. **Window > Multiplayer > Multiplayer Play Mode**, dois
   jogadores virtuais com contas diferentes.
2. `Assets/Scenes/BootstrapScene.unity`, Play.
3. Nos dois, na Home, toque em **Jogar**.

Esperado:

- os dois passam pela `VersusScene` e chegam à `MatchScene`;
- o console mostra `match_status` `Live` e linhas `match_event` do mulligan;
- no jogador A, desligue o Wi-Fi da máquina por 5 s (ou pare o container
  `backend` e suba de novo): o aviso de reconexão aparece, a `MatchScene` **não**
  recarrega, e ao voltar `match_status` passa a `Live`;
- sem ação, o mulligan estoura em 30 s pelo servidor (`mulligan_timed_out` no
  log) e a partida segue.

---

## 5. Remoções e fronteira (SC-003, SC-004)

Na raiz do projeto:

```bash
grep -rn "MatchStateDTO\|RawTextReceived" Assets --include=*.cs
grep -rln "JsonConvert\|using Newtonsoft" Assets/Scripts --include=*.cs | grep -v "^Assets/Scripts/Net/Json/"
```

Esperado: nenhuma saída nos dois.

---

## 6. Nenhuma regra de jogo (SC-008)

Revisão do diff em `Assets/Scripts/Net/Match/`:

- nenhum código compara custo com energia, conta cartas do banco contra o teto,
  decide se uma fase aceita uma jogada, subtrai Nexus ou aplica dano;
- `DisplayedUnitStats` e `HandCardHints` só são lidos por `LiveMatch.StatsOf`,
  `LiveMatch.HintFor` e pelos testes (inclusive o bot do marco);
- `TurnClock` não tem temporizador nem evento de zero;
- `CommandsIgnoreHintsTests` e `MatchCoreIsolationTests` passam.

---

## Critérios de pronto (resumo)

| Critério | Onde |
|---|---|
| Suíte EditMode passa sem aviso novo | §1 |
| `MatchStateDTO`, `RawTextReceived` e `JsonConvert` fora do codec removidos | §5 |
| Núcleo sem UnityEngine, Newtonsoft, `ClientWebSocket` | §1 |
| Nenhuma regra de jogo | §6 |
| Marco LiveServer passa e o log conta a partida | §2 |
