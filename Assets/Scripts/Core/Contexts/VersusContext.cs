using UnityEngine;

public class VersusContext : MonoBehaviour
{
    public static VersusContext Instance { get; private set; }

    public PlayerPublicDTO Player;
    public PlayerPublicDTO Opponent;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        Debug.Log("instanciado VersusContext");
    }

    public void SetContext(string json)
    {
        Debug.Log("setContext");
        Debug.Log(json);
       
        var dto = JsonUtility.FromJson<VersusDTO>(json);

        Debug.Log(dto == null ? "DTO NULL" : "DTO OK");
        Debug.Log(dto?.self == null ? "SELF NULL" : "SELF OK");
        Debug.Log(dto?.opponent == null ? "OPPONENT NULL" : "OPPONENT OK");

        Player = dto.self;
        Opponent = dto.opponent;
    }

    public void Clear()
    {
        Destroy(gameObject);
    }
}
