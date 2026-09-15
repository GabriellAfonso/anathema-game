# Feature Specification: Fachada da apresentação e prova final

**Feature Branch**: `005-presentation-facade` (nenhum branch criado; spec no `main`)

**Created**: 2026-09-14

**Status**: Draft

**Input**: User description: "Fachada da apresentação e prova final: a superfície única que a parte visual vai consumir, a composição sem singletons, e dois clientes headless jogando partidas inteiras contra o servidor usando só essa superfície. Quinta e última feature da camada de rede." (descrição completa na mensagem que abriu esta feature)

## Contexto

Quinta e última feature da camada de rede do cliente. Tudo o que a parte visual
vai precisar já existe, mas espalhado:

- a composição mora no `Awake` do singleton `PlayerSession.Instance`, e as cenas
  leem esse singleton;
- `MatchmakingClient` escreve em `VersusContext.Instance` e carrega a
  `VersusScene`; `MatchClient` escreve em `MatchSession.Instance` e carrega a
  `MatchScene`; `LoginController` carrega a `HomeScene`;
- `SelfProfileService.Instance` copia o perfil para campos soltos da
  `PlayerSession`;
- `ReconnectOverlay` assina eventos de `BaseClient`;
- sessão expirada, recusa de fila, saída da fila e catálogo indisponível ao
  abrir a partida só vão para o log (`Game/TODO.md`, itens "Caminho: feature 5").

Esta feature junta as peças das features 001–004 — `LiveNetworkAdapters`,
`LiveAccountServices`, `LiveConnectionServices`, `AccountSession`,
`CardCatalog`, `PlayerDecks`, `MatchHistory`, `MatchQueue`, `LiveMatch`
(`Mirror`, `Clock`, `Commands`, `Pending`, `HintFor`, `StatsOf`, `Refused`) e
`MatchNarrator` — atrás de uma superfície única, com um estado do app que diz em
que ponto o jogador está, sem cena. As cenas passam a só assinar e chamar essa
superfície, e dois clientes headless provam que ela basta para jogar partidas
inteiras.

Os "usuários" desta feature são o jogador — que entra, procura partida, joga,
cai, volta e vê o resultado — e quem vai escrever a parte visual, que precisa de
um lugar só para saber o que pode assinar e chamar.

**Contratos e regras** — são do backend e do vault, e não são copiados aqui.
Fonte da verdade, em `C:/Users/gabri/Projetos/dev_container/anathema/backend/`:

- `specs/009-match-protocol/contracts/` — socket de partida e códigos de recusa
- `specs/010-match-timers/contracts/` — relógio da vez, `turn_warning`, `turn_timed_out`
- `specs/011-deck-catalog-api/contracts/` — catálogo, decks e matchmaking
- `specs/012-match-result-history/contracts/http_match_history.md` — histórico
- `scripts/smoke_match.py` — referência executável do roteiro da prova

E, no vault, `C:/Users/gabri/Obsidian/Projetos/Anathema/Game/TODO.md` (dívidas
"Caminho: feature 5") e `Game/Fluxo de Partida.md`.

Quando esta spec e um contrato discordam, vale o contrato.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Saber em que ponto o jogador está, sem cena (Priority: P1)

A apresentação pergunta à fachada um único estado do app — deslogado, logado,
procurando, pareado, em partida, partida terminada, partida indisponível — e é
avisada de cada transição na thread principal, já com o que a tela seguinte
precisa: o pareamento ao entrar em pareado, a partida corrente ao entrar em
partida, o desfecho ao terminar, o motivo ao voltar para deslogado. Toda
assinatura devolve algo descartável; descartada, não recebe mais nada.

**Why this priority**: é o eixo de tudo o mais. O roteador de cena, as telas e a
prova final dirigem-se por esse estado; sem ele, cada cena volta a decidir sozinha
para onde ir.

**Independent Test**: EditMode compõe a fachada sobre os fakes nomeados das
features 001–004 (`FakeHttpTransport`, `FakeWebSocket`, `FakeMonotonicClock`,
guarda falsa) e roteiriza login, fila, pareamento, `match_start`, fim, volta,
verificando cada transição, a ordem dos avisos e o que cada um carrega.

**Acceptance Scenarios**:

1. **Given** a fachada recém-composta sem sessão guardada, **When** se consulta o estado, **Then** é deslogado, sem motivo.
2. **Given** deslogado, **When** o login ou a retomada sem senha dá certo, **Then** o estado passa a logado e o aviso traz o `UserId` próprio.
3. **Given** logado, **When** a apresentação entra na fila com um `DeckId`, **Then** o estado passa a procurando; **When** chega `match_found`, **Then** passa a pareado e o aviso traz o `MatchId` e os dois jogadores (perfil próprio e do oponente).
4. **Given** pareado, **When** chega o primeiro `match_start` da partida, **Then** o estado passa a em partida e o aviso traz a partida corrente já com o estado espelhado.
5. **Given** em partida, **When** o socket cai e volta com outro `match_start`, **Then** o estado do app continua em partida, nenhum aviso de transição sai, e a partida corrente é a mesma.
6. **Given** em partida, **When** chega o frame em fase terminada, **Then** o estado passa a partida terminada e o aviso traz o desfecho do espelho (derrotado, motivo, venci).
7. **Given** partida terminada, **When** a apresentação pede para voltar, **Then** o estado passa a logado e a partida corrente passa a nenhuma.
8. **Given** uma assinatura de transição descartada, **When** ocorre qualquer transição, **Then** o assinante não recebe nada, inclusive um aviso que já estava a caminho da thread principal quando foi descartada; descartar de novo não faz nada.

