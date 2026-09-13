# Stack
- Unity 6000.2.8f1 (C# 9)
- Universal Render Pipeline 17.2
- Input System 1.14.2
- Newtonsoft JSON (`com.unity.nuget.newtonsoft-json`) 3.2.1
- Unity Test Framework 1.6.0 (NUnit)
- Multiplayer Play Mode 1.6.3 (dois jogadores no editor, só em desenvolvimento)

Pacotes declarados em `Packages/manifest.json`. Pacote novo só com motivo
escrito no commit que o adiciona.


# Plataformas alvo

O jogo é feito **apenas** para:

- **Desktop Windows**
- **Celular Android**

**Não** há build para iOS, macOS, Linux nem WebGL. Não escreva código,
configuração de build, pós-processador ou `#if` para essas plataformas.

Consequências que valem para todo o código:

- O transporte de rede pode usar `ClientWebSocket` do .NET e `UnityWebRequest`.
- No Android, reconectar é o caminho normal: minimizar o app, bloquear a tela ou
  trocar de Wi-Fi para dados móveis derruba o socket. A camada de rede percebe a
  volta ao primeiro plano e reconecta sozinha, renovando o token de acesso antes
  (ele dura 5 minutos e vence com o app minimizado).
- O endereço do servidor é configurável, nunca fixo no código: no celular,
  `localhost` é o próprio aparelho.
- Tráfego sem TLS (`http://`, `ws://`) é liberado no Android só no build de
  desenvolvimento, nunca no de produção.
- Eventos entregues à parte visual chegam na thread principal do Unity; a
  recepção do socket roda fora dela.


# Base de conhecimento

Decisões de produto e design moram no vault do Obsidian, não neste repositório:

    C:/Users/gabri/Obsidian/Projetos/Anathema/

- `Decisões/` — ler antes de escrever código de domínio. Se o código discorda
  de uma nota de decisão, o código está errado.
- `Game/Fluxo de Partida.md` — as regras da partida. O cliente não as
  implementa, mas precisa entendê-las para saber o que cada fase espera.

Notas em português do Brasil, curtas. Nota de decisão nova só quando pedirem.

O backend fica em `C:/Users/gabri/Projetos/dev_container/anathema/backend`. O
protocolo que o cliente fala está nos contratos de lá:

- `specs/009-match-protocol/contracts/` — socket de partida e códigos de recusa
- `specs/010-match-timers/contracts/` — relógio da vez
- `specs/011-deck-catalog-api/contracts/` — catálogo, decks e matchmaking
- `specs/012-match-result-history/contracts/http_match_history.md` — histórico
- `scripts/smoke_match.py` — um cliente que funciona, de ponta a ponta

Quando o cliente e um contrato discordam, o cliente está errado.


# O servidor é a autoridade

O cliente não calcula regra nenhuma: não decide se uma jogada é legal, não
aplica dano, não avança fase. Ele espelha o estado que o servidor manda, expõe
esse estado e seus eventos para a apresentação, transforma comandos em
mensagens e repassa as recusas.

Lógica local só para conveniência de interface (ex.: ler no catálogo se um
feitiço pede alvo antes de mandar a jogada). Mesmo aí, quem decide é o servidor.

Decisões em vigor:
- Nunca expor campo chamado só `id`. Nomear o espaço: `user_id`, `match_id`,
  `deck_id`, `card_id`, `card_instance_id`.
- Toda jogada cita `card_instance_id` (a cópia na partida). `card_id` só serve
  para buscar dados no catálogo.
- Recusa é comparada pelo `code`, nunca pelo texto de `error`.
- `match_update` com `version` menor ou igual à última aplicada é descartado.
- Relógio conta a partir de `remaining_ms` e do instante em que o frame chegou,
  no relógio monotônico do aparelho. Nunca por hora absoluta.
- JSON com Newtonsoft, atrás de um codec do projeto. `JsonUtility` não lê
  dicionário nem payload cujo formato depende de `type`/`kind`.
- O refresh token é guardado no aparelho em armazenamento protegido (Android
  Keystore, DPAPI no Windows). Nunca em `PlayerPrefs`, que é texto puro.


## Estilo de código

- Métodos: 4-20 linhas. Mais que isso, dividir.
- Arquivos: menos de 500 linhas. Um tipo público por arquivo.
- Uma coisa por método, uma responsabilidade por classe (SRP).
- Nomes específicos e únicos. Evitar `data`, `handler`, `Manager`, `Helper`.
  Preferir nomes com menos de 5 ocorrências no projeto.
- Tipos explícitos. `#nullable enable` em código novo. Nada de `dynamic`, e
  `object`/`JObject`/`JToken` não saem do codec de JSON. `var` só quando o tipo
  aparece do lado direito da atribuição.
- Identificadores com tipo próprio (`CardInstanceId`, `CardId`, `UserId`): trocar
  um pelo outro precisa ser erro de compilação.
- Sem código duplicado. Extrair lógica compartilhada.
- Early return em vez de `if` aninhado. No máximo 2 níveis de indentação.
- Mensagem de exceção inclui o valor recebido e a forma esperada.

## Comentários

- Manter os comentários existentes. Não apagar em refatoração — carregam
  intenção e origem.
- Escrever o PORQUÊ, não o QUÊ.
- Membro público leva `/// <summary>` com a intenção e um `<example>` de uso.
- Linha que existe por causa de um bug ou restrição externa cita o commit ou a
  issue.

## Testes

- Unity Test Framework, em **EditMode**. Nada de cena nem de servidor nos
  testes normais.
- Todos rodam com um comando só, com o editor fechado:

      "C:/Program Files/Unity/Hub/Editor/6000.2.8f1/Editor/Unity.exe" -batchmode -projectPath . -runTests -testPlatform EditMode -testResults Logs/editmode-results.xml

- Todo método novo ganha teste. Correção de bug ganha teste de regressão que
  falha antes da correção.
- I/O externo (socket, HTTP, relógio, armazenamento, ciclo de vida do app)
  entra por interface e é substituído por fake com nome (`FakeWebSocket`,
  `FakeHttpTransport`, `FakeMonotonicClock`), nunca por stub inline.
- F.I.R.S.T: rápidos, independentes, repetíveis, autoverificáveis, oportunos.
- Teste contra o servidor local real leva `[Explicit]` e
  `[Category("LiveServer")]`, e fica fora da rodada normal.
- Testes em `Assets/Tests/EditMode/`, uma asmdef de teste por asmdef testada.

## Dependências

- Injetar pelo construtor. Nada de singleton `Instance` nem `FindObjectOfType`
  no núcleo.
- Bibliotecas de terceiros (Newtonsoft, `ClientWebSocket`, `UnityWebRequest`)
  atrás de uma interface fina do projeto.

## Estrutura

- Uma asmdef por camada. O núcleo de rede usa `noEngineReferences: true`: não
  conhece `MonoBehaviour`, cena, `Debug` nem objeto visual.
- `MonoBehaviour` só na borda: adaptadores do Unity e o objeto que hospeda a
  camada. A apresentação assina eventos e chama comandos; nunca o contrário.
- Todo arquivo em `Assets/` vai para o commit junto com o `.meta` dele.

## Formatação

- Formatador padrão de C# da IDE (Rider/Visual Studio). Estilo além disso não
  é assunto de revisão.

## Logging

- Pelo `IClientLog` do projeto, com campos estruturados, para depuração.
  `Debug.Log` só dentro do adaptador que implementa essa interface.
- Texto simples só no que o jogador lê.

<!-- SPECKIT START -->
For additional context about technologies to be used, project structure,
shell commands, and other important information, read the current plan
<!-- SPECKIT END -->
