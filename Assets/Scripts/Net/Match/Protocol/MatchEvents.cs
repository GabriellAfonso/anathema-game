#nullable enable
using System.Collections.Generic;
using System.Linq;
using Anathema.Net.Core;

namespace Anathema.Net.Match
{
    /// <summary>União dos eventos de partida por <c>kind</c>: 17 da feature 009, 2 da 010 e o desconhecido.</summary>
    internal static class MatchEvents
    {
        private const string DiscriminatorField = "kind";

        private static readonly DiscriminatedUnion<MatchEvent> Union = RegisterTimers(RegisterConsequences(RegisterPlays(
            new DiscriminatedUnion<MatchEvent>(DiscriminatorField, kind => new UnrecognizedMatchEvent(kind)))));

        internal static IReadOnlyList<MatchEvent> ReadList(IPayloadReader parent, string field)
        {
            return parent.ReadObjectList(field).Select(Union.ReadNested).ToArray();
        }

        private static DiscriminatedUnion<MatchEvent> RegisterPlays(DiscriminatedUnion<MatchEvent> union)
        {
            return union
                .Register(MulliganTakenEvent.KindName, MulliganTakenEvent.Read)
                .Register(UnitPlayedEvent.KindName, UnitPlayedEvent.Read)
                .Register(SpellCastEvent.KindName, SpellCastEvent.Read)
                .Register(PassedEvent.KindName, PassedEvent.Read)
                .Register(AttackersSentEvent.KindName, AttackersSentEvent.Read)
                .Register(AttackerWithdrawnEvent.KindName, AttackerWithdrawnEvent.Read)
                .Register(AttackConfirmedEvent.KindName, AttackConfirmedEvent.Read)
                .Register(BlockerAssignedEvent.KindName, BlockerAssignedEvent.Read)
                .Register(BlockerRemovedEvent.KindName, BlockerRemovedEvent.Read)
                .Register(DefenseEndedEvent.KindName, DefenseEndedEvent.Read)
                .Register(ForfeitedEvent.KindName, ForfeitedEvent.Read);
        }

        private static DiscriminatedUnion<MatchEvent> RegisterConsequences(DiscriminatedUnion<MatchEvent> union)
        {
            return union
                .Register(UnitDamagedEvent.KindName, UnitDamagedEvent.Read)
                .Register(UnitDiedEvent.KindName, UnitDiedEvent.Read)
                .Register(NexusChangedEvent.KindName, NexusChangedEvent.Read)
                .Register(RoundStartedEvent.KindName, RoundStartedEvent.Read)
                .Register(CardsDrawnEvent.KindName, CardsDrawnEvent.Read)
                .Register(MatchFinishedEvent.KindName, MatchFinishedEvent.Read);
        }

        private static DiscriminatedUnion<MatchEvent> RegisterTimers(DiscriminatedUnion<MatchEvent> union)
        {
            return union
                .Register(TurnTimedOutEvent.KindName, TurnTimedOutEvent.Read)
                .Register(MulliganTimedOutEvent.KindName, MulliganTimedOutEvent.Read);
        }
    }
}
