#nullable enable
using System;
using System.Linq;
using System.Threading.Tasks;
using Anathema.Net.Core;
using UnityEngine;

namespace Anathema.Net.Unity
{
    /// <summary>
    /// Probe do aparelho: com <c>-e connectionProbe true</c> num build de desenvolvimento, sobe
    /// a camada de rede e roda os três passos de <see cref="LiveServerProbeSteps"/> contra o host
    /// passado em <c>serverHost</c>. O resultado sai no <c>adb logcat -s Unity</c>
    /// (specs/001-server-connection/quickstart.md, §4 a §6).
    /// </summary>
    /// <example>
    /// <code>
    /// // adb shell am start -n &lt;pacote&gt;/com.unity3d.player.UnityPlayerGameActivity -e serverHost 192.168.0.10:8000 -e connectionProbe true
    /// </code>
    /// </example>
    public static class DevConnectionProbe
    {
        private const string DefaultHost = "127.0.0.1:8000";

        /// <summary>Roda só em build de desenvolvimento e com o parâmetro igual a <c>true</c>.</summary>
        /// <example><code>bool run = DevConnectionProbe.ShouldRun(Debug.isDebugBuild, "true");</code></example>
        public static bool ShouldRun(bool developmentBuild, string? probeValue)
        {
            return developmentBuild && string.Equals(probeValue, "true", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Bases HTTP e WebSocket de um host.</summary>
        /// <example><code>(string http, string ws) = DevConnectionProbe.ResolveBases("192.168.0.10:8000", useTls: false);</code></example>
        public static (string httpBase, string wsBase) ResolveBases(string hostAndPort, bool useTls)
        {
            return ((useTls ? "https://" : "http://") + hostAndPort, (useTls ? "wss://" : "ws://") + hostAndPort);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void StartIfRequested()
        {
            if (!ShouldRun(Debug.isDebugBuild, LaunchServerHostReader.ReadLaunchValue(LaunchServerHostReader.ConnectionProbeName)))
                return;

            UnityConsoleLog log = new UnityConsoleLog();
            MainThreadQueue queue = new MainThreadQueue(log);
            LiveNetworkAdapters adapters = LiveNetworkAdapters.Create(queue, log, new CleartextPolicy(Debug.isDebugBuild));
            AttachHost(adapters);
            _ = RunStepsAsync(adapters, ResolveHost(log));
        }

        private static string ResolveHost(IClientLog log)
        {
            string? raw = LaunchServerHostReader.ReadLaunchValue(LaunchServerHostReader.ServerHostName);
            if (ServerHost.TryParse(raw, out ServerHost? host, out string problem))
                return host.HostAndPort;

            log.Warning("server_host_rejected", new LogField("raw", raw ?? "none"), new LogField("problem", problem), new LogField("using", DefaultHost));
            return DefaultHost;
        }

        private static void AttachHost(LiveNetworkAdapters adapters)
        {
            GameObject layer = new GameObject("NetworkLayer (connection probe)");
            UnityEngine.Object.DontDestroyOnLoad(layer);
            UnityAppLifecycle lifecycle = UnityAppLifecycle.Create(adapters.Clock, adapters.Queue);
            UnityNetworkReachability reachability = UnityNetworkReachability.Create(adapters.Clock, adapters.Queue);
            LogSignals(adapters.Log, lifecycle, reachability);
            layer.AddComponent<NetworkLayerHost>().Attach(adapters.Queue, lifecycle, reachability, new UnityFrameTicker());
        }

        private static void LogSignals(IClientLog log, IAppLifecycle lifecycle, INetworkReachability reachability)
        {
            lifecycle.WentToBackground += _ => log.Info("app_background");
            lifecycle.ReturnedToForeground += signal => log.Info("app_foreground", new LogField("away_ms", (long)signal.AwayFor.TotalMilliseconds));
            reachability.Changed += change => log.Info("network_kind_changed",
                new LogField("previous", change.Previous.ToString()), new LogField("current", change.Current.ToString()));
        }

        private static async Task RunStepsAsync(LiveNetworkAdapters adapters, string host)
        {
            try
            {
                (string httpBase, string wsBase) = ResolveBases(host, useTls: false);
                LiveServerProbeSteps steps = new LiveServerProbeSteps(adapters, httpBase, wsBase);
                ProbeStepResult[] results = { await steps.CheckUnauthorizedHttpAsync(), await steps.CheckAuthDeniedAsync(), await steps.CheckPingPongAsync() };
                ReportOutcome(adapters.Log, host, results);
            }
            catch (Exception failure)
            {
                adapters.Log.Error("connection_probe_failed", new LogField("step", "unexpected"), new LogField("detail", failure.ToString()));
            }
        }

        private static void ReportOutcome(IClientLog log, string host, ProbeStepResult[] results)
        {
            ProbeStepResult? failed = results.FirstOrDefault(result => !result.Passed);
            if (failed == null)
                log.Info("connection_probe_passed", new LogField("host", host));
            else
                log.Error("connection_probe_failed", new LogField("step", failed.StepName), new LogField("detail", failed.ToString()));
        }
    }
}
