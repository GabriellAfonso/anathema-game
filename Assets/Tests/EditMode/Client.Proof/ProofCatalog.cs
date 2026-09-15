#nullable enable
using Anathema.Net.Account;
using Anathema.Net.Core;
using Anathema.Net.Fakes;
using Anathema.Net.Json;
using Anathema.Net.Match;

namespace Anathema.Client.Proof.Tests
{
    /// <summary>
    /// Catálogo das cartas que as fixtures de contrato da partida citam, montado pelo leitor real da conta, para
    /// testar a estratégia da prova sem backend. A <c>LIFE POTION</c> entra para a extensão do feitiço sem alvo.
    /// </summary>
    internal static class ProofCatalog
    {
        internal const long PotionCard = 1005;

        private static readonly string Cards = "{\"cards\": ["
            + "{\"card_id\": 1, \"card_type\": \"unit\", \"name\": \"JOHN COPPER\", \"energy\": 1, \"image\": \"u1\", \"attack\": 1, \"health\": 2}, "
            + "{\"card_id\": 2, \"card_type\": \"unit\", \"name\": \"IRON GUARD\", \"energy\": 2, \"image\": \"u2\", \"attack\": 2, \"health\": 3}, "
            + "{\"card_id\": 5, \"card_type\": \"unit\", \"name\": \"SAND WOLF\", \"energy\": 3, \"image\": \"u5\", \"attack\": 3, \"health\": 4}, "
            + Spell(1002, "FIREBALL", 3, "enemy_unit", false) + ", "
            + Spell(1003, "SACRIFICIAL FIRE", 2, "none", true) + ", "
            + Spell(1004, "MAGIC BARRIER", 1, "allied_unit", false) + ", "
            + Spell(PotionCard, "LIFE POTION", 1, "none", false)
            + "]}";

        internal static LoadedCatalog Default()
        {
            FakeClientLog log = new FakeClientLog();
            IPayloadReader body = new NewtonsoftProtocolCodec(MatchFrames.CreateUnion(), log).DecodeObject(Cards).Value;
            return new LoadedCatalog(new CatalogReader(log).Read(body), 1);
        }

        private static string Spell(long cardId, string name, long energy, string targetKind, bool declarationOnly)
        {
            return "{\"card_id\": " + cardId + ", \"card_type\": \"spell\", \"name\": \"" + name + "\", \"energy\": " + energy + ", \"image\": \"s" + cardId
                + "\", \"description\": \"efeito\", \"effect\": {\"requires_target\": " + (targetKind == "none" ? "false" : "true") + ", \"target_kind\": \"" + targetKind
                + "\", \"duration\": \"permanent\", \"declaration_only\": " + (declarationOnly ? "true" : "false") + "}}";
        }
    }
}
