#nullable enable
using System;

namespace Anathema.Net.Core
{
    /// <summary>
    /// O gate de autenticação recusou o socket; chega antes do fechamento 4001
    /// (backend/specs/013-socket-heartbeat/contracts/heartbeat_messages.md, "Quando não há pong").
    /// </summary>
    /// <example>
    /// <code>
    /// if (frame is AuthDeniedFrame denied) tokens.MarkRejected(denied.Error);
    /// </code>
    /// </example>
    public sealed class AuthDeniedFrame : ServerFrame
    {
        /// <summary>Valor de <c>type</c>.</summary>
        /// <example><code>union.Register(AuthDeniedFrame.TypeName, AuthDeniedFrame.Read);</code></example>
        public const string TypeName = "auth_denied";

        /// <summary>Cria a negação.</summary>
        /// <example><code>AuthDeniedFrame denied = new AuthDeniedFrame("authentication required: token expired");</code></example>
        public AuthDeniedFrame(string error)
            : base(TypeName)
        {
            Error = error ?? throw new ArgumentNullException(nameof(error), "auth_denied error is null: expected the server error text");
        }

        /// <summary>Texto do servidor, para log.</summary>
        /// <example><code>string why = denied.Error;</code></example>
        public string Error { get; }

        /// <summary>Braço da união: lê <c>error</c> obrigatório.</summary>
        /// <example><code>AuthDeniedFrame denied = AuthDeniedFrame.Read(payload);</code></example>
        public static AuthDeniedFrame Read(IPayloadReader payload)
        {
            return new AuthDeniedFrame(payload.ReadText("error"));
        }
    }
}
