#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Anathema.Net.Core;

namespace Anathema.Net.Unity
{
    /// <summary>
    /// Os três passos que provam os adaptadores reais contra o backend: HTTP sem token recebe
    /// 401, socket com token inválido recebe <c>auth_denied</c> e 4001, e um usuário novo faz
    /// <c>ping</c>/<c>pong</c> com o mesmo marcador. Usados pelos testes LiveServer e pelo probe
    /// do aparelho, para os dois provarem a mesma coisa (specs/001-server-connection/research.md, R11).
    /// Cadastro e login seguem backend/scripts/smoke_match.py (sign_up, log_in).
    /// </summary>
    /// <example>
    /// <code>
    /// LiveServerProbeSteps steps = new LiveServerProbeSteps(adapters, "http://192.168.0.10:8000", "ws://192.168.0.10:8000");
    /// ProbeStepResult result = await steps.CheckPingPongAsync();
    /// </code>
    /// </example>
    public sealed class LiveServerProbeSteps
    {
        private const string ProbePassword = "probe-pass-123";
        private static readonly TimeSpan SocketWait = TimeSpan.FromSeconds(10);

        private readonly LiveNetworkAdapters adapters;
        private readonly string httpBase;
        private readonly string wsBase;

        /// <summary>Cria os passos para um servidor.</summary>
        /// <example><code>LiveServerProbeSteps steps = new LiveServerProbeSteps(adapters, "http://127.0.0.1:8000", "ws://127.0.0.1:8000");</code></example>
        public LiveServerProbeSteps(LiveNetworkAdapters adapters, string httpBase, string wsBase)
        {
            this.adapters = adapters ?? throw new ArgumentNullException(nameof(adapters), "adapters are null: expected LiveNetworkAdapters.Create(...)");
            this.httpBase = (httpBase ?? string.Empty).TrimEnd('/');
            this.wsBase = (wsBase ?? string.Empty).TrimEnd('/');
        }

        /// <summary><c>GET /game/cards/</c> sem token precisa ser resposta 401.</summary>
        /// <example><code>ProbeStepResult result = await steps.CheckUnauthorizedHttpAsync();</code></example>
        public async Task<ProbeStepResult> CheckUnauthorizedHttpAsync()
        {
            HttpOutcome outcome = await adapters.Http.SendAsync(new HttpRequestSpec("GET", new Uri(httpBase + "/game/cards/")));
            int status = outcome.AsResponse?.Status ?? 0;
            string failure = outcome.AsFailure?.Kind.ToString() ?? "none";
            int[] threads = { Thread.CurrentThread.ManagedThreadId };
            return Report("http_unauthorized", status == 401, threads, new LogField("status", status), new LogField("transport_failure", failure));
        }

        /// <summary><c>ws/matchmaking/</c> com token inválido precisa receber <c>auth_denied</c> e fechar com 4001.</summary>
        /// <example><code>ProbeStepResult result = await steps.CheckAuthDeniedAsync();</code></example>
        public async Task<ProbeStepResult> CheckAuthDeniedAsync()
        {
            ProbeSocketSession session = ProbeSocketSession.Open(adapters, new Uri(wsBase + "/ws/matchmaking/?token=invalid"));
            bool closedInTime = await CompletesInTime(session.Closed);
            int? code = closedInTime ? session.Closed.Result.Code : null;
            bool passed = session.Denied() && code == 4001;
            return Report("auth_denied", passed, session.ThreadIds, new LogField("close_code", code.HasValue ? code.Value.ToString() : "none"), new LogField("auth_denied_received", session.Denied()));
        }

        /// <summary>Usuário novo, socket autenticado, <c>ping</c> com marcador e <c>pong</c> com o mesmo marcador.</summary>
        /// <example><code>ProbeStepResult result = await steps.CheckPingPongAsync();</code></example>
        public async Task<ProbeStepResult> CheckPingPongAsync()
        {
            string? token = await RegisterAndLogInAsync();
            if (token == null)
                return Report("ping_pong", false, Array.Empty<int>(), new LogField("failure", "login_failed"));

            ProbeSocketSession session = ProbeSocketSession.Open(adapters, new Uri(wsBase + "/ws/matchmaking/?token=" + Uri.EscapeDataString(token)));
            await CompletesInTime(Task.WhenAny(session.Opened, session.Closed));
            PingMarker marker = new PingMarker(adapters.Clock.Now.Ticks / TimeSpan.TicksPerMillisecond, 1);
            SocketSendOutcome sent = await session.SendAsync(new PingMessage(marker));
            bool matched = await CompletesInTime(session.FirstPong) && marker.Equals(session.FirstPong.Result.Marker);
            session.Close();
            return Report("ping_pong", matched, session.ThreadIds, new LogField("send", sent.Status.ToString()), new LogField("marker_matched", matched));
        }

        private async Task<string?> RegisterAndLogInAsync()
        {
            string username = "probe_" + Guid.NewGuid().ToString("N").Substring(0, 12);
            HttpOutcome registered = await PostJsonAsync("/accounts/register/", writer => WriteRegistration(writer, username));
            if (registered.AsResponse?.Status != 201)
                return null;

            HttpOutcome login = await PostJsonAsync("/accounts/login/", writer => WriteCredentials(writer, username));
            if (login.AsResponse?.Status != 200)
                return null;

            DecodeOutcome<IPayloadReader> body = adapters.Codec.DecodeObject(login.AsResponse.Body);
            return body.IsValid ? body.Value.ReadOptionalText("token") : null;
        }

        private Task<HttpOutcome> PostJsonAsync(string path, Action<IPayloadWriter> writeBody)
        {
            Dictionary<string, string> headers = new Dictionary<string, string> { ["Content-Type"] = "application/json" };
            return adapters.Http.SendAsync(new HttpRequestSpec("POST", new Uri(httpBase + path), headers, adapters.Codec.EncodeObject(writeBody)));
        }

        private static void WriteRegistration(IPayloadWriter writer, string username)
        {
            writer.WriteText("username", username);
            writer.WriteText("email", username + "@probe.local");
            writer.WriteText("password", ProbePassword);
            writer.WriteText("password_confirmation", ProbePassword);
        }

        private static void WriteCredentials(IPayloadWriter writer, string username)
        {
            writer.WriteText("username", username);
            writer.WriteText("password", ProbePassword);
        }

        private static async Task<bool> CompletesInTime(Task task)
        {
            Task first = await Task.WhenAny(task, Task.Delay(SocketWait));
            return first == task;
        }

        private ProbeStepResult Report(string step, bool passed, IReadOnlyCollection<int> threads, params LogField[] fields)
        {
            ProbeStepResult result = new ProbeStepResult(step, passed, fields, threads);
            LogField[] logged = new[] { new LogField("step", step), new LogField("passed", passed) }.Concat(fields).ToArray();
            adapters.Log.Info("connection_probe_step", logged);
            return result;
        }
    }
}
