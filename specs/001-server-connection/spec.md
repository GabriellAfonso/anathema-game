# Feature Specification: Conexão com o servidor

**Feature Branch**: `001-server-connection` (nenhum branch criado; spec no `main`)

**Created**: 2026-09-13

**Status**: Draft

**Input**: User description: "Conexão com o servidor: a base da camada de rede do cliente Unity — interfaces, transporte real, codec do protocolo e ambiente do Android. Primeira de cinco features; nenhuma regra de jogo, nenhum login, nenhuma reconexão aqui." (descrição completa na mensagem que abriu esta feature)

## Contexto

Primeira de cinco features da camada de rede do cliente. Entrega as peças sobre
as quais conta (feature 2), reconexão e fila (feature 3) e partida (features 4 e
5) se apoiam: falar HTTP e WebSocket com o servidor, traduzir frames do
protocolo, entregar eventos na thread principal e rodar no Android — tudo
testável sem cena e sem servidor, com a fronteira do princípio VI da
constituição.

Hoje o código de rede usa a NativeWebSocket direto, depende de singletons de
cena e nenhum teste o alcança. Esta feature **não** religa esse código; ela
constrói ao lado o que ele vai usar na feature 3.

Os "usuários" desta feature são as features seguintes e quem as escreve. O
jogador só a percebe no fim: o build de desenvolvimento no celular fala com o
servidor do computador.

**Contratos do protocolo** — são do backend e não são copiados aqui. Fonte da
verdade em `C:/Users/gabri/Projetos/dev_container/anathema/backend/specs/`:

- `009-match-protocol/contracts/` — envelope, `message_refused`, códigos de recusa
- `011-deck-catalog-api/contracts/` — HTTP autenticado, socket de matchmaking,
  recusas com campos extras
- `013-socket-heartbeat/contracts/heartbeat_messages.md` — `ping`/`pong`, eco do
  payload, close codes dos gates

Quando esta spec e um contrato discordam, vale o contrato.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Testar comportamento de rede sem cena e sem servidor (Priority: P1)

Quem escreve uma feature seguinte (login, fila, partida) precisa exercitar o que
acontece quando o socket abre, recebe um frame, fecha com 4001, quando o HTTP
volta 401 ou falha por falta de rede, quando o app vai para segundo plano ou o
celular troca de Wi-Fi para dados móveis — e precisa fazer isso em teste
EditMode, com tempo injetado, sem abrir cena e sem subir servidor.

**Why this priority**: sem as portas e os fakes, nenhuma feature seguinte
consegue cumprir o princípio VII. É a peça de que todas dependem.

**Independent Test**: um teste EditMode monta cada fake, roteiriza eventos
(abertura, texto, fechamento com código, erro; resposta HTTP e falha de
transporte; ida e volta do segundo plano; troca de rede; avanço do relógio) e
verifica o que foi entregue e o que foi pedido — sem nenhum adaptador real.

**Acceptance Scenarios**:

1. **Given** um socket fake aberto, **When** o teste simula fechamento com código 4001 e motivo, **Then** o assinante recebe exatamente um aviso de fechado com o número 4001 e o motivo, e nenhum evento depois dele.
2. **Given** um transporte HTTP fake roteirizado para responder 401, **When** uma requisição é feita, **Then** o resultado é uma resposta com status 401 e corpo, e nenhuma exceção é lançada.
3. **Given** um transporte HTTP fake roteirizado para falha de transporte (sem rede ou prazo esgotado), **When** uma requisição é feita, **Then** o resultado é uma falha de transporte com a categoria da falha, distinguível de qualquer resposta do servidor.
4. **Given** um relógio fake e um ciclo de vida fake, **When** o app vai para segundo plano, o relógio avança 7 minutos e o app volta, **Then** o assinante recebe "foi para segundo plano" e depois "voltou" com duração fora de 7 minutos.
5. **Given** uma alcançabilidade fake em Wi-Fi, **When** o teste troca para dados móveis sem passar por "sem rede", **Then** o assinante recebe um aviso de mudança de Wi-Fi para dados móveis.
6. **Given** um log fake, **When** algo é registrado com campos, **Then** o teste lê nível, mensagem e cada campo nomeado.

