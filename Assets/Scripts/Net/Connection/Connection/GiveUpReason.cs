#nullable enable
using System;

namespace Anathema.Net.Connection
{
    /// <summary>
    /// Por que a conexão desistiu, com o detalhe que cada motivo carrega e o texto para o jogador
    /// (FR-013). Quem decide o que fazer compara <see cref="Kind"/>; o texto é só para mostrar.
    /// </summary>
    /// <example>
    /// <code>
    /// if (status.GiveUp!.Kind == GiveUpKind.MatchRefused) overlay.Show(status.GiveUp.PlayerText());
    /// </code>
    /// </example>
    public sealed class GiveUpReason : IEquatable<GiveUpReason>
    {
        private GiveUpReason(GiveUpKind kind, MatchRefusalDetail? match, int? attempts)
        {
            Kind = kind;
            Match = match;
            Attempts = attempts;
        }

        /// <summary>O motivo.</summary>
        /// <example><code>GiveUpKind kind = reason.Kind;</code></example>
        public GiveUpKind Kind { get; }

        /// <summary>Qual gate recusou; só em <see cref="GiveUpKind.MatchRefused"/>.</summary>
        /// <example><code>MatchRefusalDetail? detail = reason.Match;</code></example>
        public MatchRefusalDetail? Match { get; }

        /// <summary>Quantas tentativas ou recusas levaram à desistência; só nos motivos que contam.</summary>
        /// <example><code>int? attempts = reason.Attempts;</code></example>
        public int? Attempts { get; }

        /// <summary>Não há sessão autenticada.</summary>
        /// <example><code>GiveUpReason reason = GiveUpReason.NoSession();</code></example>
        public static GiveUpReason NoSession() => new GiveUpReason(GiveUpKind.NoSession, null, null);

        /// <summary>A sessão expirou.</summary>
        /// <example><code>GiveUpReason reason = GiveUpReason.SessionExpired();</code></example>
        public static GiveUpReason SessionExpired() => new GiveUpReason(GiveUpKind.SessionExpired, null, null);

        /// <summary>Token recusado <paramref name="refusals"/> vezes seguidas.</summary>
        /// <example><code>GiveUpReason reason = GiveUpReason.TokenRefusedRepeatedly(3);</code></example>
        public static GiveUpReason TokenRefusedRepeatedly(int refusals) => new GiveUpReason(GiveUpKind.TokenRefusedRepeatedly, null, RequirePositive(refusals, nameof(refusals)));

        /// <summary>O gate da partida recusou.</summary>
        /// <example><code>GiveUpReason reason = GiveUpReason.MatchRefused(MatchRefusalDetail.MatchNotFound);</code></example>
        public static GiveUpReason MatchRefused(MatchRefusalDetail detail) => new GiveUpReason(GiveUpKind.MatchRefused, detail, null);

        /// <summary>As <paramref name="attempts"/> tentativas falharam.</summary>
        /// <example><code>GiveUpReason reason = GiveUpReason.AttemptsExhausted(5);</code></example>
        public static GiveUpReason AttemptsExhausted(int attempts) => new GiveUpReason(GiveUpKind.AttemptsExhausted, null, RequirePositive(attempts, nameof(attempts)));

        /// <summary>Texto para o jogador ler.</summary>
        /// <example><code>label.text = reason.PlayerText();</code></example>
        public string PlayerText()
        {
            return Kind switch
            {
                GiveUpKind.NoSession => "Você não está conectado. Entre de novo.",
                GiveUpKind.SessionExpired => "Sua sessão expirou. Entre de novo.",
                GiveUpKind.TokenRefusedRepeatedly => "O servidor recusou seu acesso. Entre de novo.",
                GiveUpKind.MatchRefused => MatchText(),
                _ => "Não foi possível falar com o servidor. Verifique sua conexão.",
            };
        }

        /// <inheritdoc />
        public bool Equals(GiveUpReason? other) => other != null && Kind == other.Kind && Match == other.Match && Attempts == other.Attempts;

        /// <inheritdoc />
        public override bool Equals(object? obj) => Equals(obj as GiveUpReason);

        /// <inheritdoc />
        public override int GetHashCode() => ((int)Kind * 397) ^ (Match.HasValue ? (int)Match.Value + 1 : 0) ^ (Attempts ?? 0);

        /// <inheritdoc />
        public override string ToString() => $"kind={Kind} match_detail={(Match.HasValue ? Match.Value.ToString() : "none")} attempts={(Attempts.HasValue ? Attempts.Value.ToString() : "none")}";

        private string MatchText()
        {
            return Match switch
            {
                MatchRefusalDetail.NoMatchId => "A partida não foi informada.",
                MatchRefusalDetail.NotAParticipant => "Você não participa desta partida.",
                MatchRefusalDetail.MatchNotFound => "Esta partida não existe mais.",
                _ => "A entrada na partida foi recusada.",
            };
        }

        private static int RequirePositive(int value, string name)
        {
            if (value < 1)
                throw new ArgumentOutOfRangeException(name, value, $"{name} is {value}: expected 1 or more");

            return value;
        }
    }
}
