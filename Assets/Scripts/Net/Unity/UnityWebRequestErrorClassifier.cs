#nullable enable
using System;
using Anathema.Net.Core;

namespace Anathema.Net.Unity
{
    /// <summary>
    /// Traduz o texto de erro do <c>UnityWebRequest</c> para uma categoria. O
    /// <c>UnityWebRequest</c> não expõe código numérico de falha; o texto é do Unity, não do
    /// servidor, então a regra "recusa pelo code" não se aplica aqui
    /// (specs/001-server-connection/research.md, R5).
    /// </summary>
    /// <example>
    /// <code>
    /// TransportFailureKind kind = UnityWebRequestErrorClassifier.Classify(request.error);
    /// </code>
    /// </example>
    public static class UnityWebRequestErrorClassifier
    {
        /// <summary>Categoria de uma falha de transporte a partir do texto do Unity.</summary>
        /// <example><code>TransportFailureKind kind = UnityWebRequestErrorClassifier.Classify("Request timeout");</code></example>
        public static TransportFailureKind Classify(string? unityError)
        {
            string error = unityError ?? string.Empty;
            if (Mentions(error, "Request timeout"))
                return TransportFailureKind.Timeout;

            if (Mentions(error, "Cannot resolve destination host"))
                return TransportFailureKind.HostNotResolved;

            return Mentions(error, "Cannot connect to destination host") ? TransportFailureKind.CannotConnect : TransportFailureKind.Other;
        }

        private static bool Mentions(string error, string unityText)
        {
            return error.IndexOf(unityText, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