---

### User Story 2 - Traduzir frames do protocolo em tipos do projeto (Priority: P1)

Quem escreve uma feature seguinte recebe texto do socket e precisa de um valor
tipado — um envelope com `type`, uma recusa com `code`, um `pong` com o marcador
que mandou — ou de um resultado claro de "frame inválido". Precisa também
registrar braços novos de uniões fechadas (`type`, `kind`, `modifier_kind`) sem
reescrever a infraestrutura, e ter identificadores que o compilador não deixa
trocar.

**Why this priority**: todo frame que as features 2 a 5 leem passa por aqui. Sem
o codec, cada uma reimplementa o `switch` e esquece um braço (princípio IV).

**Independent Test**: testes EditMode alimentam o codec com textos — válidos,
com campos a mais, com discriminador desconhecido, malformados — e verificam o
valor tipado ou o resultado inválido; nenhum socket envolvido.

**Acceptance Scenarios**:

1. **Given** o texto de um `message_refused` com `code`, `error` e um campo extra (ex.: `deck_id` do contrato 011), **When** é decodificado, **Then** o resultado é uma recusa com o `code`, o `error` e o campo extra legível pela feature que o conhece.
2. **Given** um frame com `type` que nenhum braço registrou, **When** é decodificado, **Then** o resultado é o tipo explícito de valor desconhecido, carregando o texto do discriminador — não um erro e não um dicionário solto.
3. **Given** um texto que não é JSON, um JSON que não é objeto, ou um objeto sem `type`, **When** é decodificado, **Then** o resultado é decodificação inválida com o motivo, e nenhuma exceção escapa do codec.
4. **Given** um `ping` codificado com um marcador no payload, **When** o `pong` com o mesmo payload é decodificado, **Then** o marcador lido é igual ao enviado.
5. **Given** um `auth_denied` com `error`, **When** é decodificado, **Then** o resultado é uma negação de autenticação com o texto do erro.
6. **Given** um frame conhecido com campos que o cliente não conhece, **When** é decodificado, **Then** os campos a mais são ignorados e o valor tipado sai igual ao do frame sem eles.
7. **Given** um trecho de código que passa um `UserId` onde se espera `CardInstanceId`, **When** o projeto compila, **Then** a compilação falha.

---

### User Story 3 - Falar com o servidor local de verdade (Priority: P2)

Quem desenvolve precisa confirmar que os adaptadores reais — socket, HTTP,
relógio, ciclo de vida, alcançabilidade, log — funcionam contra o backend
rodando no computador, e que todo evento que eles produzem chega na thread
principal, na ordem, e nunca depois de o objeto hospedeiro ser destruído.

**Why this priority**: os fakes só valem se imitam os adaptadores reais. Depende
das histórias 1 e 2 (portas e codec) e é o que destrava a feature 3.

**Independent Test**: três testes `[Explicit]` `[Category("LiveServer")]`, com o
backend no ar por `docker compose up`, usando só os adaptadores reais e o codec.
A entrega na thread principal também tem testes EditMode sem servidor.

**Acceptance Scenarios**:

1. **Given** o backend local no ar, **When** o adaptador HTTP faz `GET /game/cards/` sem token, **Then** o resultado é uma resposta com status 401 (não falha de transporte, não exceção).
2. **Given** o backend local no ar, **When** o adaptador de socket abre `ws/matchmaking/` com token inválido, **Then** chega um frame que o codec decodifica como `auth_denied`, seguido do aviso de fechado com código 4001.
3. **Given** um token obtido por `POST /accounts/login/` cru, **When** o socket abre `ws/matchmaking/?token=...` e manda `ping` com um marcador, **Then** chega um `pong` com o mesmo marcador.
4. **Given** qualquer um dos três cenários acima, **When** um evento do adaptador chega ao assinante, **Then** ele está na thread principal.
5. **Given** o ponto de entrega na thread principal, **When** vários produtores em outras threads enfileiram itens, **Then** os itens são entregues na thread principal na ordem em que foram enfileirados, sem perda.
6. **Given** o objeto hospedeiro destruído, **When** outra thread enfileira um item, **Then** o item nunca é entregue e nenhuma exceção é lançada.

