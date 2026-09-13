#nullable enable

namespace Anathema.Net.Core
{
    /// <summary>
    /// Por que um frame não virou valor tipado. Os primeiros cinco seguem a ordem
    /// de verificação do envelope (specs/001-server-connection/contracts/protocol-codec.md).
    /// </summary>
    /// <example><code>if (outcome.Failure.Kind == DecodeFailureKind.NotJson) log.Warning("frame_not_json");</code></example>
    public enum DecodeFailureKind
    {
        /// <summary>O texto não é JSON.</summary>
        NotJson,

        /// <summary>A raiz não é objeto.</summary>
        NotObject,

        /// <summary>Falta o discriminador (<c>type</c>, <c>kind</c>…).</summary>
        MissingType,

        /// <summary>O discriminador não é texto.</summary>
        TypeNotText,

        /// <summary><c>payload</c> presente e não é objeto.</summary>
        PayloadNotObject,

        /// <summary>Campo obrigatório ausente.</summary>
        MissingField,

        /// <summary>Campo com tipo JSON errado.</summary>
        WrongFieldType,

        /// <summary>Campo com tipo certo e valor fora do aceito.</summary>
        InvalidValue,
    }
}
