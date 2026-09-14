#nullable enable
using System;
using Anathema.Net.Core;
using NUnit.Framework;

namespace Anathema.Net.Account.Tests
{
    public class DeckChangeTests
    {
        [Test]
        public void SemNomeESemListaLancaNomeandoOsDois()
        {
            ArgumentException error = Assert.Throws<ArgumentException>(() => new DeckChange());

            Assert.That(error.Message, Does.Contain("name").And.Contain("cards"));
        }

        [Test]
        public void SoNomeSoListaOuOsDoisSaoAceitos()
        {
            CardId[] cards = { new CardId(1) };

            Assert.That(new DeckChange(name: "Agro").Cards, Is.Null);
            Assert.That(new DeckChange(cards: cards).Name, Is.Null);
            Assert.That(new DeckChange("Agro", cards).Cards, Is.SameAs(cards));
        }

        [Test]
        public void RascunhoSemNomeOuSemListaLanca()
        {
            Assert.Throws<ArgumentNullException>(() => new DeckDraft(null!, new[] { new CardId(1) }));
            Assert.Throws<ArgumentNullException>(() => new DeckDraft("Agro", null!));
        }
    }
}
