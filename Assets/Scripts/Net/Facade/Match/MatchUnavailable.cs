#nullable enable
using System;
using Anathema.Net.Account;
using Anathema.Net.Connection;
using Anathema.Net.Core;

namespace Anathema.Net.Facade
{
    /// <summary>
    /// A partida pareada que não abriu, ou cuja conexão desistiu, com o texto para o jogador. Catálogo indisponível
    /// deixa de ser só log e vira estado explícito (specs/005-presentation-facade/spec.md, FR-014).
    /// </summary>
    /// <example>
    /// <code>
    /// MatchUnavailable unavailable = client.State.Unavailable!;
    /// errorLabel.text = unavailable.PlayerText;
    /// </code>
    /// </example>
    public sealed class MatchUnavailable
    {
        private const string CatalogText = "Não foi possível carregar as cartas da partida.";

        private MatchUnavailable(MatchId match, MatchUnavailableKind kind, GiveUpReason? giveUp, AccountCallFailure? catalogFailure, string playerText)
        {
            Match = match;
            Kind = kind;
            GiveUp = giveUp;
            CatalogFailure = catalogFailure;
            PlayerText = playerText;
        }

        /// <summary>A partida.</summary>
        /// <example><code>MatchId match = unavailable.Match;</code></example>
        public MatchId Match { get; }

        /// <summary>Por quê.</summary>
        /// <example><code>MatchUnavailableKind kind = unavailable.Kind;</code></example>
        public MatchUnavailableKind Kind { get; }

        /// <summary>O motivo da conexão, em recusa e desistência.</summary>
        /// <example><code>GiveUpKind? kind = unavailable.GiveUp?.Kind;</code></example>
        public GiveUpReason? GiveUp { get; }

        /// <summary>A falha do catálogo, quando foi de transporte ou fora do contrato.</summary>
        /// <example><code>AccountCallFailureKind? kind = unavailable.CatalogFailure?.Kind;</code></example>
        public AccountCallFailure? CatalogFailure { get; }

        /// <summary>Texto para o jogador ler.</summary>
        /// <example><code>errorLabel.text = unavailable.PlayerText;</code></example>
        public string PlayerText { get; }

        /// <summary>Forma para log.</summary>
        /// <example><code>string text = unavailable.ToString(); // CatalogUnavailable match_id=m-1</code></example>
        public override string ToString() => $"{Kind} match_id={Match.Value}{(GiveUp == null ? string.Empty : " " + GiveUp)}";

        internal static MatchUnavailable CatalogUnavailable(MatchId match, AccountCallFailure? failure)
        {
            return new MatchUnavailable(match, MatchUnavailableKind.CatalogUnavailable, null, failure, CatalogText);
        }

        internal static MatchUnavailable MatchRefused(MatchId match, GiveUpReason reason)
        {
            GiveUpReason required = Require(reason, match);
            return new MatchUnavailable(match, MatchUnavailableKind.MatchRefused, required, null, required.PlayerText());
        }

        internal static MatchUnavailable ConnectionGaveUp(MatchId match, GiveUpReason reason)
        {
            GiveUpReason required = Require(reason, match);
            return new MatchUnavailable(match, MatchUnavailableKind.ConnectionGaveUp, required, null, required.PlayerText());
        }

        private static GiveUpReason Require(GiveUpReason reason, MatchId match)
        {
            return reason ?? throw new ArgumentNullException(nameof(reason), $"give up reason of {match} is null: expected the reason from the match connection status");
        }
    }
}
