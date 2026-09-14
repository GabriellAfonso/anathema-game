#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Anathema.Net.Account;
using Anathema.Net.Core;
using Anathema.Net.Match;

namespace Anathema.Net.Unity.Tests
{
    /// <summary>
    /// A estratégia dos bots de <c>backend/scripts/smoke_match.py</c> (<c>act</c> e os candidatos por fase), sobre a
    /// visão e as dicas da feature 004. É estratégia de teste para escolher o que mandar, não regra do cliente:
    /// quem decide se a jogada vale é o servidor, e uma recusa só faz o bot tentar o próximo candidato.
    /// </summary>
    internal sealed class SmokeStrategy
    {
        internal const long RoundCap = 30;
        private const long PotionNexusCeiling = 12;
        private const int BankCeiling = 6;
        private const string PotionName = "LIFE POTION";

        private readonly string label;
        private readonly LoadedCatalog catalog;
        private readonly IProtocolCodec codec;
        private readonly HashSet<string> tried = new HashSet<string>(StringComparer.Ordinal);
        private bool mulliganSent;
        private bool withdrewOnce;

        internal SmokeStrategy(string label, LoadedCatalog catalog, IProtocolCodec codec)
        {
            this.label = label;
            this.catalog = catalog;
            this.codec = codec;
        }

        internal PlayCommand? Next(PlayerView view, long version)
        {
            if (view.Phase == MatchPhase.Mulligan)
                return Mulligan(view);

            if (view.PriorityUser != view.You.Profile.User)
                return null;

            if (view.RoundNumber >= RoundCap)
                return new ForfeitCommand();

            IEnumerable<PlayCommand>? candidates = CandidatesFor(view);
            return candidates == null ? null : FirstUntried(candidates, view, version);
        }

        private PlayCommand? Mulligan(PlayerView view)
        {
            if (view.You.MulliganTaken || mulliganSent)
                return null;

            mulliganSent = true;
            CardInstanceId[] swapped = label == "P1" ? new[] { view.You.Hand[0].Instance } : Array.Empty<CardInstanceId>();
            return new MulliganCommand(swapped);
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
            foreach (PlayCommand candidate in candidates)
            {
                if (!tried.Add(version + "|" + codec.Encode(candidate)))
                    continue;

                withdrewOnce |= candidate is WithdrawAttackerCommand;
                return candidate;
            }

            throw new InvalidOperationException($"{label} ficou sem jogada em {view.Phase}, versão {version}: expected at least pass, confirm or end defense to be untried");
        }

        private IEnumerable<PlayCommand> ActionCandidates(PlayerView view)
        {
            foreach (PlayCommand spell in SpellCandidates(view, declaration: false))
                yield return spell;

            CardInstanceId[] bank = view.You.Bank.Select(unit => unit.Card.Instance).ToArray();
            if (view.TokenHolder == view.You.Profile.User && !view.TokenConsumed && bank.Length > 0)
                yield return new DeclareAttackCommand(bank);

            foreach (PlayCommand unit in CheapestUnits(view, bank.Length))
                yield return unit;

            yield return new PassCommand();
        }

        private IEnumerable<PlayCommand> CheapestUnits(PlayerView view, int bankCount)
        {
            IEnumerable<HandCardHint> units = view.You.Hand.Select(card => HandCardHints.For(card.Instance, view, catalog))
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

        private static IEnumerable<PlayCommand> DefenseCandidates(PlayerView view)
        {
            CombatView? combat = view.Combat;
            if (combat != null && combat.Blocks.Count == 0 && combat.Attackers.Count > 0 && view.You.Bank.Count > 0)
                yield return new AssignBlockerCommand(view.You.Bank[0].Card.Instance, combat.Attackers[0]);

            yield return new EndDefenseWindowCommand();
        }

        private IEnumerable<PlayCommand> SpellCandidates(PlayerView view, bool declaration)
        {
            foreach (MatchCard card in view.You.Hand)
            {
                HandCardHint hint = HandCardHints.For(card.Instance, view, catalog);
                if (hint.Kind != HintCardKind.Spell || hint.Cost > hint.EnergyCurrent || hint.DeclarationOnly != declaration || SkipsPotion(card, view))
                    continue;

                PlayCommand? cast = Cast(hint);
                if (cast != null)
                    yield return cast;
            }
        }

        private bool SkipsPotion(MatchCard card, PlayerView view)
        {
            return catalog.Find(card.Card).Spell?.Name == PotionName && view.You.Nexus >= PotionNexusCeiling;
        }

        private static PlayCommand? Cast(HandCardHint hint)
        {
            if (hint.Target == SpellTargetKind.None)
                return new CastSpellCommand(hint.Instance, null);

            return hint.Candidates.Count == 0 ? null : new CastSpellCommand(hint.Instance, hint.Candidates[0]);
        }
    }
}
