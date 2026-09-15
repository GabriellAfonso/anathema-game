#nullable enable
using Anathema.Client.Scenes;
using Anathema.Net.Connection;
using Anathema.Net.Core;
using Anathema.Net.Facade;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Tela do pareamento: mostra os dois jogadores do estado pareado da fachada. A cena da partida vem pelo roteador
/// quando o primeiro estado chega (specs/005-presentation-facade/contracts/presentation-surface.md).
/// </summary>
/// <example><code>// Componente da VersusScene; o hospedeiro chama BindClient.</code></example>
public class VersusController : MonoBehaviour, ISceneClientConsumer
{
    [Header("UI")]
    [SerializeField] private TMP_Text playerNickname = null!;
    [SerializeField] private Image playerIconImage = null!;
    [SerializeField] private TMP_Text opponentNickname = null!;
    [SerializeField] private Image opponentIconImage = null!;

    [SerializeField] private string defaultIconName = "DefaultIcon";

    private IClientLog? log;

    /// <summary>Recebe a fachada e desenha o pareamento.</summary>
    /// <example><code>versus.BindClient(client);</code></example>
    public void BindClient(AnathemaClient client)
    {
        log = client.Log;
        MatchPairing? pairing = client.State.Pairing;
        if (pairing == null)
        {
            client.Log.Warning("versus_without_pairing", new LogField("stage", client.State.Stage.ToString()));
            return;
        }

        playerNickname.text = pairing.Self.Nickname;
        SetIconPlayer(pairing.Self.Icon);

        opponentNickname.text = pairing.Opponent.Nickname;
        SetIconOpponent(pairing.Opponent.Icon);
    }

    /// <summary>Troca o ícone do próprio jogador.</summary>
    /// <example><code>versus.SetIconPlayer("knight");</code></example>
    public void SetIconPlayer(string iconName) => SetIcon(playerIconImage, iconName);

    /// <summary>Troca o ícone do oponente.</summary>
    /// <example><code>versus.SetIconOpponent("knight");</code></example>
    public void SetIconOpponent(string iconName) => SetIcon(opponentIconImage, iconName);

    private void SetIcon(Image target, string iconName)
    {
        Sprite icon = Resources.Load<Sprite>($"Players/Icons/{iconName}");

        if (icon == null)
        {
            log?.Warning("player_icon_missing", new LogField("icon", iconName), new LogField("fallback", defaultIconName));
            icon = Resources.Load<Sprite>($"Players/Icons/{defaultIconName}");
        }

        target.sprite = icon;
    }
}
