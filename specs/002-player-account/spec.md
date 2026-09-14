# Feature Specification: Conta e dados do jogador

**Feature Branch**: `002-player-account` (nenhum branch criado; spec no `main`)

**Created**: 2026-09-13

**Status**: Draft

**Input**: User description: "Conta e dados do jogador por HTTP: cadastro, login, tokens com renovação e guarda segura, perfil, catálogo, decks e histórico. Segunda de cinco features da camada de rede. Nada de socket aqui." (descrição completa na mensagem que abriu esta feature)

## Contexto

Segunda de cinco features da camada de rede do cliente. Tudo que vem depois
(fila, partida, histórico no fim da partida) precisa de um jogador autenticado e
de um token de acesso válido. O token de acesso dura 5 minutos e, no Android,
vence com o app minimizado; o refresh token dura 1 dia e não é rotacionado.

Hoje os tokens ficam num singleton de cena, o JSON passa por `JsonUtility`, nada
sobrevive a fechar o app e nenhum teste alcança esse código. Esta feature
constrói a sessão de conta sobre as portas da feature 001 (`IHttpTransport`,
`IMonotonicClock`, `IAppLifecycle`, `IClientLog`, `IProtocolCodec` e seus fakes)
e faz o código antigo (`PlayerSession`, `TokenRefreshService`,
`SelfProfileService`, `LoginController`) evoluir no lugar, sem camada paralela.

Os "usuários" desta feature são o jogador — que entra uma vez e continua
entrando sem senha — e as features seguintes, que pedem um token válido, o
catálogo e os decks.

**Contratos** — são do backend e não são copiados aqui. Fonte da verdade:

- `C:/Users/gabri/Projetos/dev_container/anathema/backend/specs/011-deck-catalog-api/contracts/http_catalog.md`
- `C:/Users/gabri/Projetos/dev_container/anathema/backend/specs/011-deck-catalog-api/contracts/http_decks.md`
- `C:/Users/gabri/Projetos/dev_container/anathema/backend/specs/012-match-result-history/contracts/http_match_history.md`

As rotas de conta (`/accounts/register/`, `/accounts/login/`,
`/accounts/token/refresh/`, `/players/me/`) não têm contrato escrito; as formas
foram verificadas no código do backend e estão na descrição da feature. Quando
esta spec e um contrato discordam, vale o contrato.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Entrar e continuar autenticado sem perceber o token (Priority: P1)

O jogador entra com usuário e senha na mesma tela de login de hoje e chega à
Home com o próprio perfil. A partir daí, qualquer parte do cliente que precise
falar com o servidor pede um token de acesso e recebe um válido: se ele está
perto de vencer, é renovado antes; se o servidor recusar o token mesmo assim, o
cliente renova uma vez e repete o pedido uma vez. Várias partes pedindo ao mesmo
tempo geram uma única renovação. Só quando o servidor recusa o refresh token o
jogador é avisado de que precisa entrar de novo — falta de rede nunca o desloga.

**Why this priority**: sem sessão e sem token válido, nenhuma feature seguinte
funciona, e o token de 5 minutos vence no meio de qualquer uso real.

**Independent Test**: testes EditMode com transporte HTTP fake roteirizado,
relógio monotônico fake e ciclo de vida fake exercitam login, perfil, margem de
renovação, renovação reativa, concorrência, expiração e falha de rede, sem cena e
sem servidor.

**Acceptance Scenarios**:

