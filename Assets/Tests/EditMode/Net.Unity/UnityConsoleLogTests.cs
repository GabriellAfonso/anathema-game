#nullable enable
using Anathema.Net.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Anathema.Net.Unity.Tests
{
    public class UnityConsoleLogTests
    {
        [Test]
        public void CadaNivelVaiParaOTipoDeLogCorrespondente()
        {
            UnityConsoleLog log = new UnityConsoleLog();

            LogAssert.Expect(LogType.Log, "socket_opened url=ws://x");
            LogAssert.Expect(LogType.Warning, "socket_binary_frame_ignored chars=3");
            LogAssert.Expect(LogType.Error, "main_thread_item_failed exception=InvalidOperationException");

            log.Info("socket_opened", new LogField("url", "ws://x"));
            log.Warning("socket_binary_frame_ignored", new LogField("chars", 3));
            log.Error("main_thread_item_failed", new LogField("exception", "InvalidOperationException"));
        }
    }
}
