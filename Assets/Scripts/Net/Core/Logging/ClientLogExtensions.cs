#nullable enable

namespace Anathema.Net.Core
{
    /// <summary>Atalhos por nível para <see cref="IClientLog"/>.</summary>
    /// <example>
    /// <code>
    /// log.Warning("binary_frame_ignored", new LogField("bytes", 12));
    /// </code>
    /// </example>
    public static class ClientLogExtensions
    {
        /// <summary>Registra no nível <see cref="ClientLogLevel.Debug"/>.</summary>
        /// <example><code>log.Debug("socket_text_received", new LogField("chars", 120));</code></example>
        public static void Debug(this IClientLog log, string eventName, params LogField[] fields)
        {
            log.Write(new ClientLogEntry(ClientLogLevel.Debug, eventName, fields));
        }

        /// <summary>Registra no nível <see cref="ClientLogLevel.Info"/>.</summary>
        /// <example><code>log.Info("socket_opened", new LogField("url", url));</code></example>
        public static void Info(this IClientLog log, string eventName, params LogField[] fields)
        {
            log.Write(new ClientLogEntry(ClientLogLevel.Info, eventName, fields));
        }

        /// <summary>Registra no nível <see cref="ClientLogLevel.Warning"/>.</summary>
        /// <example><code>log.Warning("server_host_rejected", new LogField("raw", raw));</code></example>
        public static void Warning(this IClientLog log, string eventName, params LogField[] fields)
        {
            log.Write(new ClientLogEntry(ClientLogLevel.Warning, eventName, fields));
        }

        /// <summary>Registra no nível <see cref="ClientLogLevel.Error"/>.</summary>
        /// <example><code>log.Error("main_thread_item_failed", new LogField("exception", ex.GetType().Name));</code></example>
        public static void Error(this IClientLog log, string eventName, params LogField[] fields)
        {
            log.Write(new ClientLogEntry(ClientLogLevel.Error, eventName, fields));
        }
    }
}
