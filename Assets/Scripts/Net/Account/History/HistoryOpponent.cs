#nullable enable
using Anathema.Net.Core;

namespace Anathema.Net.Account
{
    /// <summary>O oponente de uma linha do histórico, com os campos públicos do perfil.</summary>
    /// <example>
    /// <code>
    /// opponentText.text = row.Opponent?.Nickname ?? "(perfil apagado)";
    /// </code>
    /// </example>
    public sealed class HistoryOpponent
    {
        private HistoryOpponent(IPayloadReader opponent)
        {
            User = opponent.ReadUserId("user_id");
            Nickname = opponent.ReadText("nickname");
            Icon = opponent.ReadText("icon");
            Level = opponent.ReadInteger("level");
        }

        /// <summary>Identidade do oponente.</summary>
        /// <example><code>UserId opponent = row.Opponent!.User;</code></example>
        public UserId User { get; }

        /// <summary>Apelido.</summary>
        /// <example><code>string nickname = opponent.Nickname;</code></example>
        public string Nickname { get; }

        /// <summary>Chave do ícone.</summary>
        /// <example><code>string icon = opponent.Icon;</code></example>
        public string Icon { get; }

        /// <summary>Nível.</summary>
        /// <example><code>long level = opponent.Level;</code></example>
        public long Level { get; }

        internal static HistoryOpponent Read(IPayloadReader opponent) => new HistoryOpponent(opponent);
    }
}
