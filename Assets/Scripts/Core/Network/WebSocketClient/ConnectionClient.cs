public class ConnectionClient : BaseClient
{
    public ConnectionClient(string baseUrl)
        : base(baseUrl) { }

  
    protected override void Handle(string type, string payload)
    {
        if (type == "ping")
        {
            Send("pong");
        }
    }
}

