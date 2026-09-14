#nullable enable

namespace Anathema.Net.Match
{
    /// <summary>
    /// Fase da partida como o servidor manda em <c>view.phase</c>
    /// (<c>backend/server/apps/game/match/match_state.py</c>, <c>MatchPhase</c>). O cliente só espelha:
    /// nenhuma fase é avançada aqui. Valor novo do backend vira <see cref="Unknown"/>.
    /// </summary>
    /// <example>
    /// <code>
    /// if (mirror.Phase == MatchPhase.Mulligan) ShowMulligan();
    /// </code>
    /// </example>
    public enum MatchPhase
    {
        /// <summary><c>mulligan</c>: espera do setup, antes da Rodada 1.</summary>
        Mulligan,

        /// <summary><c>upkeep</c>.</summary>
        Upkeep,

        /// <summary><c>action</c>: Fase de Ação.</summary>
        Action,

        /// <summary><c>declaration</c>: janela do atacante.</summary>
        Declaration,

        /// <summary><c>combat</c>: janela do defensor.</summary>
        Combat,

        /// <summary><c>round_end</c>.</summary>
        RoundEnd,

        /// <summary><c>finished</c>: a partida acabou.</summary>
        Finished,

        /// <summary>Texto que o cliente não conhece; o texto fica em <see cref="PlayerView.PhaseText"/>.</summary>
        Unknown,
    }
}
