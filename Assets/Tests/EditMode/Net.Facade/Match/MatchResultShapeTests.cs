#nullable enable
using System;
using Anathema.Net.Account;
using Anathema.Net.Connection;
using Anathema.Net.Core;
using Anathema.Net.Fakes;
using Anathema.Net.Json;
using Anathema.Net.Match;
using NUnit.Framework;

namespace Anathema.Net.Facade.Tests
{
    /// <summary>data-model.md, "Resultado e indisponibilidade": forma do resultado e da partida indisponível.</summary>
    public class MatchResultShapeTests
    {
        private static readonly MatchId Match = new MatchId("match-7");

        private IProtocolCodec codec = null!;

        [SetUp]
        public void CreateCodec()
        {
            codec = new NewtonsoftProtocolCodec(MatchFrames.CreateUnion(), new FakeClientLog());
        }

        [Test]
        public void BuscandoSemLinhaEZeroLeituras()
        {
            MatchResult result = MatchResult.Fetching(Match, Outcome(), true);

            Assert.That((result.RowStatus, result.Row, result.Attempts, result.Won), Is.EqualTo((HistoryRowStatus.Fetching, (MatchHistoryRow?)null, 0, true)));
            Assert.That(result.Outcome!.DefeatedUser, Is.EqualTo(new UserId(9)));
        }

        [Test]
        public void ResolvidaTemALinhaEAsLeituras()
        {
            MatchHistoryRow row = Row();

            MatchResult result = MatchResult.Fetching(Match, Outcome(), true).Resolved(row, 3);

            Assert.That((result.RowStatus, result.Row, result.Attempts), Is.EqualTo((HistoryRowStatus.Resolved, row, 3)));
        }

        [Test]
        public void IndisponivelFicaSemLinhaComODesfecho()
        {
            MatchResult result = MatchResult.Fetching(Match, Outcome(), false).Unavailable(4);

            Assert.That((result.RowStatus, result.Row, result.Won), Is.EqualTo((HistoryRowStatus.Unavailable, (MatchHistoryRow?)null, false)));
            Assert.That(result.Outcome, Is.Not.Null);
        }

        [TestCase(0)]
        [TestCase(5)]
        public void LeiturasForaDeUmAQuatroLancam(int attempts)
        {
            MatchResult fetching = MatchResult.Fetching(Match, Outcome(), true);

            Assert.Throws<ArgumentOutOfRangeException>(() => fetching.Unavailable(attempts));
        }

        [Test]
        public void CatalogoIndisponivelTemTextoESemMotivoDeConexao()
        {
            MatchUnavailable unavailable = MatchUnavailable.CatalogUnavailable(Match, null);

            Assert.That((unavailable.Kind, unavailable.GiveUp, unavailable.Match), Is.EqualTo((MatchUnavailableKind.CatalogUnavailable, (GiveUpReason?)null, Match)));
            Assert.That(unavailable.PlayerText, Is.Not.Empty);
        }

        [Test]
        public void ConexaoDesistidaTemOMotivoEOTextoDele()
        {
            GiveUpReason reason = GiveUpReason.AttemptsExhausted(5);

            MatchUnavailable unavailable = MatchUnavailable.ConnectionGaveUp(Match, reason);

            Assert.That((unavailable.Kind, unavailable.GiveUp, unavailable.PlayerText), Is.EqualTo((MatchUnavailableKind.ConnectionGaveUp, reason, reason.PlayerText())));
            Assert.Throws<ArgumentNullException>(() => MatchUnavailable.MatchRefused(Match, null!));
        }

        private MatchOutcome Outcome() => MatchOutcome.Read(codec.DecodeObject("{\"defeated_user_id\": 9, \"reason\": \"nexus_depleted\"}").Value);

        private MatchHistoryRow Row()
        {
            const string row = "{\"match_id\": \"match-7\", \"won\": true, \"end_reason\": \"nexus_depleted\", \"opponent\": null, \"duration_seconds\": 742, \"final_round\": 8, \"ended_at\": \"2026-09-12T18:03:11Z\"}";
            return MatchHistoryRow.Read(codec.DecodeObject(row).Value);
        }
    }
}
