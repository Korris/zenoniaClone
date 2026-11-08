using System.Collections.Generic;

namespace ZenoniaProto
{
    /// <summary>
    /// Outfit of a hero, from the worn body armour (designer's rule: the look comes only from the gear): one full body per armour art tier,
    /// each a matching set with the weapons of that tier (T0 = the class's own Lv1 body; T1..T9 = body/set-t1..set-t9).
    /// Each outfit is a repaint of the Lv1 rows with the same poses (Tools/build-warrior-outfit-sheets.py, build-wizard-outfit-idle-down.py),
    /// so gear anchors, fists and head anchors are shared; rows not painted yet are the Lv1 rows.
    /// Warrior: a fixed table, tiers above the last painted outfit wear the last one. Every other class: classes/&lt;class&gt;/body/set-tN is used
    /// when that folder has a bare/idle sheet; tier 0 or a tier without one = the class's Lv1 body.
    /// </summary>
    public static class ArmorLooks
    {
        // Index = armour art tier (ItemInfo.ArtTier = item level / 10); null = the class's own body.
        static readonly string[] WarriorOutfits =
        {
            null, "classes/warrior/body/set-t1", "classes/warrior/body/set-t2", "classes/warrior/body/set-t3", "classes/warrior/body/set-t4",
            "classes/warrior/body/set-t5", "classes/warrior/body/set-t6", "classes/warrior/body/set-t7", "classes/warrior/body/set-t8", "classes/warrior/body/set-t9",
        };
        const int TierCount = 10;

        static readonly Dictionary<HeroClass, string[]> Probed = new Dictionary<HeroClass, string[]>();

        /// <summary>Outfit folders of a class by tier (null = Lv1 body), probed once from Resources for every class but the Warrior.</summary>
        static string[] Of(HeroClassDef cls)
        {
            if (cls == null) return null;
            if (cls.Id == HeroClass.Warrior) return WarriorOutfits;
            string[] o;
            if (Probed.TryGetValue(cls.Id, out o)) return o;
            o = new string[TierCount];
            string bare = cls.BareFolder;
            if (!string.IsNullOrEmpty(bare) && bare.EndsWith("/body/bare"))
            {
                string body = bare.Substring(0, bare.Length - "/bare".Length); // classes/<class>/body
                for (int i = 1; i < TierCount; i++)
                {
                    string folder = body + "/set-t" + i;
                    if (SpriteSheet.TryLoad("Sprites/" + folder + "/bare/idle") != null) o[i] = folder;
                }
            }
            Probed[cls.Id] = o;
            return o;
        }

        /// <summary>Outfit index for a worn armour art tier (-1 = nothing worn): 0 = the class's own body.</summary>
        public static int OutfitIndex(HeroClassDef cls, int armorTier)
        {
            string[] o = Of(cls);
            if (o == null || armorTier <= 0) return 0;
            if (cls.Id == HeroClass.Warrior) return armorTier < o.Length ? armorTier : o.Length - 1;
            return armorTier < o.Length && o[armorTier] != null ? armorTier : 0; // not painted: the Lv1 body
        }

        /// <summary>Resources folder (under Sprites/) holding bare/ and hand/ (+ headless/) of that outfit, null for the class's own body.</summary>
        public static string OutfitFolder(HeroClassDef cls, int index)
        {
            string[] o = Of(cls);
            return o != null && index > 0 && index < o.Length ? o[index] : null;
        }

        /// <summary>Art tier of a worn piece, -1 when the slot is empty.</summary>
        public static int TierOf(Rpg.Item item) { return item != null ? Rpg.ItemInfo.ArtTier(item) : -1; }

        /// <summary>Dresses a paper doll in the outfit of a player's worn body armour.</summary>
        public static void FromWorn(HeroRenderer view, Rpg.PlayerState p) { view.SetOutfit(TierOf(p.GetEquipped(Rpg.Slots.Armor))); }

        /// <summary>Forgets the probed folders (Bootstrap's ResetState pattern; SpriteSheet's caches are reset with it).</summary>
        public static void ResetState() { Probed.Clear(); }
    }
}
