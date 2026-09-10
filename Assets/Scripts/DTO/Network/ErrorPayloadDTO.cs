/// <summary>
/// Payload de todo evento de erro do servidor. O BaseConsumer.send_error monta
/// {'error': mensagem, **extra}, entao 'error' esta sempre presente e os campos
/// extras variam por evento.
/// </summary>
[System.Serializable]
public class ErrorPayloadDTO
{
    public string error;
}
