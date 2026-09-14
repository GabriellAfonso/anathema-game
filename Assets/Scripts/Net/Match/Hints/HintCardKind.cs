#nullable enable

namespace Anathema.Net.Match
{
    /// <summary>Que carta a dica descreve. Nenhum valor diz se a jogada é legal.</summary>
    /// <example>
    /// <code>
    /// if (hint.Kind == HintCardKind.Spell &amp;&amp; hint.Target != SpellTargetKind.None) BeginTargeting(hint.Candidates);
    /// </code>
    /// </example>
    public enum HintCardKind
    {
        /// <summary>Unidade do catálogo.</summary>
        Unit,

        /// <summary>Feitiço do catálogo.</summary>
        Spell,

        /// <summary>O <c>card_id</c> da cópia não está no catálogo carregado.</summary>
        UnknownCard,

        /// <summary>A cópia não está na mão atual (saiu entre o clique e a consulta, ou nunca esteve).</summary>
        NotInHand,
    }
}
