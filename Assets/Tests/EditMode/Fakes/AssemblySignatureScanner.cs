#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Anathema.Net.Fakes
{
    /// <summary>
    /// Varre as assinaturas de todos os tipos de um assembly (campos, propriedades, eventos,
    /// parâmetros e retornos, inclusive genéricos e arrays). A asmdef com noEngineReferences
    /// não impede Newtonsoft nem System.Net.WebSockets; os testes de fronteira do núcleo e da
    /// conta usam esta varredura para pegar o que a configuração deixa passar.
    /// </summary>
    /// <example>
    /// <code>
    /// string[] offenders = AssemblySignatureScanner.FindUsages(typeof(MonotonicInstant).Assembly, "System.Net.WebSockets");
    /// </code>
    /// </example>
    public static class AssemblySignatureScanner
    {
        private const BindingFlags Everything =
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance |
            BindingFlags.Static | BindingFlags.DeclaredOnly;

        /// <summary>Pares "tipo -> tipo usado" em que o nome completo contém o trecho.</summary>
        /// <example><code>string[] networking = AssemblySignatureScanner.FindUsages(accountAssembly, "UnityEngine.Networking");</code></example>
        public static string[] FindUsages(Assembly assembly, string namespaceFragment)
        {
            return assembly.GetTypes()
                .SelectMany(type => SignatureTypes(type).Select(used => $"{type.FullName} -> {used.FullName}"))
                .Where(pair => pair.Contains(namespaceFragment))
                .ToArray();
        }

        /// <summary>Assemblies referenciados cujo nome começa com algum dos prefixos.</summary>
        /// <example><code>string[] engine = AssemblySignatureScanner.FindReferencesStartingWith(coreAssembly, "UnityEngine", "Newtonsoft");</code></example>
        public static string[] FindReferencesStartingWith(Assembly assembly, params string[] prefixes)
        {
            return assembly.GetReferencedAssemblies()
                .Select(reference => reference.Name ?? string.Empty)
                .Where(name => prefixes.Any(prefix => name.StartsWith(prefix, StringComparison.Ordinal)))
                .ToArray();
        }

        private static IEnumerable<Type> SignatureTypes(Type type)
        {
            IEnumerable<Type> direct = type.GetFields(Everything).Select(field => field.FieldType)
                .Concat(type.GetProperties(Everything).Select(property => property.PropertyType))
                .Concat(type.GetEvents(Everything).Select(evt => evt.EventHandlerType ?? typeof(void)))
                .Concat(type.GetMethods(Everything).SelectMany(MethodTypes))
                .Concat(type.GetConstructors(Everything).SelectMany(ctor => ctor.GetParameters().Select(p => p.ParameterType)));

            return direct.SelectMany(Expand);
        }

        private static IEnumerable<Type> MethodTypes(MethodInfo method)
        {
            return method.GetParameters().Select(parameter => parameter.ParameterType).Append(method.ReturnType);
        }

        private static IEnumerable<Type> Expand(Type type)
        {
            yield return type;
            Type[] nested = type.HasElementType ? new[] { type.GetElementType()! } : type.GetGenericArguments();
            foreach (Type inner in nested.SelectMany(Expand))
                yield return inner;
        }
    }
}
