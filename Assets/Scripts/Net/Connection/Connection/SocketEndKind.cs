#nullable enable

namespace Anathema.Net.Connection
{
    /// <summary>Como um socket físico terminou, do ponto de vista de quem reconecta.</summary>
    internal enum SocketEndKind
    {
        /// <summary>Gate de autenticação: renovar e reabrir.</summary>
        TokenRefused,

        /// <summary>Gate de partida: desistir.</summary>
        MatchRefused,

        /// <summary>Qualquer outra queda: esperar e tentar de novo.</summary>
        Dropped,
    }
}
