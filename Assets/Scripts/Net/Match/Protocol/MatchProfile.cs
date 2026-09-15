#nullable enable
using System;
using Anathema.Net.Core;

namespace Anathema.Net.Match
{
    /// <summary>
    /// O que um jogador mostra na partida: <c>profile</c> de cada lado da visão
    /// (<c>backend/server/apps/players/services/player_queries.py</c>, <c>PlayerData</c>).
    /// </summary>
    /// <example>
    /// <code>
    /// nameLabel.text = view.Opponent.Profile.Nickname;
    /// </code>
    /// </example>
    public sealed class MatchProfile
    {
        /// <summary>Perfil com os quatro campos públicos.</summary>
        /// <example><code>MatchProfile profile = new MatchProfile(new UserId(7), "gabriel", "default", 1);</code></example>
        public MatchProfile(UserId user, string nickname, string icon, long level)
        {
            User = user;
            Nickname = nickname ?? throw new ArgumentNullException(nameof(nickname), $"nickname of {user} is null: expected the profile nickname");
            Icon = icon ?? throw new ArgumentNullException(nameof(icon), $"icon of {user} is null: expected the profile icon key");
            Level = level;
        }

        /// <summary>A identidade do jogador.</summary>
        /// <example><code>UserId self = profile.User;</code></example>
        public UserId User { get; }

        /// <summary>Apelido.</summary>
        /// <example><code>string name = profile.Nickname;</code></example>
        public string Nickname { get; }

        /// <summary>Chave do ícone.</summary>
        /// <example><code>string icon = profile.Icon;</code></example>
        public string Icon { get; }

        /// <summary>Nível.</summary>
        /// <example><code>long level = profile.Level;</code></example>
        public long Level { get; }

        /// <summary>Lê o objeto <c>profile</c>.</summary>
        /// <example><code>MatchProfile profile = MatchProfile.Read(side.ReadObject("profile"));</code></example>
        internal static MatchProfile Read(IPayloadReader profile)
        {
            return new MatchProfile(profile.ReadUserId("user_id"), profile.ReadText("nickname"), profile.ReadText("icon"), profile.ReadInteger("level"));
        }
    }
}
