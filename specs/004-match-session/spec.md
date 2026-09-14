# Feature Specification: Partida

**Feature Branch**: `004-match-session` (nenhum branch criado; spec no `main`)

**Created**: 2026-09-14

**Status**: Draft

**Input**: User description: "Partida: formas do protocolo de partida, espelho do estado, relógio da vez, comandos, recusas, dicas de mira e sessão de partida. Quarta de cinco features da camada de rede. É o primeiro marco jogável: dois clientes headless jogando uma partida inteira, vista pelo log." (descrição completa na mensagem que abriu esta feature)

## Contexto

Quarta de cinco features da camada de rede do cliente, e o primeiro marco
jogável. A conexão autenticada e a fila funcionam (feature 003), mas a partida
não: o `MatchClient` desserializa `match_start` num `MatchStateDTO` obsoleto
(`your_hand`/`board`/`turn`) por uma ponte de `RawTextReceived` + `JsonConvert`
(research da 003, R12) e grava num `MatchSession` MonoBehaviour singleton. Não há
`match_update`, relógio, comando nem recusa.

Esta feature constrói, sobre a 001 (`IProtocolCodec`, `DiscriminatedUnion`,
`IOutgoingMessage`, `IMonotonicClock`, `IClientLog` e os fakes), a 002
(`UserId`, `CardId`, `LoadedCatalog`/`CardLookup`/`SpellEffect`, `PlayerDecks`)
e a 003 (`AuthenticatedConnection` — `FrameReceived`, `SendAsync`,
`StatusChanged`, `Recovered`, `Leave` —, `ConnectionTarget.Match`, `MatchId`,
`CardInstanceId`, `MatchQueue`), o que a parte visual vai precisar: um estado
espelhado, eventos em ordem, um relógio desenhável, comandos prontos, recusas
tipadas e dicas de mira. Regra de jogo nenhuma mora no cliente.

Os "usuários" desta feature são o jogador — que joga uma partida inteira, cai e
volta sem perder nada — e a feature 5, que liga uma apresentação a uma camada
que já joga sozinha. Enquanto nada é desenhado, a prova é o log.

**Contratos e regras** — são do backend e do vault, e não são copiados aqui.
Fonte da verdade, em `C:/Users/gabri/Projetos/dev_container/anathema/backend/`:

- `specs/009-match-protocol/contracts/client_messages.md` — os 11 comandos
- `specs/009-match-protocol/contracts/server_frames.md` — `match_start`, `match_update`, `PlayerView`, eventos
- `specs/009-match-protocol/contracts/refusal_codes.md` — `message_refused` e o conjunto fechado de códigos
- `specs/009-match-protocol/data-model.md`, seção "Eventos" — os 17 eventos e seus campos
- `specs/010-match-timers/contracts/client_messages.md` — `cast_spell` sem alvo
- `specs/010-match-timers/contracts/server_frames.md` — `ClockView`, `turn_warning`, `turn_timed_out`, `mulligan_timed_out`
- `specs/002-match-state/data-model.md`, `server/apps/game/match/player_view.py` e `server/apps/game/match/documents.py` — forma base do `PlayerView`, da unidade no banco e dos modificadores
- `scripts/smoke_match.py` — referência executável da estratégia dos bots

E `C:/Users/gabri/Obsidian/Projetos/Anathema/Game/Fluxo de Partida.md` para o
que cada fase espera do jogador.

Quando esta spec e um contrato discordam, vale o contrato.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Ler tudo o que o servidor manda na partida (Priority: P1)

Cada frame do socket de partida chega ao cliente como um tipo fechado e
completo: a visão do jogador com os dois lados, o combate, o desfecho, o
relógio, e a lista de eventos, cada um com sua forma. Nada chega como texto cru
nem dicionário solto, e um valor que o cliente ainda não conhece — fase, evento,
modificador, código de recusa — vira um tipo explícito de "desconhecido" em vez
de derrubar a leitura.

**Why this priority**: tudo o mais (espelho, relógio, dicas, sessão, narrador)
lê essas formas. Sem elas, a ponte de texto cru da 003 não sai.

**Independent Test**: testes EditMode decodificam, pelo codec do projeto,
fixtures tiradas dos exemplos dos contratos e de frames reais gravados de uma
rodada do teste LiveServer, e verificam cada campo, cada evento, cada
modificador, cada fase, cada código de recusa e os desconhecidos.

**Acceptance Scenarios**:

1. **Given** um `match_start` gravado no mulligan, **When** é decodificado, **Then** a visão traz a própria mão com `card_instance_id` e `card_id` tipados, o tamanho da mão do oponente, banco, cemitério, tamanho do deck, energia, Nexus, perfil (`UserId`, apelido, ícone, nível) e `mulligan_taken` dos dois lados; a fase é mulligan; prioridade e dono do token são nulos; combate e desfecho são nulos; o relógio tem vez nula e o prazo do mulligan em milissegundos.
2. **Given** um `match_update` com um evento de cada um dos 19 `kind` dos contratos, **When** é decodificado, **Then** cada evento vira o seu tipo, na ordem da lista, com os campos do contrato tipados (`UserId`, `CardInstanceId`, carta com `CardId`, quantidades, número da rodada, desfecho).
3. **Given** um evento `cards_drawn` do oponente, **When** é decodificado, **Then** a quantidade vem preenchida e a lista de cartas vem vazia, sem erro.
4. **Given** uma unidade no banco com um modificador de ataque até o fim da rodada, um de vida permanente e uma imunidade a dano, **When** é decodificada, **Then** os três viram tipos distintos por `modifier_kind`, os dois primeiros com `amount` e a imunidade sem ele, cada um com sua duração.
5. **Given** frames com fase, `kind` de evento, `modifier_kind` e duração que o cliente não conhece, **When** são decodificados, **Then** a leitura não falha: cada valor vira seu tipo desconhecido preservando o texto recebido, e o resto do frame é lido normalmente.
6. **Given** um `match_update` em Declaração, **When** é decodificado, **Then** o combate traz a lista de atacantes e a lista de pares bloqueador/atacante (vazia se ninguém bloqueou); **Given** um frame em fase terminada, **Then** o desfecho traz o `UserId` derrotado e o motivo (Nexus zerado ou desistência, mais desconhecido).
7. **Given** um `message_refused` de cada um dos códigos de forma/transporte e de motor, e um com código inventado, **When** são decodificados, **Then** cada um vira seu código tipado, o desconhecido preserva o texto do `code`, e o `error` fica disponível só para log.
8. **Given** um `turn_warning`, **When** é decodificado, **Then** traz `turn_number`, `UserId` do dono da vez e o restante em milissegundos.

