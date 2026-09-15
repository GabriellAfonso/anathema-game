#nullable enable

namespace Anathema.Net.Facade
{
    /// <summary>Resultado de <see cref="ClientQueue.Join"/>: como terminou e em que estágio o app ficou.</summary>
    /// <example>
    /// <code>
    /// QueueJoinResult result = client.Queue.Join(deck);
    /// playButton.interactable = result.Kind != QueueJoinKind.Started;
    /// </code>
    /// </example>
    public sealed class QueueJoinResult
    {
        private QueueJoinResult(QueueJoinKind kind, ClientStage stage)
        {
            Kind = kind;
            Stage = stage;
        }

        /// <summary>Como terminou.</summary>
        /// <example><code>QueueJoinKind kind = result.Kind;</code></example>
        public QueueJoinKind Kind { get; }

        /// <summary>O estágio depois da chamada.</summary>
        /// <example><code>ClientStage now = result.Stage;</code></example>
        public ClientStage Stage { get; }

        /// <summary>Forma para log.</summary>
        /// <example><code>string text = result.ToString(); // NotApplicable stage=InMatch</code></example>
        public override string ToString() => $"{Kind} stage={Stage}";

        internal static QueueJoinResult Of(QueueJoinKind kind, ClientStage stage) => new QueueJoinResult(kind, stage);
    }
}
