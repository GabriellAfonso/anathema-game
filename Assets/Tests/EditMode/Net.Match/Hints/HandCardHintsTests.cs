#nullable enable
using Anathema.Net.Account;
using Anathema.Net.Core;
using Anathema.Net.Fakes;
using NUnit.Framework;

namespace Anathema.Net.Match.Tests
{
    /// <summary>US5-1 a US5-6, FR-028, FR-029: dicas que só informam.</summary>
    public class HandCardHintsTests
    {
        private LoadedCatalog catalog = null!;
        private FakeClientLog log = null!;

        [SetUp]
        public void LoadCatalog()
        {
            catalog = MatchTestCatalog.Default();
            log = new FakeClientLog();
        }

        [Test]
        public void UnidadeTemCustoEEnergiaLadoALado()
        {
            PlayerView view = MirrorViews.Edited("contract-match-update-declaration.json", "{\"card_instance_id\": 25, \"card_id\": 1}", "{\"card_instance_id\": 25, \"card_id\": 5}");

            HandCardHint hint = HandCardHints.For(new CardInstanceId(25), view, catalog, log);

            Assert.That((hint.Kind, hint.Cost, hint.EnergyCurrent), Is.EqualTo((HintCardKind.Unit, (long?)3, 2L)));
            Assert.That((hint.Target, hint.Candidates.Count, hint.Card), Is.EqualTo(((SpellTargetKind?)null, 0, (CardId?)new CardId(5))));
        }

        [Test]
        public void FeiticoSemAlvoNaoTemCandidatos()
        {
            PlayerView view = MirrorViews.Edited("contract-match-update-declaration.json", "{\"card_instance_id\": 26, \"card_id\": 1002}", "{\"card_instance_id\": 26, \"card_id\": 1001}");

            HandCardHint hint = HandCardHints.For(new CardInstanceId(26), view, catalog, log);

            Assert.That((hint.Kind, hint.Target, hint.Candidates.Count), Is.EqualTo((HintCardKind.Spell, (SpellTargetKind?)SpellTargetKind.None, 0)));
        }

        [Test]
        public void FeiticoAliadoListaAsProprias()
        {
            HandCardHint hint = HandCardHints.For(new CardInstanceId(24), MirrorViews.Declaration(), catalog, log);

            Assert.That(hint.Target, Is.EqualTo(SpellTargetKind.AlliedUnit));
            Assert.That(hint.Candidates, Is.EqualTo(new[] { new CardInstanceId(21), new CardInstanceId(22) }));
        }

        [Test]
        public void FeiticoInimigoComBancoVazioTemListaVaziaSemEsconder()
        {
            HandCardHint hint = HandCardHints.For(new CardInstanceId(3), MirrorViews.Mulligan(), catalog, log);

            Assert.That((hint.Kind, hint.Target, hint.Candidates.Count), Is.EqualTo((HintCardKind.Spell, (SpellTargetKind?)SpellTargetKind.EnemyUnit, 0)));
        }

        [Test]
        public void FeiticoInimigoListaAsDoOponente()
        {
            HandCardHint hint = HandCardHints.For(new CardInstanceId(26), MirrorViews.Declaration(), catalog, log);

            Assert.That(hint.Candidates, Is.EqualTo(new[] { new CardInstanceId(40), new CardInstanceId(41) }));
        }

        [Test]
        public void SoNaDeclaracaoForaDelaInformaEFase()
        {
            PlayerView view = MirrorViews.Edited("contract-match-update-action.json", "{\"card_instance_id\": 1, \"card_id\": 1}", "{\"card_instance_id\": 1, \"card_id\": 1003}");

            HandCardHint hint = HandCardHints.For(new CardInstanceId(1), view, catalog, log);

            Assert.That((hint.DeclarationOnly, hint.Phase, hint.Kind), Is.EqualTo((true, MatchPhase.Action, HintCardKind.Spell)));
        }

        [Test]
        public void CartaForaDoCatalogoEhDesconhecidaERegistrada()
        {
            PlayerView view = MirrorViews.Edited("contract-match-update-action.json", "{\"card_instance_id\": 1, \"card_id\": 1}", "{\"card_instance_id\": 1, \"card_id\": 9999}");

            HandCardHint hint = HandCardHints.For(new CardInstanceId(1), view, catalog, log);

            Assert.That((hint.Kind, hint.Card, hint.Cost), Is.EqualTo((HintCardKind.UnknownCard, (CardId?)new CardId(9999), (long?)null)));
            Assert.That(log.Single("hint_card_unknown").Level, Is.EqualTo(ClientLogLevel.Warning));
        }

        [Test]
        public void CopiaForaDaMaoEhForaDaMao()
        {
            HandCardHint hint = HandCardHints.For(new CardInstanceId(777), MirrorViews.Action(), catalog, log);

            Assert.That((hint.Kind, hint.Card, hint.Candidates.Count), Is.EqualTo((HintCardKind.NotInHand, (CardId?)null, 0)));
        }

        [Test]
        public void AlvoDesconhecidoNaoTemCandidatos()
        {
            LoadedCatalog odd = MatchTestCatalog.Build(MatchTestCatalog.Spell(1002, "ODD", 1, "any_unit", false));

            HandCardHint hint = HandCardHints.For(new CardInstanceId(26), MirrorViews.Declaration(), odd, log);

            Assert.That((hint.Target, hint.Candidates.Count), Is.EqualTo(((SpellTargetKind?)SpellTargetKind.Unknown, 0)));
        }
    }
}