---

### User Story 2 - Um espelho da partida que nunca volta no tempo (Priority: P1)

O cliente mantém a partida exatamente como o servidor mandou por último. Cada
frame aceito substitui o estado inteiro; frame velho ou repetido é ignorado
inteiro, mesmo que chegue depois de uma reconexão. Quem desenha recebe, na
thread principal e em ordem, o estado anterior e o novo, cada evento da lista,
e avisos de fase mudou, prioridade mudou e partida terminou — e pode perguntar
fatos prontos: é minha prioridade, sou dono do token, meu mulligan está
pendente, o oponente já respondeu o mulligan, a partida terminou, eu venci.

**Why this priority**: é a fonte única do que a apresentação desenha. Um espelho
que mescla ou aceita versão velha diverge do servidor em silêncio.

**Independent Test**: EditMode alimenta o espelho com sequências de frames
decodificados (fora de ordem, duplicados, com buraco de versão, reconexão) e
verifica o estado exposto, os fatos e a ordem dos avisos, sem socket.

**Acceptance Scenarios**:

1. **Given** um espelho vazio, **When** chega `match_start` versão 4, **Then** o estado é o da versão 4, a identidade própria é o `UserId` de `you.profile`, e sai um aviso "estado substituído" com anterior nulo.
2. **Given** o espelho na versão 4, **When** chega `match_update` versão 7 (5 e 6 nunca chegaram), **Then** o estado é o da versão 7, sem erro nem espera por versões intermediárias.
3. **Given** o espelho na versão 7, **When** chega `match_update` versão 6 ou outra versão 7, **Then** nada muda e nenhum aviso sai.
4. **Given** o espelho na versão 7 depois de uma queda, **When** chega `match_start` de reconexão versão 7, **Then** o estado não é trocado e nenhum aviso de estado sai; **When** chega `match_start` versão 9, **Then** o estado é o da versão 9 e os avisos saem normalmente, sem eventos (o `match_start` não traz lista).
5. **Given** um `match_update` que muda a fase de ação para declaração, passa a prioridade e traz três eventos, **When** é aceito, **Then** os avisos saem nesta ordem: estado substituído (anterior, atual), os três eventos na ordem recebida, fase mudou, prioridade mudou.
6. **Given** a fase mulligan com `you.mulligan_taken` falso e `opponent.mulligan_taken` verdadeiro, **When** se consulta o espelho, **Then** "meu mulligan pendente" e "oponente já respondeu" são verdadeiros, e "é minha prioridade" e "sou dono do token" são falsos.
7. **Given** um `match_update` em fase terminada com desfecho em que o derrotado é o oponente, **When** é aceito, **Then** sai "partida terminou" com o desfecho e "venci" verdadeiro, exatamente uma vez, e os fatos "partida terminada" e "eu venci" passam a verdadeiros.
8. **Given** uma unidade própria de ataque base 3 e vida base 4 no catálogo, com +3 de ataque, +1 de vida e 2 de dano sofrido, **When** se pedem os atributos exibidos, **Then** são ataque 6 e vida 3 — e o valor está documentado como conveniência de exibição, sem nenhum uso em decisão.

---

### User Story 3 - Um relógio da vez que se desenha sozinho e não decide nada (Priority: P1)

A apresentação pergunta a qualquer momento quanto falta da vez e de quem ela é,
e quanto falta do próprio mulligan. O cliente conta a partir do que o servidor
mediu e do instante em que o frame chegou, no relógio monotônico do aparelho.
Avisa quando começa uma vez nova e, uma única vez por vez, quando o tempo está
acabando. Chegar a zero não faz nada: quem estoura a vez é o servidor.

**Why this priority**: toda vez tem relógio (§15 do Fluxo de Partida), e errar a
âncora mostra tempo que o jogador não tem. Não depende de socket, só do
espelho.

**Independent Test**: EditMode com `FakeMonotonicClock` avançando entre frames e
consultas, verificando restante, avisos e o que acontece com frames descartados,
`turn_warning` de outra vez e reconexão.

**Acceptance Scenarios**:

