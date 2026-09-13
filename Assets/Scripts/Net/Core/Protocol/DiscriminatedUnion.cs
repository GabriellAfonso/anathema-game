#nullable enable
using System;
using System.Collections.Generic;

namespace Anathema.Net.Core
{
    /// <summary>
    /// União fechada escolhida por um campo discriminador, com um braço explícito
    /// para valor desconhecido (constituição, princípio IV). As features só
    /// registram braços; a leitura do discriminador e as falhas ficam aqui.
    /// </summary>
    /// <example>
    /// <code>
    /// DiscriminatedUnion&lt;ServerFrame&gt; frames = GenericServerFrames.CreateUnion()
    ///     .Register("match_found", MatchFoundFrame.Read);
    /// </code>
    /// </example>
    public sealed class DiscriminatedUnion<TBase> where TBase : class
    {
        private readonly string discriminatorField;
        private readonly Func<string, TBase> unknownArm;
        private readonly Dictionary<string, Func<IPayloadReader, TBase>> arms =
            new Dictionary<string, Func<IPayloadReader, TBase>>(StringComparer.Ordinal);

        /// <summary>Cria a união vazia com o campo discriminador e o braço desconhecido.</summary>
        /// <example><code>new DiscriminatedUnion&lt;DeckProblem&gt;("kind", kind => new UnknownDeckProblem(kind));</code></example>
        public DiscriminatedUnion(string discriminatorField, Func<string, TBase> unknownArm)
        {
            if (string.IsNullOrWhiteSpace(discriminatorField))
                throw new ArgumentException($"discriminator field is '{discriminatorField}': expected a field name like type or kind", nameof(discriminatorField));

            this.discriminatorField = discriminatorField;
            this.unknownArm = unknownArm ?? throw new ArgumentNullException(nameof(unknownArm), $"unknown arm of union on '{discriminatorField}' is null: expected a factory for unknown values");
        }

        /// <summary>Registra o braço de um valor; valor repetido lança.</summary>
        /// <example><code>union.Register("too_many_copies", TooManyCopies.Read);</code></example>
        public DiscriminatedUnion<TBase> Register(string value, Func<IPayloadReader, TBase> arm)
        {
            if (arms.ContainsKey(value))
                throw new ArgumentException($"{discriminatorField} '{value}' is already registered: expected each value once", nameof(value));

            arms.Add(value, arm ?? throw new ArgumentNullException(nameof(arm), $"arm for {discriminatorField} '{value}' is null: expected a reader function"));
            return this;
        }

        /// <summary>Decodifica um corpo cujo discriminador veio de fora (o <c>type</c> do envelope).</summary>
        /// <example><code>DecodeOutcome&lt;ServerFrame&gt; frame = frames.DecodeBody(type, payload);</code></example>
        public DecodeOutcome<TBase> DecodeBody(string value, IPayloadReader body)
        {
            try
            {
                return DecodeOutcome<TBase>.Valid(Dispatch(value, body));
            }
            catch (PayloadShapeException shape)
            {
                return DecodeOutcome<TBase>.Invalid(shape.Failure);
            }
        }

        /// <summary>Decodifica um objeto que carrega o próprio discriminador.</summary>
        /// <example><code>DecodeOutcome&lt;DeckProblem&gt; problem = problems.DecodeObject(item);</code></example>
        public DecodeOutcome<TBase> DecodeObject(IPayloadReader objectWithDiscriminator)
        {
            try
            {
                return DecodeOutcome<TBase>.Valid(ReadNested(objectWithDiscriminator));
            }
            catch (PayloadShapeException shape)
            {
                return DecodeOutcome<TBase>.Invalid(shape.Failure);
            }
        }

        /// <summary>
        /// Lê um objeto aninhado dentro de outro braço. Falha lança
        /// <see cref="PayloadShapeException"/> com o caminho completo, para o braço de fora
        /// continuar em linha reta.
        /// </summary>
        /// <example><code>DeckProblem[] problems = payload.ReadObjectList("deck_problems").Select(problemUnion.ReadNested).ToArray();</code></example>
        public TBase ReadNested(IPayloadReader objectWithDiscriminator)
        {
            string value = DiscriminatorField.Read(objectWithDiscriminator, discriminatorField);
            return Dispatch(value, objectWithDiscriminator);
        }

        private TBase Dispatch(string value, IPayloadReader body)
        {
            return arms.TryGetValue(value, out Func<IPayloadReader, TBase>? arm) ? arm(body) : unknownArm(value);
        }
    }
}
