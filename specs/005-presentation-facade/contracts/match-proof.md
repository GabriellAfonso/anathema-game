# Contrato: prova final

**Feature**: `005-presentation-facade`

Roteiro da US9, executado por `MatchProofScript.RunAsync` em
`Anathema.Client.Proof`. A assembly não é amiga de nenhuma outra: tudo o que
ela usa é superfície ou composição (research R2, R11). Referência de
comportamento dos bots: `backend/scripts/smoke_match.py`.

---

## Montagem

- Dois `ProofPlayer`, `P1` e `P2`, cada um com o seu `ClientComposition.Compose`:
  - slot `proof-p1-<sufixo>` / `proof-p2-<sufixo>`;
  - `ProofLog`;
  - `AllowCleartext` ligado;
  - `AllowClockJumps` só no `P2`.
- Quem bombeia:
  - no teste, uma corrotina chama `Pump()` dos dois a cada quadro;
  - no aparelho, `AttachTo` no objeto do runner.
- Tempo limite total: 15 min. Em falha, o roteiro informa o passo, a rodada, a
  fase e o estágio de cada cliente.

## Passos

| # | Passo | Verificação |
|---|---|---|
| 1 | Cadastrar `P1` e `P2`, entrar | `SignedIn` nos dois |
| 2 | Descartar os dois `ComposedClient`; compor de novo nos mesmos slots; `ResumeAsync` | `Resumed`, `SignedIn`, mesmo `UserId` |
| 3 | `Catalog.LoadAsync` | 29 cartas |
| 4 | `Decks.ListAsync` | deck inicial sem nenhum `SpellCard` |
| 5 | `Decks.CreateAsync` do deck do `smoke_match.py` (cartas 1–8 ×3, 9, 1001–1005 ×3) | criado |
| 6 | `Decks.CreateAsync` com 12 cartas | `DeckRefusal` com `WrongDeckSize` |
| 7 | `P1`: `Queue.Join` com `DeckId` que não existe | `Queue.Refused` `DeckNotFound`, `SignedIn` |
| 8 | Os dois: `Queue.Join` com o deck de feitiços | `Paired`, mesmo `MatchId` |
| 9 | Partida 1: bots jogando | ver abaixo |
| 10 | Rodada ≥ 2, primeira Fase de Ação de `P2`: `P2` não age | `P2` vê `Clock.TurnRunningOut` da vez; depois os dois espelhos recebem `turn_timed_out` e `passed` no mesmo `match_update` |
| 11 | Rodada 3: `P2.DropSockets()` | `P2.Health.Reconnecting`, depois `Recovered`; estágio `InMatch`; mesma instância de `CurrentMatch`; `Mirror.Version` avança depois |
| 12 | Depois do passo 11, numa vez de `P1` (com `P2` fora da vez parada e já de volta da queda): `P2.JumpClock(5 min 30 s)` e `P2.DropSockets()` | no `ProofLog` de `P2`, depois da marca do salto: `access_token_renewed` antes de `connection_opening`; `Recovered`; partida segue |
| 13 | Fim da partida 1 | os dois `MatchFinished`; mesmo `DefeatedUser` e `Reason`; `Won` diferente; `ResultUpdated` com `Resolved` e `Row.Match` igual |
| 14 | Cobertura | `CommandCoverage.Missing` vazio |
| 15 | Os dois: `ReturnToLobby` | `SignedIn` |
| 16 | Partida 2: `Queue.Join` nos dois; `P1` manda `Forfeit` no mulligan | `MatchFinished`, motivo `Forfeit`; linha com `FinalRound` 1 |
| 17 | `History.ReadPageAsync` nos dois | as duas partidas nas duas contas; em cada partida, um `Won` verdadeiro e um falso |
| 18 | Encerramento | `SignOut` nos dois (apaga a guarda) e `Dispose` |

O passo 12 fica numa vez em que `P2` não está parado nem reconectando. A ordem
10 → 11 → 12 é a esperada, mas cada passo espera a sua condição, e o roteiro só
falha pelo tempo limite.

## Bots (`ProofBot` + `ProofStrategy`)

Mesma ordem de candidatos do `smoke_match.py` (a `SmokeStrategy` da 004), sobre
`LiveMatch.HintFor` e o espelho:

- **Mulligan**: `P1` troca a primeira carta, `P2` não troca.
- **Ação**:
  1. feitiço que não é só da declaração (com alvo, se houver candidato);
  2. declarar ataque com o banco inteiro, se tem o token e ele não foi usado;
  3. unidade mais barata que cabe (banco < 6);
  4. passar.
- **Declaração**: puxar o último atacante uma vez na partida (com 2 ou mais);
  depois feitiço só da declaração; depois confirmar.
- **Defesa**: atribuir o primeiro do banco ao primeiro atacante (sem bloqueio
  ainda); depois encerrar.
- **Rodada 30 com a vez**: desistir.
- Não repete, na mesma versão, candidato já tentado.

Extensões (research R11):
- **Remover bloqueador uma vez**: com bloqueio próprio aceito e "remover" não
  coberto, remove esse bloqueador antes de encerrar.
- **Feitiço sem alvo**: `LIFE POTION` ignora o teto de Nexus 12 enquanto "feitiço
  sem alvo" não foi coberto.
- **Vez parada**: `P2` não age na vez escolhida (passo 10) e volta a agir quando
  a vez muda.
- **Segurar o ataque**: com os seis itens de combate cobertos e "feitiço sem alvo"
  ainda não, ninguém declara ataque, para a partida durar até sair uma `LIFE POTION`
  (a primeira prova acabou por Nexus na rodada 7 sem nenhuma).

Envio conta na cobertura quando o `match_update` seguinte traz o evento
correspondente. Um envio recusado não conta.

Limites herdados: no máximo 20 recusas por bot e nenhuma `UnknownMessageType`.
Um bot sem candidato falha com a fase e a versão.

## Execução

- **Editor**: `MatchProofTests` (`[Explicit]`, `[Category("LiveServer")]`) em
  `Anathema.Client.Proof.Tests`. `[UnityTest]` bombeando e aguardando a tarefa do
  roteiro.
- **Aparelho**: `MatchProofRunner` na `Assets/Scenes/Dev/MatchProofScene.unity`.
  Lê o `AppConfig` (host de lançamento), roda o roteiro e escreve `proof_step` e
  `proof_finished` no log do aparelho.

## Log da prova

- `proof_step` (`step`, `passed`, `detail`);
- `proof_clock_jump` (`player`, `forward_ms`), a marca usada no passo 12;
- `proof_coverage` (`missing`);
- `proof_finished` (`passed`, `seconds`).