1. **Given** um frame aceito às 10.000 ms monotônicos com a vez 12 do oponente e 25.000 ms restantes, **When** o relógio do aparelho marca 13.000 ms, **Then** o restante é 22.000 ms e o dono é o oponente; **When** marca 40.000 ms, **Then** o restante é 0, e nenhum comando nem aviso sai por isso.
2. **Given** o relógio na vez 12, **When** chega um frame aceito com a vez 13, **Then** sai o aviso "vez nova" com o número e o dono; **When** chega um frame aceito com a vez 12 e restante menor (feitiço, atacante mandado), **Then** o relógio é reancorado e nenhum aviso de vez nova sai.
3. **Given** o relógio na vez 12, **When** chega `turn_warning` da vez 12, **Then** sai "tempo acabando" uma vez; **When** o mesmo `turn_warning` chega de novo, **Then** nada sai; **When** chega `turn_warning` da vez 11, **Then** é ignorado.
4. **Given** a fase mulligan e o próprio mulligan pendente com 30.000 ms, **When** passam 12.000 ms no relógio do aparelho, **Then** o restante do mulligan é 18.000 ms e a vez é nula; **When** chega um frame aceito com o prazo do mulligan nulo, **Then** o relógio de mulligan deixa de existir.
5. **Given** o relógio ancorado pela versão 7, **When** chega `match_update` versão 6 com outro restante, **Then** o relógio não muda.
6. **Given** o relógio ancorado pela versão 7 com 25.000 ms, o jogador cai e fica 8 s fora, **When** chega `match_start` de reconexão versão 7 com 17.000 ms, **Then** o estado espelhado não é trocado, mas o relógio é reancorado nos 17.000 ms, a partir do instante em que esse frame chegou.
7. **Given** a partida terminada, **When** chega o frame final com vez nula, **Then** o relógio da vez deixa de existir e nenhum aviso de vez nova sai.

---

### User Story 4 - Jogar por comandos e entender as recusas (Priority: P1)

A apresentação (ou um bot) joga chamando comandos que só aceitam a cópia da
carta na partida. Um comando só sai com a conexão conectada; fora disso, volta
um resultado explícito de "não enviado", sem fila escondida. Enquanto o
servidor não responde, o comando fica marcado como pendente — só para evitar
clique duplo, sem impedir outro envio. Se o servidor recusa, a recusa chega
tipada pelo código, associada por melhor esforço ao último comando enviado.

**Why this priority**: sem comando não há partida jogável; sem recusa tipada o
jogador não sabe por que nada aconteceu.

**Independent Test**: EditMode com `FakeWebSocket` sob a conexão da 003
verifica o JSON exato de cada comando, o resultado fora de "conectado", o ciclo
do pendente e a associação da recusa; um teste de compilação negativo garante
que `CardId` não entra onde se espera `CardInstanceId`.

**Acceptance Scenarios**:

1. **Given** a conexão conectada, **When** cada um dos 11 comandos é chamado, **Then** o texto enviado é o envelope do contrato 009 com o `type` e o `payload` exatos: `mulligan` com a lista (vazia inclusive), `play_unit`, `cast_spell` com alvo, `cast_spell` sem alvo, `pass`, `declare_attack` com a lista, `withdraw_attacker`, `confirm_attack`, `assign_blocker` com bloqueador e atacante, `remove_blocker`, `end_defense_window` e `forfeit`.
2. **Given** a conexão reconectando, esperando nova tentativa ou desistida, **When** um comando é chamado, **Then** nada é enviado, o resultado é "não enviado" com o estado da conexão, nada fica pendente, e nada é mandado quando a conexão voltar.
3. **Given** um `play_unit` enviado, **When** se consulta, **Then** há um comando pendente; **When** chega o próximo `match_update` aceito, **Then** o pendente é limpo. O mesmo vale quando chega uma recusa, e quando a conexão avisa que voltou depois de cair.
4. **Given** um `play_unit` pendente, **When** a apresentação chama outro comando, **Then** ele é enviado normalmente e passa a ser o pendente.
5. **Given** um `play_unit` enviado, **When** chega `message_refused` com `not_enough_energy`, **Then** sai uma recusa tipada com esse código, o `error` do servidor para log, e o `play_unit` como "provável comando recusado" — documentado como associação por melhor esforço, porque o servidor não ecoa a mensagem.
6. **Given** nenhum comando enviado desde a última atualização aceita, **When** chega uma recusa, **Then** ela sai sem comando associado.
7. **Given** um trecho de código que passa um `CardId` a `play_unit`, **When** compila, **Then** a compilação falha.

---

### User Story 5 - Dicas de mira sem decidir por ninguém (Priority: P2)

Ao olhar uma carta da mão, a apresentação sabe se ela é unidade ou feitiço; se
é feitiço, se pede alvo e de que lado, quais cópias no banco daquele lado são
candidatas, se só vale na declaração, e o custo ao lado da energia atual. A
camada não esconde, não desabilita e não bloqueia nada: um comando que o
servidor vai recusar ainda é enviado.

**Why this priority**: é conveniência de interface e depende do espelho e do
catálogo; a partida é jogável sem ela (o servidor recusa), mas os bots e a
futura tela a usam para escolher alvo.

**Independent Test**: EditMode com um `LoadedCatalog` de fixture e um espelho
em estado conhecido, verificando a dica para cada caso.

**Acceptance Scenarios**:

1. **Given** uma unidade de custo 3 na mão e energia atual 2, **When** se pede a dica, **Then** ela diz "unidade", custo 3 e energia 2, e mais nada; enviar `play_unit` com ela continua possível.
2. **Given** um feitiço sem alvo, **When** se pede a dica, **Then** ela diz "feitiço", não pede alvo, e a lista de candidatos é vazia.
3. **Given** um feitiço de alvo aliado e duas unidades próprias no banco, **When** se pede a dica, **Then** ela diz alvo aliado e lista as duas cópias próprias, na ordem do banco.
4. **Given** um feitiço de alvo inimigo e o banco do oponente vazio, **When** se pede a dica, **Then** ela diz alvo inimigo e a lista vazia, sem esconder a carta.
5. **Given** um feitiço marcado como só na declaração, **When** se pede a dica fora da declaração, **Then** ela informa "só na declaração" junto da fase atual, e não impede o envio.
6. **Given** uma cópia da mão cujo `card_id` não está no catálogo, **When** se pede a dica, **Then** ela vem como "carta desconhecida" com o `CardId`, registrada no log, sem exceção.

---

### User Story 6 - Uma sessão de partida que atravessa quedas e termina sozinha (Priority: P1)