---

### User Story 2 - Conta pela fachada, e sessão expirada leva ao login (Priority: P1)

A apresentação cadastra, entra, sai, retoma sem senha e lê o perfil próprio e o
`UserId` próprio só pela fachada. Quando o servidor recusa a renovação da sessão,
em qualquer estado do app, a fachada fecha o que estiver aberto (fila, partida)
de propósito e leva a deslogado com o motivo "sessão expirada".

**Why this priority**: é a porta de entrada de toda tela, e a dívida da 002
("`SessionExpired` sem reação visual") só se resolve com um estado que as cenas
sigam.

**Independent Test**: EditMode com `FakeHttpTransport` e guarda falsa,
roteirizando cadastro, login, retomada, perfil, saída e refresh recusado a partir
de cada estado do app.

**Acceptance Scenarios**:

1. **Given** deslogado, **When** a apresentação cadastra com um formulário válido, **Then** recebe o resultado do cadastro com as recusas de campo tipadas quando houver, e o estado continua deslogado até entrar.
2. **Given** deslogado, **When** entra com credenciais recusadas, **Then** recebe o resultado tipado de recusa e o estado continua deslogado.
3. **Given** logado, **When** pede o perfil próprio, **Then** recebe apelido, ícone, nível, experiência, moedas e créditos, ou a recusa/falha tipada.
4. **Given** cada um dos estados logado, procurando, pareado, em partida, partida terminada e partida indisponível, **When** a renovação da sessão é recusada, **Then** o estado passa a deslogado com motivo "sessão expirada", a fila é deixada, o socket de partida é fechado de propósito, e a partida corrente passa a nenhuma.
5. **Given** logado, **When** a apresentação sai, **Then** o estado passa a deslogado com motivo "saiu", sem aviso de sessão expirada.
6. **Given** uma fachada descartada depois de logar, **When** outra fachada é composta sobre a mesma guarda e retoma sem senha, **Then** o estado passa a logado com o mesmo `UserId`.

---

### User Story 3 - Catálogo, decks e histórico pela fachada (Priority: P2)

A apresentação carrega o catálogo uma vez por sessão e consulta cartas por
`CardId`; lista, lê, cria, altera e apaga decks com as recusas tipadas; e lê o
histórico paginado. Nada disso exige outro tipo além dos que a superfície expõe.

**Why this priority**: nenhuma tela desses dados existe ainda, mas o botão Jogar
lista decks, a partida precisa do catálogo, e a prova final usa os três.

**Independent Test**: EditMode com `FakeHttpTransport` verificando que o
catálogo é pedido ao servidor uma única vez por sessão, a consulta por `CardId`,
e que decks e histórico devolvem os resultados e recusas da 002.

**Acceptance Scenarios**:

1. **Given** logado, **When** a apresentação consulta o catálogo duas vezes, **Then** o servidor recebe um só pedido e as duas consultas veem as mesmas cartas.
2. **Given** o catálogo carregado, **When** consulta um `CardId` de unidade, de feitiço e um inexistente, **Then** recebe unidade, feitiço e "carta não encontrada", sem exceção.
3. **Given** logado, **When** cria um deck de 12 cartas, **Then** recebe a recusa tipada com o problema `wrong_deck_size`.
4. **Given** uma sessão nova depois de sair e entrar com outra conta, **When** consulta o catálogo, **Then** ele é pedido de novo para a sessão nova.

---

### User Story 4 - Fila pela fachada, com recusas e saída visíveis (Priority: P1)

A apresentação entra na fila com um `DeckId`, sai, lê a fase, e é avisada de
pareamento (com os dois jogadores), de recusa tipada e de saída da fila com o
motivo. Recusa e saída levam o estado do app de volta a logado.

**Why this priority**: sem isso o botão Jogar continua sem resposta visível para
recusa ou saída (dívida da 003), e a prova não verifica `deck_not_found`.

**Independent Test**: EditMode com `FakeWebSocket` roteirizando `join_queue`,
`match_found`, recusa e desistência da conexão de fila.

**Acceptance Scenarios**:

1. **Given** logado, **When** entra na fila com um `DeckId` que o servidor não conhece, **Then** sai uma recusa tipada `deck_not_found` e o estado volta a logado.
2. **Given** procurando, **When** a apresentação sai da fila, **Then** o estado volta a logado, sem aviso de recusa.
3. **Given** procurando, **When** a conexão de fila desiste, **Then** sai o aviso "saiu da fila" com o motivo e o estado volta a logado.
4. **Given** um estado diferente de logado, **When** a apresentação tenta entrar na fila, **Then** recebe um resultado explícito de "não entrou" com o estado atual, e nada é enviado.

