#nullable enable
using Anathema.Net.Core;

namespace Anathema.Net.Account
{
    /// <summary>
    /// O próprio perfil de <c>GET /players/me/</c>. A rota não traz <c>user_id</c>: a identidade
    /// do jogador vem do token (<see cref="AccountSession.Self"/>).
    /// </summary>
    /// <example>
    /// <code>
    /// nickname.text = profile.Nickname;
    /// </code>
    /// </example>
    public sealed class OwnProfile
    {
        /// <summary>Cria o perfil com os campos da rota.</summary>
        /// <example><code>OwnProfile profile = new OwnProfile("one", "default_icon", 1, 0, 0, 0);</code></example>
        public OwnProfile(string nickname, string icon, long level, long experiencePoints, long coins, long credits)
        {
            Nickname = nickname;
            Icon = icon;
            Level = level;
            ExperiencePoints = experiencePoints;
            Coins = coins;
            Credits = credits;
        }

        /// <summary>Apelido público.</summary>
        /// <example><code>string nickname = profile.Nickname;</code></example>
        public string Nickname { get; }

        /// <summary>Chave do ícone, resolvida pela apresentação.</summary>
        /// <example><code>string icon = profile.Icon;</code></example>
        public string Icon { get; }

        /// <summary>Nível.</summary>
        /// <example><code>long level = profile.Level;</code></example>
        public long Level { get; }

        /// <summary>Experiência acumulada.</summary>
        /// <example><code>long experience = profile.ExperiencePoints;</code></example>
        public long ExperiencePoints { get; }

        /// <summary>Moedas.</summary>
        /// <example><code>long coins = profile.Coins;</code></example>
        public long Coins { get; }

        /// <summary>Créditos.</summary>
        /// <example><code>long credits = profile.Credits;</code></example>
        public long Credits { get; }

        internal static OwnProfile Read(IPayloadReader body)
        {
            return new OwnProfile(body.ReadText("nickname"), body.ReadText("icon"), body.ReadInteger("level"),
                body.ReadInteger("experience_points"), body.ReadInteger("coins"), body.ReadInteger("credits"));
        }
    }
}
