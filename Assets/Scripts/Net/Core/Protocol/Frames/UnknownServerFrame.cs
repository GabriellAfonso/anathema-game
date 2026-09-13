#nullable enable

namespace Anathema.Net.Core
{
    /// <summary>
    /// Frame com <c>type</c> que nenhuma feature registrou. Não é erro: o servidor pode
    /// mandar tipos novos antes de o cliente conhecê-los.
    /// </summary>
    /// <example>
    /// <code>
    /// if (frame is UnknownServerFrame unknown) log.Debug("frame_unknown", new LogField("type", unknown.MessageType));
    /// </code>
    /// </example>
    public sealed class UnknownServerFrame : ServerFrame
    {
        /// <summary>Cria o frame desconhecido com o texto exato de <c>type</c>.</summary>
        /// <example><code>ServerFrame unknown = new UnknownServerFrame("match_update");</code></example>
        public UnknownServerFrame(string messageType)
            : base(messageType)
        {
        }
    }
}