---

### User Story 5 - Partida corrente, erro explícito e resultado com histórico (Priority: P1)

Em partida, a fachada entrega a partida corrente — estado, espelho, relógio,
comandos, pendente, dicas, atributos exibidos, recusas — ou nenhuma. Se o
catálogo não carrega ao abrir a partida, ou se a partida é recusada ou a conexão
de partida desiste, o estado vira partida indisponível com o motivo, e a
apresentação pode tentar abrir de novo ou voltar. Quando a partida termina, o
estado de partida terminada traz, além do desfecho do espelho, a linha do
histórico daquela partida, buscada depois do fim.

**Why this priority**: é o que a `MatchScene` vai desenhar, e resolve a dívida
da 004 ("catálogo indisponível só vai para o log").

**Independent Test**: EditMode com `FakeHttpTransport`, `FakeWebSocket` e
`FakeMonotonicClock` roteirizando catálogo recusado, `match_denied`, fim de
partida e respostas de histórico sem a linha, com a linha na terceira leitura, e
nunca com a linha.

**Acceptance Scenarios**:

1. **Given** pareado, **When** o catálogo falha ao abrir a partida, **Then** o estado passa a partida indisponível com motivo "catálogo indisponível" e o `MatchId`, e nenhum socket de partida é aberto.
2. **Given** partida indisponível por catálogo, **When** a apresentação tenta abrir de novo e o catálogo carrega, **Then** a partida é aberta e o estado segue para em partida no primeiro `match_start`.
3. **Given** pareado, **When** o servidor recusa a partida, **Then** o estado passa a partida indisponível com o motivo de partida recusada da 003.
4. **Given** em partida, **When** a apresentação pede a partida corrente, **Then** recebe a mesma sessão cujos espelho, relógio, comandos, pendente, dicas, atributos exibidos e recusas estão ativos; **Given** logado, **Then** recebe nenhuma.
5. **Given** o fim da partida, **When** o estado passa a partida terminada, **Then** a linha do histórico está marcada como "buscando"; **When** uma leitura do histórico traz a linha com o `MatchId` da partida, **Then** sai o aviso de linha resolvida com vitória, motivo, oponente, duração e rodada final.
6. **Given** o fim da partida e um histórico que não traz a linha, **When** as tentativas se esgotam, **Then** o estado continua partida terminada com o desfecho do espelho e a linha marcada como indisponível, sem exceção nem nova leitura.
7. **Given** partida terminada com a linha ainda buscando, **When** a apresentação volta para logado, **Then** as tentativas restantes são canceladas e nenhum aviso de linha sai depois.

---

### User Story 6 - Saúde da conexão para o overlay (Priority: P2)

O overlay de reconexão assina, pela fachada, a saúde da conexão que sustenta o
estado atual (fila em procurando; partida em pareado, em partida e partida
terminada): reconectando (tentativa, espera), voltou, desistiu (motivo em texto
para o jogador) e latência medida.

**Why this priority**: o overlay já existe e hoje depende de `BaseClient`; sem
ele, a queda parece travamento. Não bloqueia jogar.

**Independent Test**: EditMode com `FakeWebSocket` e `FakeMonotonicClock`
derrubando a conexão de fila e a de partida e verificando os avisos de saúde.

**Acceptance Scenarios**:

1. **Given** em partida, **When** o socket de partida cai, **Then** sai "reconectando" com tentativa e espera; **When** volta, **Then** sai "voltou".
2. **Given** procurando, **When** a conexão de fila fica sem rede, **Then** sai "reconectando" com a tentativa da queda que causou a suspensão.
3. **Given** em partida, **When** a conexão desiste por tentativas esgotadas, **Then** sai "desistiu" com o texto do motivo para o jogador.
4. **Given** uma latência medida pelo heartbeat, **When** a apresentação consulta ou assina, **Then** recebe a última latência da conexão ativa.
5. **Given** logado, sem conexão ativa, **When** a conexão de partida antiga emite qualquer coisa, **Then** nenhum aviso de saúde sai.

---

### User Story 7 - Composição sem singletons e cenas guiadas pelo estado (Priority: P1)

Um único hospedeiro Unity monta adaptadores e fachada e a entrega às cenas por
um ponto de acesso da borda. Um roteador de cena, também na borda, assina o
estado do app e carrega `LoginScene`, `HomeScene`, `VersusScene` e `MatchScene`,
sem recarregar a cena atual. Login, botão Jogar, Versus, mini perfil e overlay
passam a usar só a fachada. Os clientes de socket, contextos e sessões antigos
somem ou viram parte da fachada/roteador.

**Why this priority**: é o critério de pronto "nenhum `static Instance` em
código de rede, sessão ou clientes de socket; `SceneManager` só no roteador".

