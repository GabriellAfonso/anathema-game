#nullable enable

namespace Anathema.Net.Connection
{
    /// <summary>Família fechada dos motivos de desistência (FR-013).</summary>
    /// <example><code>if (reason.Kind == GiveUpKind.SessionExpired) ShowLogin();</code></example>
    public enum GiveUpKind
    {
        /// <summary>Não há sessão de conta autenticada.</summary>
        NoSession,

        /// <summary>A sessão de conta expirou: o refresh token foi recusado.</summary>
        SessionExpired,

        /// <summary>O servidor recusou o token seguidas vezes, mesmo renovado.</summary>
        TokenRefusedRepeatedly,

        /// <summary>O gate da partida recusou o socket.</summary>
        MatchRefused,

        /// <summary>Todas as tentativas de reconexão falharam.</summary>
        AttemptsExhausted,
    }
}
