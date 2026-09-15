#nullable enable
using Anathema.Net.Unity;

namespace Anathema.Client.Proof
{
    /// <summary>O backend local de <c>docker compose up</c>, com os caminhos do <c>AppConfig_Dev</c>, para a prova no editor.</summary>
    /// <example><code>ProofSetup setup = new ProofSetup(LocalServerRoutes.Create(), ClientComposition.CreateConsoleLog());</code></example>
    public static class LocalServerRoutes
    {
        /// <summary>Host e porta do backend local.</summary>
        /// <example><code>string host = LocalServerRoutes.HostAndPort; // 127.0.0.1:8000</code></example>
        public const string HostAndPort = "127.0.0.1:8000";

        /// <summary>As rotas HTTP e de socket do backend local, sem TLS.</summary>
        /// <example><code>ServerRoutes routes = LocalServerRoutes.Create();</code></example>
        public static ServerRoutes Create() => ServerRoutes.ForHost("http://" + HostAndPort, "ws://" + HostAndPort);
    }
}