**Independent Test**: Multiplayer Play Mode com dois jogadores (quickstart) e
busca no código por `static ... Instance` e `SceneManager`.

**Acceptance Scenarios**:

1. **Given** a `BootstrapScene` no Play Mode, **When** o hospedeiro sobe, **Then** existe uma única fachada composta, e as cenas a obtêm pelo ponto de acesso da borda.
2. **Given** deslogado, **When** o login dá certo, **Then** o roteador carrega a `HomeScene` e o mini perfil mostra apelido, nível, moedas e ícone lidos pela fachada.
3. **Given** a `HomeScene`, **When** o jogador aperta Jogar, **Then** a fila é chamada pela fachada com o primeiro deck, e o pareamento leva o roteador à `VersusScene`, que mostra os dois jogadores do aviso de pareado.
4. **Given** a `VersusScene`, **When** o estado passa a em partida, **Then** o roteador carrega a `MatchScene`; **When** o socket de partida cai e volta, **Then** a `MatchScene` não é recarregada e o overlay aparece e some.
5. **Given** qualquer cena, **When** a sessão expira, **Then** o roteador carrega a `LoginScene`.
6. **Given** uma cena que assinou a fachada, **When** ela é destruída por troca de cena, **Then** suas assinaturas são descartadas e nenhum aviso chega a um objeto destruído.

---

### User Story 8 - A regra de consumo escrita e verificada (Priority: P1)

Existe um contrato desta feature que lista tudo o que a apresentação pode
assinar e chamar, e nada além. Tipos que não fazem parte da superfície não são
alcançáveis pelo código de cena. `CLAUDE.md` aponta para o contrato como a porta
de entrada da parte visual.

**Why this priority**: é a promessa do projeto — ligar a parte visual sem
escrever regra, mensagem ou estado de partida — tornada verificável.

**Independent Test**: testes de fronteira de assembly, no padrão dos da
001–004, comparando os tipos públicos alcançáveis pela apresentação e os tipos
usados pela prova final com a lista do contrato.

**Acceptance Scenarios**:

1. **Given** a assembly da fachada, **When** o teste de fronteira a examina, **Then** ela não referencia o motor, a biblioteca de JSON de terceiros nem a implementação concreta de socket.
2. **Given** o contrato da superfície, **When** o teste compara os tipos públicos que a fachada expõe (assinaturas de membros, eventos e resultados) com a lista do contrato, **Then** não há tipo exposto fora da lista nem tipo da lista ausente.
3. **Given** a assembly da prova final, **When** o teste examina os tipos que ela usa das assemblies do projeto, **Then** todos estão na lista do contrato ou na composição.
4. **Given** o código de cena, **When** tenta usar diretamente um tipo interno da camada (conexão autenticada, fila, sessão de conta, transporte), **Then** a compilação falha.
5. **Given** o `CLAUDE.md`, **When** alguém procura por onde começar a parte visual, **Then** encontra uma linha apontando para o contrato da superfície.

---

### User Story 9 - Dois clientes headless jogam partidas inteiras usando só a fachada (Priority: P1)

Dois clientes no mesmo processo, cada um com sua fachada completa sobre
adaptadores reais e guardas de refresh em slots nomeados, fazem o roteiro
inteiro do jogador — conta, catálogo, decks, fila, duas partidas, queda, token
vencido, vez estourada, histórico — contra o backend local, usando só o que o
contrato da superfície e a composição expõem.

**Why this priority**: é a prova de pronto da camada de rede: se a prova precisa
de outro tipo, a superfície está incompleta.

**Independent Test**: teste `[Explicit]` `[Category("LiveServer")]` contra o
backend subido com `docker compose up`.

**Acceptance Scenarios**:

1. **Given** o backend local, **When** o teste roda, **Then** cadastra duas contas novas, entra com as duas, descarta as duas fachadas e compõe fachadas novas sobre os mesmos slots, que retomam sem senha.
2. **Given** as duas contas logadas, **When** consultam catálogo e decks, **Then** o catálogo tem 29 cartas, o deck inicial não tem nenhum feitiço, o deck com feitiços do `smoke_match.py` é criado, e um deck de 12 cartas é recusado com `wrong_deck_size`.
3. **Given** as duas contas, **When** uma entra na fila com um `DeckId` inexistente, **Then** recebe `deck_not_found` e volta a logado; **When** as duas entram com o deck com feitiços, **Then** ficam pareadas na mesma partida.
4. **Given** a partida 1, **When** os bots jogam com a estratégia do `smoke_match.py`, **Then** até o fim cada um destes comandos é enviado pelo menos uma vez por algum dos bots: mulligan trocando (P1) e sem trocar (P2), jogar unidade, feitiço com alvo, feitiço sem alvo, passar, declarar ataque, puxar atacante, confirmar ataque, atribuir bloqueador, remover bloqueador, encerrar defesa.
5. **Given** a partida 1 em andamento, **When** o teste derruba o socket de partida de um cliente, **Then** a saúde da conexão dele avisa reconectando e voltou, o estado do app continua em partida, e a partida corrente dele segue com o estado atual do servidor.
6. **Given** noutro momento da partida 1, **When** o teste faz o relógio monotônico de um cliente saltar além da validade do token de acesso e a conexão de partida dele reabre, **Then** o token é renovado antes da abertura, e a partida segue.
7. **Given** uma vez na Fase de Ação de um bot, **When** ele deixa a vez correr sem jogar, **Then** ele recebe `turn_warning`, e os dois veem `turn_timed_out` seguido de `passed`.
8. **Given** o fim da partida 1 (Nexus ou desistência na rodada 30), **When** os dois terminam, **Then** os dois veem o mesmo desfecho e o estado partida terminada com a linha do histórico daquela partida resolvida.
9. **Given** as duas contas de volta a logado, **When** entram de novo na fila e um bot desiste no mulligan, **Then** a partida 2 termina com motivo `forfeit`, e a linha do histórico dela traz rodada final 1.
10. **Given** as duas partidas terminadas, **When** cada conta lê o histórico, **Then** as duas partidas aparecem nas duas contas, com um vencedor e um perdedor em cada partida.

