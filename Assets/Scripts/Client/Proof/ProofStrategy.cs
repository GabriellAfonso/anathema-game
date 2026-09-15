#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Anathema.Net.Account;
using Anathema.Net.Core;
using Anathema.Net.Match;

namespace Anathema.Client.Proof
{
    /// <summary>
    /// A estratégia dos bots de <c>backend/scripts/smoke_match.py</c> (<c>act</c> e os candidatos por fase), sobre a
    /// visão e as dicas da feature 004. É estratégia de teste para escolher o que mandar, não regra do cliente:
    /// quem decide se a jogada vale é o servidor, e uma recusa só faz o bot tentar o próximo candidato.
    /// Na 005 ganhou as extensões da prova final (remover um bloqueador, poção sem alvo, vez parada e desistência no
    /// mulligan) e passou a ler as dicas pela <c>LiveMatch.HintFor</c> (specs/005-presentation-facade/contracts/match-proof.md).
    /// </summary>
    internal sealed class ProofStrategy
    {
        internal const long RoundCap = 30;
        private const long PotionNexusCeiling = 12;
        private const int BankCeiling = 6;
        private const string PotionName = "LIFE POTION";
        private const int LastCandidateRepeats = 3;

        private static readonly CoverageItem[] AttackItems =
        {
            CoverageItem.DeclareAttack, CoverageItem.WithdrawAttacker, CoverageItem.ConfirmAttack, CoverageItem.AssignBlocker, CoverageItem.RemoveBlocker,
            CoverageItem.EndDefense,
        };

        private readonly string label;
        private readonly LoadedCatalog catalog;
        private readonly Func<CardInstanceId, HandCardHint> hints;
        private readonly CommandCoverage coverage;
        private readonly HashSet<string> tried = new HashSet<string>(StringComparer.Ordinal);
        private bool mulliganSent;
        private bool withdrewOnce;
        private long repeatedVersion = -1;
        private int repeats;
        private bool stallStarted;
        private bool stallOver;

        internal ProofStrategy(string label, LoadedCatalog catalog, Func<CardInstanceId, HandCardHint> hints, CommandCoverage coverage)
        {
            this.label = label;
            this.catalog = catalog;
            this.hints = hints;
            this.coverage = coverage;
        }

        /// <summary>P1 da partida 2: desiste no mulligan em vez de responder.</summary>
        internal bool ForfeitsInMulligan { get; set; }

        /// <summary>A partir desta rodada, a primeira Fase de Ação com a vez fica parada até o relógio estourar; nulo nunca para.</summary>
        internal long? StallFromRound { get; set; }

        internal bool IsStalling => stallStarted && !stallOver;

        internal PlayCommand? Next(PlayerView view, long version)
        {
            if (view.Phase == MatchPhase.Mulligan)
                return Mulligan(view);

            if (view.PriorityUser != view.You.Profile.User)
                return null;

            if (view.RoundNumber >= RoundCap)
                return new ForfeitCommand();

            if (Stalls(view))
                return null;

            IEnumerable<PlayCommand>? candidates = CandidatesFor(view);
            return candidates == null ? null : FirstUntried(candidates, view, version);
        }

        /// <summary>O relógio estourou a vez deste jogador: a vez parada acabou e o bot volta a agir.</summary>
        internal void NoteTurnTimedOut() => stallOver |= stallStarted;

        /// <summary>
        /// A conexão voltou a ao vivo: o que foi mandado antes da queda pode ter se perdido com o socket. Se o servidor
        /// tivesse aplicado, o <c>match_start</c> já traria outra versão; na mesma versão, os candidatos voltam a valer.
        /// </summary>
        internal void ForgetTried() => tried.Clear();

        private PlayCommand? Mulligan(PlayerView view)
        {
            if (view.You.MulliganTaken || mulliganSent)
                return null;

            mulliganSent = true;
            if (ForfeitsInMulligan)
                return new ForfeitCommand();

            CardInstanceId[] swapped = label == "P1" ? new[] { view.You.Hand[0].Instance } : Array.Empty<CardInstanceId>();
            return new MulliganCommand(swapped);
        }

        private bool Stalls(PlayerView view)
        {
            if (StallFromRound == null || stallOver || view.Phase != MatchPhase.Action || view.RoundNumber < StallFromRound.Value)
                return false;

            stallStarted = true;
            return true;
        }

        private IEnumerable<PlayCommand>? CandidatesFor(PlayerView view)
        {
            return view.Phase switch
            {
                MatchPhase.Action => ActionCandidates(view),
                MatchPhase.Declaration => DeclarationCandidates(view),
                MatchPhase.Combat => DefenseCandidates(view),
                _ => null,
            };
        }

        private PlayCommand FirstUntried(IEnumerable<PlayCommand> candidates, PlayerView view, long version)
        {
            PlayCommand[] ordered = candidates.ToArray();
            foreach (PlayCommand candidate in ordered)
            {
                if (!tried.Add(version + "|" + candidate.MessageType + "|" + string.Join(",", CardsOf(candidate))))
                    continue;

                withdrewOnce |= candidate is WithdrawAttackerCommand;
                return candidate;
            }

            return RepeatLast(ordered, view, version);
        }

