#nullable enable
using System.Collections.Generic;
using System.Linq;
using Anathema.Net.Core;

namespace Anathema.Net.Match
{
    /// <summary>União dos modificadores por <c>modifier_kind</c>, com braço desconhecido.</summary>
    internal static class UnitModifiers
    {
        private const string DiscriminatorField = "modifier_kind";

        private static readonly DiscriminatedUnion<UnitModifier> Union =
            new DiscriminatedUnion<UnitModifier>(DiscriminatorField, (kind, item) => new UnrecognizedModifier(kind, item.ReadOptionalText("duration") ?? string.Empty))
                .Register(AttackModifier.KindName, AttackModifier.Read)
                .Register(HealthModifier.KindName, HealthModifier.Read)
                .Register(DamageImmunityModifier.KindName, DamageImmunityModifier.Read);

        internal static UnitModifier ReadOne(IPayloadReader modifier) => Union.ReadNested(modifier);

        internal static IReadOnlyList<UnitModifier> ReadList(IPayloadReader parent, string field)
        {
            return parent.ReadObjectList(field).Select(Union.ReadNested).ToArray();
        }
    }
}
