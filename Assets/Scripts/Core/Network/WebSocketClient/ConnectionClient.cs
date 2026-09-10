/// <summary>
/// Socket de presenca. Nao tem protocolo proprio ainda: o ping/pong que ficava
/// aqui subiu para o BaseClient, onde vale para os tres clients, e a direcao
/// virou cliente para servidor.
/// </summary>
public class ConnectionClient : BaseClient
{
    public ConnectionClient(
        string baseUrl,
        ReconnectPolicy policy = null,
        Heartbeat heartbeat = null)
        : base(baseUrl, policy, heartbeat) { }
}
