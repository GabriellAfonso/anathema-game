#nullable enable
using System;
using System.Collections.Generic;
using Anathema.Net.Connection;
using NUnit.Framework;

namespace Anathema.Net.Match.Tests
{
    /// <summary>Sessão de partida sobre o <see cref="MatchTestRig"/>, com os estados gravados em ordem.</summary>
    public abstract class LiveMatchTestBase
    {
        internal MatchTestRig Rig { get; private set; } = null!;

        internal AuthenticatedConnection Connection { get; private set; } = null!;

        internal LiveMatch Live { get; private set; } = null!;

        internal List<LiveMatchStatus> Statuses { get; private set; } = null!;

        [SetUp]
        public void CreateLiveMatch()
        {
            Rig = new MatchTestRig();
            Connection = Rig.Connection();
            Live = new LiveMatch(Connection, MatchTestRig.MatchBase, MatchTestRig.Match, Rig.Catalog, Rig.Clock, Rig.Log);
            Statuses = new List<LiveMatchStatus>();
            Live.StatusChanged += Statuses.Add;
        }

        internal void StartAndOpen()
        {
            Live.Start();
            Rig.OpenLatest();
        }

        internal void GoLive(string firstFrame)
        {
            StartAndOpen();
            Rig.Receive(firstFrame);
        }

        internal void DropAndReopen(TimeSpan away)
        {
            Rig.Sockets.Latest.SimulateClosed(null, "queda");
            Rig.Advance(away);
            Rig.OpenLatest();
        }
    }
}