Pareado, o cliente abre a sessão da partida pelo `MatchId`. A sessão conecta,
fica ao vivo quando o primeiro `match_start` chega, e junta espelho, relógio e
comandos. Se o socket cai, o estado continua visível e marcado como
desatualizado até a reconexão trazer um `match_start` que ressincroniza. Se o
servidor recusa a partida, a sessão diz que foi recusada. Quando a partida
termina, a sessão publica o desfecho e fecha o socket de propósito, porque o
servidor não fecha. Um narrador escreve no log uma linha por evento, com nomes de
carta e apelidos.

**Why this priority**: é o que o `MatchClient` e as cenas usam, e é o que os
dois bots headless dirigem no marco.

**Independent Test**: EditMode com `FakeWebSocket`, `FakeMonotonicClock` e os
fakes da 003 roteiriza conexão, `match_start`, quedas, `match_denied` e fim de
partida, verificando estados, avisos, fechamento do socket e linhas do
narrador num `IClientLog` fake.

**Acceptance Scenarios**:

1. **Given** um `MatchId`, **When** a sessão começa, **Then** a conexão abre no alvo de partida com esse `MatchId` e o estado é conectando; **When** chega o primeiro `match_start`, **Then** o estado é ao vivo e o espelho tem o estado recebido.
2. **Given** a sessão ao vivo na versão 7, **When** o socket cai, **Then** o estado é reconectando, o estado espelhado continua exposto e marcado como desatualizado, e os comandos devolvem "não enviado"; **When** a conexão volta e chega `match_start` versão 9, **Then** o estado é ao vivo, a marca de desatualizado some, e o espelho está na versão 9.
3. **Given** a sessão conectando, **When** a conexão desiste por `match_denied`/44xx, **Then** o estado é recusada, com o motivo da 003, e nada mais é aberto.
4. **Given** a sessão ao vivo, **When** a conexão desiste por outro motivo (sessão expirada, token recusado repetidamente), **Then** o estado é desistiu com esse motivo e o estado espelhado continua legível.
5. **Given** a sessão ao vivo, **When** chega um `match_update` em fase terminada, **Then** o espelho publica o desfecho, o estado é terminada, e o socket é fechado de propósito, sem reconexão e sem aviso de queda.
6. **Given** um `match_update` com `unit_played` de uma carta 5 do jogador "gabriel", **When** é aceito, **Then** o narrador registra uma linha estruturada com a rodada, o `kind`, o apelido e o nome da carta 5 do catálogo; o mesmo, uma linha por evento, para cada evento da lista.
7. **Given** o fluxo de cena atual, **When** chega o primeiro `match_start`, **Then** a `MatchScene` é carregada como hoje; **When** chega o `match_start` de uma reconexão, **Then** a cena não é recarregada.

---

### User Story 7 - Dois clientes headless jogam uma partida inteira (Priority: P1)

Dois clientes no mesmo processo, só com adaptadores reais e as features 002 a
004, criam contas novas, montam o deck com feitiços, entram na fila, são
pareados e jogam a partida do mulligan ao desfecho com a estratégia dos bots do
`smoke_match.py`. No meio, um deles perde o socket, volta e continua jogando.
Os dois terminam com o mesmo desfecho, e o log conta a partida inteira.

**Why this priority**: é o marco da feature — prova de ponta a ponta, sem nada
visível, que a camada joga sozinha contra o servidor real.

**Independent Test**: teste `[Explicit]` `[Category("LiveServer")]` contra o
backend local subido com `docker compose up`.

**Acceptance Scenarios**:

1. **Given** o backend local, **When** o teste roda, **Then** duas contas novas carregam o catálogo, criam o deck com feitiços do `smoke_match.py` (`spell_deck_id`), entram na fila, recebem `match_found` com o mesmo `MatchId`, e abrem cada uma sua sessão de partida.
2. **Given** a partida em mulligan, **When** os bots decidem, **Then** P1 troca a primeira carta da mão e P2 não troca nenhuma.
3. **Given** a vez de um bot na Fase de Ação, **When** ele decide, **Then** segue a ordem do `smoke_match.py`: feitiço que não é só de declaração, com alvo quando houver candidato; declarar ataque com o banco inteiro se tem o token não consumido; a unidade mais barata que cabe; passar — e não repete, na mesma versão, uma jogada que já foi recusada.
4. **Given** a Declaração do atacante com dois ou mais atacantes, **When** ele decide, **Then** puxa o último de volta uma única vez na partida, depois feitiço só de declaração se houver, depois confirma; **Given** a defesa sem bloqueio, **When** o defensor decide, **Then** bloqueia o primeiro atacante com a primeira unidade do banco, depois encerra a defesa.
5. **Given** a rodada 30 com a vez na mão, **When** um bot decide, **Then** ele desiste.
6. **Given** a partida em andamento, **When** o teste derruba o socket de um dos bots, **Then** a sessão dele passa por reconectando, recebe `match_start`, volta a ao vivo e ele continua jogando até o fim.
7. **Given** o fim da partida, **When** os dois terminam, **Then** os dois desfechos são iguais, cada bot teve no máximo 20 recusas e nenhuma `unknown_message_type`, os dois sockets de partida foram fechados de propósito, e o log tem uma linha de narrador por evento, do mulligan ao desfecho.
8. **Given** uma rodada do teste, **When** ela termina, **Then** os frames recebidos por um dos bots ficam gravados como fixtures para os testes EditMode.

---

### Edge Cases

