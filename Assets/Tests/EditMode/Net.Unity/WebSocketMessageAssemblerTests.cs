#nullable enable
using System;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace Anathema.Net.Unity.Tests
{
    public class WebSocketMessageAssemblerTests
    {
        [Test]
        public void TresFragmentosViramUmTexto()
        {
            WebSocketMessageAssembler assembler = new WebSocketMessageAssembler();

            foreach (string part in new[] { "{\"type\":", " \"pong\",", " \"payload\": {}}" })
                assembler.Append(Bytes(part));

            Assert.That(assembler.Complete(), Is.EqualTo("{\"type\": \"pong\", \"payload\": {}}"));
        }

        [Test]
        public void CaractereMultibyteCortadoEntreFragmentosEhRemontado()
        {
            byte[] text = Encoding.UTF8.GetBytes("ação");
            WebSocketMessageAssembler assembler = new WebSocketMessageAssembler();

            assembler.Append(new ArraySegment<byte>(text, 0, 2));
            assembler.Append(new ArraySegment<byte>(text, 2, text.Length - 2));

            Assert.That(assembler.Complete(), Is.EqualTo("ação"));
        }

        [Test]
        public void MensagemDe64KiBEmPedacosDe8KiBSaiInteira()
        {
            string big = new string('x', 64 * 1024);
            byte[] bytes = Encoding.UTF8.GetBytes(big);
            WebSocketMessageAssembler assembler = new WebSocketMessageAssembler();

            for (int offset = 0; offset < bytes.Length; offset += 8192)
                assembler.Append(new ArraySegment<byte>(bytes, offset, Math.Min(8192, bytes.Length - offset)));

            Assert.That(assembler.Complete(), Is.EqualTo(big));
        }

        [Test]
        public void CompleteEResetRecomecamVazio()
        {
            WebSocketMessageAssembler assembler = new WebSocketMessageAssembler();
            assembler.Append(Bytes("first"));
            assembler.Complete();
            assembler.Append(Bytes("lost"));

            assembler.Reset();
            assembler.Append(Bytes("second"));

            Assert.That(assembler.Complete(), Is.EqualTo("second"));
        }

        private static ArraySegment<byte> Bytes(string text) => new ArraySegment<byte>(Encoding.UTF8.GetBytes(text).ToArray());
    }
}
