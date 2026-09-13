# Quickstart: Conexão com o servidor

**Feature**: `001-server-connection`

Como provar a feature sem nada visível. Detalhes de forma e comportamento em
[contracts/](./contracts/) e [data-model.md](./data-model.md).

## 0. Pré-requisitos

- Unity 6000.2.8f1 com módulo Android.
- Backend em `C:/Users/gabri/Projetos/dev_container/anathema/backend`.
- Para §4 e §5: aparelho Android com depuração USB, `adb` no `PATH` (vem em
  `Editor/Data/PlaybackEngines/AndroidPlayer/SDK/platform-tools`), e o aparelho
  na mesma rede Wi-Fi do computador.

## 1. Suíte EditMode (sem servidor)

Com o editor **fechado**, na raiz do projeto:

```sh
"C:/Program Files/Unity/Hub/Editor/6000.2.8f1/Editor/Unity.exe" -batchmode -projectPath . -runTests -testPlatform EditMode -testResults Logs/editmode-results.xml
```

Esperado:
- `Logs/editmode-results.xml` com `result="Passed"` e `failed="0"`.
- Nenhum teste `LiveServer` executado (são `[Explicit]`).
- O log do editor não mostra aviso de compilação vindo de `Assets/Scripts/Net/`,
  `Assets/Scripts/Core/Config/` ou `Assets/Tests/EditMode/`.

Cobre: ordem e destruição da fila, codec (envelope, união com desconhecido,
inválidos, eco do pong, recusa com extras), identificadores, HTTP resposta ×
falha, ciclo de vida e troca de rede com fakes, gate de build, fronteira do
núcleo (SC-001, SC-002, SC-005, SC-006, SC-009).

## 2. Testes LiveServer (servidor local)

```sh
cd C:/Users/gabri/Projetos/dev_container/anathema/backend
docker compose up
```

Esperar o uvicorn escutar na 8000. Depois, com o editor fechado, no projeto do
jogo:

```sh
"C:/Program Files/Unity/Hub/Editor/6000.2.8f1/Editor/Unity.exe" -batchmode -projectPath . -runTests -testPlatform EditMode -testCategory LiveServer -testResults Logs/liveserver-results.xml
```

Se o filtro por categoria não incluir testes `[Explicit]` nesta versão do Test
Framework, rodar pelo Test Runner do editor: selecionar os três testes e
**Run Selected**.

Esperado (SC-003, SC-004):
1. `GET /game/cards/` sem token → resposta 401.
2. `ws/matchmaking/` com token inválido → `AuthDeniedFrame`, depois `Closed` com
   código **4001**.
3. Cadastro + `POST /accounts/login/` → `ws/matchmaking/?token=…` → `ping` com
   marcador → `pong` com o mesmo marcador.
4. Nos três, todo aviso observado na thread principal.

## 3. Gate de build de produção

No editor, com `BootstrapScene` no build:

1. No `AppEnvManager`, deixar `isProd` desligado (seleciona `AppConfig_Dev`,
   `useTls=false`).
2. **File → Build Profiles** → Android ou Windows → **Development Build**
   desligado → **Build**.

Esperado: o build falha antes de gerar o pacote, com a mensagem de
[environment-build.md](./contracts/environment-build.md#gate-de-build-de-produção)
citando `AppConfig_Dev` e `useTls=false` (SC-007).

3. Ligar **Development Build** e buildar de novo → o gate não interfere.

## 4. Aparelho Android falando com o computador

1. IP do computador na rede: `ipconfig` → "Endereço IPv4" do adaptador Wi-Fi
   (ex.: `192.168.0.10`).
2. Liberar a porta 8000 no Firewall do Windows para rede privada, se ainda não
   estiver liberada.
3. No computador, conferir que o backend responde pelo IP:
   `curl http://192.168.0.10:8000/game/cards/` → 401.
4. Build de desenvolvimento para Android (IL2CPP, **Development Build** ligado)
   e instalar: **Build And Run**, ou `adb install -r <apk>`.
5. Abrir o app com o host e o probe:

   ```sh
   adb shell am force-stop com.UnityTechnologies.com.unity.template.urpblank
   adb shell am start -n com.UnityTechnologies.com.unity.template.urpblank/com.unity3d.player.UnityPlayerGameActivity -e serverHost 192.168.0.10:8000 -e connectionProbe true
   adb logcat -s Unity
   ```

Esperado no logcat (SC-008), sem editar código nem gerar outro build:

```text
connection_probe_step step=http_unauthorized status=401
connection_probe_step step=auth_denied close_code=4001
connection_probe_step step=ping_pong marker_matched=true
connection_probe_passed host=192.168.0.10:8000
```

Diagnóstico:
- `TransportFailure kind=CleartextRefused` ou "Cleartext HTTP traffic not
  permitted": o build não saiu como desenvolvimento, ou o manifesto não recebeu
  `usesCleartextTraffic` (research R3).
- `kind=CannotConnect`/`Timeout`: firewall, IP errado ou outra rede.
- Frame `UnknownServerFrame` onde se esperava `pong`/`auth_denied`: stripping
  removeu algo do codec (research R7).

## 5. Tempo fora com o aparelho dormindo

Com o app do §4 aberto:

1. Bloquear a tela e esperar **6 minutos**.
2. Desbloquear e voltar ao app.

Esperado no logcat:

```text
app_background
app_foreground away_ms=<≥ 360000>
```

`away_ms` muito menor que 360000 quer dizer que o relógio não contou o sono do
aparelho (research R1).

## 6. Troca de rede

Com o app aberto, desligar o Wi-Fi com os dados móveis ligados.

Esperado: `network_kind_changed previous=LocalArea current=CarrierData`, uma vez.
Pode aparecer `None` entre os dois se o aparelho passar por "sem rede". Não pode
haver aviso repetido com o mesmo estado.