---

### User Story 4 - Rodar o build de desenvolvimento no celular apontando para o computador (Priority: P3)

Quem desenvolve instala o build de desenvolvimento num aparelho Android na mesma
rede do computador, informa o IP do computador sem gerar outro build, e o app
fala HTTP e WebSocket sem TLS com o backend local. O build de produção nunca
aceita tráfego sem TLS: se alguém tentar gerá-lo com TLS desligado, o build
falha na hora, dizendo qual ambiente e o que corrigir.

**Why this priority**: no celular, `localhost` é o próprio aparelho; sem isso não
há teste manual no Android. Mas depende das três histórias anteriores e não
bloqueia as features 2 e 3, que evoluem com testes EditMode.

**Independent Test**: teste EditMode da validação de build (configuração de
produção com TLS desligado é rejeitada com a mensagem esperada; com TLS ligado
passa) e quickstart manual no aparelho fazendo `ping`/`pong`.

**Acceptance Scenarios**:

1. **Given** a configuração de produção com TLS desligado, **When** um build de produção é iniciado, **Then** o build falha antes de gerar o pacote, com mensagem que nomeia o ambiente de produção e diz para ligar TLS.
2. **Given** a configuração de produção com TLS ligado, **When** a validação de build roda, **Then** ela passa sem aviso.
3. **Given** o build de desenvolvimento instalado no aparelho e o backend no computador, **When** o host é trocado em tempo de execução para o IP do computador, **Then** as URLs HTTP e WebSocket passam a usar esse host sem novo build.
4. **Given** o build de desenvolvimento apontando para o computador, **When** o app faz login cru e manda `ping` com marcador pelo socket de matchmaking, **Then** recebe o `pong` com o mesmo marcador, seguindo só os passos do quickstart.
5. **Given** o build de produção, **When** qualquer código tenta trocar o host em tempo de execução ou abrir `http://`/`ws://`, **Then** a troca não é aceita e o tráfego sem TLS é bloqueado.

---

### Edge Cases

**Socket**
- Queda sem handshake de fechamento (rede caiu, servidor morreu): o aviso de fechado chega uma vez, marcado como "sem código do servidor", distinguível de um fechamento com código.
- Texto do servidor fragmentado em vários quadros WebSocket, ou maior que o buffer de leitura (ex.: estado inteiro da partida): entregue como um único texto, inteiro.
- Servidor manda frame binário: vira aviso de erro; não derruba o app nem é entregue como texto.
- Fechar durante a abertura, fechar duas vezes, ou fechar depois de o servidor fechar: um único aviso de fechado no total, sem exceção.
- Mandar texto antes de abrir ou depois de fechar: falha observável para quem chamou, sem exceção que escape e sem derrubar a recepção.
- Abrir URL malformada ou com esquema errado: aviso de erro com a URL recebida e a forma esperada.

**HTTP**
- Status 5xx com corpo que não é JSON (ex.: página HTML de proxy): resposta com status e corpo em texto; o transporte não tenta interpretar o corpo.
- Prazo esgotado sem resposta e resposta 504 de um proxy: o primeiro é falha de transporte, o segundo é resposta.
- Host que não resolve ou conexão recusada: falha de transporte, com categoria distinta de prazo esgotado quando a plataforma permitir distinguir.

**Relógio e ciclo de vida**
- Aparelho dorme com o app em segundo plano: a duração fora reflete o tempo real decorrido, inclusive o tempo de sono do aparelho (o token vence em 5 minutos de relógio de parede do servidor).
- Hora do sistema alterada pelo usuário ou por sincronização enquanto o app roda: o relógio monotônico não salta nem retrocede.
- Perda de foco sem ir para segundo plano (diálogo do sistema no Android, outra janela no Windows) e avisos duplicados da plataforma: no máximo um "foi para segundo plano" por "voltou"; sequência nunca começa com "voltou".