---

### Edge Cases

**Estado do app**
- Transição pedida fora de ordem (sair da fila em logado, voltar de partida terminada estando em partida): devolve resultado explícito "não se aplica" com o estado atual; nada muda, nada é enviado.
- `match_found` chega depois de a apresentação ter pedido para sair da fila: o estado segue o que a fila da 003 decide; se ela entrega o pareamento, o estado passa a pareado.
- Pareado e o primeiro `match_start` nunca chega (servidor encerra o mulligan por tempo e a partida termina sem este cliente conectar): o frame final chega ao conectar, e o estado vai direto a partida terminada.
- Sessão expira durante uma transição assíncrona (catálogo carregando, histórico buscando): o trabalho em curso é abandonado e nada que ele termine depois muda o estado.
- Sair (explicitamente) em partida: o socket é fechado de propósito; a partida continua no servidor e termina pelos relógios dele. A fachada não manda `forfeit` por conta própria.
- Descartar a fachada: fecha fila e partida de propósito, cancela buscas de histórico, e nenhum aviso sai depois.

**Assinaturas**
- Assinante que lança exceção: registrado no log; os demais assinantes e os avisos seguintes ainda recebem.
- Assinatura descartada dentro do próprio aviso: o assinante não recebe avisos seguintes, inclusive do mesmo frame.
- Assinar a mesma coisa duas vezes: duas assinaturas independentes, descartáveis separadamente.

**Partida e histórico**
- Linha do histórico chega com `won` diferente do "venci" do espelho: vale o que cada fonte diz no seu campo; a divergência é registrada no log como erro, e nada é corrigido.
- Linha do histórico com `opponent` nulo (perfil apagado): a linha é resolvida com oponente ausente.
- Histórico responde com falha de transporte ou recusa numa tentativa: conta como tentativa sem a linha.
- Nova partida pareada enquanto a linha da anterior ainda busca: impossível pelo estado (é preciso voltar a logado antes); se ocorrer, a busca anterior é cancelada.
- Catálogo indisponível e o servidor encerra o mulligan por tempo: tentar abrir de novo leva ao frame final e a partida terminada.

**Saúde da conexão**
- Troca de conexão ativa (fila para partida) com um aviso da fila ainda a caminho: descartado.
- Volta do segundo plano no Android: a reconexão é a da 003; a saúde avisa reconectando/voltou como numa queda.

**Prova final**
- Um comando da lista não foi coberto quando a partida 1 termina: o teste falha nomeando os comandos não cobertos.
- A estratégia fica sem jogada candidata: falha com fase e versão, como o `smoke_match.py`.
- A prova passa do tempo limite: falha com a rodada, a fase e o estado do app de cada cliente.
- O salto do relógio também faz a detecção de silêncio e o relógio da vez perceberem tempo passado: esperado; o que se verifica é a renovação antes da reabertura e a partida seguindo.

## Requirements *(mandatory)*

### Functional Requirements

**Fachada**

- **FR-001**: MUST existir uma fachada única, numa assembly própria sem referência ao motor, composta por construtor a partir das peças das features 001–004, sem ler singleton nem estado estático mutável.
- **FR-002**: A fachada MUST expor para a conta: estado da sessão, cadastrar, entrar, sair, retomar sem senha, perfil próprio, `UserId` próprio e o aviso de sessão expirada, com os resultados e recusas tipados da 002.
- **FR-003**: A fachada MUST expor o catálogo carregado no máximo uma vez por sessão de conta e a consulta de carta por `CardId`.
- **FR-004**: A fachada MUST expor os decks (listar, ler, criar, alterar, apagar) e o histórico paginado, com os resultados e recusas tipados da 002.
- **FR-005**: A fachada MUST expor a fila: entrar com `DeckId`, sair, fase, e avisos de pareado (com `MatchId` e os dois jogadores), recusa tipada e saída da fila com motivo.
- **FR-006**: A fachada MUST expor a partida corrente — a sessão de partida da 004 com estado, espelho, relógio, comandos, pendente, dicas, atributos exibidos e recusas — ou nenhuma.
- **FR-007**: A fachada MUST expor a saúde da conexão ativa: reconectando (tentativa, espera), voltou, desistiu (motivo com texto para o jogador) e latência. A conexão ativa é a de fila em procurando e a de partida em pareado, em partida, partida terminada e partida indisponível; nos demais estados não há conexão ativa e nenhum aviso de saúde sai.
- **FR-008**: Toda operação chamada num estado em que não se aplica MUST devolver um resultado explícito com o estado atual, sem enviar nada e sem exceção.

