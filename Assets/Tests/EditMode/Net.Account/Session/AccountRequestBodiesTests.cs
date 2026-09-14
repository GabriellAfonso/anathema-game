#nullable enable
using Anathema.Net.Core;
using Anathema.Net.Fakes;
using NUnit.Framework;

namespace Anathema.Net.Account.Tests
{
    public class AccountRequestBodiesTests
    {
        private static readonly MonotonicInstant ArrivedAt = new MonotonicInstant(99);

        [Test]
        public void CorpoDeLoginTemSoUsuarioESenha()
        {
            IProtocolCodec codec = AccountTestCodec.Codec();

            IPayloadReader body = AccountTestCodec.Reader(codec.EncodeObject(writer => AccountRequestBodies.WriteLogin(writer, "one", new Password("secret"))));

            Assert.That(body.FieldNames, Is.EquivalentTo(new[] { "username", "password" }));
            Assert.That(body.ReadText("username"), Is.EqualTo("one"));
            Assert.That(body.ReadText("password"), Is.EqualTo("secret"));
        }

        [Test]
        public void CorpoDeRenovacaoTemSoRefresh()
        {
            IProtocolCodec codec = AccountTestCodec.Codec();

            IPayloadReader body = AccountTestCodec.Reader(codec.EncodeObject(writer => AccountRequestBodies.WriteRefresh(writer, new RefreshToken("r-1"))));

            Assert.That(body.FieldNames, Is.EquivalentTo(new[] { "refresh" }));
            Assert.That(body.ReadText("refresh"), Is.EqualTo("r-1"));
        }

        [Test]
        public void RespostaDeLoginDaAcessoERefresh()
        {
            DecodeOutcome<SessionTokens> issued = ReadLogin(AccountTestRig.LoginBody(FakeAccessJwt.FiveMinutes()));

            Assert.That(issued.Value.Access.Owner, Is.EqualTo(new UserId(7)));
            Assert.That(issued.Value.Access.ArrivedAt, Is.EqualTo(ArrivedAt));
            Assert.That(issued.Value.Refresh.RevealForRequest(), Is.EqualTo(AccountTestRig.RefreshText));
        }

        [Test]
        public void RespostaDeLoginSemTokenFalhaNoCampo()
        {
            DecodeOutcome<SessionTokens> issued = ReadLogin("{\"refresh\": \"r-1\"}");

            Assert.That(issued.Failure.Kind, Is.EqualTo(DecodeFailureKind.MissingField));
            Assert.That(issued.Failure.Path, Is.EqualTo("token"));
        }

        [Test]
        public void RespostaDeLoginSemRefreshFalhaNoCampo()
        {
            DecodeOutcome<SessionTokens> issued = ReadLogin("{\"token\": \"" + FakeAccessJwt.FiveMinutes() + "\"}");

            Assert.That(issued.Failure.Kind, Is.EqualTo(DecodeFailureKind.MissingField));
            Assert.That(issued.Failure.Path, Is.EqualTo("refresh"));
        }

        [Test]
        public void RespostaDeRenovacaoDaOAcesso()
        {
            DecodeOutcome<AccessToken> access = AccountRequestBodies.ReadRefreshedAccess(AccountTestCodec.Codec(), Reader(), AccountTestRig.RefreshBody(FakeAccessJwt.FiveMinutes("renewed")), ArrivedAt);

            Assert.That(access.Value.RevealForRequest(), Is.EqualTo(FakeAccessJwt.FiveMinutes("renewed")));
        }

        [Test]
        public void RespostaDeRenovacaoComTokenNoLugarDeAccessFalha()
        {
            DecodeOutcome<AccessToken> access = AccountRequestBodies.ReadRefreshedAccess(AccountTestCodec.Codec(), Reader(), "{\"token\": \"x\"}", ArrivedAt);

            Assert.That(access.Failure.Path, Is.EqualTo("access"));
        }

        [Test]
        public void CorpoQueNaoEhJsonFalhaSemEcoarOTexto()
        {
            DecodeOutcome<SessionTokens> issued = ReadLogin("<html> refresh-token-secret");

            Assert.That(issued.IsValid, Is.False);
            Assert.That(issued.Failure.Detail, Does.Not.Contain("refresh-token-secret"));
        }

        private static AccessTokenReader Reader() => new AccessTokenReader(AccountTestCodec.Codec());

        private static DecodeOutcome<SessionTokens> ReadLogin(string body)
        {
            return AccountRequestBodies.ReadLoginTokens(AccountTestCodec.Codec(), Reader(), body, ArrivedAt);
        }

        [Test]
        public void CorpoDeCadastroTemOsQuatroCamposDoSerializer()
        {
            IProtocolCodec codec = AccountTestCodec.Codec();
            RegistrationForm form = new RegistrationForm("one", "one@example.com", new Password("s3cret"), new Password("s3cret-2"));

            IPayloadReader body = AccountTestCodec.Reader(codec.EncodeObject(writer => AccountRequestBodies.WriteRegistration(writer, form)));

            Assert.That(body.ReadText("username"), Is.EqualTo("one"));
            Assert.That(body.ReadText("email"), Is.EqualTo("one@example.com"));
            Assert.That(body.ReadText("password"), Is.EqualTo("s3cret"));
            Assert.That(body.ReadText("password_confirmation"), Is.EqualTo("s3cret-2"));
        }
    }
}
