using UnityEngine;

public class MyButtonScript : MonoBehaviour
{
    public void OnClickAction()
    {
        var matchmakingClient = NetworkBootstrap.MatchmakingClient;
        matchmakingClient.Connect();
    }
}