**Alcançabilidade**
- Wi-Fi → dados móveis sem passar por "sem rede": um aviso de mudança, com estado anterior e novo.
- Estado lido de novo sem mudança: nenhum aviso.
- Wi-Fi conectado sem internet (portal cativo): reportado como Wi-Fi. Alcançabilidade descreve o tipo de rede, não prova que o servidor responde.

**Entrega na thread principal**
- Item enfileirado pela própria thread principal: entregue no próximo quadro, na mesma ordem dos demais, nunca reentrante.
- Assinante que lança exceção durante a entrega: registrada pelo log, e os itens seguintes do mesmo quadro continuam sendo entregues.
- Hospedeiro destruído com itens pendentes: pendentes descartados, nenhum entregue.

**Codec**
- `payload` ausente: tratado como objeto vazio. `payload` presente que não é objeto: decodificação inválida.
- `type` presente mas não é texto: decodificação inválida.
- Braço conhecido com campo obrigatório ausente ou de tipo errado (ex.: `user_id` como texto): decodificação inválida com o nome do campo — nunca valor desconhecido, nunca valor padrão silencioso.
- Discriminador aninhado (`kind` dentro de um item de lista, como em `deck_problems`): mesma infraestrutura, mesmo tipo de valor desconhecido.
- `message_refused` com `code` que o cliente não conhece: recusa válida com o código preservado; o cliente nunca decide pelo texto de `error`.

**Ambiente**
- Host de desenvolvimento informado com esquema (`http://192.168.0.10:8000/`) ou barra final: normalizado para host e porta, como a configuração já faz hoje.
- Host vazio ou inválido: rejeitado com mensagem que mostra o valor recebido e a forma esperada; o host anterior continua valendo.

## Requirements *(mandatory)*

### Functional Requirements

**Estrutura da camada**

- **FR-001**: A camada MUST ter uma assembly de núcleo sem referência ao motor e uma assembly de adaptadores do motor que depende do núcleo; o núcleo MUST NOT depender dos adaptadores.
- **FR-002**: Cada assembly testada MUST ter a sua assembly de teste EditMode correspondente.
- **FR-003**: Nenhum tipo do núcleo MUST referenciar o motor, a biblioteca de JSON de terceiros nem a implementação concreta de socket; a verificação MUST ser mecânica (configuração de assembly e teste), não revisão manual.

**Porta de socket**

- **FR-004**: A porta de socket MUST permitir abrir uma conexão por URL, mandar texto e fechar de propósito.
- **FR-005**: A porta de socket MUST avisar: conexão aberta; texto recebido, na ordem de chegada; conexão fechada, com código numérico e motivo; e erro.
- **FR-006**: O código de fechamento MUST chegar como o número exato enviado pelo servidor, inclusive os códigos de aplicação 4001, 4400, 4403 e 4404; fechamento sem código do servidor MUST ser distinguível de fechamento com código.
- **FR-007**: Cada conexão MUST produzir no máximo um aviso de fechado, e nenhum aviso de texto ou aberto depois dele.
- **FR-008**: Mandar texto sem conexão aberta MUST falhar de forma observável para quem chamou, sem exceção que escape da porta.
- **FR-009**: Texto fragmentado ou maior que o buffer de leitura MUST ser entregue inteiro, como um único texto.

**Porta de HTTP**

- **FR-010**: A porta de HTTP MUST aceitar método, URL, cabeçalhos e corpo, e devolver status e corpo.
- **FR-011**: Resposta com status 4xx ou 5xx MUST ser um resultado de resposta, nunca exceção.
- **FR-012**: Falha de transporte (sem rede, host não resolvido, conexão recusada, prazo esgotado) MUST ser um resultado distinto de resposta do servidor, com a categoria da falha e sem exceção.
- **FR-013**: Toda requisição MUST ter prazo; o prazo padrão MUST ser definido e documentado, e pode ser trocado por requisição.

**Porta de relógio**

- **FR-014**: A porta de relógio MUST dar o instante monotônico atual; ele MUST NOT retroceder nem ser afetado por mudança da hora do sistema, e MUST NOT expor hora absoluta.

**Porta de ciclo de vida do app**

