#if UNITY_EDITOR_WIN
#nullable enable
using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using Anathema.Net.Account;
using Anathema.Net.Core;
using Anathema.Net.Fakes;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Anathema.Net.Unity.Tests
{
    /// <summary>
    /// Conta e dados do jogador contra o backend local (<c>docker compose up</c> em
    /// C:/Users/gabri/Projetos/dev_container/anathema/backend), só com adaptadores reais e a guarda
    /// DPAPI numa pasta temporária (specs/002-player-account/research.md, R11). Fora da rodada normal.
    /// </summary>
    [Explicit, Category("LiveServer")]
    public class LiveAccountTests
    {
        // O backend só exige 6 caracteres (MinimumLengthValidator em server/core/settings.py).
        private const string Password = "live-account-123";
        private static readonly TimeSpan ScenarioTimeout = TimeSpan.FromSeconds(30);
        private FakeClientLog log = null!;
        private MainThreadQueue queue = null!;
        private LiveNetworkAdapters adapters = null!;
        private string vaultDirectory = string.Empty;
        private RefreshTokenVaultSlot slot = null!;

        [SetUp]
        public void CreateLayer()
        {
            log = new FakeClientLog();
            queue = new MainThreadQueue(log);
            adapters = LiveNetworkAdapters.Create(queue, log, new CleartextPolicy(true));
            string suffix = Guid.NewGuid().ToString("N").Substring(0, 12);
            vaultDirectory = Path.Combine(Path.GetTempPath(), "anathema-live-" + suffix);
            slot = RefreshTokenVaultSlot.Named("live-test-" + suffix);
        }

        [TearDown]
        public void CloseLayer()
        {
            queue.Close();
            if (Directory.Exists(vaultDirectory))
                Directory.Delete(vaultDirectory, true);
        }

        [UnityTest]
        public IEnumerator CadastrarEntrarELerPerfil()
        {
            return Run(async account =>
            {
                string username = await RegisterAndSignInAsync(account);
                OwnProfile profile = Require(await account.Profile.ReadAsync());
                Assert.That(profile.Nickname, Is.EqualTo(username));
            });
        }

        [UnityTest]
        public IEnumerator CatalogoTemVinteEQuatroUnidadesECincoFeiticos()
        {
            return Run(async account =>
            {
                await RegisterAndSignInAsync(account);
                LoadedCatalog catalog = Require(await account.Catalog.LoadAsync());
                Assert.That(catalog.Cards.Count, Is.EqualTo(29));
                Assert.That(catalog.Cards.OfType<UnitCard>().Count(), Is.EqualTo(24));
                Assert.That(catalog.Cards.OfType<SpellCard>().Count(), Is.EqualTo(5));
            });
        }

        [UnityTest]
        public IEnumerator DeckInicialNaoTemFeitico()
        {
            return Run(async account =>
            {
                await RegisterAndSignInAsync(account);
                LoadedCatalog catalog = Require(await account.Catalog.LoadAsync());
                PlayerDeck starter = Require(await account.Decks.ListAsync()).Single(deck => deck.Name == "Deck inicial");
                Assert.That(starter.Cards.Select(card => catalog.Find(card).Kind), Has.None.EqualTo(CardLookupKind.Spell));
            });
        }

        [UnityTest]
        public IEnumerator CriarRenomearRecusarEApagarDeck()
        {
            return Run(async account =>
            {
                await RegisterAndSignInAsync(account);
                PlayerDeck created = Require(await account.Decks.CreateAsync(new DeckDraft("Fumaça", SpellDeckCards())));
                Require(await account.Decks.ChangeAsync(created.Deck, new DeckChange(name: "Fumaça v2")));
                await AssertTwelveCardsAreRefusedAsync(account);
                Require(await account.Decks.DeleteAsync(created.Deck));
                AccountCallOutcome<PlayerDeck, DeckRefusal> gone = await account.Decks.ReadAsync(created.Deck);
                Assert.That(gone.Refusal!.Reasons.Single(), Is.SameAs(DeckNotFound.Instance));
            });
        }

        [UnityTest]
        public IEnumerator HistoricoDaContaNovaEstaVazio()
        {
            return Run(async account =>
            {
                await RegisterAndSignInAsync(account);
                MatchHistoryPage page = Require(await account.History.ReadPageAsync(new HistoryPageRequest()));
                Assert.That(page.Count, Is.EqualTo(0));
            });
        }

        [UnityTest]
        public IEnumerator TokenRenovadoFuncionaNumaRotaAutenticada()
        {
            return Run(async account =>
            {
                await RegisterAndSignInAsync(account);
                RenewalOutcome renewed = await account.Tokens.RenewNowAsync();
                Assert.That(renewed.Kind, Is.EqualTo(RenewalOutcomeKind.Renewed), renewed.ToString());
                Require(await account.Profile.ReadAsync());
            });
        }

        [UnityTest]
        public IEnumerator SegundaSessaoComAMesmaGuardaRetomaSemSenha()
        {
            return Run(async account =>
            {
                await RegisterAndSignInAsync(account);
                using LiveAccountServices reopened = Compose();
                ResumeOutcome resumed = await reopened.Session.ResumeAsync();
                Assert.That(resumed.Kind, Is.EqualTo(ResumeOutcomeKind.Resumed), resumed.ToString());
                Assert.That(resumed.User, Is.EqualTo(account.Session.Self));
            });
        }

        private IEnumerator Run(Func<LiveAccountServices, Task> scenario)
        {
            using LiveAccountServices account = Compose();
            Task running = scenario(account);
            Stopwatch waited = Stopwatch.StartNew();
            while (!running.IsCompleted && waited.Elapsed < ScenarioTimeout)
            {
                queue.Drain();
                yield return null;
            }

            Assert.That(running.IsCompleted, Is.True, $"scenario did not finish in {ScenarioTimeout.TotalSeconds}s: is the backend up on {LocalAccountRoutes.HttpBase}?");
            if (running.IsFaulted)
                ExceptionDispatchInfo.Capture(running.Exception!.InnerException!).Throw();
        }

        private LiveAccountServices Compose()
        {
            FakeAppLifecycle lifecycle = new FakeAppLifecycle(new FakeMonotonicClock());
            return new LiveAccountServices(adapters.Http, adapters.Codec, adapters.Clock, lifecycle, log, LocalAccountRoutes.Create(),
                new DpapiRefreshTokenVault(vaultDirectory, slot), new AccountTiming());
        }

        private static async Task<string> RegisterAndSignInAsync(LiveAccountServices account)
        {
            string username = "acct_" + Guid.NewGuid().ToString("N").Substring(0, 12);
            RegistrationForm form = new RegistrationForm(username, username + "@live.local", new Password(Password), new Password(Password));
            Require(await account.Registration.RegisterAsync(form));
            SignInOutcome signedIn = await account.Session.SignInAsync(username, new Password(Password));
            Assert.That(signedIn.Kind, Is.EqualTo(SignInOutcomeKind.SignedIn), signedIn.ToString());
            return username;
        }

        private static async Task AssertTwelveCardsAreRefusedAsync(LiveAccountServices account)
        {
            CardId[] twelve = SpellDeckCards().Take(12).ToArray();
            AccountCallOutcome<PlayerDeck, DeckRefusal> refused = await account.Decks.CreateAsync(new DeckDraft("Curto", twelve));
            WrongDeckSize size = refused.Refusal!.Reasons.OfType<DeckListRejected>().Single().Problems.OfType<WrongDeckSize>().Single();
            Assert.That((size.Found, size.Required), Is.EqualTo((12L, 40L)));
        }

        /// <summary>O deck de feitiços de scripts/smoke_match.py (spell_deck_id): 1 a 8 com 3 cópias, 9 uma vez, 1001 a 1005 com 3 cópias.</summary>
        private static CardId[] SpellDeckCards()
        {
            return Enumerable.Range(1, 8).SelectMany(card => Enumerable.Repeat(card, 3))
                .Append(9)
                .Concat(Enumerable.Range(1001, 5).SelectMany(card => Enumerable.Repeat(card, 3)))
                .Select(card => new CardId(card))
                .ToArray();
        }

        private static TValue Require<TValue, TRefusal>(AccountCallOutcome<TValue, TRefusal> outcome) where TRefusal : class
        {
            if (!outcome.IsSuccess)
                Assert.Fail($"expected success, got refusal '{outcome.Refusal}' and failure '{outcome.Failure}'");

            return outcome.Value;
        }
    }
}
#endif
