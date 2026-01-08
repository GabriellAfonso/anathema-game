using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class VersusController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text playerNickname;
    [SerializeField] private Image playerIconImage;
    [SerializeField] private TMP_Text opponentNickname;
    [SerializeField] private Image opponentIconImage;
  

    [SerializeField] private string defaultIconName = "DefaultIcon";

    private void Awake()
    {
     

        if (VersusContext.Instance != null)
        {
            var player = VersusContext.Instance.Player;
            var opponent = VersusContext.Instance.Opponent;

            playerNickname.text = player.nickname;
            SetIconPlayer(player.icon);

            opponentNickname.text = opponent.nickname;
            SetIconOpponent(opponent.icon);
        }
        else
        {
            //Debug.LogWarning("PlayerSession não encontrado!");
            //playerNickname.text = "";
            //SetIcons(playerSession.Icon);
        }


    }

    public void SetIconPlayer(string iconName)
    {
        Sprite icon = Resources.Load<Sprite>($"Players/Icons/{iconName}");

        if (icon == null)
        {
            Debug.LogWarning($"Ícone não encontrado: {iconName}, usando padrão.");
            icon = Resources.Load<Sprite>($"Players/Icons/{defaultIconName}");
        }

        playerIconImage.sprite = icon;
    }

    public void SetIconOpponent(string iconName)
    {
        Sprite icon = Resources.Load<Sprite>($"Players/Icons/{iconName}");

        if (icon == null)
        {
            Debug.LogWarning($"Ícone não encontrado: {iconName}, usando padrão.");
            icon = Resources.Load<Sprite>($"Players/Icons/{defaultIconName}");
        }

        opponentIconImage.sprite = icon;
    }
}
