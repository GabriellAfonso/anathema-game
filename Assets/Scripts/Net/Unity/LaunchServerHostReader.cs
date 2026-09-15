#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_ANDROID && !UNITY_EDITOR
using UnityEngine.Android;
#endif

namespace Anathema.Net.Unity
{
    /// <summary>
    /// Lê parâmetros de inicialização do build de desenvolvimento, sem interface visual: extra
    /// de intent no Android (<c>adb shell am start … -e serverHost 192.168.0.10:8000</c>),
    /// argumento de linha de comando no Windows (<c>-serverHost 192.168.0.10:8000</c>).
    /// Build de produção nunca lê (specs/001-server-connection/research.md, R10).
    /// </summary>
    /// <example>
    /// <code>
    /// string? host = LaunchServerHostReader.ReadLaunchValue(LaunchServerHostReader.ServerHostName);
    /// </code>
    /// </example>
    internal static class LaunchServerHostReader
    {
        /// <summary>Parâmetro com o host do servidor.</summary>
        /// <example><code>ReadLaunchValue(LaunchServerHostReader.ServerHostName);</code></example>
        public const string ServerHostName = "serverHost";

        /// <summary>Parâmetro que liga o probe de conexão.</summary>
        /// <example><code>ReadLaunchValue(LaunchServerHostReader.ConnectionProbeName);</code></example>
        public const string ConnectionProbeName = "connectionProbe";

        /// <summary>Só build de desenvolvimento lê parâmetros.</summary>
        /// <example><code>if (!LaunchServerHostReader.ShouldRead(Debug.isDebugBuild)) return;</code></example>
        public static bool ShouldRead(bool developmentBuild) => developmentBuild;

        /// <summary>Valor que segue <paramref name="flag"/> nos argumentos, ou nulo.</summary>
        /// <example><code>string? host = LaunchServerHostReader.FindCommandLineValue(Environment.GetCommandLineArgs(), "-serverHost");</code></example>
        public static string? FindCommandLineValue(IReadOnlyList<string> arguments, string flag)
        {
            for (int index = 0; index < arguments.Count - 1; index++)
            {
                if (arguments[index] == flag && !arguments[index + 1].StartsWith("-", StringComparison.Ordinal))
                    return arguments[index + 1];
            }

            return null;
        }

        /// <summary>Valor do parâmetro <paramref name="name"/> nesta execução, ou nulo. Chamar na thread principal.</summary>
        /// <example><code>string? probe = LaunchServerHostReader.ReadLaunchValue("connectionProbe");</code></example>
        public static string? ReadLaunchValue(string name)
        {
            if (!ShouldRead(Debug.isDebugBuild))
                return null;

#if UNITY_ANDROID && !UNITY_EDITOR
            return ReadIntentExtra(name);
#else
            return FindCommandLineValue(Environment.GetCommandLineArgs(), "-" + name);
#endif
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        private static string? ReadIntentExtra(string name)
        {
            using AndroidJavaObject intent = AndroidApplication.currentActivity.Call<AndroidJavaObject>("getIntent");
            return intent.Call<string>("getStringExtra", name);
        }
#endif
    }
}
