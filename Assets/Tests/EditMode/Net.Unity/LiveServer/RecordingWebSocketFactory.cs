#nullable enable
using System;
using System.Globalization;
using System.IO;
using Anathema.Net.Core;

namespace Anathema.Net.Unity.Tests
{
    /// <summary>
    /// Fábrica de socket de teste que grava cada texto recebido em arquivos numerados, para virar fixture da
    /// partida (specs/004-match-session/research.md, R14). Embrulha a fábrica real sem mudar o socket: a
    /// produção não precisa de ponte de texto cru.
    /// </summary>
    internal sealed class RecordingWebSocketFactory : IWebSocketFactory
    {
        private const string TypeKey = "\"type\"";

        private readonly IWebSocketFactory inner;
        private readonly string directory;
        private int written;

        internal RecordingWebSocketFactory(IWebSocketFactory inner, string directory)
        {
            this.inner = inner ?? throw new ArgumentNullException(nameof(inner), "inner factory is null: expected the real socket factory");
            this.directory = directory ?? throw new ArgumentNullException(nameof(directory), "recording directory is null: expected a folder under Logs/match-recording");
        }

        internal int Written => written;

        public IWebSocket Create()
        {
            IWebSocket socket = inner.Create();
            socket.TextReceived += Record;
            return socket;
        }

        internal static string TypeOf(string frameText)
        {
            int key = frameText.IndexOf(TypeKey, StringComparison.Ordinal);
            int open = key < 0 ? -1 : frameText.IndexOf('"', frameText.IndexOf(':', key) + 1);
            int close = open < 0 ? -1 : frameText.IndexOf('"', open + 1);
            return close < 0 ? "unknown" : frameText.Substring(open + 1, close - open - 1);
        }

        private void Record(string text)
        {
            Directory.CreateDirectory(directory);
            written++;
            string name = written.ToString("D4", CultureInfo.InvariantCulture) + "-" + TypeOf(text) + ".json";
            File.WriteAllText(Path.Combine(directory, name), text);
        }
    }
}