**Formas**
- Campo obrigatório faltando ou de tipo errado num `match_start`/`match_update`: o frame inteiro é registrado como inválido e descartado, sem aplicar metade; a conexão continua.
- Campo a mais em qualquer frame, visão, evento ou modificador: ignorado.
- `turn` e `mulligan_remaining_ms` presentes ao mesmo tempo, ou os dois nulos: lidos como vieram; o relógio só expõe o que existe.
- Frame sem `clock` (formato anterior à feature 010): lido com vez e prazo de mulligan nulos.
- `cast_spell` sem alvo: `target_card_instance_id` omitido ou `null`, os dois aceitos pelo contrato (ver Assumptions para o escolhido).
- `declare_attack` com lista vazia: enviado; quem recusa é o servidor (`no_attackers_selected`).
- Frame de tipo que a sessão de partida não trata (ex.: `pong` já consumido pela conexão, ou tipo novo): registrado no log, ignorado.

**Espelho e versão**
- `match_update` antes de qualquer `match_start` (não previsto pelo contrato): aplicado pela mesma regra de versão, já que o espelho vazio não tem versão aplicada.
- `match_start` com versão menor que a aplicada, depois de reconexão: descartado inteiro, e o relógio não muda.
- `turn_warning` cria uma versão que nenhum `match_update` carrega: o espelho não conta com versões contínuas.
- Partida terminada e chega outro `match_update` com versão maior (não previsto): aplicado pela regra de versão; "partida terminou" não sai de novo.
- Assinante que lança exceção dentro de um aviso: registrado no log; os avisos seguintes do mesmo frame ainda saem e o espelho não fica meio aplicado.
- `you.profile.user_id` diferente entre dois frames aceitos (não previsto): registrado no log como erro; a identidade é a do frame mais recente.

**Relógio**
- Vez nova com o mesmo dono na rodada seguinte: `turn_number` muda, e sai "vez nova".
- Frame aceito que já traz `warning` verdadeiro para a vez atual (ex.: o oponente, que não recebe `turn_warning`, ou quem reconectou depois do aviso): "tempo acabando" sai se ainda não saiu para essa vez (ver Assumptions).
- `turn_warning` que chega antes do `match_update` da vez que ele cita (vez que o cliente ainda não desenha): ignorado.
- Relógio do aparelho que salta por pausa (app minimizado, cena carregando): o restante continua `remaining_ms` menos o tempo monotônico decorrido, preso em zero.
- Relógio em zero por muito tempo sem frame: continua em zero; nada é enviado, nada é inferido.

**Comandos e recusas**
- Comando chamado entre a queda do socket e o aviso de estado da conexão: o envio devolve "não enviado" pelo resultado da própria conexão, sem exceção.
- Recusa chegando depois de uma atualização aceita que já limpou o pendente: sai com o último comando enviado ainda como provável, por melhor esforço.
- Duas recusas seguidas para dois comandos rápidos: cada uma associada ao último enviado no momento em que chega; a imprecisão fica documentada.
- Recusa `match_is_over`, `match_not_found` ou `concurrent_match_write`: entregue como qualquer outra; a sessão não muda de estado por recusa.
- Jogada que perde a disputa para o estouro da vez: recebe a recusa comum (`not_your_priority`, `mulligan_already_taken`); nenhum tratamento especial.

**Sessão**
- `match_start` de reconexão com a mesma versão: a sessão volta a ao vivo e tira a marca de desatualizado, mesmo sem troca de estado.
- Queda depois da fase terminada, antes do fechamento de propósito: nada reabre.
- Sessão encerrada por quem consome (saída de cena) antes do fim: fecha o socket de propósito; nenhum aviso sai depois.
- `forfeit` enviado: a sessão continua ao vivo até o `match_update` terminado chegar; o cliente não declara a derrota sozinho.
- Carta ou apelido que o narrador não encontra: a linha sai com o identificador tipado no lugar do nome.

**Marco jogável**
- Bot sem jogada candidata (todas já recusadas na versão atual): o teste falha com a fase e a versão, como o `smoke_match.py`.
- Partida que passa do limite de tempo do teste: falha com a última rodada e fase de cada bot.
- Queda provocada durante o mulligan ou durante a Declaração: o bot volta e continua pela visão reenviada (`mulligan_taken` diz se já respondeu).

## Requirements *(mandatory)*

### Functional Requirements

**Formas do protocolo**

- **FR-001**: Os frames `match_start`, `match_update`, `turn_warning` e `message_refused` MUST ser decodificados pelo codec do projeto, registrados na união discriminada por `type`, em tipos fechados do projeto, com as formas dos contratos 009 e 010.
- **FR-002**: A visão do jogador MUST trazer: identificador da partida; número da rodada; fase; `UserId` da prioridade e do dono do token, nulos no mulligan; token consumido; passes consecutivos; combate nulo ou com atacantes e pares bloqueador/atacante; desfecho nulo ou com `UserId` derrotado e motivo; o próprio lado (perfil, Nexus, energia atual, mão, banco, cemitério, tamanho do deck, `mulligan_taken`); e o lado do oponente com tamanho da mão no lugar da mão.
- **FR-003**: A fase MUST ser um conjunto fechado — mulligan, upkeep, ação, declaração, combate, fim de rodada, terminada — mais um valor desconhecido que preserva o texto recebido. O motivo do desfecho MUST ser fechado — Nexus zerado, desistência — mais desconhecido.
- **FR-004**: Carta em partida MUST trazer `CardInstanceId` e `CardId`. Unidade no banco MUST trazer a carta, o dano sofrido e a lista de modificadores como família fechada por `modifier_kind`: ataque e vida com quantidade, imunidade a dano sem quantidade, cada um com duração (permanente, até o fim da rodada, desconhecida) e um modificador desconhecido que preserva o `modifier_kind`.
- **FR-005**: O relógio do frame MUST trazer a vez — nula, ou com `turn_number`, `UserId` do dono, restante em milissegundos e `warning` — e o prazo do próprio mulligan, nulo ou em milissegundos. Frame sem relógio MUST ser lido com os dois nulos.
- **FR-006**: Os eventos MUST ser uma família fechada por `kind` com os 17 do contrato 009 (`mulligan_taken`, `unit_played`, `spell_cast`, `passed`, `attackers_sent`, `attacker_withdrawn`, `attack_confirmed`, `blocker_assigned`, `blocker_removed`, `defense_ended`, `forfeited`, `unit_damaged`, `unit_died`, `nexus_changed`, `round_started`, `cards_drawn`, `match_finished`), os 2 do contrato 010 (`turn_timed_out`, `mulligan_timed_out`), e um evento desconhecido que preserva o `kind`. Cada um MUST ter os campos do contrato tipados; `cards_drawn` do oponente MUST aceitar lista de cartas vazia.
- **FR-007**: Os códigos de recusa MUST ser um conjunto fechado com os 5 de forma/transporte e os 26 de motor do contrato 009, mais um código desconhecido que preserva o texto. O texto de `error` MUST ficar disponível só para log e MUST NOT ser comparado em lugar nenhum.
- **FR-008**: Os 11 comandos MUST ser mensagens de saída que produzem o envelope e o `payload` exatos do contrato 009, e MUST receber só `CardInstanceId` como identificador de carta; passar `CardId` MUST ser erro de compilação.
- **FR-009**: Frame de partida que não decodifica MUST ser registrado e descartado inteiro, sem aplicar parte dele.