- **FR-015**: A porta de ciclo de vida MUST avisar "foi para segundo plano" e "voltou ao primeiro plano", e o aviso de volta MUST trazer quanto tempo o app ficou fora.
- **FR-016**: A duração fora MUST incluir o tempo em que o aparelho dormiu; qual fonte de tempo garante isso no Android e no Windows MUST ser investigada e registrada no research.
- **FR-017**: A porta MUST suprimir avisos duplicados da plataforma: no máximo um "foi" por "voltou", e a sequência nunca começa com "voltou".

**Porta de alcançabilidade de rede**

- **FR-018**: A porta de alcançabilidade MUST informar o estado atual entre sem rede, Wi-Fi/rede local e dados móveis.
- **FR-019**: A porta MUST avisar cada mudança de estado com estado anterior e novo, inclusive Wi-Fi → dados móveis sem passar por sem rede, e MUST NOT avisar quando o estado não mudou.

**Porta de log**

- **FR-020**: A porta de log MUST registrar nível, mensagem e campos nomeados; o adaptador real MUST ser o único ponto que escreve no console do motor.

**Fakes**

- **FR-021**: Cada porta MUST ter um fake nomeado, reutilizável entre testes, que permite roteirizar os eventos que a porta produz e inspecionar o que foi pedido a ela (ex.: `FakeWebSocket`, `FakeHttpTransport`, `FakeMonotonicClock`, e equivalentes para ciclo de vida, alcançabilidade e log).
- **FR-022**: Os fakes MUST viver onde as assemblies de teste das features seguintes possam usá-los sem copiá-los.

**Adaptadores reais e entrega na thread principal**

- **FR-023**: Cada porta MUST ter um adaptador real na assembly do motor. O adaptador de socket MUST receber fora da thread principal.
- **FR-024**: Um único ponto da borda MUST receber itens de qualquer thread e entregá-los na thread principal, a cada quadro, na ordem em que foram enfileirados.
- **FR-025**: Depois que o objeto hospedeiro é destruído, nenhum item MUST ser entregue — nem os pendentes, nem os enfileirados depois — e enfileirar MUST NOT lançar exceção.
- **FR-026**: Todo evento produzido pelos adaptadores reais MUST chegar ao assinante na thread principal, passando por esse ponto.
- **FR-027**: Exceção lançada por um assinante durante a entrega MUST ser registrada pela porta de log e MUST NOT impedir a entrega dos itens seguintes.
- **FR-028**: O objeto hospedeiro MUST ser o único componente de cena introduzido por esta feature; o adaptador de ciclo de vida recebe dele os avisos de pausa e foco.

**Codec do protocolo**

- **FR-029**: O codec MUST codificar e decodificar o envelope `{"type": ..., "payload": {...}}`, atrás de uma interface do projeto; os tipos internos da biblioteca de JSON MUST NOT sair dele.
- **FR-030**: O codec MUST oferecer infraestrutura reutilizável de união fechada por campo discriminador (`type`, `kind`, `modifier_kind`, em qualquer nível do payload), em que as features seguintes só registram os braços.
- **FR-031**: Valor de discriminador sem braço registrado MUST virar um tipo explícito de valor desconhecido que preserva o texto do discriminador.
- **FR-032**: Campos a mais em qualquer nível MUST ser ignorados.
- **FR-033**: Texto que não é JSON, JSON que não é objeto, objeto sem `type` ou com `type` que não é texto, `payload` que não é objeto, e braço conhecido com campo obrigatório ausente ou de tipo errado MUST virar resultado de decodificação inválida com o motivo; nenhuma exceção MUST escapar do codec.
- **FR-034**: O codec MUST decodificar `message_refused` com `code`, `error` e os campos extras preservados, legíveis por quem os conhece sem expor tipos internos da biblioteca de JSON. Código de recusa desconhecido MUST ser preservado.
- **FR-035**: O codec MUST decodificar `auth_denied` com `error`.
- **FR-036**: O codec MUST codificar `ping` com payload opcional e decodificar `pong` com o payload ecoado, conforme o contrato 013, de modo que o marcador enviado seja comparável ao recebido.
- **FR-037**: Os tipos decodificados MUST sobreviver à remoção de código não usado do build Android; a garantia MUST ser configurada no projeto e provada pelo quickstart no aparelho.

