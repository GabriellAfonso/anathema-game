#nullable enable
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Anathema.Net.Account;
using Anathema.Net.Core;
using Anathema.Net.Facade;

namespace Anathema.Client.Proof
{
    /// <summary>Passos 1 a 6 de contracts/match-proof.md: contas, retomada sem senha, catálogo e decks.</summary>
    internal static class AccountProofSteps
    {
        private const string Password = "proof-match-123";
        private const int CatalogSize = 29;
        private const string SpellDeckName = "Fumaça";

        internal static async Task RunAsync(ProofRun run)
        {
            await SignUpAsync(run);
            await ResumeAsync(run);
            await LoadCatalogsAsync(run);
            await CheckStarterDecksAsync(run);
            await CreateSpellDecksAsync(run);
            await RefuseShortDeckAsync(run);
        }

        private static async Task SignUpAsync(ProofRun run)
        {
            foreach (ProofPlayer player in run.Players)
            {
                string username = $"proof_{player.Label.ToLowerInvariant()}_{run.Setup.Suffix}";
                RegistrationForm form = new RegistrationForm(username, username + "@proof.local", new Password(Password), new Password(Password));
                AccountCallOutcome<AccountCreated, RegistrationRefusal> created = await player.Client.Account.RegisterAsync(form);
                run.Expect("1", created.IsSuccess, "account created for " + username, ProofRun.ProblemOf(created));
                SignInOutcome signedIn = await player.Client.Account.SignInAsync(username, new Password(Password));
                run.Expect("1", signedIn.Kind == SignInOutcomeKind.SignedIn, "SignedIn for " + username, signedIn.ToString());
                player.User = signedIn.User;
            }

            await run.UntilAsync("1", "both clients SignedIn", () => run.AllAt(ClientStage.SignedIn));
            run.Passed("1", "cadastro e entrada dos dois");
        }

        private static async Task ResumeAsync(ProofRun run)
        {
            foreach (ProofPlayer player in run.Players)
            {
                player.Recompose();
                ResumeOutcome resumed = await player.Client.Account.ResumeAsync();
                run.Expect("2", resumed.Kind == ResumeOutcomeKind.Resumed && Equals(resumed.User, player.User), $"Resumed as {player.User}", resumed.ToString());
            }

            await run.UntilAsync("2", "both clients SignedIn after resuming", () => run.AllAt(ClientStage.SignedIn));
            run.Passed("2", "clientes compostos de novo nos mesmos slots e retomados sem senha");
        }

        private static async Task LoadCatalogsAsync(ProofRun run)
        {
            foreach (ProofPlayer player in run.Players)
            {
                AccountCallOutcome<LoadedCatalog, UnrecognizedRefusal> loaded = await player.Client.Catalog.LoadAsync();
                string received = loaded.IsSuccess ? loaded.Value.Cards.Count + " cards" : ProofRun.ProblemOf(loaded);
                run.Expect("3", loaded.IsSuccess && loaded.Value.Cards.Count == CatalogSize, $"{CatalogSize} cards", received);
                player.Catalog = loaded.Value;
            }

            run.Passed("3", $"catálogo com {CatalogSize} cartas");
        }

        private static async Task CheckStarterDecksAsync(ProofRun run)
        {
            foreach (ProofPlayer player in run.Players)
            {
                AccountCallOutcome<IReadOnlyList<PlayerDeck>, DeckRefusal> decks = await player.Client.Decks.ListAsync();
                run.Expect("4", decks.IsSuccess && decks.Value.Count > 0, "the starter deck", decks.IsSuccess ? "no deck" : ProofRun.ProblemOf(decks));
                PlayerDeck starter = decks.Value[0];
                int spells = starter.Cards.Count(card => player.Catalog!.Find(card).Spell != null);
                run.Expect("4", spells == 0, "starter deck without SpellCard", $"{spells} spells in {starter.Deck}");
            }

            run.Passed("4", "deck inicial sem feitiço");
        }

        private static async Task CreateSpellDecksAsync(ProofRun run)
        {
            foreach (ProofPlayer player in run.Players)
            {
                AccountCallOutcome<PlayerDeck, DeckRefusal> created = await player.Client.Decks.CreateAsync(new DeckDraft(SpellDeckName, SpellDeckCards()));
                run.Expect("5", created.IsSuccess, "spell deck created", ProofRun.ProblemOf(created));
                player.SpellDeck = created.Value.Deck;
            }

            run.Passed("5", "deck com feitiços do smoke_match.py criado nos dois");
        }

        private static async Task RefuseShortDeckAsync(ProofRun run)
        {
            IReadOnlyList<CardId> twelve = SpellDeckCards().Take(12).ToArray();
            AccountCallOutcome<PlayerDeck, DeckRefusal> refused = await run.P1.Client.Decks.CreateAsync(new DeckDraft("Curto", twelve));
            bool wrongSize = refused.Refusal != null && refused.Refusal.Reasons.OfType<DeckListRejected>().SelectMany(reason => reason.Problems).OfType<WrongDeckSize>().Any();
            run.Expect("6", wrongSize, "DeckRefusal with WrongDeckSize", ProofRun.ProblemOf(refused));
            run.Passed("6", "deck de 12 cartas recusado com wrong_deck_size");
        }

        /// <summary>
        /// O deck com feitiços de <c>spell_deck_id</c> em <c>backend/scripts/smoke_match.py</c>: o deck
        /// inicial não tem feitiço nenhum, e sem isto a partida nunca lança um.
        /// </summary>
        private static IReadOnlyList<CardId> SpellDeckCards()
        {
            IEnumerable<long> units = Enumerable.Range(1, 8).SelectMany(card => Enumerable.Repeat((long)card, 3)).Append(9);
            IEnumerable<long> spells = Enumerable.Range(1001, 5).SelectMany(card => Enumerable.Repeat((long)card, 3));
            return units.Concat(spells).Select(card => new CardId(card)).ToArray();
        }
    }
}