**Espelho da partida**

- **FR-010**: O espelho MUST aceitar `match_start` e `match_update` só com versão maior que a última aplicada, inclusive frente à do `match_start`, sem supor versões contínuas; frame com versão menor ou igual MUST ser descartado inteiro, sem aviso.
- **FR-011**: Cada frame aceito MUST substituir o estado inteiro, sem mesclar com o anterior.
- **FR-012**: A identidade própria MUST vir de `you.profile.user_id` do frame aceito.
- **FR-013**: O espelho MUST expor o estado atual somente leitura, a versão aplicada, e os fatos: é minha prioridade, sou dono do token, fase atual, meu mulligan pendente, mulligan do oponente já respondido, partida terminada, eu venci (nulo enquanto não terminou).
- **FR-014**: Para cada frame aceito, o espelho MUST emitir, na thread principal e nesta ordem: estado substituído (anterior, atual); cada evento da lista, na ordem recebida; fase mudou (anterior, atual), se mudou; prioridade mudou (anterior, atual), se mudou; partida terminou (desfecho, venci), só na primeira vez que a fase terminada é aceita.
- **FR-015**: Exceção de um assinante MUST ser registrada no log e MUST NOT impedir os avisos seguintes nem deixar o espelho parcialmente aplicado.
- **FR-016**: O espelho MUST oferecer os atributos exibidos de uma unidade — ataque e vida base do catálogo, somando os modificadores de ataque e vida e descontando o dano sofrido na vida —, documentados como conveniência de exibição; nenhum componente da camada MUST usá-los para decidir, habilitar ou bloquear nada.

**Relógio da vez**

- **FR-017**: O relógio MUST ser ancorado no instante monotônico em que chegou o frame que trouxe a vez; o restante agora MUST ser `remaining_ms` menos o tempo monotônico decorrido desde a âncora, nunca negativo. Hora absoluta MUST NOT ser usada.
- **FR-018**: A vez MUST ser identificada por `turn_number`. Frame aceito com `turn_number` diferente do desenhado MUST emitir "vez nova" (número, dono, restante); com o mesmo `turn_number`, MUST só reancorar.
- **FR-019**: `turn_warning` com o `turn_number` da vez desenhada MUST emitir "tempo acabando" e reancorar; com outro `turn_number` MUST ser ignorado. "Tempo acabando" MUST sair no máximo uma vez por `turn_number`, venha do `turn_warning` ou do `warning` de um frame aceito.
- **FR-020**: O relógio de mulligan MUST existir só enquanto o frame aceito traz prazo de mulligan, ancorado da mesma forma, e desaparecer quando o prazo vier nulo.
- **FR-021**: Frame descartado pela regra de versão MUST NOT mexer no relógio. `match_start` com versão igual à aplicada MUST NOT trocar o estado, mas MUST reancorar a vez e o mulligan a partir do instante em que chegou.
- **FR-022**: Restante zero MUST NOT disparar comando, mudança de estado nem inferência local; o estouro chega do servidor por `match_update` com `turn_timed_out` ou `mulligan_timed_out`.

**Comandos e recusas**

- **FR-023**: Os comandos MUST ser oferecidos como operações: mulligan (lista de cópias), jogar unidade, lançar feitiço com alvo, lançar feitiço sem alvo, passar, declarar ataque (lista), puxar atacante de volta, confirmar ataque, atribuir bloqueador, remover bloqueador, encerrar defesa e desistir.
- **FR-024**: Comando com a conexão fora de conectado MUST NOT ser enviado nem enfileirado, e MUST devolver um resultado explícito de "não enviado" com o estado da conexão; comando enviado MUST devolver "enviado".
- **FR-025**: Comando enviado MUST ficar marcado como pendente até a próxima atualização aceita, a próxima recusa ou o aviso de que a conexão voltou depois de cair. O pendente MUST NOT bloquear um novo envio; um novo envio MUST substituir o pendente.
- **FR-026**: `message_refused` MUST virar uma recusa tipada com o código (FR-007), o `error` para log e o último comando enviado como "provável comando recusado", ou nenhum se nada foi enviado desde a última atualização aceita. A associação MUST ser documentada como melhor esforço, porque o servidor não ecoa a mensagem recusada.
- **FR-027**: Recusa MUST NOT mudar o estado espelhado, o relógio nem o estado da sessão.

**Dicas de mira**