1. **Given** credenciais corretas, **When** o jogador entra, **Then** a sessão fica autenticada com o par de tokens em memória, o refresh token guardado no aparelho, o `UserId` próprio lido do token, e o perfil (apelido, ícone, nível, experiência, moedas, créditos) disponível.
2. **Given** credenciais erradas, **When** o jogador entra, **Then** o resultado é "credencial recusada", nada é guardado e a sessão continua não autenticada.
3. **Given** um token de acesso cujo `exp - iat` é 300 s, recebido no instante monotônico T, e uma margem de renovação M, **When** alguém pede um token no instante T + 300 s − M + 1 s, **Then** uma renovação acontece antes da entrega e quem pediu recebe o token novo.
4. **Given** o mesmo token, **When** alguém pede um token no instante T + 60 s, **Then** recebe o token atual sem nenhuma requisição de renovação.
5. **Given** um token com `iat` e `exp` de um ano qualquer muito distante da hora do aparelho, **When** a validade é avaliada com o relógio monotônico fake, **Then** o resultado depende só de `exp - iat` e do tempo monotônico decorrido — nunca da hora do sistema.
6. **Given** uma rota autenticada que responde 401, **When** o pedido é feito, **Then** acontece exatamente uma renovação e o pedido é repetido exatamente uma vez com o token novo; se a repetição responder 401 de novo, o resultado é essa recusa, sem nova renovação.
7. **Given** duas (ou dez) chamadas concorrentes que precisam de renovação, **When** elas acontecem ao mesmo tempo, **Then** o servidor recebe uma única requisição de renovação e todas as chamadas recebem o mesmo resultado.
8. **Given** uma renovação que o servidor recusa com 401, **When** ela termina, **Then** a sessão fica expirada, o refresh token é apagado do aparelho, todos que esperavam recebem "sessão expirada" e a apresentação recebe um único aviso de sessão expirada.
9. **Given** uma renovação que falha por transporte (sem rede, prazo esgotado), **When** ela termina, **Then** a sessão não expira, o refresh token continua guardado, quem esperava recebe falha de transporte e a próxima necessidade tenta renovar de novo.
10. **Given** uma sessão autenticada e o app em segundo plano, **When** o app volta depois de o token vencer ou entrar na margem, **Then** a renovação começa na volta, antes de qualquer pedido de token.
11. **Given** o jogador autenticado, **When** ele sai, **Then** tokens em memória e refresh guardado são apagados, a sessão fica não autenticada e nenhum aviso de sessão expirada é emitido.
12. **Given** a `LoginScene`, **When** o jogador entra com credenciais corretas, **Then** chega à `HomeScene` como hoje.

---

### User Story 2 - Abrir o app e continuar logado (Priority: P1)

O jogador fecha o app (ou o sistema o encerra) e abre de novo mais tarde. Se o
refresh token guardado ainda vale, ele entra sem digitar senha. Se não vale
mais, o cliente limpa o que estava guardado e mostra o login. A credencial fica
em armazenamento protegido da plataforma, nunca em texto puro.

**Why this priority**: no Android, o sistema encerra o app em segundo plano o
tempo todo. Pedir senha a cada volta torna o jogo inutilizável no celular.

**Independent Test**: testes EditMode com uma guarda segura fake pré-carregada
e transporte fake; teste LiveServer que cria uma segunda instância da sessão
sobre a mesma guarda real no Windows/editor.

**Acceptance Scenarios**:

1. **Given** um refresh token guardado e válido no servidor, **When** o app abre e a sessão tenta retomar, **Then** uma renovação é feita e a sessão fica autenticada sem senha.
2. **Given** um refresh token guardado que o servidor recusa (vencido ou inválido), **When** a sessão tenta retomar, **Then** a guarda é apagada, a sessão fica não autenticada e o resultado é "retomada recusada".
3. **Given** um refresh token guardado e nenhuma rede, **When** a sessão tenta retomar, **Then** a guarda continua intacta, a sessão fica não autenticada e o resultado é falha de transporte, permitindo nova tentativa.
4. **Given** nada guardado, **When** a sessão tenta retomar, **Then** nenhuma requisição é feita e o resultado é "nada a retomar".
5. **Given** a guarda real do Windows/editor, **When** uma sessão entra e outra instância da sessão é criada sobre a mesma guarda, **Then** a segunda retoma sem senha.

---

### User Story 3 - Criar conta (Priority: P2)

Quem ainda não tem conta cadastra usuário, e-mail, senha e confirmação. Se o
servidor recusar, cada campo recusado vem com as mensagens que o servidor mandou,
para a apresentação mostrar ao lado do campo certo. Cadastro não autentica: o
login vem em seguida.

**Why this priority**: necessário para o teste de ponta a ponta com conta nova e
para a futura tela de cadastro, mas o jogo já funciona com contas existentes.

**Independent Test**: EditMode com transporte fake para 201 e para 400 com
dicionário campo → mensagens; LiveServer cadastrando nome único.

**Acceptance Scenarios**:

