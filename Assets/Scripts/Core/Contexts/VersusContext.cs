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

    public void SetContext(string json)
    {  
        var dto = JsonUtility.FromJson<VersusDTO>(json);

        Player = dto.self;
        Opponent = dto.opponent;
        MatchId = dto.match_id;
    }

    public void Clear()
    {
        Destroy(gameObject);
    }
}
