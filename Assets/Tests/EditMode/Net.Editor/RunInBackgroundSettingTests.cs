#nullable enable
using NUnit.Framework;
using UnityEditor;

namespace Anathema.Net.Editor.Tests
{
    /// <summary>
    /// FR-026: no Windows o jogo segue rodando minimizado, e a conexão não pode cair nem suspender por
    /// isso (specs/003-authenticated-socket-queue/research.md, R9).
    /// </summary>
    public class RunInBackgroundSettingTests
    {
        [Test]
        public void BuildWindowsSegueRodandoMinimizado()
        {
            Assert.That(PlayerSettings.runInBackground, Is.True,
                "runInBackground is false: expected Project Settings > Player > Resolution and Presentation > Run In Background on, so minimizing on Windows keeps the queue socket alive");
        }
    }
}
