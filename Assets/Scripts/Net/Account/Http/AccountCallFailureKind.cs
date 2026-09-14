#nullable enable

namespace Anathema.Net.Account
{
    /// <summary>Falhas comuns a toda chamada autenticada, que não são recusa do recurso.</summary>
    /// <example><code>if (outcome.Failure?.Kind == AccountCallFailureKind.TransportFailed) ShowOffline();</code></example>
    public enum AccountCallFailureKind
    {
        /// <summary>Sem sessão, ou sessão expirada.</summary>
        SessionUnavailable,

        /// <summary>O pedido não chegou ao servidor, ou a resposta não voltou.</summary>
        TransportFailed,

        /// <summary>O servidor respondeu algo que não segue o contrato.</summary>
        OutOfContract,

        /// <summary>
        /// O token precisava de renovação e ela não saiu por status do servidor ou resposta fora do
        /// contrato; a sessão continua valendo e vale tentar de novo (research R5).
        /// </summary>
        RenewalUnavailable,
    }
}
