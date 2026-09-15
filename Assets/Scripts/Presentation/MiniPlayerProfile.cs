#nullable enable
using System;
using System.Threading.Tasks;
using Anathema.Client.Scenes;
using Anathema.Net.Account;
using Anathema.Net.Core;
using Anathema.Net.Facade;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Mini perfil da Home: apelido, nível, moedas e ícone lidos pela conta da fachada quando a Home recebe a fachada.
/// Antes o login esperava o perfil para a Home não abrir vazia; agora a própria tela lê e preenche quando chega
/// (specs/005-presentation-facade/contracts/composition-and-scenes.md, "Destino do código antigo").
/// </summary>
/// <example><code>// Componente da HomeScene; o hospedeiro chama BindClient.</code></example>
public class MiniPlayerProfile : MonoBehaviour, ISceneClientConsumer
{
    [Header("UI")]
    [SerializeField] private TMP_Text nickname = null!;
    [SerializeField] private Image iconImage = null!;
    [SerializeField] private TMP_Text levelText = null!;
    [SerializeField] private TMP_Text coinsText = null!;

    [SerializeField] private string defaultIconName = "DefaultIcon";

    private IClientLog? log;

    /// <summary>Recebe a fachada, limpa os campos e lê o perfil.</summary>
    /// <example><code>profile.BindClient(client);</code></example>
    public void BindClient(AnathemaClient client)
    {
        log = client.Log;
        ShowEmpty();
        _ = LoadProfileAsync(client);
    }

    /// <summary>Troca o ícone pelo nome do recurso, com o padrão quando não existe.</summary>
    /// <example><code>profile.SetIcon("knight");</code></example>
    public void SetIcon(string iconName)
    {
        Sprite icon = Resources.Load<Sprite>($"Players/Icons/{iconName}");

        if (icon == null)
        {
            log?.Warning("player_icon_missing", new LogField("icon", iconName), new LogField("fallback", defaultIconName));
            icon = Resources.Load<Sprite>($"Players/Icons/{defaultIconName}");
        }

        iconImage.sprite = icon;
    }

    private async Task LoadProfileAsync(AnathemaClient client)
    {
        try
        {
            AccountCallOutcome<OwnProfile, ProfileRefusal> profile = await client.Account.ReadProfileAsync();
            // A cena pode ter sido destruída enquanto o perfil chegava.
            if (this == null)
                return;

            if (profile.IsSuccess)
                Show(profile.Value);
            else
                client.Log.Warning("own_profile_unavailable", new LogField("refusal", profile.Refusal?.ToString() ?? "none"), new LogField("failure", profile.Failure?.ToString() ?? "none"));
        }
        catch (Exception unexpected)
        {
            client.Log.Error("own_profile_failed", new LogField("error", unexpected.GetType().Name));
        }
    }

    private void Show(OwnProfile profile)
    {
        nickname.text = profile.Nickname;
        levelText.text = $"Lv: {profile.Level}";
        coinsText.text = profile.Coins.ToString();
        SetIcon(profile.Icon);
    }

    private void ShowEmpty()
    {
        nickname.text = "";
        levelText.text = "";
        coinsText.text = "";
        SetIcon(defaultIconName);
    }
}
