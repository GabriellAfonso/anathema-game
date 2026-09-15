#nullable enable
using System;
using Anathema.Net.Connection;
using Anathema.Net.Core;
using Anathema.Net.Match;

namespace Anathema.Net.Facade
{
    /// <summary>
    /// O estado do app num instante, imutável e trocado inteiro a cada transição. Cada estágio preenche só o que a
    /// tela dele precisa; o resto é nulo (specs/005-presentation-facade/data-model.md, "ClientState").
    /// </summary>
    /// <example>
    /// <code>
    /// ClientState state = client.State;
    /// if (state.Stage == ClientStage.Paired) ShowVersus(state.Pairing!.Self, state.Pairing.Opponent);
    /// </code>
    /// </example>
    public sealed class ClientState
    {
        private ClientState(ClientStage stage, UserId? self)
        {
            Stage = stage;
            Self = self;
        }

        /// <summary>O estágio.</summary>
        /// <example><code>ClientStage stage = state.Stage;</code></example>
        public ClientStage Stage { get; }

        /// <summary>O próprio jogador; nulo só em <see cref="ClientStage.SignedOut"/>.</summary>
        /// <example><code>UserId? self = state.Self;</code></example>
        public UserId? Self { get; }

        /// <summary>Por que está deslogado; só em <see cref="ClientStage.SignedOut"/>.</summary>
        /// <example><code>bool expired = state.SignedOutReason == SignedOutReason.SessionExpired;</code></example>
        public SignedOutReason? SignedOutReason { get; private set; }

        /// <summary>O pareamento, de <see cref="ClientStage.Paired"/> até o fim ou a indisponibilidade.</summary>
        /// <example><code>string rival = state.Pairing?.Opponent.Nickname ?? string.Empty;</code></example>
        public MatchPairing? Pairing { get; private set; }

        /// <summary>A partida corrente: depois de o catálogo carregar em <see cref="ClientStage.Paired"/>, em partida e no fim.</summary>
        /// <example><code>PlayerView? view = state.Match?.Mirror.Current;</code></example>
        public LiveMatch? Match { get; private set; }

        /// <summary>O resultado; só em <see cref="ClientStage.MatchFinished"/>.</summary>
        /// <example><code>bool won = state.Result?.Won ?? false;</code></example>
        public MatchResult? Result { get; private set; }

        /// <summary>Por que a partida não está disponível; só em <see cref="ClientStage.MatchUnavailable"/>.</summary>
        /// <example><code>string text = state.Unavailable?.PlayerText ?? string.Empty;</code></example>
        public MatchUnavailable? Unavailable { get; private set; }

        /// <summary>Forma para log.</summary>
        /// <example><code>string text = state.ToString(); // InMatch self=user_id=7 match_id=m-1</code></example>
        public override string ToString()
        {
            string self = Self.HasValue ? " self=" + Self.Value : string.Empty;
            string reason = SignedOutReason.HasValue ? " reason=" + SignedOutReason.Value : string.Empty;
            string match = Pairing == null ? string.Empty : " match_id=" + Pairing.Match.Value;
            return Stage + self + reason + match;
        }

        internal static ClientState SignedOut(SignedOutReason reason) => new ClientState(ClientStage.SignedOut, null) { SignedOutReason = reason };

        internal static ClientState SignedIn(UserId self) => new ClientState(ClientStage.SignedIn, self);

        internal static ClientState Searching(UserId self) => new ClientState(ClientStage.Searching, self);

        internal static ClientState Paired(UserId self, MatchPairing pairing) => new ClientState(ClientStage.Paired, self) { Pairing = Require(pairing, nameof(pairing)) };

        internal static ClientState InMatch(UserId self, MatchPairing pairing, LiveMatch match)
        {
            return new ClientState(ClientStage.InMatch, self) { Pairing = Require(pairing, nameof(pairing)), Match = Require(match, nameof(match)) };
        }

        internal static ClientState MatchFinished(UserId self, MatchPairing pairing, LiveMatch match, MatchResult result)
        {
            return new ClientState(ClientStage.MatchFinished, self) { Pairing = Require(pairing, nameof(pairing)), Match = Require(match, nameof(match)), Result = Require(result, nameof(result)) };
        }

        internal static ClientState MatchUnavailable(UserId self, MatchPairing pairing, MatchUnavailable unavailable)
        {
            return new ClientState(ClientStage.MatchUnavailable, self) { Pairing = Require(pairing, nameof(pairing)), Unavailable = Require(unavailable, nameof(unavailable)) };
        }

        internal ClientState WithOpenedMatch(LiveMatch match)
        {
            RequireStage(ClientStage.Paired, nameof(WithOpenedMatch));
            return new ClientState(Stage, Self) { Pairing = Pairing, Match = Require(match, nameof(match)) };
        }

        internal ClientState WithResult(MatchResult result)
        {
            RequireStage(ClientStage.MatchFinished, nameof(WithResult));
            return new ClientState(Stage, Self) { Pairing = Pairing, Match = Match, Result = Require(result, nameof(result)) };
        }

        internal bool SameAs(ClientState other)
        {
            return Stage == other.Stage && Nullable.Equals(Self, other.Self) && Nullable.Equals(SignedOutReason, other.SignedOutReason)
                && ReferenceEquals(Pairing, other.Pairing) && ReferenceEquals(Match, other.Match)
                && ReferenceEquals(Result, other.Result) && ReferenceEquals(Unavailable, other.Unavailable);
        }

        private void RequireStage(ClientStage expected, string operation)
        {
            if (Stage != expected)
                throw new InvalidOperationException($"{operation} on a {Stage} state: expected a {expected} state");
        }

        private static T Require<T>(T? value, string name) where T : class
        {
            return value ?? throw new ArgumentNullException(name, $"{name} of the client state is null: expected the value that stage carries");
        }
    }
}