1. **Given** dados válidos e inéditos, **When** o cadastro é enviado, **Then** o resultado é "conta criada", sem tokens e sem mudar a sessão.
2. **Given** um e-mail já usado, **When** o cadastro é enviado, **Then** o resultado é recusa por campo com o campo e-mail e a lista de mensagens do servidor.
3. **Given** uma recusa com um campo que o cliente não conhece (ex.: erro geral sem campo), **When** é lida, **Then** o campo desconhecido é preservado com o nome que o servidor mandou e as mensagens, sem descartar os demais.

---

### User Story 4 - Consultar o catálogo de cartas (Priority: P2)

Qualquer parte do cliente que tem um `CardId` na mão (mão, banco, deck,
histórico) consulta nome, custo, arte e — conforme o tipo — ataque e vida da
unidade, ou descrição e forma do efeito do feitiço. O catálogo é buscado uma vez
por sessão.

**Why this priority**: sem catálogo, as features de partida não conseguem mostrar
nem mirar carta alguma; mas depende da sessão (P1).

**Independent Test**: EditMode com transporte fake devolvendo um catálogo com
unidades, feitiços, valores fora do conjunto e carta malformada; LiveServer
contando cartas.

**Acceptance Scenarios**:

1. **Given** o catálogo ainda não buscado, **When** duas consultas chegam ao mesmo tempo, **Then** uma única requisição é feita e as duas recebem o resultado.
2. **Given** o catálogo buscado, **When** um `CardId` de unidade é consultado, **Then** o resultado é uma carta de unidade com nome, custo, arte, ataque e vida — um tipo distinto do de feitiço.
3. **Given** o catálogo buscado, **When** um `CardId` de feitiço é consultado, **Then** o resultado é uma carta de feitiço com descrição e efeito tipado (`requires_target`, `target_kind`, `duration`, `declaration_only`).
4. **Given** um `CardId` que não está no catálogo, **When** é consultado, **Then** o resultado é "carta não encontrada", sem exceção.
5. **Given** um feitiço com `target_kind` ou `duration` fora do conjunto do contrato, **When** o catálogo é lido, **Then** a carta continua consultável com o valor desconhecido explícito preservando o texto, e o fato é registrado no log; as demais cartas não são afetadas.
6. **Given** uma busca que falhou por transporte, **When** a próxima consulta chega, **Then** uma nova busca é feita (falha não fica guardada).

---

### User Story 5 - Gerenciar os próprios decks (Priority: P2)

O jogador lista os decks, lê um, cria, renomeia e/ou troca a lista de cartas e
apaga. Quando o servidor recusa, o cliente entrega uma recusa tipada: nome
inválido, problemas da lista (tamanho errado, cópias demais, carta desconhecida,
ou tipo de problema novo com a mensagem do servidor), campo faltando, teto de
decks, ou "não encontrado". O cliente não valida o deck: quem valida é o servidor.

**Why this priority**: o deck é o que o jogador leva para a fila (feature 3); a
tela de decks vem depois, mas as operações e as recusas precisam existir.

**Independent Test**: EditMode com transporte fake para cada resposta do
contrato de decks; LiveServer com o deck de feitiços do `scripts/smoke_match.py`.

**Acceptance Scenarios**:

1. **Given** um jogador sem deck, **When** lista, **Then** recebe lista vazia, não recusa.
2. **Given** uma lista de 40 cartas válida, **When** cria, **Then** recebe o deck criado com `DeckId`, nome e `CardId`s.
3. **Given** uma lista de 12 cartas, **When** cria, **Then** recebe recusa com o problema "tamanho errado", encontrado 12 e exigido 40.
4. **Given** uma recusa com três problemas na lista, **When** é lida, **Then** os três chegam juntos e na ordem do servidor, cada um com seus campos.
5. **Given** um problema de lista com `kind` que o cliente não conhece, **When** é lido, **Then** vira problema desconhecido com o texto do `kind` e a `message`.
6. **Given** um `DeckId` inexistente ou de outro jogador, **When** lê, altera ou apaga, **Then** o resultado é o mesmo "deck não encontrado" nos dois casos.
7. **Given** um deck existente, **When** apaga e lê de novo, **Then** apagar tem sucesso e a leitura seguinte dá "deck não encontrado".
8. **Given** o tipo que representa um problema de lista, **When** a feature 3 o usar para a recusa `invalid_deck` do socket, **Then** ele não depende de nada do HTTP.

---

### User Story 6 - Ler o histórico de partidas (Priority: P3)

