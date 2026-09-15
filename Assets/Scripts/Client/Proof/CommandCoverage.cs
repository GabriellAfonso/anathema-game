#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Anathema.Net.Core;
using Anathema.Net.Match;

namespace Anathema.Client.Proof
{
    /// <summary>
    /// O que a partida 1 já provou. Um envio só conta quando um match_update seguinte traz o evento do mesmo jogador: o
    /// servidor é quem aceita. Envio recusado sai da espera e não conta, e evento sem envio (ex.: o passar que o relógio
    /// força) também não (specs/005-presentation-facade/contracts/match-proof.md, "Bots").
    /// </summary>
    internal sealed class CommandCoverage
    {
        private readonly HashSet<CoverageItem> covered = new HashSet<CoverageItem>();
        private readonly Dictionary<string, List<CoverageItem>> waiting = new Dictionary<string, List<CoverageItem>>(StringComparer.Ordinal);

        internal IReadOnlyList<string> Missing => Enum.GetValues(typeof(CoverageItem)).Cast<CoverageItem>().Where(item => !covered.Contains(item)).Select(item => item.ToString()).ToArray();

        internal bool Covers(CoverageItem item) => covered.Contains(item);

        internal void NoteSent(string player, PlayCommand command)
        {
            CoverageItem? item = ItemOf(command);
            if (item != null)
                WaitingFor(player).Add(item.Value);
        }

        internal void NoteRefused(string player, PlayRefusal refusal)
        {
            CoverageItem? item = refusal.ProbableCommand == null ? null : ItemOf(refusal.ProbableCommand);
            if (item != null)
                WaitingFor(player).Remove(item.Value);
        }

        internal void NoteEvent(string player, UserId self, MatchEvent matchEvent)
        {
            (UserId User, CoverageItem Item)? confirmed = ItemOf(matchEvent);
            if (confirmed == null || confirmed.Value.User != self || !WaitingFor(player).Remove(confirmed.Value.Item))
                return;

            covered.Add(confirmed.Value.Item);
        }

        private List<CoverageItem> WaitingFor(string player)
        {
            if (!waiting.TryGetValue(player, out List<CoverageItem>? items))
            {
                items = new List<CoverageItem>();
                waiting[player] = items;
            }

            return items;
        }

        private static CoverageItem? ItemOf(PlayCommand command)
        {
            return command switch
            {
                MulliganCommand mulligan => mulligan.Swapped.Count > 0 ? CoverageItem.MulliganSwapping : CoverageItem.MulliganKeeping,
                PlayUnitCommand _ => CoverageItem.PlayUnit,
                CastSpellCommand spell => spell.Target == null ? CoverageItem.SpellWithoutTarget : CoverageItem.SpellWithTarget,
                PassCommand _ => CoverageItem.Pass,
                DeclareAttackCommand _ => CoverageItem.DeclareAttack,
                WithdrawAttackerCommand _ => CoverageItem.WithdrawAttacker,
                ConfirmAttackCommand _ => CoverageItem.ConfirmAttack,
                AssignBlockerCommand _ => CoverageItem.AssignBlocker,
                RemoveBlockerCommand _ => CoverageItem.RemoveBlocker,
                EndDefenseWindowCommand _ => CoverageItem.EndDefense,
                _ => null,
            };
        }

        private static (UserId User, CoverageItem Item)? ItemOf(MatchEvent matchEvent)
        {
            return matchEvent switch
            {
                MulliganTakenEvent mulligan => (mulligan.User, mulligan.SwappedCount > 0 ? CoverageItem.MulliganSwapping : CoverageItem.MulliganKeeping),
                UnitPlayedEvent unit => (unit.User, CoverageItem.PlayUnit),
                SpellCastEvent spell => (spell.User, spell.Target == null ? CoverageItem.SpellWithoutTarget : CoverageItem.SpellWithTarget),
                PassedEvent passed => (passed.User, CoverageItem.Pass),
                AttackersSentEvent attack => (attack.User, CoverageItem.DeclareAttack),
                AttackerWithdrawnEvent withdrawn => (withdrawn.User, CoverageItem.WithdrawAttacker),
                AttackConfirmedEvent confirmed => (confirmed.User, CoverageItem.ConfirmAttack),
                BlockerAssignedEvent assigned => (assigned.User, CoverageItem.AssignBlocker),
                BlockerRemovedEvent removed => (removed.User, CoverageItem.RemoveBlocker),
                DefenseEndedEvent ended => (ended.User, CoverageItem.EndDefense),
                _ => null,
            };
        }
    }
}
