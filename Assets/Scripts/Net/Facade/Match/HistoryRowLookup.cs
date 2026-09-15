#nullable enable
using System;
using System.Linq;
using System.Threading.Tasks;
using Anathema.Net.Account;
using Anathema.Net.Core;

namespace Anathema.Net.Facade
{
    /// <summary>
    /// Busca a linha do histórico da partida que acabou, porque o servidor grava depois de entregar o último frame:
    /// até 4 leituras da primeira página, a primeira na hora e as outras 1, 2 e 4 s depois da anterior, contadas no
    /// relógio monotônico a cada tique, sem temporizador (specs/005-presentation-facade/research.md, R6).
    /// </summary>
    internal sealed class HistoryRowLookup : IDisposable
    {
        private static readonly TimeSpan[] Waits = { TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(4) };

        private readonly MatchHistory history;
        private readonly ClientPorts ports;
        private readonly ClientStages stages;
        private readonly Action<MatchResult> resultUpdated;
        private MatchResult? pending;
        private int generation;
        private int attempts;
        private bool reading;
        private MonotonicInstant? nextReadAt;

        internal HistoryRowLookup(MatchHistory history, ClientPorts ports, ClientStages stages, Action<MatchResult> resultUpdated)
        {
            this.history = history;
            this.ports = ports;
            this.stages = stages;
            this.resultUpdated = resultUpdated;
            ports.Ticker.Ticked += OnTick;
        }

        internal void Start(MatchResult fetching)
        {
            pending = fetching;
            generation = stages.Generation;
            attempts = 0;
            nextReadAt = null;
            Read();
        }

        internal void Stop()
        {
            pending = null;
            nextReadAt = null;
        }

        public void Dispose()
        {
            Stop();
            ports.Ticker.Ticked -= OnTick;
        }

        private void OnTick()
        {
            if (pending == null || reading || nextReadAt == null || ports.Clock.Now < nextReadAt.Value)
                return;

            nextReadAt = null;
            Read();
        }

        private void Read()
        {
            attempts++;
            reading = true;
            _ = ReadAsync(pending!, generation);
        }

        private async Task ReadAsync(MatchResult fetching, int readGeneration)
        {
            AccountCallOutcome<MatchHistoryPage, HistoryRefusal>? page = null;
            try
            {
                page = await history.ReadPageAsync(new HistoryPageRequest());
            }
            catch (Exception failure)
            {
                ports.Log.Error("match_history_read_failed", new LogField("match_id", fetching.Match.Value), new LogField("exception", failure.GetType().Name));
            }

            ports.Queue.Enqueue(() => AfterRead(fetching, readGeneration, page));
        }

        private void AfterRead(MatchResult fetching, int readGeneration, AccountCallOutcome<MatchHistoryPage, HistoryRefusal>? page)
        {
            reading = false;
            if (!ReferenceEquals(fetching, pending) || readGeneration != stages.Generation || stages.State.Stage != ClientStage.MatchFinished)
                return;

            // Falha de transporte e recusa contam como tentativa sem a linha.
            MatchHistoryRow? row = page != null && page.IsSuccess ? page.Value.Rows.FirstOrDefault(candidate => candidate.Match.Equals(fetching.Match)) : null;
            if (row != null)
                Resolve(fetching, row);
            else if (attempts >= MatchResult.MaxAttempts)
                Publish(fetching.Unavailable(attempts));
            else
                nextReadAt = ports.Clock.Now.Add(Waits[attempts - 1]);
        }

        private void Resolve(MatchResult fetching, MatchHistoryRow row)
        {
            // As duas fontes ficam como vieram: o cliente não corrige o servidor nem o espelho.
            if (row.Won != fetching.Won)
                ports.Log.Error("match_history_disagrees", new LogField("match_id", fetching.Match.Value), new LogField("won", row.Won), new LogField("did_i_win", fetching.Won));

            Publish(fetching.Resolved(row, attempts));
        }

        private void Publish(MatchResult result)
        {
            pending = null;
            stages.Replace(stages.State.WithResult(result));
            ports.Log.Info("match_history_row", new LogField("match_id", result.Match.Value), new LogField("status", result.RowStatus.ToString()), new LogField("attempts", result.Attempts));
            resultUpdated(result);
        }
    }
}