**Estado do app**

- **FR-009**: A fachada MUST expor o estado do app como conjunto fechado: deslogado, logado, procurando, pareado, em partida, partida terminada, partida indisponível.
- **FR-010**: As transições MUST ser: deslogado → logado (login ou retomada); logado → procurando (entrar na fila); procurando → logado (sair, recusa, saída da fila); procurando → pareado (pareamento); pareado → em partida (primeiro `match_start` aceito); pareado ou em partida → partida terminada (frame em fase terminada); pareado ou em partida → partida indisponível (catálogo indisponível, partida recusada, conexão de partida desistiu por motivo que não é sessão expirada); partida indisponível → pareado (tentar abrir de novo); partida terminada ou partida indisponível → logado (voltar); qualquer estado → deslogado (sair ou sessão expirada).
- **FR-011**: Cada transição MUST avisar, na thread principal, com o estado anterior, o novo e o que o novo precisa: `UserId` em logado; pareamento em pareado; partida corrente em em partida; desfecho e linha do histórico em partida terminada; `MatchId` e motivo em partida indisponível; motivo (saiu, sessão expirada) em deslogado.
- **FR-012**: Queda e reconexão do socket de partida MUST NOT mudar o estado do app nem trocar a partida corrente.
- **FR-013**: Sessão expirada em qualquer estado MUST deixar a fila, fechar o socket de partida de propósito, cancelar trabalho assíncrono em curso, esquecer a partida corrente e levar a deslogado com motivo "sessão expirada".
- **FR-014**: Catálogo indisponível ao abrir a partida MUST levar a partida indisponível com motivo "catálogo indisponível", sem abrir socket de partida, e registrar no log.

**Resultado da partida**

- **FR-015**: Ao entrar em partida terminada, o estado MUST trazer o desfecho do espelho (derrotado, motivo, venci) e a linha do histórico daquela partida marcada como "buscando".
- **FR-016**: A fachada MUST buscar a linha pelo `MatchId` na primeira página do histórico, com até 4 leituras, a primeira logo depois do fim e as seguintes espaçadas por esperas crescentes, com todas as leituras terminadas em até 8 segundos; o tempo MUST vir do relógio monotônico injetado.
- **FR-017**: Linha encontrada MUST ser publicada por aviso de linha resolvida (vitória, motivo, oponente ou ausente, duração, rodada final); sem a linha depois das tentativas, a linha MUST ficar marcada como indisponível, com aviso, e o estado MUST continuar partida terminada com o desfecho do espelho.
- **FR-018**: Voltar, sair, sessão expirada ou descarte da fachada MUST cancelar as leituras restantes; nenhum aviso de linha MUST sair depois.

**Regra de consumo**

- **FR-019**: MUST existir um contrato da feature que lista tudo o que a apresentação pode assinar e chamar — membros da fachada e os tipos que eles expõem — e declara que nada fora da lista faz parte da superfície.
- **FR-020**: Toda assinatura de aviso da fachada MUST devolver algo descartável; depois de descartada, MUST NOT entregar nada, inclusive avisos já enfileirados para a thread principal. Descartar mais de uma vez MUST NOT falhar.
- **FR-021**: Tipos que não fazem parte da superfície MUST ficar `internal` ou fora do alcance da assembly de cena, de modo que usá-los no código de cena seja erro de compilação.
- **FR-022**: MUST existir teste de fronteira de assembly que verifica: a fachada sem motor, sem biblioteca de JSON de terceiros e sem socket concreto; os tipos expostos pela fachada iguais à lista do contrato; os tipos das assemblies do projeto usados pela prova final contidos na lista do contrato ou na composição.
- **FR-023**: `CLAUDE.md` MUST ganhar uma linha apontando para o contrato da superfície como a porta de entrada da parte visual.
- **FR-024**: Todo aviso entregue à apresentação MUST chegar na thread principal, pela troca única de thread da borda.

**Composição e cenas**

