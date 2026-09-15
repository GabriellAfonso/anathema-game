#nullable enable
using System;
using System.Linq;
using System.Reflection;
using Anathema.Net.Account;
using Anathema.Net.Connection;
using Anathema.Net.Core;
using Anathema.Net.Match;
using NUnit.Framework;

namespace Anathema.Net.Facade.Tests
{
    /// <summary>
    /// FR-019, FR-022, SC-006: os tipos públicos de cada assembly da superfície são exatamente os de
    /// specs/005-presentation-facade/contracts/presentation-surface.md ("Tipos públicos, por assembly"). Mudou um,
    /// muda o contrato e este teste na mesma mudança.
    /// </summary>
    public class SurfaceContractTests
    {
        internal static readonly string[] Core =
        {
            "CardId", "CardInstanceId", "ClientLogEntry", "ClientLogExtensions", "ClientLogLevel", "DeckId", "DeckProblem", "DecodeFailure",
            "DecodeFailureKind", "EventFeed", "IClientLog", "LogField", "MatchId", "TooManyCopies", "UnknownCard", "UnrecognizedDeckProblem",
            "UserId", "WrongDeckSize",
        };

        internal static readonly string[] Account =
        {
            "AccountCallFailure", "AccountCallFailureKind", "AccountCallOutcome", "AccountCreated", "AccountSessionState", "CardCatalog",
            "CardLookup", "CardLookupKind", "CatalogCard", "DeckChange", "DeckDeleted", "DeckDraft", "DeckLimitReached", "DeckListRejected",
            "DeckNotFound", "DeckRefusal", "DeckRefusalReason", "HistoryOpponent", "HistoryPageRequest", "HistoryRefusal", "HistoryRefusalKind",
            "InvalidDeckName", "LoadedCatalog", "MatchEndReason", "MatchHistory", "MatchHistoryPage", "MatchHistoryRow", "MissingDeckField",
            "OwnProfile", "Password", "PlayerDeck", "PlayerDecks", "ProfileRefusal", "ProfileRefusalKind", "RegistrationField",
            "RegistrationFieldError", "RegistrationForm", "RegistrationRefusal", "ResumeOutcome", "ResumeOutcomeKind", "SessionUnavailableKind",
            "SignInOutcome", "SignInOutcomeKind", "SpellCard", "SpellDuration", "SpellEffect", "SpellTargetKind", "UnitCard",
            "UnrecognizedDeckRefusal", "UnrecognizedRefusal",
        };

        internal static readonly string[] Connection =
        {
            "ConnectionPhase", "GiveUpKind", "GiveUpReason", "MatchPairing", "MatchRefusalDetail", "PairedPlayer", "QueuePhase", "QueueRefusal",
            "QueueRefusalKind",
        };

        internal static readonly string[] Match =
        {
            "AssignBlockerCommand", "AttackConfirmedEvent", "AttackModifier", "AttackerWithdrawnEvent", "AttackersSentEvent", "BankUnit", "BlockPair",
            "BlockerAssignedEvent", "BlockerRemovedEvent", "CardsDrawnEvent", "CastSpellCommand", "ClockView", "CombatView", "ConfirmAttackCommand",
            "DamageImmunityModifier", "DeclareAttackCommand", "DefenseEndedEvent", "DisplayedUnitStats", "EndDefenseWindowCommand", "EventDetail",
            "ForfeitCommand", "ForfeitedEvent", "HandCardHint", "HealthModifier", "HintCardKind", "LiveMatch", "LiveMatchPhase", "LiveMatchStatus",
            "MatchCard", "MatchCommands", "MatchEnding", "MatchEvent", "MatchFinishedEvent", "MatchMirror", "MatchOutcome", "MatchPhase",
            "MatchProfile", "MulliganCommand", "MulliganTakenEvent", "MulliganTimedOutEvent", "NexusChangedEvent", "OpponentSideView", "OwnSideView",
            "PassCommand", "PassedEvent", "PendingPlay", "PhaseChange", "PlayCommand", "PlayRefusal", "PlayRefusalCode", "PlaySendResult",
            "PlaySendStatus", "PlayUnitCommand", "PlayerView", "PriorityChange", "RemoveBlockerCommand", "RoundStartedEvent", "SideView",
            "SpellCastEvent", "TurnClock", "TurnTimedOutEvent", "TurnView", "UnitDamagedEvent", "UnitDiedEvent", "UnitModifier", "UnitPlayedEvent",
            "UnrecognizedMatchEvent", "UnrecognizedModifier", "ViewReplaced", "WithdrawAttackerCommand",
        };

        internal static readonly string[] Facade =
        {
            "AnathemaClient", "ClientAccount", "ClientQueue", "ClientStage", "ClientStageChange", "ClientState", "ConnectionHealth", "GaveUpNotice",
            "HistoryRowStatus", "MatchResult", "MatchUnavailable", "MatchUnavailableKind", "QueueExit", "QueueExitKind", "QueueJoinKind",
            "QueueJoinResult", "ReconnectingNotice", "RecoveredNotice", "SignedOutReason", "StageRequestResult",
        };

        [Test]
        public void NucleoExpoeSoOContrato() => AssertExported(typeof(UserId).Assembly, Core);

        [Test]
        public void ContaExpoeSoOContrato() => AssertExported(typeof(LoadedCatalog).Assembly, Account);

        [Test]
        public void ConexaoExpoeSoOContrato() => AssertExported(typeof(GiveUpReason).Assembly, Connection);

        [Test]
        public void PartidaExpoeSoOContrato() => AssertExported(typeof(LiveMatch).Assembly, Match);

        [Test]
        public void FachadaExpoeSoOContrato() => AssertExported(typeof(AnathemaClient).Assembly, Facade);

        internal static string[] ExportedNames(Assembly assembly)
        {
            return assembly.GetExportedTypes().Where(type => !type.IsNested).Select(type => type.Name.Split('`')[0]).Distinct().OrderBy(name => name, StringComparer.Ordinal).ToArray();
        }

        internal static void AssertExported(Assembly assembly, string[] contract)
        {
            string[] exported = ExportedNames(assembly);
            string[] extra = exported.Except(contract).ToArray();
            string[] missing = contract.Except(exported).ToArray();

            Assert.That(extra, Is.Empty, $"{assembly.GetName().Name} expõe tipos fora do contrato");
            Assert.That(missing, Is.Empty, $"{assembly.GetName().Name} não expõe tipos do contrato");
        }
    }
}
