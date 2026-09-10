using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class SelfProfileService : MonoBehaviour
{
    public static SelfProfileService Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void LoadProfile(string token)
    {
        StartCoroutine(FetchSelfProfile(token));
    }

    private IEnumerator FetchSelfProfile(string token)
    {
        var url = BuildFetchSelfProfileUrl();
        var request = UnityWebRequest.Get(url);

        request.SetRequestHeader("Authorization", $"Bearer {token}");
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"Erro ao carregar perfil: {request.error}");
            yield break;
        }

        var json = request.downloadHandler.text;
        var profile = JsonUtility.FromJson<SelfPlayerProfileDTO>(json);

        PlayerSession.Instance.SetProfile(profile);
    }
    private string BuildFetchSelfProfileUrl()
    {
        return AppEnvManager.Settings.HttpUrl(AppEnvManager.Settings.playerMe);
    }
}
