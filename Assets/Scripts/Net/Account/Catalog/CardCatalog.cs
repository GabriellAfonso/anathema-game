#nullable enable
using System;
using System.Threading.Tasks;
using Anathema.Net.Core;

namespace Anathema.Net.Account
{
    /// <summary>
    /// O catálogo de <c>GET /game/cards/</c>, buscado no máximo uma vez por geração da sessão
    /// (FR-028). Buscas concorrentes compartilham o mesmo pedido; falha não fica guardada; sair,
    /// expirar ou entrar de novo descarta o guardado, porque muda a geração.
    /// </summary>
    /// <example>
    /// <code>
    /// AccountCallOutcome&lt;LoadedCatalog, UnrecognizedRefusal&gt; catalog = await cards.LoadAsync();
    /// if (catalog.IsSuccess) Show(catalog.Value.Find(card));
    /// </code>
    /// </example>
    public sealed class CardCatalog
    {
        private readonly AuthenticatedHttpClient client;
        private readonly AccountSession session;
        private readonly AccountRoutes routes;
        private readonly AccountResponseReader responses;
        private readonly CatalogReader reader;
        private LoadedCatalog? loaded;
        private Task<AccountCallOutcome<LoadedCatalog, UnrecognizedRefusal>>? inFlight;
        private int inFlightGeneration;

        /// <summary>Cria o catálogo da sessão.</summary>
        /// <example><code>CardCatalog cards = new CardCatalog(client, session, codec, log, routes);</code></example>
        public CardCatalog(AuthenticatedHttpClient client, AccountSession session, IProtocolCodec codec, IClientLog log, AccountRoutes routes)
        {
            this.client = client ?? throw new ArgumentNullException(nameof(client), "catalog client is null: expected the authenticated http client");
            this.session = session ?? throw new ArgumentNullException(nameof(session), "catalog session is null: expected the account session");
            this.routes = routes ?? throw new ArgumentNullException(nameof(routes), "catalog routes is null: expected the account routes");
            responses = new AccountResponseReader(codec, log);
            reader = new CatalogReader(log);
        }

        /// <summary>O catálogo guardado da geração atual, ou uma busca (compartilhada se já houver uma).</summary>
        /// <example><code>AccountCallOutcome&lt;LoadedCatalog, UnrecognizedRefusal&gt; catalog = await cards.LoadAsync();</code></example>
        public Task<AccountCallOutcome<LoadedCatalog, UnrecognizedRefusal>> LoadAsync()
        {
            int generation = session.Generation;
            if (loaded != null && loaded.Generation == generation)
                return Task.FromResult(AccountCallOutcome<LoadedCatalog, UnrecognizedRefusal>.Success(loaded));

            if (inFlight != null && inFlightGeneration == generation)
                return inFlight;

            Task<AccountCallOutcome<LoadedCatalog, UnrecognizedRefusal>> running = FetchAsync(generation);
            // Transporte que completa na hora já terminou aqui; guardar a tarefa concluída prenderia a próxima busca.
            inFlight = running.IsCompleted ? null : running;
            inFlightGeneration = generation;
            return running;
        }

        private async Task<AccountCallOutcome<LoadedCatalog, UnrecognizedRefusal>> FetchAsync(int generation)
        {
            AuthenticatedCallResult result;
            try
            {
                result = await client.SendAsync(AuthenticatedRequest.Get(routes.Cards)).ConfigureAwait(false);
            }
            finally
            {
                if (inFlightGeneration == generation)
                    inFlight = null;
            }

            AccountCallOutcome<LoadedCatalog, UnrecognizedRefusal> outcome = Interpret(result, generation);
            if (outcome.IsSuccess && generation == session.Generation)
                loaded = outcome.Value;

            return outcome;
        }

        private AccountCallOutcome<LoadedCatalog, UnrecognizedRefusal> Interpret(AuthenticatedCallResult result, int generation)
        {
            if (result.Failure != null)
                return AccountCallOutcome<LoadedCatalog, UnrecognizedRefusal>.Failed(result.Failure);

            if (result.Status != 200)
                return AccountCallOutcome<LoadedCatalog, UnrecognizedRefusal>.Refused(new UnrecognizedRefusal(result.Status, result.BodyText));

            return responses.ReadSuccess<LoadedCatalog, UnrecognizedRefusal>(result, body => new LoadedCatalog(reader.Read(body), generation));
        }
    }
}
