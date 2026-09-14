#nullable enable

namespace Anathema.Net.Connection
{
    /// <summary>O fim classificado de um socket, com o close code que o servidor mandou (ou nenhum).</summary>
    internal sealed class SocketEnd
    {
        private SocketEnd(SocketEndKind kind, MatchRefusalDetail? match, int? closeCode)
        {
            Kind = kind;
            Match = match;
            CloseCode = closeCode;
        }

        internal SocketEndKind Kind { get; }

        internal MatchRefusalDetail? Match { get; }

        internal int? CloseCode { get; }

        internal static SocketEnd TokenRefused(int? closeCode) => new SocketEnd(SocketEndKind.TokenRefused, null, closeCode);

        internal static SocketEnd MatchRefused(MatchRefusalDetail detail, int? closeCode) => new SocketEnd(SocketEndKind.MatchRefused, detail, closeCode);

        internal static SocketEnd Dropped(int? closeCode) => new SocketEnd(SocketEndKind.Dropped, null, closeCode);

        public override string ToString() => $"{Kind} match_detail={(Match.HasValue ? Match.Value.ToString() : "none")} close_code={(CloseCode.HasValue ? CloseCode.Value.ToString() : "none")}";
    }
}
