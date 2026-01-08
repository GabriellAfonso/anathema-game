using UnityEngine;

public class MyButtonScript : MonoBehaviour
{
    public void OnClickAction()
    {
        var matchmakingClient = WebSocketClient.MatchmakingClient;
        matchmakingClient.Connect();
    }
}
