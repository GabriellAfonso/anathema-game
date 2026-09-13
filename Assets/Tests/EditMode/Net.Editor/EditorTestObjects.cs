#nullable enable
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Anathema.Net.Editor.Tests
{
    /// <summary>
    /// Cria configs e seletores de ambiente para os testes do gate, e destrói tudo no fim. O
    /// seletor é o próprio AppEnvManager, achado pelo MonoScript: um MonoBehaviour declarado nesta
    /// asmdef (só de editor) não pode ser adicionado a um GameObject.
    /// </summary>
    internal sealed class EditorTestObjects
    {
        private const string AppEnvManagerPath = "Assets/Scripts/Bootstrap/AppEnvManager.cs";
        private readonly List<Object> created = new List<Object>();

        internal static Type? AppEnvManagerType()
        {
            MonoScript? script = AssetDatabase.LoadAssetAtPath<MonoScript>(AppEnvManagerPath);
            return script == null ? null : script.GetClass();
        }

        internal AppConfig Config(string name, bool useTls)
        {
            AppConfig config = ScriptableObject.CreateInstance<AppConfig>();
            config.name = name;
            config.useTls = useTls;
            created.Add(config);
            return config;
        }

        internal GameObject Owner()
        {
            GameObject owner = new GameObject("environment selector");
            created.Add(owner);
            return owner;
        }

        internal Component Selector(bool production, AppConfig? development, AppConfig? productionConfig)
        {
            Type type = AppEnvManagerType() ?? throw new InvalidOperationException($"{AppEnvManagerPath} not found: expected the scene environment selector script");
            Component selector = Owner().AddComponent(type);
            SerializedObject serialized = new SerializedObject(selector);
            serialized.FindProperty(EnvironmentSelectionReader.IsProdField).boolValue = production;
            serialized.FindProperty(EnvironmentSelectionReader.DevConfigField).objectReferenceValue = development;
            serialized.FindProperty(EnvironmentSelectionReader.ProdConfigField).objectReferenceValue = productionConfig;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return selector;
        }

        internal void DestroyAll()
        {
            foreach (Object item in created)
                Object.DestroyImmediate(item);

            created.Clear();
        }
    }
}
