using UnityEngine;

public class PlayerSession : MonoBehaviour
{
    public static PlayerSession Instance { get; private set; }

    public string Token { get; private set; }
    public string RefreshToken { get; private set; }
    public string Nickname { get; private set; }
    public string Icon { get; private set; }
    public int Level { get; private set; }
    public int Experience_points { get; private set; }
    public int Coins { get; private set; }
    public int Credits { get; private set; }
    

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

    public void SetProfile(SelfPlayerProfileDTO profile)
    {
        Nickname = profile.nickname;
        Icon = profile.icon;
        Level = profile.level;
        Experience_points = profile.experience_points;
        Coins = profile.coins;
        Credits = profile.credits;

    }

    /// <summary>Guarda o par que o login devolve. O refresh e o unico jeito
    /// de sobreviver aos 5 minutos de vida do access token.</summary>
    public void SetTokens(string token, string refreshToken)
    {
        Token = token;
        RefreshToken = refreshToken;
    }

    /// <summary>So o access token, usado pelo TokenRefreshService: o refresh
    /// segue valido e nao pode ser sobrescrito aqui.</summary>
    public void SetAccessToken(string token)
    {
        Token = token;
    }
}
