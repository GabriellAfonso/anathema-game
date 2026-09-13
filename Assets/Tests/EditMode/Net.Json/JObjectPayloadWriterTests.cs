#nullable enable
using System;
using Anathema.Net.Core;
using NUnit.Framework;
using static Anathema.Net.Json.Tests.CodecTestFactory;

namespace Anathema.Net.Json.Tests
{
    public class JObjectPayloadWriterTests
    {
        [Test]
        public void EscreveTodosOsTiposEOLeitorDevolveIgual()
        {
            string json = Codec().EncodeObject(writer =>
            {
                writer.WriteText("username", "one");
                writer.WriteInteger("deck_id", 4);
                writer.WriteBoolean("keep_hand", true);
                writer.WriteObject("target", target => target.WriteInteger("card_instance_id", 3));
                writer.WriteIntegerList("card_ids", new long[] { 1, 2 });
            });

            IPayloadReader reader = Reader(json);
            Assert.That(reader.ReadText("username"), Is.EqualTo("one"));
            Assert.That(reader.ReadInteger("deck_id"), Is.EqualTo(4));
            Assert.That(reader.ReadBoolean("keep_hand"), Is.True);
            Assert.That(reader.ReadObject("target").ReadInteger("card_instance_id"), Is.EqualTo(3));
            Assert.That(reader.ReadIntegerList("card_ids"), Is.EqualTo(new long[] { 1, 2 }));
        }

        [Test]
        public void CampoRepetidoLancaComONome()
        {
            ArgumentException error = Assert.Throws<ArgumentException>(() => Codec().EncodeObject(writer =>
            {
                writer.WriteText("username", "one");
                writer.WriteText("username", "two");
            }));

            Assert.That(error.Message, Does.Contain("username"));
        }
    }
}
