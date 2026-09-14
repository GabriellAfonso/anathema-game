#nullable enable
using System;
using NUnit.Framework;

namespace Anathema.Net.Account.Tests
{
    public class HistoryPageRequestTests
    {
        [Test]
        public void PadraoEhPrimeiraPaginaComVinte()
        {
            HistoryPageRequest request = new HistoryPageRequest();

            Assert.That(request.Page, Is.EqualTo(1));
            Assert.That(request.PageSize, Is.EqualTo(20));
            Assert.That(request.ToQuery(), Is.EqualTo("?page=1&page_size=20"));
        }

        [Test]
        public void PaginaZeroLancaComOValor()
        {
            ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() => new HistoryPageRequest(0, 20));

            Assert.That(error.Message, Does.Contain("page is 0"));
        }

        [Test]
        public void TamanhoZeroLancaComOValor()
        {
            ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() => new HistoryPageRequest(1, 0));

            Assert.That(error.Message, Does.Contain("page_size is 0"));
        }

        [Test]
        public void TamanhoAcimaDoTetoVaiComoVeio()
        {
            Assert.That(new HistoryPageRequest(1, 500).ToQuery(), Is.EqualTo("?page=1&page_size=500"));
        }
    }
}
