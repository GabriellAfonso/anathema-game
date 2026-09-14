#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Anathema.Net.Core;
using NUnit.Framework;

namespace Anathema.Net.Match.Tests
{
    /// <summary>
    /// US4-7, research R7: jogada só aceita cópia na partida. A 001 já impede converter <see cref="CardId"/> em
    /// <see cref="CardInstanceId"/>; aqui se garante que nenhuma entrada pública aceite o identificador errado
    /// nem um número solto.
    /// </summary>
    public class CommandIdentityTests
    {
        private static readonly Type[] Forbidden = { typeof(CardId), typeof(long), typeof(int) };

        [Test]
        public void HaOnzeMensagensDeComando()
        {
            Assert.That(CommandTypes().Count(), Is.EqualTo(11));
        }

        [Test]
        public void NenhumConstrutorDeComandoAceitaCardIdOuNumero()
        {
            Assert.That(Offenders(CommandTypes().SelectMany(type => type.GetConstructors())), Is.Empty);
        }

        [Test]
        public void NenhumMetodoPublicoDeMatchCommandsAceitaCardIdOuNumero()
        {
            MethodInfo[] methods = typeof(MatchCommands).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

            Assert.That(methods, Has.Length.EqualTo(13));
            Assert.That(Offenders(methods), Is.Empty);
        }

        private static string[] Offenders(IEnumerable<MethodBase> members)
        {
            return members
                .SelectMany(member => member.GetParameters().Select(parameter => (member, parameter)))
                .Where(pair => IsForbidden(pair.parameter.ParameterType))
                .Select(pair => $"{pair.member.DeclaringType!.Name}.{pair.member.Name}({pair.parameter.Name}: {pair.parameter.ParameterType.Name})")
                .ToArray();
        }

        internal static bool IsForbidden(Type parameter)
        {
            Type candidate = Nullable.GetUnderlyingType(parameter) ?? parameter;
            if (Forbidden.Contains(candidate))
                return true;

            return candidate.IsGenericType && candidate.GetGenericArguments().Any(IsForbidden);
        }

        private static IEnumerable<Type> CommandTypes()
        {
            return typeof(PlayCommand).Assembly.GetTypes().Where(type => type.IsSubclassOf(typeof(PlayCommand)) && !type.IsAbstract && type.IsPublic);
        }
    }
}
