#nullable enable
using System;
using Anathema.Net.Account;
using Anathema.Net.Core;
using Anathema.Net.Facade;
using Anathema.Net.Fakes;
using NUnit.Framework;

namespace Anathema.Net.Unity.Tests
{
    /// <summary>research R8 da 005: as portas reais saem dos adaptadores e do que quem compõe entrega.</summary>
    public class LivePortsTests
    {
        private FakeClientLog log = null!;
        private MainThreadQueue queue = null!;
        private LiveNetworkAdapters adapters = null!;

        [SetUp]
        public void CreateAdapters()
        {
            log = new FakeClientLog();
            queue = new MainThreadQueue(log);
            adapters = LiveNetworkAdapters.Create(queue, log, new CleartextPolicy(true));
        }

        [TearDown]
        public void CloseQueue() => queue.Close();

        [Test]
        public void PortasVemDosAdaptadoresEDoQueFoiEntregue()
        {
            FakeMonotonicClock clock = new FakeMonotonicClock();
            FakeAppLifecycle lifecycle = new FakeAppLifecycle(clock);
            FakeRefreshTokenVault vault = new FakeRefreshTokenVault();

            ClientPorts ports = LivePorts.Create(adapters, lifecycle, new FakeNetworkReachability(NetworkKind.LocalArea), new FakeFrameTicker(), vault,
                LocalAccountRoutes.Create(), LocalConnectionRoutes.Create());

            Assert.That((ports.Http, ports.Sockets, ports.Clock, ports.Codec, ports.Queue, ports.Log), Is.EqualTo((adapters.Http, adapters.Sockets, adapters.Clock, adapters.Codec, adapters.Queue, adapters.Log)));
            Assert.That((ports.Lifecycle, ports.Vault), Is.EqualTo(((IAppLifecycle)lifecycle, (IRefreshTokenVault)vault)));
            Assert.That(ports.Timing.RenewalMargin, Is.EqualTo(AccountTiming.DefaultRenewalMargin));
        }

        [Test]
        public void SemAdaptadoresLanca()
        {
            FakeMonotonicClock clock = new FakeMonotonicClock();

            Assert.Throws<ArgumentNullException>(() => LivePorts.Create(null!, new FakeAppLifecycle(clock), new FakeNetworkReachability(NetworkKind.LocalArea), new FakeFrameTicker(),
                new FakeRefreshTokenVault(), LocalAccountRoutes.Create(), LocalConnectionRoutes.Create()));
        }
    }
}
