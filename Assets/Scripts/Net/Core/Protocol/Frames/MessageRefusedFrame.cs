#nullable enable
using System;

namespace Anathema.Net.Core
{
    /// <summary>
    /// O servidor recusou uma mensagem. Decisão sempre pelo <see cref="Code"/>, nunca pelo
    /// texto de <see cref="Error"/> (constituição, princípio II). Campos extras (como
    /// <c>deck_id</c> ou <c>deck_problems</c>) ficam em <see cref="Details"/>, para a
    /// feature que os conhece. Contratos: backend/specs/009-match-protocol/contracts/refusal_codes.md
    /// e backend/specs/011-deck-catalog-api/contracts/matchmaking_messages.md.
    /// </summary>
    /// <example>
    /// <code>
    /// if (refused.Code == "deck_not_found") ShowMissingDeck(refused.Details.ReadInteger("deck_id"));
    /// </code>
    /// </example>
    public sealed class MessageRefusedFrame : ServerFrame
    {
        /// <summary>Valor de <c>type</c>.</summary>
        /// <example><code>union.Register(MessageRefusedFrame.TypeName, MessageRefusedFrame.Read);</code></example>
        public const string TypeName = "message_refused";

        /// <summary>Cria a recusa.</summary>
        /// <example><code>MessageRefusedFrame refused = new MessageRefusedFrame("not_your_turn", "…", payload);</code></example>
        public MessageRefusedFrame(string code, string error, IPayloadReader details)
            : base(TypeName)
        {
            Code = code ?? throw new ArgumentNullException(nameof(code), "refusal code is null: expected the server code text");
            Error = error ?? throw new ArgumentNullException(nameof(error), $"refusal error of '{code}' is null: expected the server error text");
            Details = details ?? throw new ArgumentNullException(nameof(details), $"refusal details of '{code}' are null: expected the payload reader");
        }

        /// <summary>Código estável da recusa; pode ser um que o cliente ainda não conhece.</summary>
        /// <example><code>bool turn = refused.Code == "not_your_turn";</code></example>
        public string Code { get; }

        /// <summary>Texto para humano ler no log. Nunca decida por ele.</summary>
        /// <example><code>log.Info("refused", new LogField("error", refused.Error));</code></example>
        public string Error { get; }

        /// <summary>O payload inteiro, só leitura, com os campos extras.</summary>
        /// <example><code>long deckId = refused.Details.ReadInteger("deck_id");</code></example>
        public IPayloadReader Details { get; }

        /// <summary>Braço da união: lê <c>code</c> e <c>error</c> obrigatórios.</summary>
        /// <example><code>MessageRefusedFrame refused = MessageRefusedFrame.Read(payload);</code></example>
        public static MessageRefusedFrame Read(IPayloadReader payload)
        {
            return new MessageRefusedFrame(payload.ReadText("code"), payload.ReadText("error"), payload);
        }
    }
}
