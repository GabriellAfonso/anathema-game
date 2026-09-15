#nullable enable

namespace Anathema.Net.Core
{
    /// <summary>
    /// Lê o valor de um campo discriminador (<c>type</c>, <c>kind</c>, <c>modifier_kind</c>)
    /// com as falhas próprias de discriminador: ausente é <see cref="DecodeFailureKind.MissingType"/>,
    /// não-texto é <see cref="DecodeFailureKind.TypeNotText"/>. Usado pela união e pelo
    /// envelope, para que os dois falhem igual.
    /// </summary>
    /// <example>
    /// <code>
    /// string kind = DiscriminatorField.Read(problem, "kind");
    /// </code>
    /// </example>
    internal static class DiscriminatorField
    {
        /// <summary>Valor exato do discriminador (com diferença de maiúsculas).</summary>
        /// <example><code>string type = DiscriminatorField.Read(envelope, "type");</code></example>
        internal static string Read(IPayloadReader reader, string field)
        {
            string path = PayloadPath.Field(reader.Path, field);
            if (!reader.Has(field))
                throw Shape(DecodeFailureKind.MissingType, path, $"object has no '{field}': expected a text discriminator");

            try
            {
                return reader.ReadText(field);
            }
            catch (PayloadShapeException wrongType) when (wrongType.Failure.Kind == DecodeFailureKind.WrongFieldType)
            {
                throw Shape(DecodeFailureKind.TypeNotText, path, wrongType.Failure.Detail);
            }
        }

        private static PayloadShapeException Shape(DecodeFailureKind kind, string path, string detail)
        {
            return new PayloadShapeException(new DecodeFailure(kind, path, detail));
        }
    }
}