        private PlayCommand RepeatLast(PlayCommand[] ordered, PlayerView view, long version)
        {
            // Primeira prova contra o servidor (2026-09-14): na volta de uma queda o bot é chamado de novo na mesma versão
            // (match_start sem troca de versão, volta a ao vivo, recusa atrasada de um envio de antes da queda). O último
            // candidato (passar, confirmar ou encerrar a defesa) pode ser repetido algumas vezes; só depois disso é travamento.
            if (repeatedVersion != version)
            {
                repeatedVersion = version;
                repeats = 0;
            }

            if (ordered.Length > 0 && repeats++ < LastCandidateRepeats)
                return ordered[ordered.Length - 1];

            throw new InvalidOperationException($"{label} ficou sem jogada em {view.Phase}, versão {version}: expected an untried candidate or the last one repeated fewer than {LastCandidateRepeats} times");
        }

        private IEnumerable<PlayCommand> ActionCandidates(PlayerView view)
        {
            foreach (PlayCommand spell in SpellCandidates(view, declaration: false))
                yield return spell;

            CardInstanceId[] bank = view.You.Bank.Select(unit => unit.Card.Instance).ToArray();
            if (view.TokenHolder == view.You.Profile.User && !view.TokenConsumed && bank.Length > 0 && !HoldsAttack())
                yield return new DeclareAttackCommand(bank);

            foreach (PlayCommand unit in CheapestUnits(view, bank.Length))
                yield return unit;

            yield return new PassCommand();
        }

        private bool HoldsAttack()
        {
            // Extensão da prova: com o combate já provado e o feitiço sem alvo ainda não, atacar só encurta a partida. Na
            // primeira prova contra o servidor (2026-09-14) a partida 1 acabou por Nexus na rodada 7 sem nenhuma LIFE POTION.
            return !coverage.Covers(CoverageItem.SpellWithoutTarget) && AttackItems.All(coverage.Covers);
        }

        private IEnumerable<PlayCommand> CheapestUnits(PlayerView view, int bankCount)
        {
            IEnumerable<HandCardHint> units = view.You.Hand.Select(card => hints(card.Instance))
                .Where(hint => hint.Kind == HintCardKind.Unit)
                .OrderBy(hint => hint.Cost);
            foreach (HandCardHint unit in units)
            {
                if (unit.Cost <= unit.EnergyCurrent && bankCount < BankCeiling)
                    yield return new PlayUnitCommand(unit.Instance);
            }
        }

        private IEnumerable<PlayCommand> DeclarationCandidates(PlayerView view)
        {
            IReadOnlyList<CardInstanceId> zone = view.Combat?.Attackers ?? Array.Empty<CardInstanceId>();
            if (!withdrewOnce && zone.Count >= 2)
                yield return new WithdrawAttackerCommand(zone[zone.Count - 1]);

            foreach (PlayCommand spell in SpellCandidates(view, declaration: true))
                yield return spell;

            yield return new ConfirmAttackCommand();
        }

        private IEnumerable<PlayCommand> DefenseCandidates(PlayerView view)
        {
            // Extensão da prova: com o próprio bloqueio já aceito e "remover" ainda não provado, desfaz esse bloqueio uma vez.
            BlockPair? ownBlock = OwnBlock(view);
            if (ownBlock != null && !coverage.Covers(CoverageItem.RemoveBlocker))
                yield return new RemoveBlockerCommand(ownBlock.Blocker);

            CombatView? combat = view.Combat;
            if (combat != null && combat.Blocks.Count == 0 && combat.Attackers.Count > 0 && view.You.Bank.Count > 0)
                yield return new AssignBlockerCommand(view.You.Bank[0].Card.Instance, combat.Attackers[0]);

            yield return new EndDefenseWindowCommand();
        }

        private static BlockPair? OwnBlock(PlayerView view)
        {
            return view.Combat?.Blocks.FirstOrDefault(block => view.You.Bank.Any(unit => unit.Card.Instance == block.Blocker));
        }

        private IEnumerable<PlayCommand> SpellCandidates(PlayerView view, bool declaration)
        {
            foreach (MatchCard card in view.You.Hand)
            {
                HandCardHint hint = hints(card.Instance);
                if (hint.Kind != HintCardKind.Spell || hint.Cost > hint.EnergyCurrent || hint.DeclarationOnly != declaration || SkipsPotion(card, view))
                    continue;

                PlayCommand? cast = Cast(hint);
                if (cast != null)
                    yield return cast;
            }
        }

        private bool SkipsPotion(MatchCard card, PlayerView view)
        {
            // Extensão da prova: a poção é o feitiço sem alvo do deck, e o teto de Nexus só vale depois de ela ter sido provada.
            return catalog.Find(card.Card).Spell?.Name == PotionName && view.You.Nexus >= PotionNexusCeiling && coverage.Covers(CoverageItem.SpellWithoutTarget);
        }

        private static PlayCommand? Cast(HandCardHint hint)
        {
            if (hint.Target == SpellTargetKind.None)
                return new CastSpellCommand(hint.Instance, null);

            return hint.Candidates.Count == 0 ? null : new CastSpellCommand(hint.Instance, hint.Candidates[0]);
        }

        private static IEnumerable<long> CardsOf(PlayCommand command)
        {
            return command switch
            {
                MulliganCommand mulligan => mulligan.Swapped.Select(card => card.Value),
                PlayUnitCommand unit => new[] { unit.Card.Value },
                CastSpellCommand spell => new[] { spell.Card.Value, spell.Target?.Value ?? -1 },
                DeclareAttackCommand attack => attack.Attackers.Select(card => card.Value),
                WithdrawAttackerCommand withdraw => new[] { withdraw.Attacker.Value },
                AssignBlockerCommand block => new[] { block.Blocker.Value, block.Attacker.Value },
                RemoveBlockerCommand remove => new[] { remove.Blocker.Value },
                _ => Array.Empty<long>(),
            };
        }
    }
}
