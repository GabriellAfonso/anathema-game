#nullable enable
using System.Diagnostics.CodeAnalysis;
using UnityEditor;
using UnityEngine;

namespace Anathema.Net.Editor
{
    /// <summary>
    /// Lê, pelo nome dos campos serializados, qual <see cref="AppConfig"/> o
    /// <c>AppEnvManager</c> de uma cena seleciona. Por nome e não pelo tipo porque o
    /// <c>AppEnvManager</c> está no Assembly-CSharp, que nenhuma asmdef referencia, e esta
    /// feature não o altera (specs/001-server-connection/plan.md, Complexity Tracking).
    /// <c>AppEnvManagerFieldsTests</c> falha se um dos campos sumir.
    /// </summary>
    /// <example>
    /// <code>
    /// if (EnvironmentSelectionReader.TryRead(component, out EnvironmentSelection? selection)) Check(selection);
    /// </code>
    /// </example>
    public static class EnvironmentSelectionReader
    {
        /// <summary>Campo booleano de Assets/Scripts/Bootstrap/AppEnvManager.cs.</summary>
        /// <example><code>serialized.FindProperty(EnvironmentSelectionReader.IsProdField);</code></example>
        public const string IsProdField = "isProd";

        /// <summary>Campo do config de desenvolvimento de Assets/Scripts/Bootstrap/AppEnvManager.cs.</summary>
        /// <example><code>serialized.FindProperty(EnvironmentSelectionReader.DevConfigField);</code></example>
        public const string DevConfigField = "configDev";

        /// <summary>Campo do config de produção de Assets/Scripts/Bootstrap/AppEnvManager.cs.</summary>
        /// <example><code>serialized.FindProperty(EnvironmentSelectionReader.ProdConfigField);</code></example>
        public const string ProdConfigField = "configProd";

        /// <summary>Verdadeiro se o componente é um seletor de ambiente; a seleção sai em <paramref name="selection"/>.</summary>
        /// <example><code>bool isSelector = EnvironmentSelectionReader.TryRead(component, out EnvironmentSelection? selection);</code></example>
        public static bool TryRead(Component component, [NotNullWhen(true)] out EnvironmentSelection? selection)
        {
            selection = null;
            SerializedObject serialized = new SerializedObject(component);
            SerializedProperty? isProd = serialized.FindProperty(IsProdField);
            SerializedProperty? dev = serialized.FindProperty(DevConfigField);
            SerializedProperty? prod = serialized.FindProperty(ProdConfigField);
            if (isProd == null || dev == null || prod == null)
                return false;

            bool production = isProd.boolValue;
            SerializedProperty chosen = production ? prod : dev;
            selection = new EnvironmentSelection(production, production ? ProdConfigField : DevConfigField, chosen.objectReferenceValue as AppConfig);
            return true;
        }
    }
}
