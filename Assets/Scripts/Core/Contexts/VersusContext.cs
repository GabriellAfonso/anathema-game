using Anathema.Net.Connection;
using UnityEngine;

public class VersusContext : MonoBehaviour
{
    public static VersusContext Instance { get; private set; }

    public PlayerPublicDTO Player;
    public PlayerPublicDTO Opponent;
    public string MatchId;

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

    /// <summary>
    /// Guarda o pareamento do match_found, ja tipado pela fila (feature 003), para a VersusScene mostrar.
    /// </summary>
    /// <example><code>VersusContext.Instance.SetContext(pairing);</code></example>
    public void SetContext(MatchPairing pairing)
    {
        Player = ToPublic(pairing.Self);
        Opponent = ToPublic(pairing.Opponent);
        MatchId = pairing.Match.Value;
    }

    public void Clear()
    {
        Destroy(gameObject);
    }

    private static PlayerPublicDTO ToPublic(PairedPlayer player)
    {
        return new PlayerPublicDTO
        {
            user_id = (int)player.User.Value,
            nickname = player.Nickname,
            icon = player.Icon,
            level = (int)player.Level,
        };
    }
}
