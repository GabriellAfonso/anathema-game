#nullable enable
using Anathema.Net.Core;
using NUnit.Framework;

namespace Anathema.Net.Connection.Tests
{
    /// <summary>Tabela de specs/003-authenticated-socket-queue/research.md, R7.</summary>
    public class SocketEndClassifierTests
    {
        [TestCase(true, false, null)]
        [TestCase(true, false, 4001)]
        [TestCase(false, false, 4001)]
        [TestCase(true, false, 4404)]
        public void RecusaDeToken(bool sawAuthDenied, bool sawMatchDenied, int? code)
        {
            SocketEnd end = Classify(sawAuthDenied, sawMatchDenied, code);

            Assert.That(end.Kind, Is.EqualTo(SocketEndKind.TokenRefused));
            Assert.That(end.CloseCode, Is.EqualTo(code));
        }

        [TestCase(true, 4400, MatchRefusalDetail.NoMatchId)]
        [TestCase(false, 4400, MatchRefusalDetail.NoMatchId)]
        [TestCase(true, 4403, MatchRefusalDetail.NotAParticipant)]
        [TestCase(false, 4403, MatchRefusalDetail.NotAParticipant)]
        [TestCase(true, 4404, MatchRefusalDetail.MatchNotFound)]
        [TestCase(false, 4404, MatchRefusalDetail.MatchNotFound)]
        [TestCase(true, null, MatchRefusalDetail.Unspecified)]
        [TestCase(true, 1006, MatchRefusalDetail.Unspecified)]
        public void RecusaDePartida(bool sawMatchDenied, int? code, MatchRefusalDetail expected)
        {
            SocketEnd end = Classify(false, sawMatchDenied, code);

            Assert.That(end.Kind, Is.EqualTo(SocketEndKind.MatchRefused));
            Assert.That(end.Match, Is.EqualTo(expected));
        }

        [TestCase(null)]
        [TestCase(1000)]
        [TestCase(1001)]
        [TestCase(1006)]
        [TestCase(4999)]
        public void QuedaComum(int? code)
        {
            SocketEnd end = Classify(false, false, code);

            Assert.That(end.Kind, Is.EqualTo(SocketEndKind.Dropped));
            Assert.That(end.CloseCode, Is.EqualTo(code));
        }

        private static SocketEnd Classify(bool sawAuthDenied, bool sawMatchDenied, int? code)
        {
            return SocketEndClassifier.Classify(sawAuthDenied, sawMatchDenied, new SocketClosure(code, "test"));
        }
    }
}