O jogador vê as próprias partidas, da mais recente para a mais antiga, página a
página: se ganhou, como acabou, contra quem (quando o oponente ainda existe),
quanto durou, em que rodada terminou e quando.

**Why this priority**: útil no fim da partida e numa tela futura, mas nada
depende dele.

**Independent Test**: EditMode com transporte fake para página cheia, lista
vazia, oponente nulo, `end_reason` fora do conjunto e 404 na primeira e em outra
página; LiveServer com conta nova.

**Acceptance Scenarios**:

1. **Given** uma conta nova, **When** lê a página 1, **Then** recebe total 0, lista vazia, sem próxima nem anterior.
2. **Given** uma página com oponente nulo, **When** é lida, **Then** a linha existe com o desfecho intacto e oponente explicitamente ausente.
3. **Given** um 404 pedindo a página 3, **When** é lido, **Then** o resultado é "passou do fim".
4. **Given** um 404 pedindo a página 1, **When** é lido, **Then** o resultado é "conta sem perfil".
5. **Given** uma linha com `end_reason` fora do conjunto, **When** é lida, **Then** a linha continua com o valor desconhecido explícito e o fato é registrado no log.

---

### Edge Cases

**Token e validade**
- Resposta de login ou de renovação com token que não se lê como JWT, sem `exp` ou `iat` numéricos, com `exp <= iat`, ou sem `user_id` que seja texto com um inteiro positivo (o SimpleJWT grava o claim como texto, ex.: `"7"`): resultado "resposta fora do contrato"; no login a sessão fica não autenticada; na renovação a sessão não expira. Nenhum token vai para o log.
- Hora do aparelho errada em horas ou dias, ou alterada enquanto o app roda: validade inalterada.
- Latência entre a emissão e a chegada do token: a vida útil contada a partir da chegada fica maior que a real no máximo pela latência; a margem de renovação cobre essa diferença.
- Pedido que tomou 401 com um token que outra chamada já trocou: repete com o token atual sem disparar outra renovação.
- 401 numa rota autenticada depois que a sessão já expirou por outra chamada: resultado "sessão expirada", sem nova renovação.
- Renovação que termina depois de o jogador sair ou de outro login: o resultado é descartado e não altera a sessão atual.
- Renovação recusada com status diferente de 401 (400, 5xx): não expira a sessão; é falha distinguível de transporte.
- Volta ao primeiro plano com renovação já em curso: compartilha a mesma; volta sem sessão autenticada: nada acontece.
- Pedido de token sem sessão autenticada: resultado "sem sessão", sem requisição e sem aviso de sessão expirada.
- Segundo login pedido enquanto um está em curso: recusado localmente com "login em andamento".
- Login de outra conta com uma sessão ativa: substitui tokens, guarda e `UserId`; catálogo buscado de novo na nova sessão.

**Guarda segura**
- Leitura que falha (chave da plataforma invalidada, arquivo corrompido, outro usuário do Windows): tratada como nada guardado, entrada apagada e fato registrado sem o valor.
- Falha ao gravar no login: o login vale em memória; o fato é registrado e a retomada seguinte não encontra nada.
- Dois jogadores virtuais do Multiplayer Play Mode no mesmo editor: cada um tem a própria guarda; um nunca retoma a sessão do outro.
- Refresh token com mais de 1 dia: a retomada é recusada pelo servidor e segue o cenário de retomada recusada.

**Cliente HTTP autenticado**
- Corpo de erro que não é JSON (página HTML de proxy, 502): recusa com status e corpo não reconhecido, sem exceção.
- Corpo de sucesso que não segue o contrato: "resposta fora do contrato", com status, registrado no log sem credencial.

**Catálogo**
- `card_type` fora do conjunto: a carta é omitida e registrada no log com `card_id` e texto; o resto do catálogo vale.
- Carta com campo obrigatório ausente ou de tipo errado: omitida e registrada com `card_id` e nome do campo.
- `card_id` repetido: vale a primeira ocorrência; a repetição é registrada.
- `requires_target` incoerente com `target_kind`: exposto como veio; o cliente não decide regra.
- Resposta sem a lista `cards`: a busca inteira falha como "resposta fora do contrato".
- Sessão encerrada (sair, expirar): o catálogo guardado é descartado.

**Decks**
- Corpo 400 com mais de uma recusa reconhecida (ex.: nome e lista): todas entregues juntas.
- Corpo 400 sem nenhuma chave reconhecida: recusa não reconhecida com status e corpo em texto.
- Alterar sem nome e sem lista: o comando não pode ser montado.

