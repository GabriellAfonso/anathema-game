#nullable enable
using System;
using System.Collections.Generic;
using Anathema.Net.Core;

namespace Anathema.Net.Connection
{
    /// <summary>
    /// Uma recusa do socket de fila, tipada pelo <c>code</c>. <see cref="Error"/> é só para leitura
    /// humana: nenhuma decisão compara o texto (constituição, princípio II).
    /// </summary>
    /// <example>
    /// <code>
    /// queue.Refused += refusal => log.Info("queue_refused", new LogField("code", refusal.Code));
    /// </code>
    /// </example>
    public sealed class QueueRefusal
    {
        private static readonly IReadOnlyList<DeckProblem> NoProblems = Array.Empty<DeckProblem>();

        private QueueRefusal(QueueRefusalKind kind, string code, string error, DeckId? deck, IReadOnlyList<DeckProblem> problems)
        {
            Kind = kind;
            Code = code;
            Error = error;
            Deck = deck;
            Problems = problems;
        }

        /// <summary>A recusa, pelo código.</summary>
        /// <example><code>QueueRefusalKind kind = refusal.Kind;</code></example>
        public QueueRefusalKind Kind { get; }

        /// <summary>O <c>code</c> como veio, sempre preenchido.</summary>
        /// <example><code>string code = refusal.Code;</code></example>
        public string Code { get; }

        /// <summary>O texto do servidor, só para leitura humana.</summary>
        /// <example><code>string text = refusal.Error;</code></example>
        public string Error { get; }

        /// <summary>O deck ecoado pelo servidor, quando vem.</summary>
        /// <example><code>DeckId? deck = refusal.Deck;</code></example>
        public DeckId? Deck { get; }

        /// <summary>Os problemas do deck em <see cref="QueueRefusalKind.InvalidDeck"/>, na ordem do servidor; vazia nos outros.</summary>
        /// <example><code>foreach (DeckProblem problem in refusal.Problems) Show(problem.Message);</code></example>
        public IReadOnlyList<DeckProblem> Problems { get; }

        internal static QueueRefusal Of(QueueRefusalKind kind, string code, string error) => new QueueRefusal(kind, code, error, null, NoProblems);

        internal static QueueRefusal DeckNotFound(string code, string error, DeckId deck) => new QueueRefusal(QueueRefusalKind.DeckNotFound, code, error, deck, NoProblems);

        internal static QueueRefusal InvalidDeck(string code, string error, DeckId? deck, IReadOnlyList<DeckProblem> problems)
        {
            return new QueueRefusal(QueueRefusalKind.InvalidDeck, code, error, deck, problems);
        }

        /// <inheritdoc />
        public override string ToString() => $"kind={Kind} code={Code} problems={Problems.Count}";
    }
}
