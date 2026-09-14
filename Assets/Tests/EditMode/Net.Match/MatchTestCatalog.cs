#nullable enable
using System.Globalization;
using Anathema.Net.Account;
using Anathema.Net.Core;
using Anathema.Net.Fakes;
using Anathema.Net.Json;

namespace Anathema.Net.Match.Tests
{
    /// <summary>
    /// Catálogo de teste montado pelo leitor real da conta (specs/004-match-session/research.md, R13).
    /// <see cref="Default"/> tem as cartas que as fixtures de contrato citam.
    /// </summary>
    internal static class MatchTestCatalog
    {
        internal const long JohnCopper = 1;
        internal const long IronGuard = 2;
        internal const long SandWolf = 5;
        internal const long LifePotion = 1001;
        internal const long Fireball = 1002;
        internal const long SacrificialFire = 1003;
        internal const long MagicBarrier = 1004;

        internal static LoadedCatalog Default()
        {
            return Build(
                Unit(JohnCopper, "JOHN COPPER", 1, 1, 2),
                Unit(IronGuard, "IRON GUARD", 2, 2, 3),
                Unit(SandWolf, "SAND WOLF", 3, 3, 4),
                Spell(LifePotion, "LIFE POTION", 2, "none", false),
                Spell(Fireball, "FIREBALL", 3, "enemy_unit", false),
                Spell(SacrificialFire, "SACRIFICIAL FIRE", 2, "none", true),
                Spell(MagicBarrier, "MAGIC BARRIER", 1, "allied_unit", false));
        }

        internal static LoadedCatalog Build(params string[] items)
        {
            FakeClientLog log = new FakeClientLog();
            IProtocolCodec codec = new NewtonsoftProtocolCodec(MatchFrames.CreateUnion(), log);
            IPayloadReader body = codec.DecodeObject("{\"cards\": [" + string.Join(", ", items) + "]}").Value;
            return new LoadedCatalog(new CatalogReader(log).Read(body), 1);
        }

        internal static string Unit(long cardId, string name, long energy, long attack, long health)
        {
            return "{\"card_id\": " + Number(cardId) + ", \"card_type\": \"unit\", \"name\": \"" + name + "\", \"energy\": " + Number(energy)
                + ", \"image\": \"unit_" + Number(cardId) + "\", \"attack\": " + Number(attack) + ", \"health\": " + Number(health) + "}";
        }

        internal static string Spell(long cardId, string name, long energy, string targetKind, bool declarationOnly)
        {
            string requiresTarget = targetKind == "none" ? "false" : "true";
            return "{\"card_id\": " + Number(cardId) + ", \"card_type\": \"spell\", \"name\": \"" + name + "\", \"energy\": " + Number(energy)
                + ", \"image\": \"spell_" + Number(cardId) + "\", \"description\": \"efeito\", \"effect\": {\"requires_target\": " + requiresTarget
                + ", \"target_kind\": \"" + targetKind + "\", \"duration\": \"permanent\", \"declaration_only\": " + (declarationOnly ? "true" : "false") + "}}";
        }

        private static string Number(long value) => value.ToString(CultureInfo.InvariantCulture);
    }
}
