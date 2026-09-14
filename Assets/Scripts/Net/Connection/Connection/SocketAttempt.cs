#nullable enable
using System;
using System.Threading.Tasks;
using Anathema.Net.Core;

namespace Anathema.Net.Connection
{
    /// <summary>
    /// Um socket físico da conexão lógica: abre, decodifica, lembra os frames de gate, avisa a prova
    /// de sessão e classifica o fim. Ao ser descartado desliga os próprios handlers antes de fechar, e
    /// o fechamento tardio de um socket velho nunca chega a quem reconecta
    /// (specs/003-authenticated-socket-queue/research.md, R6).
    /// </summary>
    internal sealed class SocketAttempt
    {
        private readonly IWebSocket socket;
        private readonly IProtocolCodec codec;
        private readonly IClientLog log;
        private bool sawAuthDenied;
        private bool sawMatchDenied;
        private bool proven;
        private bool finished;

        internal SocketAttempt(IWebSocket socket, IProtocolCodec codec, IClientLog log)
        {
            this.socket = socket;
            this.codec = codec;
            this.log = log;
        }

        /// <summary>Handshake concluído.</summary>
        internal event Action? Opened;

        /// <summary>Primeiro frame que não é negação de gate: o servidor aceitou este socket.</summary>
        internal event Action? Proven;

        /// <summary>Frame decodificado, com o texto cru.</summary>
        internal event Action<ServerFrame, string>? FrameArrived;

        /// <summary>O socket terminou; uma vez só.</summary>
        internal event Action<SocketEnd>? Ended;

        internal bool IsOpen { get; private set; }

        internal void Open(Uri url)
        {
            socket.Opened += OnOpened;
            socket.TextReceived += OnText;
            socket.Closed += OnClosed;
            socket.Open(url);
        }

        internal Task<SocketSendOutcome> SendTextAsync(string text) => socket.SendTextAsync(text);

        internal void Discard()
        {
            if (finished)
                return;

            Finish();
            socket.Close();
        }

        private void OnOpened()
        {
            if (finished)
                return;

            IsOpen = true;
            Opened?.Invoke();
        }

        private void OnText(string text)
        {
            DecodeOutcome<ServerFrame> decoded = codec.Decode(text);
            if (!decoded.IsValid)
            {
                log.Warning("connection_frame_invalid", new LogField("kind", decoded.Failure.Kind.ToString()), new LogField("path", decoded.Failure.Path));
                return;
            }

            NoteGate(decoded.Value);
            ProveUnlessGate(decoded.Value);
            if (!finished)
                FrameArrived?.Invoke(decoded.Value, text);
        }

        private void NoteGate(ServerFrame frame)
        {
            if (frame is AuthDeniedFrame)
                sawAuthDenied = true;
            else if (frame is MatchDeniedFrame)
                sawMatchDenied = true;
        }

        private void ProveUnlessGate(ServerFrame frame)
        {
            // Pong também prova: ele só sai depois dos gates (contrato 013), e o socket de fila não
            // recebe mais nada até o match_found.
            if (proven || finished || frame is AuthDeniedFrame || frame is MatchDeniedFrame)
                return;

            proven = true;
            Proven?.Invoke();
        }

        private void OnClosed(SocketClosure closure)
        {
            if (finished)
                return;

            Finish();
            Ended?.Invoke(SocketEndClassifier.Classify(sawAuthDenied, sawMatchDenied, closure));
        }

        private void Finish()
        {
            finished = true;
            IsOpen = false;
            socket.Opened -= OnOpened;
            socket.TextReceived -= OnText;
            socket.Closed -= OnClosed;
        }
    }
}
