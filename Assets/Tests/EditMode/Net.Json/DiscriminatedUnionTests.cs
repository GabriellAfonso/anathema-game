#nullable enable
using System;
using System.Linq;
using Anathema.Net.Core;
using NUnit.Framework;
using static Anathema.Net.Json.Tests.CodecTestFactory;

namespace Anathema.Net.Json.Tests
{
    /// <summary>
    /// Aqui, e não em Net.Core, porque a união precisa de um IPayloadReader de verdade;
    /// um reader de dicionário só para o teste duplicaria o JObjectPayloadReader.
    /// </summary>
    public class DiscriminatedUnionTests
    {
        [Test]
        public void RegistrarOMesmoValorDuasVezesLanca()
        {
            DiscriminatedUnion<Problem> union = ProblemUnion();

            Assert.Throws<ArgumentException>(() => union.Register("too_many_copies", TooManyCopies.Read));
        }

        [Test]
        public void ValorSemBracoViraDesconhecidoComOTextoExato()
        {
            DecodeOutcome<Problem> outcome = ProblemUnion().DecodeObject(Reader("{\"kind\": \"Too_Many_Copies\"}"));

            Assert.That(outcome.Value, Is.InstanceOf<UnknownProblem>());
            Assert.That(((UnknownProblem)outcome.Value).Kind, Is.EqualTo("Too_Many_Copies"));
        }

        [Test]
        public void SemDiscriminadorEhMissingType()
        {
            DecodeOutcome<Problem> outcome = ProblemUnion().DecodeObject(Reader("{\"card_id\": 12}"));

            Assert.That(outcome.Failure.Kind, Is.EqualTo(DecodeFailureKind.MissingType));
            Assert.That(outcome.Failure.Path, Is.EqualTo("kind"));
        }

        [Test]
        public void DiscriminadorQueNaoEhTextoEhTypeNotText()
        {
            DecodeOutcome<Problem> outcome = ProblemUnion().DecodeObject(Reader("{\"kind\": 3}"));

            Assert.That(outcome.Failure.Kind, Is.EqualTo(DecodeFailureKind.TypeNotText));
        }

        [Test]
        public void FalhaDoBracoViraResultadoComCaminho()
        {
            DecodeOutcome<Problem> outcome = ProblemUnion().DecodeObject(Reader("{\"kind\": \"too_many_copies\"}"));

            Assert.That(outcome.Failure.Kind, Is.EqualTo(DecodeFailureKind.MissingField));
            Assert.That(outcome.Failure.Path, Is.EqualTo("card_id"));
        }

        [Test]
        public void UniaoAninhadaDevolveDesconhecidoNoItem()
        {
            IPayloadReader payload = Reader("{\"deck_problems\": [{\"kind\": \"too_many_copies\", \"card_id\": 12}, {\"kind\": \"brand_new\"}]}");

            Problem[] problems = payload.ReadObjectList("deck_problems").Select(ProblemUnion().ReadNested).ToArray();

            Assert.That(problems[0], Is.InstanceOf<TooManyCopies>());
            Assert.That(problems[1], Is.InstanceOf<UnknownProblem>());
        }

        [Test]
        public void FalhaAninhadaSaiComOCaminhoCompleto()
        {
            IPayloadReader payload = Reader("{\"payload\": {\"deck_problems\": [{\"kind\": \"too_many_copies\", \"card_id\": \"12\"}]}}");
            DiscriminatedUnion<Problem> problems = ProblemUnion();
            DiscriminatedUnion<Problem> outer = new DiscriminatedUnion<Problem>("type", type => new UnknownProblem(type))
                .Register("deck_refused", body => problems.ReadNested(body.ReadObjectList("deck_problems")[0]));

            DecodeOutcome<Problem> outcome = outer.DecodeBody("deck_refused", payload.ReadObject("payload"));

            Assert.That(outcome.Failure.Path, Is.EqualTo("payload.deck_problems[0].card_id"));
        }

        private static DiscriminatedUnion<Problem> ProblemUnion()
        {
            return new DiscriminatedUnion<Problem>("kind", kind => new UnknownProblem(kind))
                .Register("too_many_copies", TooManyCopies.Read);
        }

        private abstract class Problem
        {
        }

        private sealed class TooManyCopies : Problem
        {
            private TooManyCopies(long cardId)
            {
                CardId = cardId;
            }

            public long CardId { get; }

            public static Problem Read(IPayloadReader reader) => new TooManyCopies(reader.ReadInteger("card_id"));
        }

        private sealed class UnknownProblem : Problem
        {
            public UnknownProblem(string kind)
            {
                Kind = kind;
            }

            public string Kind { get; }
        }
    }
}
