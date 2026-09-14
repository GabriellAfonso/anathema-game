#nullable enable
using System.Threading.Tasks;
using Anathema.Net.Account;
using Anathema.Net.Core;
using UnityEngine;

/// <summary>
/// Lê o próprio perfil pela sessão de conta e grava na PlayerSession, para a Home mostrar.
/// Delega para o OwnProfileQuery (specs/002-player-account); fica como componente da
/// BootstrapScene até existir tela de perfil que leia direto.
/// </summary>
/// <example><code>bool loaded = await SelfProfileService.Instance.LoadProfileAsync();</code></example>
public class SelfProfileService : MonoBehaviour
{
    /// <summary>O serviço da BootstrapScene.</summary>
    /// <example><code>await SelfProfileService.Instance.LoadProfileAsync();</code></example>
    public static SelfProfileService Instance { get; private set; } = null!;

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

    /// <summary>Lê o perfil e grava na sessão; devolve se conseguiu. Falha fica registrada.</summary>
    /// <example><code>bool loaded = await SelfProfileService.Instance.LoadProfileAsync();</code></example>
    public async Task<bool> LoadProfileAsync()
    {
        PlayerSession session = PlayerSession.Instance;
        AccountCallOutcome<OwnProfile, ProfileRefusal> profile = await session.Account.Profile.ReadAsync();
        if (profile.IsSuccess)
        {
            session.SetProfile(profile.Value);
            return true;
        }

        session.Log.Warning("own_profile_unavailable", new LogField("refusal", profile.Refusal?.ToString() ?? "none"), new LogField("failure", profile.Failure?.ToString() ?? "none"));
        return false;
    }
}