**Identificadores**

- **FR-038**: `UserId`, `MatchId` e `CardInstanceId` MUST ser tipos distintos; passar um onde se espera outro MUST ser erro de compilação.
- **FR-039**: No JSON, `UserId` e `CardInstanceId` MUST viajar como inteiro cru e `MatchId` como texto cru, nos dois sentidos; valor de tipo errado MUST resultar em decodificação inválida.
- **FR-040**: Identificadores MUST ter igualdade por valor e representação legível no log.

**Ambiente e build**

- **FR-041**: O endereço do servidor MUST vir da configuração existente do app (`AppConfig`), evoluída no lugar; MUST NOT existir um segundo objeto de configuração.
- **FR-042**: A rota `ws/connection/` MUST sair da configuração, porque não existe mais no backend.
- **FR-043**: No build de desenvolvimento, o host MUST poder ser trocado em tempo de execução (ex.: para o IP do computador na rede local), com URLs HTTP e WebSocket derivadas do host novo, sem novo build e sem interface visual nesta feature.
- **FR-044**: No build de produção, a troca de host em tempo de execução MUST NOT ser aceita.
- **FR-045**: `http://` e `ws://` MUST ser liberados só no build de desenvolvimento; no build de produção do Android o tráfego sem TLS MUST ser bloqueado pela plataforma.
- **FR-046**: Iniciar um build de produção com a configuração de TLS desligado MUST falhar na hora do build, antes de gerar o pacote, com mensagem que nomeia o ambiente e diz o que corrigir.
- **FR-047**: Qual camada libera ou bloqueia tráfego sem TLS no Android MUST ser investigada e registrada no research separadamente para o adaptador HTTP e para o adaptador de socket, e o quickstart MUST provar que os dois funcionam no build de desenvolvimento.
- **FR-048**: O código de rede anterior MUST continuar compilando e sem mudança de comportamento, exceto a configuração e o mínimo necessário para remover a rota `ws/connection/` (ver Assumptions).

### Key Entities

- **Envelope**: um frame do protocolo — discriminador `type` e `payload` objeto. Ida e volta.
- **Resultado de decodificação**: ou um valor tipado, ou decodificação inválida com motivo. Nunca exceção.
- **União por discriminador**: família fechada de tipos escolhida por um campo (`type`, `kind`, `modifier_kind`), com braços registrados e um braço explícito de valor desconhecido.
- **Recusa**: `message_refused` — `code` (comparável), `error` (só para leitura humana) e campos extras preservados.
- **Negação de autenticação**: `auth_denied` — `error`; seguida do fechamento 4001.
- **Ping / Pong**: mensagem de batimento do contrato 013; o pong ecoa o payload objeto do ping.
- **Resultado HTTP**: resposta do servidor (status, corpo) ou falha de transporte (categoria). Mutuamente exclusivos.
- **Fechamento de socket**: código numérico ou ausência de código, e motivo.
- **Instante monotônico**: ponto no relógio do aparelho que só avança; diferenças entre instantes são durações.
- **Mudança de ciclo de vida**: foi para segundo plano / voltou, com duração fora.
- **Estado de alcançabilidade**: sem rede, Wi-Fi/rede local, dados móveis; mudança com anterior e novo.
- **Registro de log**: nível, mensagem, campos nomeados.
- **Identificadores**: `UserId`, `MatchId`, `CardInstanceId` — valores tipados, sem conversão implícita entre si.
- **Configuração de ambiente**: host, uso de TLS, rotas HTTP e WebSocket; host trocável só em desenvolvimento.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A suíte EditMode passa 100% pelo comando único da constituição, com o editor fechado, e o código novo não produz nenhum aviso de compilação.
- **SC-002**: Zero tipos do núcleo referenciam o motor, a biblioteca de JSON de terceiros ou a implementação concreta de socket, verificado por teste automatizado.
- **SC-003**: Os 3 testes LiveServer passam contra o backend local subido com `docker compose up`.
- **SC-004**: Em 100% dos eventos observados nos testes LiveServer, o assinante está na thread principal.
- **SC-005**: Com 10.000 itens enfileirados por 4 threads concorrentes, 100% são entregues na thread principal, na ordem de enfileiramento, e 0 são entregues depois da destruição do hospedeiro.
- **SC-006**: 100% dos frames do conjunto de teste de frames inválidos viram decodificação inválida, e 0 exceções escapam do codec.
- **SC-007**: Um build de produção com TLS desligado falha em 100% das tentativas, e a mensagem nomeia o ambiente e a correção.
- **SC-008**: Seguindo só o quickstart, quem desenvolve faz `ping`/`pong` do aparelho Android com o backend do computador na primeira tentativa, sem editar código nem gerar build extra para trocar o IP.
- **SC-009**: Os testes EditMode desta feature, somados, executam em menos de 5 segundos (fora a inicialização do editor), sem espera real de tempo.
- **SC-010**: Nenhum teste de feature seguinte precisa de stub inline para socket, HTTP, relógio, ciclo de vida, alcançabilidade ou log: os fakes desta feature cobrem as seis portas.

