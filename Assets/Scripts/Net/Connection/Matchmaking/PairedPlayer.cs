#nullable enable
using System;
using Anathema.Net.Core;

namespace Anathema.Net.Connection
{
    /// <summary>Um dos dois jogadores de um pareamento, com o que a tela de versus mostra.</summary>
    /// <example>
    /// <code>
    /// opponentName.text = pairing.Opponent.Nickname;
    /// </code>
    /// </example>
    public sealed class PairedPlayer
    {
        /// <summary>Jogador pareado.</summary>
        /// <example><code>PairedPlayer player = new PairedPlayer(new UserId(7), "one", "default", 1);</code></example>
        public PairedPlayer(UserId user, string nickname, string icon, long level)
        {
            User = user;
            Nickname = nickname ?? throw new ArgumentNullException(nameof(nickname), $"nickname of {user} is null: expected the nickname from match_found");
            Icon = icon ?? throw new ArgumentNullException(nameof(icon), $"icon of {user} is null: expected the icon key from match_found");
            Level = level;
        }

        /// <summary>Identidade do jogador.</summary>
        /// <example><code>bool isMe = pairing.Self.User == session.Self;</code></example>
        public UserId User { get; }

        /// <summary>Apelido.</summary>
        /// <example><code>string nickname = player.Nickname;</code></example>
        public string Nickname { get; }

        /// <summary>Chave do ícone.</summary>
        /// <example><code>string icon = player.Icon;</code></example>
        public string Icon { get; }

        /// <summary>Nível.</summary>
        /// <example><code>long level = player.Level;</code></example>
        public long Level { get; }

        /// <summary>Lê um jogador do objeto <c>self</c> ou <c>opponent</c>.</summary>
        /// <example><code>PairedPlayer opponent = PairedPlayer.Read(payload.ReadObject("opponent"));</code></example>
        public static PairedPlayer Read(IPayloadReader player)
        {
            return new PairedPlayer(player.ReadUserId("user_id"), player.ReadText("nickname"), player.ReadText("icon"), player.ReadInteger("level"));
        }
    }
}
