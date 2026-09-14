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

        [Test]
        public void BracoDesconhecidoComObjetoLeOsCamposDoItem()
        {
            DecodeOutcome<Problem> outcome = MessageAwareUnion().DecodeObject(Reader("{\"kind\": \"banned_card\", \"message\": \"card 3 is banned\"}"));

            UnknownProblem unknown = (UnknownProblem)outcome.Value;
            Assert.That(unknown.Kind, Is.EqualTo("banned_card"));
            Assert.That(unknown.Message, Is.EqualTo("card 3 is banned"));
        }

        [Test]
        public void BracoDesconhecidoSemCampoObrigatorioFalhaComCaminho()
        {
            IPayloadReader payload = Reader("{\"deck_problems\": [{\"kind\": \"banned_card\"}]}");

            DecodeOutcome<Problem> outcome = MessageAwareUnion().DecodeObject(payload.ReadObjectList("deck_problems")[0]);

            Assert.That(outcome.Failure.Kind, Is.EqualTo(DecodeFailureKind.MissingField));
            Assert.That(outcome.Failure.Path, Is.EqualTo("deck_problems[0].message"));
        }

        [Test]
        public void ConstrutorAntigoContinuaEntregandoSoOTexto()
        {
            DecodeOutcome<Problem> outcome = ProblemUnion().DecodeObject(Reader("{\"kind\": \"banned_card\", \"message\": \"ignored\"}"));

            Assert.That(((UnknownProblem)outcome.Value).Message, Is.Null);
        }

        private static DiscriminatedUnion<Problem> MessageAwareUnion()
        {
            return new DiscriminatedUnion<Problem>("kind", (kind, item) => new UnknownProblem(kind, item.ReadText("message")))
                .Register("too_many_copies", TooManyCopies.Read);
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
            public UnknownProblem(string kind, string? message = null)
            {
                Kind = kind;
                Message = message;
            }

            public string Kind { get; }

            public string? Message { get; }
        }
    }
}
