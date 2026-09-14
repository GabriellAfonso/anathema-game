#nullable enable
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Anathema.Net.Connection;
using Anathema.Net.Core;

namespace Anathema.Net.Match
{
    /// <summary>
    /// As jogadas da partida como operações. Só manda com a conexão conectada; fora disso devolve "não enviado" e
    /// nada fica guardado. Não consulta espelho, catálogo nem dica: um comando que o servidor vai recusar ainda é
    /// enviado, e a recusa chega pela sessão (<c>backend/specs/009-match-protocol/contracts/client_messages.md</c>).
    /// </summary>
    /// <example>
    /// <code>
    /// PlaySendResult result = await match.Commands.PlayUnit(card.Instance);
    /// </code>
    /// </example>
    public sealed class MatchCommands
    {
        private readonly AuthenticatedConnection connection;
        private readonly PendingPlay pending;
        private readonly IClientLog log;

        internal MatchCommands(AuthenticatedConnection connection, PendingPlay pending, IClientLog log)
        {
            this.connection = connection ?? throw new ArgumentNullException(nameof(connection), "connection is null: expected the match AuthenticatedConnection");
            this.pending = pending ?? throw new ArgumentNullException(nameof(pending), "pending is null: expected the PendingPlay of the match");
            this.log = log ?? throw new ArgumentNullException(nameof(log), "log is null: expected the client log");
        }

        /// <summary>Responde o mulligan trocando as cópias dadas; lista vazia confirma sem trocar.</summary>
        /// <example><code>await commands.Mulligan(Array.Empty&lt;CardInstanceId&gt;());</code></example>
        public Task<PlaySendResult> Mulligan(IReadOnlyList<CardInstanceId> swapped) => Send(new MulliganCommand(swapped));

        /// <summary>Joga uma unidade da mão.</summary>
        /// <example><code>await commands.PlayUnit(card.Instance);</code></example>
        public Task<PlaySendResult> PlayUnit(CardInstanceId card) => Send(new PlayUnitCommand(card));

        /// <summary>Lança um feitiço sem alvo.</summary>
        /// <example><code>await commands.CastSpell(spell.Instance);</code></example>
        public Task<PlaySendResult> CastSpell(CardInstanceId card) => Send(new CastSpellCommand(card, null));

        /// <summary>Lança um feitiço com alvo.</summary>
        /// <example><code>await commands.CastSpellAt(spell.Instance, target);</code></example>
        public Task<PlaySendResult> CastSpellAt(CardInstanceId card, CardInstanceId target) => Send(new CastSpellCommand(card, target));

        /// <summary>Passa a vez.</summary>
        /// <example><code>await commands.Pass();</code></example>
        public Task<PlaySendResult> Pass() => Send(new PassCommand());

        /// <summary>Manda unidades para a zona de ataque.</summary>
        /// <example><code>await commands.DeclareAttack(attackers);</code></example>
        public Task<PlaySendResult> DeclareAttack(IReadOnlyList<CardInstanceId> attackers) => Send(new DeclareAttackCommand(attackers));

        /// <summary>Puxa um atacante de volta.</summary>
        /// <example><code>await commands.WithdrawAttacker(attacker);</code></example>
        public Task<PlaySendResult> WithdrawAttacker(CardInstanceId attacker) => Send(new WithdrawAttackerCommand(attacker));

        /// <summary>Aperta Atacar.</summary>
        /// <example><code>await commands.ConfirmAttack();</code></example>
        public Task<PlaySendResult> ConfirmAttack() => Send(new ConfirmAttackCommand());

        /// <summary>Põe um bloqueador na frente de um atacante.</summary>
        /// <example><code>await commands.AssignBlocker(blocker: mine, attacker: theirs);</code></example>
        public Task<PlaySendResult> AssignBlocker(CardInstanceId blocker, CardInstanceId attacker) => Send(new AssignBlockerCommand(blocker, attacker));

        /// <summary>Tira um bloqueador.</summary>
        /// <example><code>await commands.RemoveBlocker(blocker);</code></example>
        public Task<PlaySendResult> RemoveBlocker(CardInstanceId blocker) => Send(new RemoveBlockerCommand(blocker));

        /// <summary>Aperta Resolver na defesa.</summary>
        /// <example><code>await commands.EndDefenseWindow();</code></example>
        public Task<PlaySendResult> EndDefenseWindow() => Send(new EndDefenseWindowCommand());

        /// <summary>Desiste da partida.</summary>
        /// <example><code>await commands.Forfeit();</code></example>
        public Task<PlaySendResult> Forfeit() => Send(new ForfeitCommand());

        /// <summary>Manda um comando já montado, sem conhecer o tipo dele.</summary>
        /// <example><code>await commands.Send(new PassCommand());</code></example>
        public async Task<PlaySendResult> Send(PlayCommand command)
        {
            PlayCommand required = command ?? throw new ArgumentNullException(nameof(command), "command is null: expected a PlayCommand such as PassCommand");
            ConnectionPhase phase = connection.Status.Phase;
            if (phase != ConnectionPhase.Connected)
                return NotSent(required, PlaySendStatus.NotConnected, phase);

            pending.MarkSent(required);
            SocketSendOutcome outcome = await connection.SendAsync(required);
            if (outcome.Status == SocketSendStatus.Sent)
                return new PlaySendResult(PlaySendStatus.Sent, required, phase);

            pending.Unmark(required);
            return NotSent(required, PlaySendStatus.SocketFailed, connection.Status.Phase);
        }

        private PlaySendResult NotSent(PlayCommand command, PlaySendStatus status, ConnectionPhase phase)
        {
            log.Warning("match_command_not_sent", new LogField("command", command.MessageType), new LogField("connection_phase", phase.ToString()), new LogField("status", status.ToString()));
            return new PlaySendResult(status, command, phase);
        }
    }
}
