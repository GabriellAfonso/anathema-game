#nullable enable
using System;
using System.Linq;
using System.Threading.Tasks;
using Anathema.Net.Core;
using Anathema.Net.Fakes;

namespace Anathema.Net.Account.Tests
{
    /// <summary>
    /// Sessão de conta montada sobre os fakes, com o codec real. Um lugar só para compor, para os
    /// testes de sessão, renovação, cliente e serviços não repetirem a montagem.
    /// </summary>
    internal sealed class AccountTestRig
    {
        internal const string Username = "player-one";
        internal const string PasswordText = "pass-word-secret";
        internal const string RefreshText = "refresh-token-secret";

        internal AccountTestRig()
        {
            Lifecycle = new FakeAppLifecycle(Clock);
            Codec = AccountTestCodec.Codec(Log);
            Session = new AccountSession(Http, Codec, Clock, Vault, Log, Routes);
            Tokens = new SessionAccessTokens(Session, Clock, Timing);
            Client = new AuthenticatedHttpClient(Http, Session, Tokens);
        }

        internal FakeHttpTransport Http { get; } = new FakeHttpTransport();

        internal FakeMonotonicClock Clock { get; } = new FakeMonotonicClock();

        internal FakeRefreshTokenVault Vault { get; } = new FakeRefreshTokenVault();

        internal FakeClientLog Log { get; } = new FakeClientLog();

        internal FakeAppLifecycle Lifecycle { get; }

        internal IProtocolCodec Codec { get; }

        internal AccountRoutes Routes { get; } = AccountRoutesTests.TestRoutes();

        internal AccountTiming Timing { get; } = new AccountTiming();

        internal AccountSession Session { get; }

        internal SessionAccessTokens Tokens { get; }

        internal AuthenticatedHttpClient Client { get; }

        internal static string LoginBody(string accessJwt, string refresh = RefreshText) => FakeAccountResponses.Login(accessJwt, refresh);

        internal static string RefreshBody(string accessJwt) => FakeAccountResponses.Refresh(accessJwt);

        internal async Task SignInAsync(string tokenId = "login")
        {
            Http.RespondNext(200, LoginBody(FakeAccessJwt.FiveMinutes(tokenId)));
            SignInOutcome outcome = await Session.SignInAsync(Username, new Password(PasswordText));
            if (outcome.Kind != SignInOutcomeKind.SignedIn)
                throw new InvalidOperationException($"rig sign in ended as {outcome}: expected SignedIn");
        }

        internal int RequestsTo(Uri url) => Http.Requests.Count(request => request.Url == url);
    }
}