- **FR-025**: MUST existir uma composição que monta adaptadores e fachada e aceita, de quem compõe: o slot da guarda de refresh, o relógio monotônico (para permitir um decorador com salto controlado), o endereço do servidor e o log. O hospedeiro Unity e a prova final MUST usar essa mesma composição.
- **FR-026**: Um único hospedeiro Unity MUST montar a composição e entregá-la às cenas por um ponto de acesso da borda. `PlayerSession` MUST deixar de ser singleton de composição.
- **FR-027**: Nenhum `static Instance` MUST restar em código de rede, de sessão ou de clientes de socket (inclui `PlayerSession`, `MatchSession`, `VersusContext` e `SelfProfileService`).
- **FR-028**: Um roteador de cena na borda MUST assinar o estado do app e carregar: `LoginScene` em deslogado; `HomeScene` em logado e procurando; `VersusScene` em pareado; `MatchScene` em em partida e partida terminada. Em partida indisponível MUST manter a cena atual. MUST NOT recarregar a cena que já está ativa.
- **FR-029**: `SceneManager` MUST ser usado só pelo roteador de cena (ferramentas de editor fora da contagem).
- **FR-030**: `BaseClient`, `MatchClient`, `MatchmakingClient`, `MatchSession`, `VersusContext` e `SelfProfileService` MUST evoluir para dentro da fachada/roteador ou ser removidos quando a responsabilidade já estiver lá; o plano MUST dizer o destino de cada um, e nenhum MUST continuar com responsabilidade duplicada.
- **FR-031**: `LoginController`, `MyButtonScript` (Jogar), `VersusController`, `MiniPlayerProfile` e `ReconnectOverlay` MUST usar só a fachada, e MUST descartar suas assinaturas quando destruídos. O login automático de desenvolvimento do Multiplayer Play Mode MUST continuar funcionando.
- **FR-032**: `Game/TODO.md` MUST ser atualizado: itens resolvidos por esta feature marcados como feitos na 005; itens que dependem de tela (`MatchScene` não desenha nada, botão Jogar com o primeiro deck, sem tela de erro de partida e de resultado) apontando para a parte visual.

**Prova final**

- **FR-033**: MUST existir um teste `[Explicit]` `[Category("LiveServer")]` com dois clientes headless no mesmo processo, cada um com sua composição completa sobre adaptadores reais, com guardas de refresh em slots nomeados, que usa só o contrato da superfície e a composição (FR-022).
- **FR-034**: A prova MUST cumprir o roteiro da User Story 9, cenários 1 a 10.
- **FR-035**: Os bots MUST seguir a estratégia do `smoke_match.py`, estendida só no necessário para cobrir remover bloqueador e feitiço sem alvo, sem decidir legalidade; recusas continuam a caminho normal.
- **FR-036**: O salto de relógio MUST ser feito por um decorador do relógio monotônico real, injetado pela composição, com o salto controlado pelo teste.
- **FR-037**: A prova MUST verificar a renovação do token antes da reabertura por meio do que a composição expõe (ver Assumptions).

**Limites**

- **FR-038**: Nenhum componente desta feature MUST decidir legalidade de jogada, calcular dano, avançar fase ou estourar a vez.
- **FR-039**: Nenhum contorno MUST ser escrito para as limitações do backend registradas no `Game/TODO.md`.

### Key Entities

- **Fachada**: a superfície única; conta, catálogo, decks, histórico, fila, partida corrente, saúde da conexão e estado do app.
- **Estado do app**: deslogado, logado, procurando, pareado, em partida, partida terminada, partida indisponível; com o que cada um carrega.
- **Transição do app**: estado anterior, novo, e a carga do novo.
- **Motivo de deslogado**: saiu, sessão expirada (e nenhum, ao compor).
- **Motivo de partida indisponível**: catálogo indisponível, partida recusada, conexão desistiu; com o `MatchId`.
- **Resultado da partida**: desfecho do espelho e linha do histórico (buscando, resolvida, indisponível).
- **Saúde da conexão**: reconectando (tentativa, espera), voltou, desistiu (motivo), latência.
- **Assinatura**: o descartável devolvido por toda assinatura de aviso.
- **Contrato da superfície**: a lista do que a apresentação pode assinar e chamar.
- **Composição**: montagem de adaptadores e fachada, com slot da guarda, relógio, endereço e log escolhidos por quem compõe.
- **Hospedeiro Unity e ponto de acesso**: quem monta a composição nas cenas e onde as cenas a obtêm.
- **Roteador de cena**: o mapa estado do app → cena.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A suíte EditMode passa 100% pelo comando único da constituição, com o editor fechado, sem nenhum aviso de compilação novo.
- **SC-002**: 100% das transições do FR-010 têm teste EditMode, incluindo sessão expirada a partir de cada um dos 6 estados logados.
- **SC-003**: Em 100% dos roteiros de histórico (linha na primeira leitura, numa leitura intermediária, nunca, e cancelado), o número de leituras nunca passa de 4, o tempo total fica abaixo de 8 segundos de relógio injetado, e nenhum aviso sai depois de cancelado.
- **SC-004**: Zero avisos chegam a assinaturas descartadas, verificado por teste para cada tipo de aviso da fachada.
- **SC-005**: Uma busca no código encontra zero `static Instance` em código de rede, sessão ou clientes de socket, e zero usos de `SceneManager` fora do roteador de cena e das ferramentas de editor.
- **SC-006**: O teste de fronteira encontra zero tipos expostos pela fachada fora do contrato, zero tipos do contrato ausentes, e zero tipos usados pela prova final fora do contrato e da composição.
- **SC-007**: A prova final passa contra o backend local subido com `docker compose up`, cobrindo 100% dos itens da User Story 9, com as duas partidas terminando em menos de 15 minutos no total.
- **SC-008**: Com dois jogadores no Multiplayer Play Mode, Login → Home → Jogar → Versus → Match completa em 100% das tentativas, e derrubar o socket de partida não recarrega a `MatchScene`.
- **SC-009**: No Android, build de desenvolvimento, a cena de prova sem visual roda os dois clientes headless contra o IP do computador, e minimizar no meio da partida e voltar deixa a partida seguindo até o fim.
- **SC-010**: Os testes EditMode desta feature, somados, executam em menos de 5 segundos (fora a inicialização do editor), sem espera real de tempo.
- **SC-011**: Uma revisão encontra zero decisões de legalidade, dano, fase ou estouro de vez no código desta feature.

