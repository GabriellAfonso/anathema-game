#nullable enable

namespace Anathema.Net.Account
{
    /// <summary>
    /// Que alvo um feitiço pede, pelo conjunto fechado de <c>target_kind</c> do contrato de
    /// catálogo. O cliente só usa para desenhar a mira; quem recusa alvo errado é o servidor.
    /// </summary>
    /// <example><code>if (spell.Effect.TargetKind == SpellTargetKind.EnemyUnit) HighlightOpponentBench();</code></example>
    public enum SpellTargetKind
    {
        /// <summary><c>none</c>: sem alvo, sem mira.</summary>
        None,

        /// <summary><c>allied_unit</c>: uma unidade do próprio banco.</summary>
        AlliedUnit,

        /// <summary><c>enemy_unit</c>: uma unidade do banco do oponente.</summary>
        EnemyUnit,

        /// <summary>Valor fora do conjunto; o texto fica em <c>TargetKindText</c>.</summary>
        Unknown,
    }
}
