#nullable enable
using Anathema.Net.Core;

namespace Anathema.Net.Connection
{
    /// <summary>
    /// Junta o que o socket mostrou antes de fechar (frames de gate) com o close code exato. O
    /// <c>DotNetWebSocket</c> entrega o número, então não é mais preciso adivinhar pelo frame como o
    /// <c>BaseClient</c> fazia com a NativeWebSocket; o frame sozinho ainda vale para o fechamento sem
    /// código (specs/003-authenticated-socket-queue/research.md, R7).
    /// </summary>
    internal static class SocketEndClassifier
    {
        internal static SocketEnd Classify(bool sawAuthDenied, bool sawMatchDenied, SocketClosure closure)
        {
            int? code = closure.Code;

            // O gate de autenticação vem antes do de partida no servidor: um socket nunca passa pelos dois.
            if (sawAuthDenied || code == ReconnectPolicy.AuthRejected)
                return SocketEnd.TokenRefused(code);

            MatchRefusalDetail? detail = MatchDetailOf(code);
            if (detail.HasValue)
                return SocketEnd.MatchRefused(detail.Value, code);

            return sawMatchDenied ? SocketEnd.MatchRefused(MatchRefusalDetail.Unspecified, code) : SocketEnd.Dropped(code);
        }

        private static MatchRefusalDetail? MatchDetailOf(int? code)
        {
            return code switch
            {
                ReconnectPolicy.MatchIdMissing => MatchRefusalDetail.NoMatchId,
                ReconnectPolicy.NotAParticipant => MatchRefusalDetail.NotAParticipant,
                ReconnectPolicy.MatchNotFound => MatchRefusalDetail.MatchNotFound,
                _ => null,
            };
        }
    }
}