## Assumptions

- **Partida terminada entra na hora, e a linha chega depois.** O estado passa a partida terminada assim que o frame final é aceito, com o desfecho do espelho e a linha "buscando"; a linha resolvida ou indisponível sai como aviso em seguida. Esperar a linha para entrar no estado atrasaria a tela em segundos sem ganho. Se a intenção era entrar só com a linha decidida, este é o ponto a corrigir antes do plano.
- **Voltar de partida terminada é pedido da apresentação.** A fachada não volta sozinha para logado, porque a tela de resultado precisa ficar; a prova final pede a volta antes da partida 2.
- **Partida indisponível é um estado do app**, e não só um estado da partida corrente: cobre catálogo indisponível (dívida da 004), partida recusada e conexão de partida desistida por motivo que não é sessão expirada. Oferece tentar abrir de novo e voltar.
- **Tentativas do histórico**: até 4 leituras, a primeira imediata e as seguintes com esperas crescentes (por exemplo 1, 2 e 4 s), só da primeira página. "Poucas tentativas espaçadas" foi lido assim; o plano pode ajustar os números dentro do FR-016.
- **"Sair e retomar sem senha" na prova** é fechar o cliente (descartar a fachada) e compor outro sobre o mesmo slot. `sair` pela sessão de conta apaga a guarda (FR-007 da 002), então retomar depois dele não é possível por projeto; esse caminho é coberto em EditMode.
- **"A asmdef do teste referencia só a fachada e a composição"** foi lido como: a prova só usa tipos que o contrato da superfície lista ou que a composição expõe. As referências de asmdef no Unity não são transitivas, e a superfície expõe tipos das assemblies da 001–004 (`UserId`, `DeckId`, `PlayerView`, comandos); a asmdef da prova pode precisar referenciá-las para compilar. A garantia de "só a superfície" vem do teste de fronteira (FR-022), não da lista de referências. Se o plano achar um arranjo em que a lista de referências baste sozinha, melhor.
- **Verificação da renovação antes da reabertura** usa o log de diagnóstico que a composição aceita de quem compõe (FR-025): a prova confere, no log daquele cliente, a renovação do token antes da abertura do socket. Se o plano preferir um aviso da superfície, precisa justificar que a apresentação também precisa dele.
- **Estratégia da prova**: a do `smoke_match.py` (`ROUND_CAP` 30, `REFUSAL_LIMIT` 20, poção só com Nexus abaixo de 12), estendida para: remover bloqueador uma vez (atribui, remove, atribui de novo, encerra); preferir feitiço sem alvo quando houver na mão; deixar a vez estourar uma vez na Fase de Ação (`--stall`); e, na partida 2, desistir no mulligan. A cobertura dos comandos depende do embaralhamento do servidor; o teste falha nomeando o que faltou.
- **Tempos do servidor**: vez de 45 s com aviso aos 30 s, e mulligan de 30 s (`turn_clock.py`); o tempo limite de 15 minutos da prova cobre a partida 1 com uma vez estourada e a partida 2.
- **Saúde da conexão** mostra só a conexão que sustenta o estado atual; a de fila fica de fora a partir de pareado, como o overlay já fazia com o socket de presença.
- **Desistência só por comando**: sair ou sessão expirada em partida fecham o socket e não mandam `forfeit`; a partida termina pelos relógios do servidor.
- **Cena de prova Android**: uma cena sem visual, fora do fluxo normal, só em build de desenvolvimento, que compõe os dois clientes pela mesma composição da prova. Não entra no build de produção.
- **Limitações do backend** (`match_found` perdido, `leave` por `user_id`) continuam sem contorno.
- **Dependências**: features 001–004 no `main`; backend local em `C:/Users/gabri/Projetos/dev_container/anathema/backend` com `docker compose up`.
- **Fora do escopo**: desenhar partida, arena, cartas, telas de deck, fila, histórico ou resultado; escolha de deck; corrigir limitações do backend; regra de jogo no cliente.
