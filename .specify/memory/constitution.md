<!--
Sync Impact Report — 1.0.0 (2026-09-13)
- Version change: (uninitialized template) → 1.0.0
- Bump rationale: primeira ratificação. Todo placeholder substituído por regras
  concretas derivadas do CLAUDE.md deste repositório, da constituição do backend
  (1.1.0) adaptada para C# e Unity, e das decisões tomadas para a camada de rede
  do cliente.
- Modified principles: none (adoção inicial). Placeholders resolvidos para:
  [PRINCIPLE_1_NAME] → I. Notas De Decisão E Contratos Do Servidor São A Fonte Da Verdade
  [PRINCIPLE_2_NAME] → II. O Servidor É A Autoridade
  [PRINCIPLE_3_NAME] → III. Identidade Nomeada E Tipada, Nunca Um `id` Solto
  [PRINCIPLE_4_NAME] → IV. Tipos Explícitos, Verificados Pelo Compilador
  [PRINCIPLE_5_NAME] → V. Unidades Pequenas, Uma Responsabilidade Cada
  (acrescentados)    → VI. Núcleo Independente Do Unity
  (acrescentados)    → VII. Comportamento Testado Com Fakes Nomeados
  [SECTION_2_NAME]   → Technology And Platform Constraints
  [SECTION_3_NAME]   → Development Workflow And Quality Gates
- Added sections: Core Principles (7), Technology And Platform Constraints,
  Development Workflow And Quality Gates, Governance.
- Removed sections: none.
- Templates requiring updates:
  ✅ .specify/templates/tasks-template.md — testes deixaram de ser opcionais
     (princípio VII).
  ✅ .specify/templates/plan-template.md — "Constitution Check" é genérico e
     resolve contra este arquivo; nenhuma edição necessária.
  ✅ .specify/templates/spec-template.md — nenhuma seção depende da constituição.
  ✅ .specify/templates/checklist-template.md — nenhuma referência à constituição.
  ✅ CLAUDE.md — esta constituição reafirma as regras dele; nenhuma divergência.
  ⚠ .specify/templates/commands/*.md — não existe nesta instalação (skills em
     .claude/skills); nada a verificar.
- Follow-up TODOs: none.
-->

# Anathema Game Client Constitution

## Core Principles

### I. Notas De Decisão E Contratos Do Servidor São A Fonte Da Verdade

Decisões de produto e de domínio moram no vault do Obsidian em
`C:/Users/gabri/Obsidian/Projetos/Anathema/`, não neste repositório. `Decisões/`
MUST ser lida antes de escrever código de domínio, e `Game/Fluxo de Partida.md`
antes de qualquer código que dependa do que uma fase da partida espera do
jogador. Quando o código e uma nota de decisão discordam, o código está errado
e MUST ser corrigido — nunca a nota. Nota de decisão nova só quando o mantenedor
pedir; notas em português do Brasil, curtas.

O protocolo que o cliente fala é definido pelos contratos do backend
(`C:/Users/gabri/Projetos/dev_container/anathema/backend/specs/*/contracts/`), e
`scripts/smoke_match.py` de lá é a referência executável de um cliente que
funciona. Quando o cliente e um contrato discordam, o cliente MUST ser
corrigido. Um contrato que parece errado vira pedido ao backend, nunca
contorno no cliente.

Rationale: o cliente é o segundo leitor de um protocolo que já foi verificado
de ponta a ponta; reinterpretá-lo aqui cria dois protocolos.

### II. O Servidor É A Autoridade

O cliente MUST NOT calcular regra de jogo: não decide se uma jogada é legal, não
aplica dano, não avança fase, não expira a vez. Ele:

1. mantém um espelho do estado que o servidor manda, substituindo-o a cada
   frame aceito, sem mesclar;
2. expõe esse estado e os eventos de cada mudança para a apresentação;
3. oferece comandos que viram mensagens para o servidor;
4. repassa as recusas do servidor.

Lógica local é permitida só como conveniência de interface — por exemplo, ler no
catálogo se um feitiço pede alvo antes de mandar a jogada — e MUST NOT impedir o
envio de uma jogada que o servidor possa aceitar. Regras de protocolo em vigor:

- Recusa é comparada pelo `code`, nunca pelo texto de `error`.
- `match_update` com `version` menor ou igual à última aplicada é descartado;
  versões não são contínuas.
- O relógio da vez conta a partir de `remaining_ms` e do instante em que o frame
  chegou, no relógio monotônico do aparelho, nunca por hora absoluta.

Rationale: um cliente que decide regra diverge do servidor em silêncio, e a
divergência só aparece como bug de partida impossível de reproduzir.

### III. Identidade Nomeada E Tipada, Nunca Um `id` Solto

Nenhum DTO, mensagem, evento ou assinatura pública MAY expor campo ou parâmetro
chamado só `id`. O espaço de identidade MUST ser nomeado: `user_id`, `match_id`,
`deck_id`, `card_id`, `card_instance_id`.

Identificadores MUST ter tipo próprio no C# (`UserId`, `MatchId`, `DeckId`,
`CardId`, `CardInstanceId`), de modo que passar um no lugar de outro seja erro
de compilação. Toda jogada cita `CardInstanceId`; `CardId` só serve para
consultar o catálogo.

Rationale: trocar `card_id` por `card_instance_id` é o erro mais provável do
protocolo, e com dois `int` o compilador não tem como pegar.

### IV. Tipos Explícitos, Verificados Pelo Compilador

- Todo arquivo novo MUST declarar `#nullable enable`.
- `dynamic` é proibido. `object`, `JObject` e `JToken` MUST NOT sair do codec de
  JSON: o resto do código só vê tipos do projeto.
