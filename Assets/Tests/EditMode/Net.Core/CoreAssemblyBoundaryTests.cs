#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

namespace Anathema.Net.Core.Tests
{
    /// <summary>
    /// Fronteira do núcleo (SC-002; constituição, princípio VI). A asmdef já tem
    /// noEngineReferences, mas isso não impede Newtonsoft nem System.Net.WebSockets.
    /// </summary>
    public class CoreAssemblyBoundaryTests
    {
        private const BindingFlags Everything =
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance |
            BindingFlags.Static | BindingFlags.DeclaredOnly;

        private static readonly string[] ForbiddenAssemblyPrefixes = { "UnityEngine", "UnityEditor", "Newtonsoft" };

        private static Assembly CoreAssembly => typeof(MonotonicInstant).Assembly;

        [Test]
        public void NucleoNaoReferenciaMotorNemNewtonsoft()
        {
            string[] forbidden = CoreAssembly.GetReferencedAssemblies()
                .Select(reference => reference.Name ?? string.Empty)
                .Where(name => ForbiddenAssemblyPrefixes.Any(prefix => name.StartsWith(prefix, StringComparison.Ordinal)))
                .ToArray();

            Assert.That(forbidden, Is.Empty);
        }

        [Test]
        public void NenhumTipoDoNucleoUsaWebSocketsDoDotNet()
        {
            string[] offenders = CoreAssembly.GetTypes()
                .SelectMany(type => SignatureTypes(type).Select(used => $"{type.FullName} -> {used.FullName}"))
                .Where(pair => pair.Contains("System.Net.WebSockets"))
                .ToArray();

            Assert.That(offenders, Is.Empty);
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