**Histórico**
- Página ou tamanho de página menor que 1: o pedido não pode ser montado; mensagem com o valor recebido e o esperado.
- Tamanho de página acima de 100: enviado como veio; o servidor limita.
- `ended_at` é só para mostrar: nunca usado para medir tempo.

## Requirements *(mandatory)*

### Functional Requirements

**Identificadores**

- **FR-001**: `DeckId` e `CardId` MUST existir como tipos distintos, no mesmo padrão de `UserId`, `MatchId` e `CardInstanceId` da feature 001: passar um onde se espera outro MUST ser erro de compilação, igualdade por valor, representação legível no log, inteiro cru no JSON e valor de tipo errado resultando em decodificação inválida.

**Conta**

- **FR-002**: A sessão MUST cadastrar conta com usuário, e-mail, senha e confirmação; sucesso MUST NOT autenticar nem mudar a sessão.
- **FR-003**: Recusa de cadastro MUST ser entregue por campo, com a lista de mensagens de cada um; campos conhecidos (usuário, e-mail, senha, confirmação) MUST ser tipados e campo desconhecido MUST ser preservado com o nome recebido.
- **FR-004**: A sessão MUST entrar com usuário e senha; credencial recusada MUST ser resultado distinto de recusa com outro status, de falha de transporte e de resposta fora do contrato.
- **FR-005**: Ao entrar, a sessão MUST manter o par de tokens só em memória, guardar o refresh token no aparelho e expor o `UserId` próprio lido do claim `user_id` do token de acesso.
- **FR-006**: A sessão MUST ler o próprio perfil (apelido, ícone, nível, experiência, moedas, créditos); conta sem perfil MUST ser resultado explícito.
- **FR-007**: A sessão MUST sair apagando tokens em memória e refresh guardado, sem chamar o servidor e sem aviso de sessão expirada.
- **FR-008**: A sessão MUST aceitar um login por vez; pedido concorrente MUST ser recusado localmente com resultado próprio.

**Validade do token**

- **FR-009**: A vida útil do token de acesso MUST ser `exp - iat` do próprio token e MUST ser contada a partir do instante monotônico em que a resposta chegou. A hora do sistema MUST NOT participar do cálculo.
- **FR-010**: Token sem `exp` e `iat` numéricos ou com `exp <= iat` MUST ser tratado como resposta fora do contrato.

**Renovação**

- **FR-011**: MUST existir uma porta que entrega um token de acesso válido, usada pelo cliente HTTP autenticado e, na feature 3, pelo `?token=` do socket.
- **FR-012**: Se faltar menos que a margem de renovação para o token vencer, a porta MUST renovar antes de entregar. A margem MUST ser definida, documentada e injetável nos testes.
- **FR-013**: Renovações MUST ser únicas por vez: pedidos concorrentes MUST compartilhar a mesma requisição e o mesmo resultado.
- **FR-014**: Renovação recusada com 401 MUST expirar a sessão, apagar o refresh guardado, entregar "sessão expirada" a todos que esperavam e emitir exatamente um aviso de sessão expirada para a apresentação.
- **FR-015**: Renovação que falha por transporte, por status diferente de 401 ou por resposta fora do contrato MUST NOT expirar a sessão nem apagar a guarda.
- **FR-016**: Resultado de renovação que chega depois de sair ou de outro login MUST ser descartado.
- **FR-017**: Ao voltar ao primeiro plano com sessão autenticada e token vencido ou na margem, a sessão MUST começar a renovação imediatamente, sem esperar pedido de token.

**Retomada**

- **FR-018**: A sessão MUST oferecer retomada: com refresh guardado, renova e fica autenticada sem senha, lendo o `UserId` do novo token de acesso.
- **FR-019**: Retomada recusada com 401 MUST apagar a guarda e deixar a sessão não autenticada, sem aviso de sessão expirada; retomada com falha de transporte MUST manter a guarda; sem nada guardado MUST NOT fazer requisição. Os quatro desfechos MUST ser distinguíveis.

**Guarda segura**

