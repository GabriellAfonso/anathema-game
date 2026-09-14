#nullable enable
using System.Collections.Generic;
using System.Linq;
using Anathema.Net.Core;

namespace Anathema.Net.Connection
{
    /// <summary>
    /// Lê as recusas do socket de fila pelo <c>code</c>. Código conhecido com campo ausente ou de tipo
    /// errado não derruba nada: vira recusa não reconhecida, registrada com o caminho do campo. Os
    /// problemas de deck são lidos pela mesma família do HTTP (feature 002).
    /// </summary>
    internal sealed class QueueRefusalReader
    {
        private const string DeckNotSpecifiedCode = "deck_not_specified";
        private const string DeckNotFoundCode = "deck_not_found";
        private const string InvalidDeckCode = "invalid_deck";
        private const string MalformedMessageCode = "malformed_message";
        private const string UnknownMessageTypeCode = "unknown_message_type";
        private const string DeckField = "deck_id";
        private const string ProblemsField = "deck_problems";

        private readonly DiscriminatedUnion<DeckProblem> problems = DeckProblemUnion.Create();
        private readonly IClientLog log;

        internal QueueRefusalReader(IClientLog log)
        {
            this.log = log;
        }

        internal QueueRefusal Read(MessageRefusedFrame frame)
        {
            try
            {
                return ReadByCode(frame);
            }
            catch (PayloadShapeException shape)
            {
                log.Warning("queue_refusal_out_of_contract", new LogField("code", frame.Code), new LogField("path", shape.Failure.Path));
                return QueueRefusal.Of(QueueRefusalKind.Unrecognized, frame.Code, frame.Error);
            }
        }

        private QueueRefusal ReadByCode(MessageRefusedFrame frame)
        {
            return frame.Code switch
            {
                DeckNotSpecifiedCode => QueueRefusal.Of(QueueRefusalKind.DeckNotSpecified, frame.Code, frame.Error),
                DeckNotFoundCode => QueueRefusal.DeckNotFound(frame.Code, frame.Error, frame.Details.ReadDeckId(DeckField)),
                InvalidDeckCode => QueueRefusal.InvalidDeck(frame.Code, frame.Error, ReadOptionalDeck(frame.Details), ReadProblems(frame.Details)),
                MalformedMessageCode => QueueRefusal.Of(QueueRefusalKind.MalformedMessage, frame.Code, frame.Error),
                UnknownMessageTypeCode => QueueRefusal.Of(QueueRefusalKind.UnknownMessageType, frame.Code, frame.Error),
                _ => QueueRefusal.Of(QueueRefusalKind.Unrecognized, frame.Code, frame.Error),
            };
        }

        private IReadOnlyList<DeckProblem> ReadProblems(IPayloadReader details)
        {
            return details.ReadObjectList(ProblemsField).Select(problems.ReadNested).ToArray();
        }

        private static DeckId? ReadOptionalDeck(IPayloadReader details)
        {
            return details.Has(DeckField) ? details.ReadDeckId(DeckField) : (DeckId?)null;
        }
    }
}
