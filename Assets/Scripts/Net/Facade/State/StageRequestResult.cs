#nullable enable

namespace Anathema.Net.Facade
{
    /// <summary>
    /// Resultado de uma operação que só vale em certos estágios: diz se foi aplicada e em que estágio o app ficou.
    /// Chamada fora de hora não lança nem manda nada (FR-008).
    /// </summary>
    /// <example>
    /// <code>
    /// StageRequestResult result = client.ReturnToLobby();
    /// if (!result.Applied) log.Warning("return_ignored", new LogField("stage", result.Stage.ToString()));
    /// </code>
    /// </example>
    public sealed class StageRequestResult
    {
        private StageRequestResult(bool applied, ClientStage stage)
        {
            Applied = applied;
            Stage = stage;
        }

        /// <summary>A operação foi aplicada.</summary>
        /// <example><code>bool done = result.Applied;</code></example>
        public bool Applied { get; }

        /// <summary>O estágio depois da chamada.</summary>
        /// <example><code>ClientStage now = result.Stage;</code></example>
        public ClientStage Stage { get; }

        /// <summary>Forma para log.</summary>
        /// <example><code>string text = result.ToString(); // applied=false stage=SignedIn</code></example>
        public override string ToString() => $"applied={(Applied ? "true" : "false")} stage={Stage}";

        internal static StageRequestResult Done(ClientStage stage) => new StageRequestResult(true, stage);

        internal static StageRequestResult NotApplicable(ClientStage stage) => new StageRequestResult(false, stage);
    }
}
