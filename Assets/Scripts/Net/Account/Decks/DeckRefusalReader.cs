#nullable enable
using System.Collections.Generic;
using System.Linq;
using Anathema.Net.Core;

namespace Anathema.Net.Account
{
    /// <summary>
    /// Lê o corpo 400 de uma operação de deck por chave, sem olhar texto (research R9), juntando
    /// todos os motivos reconhecidos na ordem da tabela de <c>contracts/player-data.md</c>. Corpo sem
    /// nenhuma chave conhecida lança <see cref="PayloadShapeException"/>, e quem chama o trata como
    /// recusa não reconhecida.
    /// </summary>
    /// <example>
    /// <code>
    /// DecodeOutcome&lt;DeckRefusal&gt; refusal = responses.Decode(body, new DeckRefusalReader().ReadBadRequest);
    /// </code>
    /// </example>
    internal sealed class DeckRefusalReader
    {
        private readonly DiscriminatedUnion<DeckProblem> problems = DeckProblemUnion.Create();

        /// <summary>Os motivos do 400; nenhum reconhecido lança.</summary>
        internal DeckRefusal ReadBadRequest(IPayloadReader body)
        {
            List<DeckRefusalReason> reasons = new List<DeckRefusalReason>();
            if (body.Has("name"))
                reasons.Add(new InvalidDeckName(body.ReadTextList("name")));

            if (body.Has("deck_problems"))
                reasons.Add(new DeckListRejected(body.ReadObjectList("deck_problems").Select(problems.ReadNested).ToArray()));

            if (body.Has("deck_limit"))
                reasons.Add(new DeckLimitReached(body.ReadTextList("deck_limit")));

            if (body.Has("detail"))
                reasons.Add(new MissingDeckField(body.ReadText("detail")));

            if (reasons.Count > 0)
                return DeckRefusal.Of(reasons.ToArray());

            throw new PayloadShapeException(new DecodeFailure(DecodeFailureKind.MissingField, body.Path, "deck 400 body has none of name, deck_problems, deck_limit, detail: expected at least one"));
        }
    }
}
