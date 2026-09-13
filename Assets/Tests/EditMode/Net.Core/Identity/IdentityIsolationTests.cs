#nullable enable
using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

namespace Anathema.Net.Core.Tests
{
    /// <summary>
    /// Trocar um identificador pelo outro precisa ser erro de compilação (constituição,
    /// princípio III). Compilação que falha não vira teste; o que dá para garantir aqui é
    /// que nenhum dos três ganhe uma conversão que abriria essa porta.
    /// </summary>
    public class IdentityIsolationTests
    {
        private static readonly Type[] IdentityTypes = { typeof(UserId), typeof(CardInstanceId), typeof(MatchId) };

        [Test]
        public void NenhumIdentificadorTemOperadorDeConversao()
        {
            string[] conversions = IdentityTypes
                .SelectMany(type => type.GetMethods(BindingFlags.Public | BindingFlags.Static))
                .Where(method => method.Name == "op_Implicit" || method.Name == "op_Explicit")
                .Select(method => $"{method.DeclaringType!.Name}.{method.Name}")
                .ToArray();

            Assert.That(conversions, Is.Empty);
        }

        [Test]
        public void NenhumConstrutorAceitaOutroIdentificador()
        {
            string[] crossConstructors = IdentityTypes
                .SelectMany(type => type.GetConstructors())
                .Where(ctor => ctor.GetParameters().Any(parameter => IdentityTypes.Contains(parameter.ParameterType)))
                .Select(ctor => ctor.DeclaringType!.Name)
                .ToArray();

            Assert.That(crossConstructors, Is.Empty);
        }
    }
}
