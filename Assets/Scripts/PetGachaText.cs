using System;
using System.Globalization;
using System.Text;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// Texts of the pet gacha shared by its two looks (the old PetGachaWindow and Lumi's counter, LumiCounterWindow): the published table and the
    /// rules under it. Every number comes from the constants the server rolls with (PetRules / PetGacha), so what is shown can never drift from the rules.
    /// </summary>
    public static class PetGachaText
    {
        public const string CrystalSource = "Crystals come from play: Wanted Boss, bosses, levels.";

        /// <summary>"2.5%", "6.25%", "36%".</summary>
        public static string Pct(double v) { return (v * 100.0).ToString(v * 100.0 % 1.0 == 0 ? "0" : "0.###", CultureInfo.InvariantCulture) + "%"; }

        /// <summary>When a pull happened, in the game's clock (UTC+7).</summary>
        public static string When(long atMs) { return DateTimeOffset.FromUnixTimeMilliseconds(atMs).ToOffset(TimeSpan.FromHours(7)).ToString("dd/MM HH:mm", CultureInfo.InvariantCulture); }

        public static string Headline { get { return "<b>Rates per pull</b> (pets of one rarity share it evenly; 1 Gacha Coin a pull, " + PetRules.CrystalsPerPull + " Pet Crystals buy a coin)"; } }

        /// <summary>What the shard result gives.</summary>
        public static string SharedShardNote { get { return PetRules.SharedShardsPerPull + " shared shards (Growth tab turns them into any owned pet's shards)"; } }

        /// <summary>The whole table of the old window's Rates tab (rich text for a dark panel): rates by rarity and pet, the shard result, then Rules.</summary>
        public static string RateTable()
        {
            var b = new StringBuilder();
            b.Append(Headline).Append('\n');
            for (int r = PetGacha.Rates.Length - 1; r >= 0; r--)
            {
                var rarity = (PetRarity)r;
                b.Append("<color=").Append(PetArt.RarityHex(rarity)).Append(">").Append(rarity).Append("  ").Append(Pct(PetGacha.Rates[r])).Append("</color>   ");
                bool first = true;
                foreach (PetDef d in PetConfig.All)
                {
                    if (d.Legacy || d.Rarity != rarity) continue;
                    b.Append(first ? "" : ", ").Append(d.Name).Append(" ").Append(Pct(PetGacha.PetRate(d)));
                    first = false;
                }
                b.Append('\n');
            }
            b.Append("<color=#bfe6ff>Shards  ").Append(Pct(PetRules.GachaShardRate)).Append("</color>   ").Append(SharedShardNote).Append("\n\n");
            return b.Append(Rules()).ToString();
        }

        /// <summary>Guarantees, duplicates, stars, UR shards and the Legacy pets (no colours: reads on a dark panel and on paper).</summary>
        public static string Rules()
        {
            var b = new StringBuilder();
            b.Append("<b>Guarantees</b> (count per account, never expire)\n");
            double ur = PetGacha.Rates[4] / (PetGacha.Rates[3] + PetGacha.Rates[4]);
            b.Append(PetRules.SsrPity).Append("th pull without SSR or better: SSR+ (SSR ").Append(Pct(1 - ur)).Append(", UR ").Append(Pct(ur)).Append("); an SSR+ resets this counter.\n");
            b.Append(PetRules.UrPity).Append("th pull without UR: UR; a UR resets both counters.  x10 = ten single pulls, same rates.\n");
            b.Append("\n<b>Duplicates and stars</b>\n");
            b.Append("A pet you own: +").Append(PetRules.DuplicateShards).Append(" shards of it. Stars: ").Append(PetRules.StarShards[0]).Append(" shards + ").Append(PetRules.StarGold[0]).Append(" G, then ")
             .Append(PetRules.StarShards[1]).Append(" + ").Append(PetRules.StarGold[1]).Append(", ").Append(PetRules.StarShards[2]).Append(" + ").Append(PetRules.StarGold[2]).Append(", ").Append(PetRules.StarShards[3]).Append(" + ").Append(PetRules.StarGold[3])
             .Append(" G. Each star: passive x1.1 more (5 stars = x1.4).\n");
            b.Append("Every pull that is not UR: +1 UR shard; ").Append(PetRules.UrExchangeCost).Append(" UR shards = Frostfang King.\n");
            b.Append("Legacy pets (not in the gacha): Spirit at level 5, Imp for 3000 G at a merchant.");
            return b.ToString();
        }
    }
}
