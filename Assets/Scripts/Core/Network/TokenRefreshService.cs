using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public enum TokenRefreshResult
{
    /// <summary>Token novo gravado na sessao.</summary>
    Success,

    /// <summary>Refresh recusado: a sessao morreu, o jogador precisa logar de novo.</summary>
    Expired,

    /// <summary>Servidor fora do ar ou sem rede. Vale tentar de novo depois.</summary>
    NetworkError,
}

/// <summary>
/// Troca o refresh token por um access token novo em /accounts/token/refresh/.
///
/// O access token do SimpleJWT dura 5 minutos por padrao, e o middleware de
/// WebSocket valida o token so no handshake. Um socket aberto sobrevive, mas
/// qualquer reconexao depois desses 5 minutos leva close 4001 sem isto aqui.
/// </summary>
public class TokenRefreshService : MonoBehaviour
{
    private static TokenRefreshService instance;

    /// <summary>
    /// Cria o servico sob demanda se ele nao estiver em cena.
    ///
    /// Diferente dos outros singletons do projeto de proposito: quem chama
    /// isto e o caminho de reconexao, que so roda quando algo ja deu errado.
    /// Depender de alguem lembrar de arrastar o componente para a
    /// BootstrapScene daria NullReferenceException no pior momento possivel.
    /// </summary>
    public static TokenRefreshService Instance
    {
        get
        {
            if (instance != null)
                return instance;

            var host = new GameObject(nameof(TokenRefreshService));
            DontDestroyOnLoad(host);

            // AddComponent roda o Awake na hora, que ja preenche `instance`.
            host.AddComponent<TokenRefreshService>();

            return instance;
        }
    }

    // Os tres clients podem tomar 4001 no mesmo frame. Sem isto, cada um
    // dispara um refresh e as respostas se sobrescrevem.
    private readonly List<Action<TokenRefreshResult>> waiting = new();
    private bool inFlight;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// Pede um access token novo. Chamadas concorrentes compartilham a mesma
    /// requisicao: todos os callbacks recebem o mesmo resultado.
    /// </summary>
    public void Refresh(Action<TokenRefreshResult> onDone)
    {
        if (onDone != null)
            waiting.Add(onDone);

        if (inFlight)
            return;

        inFlight = true;
        StartCoroutine(SendRefreshRequest());
    }

    private IEnumerator SendRefreshRequest()
    {
        var refreshToken = PlayerSession.Instance.RefreshToken;

        if (string.IsNullOrEmpty(refreshToken))
        {
            Debug.LogWarning("TokenRefreshService: sem refresh token guardado na sessao.");
            Finish(TokenRefreshResult.Expired);
            yield break;
        }

        var body = JsonUtility.ToJson(new TokenRefreshRequestDTO { refresh = refreshToken });

        using var request = new UnityWebRequest(BuildRefreshUrl(), UnityWebRequest.kHttpVerbPOST)
        {
            uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body)),
            downloadHandler = new DownloadHandlerBuffer(),
        };

        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        // 401 e a unica resposta que mata a sessao: o refresh token expirou ou
        // foi revogado. Qualquer outra falha pode ser rede, e reautenticar o
        // jogador por causa de wifi ruim seria pior que tentar de novo.
        if (request.responseCode == 401)
        {
            Debug.LogWarning("TokenRefreshService: refresh recusado (401). Sessao expirada.");
            Finish(TokenRefreshResult.Expired);
            yield break;
        }

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogWarning($"TokenRefreshService: falha ao renovar token: {request.error}");
            Finish(TokenRefreshResult.NetworkError);
            yield break;
        }

        var response = JsonUtility.FromJson<TokenRefreshResponseDTO>(request.downloadHandler.text);

        if (response == null || string.IsNullOrEmpty(response.access))
        {
            Debug.LogError(
                $"TokenRefreshService: resposta sem 'access': {request.downloadHandler.text}");
            Finish(TokenRefreshResult.NetworkError);
            yield break;
        }

        PlayerSession.Instance.SetAccessToken(response.access);
        Finish(TokenRefreshResult.Success);
    }

    private void Finish(TokenRefreshResult result)
    {
        inFlight = false;

        // Copia antes de invocar: um callback pode chamar Refresh de novo.
        var callbacks = waiting.ToArray();
        waiting.Clear();

        foreach (var callback in callbacks)
            callback(result);
    }

    private string BuildRefreshUrl()
    {
        return AppEnvManager.Settings.HttpUrl(AppEnvManager.Settings.tokenRefreshEndpoint);
    }
}
