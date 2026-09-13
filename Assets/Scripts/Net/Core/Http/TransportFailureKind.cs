#nullable enable

namespace Anathema.Net.Core
{
    /// <summary>Por que o pedido não teve resposta.</summary>
    /// <example><code>if (failure.Kind == TransportFailureKind.CleartextRefused) log.Error("cleartext_in_production");</code></example>
    public enum TransportFailureKind
    {
        /// <summary>O prazo do pedido esgotou.</summary>
        Timeout,

        /// <summary>O nome do host não resolveu.</summary>
        HostNotResolved,

        /// <summary>O host resolveu, mas a conexão foi recusada ou não abriu.</summary>
        CannotConnect,

        /// <summary>URL sem TLS num build que não permite.</summary>
        CleartextRefused,

        /// <summary>Qualquer outra falha; o texto original fica no detalhe.</summary>
        Other,
    }
}
