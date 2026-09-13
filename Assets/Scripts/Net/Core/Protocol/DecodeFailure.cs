#nullable enable
using System;

namespace Anathema.Net.Core
{
    /// <summary>
    /// Motivo de uma decodificação inválida: categoria, caminho do campo e o valor
    /// recebido com a forma esperada, para que o log diga o que o servidor mandou.
    /// </summary>
    /// <example>
    /// <code>
    /// DecodeFailure failure = new DecodeFailure(DecodeFailureKind.MissingField, "payload.code", "code is missing: expected text");
    /// </code>
    /// </example>
    public sealed class DecodeFailure
    {
        /// <summary>Cria o motivo.</summary>
        /// <example><code>DecodeFailure failure = new DecodeFailure(DecodeFailureKind.NotObject, "", "root is Array: expected a JSON object");</code></example>
        public DecodeFailure(DecodeFailureKind kind, string path, string detail)
        {
            Kind = kind;
            Path = path ?? throw new ArgumentNullException(nameof(path), $"path of {kind} is null: expected a field path, empty for the root");
            Detail = detail ?? throw new ArgumentNullException(nameof(detail), $"detail of {kind} is null: expected the received value and the expected shape");
        }

        /// <summary>Categoria.</summary>
        /// <example><code>DecodeFailureKind kind = failure.Kind;</code></example>
        public DecodeFailureKind Kind { get; }

        /// <summary>Caminho do campo, como <c>payload.deck_problems[0].kind</c>; vazio na raiz.</summary>
        /// <example><code>string where = failure.Path;</code></example>
        public string Path { get; }

        /// <summary>Valor recebido (cortado) e forma esperada.</summary>
        /// <example><code>string what = failure.Detail;</code></example>
        public string Detail { get; }

        /// <summary>Forma para log e mensagem de exceção.</summary>
        /// <example><code>string text = failure.ToString(); // MissingField at payload.code: …</code></example>
        public override string ToString() => $"{Kind} at {(Path.Length == 0 ? "<root>" : Path)}: {Detail}";
    }
}
