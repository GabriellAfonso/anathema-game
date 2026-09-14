#nullable enable
using System.Collections.Generic;
using Anathema.Net.Core;

namespace Anathema.Net.Account
{
    /// <summary>
    /// A forma do efeito de um feitiço, para o cliente decidir a mira sem ler a descrição
    /// (<c>backend/specs/011-deck-catalog-api/contracts/http_catalog.md</c>). Valores fora dos
    /// conjuntos viram <c>Unknown</c> com o texto preservado; <see cref="RequiresTarget"/> é exposto
    /// como veio, mesmo incoerente com <see cref="TargetKind"/>: o cliente não decide regra.
    /// </summary>
    /// <example>
    /// <code>
    /// if (spell.Effect.RequiresTarget) BeginTargeting(spell.Effect.TargetKind);
    /// </code>
    /// </example>
    public sealed class SpellEffect
    {
        private static readonly Dictionary<string, SpellTargetKind> TargetKinds = new Dictionary<string, SpellTargetKind>
        {
            ["none"] = SpellTargetKind.None,
            ["allied_unit"] = SpellTargetKind.AlliedUnit,
            ["enemy_unit"] = SpellTargetKind.EnemyUnit,
        };

        private SpellEffect(IPayloadReader effect)
        {
            RequiresTarget = effect.ReadBoolean("requires_target");
            TargetKindText = effect.ReadText("target_kind");
            TargetKind = TargetKinds.TryGetValue(TargetKindText, out SpellTargetKind target) ? target : SpellTargetKind.Unknown;
            DurationText = effect.ReadText("duration");
            // O mesmo texto chega nos modificadores de unidade da partida; um mapeamento só (specs/004-match-session/research.md, R2).
            Duration = SpellDurationText.Parse(DurationText);
            DeclarationOnly = effect.ReadBoolean("declaration_only");
        }

        /// <summary>O feitiço pede alvo.</summary>
        /// <example><code>bool aim = effect.RequiresTarget;</code></example>
        public bool RequiresTarget { get; }

        /// <summary>Tipo de alvo.</summary>
        /// <example><code>SpellTargetKind kind = effect.TargetKind;</code></example>
        public SpellTargetKind TargetKind { get; }

        /// <summary>O texto de <c>target_kind</c> como veio.</summary>
        /// <example><code>string raw = effect.TargetKindText;</code></example>
        public string TargetKindText { get; }

        /// <summary>Duração.</summary>
        /// <example><code>SpellDuration duration = effect.Duration;</code></example>
        public SpellDuration Duration { get; }

        /// <summary>O texto de <c>duration</c> como veio.</summary>
        /// <example><code>string raw = effect.DurationText;</code></example>
        public string DurationText { get; }

        /// <summary>Só pode ser usado na declaração de ataque.</summary>
        /// <example><code>bool onlyWhenDeclaring = effect.DeclarationOnly;</code></example>
        public bool DeclarationOnly { get; }

        internal static SpellEffect Read(IPayloadReader effect) => new SpellEffect(effect);
    }
}