- **FR-020**: MUST existir uma porta de guarda segura para salvar, ler e apagar o refresh token, com um fake nomeado reutilizável pelas features seguintes.
- **FR-021**: O adaptador do Android MUST proteger o valor com o Android Keystore; o do Windows (build e editor) MUST usar DPAPI. Como chegar à DPAPI sem `ProtectedData` no perfil de API do projeto MUST ser investigado e registrado no research.
- **FR-022**: Credencial MUST NOT ser gravada em `PlayerPrefs` nem em nenhum armazenamento em texto puro.
- **FR-023**: Falha de leitura da guarda MUST virar "nada guardado" com a entrada apagada; falha de gravação MUST NOT impedir o login em memória; as duas MUST ser registradas sem o valor.
- **FR-024**: No editor com Multiplayer Play Mode, cada jogador virtual MUST ter guarda própria.

**Cliente HTTP autenticado**

- **FR-025**: Catálogo, decks, perfil e histórico MUST passar por um único cliente HTTP autenticado sobre `IHttpTransport`, que põe o token de acesso no cabeçalho `Authorization: Bearer`.
- **FR-026**: Um 401 numa rota autenticada MUST gerar no máximo uma renovação e no máximo uma repetição do pedido; um segundo 401 MUST ser devolvido como recusa. Se o token já foi trocado por outra chamada, a repetição MUST usar o token atual sem nova renovação.
- **FR-027**: O resultado MUST distinguir: sucesso (status e corpo tipado), recusa do servidor (status e corpo tipado, com braço explícito para corpo não reconhecido), sessão expirada ou sem sessão, falha de transporte e resposta fora do contrato. Nenhuma exceção MUST escapar.

**Catálogo**

- **FR-028**: O catálogo MUST ser buscado no máximo uma vez por sessão e guardado; buscas concorrentes MUST compartilhar a mesma requisição; falha MUST NOT ficar guardada; sair ou expirar MUST descartar o guardado.
- **FR-029**: A consulta por `CardId` MUST devolver carta de unidade, carta de feitiço (tipos distintos) ou "carta não encontrada", sem exceção.
- **FR-030**: O efeito do feitiço MUST ser tipado com `requires_target`, `target_kind`, `duration` e `declaration_only`, com os conjuntos fechados do contrato de catálogo; valor fora do conjunto MUST virar valor desconhecido explícito que preserva o texto, registrado no log.
- **FR-031**: Carta com `card_type` fora do conjunto, com campo obrigatório ausente ou de tipo errado, ou com `card_id` repetido MUST ser omitida (a repetição, não a primeira) e registrada no log, sem derrubar o restante do catálogo.

**Decks**

- **FR-032**: MUST ser possível listar, ler um, criar (nome e lista), alterar (nome, lista ou os dois; ao menos um) e apagar decks do autenticado, conforme o contrato de decks.
- **FR-033**: Recusas de deck MUST ser tipadas: nome inválido (mensagens), problemas da lista, campo faltando na criação (texto), teto de decks (mensagens), deck não encontrado (um só resultado para "não existe" e "não é seu") e recusa não reconhecida (status e corpo em texto). Recusas reconhecidas presentes no mesmo corpo MUST ser entregues juntas.
- **FR-034**: Problema de lista MUST ser uma família fechada: tamanho errado (encontrado, exigido), cópias demais (`CardId`, contagem, limite), carta desconhecida (`CardId`) e desconhecido (texto do `kind`); todos com a `message` do servidor, na ordem recebida.
- **FR-035**: O tipo de problema de lista MUST NOT depender do cliente HTTP nem do resultado HTTP, para ser reusado pela recusa `invalid_deck` do socket de fila (feature 3).
- **FR-036**: O cliente MUST NOT validar tamanho, cópias ou cartas do deck antes de enviar.

**Histórico**

- **FR-037**: O histórico MUST ser lido por página e tamanho de página (ambos ≥ 1) e devolver total, se há próxima, se há anterior e as linhas na ordem do servidor.
- **FR-038**: Cada linha MUST trazer `MatchId`, vitória, motivo do fim (conjunto fechado do contrato, com valor desconhecido explícito registrado no log), oponente ou ausência explícita dele, duração em segundos, rodada final e instante do fim só para exibição.
- **FR-039**: 404 numa página maior que 1 MUST ser "passou do fim"; na página 1 MUST ser "conta sem perfil".

**Credenciais e log**

- **FR-040**: Senha, token de acesso e refresh token MUST NOT aparecer em nenhum registro de log, em nenhum cenário, inclusive nos de resposta fora do contrato.