- `var` só quando o tipo aparece no lado direito da atribuição.
- Payload cuja forma depende de um campo discriminador (`type`, `kind`,
  `modifier_kind`) MUST virar uma hierarquia fechada de tipos, com um tipo
  explícito para o valor desconhecido, nunca um dicionário solto.
- Código novo MUST compilar sem aviso produzido por ele mesmo.

Rationale: o protocolo inteiro é etiquetado por discriminador; sem tipos
fechados, cada consumidor reimplementa o `switch` e esquece um braço.

### V. Unidades Pequenas, Uma Responsabilidade Cada

- Métodos: 4-20 linhas. Mais longo MUST ser dividido.
- Arquivos: menos de 500 linhas, um tipo público por arquivo.
- Uma coisa por método, uma responsabilidade por classe.
- Early return em vez de condicional aninhada; no máximo 2 níveis de
  indentação.
- Sem lógica duplicada: comportamento compartilhado MUST ser extraído.
- Nomes MUST ser específicos e únicos. `data`, `handler`, `Manager` e `Helper`
  são proibidos em código novo; preferir nomes com menos de 5 ocorrências no
  projeto.
- Mensagem de exceção MUST incluir o valor recebido e a forma esperada.

Rationale: são os limites que o backend já cumpre; como número, a violação vira
revisão objetiva em vez de discussão.

### VI. Núcleo Independente Do Unity

- A camada de rede é dividida em assemblies por responsabilidade: transporte
  (socket e HTTP), forma das mensagens, estado espelhado da partida,
  orquestração da sessão e a superfície que a apresentação consome.
- As assemblies do núcleo MUST usar `noEngineReferences: true`: não conhecem
  `MonoBehaviour`, cena, `Debug`, `PlayerPrefs` nem objeto visual.
- `MonoBehaviour` só na borda: adaptadores do Unity e o objeto que hospeda a
  camada. A apresentação assina eventos e chama comandos; o núcleo MUST NOT
  conhecer a apresentação.
- Dependências MUST entrar pelo construtor. Singleton `Instance`,
  `FindObjectOfType` e estado estático mutável são proibidos no núcleo.
- Socket, HTTP, relógio, armazenamento seguro, ciclo de vida do app e
  alcançabilidade de rede MUST entrar por interface do projeto.
- Bibliotecas de terceiros (Newtonsoft, `ClientWebSocket`, `UnityWebRequest`)
  MUST ficar atrás de uma interface fina do projeto, e não vazam para o núcleo.
- Todo evento entregue à apresentação MUST chegar na thread principal do Unity.
  A troca de thread acontece em um único ponto da borda.

Rationale: é essa fronteira que permite construir a camada inteira sem nada
visível, e ligar a parte visual depois sem reescrever regra, mensagem ou estado.

### VII. Comportamento Testado Com Fakes Nomeados

- Testes com Unity Test Framework, em **EditMode**, sem cena e sem servidor.
- A suíte inteira MUST rodar com um comando, com o editor fechado:
  `"C:/Program Files/Unity/Hub/Editor/6000.2.8f1/Editor/Unity.exe" -batchmode
  -projectPath . -runTests -testPlatform EditMode -testResults
  Logs/editmode-results.xml`.
- Todo método novo ganha teste. Toda correção de bug ganha teste de regressão
  que falha antes da correção.
- I/O externo MUST ser substituído por classe fake com nome (`FakeWebSocket`,
  `FakeHttpTransport`, `FakeMonotonicClock`), nunca por stub inline ou lambda
  improvisada.
- Testes MUST ser F.I.R.S.T.: rápidos, independentes, repetíveis,
  autoverificáveis, oportunos. Tempo é injetado, nunca esperado.
- Teste contra o servidor local real MUST levar `[Explicit]` e
  `[Category("LiveServer")]`, e fica fora da rodada normal.
- Testes em `Assets/Tests/EditMode/`, uma asmdef de teste por asmdef testada.

Rationale: fakes nomeados são encontráveis e reutilizáveis; stubs inline se
afastam em silêncio da interface que imitam.

## Technology And Platform Constraints

A stack é fixada e MUST NOT mudar sem emenda a esta constituição:

