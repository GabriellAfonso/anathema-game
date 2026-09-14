#nullable enable

namespace Anathema.Net.Match
{
    /// <summary>
    /// <c>modifier_kind</c> que o cliente não conhece. Não é erro: o servidor pode mandar mecânica nova antes
    /// de o cliente saber desenhá-la.
    /// </summary>
    /// <example>
    /// <code>
    /// if (modifier is UnrecognizedModifier unknown) log.Debug("modifier_unknown", new LogField("kind", unknown.KindText));
    /// </code>
    /// </example>
    public sealed class UnrecognizedModifier : UnitModifier
    {
        /// <summary>Modificador desconhecido com o texto de <c>modifier_kind</c> e de <c>duration</c> (vazio se ausente).</summary>
        /// <example><code>UnitModifier unknown = new UnrecognizedModifier("poison", "permanent");</code></example>
        public UnrecognizedModifier(string kindText, string durationText)
            : base(kindText, durationText)
        {
        }
    }
}