## Assumptions

- **Trechos cortados da descrição** foram lidos assim: "É o único Mon…" = o objeto hospedeiro é o único `MonoBehaviour` desta feature (FR-028); "desenvolvime…" = liberado só no build de desenvolvimento (FR-045); "bloqueia t…" = qual camada bloqueia tráfego sem TLS no Android (FR-047); "contra o serv…" = contra o servidor local, exercitando só os adaptadores reais (História 3); a linha solta "log estruturado" = sexta porta do núcleo (FR-020).
- **Rota `ws/connection/` e código antigo**: remover o campo da configuração quebra a compilação de `NetworkBootstrap` (que constrói o `ConnectionClient` com essa rota) e o que o `LoginController` faz com esse cliente. Como a rota não existe no backend, esse caminho já não funciona. Assume-se que o mínimo necessário para compilar sem a rota — tirar a construção e o uso do `ConnectionClient` — é permitido, e o plano registra exatamente o que mudou. Religar o resto fica para a feature 3.
- **Validação de TLS no build** vale para qualquer build de produção (Windows e Android): token em texto puro é problema nos dois. A liberação de tráfego sem TLS pela plataforma é assunto só do Android.
- **Troca de host em desenvolvimento** sem interface visual: exposta como operação da configuração, e o quickstart diz como informar o IP no aparelho (ex.: parâmetro de inicialização). O host informado pode ser lembrado entre execuções do build de desenvolvimento; não é credencial.
- **Alcançabilidade** descreve o tipo de rede disponível, não prova que o servidor responde. Detectar servidor fora do ar é da feature 3 (heartbeat).
- **Recepção binária** não faz parte do protocolo; o servidor só manda texto.
- **Nomes dos fakes** citados em FR-021 seguem a constituição; os das portas de ciclo de vida, alcançabilidade e log são escolhidos no plano.
- **Prazo padrão de HTTP** e categorias de falha de transporte são definidos no plano, a partir do que o transporte da plataforma consegue distinguir.
- **Stack e plataformas** são as da constituição; tecnologias citadas na descrição (transporte de socket do .NET, transporte HTTP do Unity, Newtonsoft, relógio de alta resolução, callbacks de pausa e foco, alcançabilidade do Unity) são restrições já decididas e vão para o plano, não para os requisitos.
- **Dependências**: backend local em `C:/Users/gabri/Projetos/dev_container/anathema/backend` com `docker compose up` para os testes LiveServer e o quickstart; um usuário de teste que consiga fazer login (o mesmo que `scripts/smoke_match.py` usa).
- **Fora do escopo**: login, tokens, renovação e armazenamento seguro (feature 2); reconexão, retry, heartbeat, fila, religar `BaseClient`/`MatchClient`/`MatchmakingClient` nas portas novas e remover a NativeWebSocket (feature 3); formas das mensagens de partida, catálogo e decks (features 2 e 4); qualquer interface visual; iOS, WebGL, macOS e Linux.
