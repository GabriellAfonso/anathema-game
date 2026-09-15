#nullable enable
using System;
using Anathema.Net.Core;

namespace Anathema.Net.Facade
{
    /// <summary>
    /// O único dono do estado do app: troca o estado inteiro, registra e avisa. A geração sobe a cada saída,
    /// volta, expiração e descarte, para continuação assíncrona de um estado velho não aplicar nada
    /// (specs/005-presentation-facade/research.md, R4).
    /// </summary>
    internal sealed class ClientStages
    {
        private readonly IClientLog log;

        internal ClientStages(IClientLog log)
        {
            this.log = log ?? throw new ArgumentNullException(nameof(log), "log is null: expected the client log that records stage transitions");
            StageChanged = new EventFeed<ClientStageChange>("client_stage_changed", log);
            State = ClientState.SignedOut(SignedOutReason.Startup);
        }

        internal ClientState State { get; private set; }

        internal EventFeed<ClientStageChange> StageChanged { get; }

        internal int Generation { get; private set; }

        internal void Invalidate() => Generation++;

        internal bool Move(ClientState next, string cause)
        {
            ClientState required = next ?? throw new ArgumentNullException(nameof(next), $"next state for '{cause}' is null: expected a ClientState");
            if (required.SameAs(State))
                return false;

            ClientState previous = State;
            State = required;
            log.Info("client_stage", new LogField("previous", previous.Stage.ToString()), new LogField("current", required.Stage.ToString()),
                new LogField("cause", cause), new LogField("state", required.ToString()));
            StageChanged.Publish(new ClientStageChange(previous, required));
            return true;
        }

        internal void Replace(ClientState next)
        {
            ClientState required = next ?? throw new ArgumentNullException(nameof(next), "replacing state is null: expected a ClientState of the same stage");
            if (required.Stage != State.Stage)
                throw new InvalidOperationException($"replace from {State.Stage} to {required.Stage}: expected the same stage; use Move for a transition");

            State = required;
        }
    }
}
