#nullable enable
using Anathema.Net.Core;

namespace Anathema.Net.Match
{
    /// <summary>
    /// <c>modifier_kind</c> <c>damage_immunity</c>, sem quantidade: a mecânica é tudo ou nada (Magic Barrier,
    /// Fluxo de Partida §14). Quem decide o dano que ela anula é o servidor.
    /// </summary>
    /// <example>
    /// <code>
    /// bool shielded = unit.Modifiers.Any(modifier => modifier is DamageImmunityModifier);
    /// </code>
    /// </example>
    public sealed class DamageImmunityModifier : UnitModifier
    {
        /// <summary>Valor de <c>modifier_kind</c>.</summary>
        /// <example><code>union.Register(DamageImmunityModifier.KindName, DamageImmunityModifier.Read);</code></example>
        public const string KindName = "damage_immunity";

        private DamageImmunityModifier(IPayloadReader modifier)
            : base(KindName, modifier.ReadText("duration"))
        {
        }

        internal static DamageImmunityModifier Read(IPayloadReader modifier) => new DamageImmunityModifier(modifier);
    }
}
