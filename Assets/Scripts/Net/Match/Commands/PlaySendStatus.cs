#nullable enable

namespace Anathema.Net.Match
{
    /// <summary>O que aconteceu com um comando ao ser mandado. Nenhum comando fica guardado para depois.</summary>
    /// <example>
    /// <code>
    /// if (result.Status == PlaySendStatus.NotConnected) ShowReconnecting();
    /// </code>
    /// </example>
    public enum PlaySendStatus
    {
        /// <summary>Entregue ao socket; a resposta vem por atualização ou recusa.</summary>
        Sent,

        /// <summary>A conexão não estava conectada; nada foi enviado.</summary>
        NotConnected,

        /// <summary>A conexão estava conectada mas o socket não aceitou o envio.</summary>
        SocketFailed,
    }
}
