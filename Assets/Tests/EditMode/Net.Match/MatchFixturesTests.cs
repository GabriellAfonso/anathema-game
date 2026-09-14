#nullable enable
using System.Collections.Generic;
using Anathema.Net.Core;
using Anathema.Net.Fakes;
using Anathema.Net.Json;
using NUnit.Framework;

namespace Anathema.Net.Match.Tests
{
    public class MatchFixturesTests
    {
        private static IEnumerable<string> ContractFixtures() => MatchFixtures.Names("contract-");

        [Test]
        public void HaAsOitoFixturesDeContrato()
        {
            Assert.That(MatchFixtures.Names("contract-"), Has.Length.EqualTo(8));
        }

        [TestCaseSource(nameof(ContractFixtures))]
        public void CadaFixtureDeContratoEhUmObjetoJson(string name)
        {
            IProtocolCodec codec = new NewtonsoftProtocolCodec(MatchFrames.CreateUnion(), new FakeClientLog());

            Assert.That(codec.DecodeObject(MatchFixtures.Text(name)).IsValid, Is.True, name);
        }

        [Test]
        public void FixtureAusenteLancaComOCaminho()
        {
            System.IO.FileNotFoundException error = Assert.Throws<System.IO.FileNotFoundException>(() => MatchFixtures.Text("nao-existe.json"));

            Assert.That(error.Message, Does.Contain("nao-existe.json"));
        }
    }
}