**Código anterior, evoluindo no lugar**

- **FR-041**: `PlayerSession`, `TokenRefreshService` e `SelfProfileService` MUST passar a ser (ou delegar para) a sessão de conta, a renovação e a leitura de perfil desta feature; MUST NOT existir segunda fonte de tokens.
- **FR-042**: `LoginController` MUST usar a sessão de conta, manter a mesma tela e o mesmo destino (`HomeScene`), manter o login automático de desenvolvimento dos jogadores virtuais, e MUST NOT referenciar `UnityEditor`.
- **FR-043**: `BaseClient`, `MatchClient` e `MatchmakingClient` MUST continuar funcionando sem mudança de comportamento: a leitura do token atual e o pedido de renovação que fazem hoje MUST passar por uma ponte de compatibilidade sobre a sessão nova, e o plano MUST registrar essa ponte como dívida para a feature 3.
- **FR-044**: A tela que mostra o mini perfil na Home MUST continuar mostrando apelido, nível, moedas e ícone do jogador logado.
- **FR-045**: `AppConfig` MUST ganhar as rotas de cadastro, catálogo, decks e histórico, no mesmo padrão das rotas existentes, e os assets de configuração de desenvolvimento e produção MUST ser preenchidos.
- **FR-046**: DTOs com `JsonUtility` tocados por esta feature (login, renovação, perfil) MUST passar para o codec do projeto ou ser removidos.

### Key Entities

- **Sessão de conta**: estado (não autenticada, autenticando, autenticada, expirada), `UserId` próprio, par de tokens em memória; emite aviso de sessão expirada.
- **Token de acesso**: texto opaco para o servidor, vida útil `exp - iat`, instante monotônico de chegada, `UserId` do claim.
- **Refresh token**: texto guardado na guarda segura; não rotacionado pelo servidor.
- **Guarda segura**: salvar, ler, apagar um refresh token; uma por jogador virtual no editor.
- **Resultado de login / cadastro / retomada / renovação**: famílias fechadas com os desfechos listados nos requisitos.
- **Recusa de cadastro**: campo (conhecido ou desconhecido com nome) → lista de mensagens.
- **Perfil próprio**: apelido, ícone, nível, experiência, moedas, créditos.
- **Resultado HTTP autenticado**: sucesso, recusa (status + corpo tipado ou não reconhecido), sessão expirada/sem sessão, falha de transporte, resposta fora do contrato.
- **Catálogo**: conjunto de cartas indexado por `CardId`; carta de unidade (nome, custo, arte, ataque, vida) e carta de feitiço (nome, custo, arte, descrição, efeito).
- **Efeito de feitiço**: pede alvo, tipo de alvo (nenhum, unidade aliada, unidade inimiga, desconhecido), duração (permanente, até o fim da rodada, desconhecida), só na declaração.
- **Deck**: `DeckId`, nome, lista de `CardId`.
- **Problema de lista de deck**: tamanho errado, cópias demais, carta desconhecida, desconhecido — cada um com `message`. Independente do HTTP.
- **Recusa de deck**: nome inválido, problemas da lista, campo faltando, teto, não encontrado, não reconhecida.
- **Página de histórico**: total, há próxima, há anterior, linhas.
- **Linha de histórico**: `MatchId`, vitória, motivo do fim, oponente opcional (`UserId`, apelido, ícone, nível), duração, rodada final, instante do fim para exibição.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A suíte EditMode passa 100% pelo comando único da constituição, com o editor fechado, e o código novo não produz nenhum aviso de compilação.
- **SC-002**: Zero tipos do núcleo referenciam o motor, a biblioteca de JSON de terceiros ou o transporte HTTP do motor, verificado por teste automatizado.
- **SC-003**: Em todos os cenários EditMode desta feature, zero registros de log contêm a senha, o token de acesso ou o refresh token usados no cenário, e zero credenciais são gravadas em `PlayerPrefs`.
- **SC-004**: Com 10 pedidos concorrentes de token na margem ou tomando 401, o servidor fake recebe exatamente 1 requisição de renovação.
- **SC-005**: Em 100% dos cenários de 401 numa rota autenticada, o número de renovações é ≤ 1 e o de repetições é ≤ 1.
- **SC-006**: Os testes LiveServer passam contra o backend local subido com `docker compose up`: cadastrar conta nova, entrar e ler perfil; catálogo com 29 cartas (24 unidades, 5 feitiços); deck inicial listado sem feitiço; criar o deck de feitiços do `scripts/smoke_match.py` (201), renomear, criar deck de 12 cartas (tamanho errado), apagar e ler (não encontrado); histórico da conta nova com total 0; renovar e usar o token novo numa rota autenticada; segunda instância da sessão sobre a mesma guarda retoma sem senha.
- **SC-007**: Pela `LoginScene`, o login com credenciais corretas leva à `HomeScene` com o mini perfil preenchido em 100% das tentativas.
- **SC-008**: Seguindo só o quickstart no Android, o jogador entra, fecha o app pelo sistema, abre de novo e continua logado sem digitar senha, na primeira tentativa.
- **SC-009**: Seguindo só o quickstart no Android, depois de mais de 5 minutos minimizado, a primeira rota autenticada ao voltar funciona sem pedir login e sem erro visível.
- **SC-010**: Um catálogo com uma carta malformada e um feitiço com valor fora do conjunto continua servindo 100% das demais cartas.
- **SC-011**: Os testes EditMode desta feature, somados, executam em menos de 5 segundos (fora a inicialização do editor), sem espera real de tempo.

