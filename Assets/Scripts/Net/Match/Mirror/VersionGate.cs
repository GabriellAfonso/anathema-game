#nullable enable

namespace Anathema.Net.Match
{
    /// <summary>
    /// A regra de versão do espelho: <c>match_update</c> com versão menor ou igual à última aplicada é descartado,
    /// inclusive frente à do <c>match_start</c>; versões não são contínuas
    /// (<c>backend/specs/009-match-protocol/contracts/server_frames.md</c>). Julgar não muda nada: quem aplica
    /// registra depois (specs/004-match-session/research.md, R4).
    /// </summary>
    internal sealed class VersionGate
    {
        internal long? AppliedVersion { get; private set; }

        internal FrameVerdict Judge(long version, bool isStart)
        {
            if (!AppliedVersion.HasValue || version > AppliedVersion.Value)
                return FrameVerdict.Accept;

            return isStart && version == AppliedVersion.Value ? FrameVerdict.ResyncSameVersion : FrameVerdict.Discard;
        }

        internal void Record(long version)
        {
            AppliedVersion = version;
        }
    }
}