- **FR-028**: Para uma cópia da mão própria, a dica MUST informar, a partir do catálogo e do espelho: unidade ou feitiço; o custo e a energia atual lado a lado; para feitiço, se pede alvo e de que lado (nenhum, aliado, inimigo, desconhecido), a lista de `CardInstanceId` do banco daquele lado na ordem do banco, e se é só na declaração, junto da fase atual.
- **FR-029**: Cópia cujo `CardId` não está no catálogo MUST virar dica de carta desconhecida com o `CardId`, registrada no log, sem exceção.
- **FR-030**: Nenhuma dica MUST esconder, desabilitar ou impedir o envio de um comando; os comandos MUST NOT consultar as dicas.

**Sessão de partida**

- **FR-031**: A sessão MUST iniciar por `MatchId` abrindo a conexão autenticada da 003 no alvo de partida, e ser a única dona de espelho, relógio, comandos e narrador daquela partida.
- **FR-032**: A sessão MUST expor o estado e avisar cada mudança: conectando (antes do primeiro `match_start`); ao vivo; reconectando (estado espelhado mantido e marcado como desatualizado); terminada (com desfecho); recusada (com o motivo de partida recusada da 003); desistiu (com os demais motivos de desistência da conexão).
- **FR-033**: Queda da conexão com a sessão ao vivo MUST levar a reconectando; o `match_start` da reconexão MUST passar pelo espelho (FR-010, FR-021) e levar de volta a ao vivo, tirando a marca de desatualizado, com ou sem troca de estado.
- **FR-034**: Frame aceito em fase terminada MUST levar a terminada, publicar o desfecho e fechar o socket de propósito, sem reconexão e sem aviso de queda, porque o servidor não fecha o socket de partida.
- **FR-035**: Encerrar a sessão por quem consome MUST fechar o socket de propósito; nenhum aviso MUST sair depois.
- **FR-036**: A sessão MUST ter um narrador que registra pelo `IClientLog`, com campos estruturados, uma linha por evento aceito: rodada, `kind` e os campos do evento, com o nome da carta do catálogo no lugar do `CardId` e o apelido do jogador no lugar do `UserId`; sem nome conhecido, o identificador tipado.
- **FR-037**: A sessão MUST registrar no log cada recusa (código, `error`, provável comando) e cada mudança de estado da sessão.

**Marco jogável**

- **FR-038**: MUST existir um teste `[Explicit]` `[Category("LiveServer")]` com dois clientes headless no mesmo processo, só com adaptadores reais e as features 002 a 004, que cria contas novas, carrega o catálogo, cria o deck com feitiços do `smoke_match.py`, entra na fila, recebe `match_found` e abre as duas sessões de partida.
- **FR-039**: Os bots do teste MUST seguir a estratégia do `smoke_match.py` (User Story 7, cenários 2 a 5), usando só espelho, dicas e comandos desta feature, e sem repetir na mesma versão uma jogada já tentada.
- **FR-040**: No meio da partida, o teste MUST derrubar o socket de um dos bots e verificar que a sessão dele reconecta, recebe `match_start` e continua jogando.
- **FR-041**: O teste MUST verificar desfechos iguais, no máximo 20 recusas por bot, nenhuma `unknown_message_type`, sockets fechados de propósito e o narrador cobrindo a partida do mulligan ao desfecho; e MUST gravar os frames recebidos por um bot como fixtures dos testes EditMode.

**Código existente, evoluindo no lugar**

- **FR-042**: `MatchClient` MUST passar a ser, ou delegar para, a sessão de partida; a ponte `RawTextReceived`, o uso de `JsonConvert` fora do codec e o `MatchStateDTO` MUST ser removidos, sem referência restante, e os testes que cobriam a ponte MUST mudar junto.
- **FR-043**: `Core/Session/MatchSession.cs` MUST passar a expor o espelho novo ou delegar para ele; o plano MUST escolher nomes que não colidam com a sessão de partida nova.
- **FR-044**: O fluxo `VersusScene` → primeiro `match_start` → carregar `MatchScene` MUST continuar, e o `match_start` de reconexão MUST NOT recarregar a cena.
- **FR-045**: `PlayerSession.Instance` MAY continuar como raiz de composição das cenas até a feature 5; a sessão de partida e o núcleo MUST receber dependências na construção e MUST NOT ler singletons.
- **FR-046**: Nenhum tipo do núcleo MUST referenciar o motor, a biblioteca de JSON de terceiros ou a implementação concreta de socket, verificado pelo teste de fronteira existente.
- **FR-047**: Nenhum componente desta feature MUST decidir legalidade de jogada, calcular dano, avançar fase ou estourar a vez.

### Key Entities

