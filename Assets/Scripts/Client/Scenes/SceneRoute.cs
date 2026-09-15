#nullable enable
using Anathema.Net.Facade;

namespace Anathema.Client.Scenes
{
    /// <summary>
    /// Qual cena mostra cada estágio do app. Partida indisponível não troca de cena: não há tela para ela ainda, e
    /// a cena atual continua (specs/005-presentation-facade/research.md, R9).
    /// </summary>
    /// <example><code>string? scene = SceneRoute.For(client.State.Stage);</code></example>
    public static class SceneRoute
    {
        /// <summary>Tela de login.</summary>
        public const string Login = "LoginScene";

        /// <summary>Tela inicial, com o botão Jogar.</summary>
        public const string Home = "HomeScene";

        /// <summary>Tela do pareamento.</summary>
        public const string Versus = "VersusScene";

        /// <summary>Tela da partida.</summary>
        public const string Match = "MatchScene";

        /// <summary>A cena do estágio; nula quando o estágio não troca de cena.</summary>
        /// <example><code>string? scene = SceneRoute.For(ClientStage.Paired); // VersusScene</code></example>
        public static string? For(ClientStage stage) => stage switch
        {
            ClientStage.SignedOut => Login,
            ClientStage.SignedIn or ClientStage.Searching => Home,
            ClientStage.Paired => Versus,
            ClientStage.InMatch or ClientStage.MatchFinished => Match,
            _ => null,
        };
    }
}
