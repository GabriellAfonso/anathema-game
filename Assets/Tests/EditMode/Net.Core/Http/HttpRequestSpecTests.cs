#nullable enable
using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace Anathema.Net.Core.Tests
{
    public class HttpRequestSpecTests
    {
        private static readonly Uri CardsUrl = new Uri("http://127.0.0.1:8000/game/cards/");

        [Test]
        public void PrazoPadraoEhDezSegundos()
        {
            Assert.That(new HttpRequestSpec("GET", CardsUrl).TimeoutSeconds, Is.EqualTo(10));
        }

        [TestCase(0)]
        [TestCase(121)]
        public void PrazoForaDeUmACentoEVinteLanca(int seconds)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new HttpRequestSpec("GET", CardsUrl, timeoutSeconds: seconds));
        }

        [Test]
        public void UrlRelativaLanca()
        {
            Assert.Throws<ArgumentException>(() => new HttpRequestSpec("GET", new Uri("/game/cards/", UriKind.Relative)));
        }

        [TestCase("HEAD")]
        [TestCase("get")]
        public void MetodoForaDaListaLanca(string method)
        {
            Assert.Throws<ArgumentException>(() => new HttpRequestSpec(method, CardsUrl));
        }

        [Test]
        public void CabecalhosSaoCopiados()
        {
            Dictionary<string, string> headers = new Dictionary<string, string> { ["Authorization"] = "Bearer a" };
            HttpRequestSpec request = new HttpRequestSpec("GET", CardsUrl, headers);

            headers["Authorization"] = "Bearer b";

            Assert.That(request.Headers["Authorization"], Is.EqualTo("Bearer a"));
        }
    }
}