## Assumptions

- **Claims do token verificados no backend**: não há configuração de SimpleJWT no backend (5.5.1), então valem os padrões — acesso 5 min, refresh 1 dia, sem rotação, claim `user_id` com o `User.id` convertido em texto (`str(user_id)` em `RefreshToken.for_user`) e `iat` gravado em todo token novo, inclusive no acesso gerado pela renovação. Por isso `UserId` é exigido no login e na retomada (FR-005, FR-018). Pela decisão `0001 - Um perfil por usuário`, esse `user_id` é a identidade em toda a camada.
- **Margem de renovação**: 30 segundos por padrão, injetável. Cobre latência de ida e volta e o intervalo entre pedir o token e usá-lo; o plano pode ajustar com justificativa.
- **Retomada recusada não emite aviso de sessão expirada**: não havia sessão autenticada para expirar; a apresentação já está no login.
- **Segundo 401 depois de renovar com sucesso** é devolvido como recusa, não expira a sessão: o refresh valeu, então a causa não é a sessão (FR-026).
- **"Resposta fora do contrato"** é um quinto desfecho do cliente autenticado, além dos quatro da descrição, para corpo de sucesso que não decodifica; sem ele, a quebra de contrato viraria exceção ou sucesso vazio.
- **Mini perfil na Home**: hoje o login carrega a Home sem esperar o perfil, e o mini perfil lê a sessão no `Awake`, podendo aparecer vazio. Assume-se que ler o perfil antes de trocar de cena mantém "o mesmo fluxo"; falha ao ler o perfil não impede a Home e fica registrada.
- **`PlayerPrefs`**: o `Game/TODO.md` diz que o login ainda usa `PlayerPrefs`, mas o código atual de `Assets/Scripts` não o usa. A exigência (FR-022, SC-003) vale como garantia, não como remoção.
- **Guarda por jogador virtual**: o Multiplayer Play Mode roda dois jogadores na mesma máquina e no mesmo usuário do Windows; sem separação, um retomaria a sessão do outro (FR-024). Como separar é decidido no plano.
- **Logout** é só local: o backend não tem rota de logout nem lista de revogação; o refresh continua válido no servidor até vencer.
- **Perfil** não é guardado entre execuções; a retomada relê quando alguém precisar.
- **Tecnologias citadas na descrição** (Android Keystore, DPAPI, P/Invoke em `crypt32`, `UnityWebRequest`, Newtonsoft) são restrições já decididas pela constituição e vão para o plano e o research.
- **Dependências**: portas, fakes e codec da feature 001; backend local em `C:/Users/gabri/Projetos/dev_container/anathema/backend` com `docker compose up` para LiveServer e quickstart; `scripts/smoke_match.py` como referência do deck de feitiços.
- **Fora do escopo**: socket, reconexão, heartbeat, fila e religar `BaseClient` nas portas novas (feature 3); mensagens e estado de partida (feature 4); validar deck no cliente; telas de cadastro, decks ou histórico e qualquer UI além de religar o `LoginController`; logout no servidor; iOS, WebGL, macOS e Linux.