- **Visão do jogador**: a partida pelos olhos de um jogador, numa versão; os dois lados, fase, prioridade, token, passes, rodada, combate, desfecho.
- **Lado próprio / lado do oponente**: perfil, Nexus, energia, banco, cemitério, tamanho do deck, `mulligan_taken`; mão inteira só no próprio, tamanho da mão no do oponente.
- **Carta em partida**: `CardInstanceId` (a cópia, citada nas jogadas) e `CardId` (a entrada do catálogo).
- **Unidade no banco**: carta, dano sofrido, modificadores.
- **Modificador**: ataque (quantidade), vida (quantidade), imunidade a dano; cada um com duração; desconhecido.
- **Combate**: atacantes e pares bloqueador/atacante.
- **Desfecho**: `UserId` derrotado e motivo.
- **Relógio do frame**: vez (número, dono, restante, aviso) e prazo do mulligan.
- **Evento de partida**: um dos 19 `kind`, ou desconhecido; lista ordenada, jogada primeiro, consequências depois.
- **Comando**: uma das 11 jogadas, com resultado enviado / não enviado.
- **Recusa**: código fechado ou desconhecido, `error` para log, provável comando recusado.
- **Espelho da partida**: versão aplicada, estado atual, identidade própria, fatos derivados, avisos.
- **Relógio da vez**: vez desenhada, âncora monotônica, aviso já emitido, relógio de mulligan.
- **Dica de mira**: tipo da carta, custo e energia, lado do alvo, candidatos, só na declaração.
- **Sessão de partida**: `MatchId`, estado (conectando, ao vivo, reconectando, terminada, recusada, desistiu), marca de desatualizado, espelho, relógio, comandos, narrador.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A suíte EditMode passa 100% pelo comando único da constituição, com o editor fechado, e o código novo não produz nenhum aviso de compilação.
- **SC-002**: 100% dos 19 tipos de evento, 3 tipos de modificador, 7 fases e 31 códigos de recusa dos contratos têm um teste de decodificação, e cada família tem um teste para o valor desconhecido.
- **SC-003**: Zero referências a `MatchStateDTO`, à ponte `RawTextReceived` e a `JsonConvert` fora do codec restam no projeto.
- **SC-004**: Zero tipos do núcleo referenciam o motor, a biblioteca de JSON de terceiros ou a implementação concreta de socket, verificado por teste automatizado.
- **SC-005**: Em 100% dos roteiros de versão (fora de ordem, duplicado, buraco, reconexão com versão igual e maior), o espelho nunca expõe um estado de versão menor que um já exposto, e frame descartado não produz nenhum aviso nem mexe no relógio.
- **SC-006**: Em 100% dos roteiros de relógio, o restante exposto nunca é negativo, nunca passa do último `remaining_ms` recebido, e "tempo acabando" sai no máximo uma vez por vez.
- **SC-007**: Zero comandos são enviados com a conexão fora de conectado, e os 11 comandos produzem o JSON exato do contrato.
- **SC-008**: Uma revisão do código da feature encontra zero decisões de legalidade, dano, fase ou estouro de vez; dicas e atributos exibidos não são lidos por nenhum comando.
- **SC-009**: O teste LiveServer do marco passa contra o backend local subido com `docker compose up`: os dois bots jogam do mulligan ao desfecho em menos de 10 minutos, com uma queda provocada no meio, desfechos iguais, no máximo 20 recusas por bot, e o log com uma linha de narrador por evento recebido.
- **SC-010**: Com dois jogadores do Multiplayer Play Mode, Jogar → pareamento → `VersusScene` → `MatchScene` continua completando em 100% das tentativas, e derrubar o socket de partida não recarrega a cena.
- **SC-011**: Os testes EditMode desta feature, somados, executam em menos de 5 segundos (fora a inicialização do editor), sem espera real de tempo.

## Assumptions

- **"desistiu" nos estados da sessão** foi lido como a conexão ter desistido por um motivo que não é partida recusada (sessão expirada, token recusado repetidamente, sem sessão, tentativas esgotadas), ao lado de "recusada" para `match_denied`/44xx. A desistência do jogador na partida (`forfeit`) termina pelo servidor e cai em "terminada" com o motivo desistência. Se a descrição queria outro sentido, este é o ponto a corrigir antes do plano.
- **`cast_spell` sem alvo omite o campo**, como o exemplo do contrato 010; o contrato aceita os dois, e o teste de JSON fixa o escolhido.
- **"Tempo acabando" também pelo `warning` do frame**: o `turn_warning` só vai aos sockets do dono da vez, e quem reconecta depois do aviso não o recebe de novo; ler o `warning` de um frame aceito dá o mesmo aviso para o oponente e para quem voltou, com a regra de no máximo uma vez por vez (FR-019).
- **`turn_warning` da vez desenhada reancora o relógio**: traz um `remaining_ms` medido pelo servidor mais recente que o do último frame, e não muda a partida nem a versão.
- **Identidade de "eu venci"**: venci quando o `UserId` derrotado é diferente da identidade própria; não existe empate (§10), e um desfecho com motivo desconhecido segue a mesma regra.
- **Atributos exibidos**: vida exibida = vida base + modificadores de vida − dano sofrido; ataque exibido = ataque base + modificadores de ataque; imunidade não altera número. Pode ficar negativa ou zero na exibição sem que o cliente conclua nada.
- **Pendente limpo por qualquer atualização aceita**, inclusive a causada pelo oponente: a camada não sabe qual atualização respondeu ao comando, e o pendente só serve para evitar clique duplo.
- **Estratégia dos bots**: segue `scripts/smoke_match.py` (limite de 20 recusas e rodada 30 são as constantes `REFUSAL_LIMIT` e `ROUND_CAP` de lá; a poção que só é lançada com Nexus abaixo de 12 também vem de lá). A queda provocada é feita pelo adaptador real de socket, sem fake.
- **Tempo limite do marco**: 10 minutos, o `MATCH_TIMEOUT_S` do `smoke_match.py`.
- **Frames gravados como fixture** passam a morar nos testes EditMode; tokens e senhas não aparecem neles.
- **Nomes**: o plano escolhe o nome da sessão de partida nova e o destino de `MatchSession` sem colisão (FR-043); esta spec usa "sessão de partida" para a nova e `MatchSession` para a classe existente.
- **`MatchSession` só é usado hoje pelo `MatchClient`** (conferido no código); a `MatchScene` não desenha nada desta feature.
- **Limitações do backend registradas na 003 (R11)** — `match_found` perdido e saída tardia do socket antigo — continuam sem contorno no cliente.
- **Dependências**: codec, portas e fakes da 001; `UserId`, `CardId`, catálogo e decks da 002; conexão autenticada, `MatchId`, `CardInstanceId` e fila da 003; backend local em `C:/Users/gabri/Projetos/dev_container/anathema/backend` com `docker compose up` para o LiveServer.
- **Fora do escopo**: fachada única para a apresentação, histórico depois da partida e prova final completa (feature 5); qualquer desenho na `MatchScene`, arrastar cartas, animação; validar jogada, calcular dano, avançar fase ou estourar a vez no cliente; contorno para as limitações do backend da 003.
