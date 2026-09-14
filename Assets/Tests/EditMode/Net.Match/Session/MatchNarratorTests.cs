#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Anathema.Net.Core;
using Anathema.Net.Fakes;
using NUnit.Framework;

namespace Anathema.Net.Match.Tests
{
    /// <summary>US6-6, FR-036: uma linha estruturada por evento, com apelido e nome de carta.</summary>
    public class MatchNarratorTests
    {
        private FakeClientLog log = null!;
        private MatchMirror mirror = null!;
        private MatchNarrator narrator = null!;

        [SetUp]
        public void CreateNarrator()
        {
            log = new FakeClientLog();
            mirror = new MatchMirror(log);
            narrator = new MatchNarrator(mirror, MatchTestCatalog.Default(), new MatchId("match-7"), log);
        }

        [TearDown]
        public void DisposeNarrator()
        {
            narrator.Dispose();
        }

        [Test]
        public void UnidadeJogadaTemApelidoENomeDaCarta()
        {
            Apply("{\"kind\": \"unit_played\", \"user_id\": 7, \"card\": {\"card_instance_id\": 21, \"card_id\": 5}}");

            Dictionary<string, string> fields = Fields(log.Single("match_event"));
            Assert.That(fields["match_id"], Is.EqualTo("match-7"));
            Assert.That(fields["round"], Is.EqualTo("2"));
            Assert.That(fields["kind"], Is.EqualTo("unit_played"));
            Assert.That(fields["user_id"], Is.EqualTo("gabriel"));
            Assert.That(fields["card"], Is.EqualTo("SAND WOLF#21"));
        }

        [Test]
        public void UmaLinhaPorEventoNaOrdem()
        {
            MatchUpdateFrame update = MatchJson.Fixture<MatchUpdateFrame>("contract-match-update-all-events.json");

            mirror.Apply(update.View, update.Version, update.Events);

            string[] kinds = log.Entries.Where(entry => entry.EventName == "match_event").Select(entry => Fields(entry)["kind"]).ToArray();
            Assert.That(kinds, Is.EqualTo(update.Events.Select(item => item.KindText)));
        }

        [Test]
        public void ListaDeAtacantesSeparadaPorVirgulaEOponentePeloApelido()
        {
            Apply("{\"kind\": \"attackers_sent\", \"user_id\": 9, \"attacker_card_instance_ids\": [21, 22]}");

            Dictionary<string, string> fields = Fields(log.Single("match_event"));
            Assert.That((fields["user_id"], fields["attacker_card_instance_ids"]), Is.EqualTo(("rival", "21,22")));
        }

        [Test]
        public void SemNomeConhecidoSaiOIdentificadorTipado()
        {
            Apply("{\"kind\": \"unit_died\", \"user_id\": 99, \"card\": {\"card_instance_id\": 40, \"card_id\": 9999}}");

            Dictionary<string, string> fields = Fields(log.Single("match_event"));
            Assert.That(fields["user_id"], Is.EqualTo(new UserId(99).ToString()));
            Assert.That(fields["card"], Is.EqualTo("card_instance_id=40 card_id=9999"));
        }

        [Test]
        public void EventoDesconhecidoSaiMarcado()
        {
            Apply("{\"kind\": \"card_discarded\", \"user_id\": 7}");

            Dictionary<string, string> fields = Fields(log.Single("match_event"));
            Assert.That((fields["kind"], fields["unrecognized"]), Is.EqualTo(("card_discarded", "true")));
        }

        [Test]
        public void DepoisDeDisposeNaoNarra()
        {
            narrator.Dispose();

            Apply("{\"kind\": \"passed\", \"user_id\": 9}");

            Assert.That(log.Entries.Count(entry => entry.EventName == "match_event"), Is.EqualTo(0));
        }

        private void Apply(string singleEvent)
        {
            string frame = MatchFixtures.Text("contract-match-update-no-clock.json").Replace("{\"kind\": \"passed\", \"user_id\": 9}", singleEvent);
            MatchUpdateFrame update = MatchJson.Frame<MatchUpdateFrame>(frame);
            mirror.Apply(update.View, update.Version, update.Events);
        }

        private static Dictionary<string, string> Fields(ClientLogEntry entry)
        {
            return entry.Fields.ToDictionary(field => field.Name, field => field.Value, StringComparer.Ordinal);
        }
    }
}
