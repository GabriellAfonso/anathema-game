# Contrato: formas do protocolo de partida

**Feature**: `004-match-session`

Formas do socket `ws/match/`, **não copiadas aqui**. Fonte:

- `C:/Users/gabri/Projetos/dev_container/anathema/backend/specs/009-match-protocol/contracts/client_messages.md`
- `.../009-match-protocol/contracts/server_frames.md`
- `.../009-match-protocol/contracts/refusal_codes.md`
- `.../009-match-protocol/data-model.md` (seção "Eventos")
- `.../010-match-timers/contracts/client_messages.md` e `server_frames.md`
- `.../server/apps/game/match/player_view.py` e `documents.py`

Este arquivo diz como o cliente lê e escreve essas formas. Tipos em
[data-model.md](../data-model.md).

---

## Leitura

| Tipo | Lê | Regra |
|---|---|---|
| `MatchStartFrame.Read` | `version`, `view`, `clock` opcional | `clock` ausente → `ClockView.Empty` |
| `MatchUpdateFrame.Read` | `version`, `view`, `events`, `clock` opcional | eventos pela união de `kind`, na ordem |
| `TurnWarningFrame.Read` | `turn_number`, `holder_user_id`, `remaining_ms` | — |
| `PlayerView.Read` | todos os campos de `PlayerView` | `priority_user_id`, `token_holder_user_id`, `outcome`, `combat` opcionais |
| `OwnSideView.Read` / `OpponentSideView.Read` | lados | `hand` só no próprio, `hand_size` só no oponente |
| `BankUnit.Read` | `card`, `damage_taken`, `modifiers` | modificadores pela união de `modifier_kind` |
| `UnitModifier` (união) | `attack`, `health` com `amount`; `damage_immunity` sem | outro valor → `UnrecognizedModifier` |
| `MatchEvent` (união) | os 19 `kind` | outro valor → `UnrecognizedMatchEvent` |
| `MatchPhase` | 7 textos | outro → `Unknown`, `PhaseText` preservado |
| `MatchOutcome.Read` | `defeated_user_id`, `reason` | motivo por `MatchEndReasonText` (002) |
| duração | `permanent`, `until_end_of_round` | por `SpellDurationText` (002); outro → `Unknown` |

Garantias:

- Campo obrigatório faltando ou de tipo errado → `PayloadShapeException` com
  caminho; o codec descarta o frame inteiro e registra `connection_frame_invalid`
  (FR-009).
- Campo a mais → ignorado.
- Identificadores sempre tipados (`UserId`, `CardInstanceId`, `CardId`,
  `MatchId`); listas de cópia por `ReadCardInstanceIdList`.
- Nenhum `JObject`/`JToken` sai do codec; nenhum dicionário solto.

`MatchFrames.CreateUnion()` registra `match_start`, `match_update` e
`turn_warning` sobre `ConnectionFrames.CreateUnion()`. `message_refused` segue o
`MessageRefusedFrame` genérico.

---

## Escrita (comandos)

| Comando | Envelope produzido |
|---|---|
| `MulliganCommand([3, 7])` | `{"type":"mulligan","payload":{"card_instance_ids":[3,7]}}` |
| `MulliganCommand([])` | `{"type":"mulligan","payload":{"card_instance_ids":[]}}` |
| `PlayUnitCommand(12)` | `{"type":"play_unit","payload":{"card_instance_id":12}}` |
| `CastSpellCommand(12, 40)` | `{"type":"cast_spell","payload":{"card_instance_id":12,"target_card_instance_id":40}}` |
| `CastSpellCommand(23, null)` | `{"type":"cast_spell","payload":{"card_instance_id":23}}` |
| `PassCommand()` | `{"type":"pass","payload":{}}` |
| `DeclareAttackCommand([21, 22])` | `{"type":"declare_attack","payload":{"attacker_card_instance_ids":[21,22]}}` |
| `WithdrawAttackerCommand(22)` | `{"type":"withdraw_attacker","payload":{"attacker_card_instance_id":22}}` |
| `ConfirmAttackCommand()` | `{"type":"confirm_attack","payload":{}}` |
| `AssignBlockerCommand(4, 21)` | `{"type":"assign_blocker","payload":{"blocker_card_instance_id":4,"attacker_card_instance_id":21}}` |
| `RemoveBlockerCommand(4)` | `{"type":"remove_blocker","payload":{"blocker_card_instance_id":4}}` |
| `EndDefenseWindowCommand()` | `{"type":"end_defense_window","payload":{}}` |
| `ForfeitCommand()` | `{"type":"forfeit","payload":{}}` |

O codec sempre escreve `payload` (001); o contrato aceita `{}` onde a mensagem
não tem campo. Os construtores só aceitam `CardInstanceId`.

---

## Recusas

`PlayRefusalReader` compara `MessageRefusedFrame.Code` com a tabela fechada:

| Grupo | Códigos |
|---|---|
| forma/transporte | `malformed_message`, `unknown_message_type`, `match_not_found`, `concurrent_match_write`, `internal_error` |
| motor | os 26 de `refusal_codes.md` |
| fora da tabela | `PlayRefusalCode.Unknown`, `CodeText` preservado |

`Error` nunca é comparado; só vai para `PlayRefusal.Error` e para o log.

---

## Testes (`Anathema.Net.Match.Tests/Protocol`)

| Arquivo | Cobre |
|---|---|
| `MatchFramesTests` | união registra os três tipos; frame de partida com campo faltando → inválido com caminho |
| `PlayerViewReadingTests` | US1-1, US1-6; mulligan com nulos; combate com e sem bloqueio; desfecho com os dois motivos e desconhecido |
| `MatchPhaseReadingTests` | 7 fases e desconhecida (SC-002) |
| `UnitModifierUnionTests` | US1-4; 3 tipos, desconhecido, duração desconhecida (SC-002) |
| `MatchEventUnionTests` | US1-2, US1-3; os 19 `kind` e o desconhecido, `Details` de cada um (SC-002) |
| `ClockViewReadingTests` | vez nula, mulligan nulo, os dois, `clock` ausente |
| `TurnWarningFrameTests` | US1-8 |
| `PlayRefusalReaderTests` | US1-7; os 31 códigos e o desconhecido (SC-002) |
| `PlayCommandEncodingTests` | US4-1; cada linha da tabela de escrita (SC-007) |
| `CommandIdentityTests` | US4-7; nenhum parâmetro público `CardId`/`long`/`int` |
| `RecordedFramesTests` | cada `recorded-*.json` decodifica como frame válido |