- Unity 6000.2.8f1 (C# 9)
- Universal Render Pipeline 17.2
- Input System 1.14.2
- Newtonsoft JSON (`com.unity.nuget.newtonsoft-json`) 3.2.1
- Unity Test Framework 1.6.0
- Multiplayer Play Mode 1.6.3, só para desenvolvimento

Plataformas alvo, e só elas:

- **Windows desktop**
- **Android**

Não há build para iOS, macOS, Linux nem WebGL. Código, configuração de build,
pós-processador ou `#if` para essas plataformas MUST NOT ser escritos.

Regras que decorrem das plataformas:

- O transporte usa `ClientWebSocket` do .NET e `UnityWebRequest`.
- No Android, reconexão é o caminho normal. A camada de rede MUST perceber a
  volta ao primeiro plano e reconectar sozinha, renovando o token de acesso
  antes quando ele venceu ou está perto de vencer, sem a apresentação pedir.
- Voltar ao primeiro plano MUST zerar a detecção de silêncio da conexão antes
  de avaliá-la: o tempo pausado não é silêncio do servidor.
- O endereço do servidor MUST ser configurável; nenhum host fixo no código.
- Tráfego sem TLS (`http://`, `ws://`) MUST ser liberado só no build de
  desenvolvimento do Android, nunca no de produção.
- O refresh token MUST ficar em armazenamento protegido (Android Keystore,
  DPAPI no Windows). `PlayerPrefs` é texto puro e MUST NOT guardar credencial.
- JSON MUST passar pelo codec do projeto sobre Newtonsoft. `JsonUtility` MUST
  NOT ser usado para mensagens do protocolo.

Regras estruturais:

- Uma asmdef por camada, com dependências só no sentido da borda para o núcleo.
- Todo arquivo em `Assets/` MUST ir para o commit junto com o seu `.meta`.
- Formatação é do formatador padrão de C# da IDE (Rider/Visual Studio). Estilo
  além disso não é assunto de revisão.
- Log de depuração passa pela interface de log do projeto, com campos
  estruturados. `Debug.Log` só dentro do adaptador que a implementa. Texto
  simples só no que o jogador lê.

## Development Workflow And Quality Gates

Antes de uma mudança ser considerada pronta:

1. O projeto compila no editor sem erro e sem aviso novo.
2. A suíte EditMode passa pelo comando do princípio VII.
3. Arquivos novos em `Assets/` estão acompanhados do `.meta`.

Disciplina de comentários faz parte da revisão:

- Comentários existentes MUST ser preservados numa refatoração — carregam
  intenção e origem.
- Comentário explica o PORQUÊ, não o QUÊ.
- Membro público leva `/// <summary>` com a intenção e um `<example>` de uso.
- Linha que existe por causa de um bug específico ou de uma restrição externa
  MUST citar o commit ou a issue.

Código anterior a esta constituição:

- O código de rede existente (`BaseClient`, `MatchClient`, `MatchmakingClient`,
  `TokenRefreshService`, `PlayerSession`, `MatchSession`, `NetworkBootstrap`) é
  **reaproveitado e evolui no lugar**; não é substituído por uma camada paralela.
- Ele MUST NOT ser reescrito de passagem em trabalho não relacionado. A feature
  que toca um desses arquivos MUST deixá-lo conforme esta constituição naquilo
  que tocou, e registrar no plano o que ficou para depois.

Trabalho adiado de propósito é registrado em `Game/TODO.md` no vault, com
diagnóstico e caminho. Um item ali é decisão, não bug desconhecido, e MUST NOT
ser "consertado" de passagem.

## Governance

Esta constituição prevalece sobre qualquer outra prática, hábito ou convenção
deste repositório. Onde `CLAUDE.md` e este documento se sobrepõem, eles MUST
concordar; se divergirem, vale este documento e o `CLAUDE.md` é corrigido.

Emendas:

- Toda mudança neste arquivo MUST ser uma edição deliberada e isolada, que diz
  no commit o que mudou e por quê.
- A versão segue versionamento semântico:
  - MAJOR — um princípio é removido ou redefinido de forma incompatível, ou uma
    regra de governança é retirada.
  - MINOR — um princípio ou seção é acrescentado, ou uma orientação existente é
    ampliada de forma material.
  - PATCH — esclarecimento, redação e correção de digitação que não mudam regra.
- Mudança na stack fixada ou nas plataformas alvo é no mínimo MINOR e MUST ser
  registrada aqui antes de o pacote ou o build mudar.

Conformidade:

- Toda revisão verifica as portas de qualidade acima e os princípios.
- Complexidade que viola um princípio MUST ser justificada por escrito no ponto
  da exceção (na tabela "Complexity Tracking" do plano), ou removida.
- `CLAUDE.md` continua sendo a orientação de desenvolvimento para agentes; o
  vault do Obsidian continua sendo a fonte da verdade das decisões de domínio.

**Version**: 1.0.0 | **Ratified**: 2026-09-13 | **Last Amended**: 2026-09-13
