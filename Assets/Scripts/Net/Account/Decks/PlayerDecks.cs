#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Anathema.Net.Core;

namespace Anathema.Net.Account
{
    /// <summary>
    /// Os decks do autenticado em <c>/players/decks/</c>, pelo cliente autenticado (FR-032 a FR-036;
    /// <c>backend/specs/011-deck-catalog-api/contracts/http_decks.md</c>). Qual recusa cada status vira
    /// depende da operação: só quem cita um deck recebe "não encontrado", só quem manda corpo lê o 400.
    /// </summary>
    /// <example>
    /// <code>
    /// AccountCallOutcome&lt;PlayerDeck, DeckRefusal&gt; created = await decks.CreateAsync(new DeckDraft("Agro", cards));
    /// </code>
    /// </example>
    public sealed class PlayerDecks
    {
        private readonly AuthenticatedHttpClient client;
        private readonly IProtocolCodec codec;
        private readonly AccountRoutes routes;
        private readonly AccountResponseReader responses;
        private readonly DeckRefusalReader refusals = new DeckRefusalReader();

        /// <summary>Cria o serviço de decks.</summary>
        /// <example><code>PlayerDecks decks = new PlayerDecks(client, codec, log, routes);</code></example>
        public PlayerDecks(AuthenticatedHttpClient client, IProtocolCodec codec, IClientLog log, AccountRoutes routes)
        {
            this.client = client ?? throw new ArgumentNullException(nameof(client), "decks client is null: expected the authenticated http client");
            this.codec = codec ?? throw new ArgumentNullException(nameof(codec), "decks codec is null: expected the project codec");
            this.routes = routes ?? throw new ArgumentNullException(nameof(routes), "decks routes is null: expected the account routes");
            responses = new AccountResponseReader(codec, log);
        }

        private enum Operation
        {
            List,
            Read,
            Create,
            Change,
            Delete,
        }

        /// <summary>Lista os decks; sem deck é lista vazia.</summary>
        /// <example><code>AccountCallOutcome&lt;IReadOnlyList&lt;PlayerDeck&gt;, DeckRefusal&gt; listed = await decks.ListAsync();</code></example>
        public Task<AccountCallOutcome<IReadOnlyList<PlayerDeck>, DeckRefusal>> ListAsync()
        {
            return SendAsync(AuthenticatedRequest.Get(routes.Decks), Operation.List, 200, result => ReadBody<IReadOnlyList<PlayerDeck>>(result, ReadList));
        }

        /// <summary>Lê um deck.</summary>
        /// <example><code>AccountCallOutcome&lt;PlayerDeck, DeckRefusal&gt; deck = await decks.ReadAsync(new DeckId(4));</code></example>
        public Task<AccountCallOutcome<PlayerDeck, DeckRefusal>> ReadAsync(DeckId deck)
        {
            return SendAsync(AuthenticatedRequest.Get(routes.Deck(deck)), Operation.Read, 200, result => ReadBody(result, PlayerDeck.Read));
        }

        /// <summary>Cria um deck com nome e lista, enviados como vieram.</summary>
        /// <example><code>AccountCallOutcome&lt;PlayerDeck, DeckRefusal&gt; created = await decks.CreateAsync(draft);</code></example>
        public Task<AccountCallOutcome<PlayerDeck, DeckRefusal>> CreateAsync(DeckDraft draft)
        {
            string body = codec.EncodeObject(writer => WriteDraft(writer, draft));
            return SendAsync(new AuthenticatedRequest("POST", routes.Decks, body), Operation.Create, 201, result => ReadBody(result, PlayerDeck.Read));
        }

        /// <summary>Renomeia e/ou troca a lista; manda só o que a mudança traz.</summary>
        /// <example><code>AccountCallOutcome&lt;PlayerDeck, DeckRefusal&gt; renamed = await decks.ChangeAsync(deck, new DeckChange(name: "Agro v2"));</code></example>
        public Task<AccountCallOutcome<PlayerDeck, DeckRefusal>> ChangeAsync(DeckId deck, DeckChange change)
        {
            string body = codec.EncodeObject(writer => WriteChange(writer, change));
            return SendAsync(new AuthenticatedRequest("PATCH", routes.Deck(deck), body), Operation.Change, 200, result => ReadBody(result, PlayerDeck.Read));
        }

        /// <summary>Apaga um deck; 204 não tem corpo.</summary>
        /// <example><code>AccountCallOutcome&lt;DeckDeleted, DeckRefusal&gt; deleted = await decks.DeleteAsync(deck);</code></example>
        public Task<AccountCallOutcome<DeckDeleted, DeckRefusal>> DeleteAsync(DeckId deck)
        {
            return SendAsync(new AuthenticatedRequest("DELETE", routes.Deck(deck)), Operation.Delete, 204, _ => AccountCallOutcome<DeckDeleted, DeckRefusal>.Success(DeckDeleted.Instance));
        }

        private async Task<AccountCallOutcome<T, DeckRefusal>> SendAsync<T>(AuthenticatedRequest request, Operation operation, int successStatus,
            Func<AuthenticatedCallResult, AccountCallOutcome<T, DeckRefusal>> onSuccess)
        {
            AuthenticatedCallResult result = await client.SendAsync(request).ConfigureAwait(false);
            if (result.Failure != null)
                return AccountCallOutcome<T, DeckRefusal>.Failed(result.Failure);

            return result.Status == successStatus ? onSuccess(result) : AccountCallOutcome<T, DeckRefusal>.Refused(RefusalOf(result, operation));
        }

        private AccountCallOutcome<T, DeckRefusal> ReadBody<T>(AuthenticatedCallResult result, Func<IPayloadReader, T> read)
        {
            return responses.ReadSuccess<T, DeckRefusal>(result, read);
        }

        private DeckRefusal RefusalOf(AuthenticatedCallResult result, Operation operation)
        {
            if (result.Status == 404 && operation != Operation.List && operation != Operation.Create)
                return DeckRefusal.Of(DeckNotFound.Instance);

            DecodeOutcome<DeckRefusal> badRequest = result.Status == 400 && (operation == Operation.Create || operation == Operation.Change)
                ? responses.Decode(result.BodyText, refusals.ReadBadRequest)
                : DecodeOutcome<DeckRefusal>.Invalid(new DecodeFailure(DecodeFailureKind.InvalidValue, string.Empty, $"http {result.Status} on {operation}: expected no typed refusal"));

            return badRequest.IsValid ? badRequest.Value : DeckRefusal.Of(new UnrecognizedDeckRefusal(new UnrecognizedRefusal(result.Status, result.BodyText)));
        }

        private static IReadOnlyList<PlayerDeck> ReadList(IPayloadReader body) => body.ReadObjectList("decks").Select(PlayerDeck.Read).ToArray();

        private static void WriteDraft(IPayloadWriter writer, DeckDraft draft)
        {
            writer.WriteText("name", draft.Name);
            writer.WriteCardIdList("card_ids", draft.Cards);
        }

        private static void WriteChange(IPayloadWriter writer, DeckChange change)
        {
            if (change.Name != null)
                writer.WriteText("name", change.Name);

            if (change.Cards != null)
                writer.WriteCardIdList("card_ids", change.Cards);
        }
    }
}
