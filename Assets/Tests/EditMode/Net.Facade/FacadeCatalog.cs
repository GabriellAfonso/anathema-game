#nullable enable
using System.Globalization;
using Anathema.Net.Account;
using Anathema.Net.Core;
using Anathema.Net.Fakes;
using Anathema.Net.Json;
using Anathema.Net.Match;

namespace Anathema.Net.Facade.Tests
{
    /// <summary>
    /// Catálogo dos testes da fachada, com as cartas que as fixtures de partida da 004 citam, montado pelo leitor
    /// real da conta. <see cref="Body"/> é o mesmo conteúdo como resposta HTTP do catálogo.
    /// </summary>
    internal static class FacadeCatalog
    {
        internal static LoadedCatalog Default(FakeClientLog log)
        {
            IProtocolCodec codec = new NewtonsoftProtocolCodec(MatchFrames.CreateUnion(), log);
            IPayloadReader body = codec.DecodeObject(Body()).Value;
            return new LoadedCatalog(new CatalogReader(log).Read(body), 1);
        }

        internal static string Body()
        {
            return "{\"cards\": [" + string.Join(", ", Unit(1, "JOHN COPPER", 1, 1, 2), Unit(2, "IRON GUARD", 2, 2, 3), Unit(5, "SAND WOLF", 3, 3, 4),
                Spell(1001, "LIFE POTION", 2, "none"), Spell(1002, "FIREBALL", 3, "enemy_unit")) + "]}";
        }

        private static string Unit(long cardId, string name, long energy, long attack, long health)
        {
            return "{\"card_id\": " + Number(cardId) + ", \"card_type\": \"unit\", \"name\": \"" + name + "\", \"energy\": " + Number(energy)
                + ", \"image\": \"unit_" + Number(cardId) + "\", \"attack\": " + Number(attack) + ", \"health\": " + Number(health) + "}";
        }

        private static string Spell(long cardId, string name, long energy, string targetKind)
        {
            string requiresTarget = targetKind == "none" ? "false" : "true";
            return "{\"card_id\": " + Number(cardId) + ", \"card_type\": \"spell\", \"name\": \"" + name + "\", \"energy\": " + Number(energy)
                + ", \"image\": \"spell_" + Number(cardId) + "\", \"description\": \"efeito\", \"effect\": {\"requires_target\": " + requiresTarget
                + ", \"target_kind\": \"" + targetKind + "\", \"duration\": \"permanent\", \"declaration_only\": false}}";
        }

        private static string Number(long value) => value.ToString(CultureInfo.InvariantCulture);
    }
}
