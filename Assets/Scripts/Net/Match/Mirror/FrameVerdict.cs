#nullable enable

namespace Anathema.Net.Match
{
    /// <summary>O que fazer com um frame de estado, decidido antes de qualquer mudança (specs/004-match-session/research.md, R4).</summary>
    internal enum FrameVerdict
    {
        /// <summary>Versão nova (ou espelho vazio): substitui o estado.</summary>
        Accept,

        /// <summary><c>match_start</c> de reconexão com a mesma versão: não troca o estado, reancora o relógio.</summary>
        ResyncSameVersion,

        /// <summary>Versão velha ou repetida: nada muda.</summary>
        Discard,
    }
}
