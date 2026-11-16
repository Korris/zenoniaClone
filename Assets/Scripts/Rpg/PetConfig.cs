namespace ZenoniaProto.Rpg
{
    /// <summary>The automatic skill of a pet. Byte values travel in NetEvent PetSkill.Step (bit 7 set = the 3 s basic attack): the client picks the FX from it. Append only.</summary>
    public enum PetSkillKind : byte { None = 0, Shield = 1, Fire = 2, Ice = 3, Poison = 4, Lightning = 5, Heal = 6, Strike = 7 }

    /// <summary>What a pet's passive raises (one number, by stars).</summary>
    public enum PetPassive : byte { MaxHp, Damage, MoveSpeed, DamageTaken, PotionHeal, AttackSpeed }

    /// <summary>Pet gacha rarity (ChatGPT: N / R / SR / SSR / UR like Ragnarok). Each kind has one fixed rarity. Legacy pets (PetDef.Legacy) have none and are not in the gacha.</summary>
    public enum PetRarity : byte { N = 0, R = 1, SR = 2, SSR = 3, UR = 4 }

    /// <summary>
    /// One Pet Companion ("Thú Đồng Hành"). Its passive and skill come from this table by id and the account's stars, never from saved numbers.
    /// PassivePct is the 1-star value; each star adds PassiveStep. SkillPct = share of one plain hit (attack skills) or of max HP (Shield / Heal).
    /// Price 0 = not sold; UnlockLevel 0 = not unlocked by level. Art = Sprites/pets/&lt;ArtId&gt;/art.png tinted by Tint (0xRRGGBB, 0xFFFFFF = as drawn), drawn HeightUnits tall.
    /// Melee = it lunges at its target (basic attack and attack skill); otherwise it shoots BasicFx's projectile. Flies = hovers beside the owner instead of walking.
    /// </summary>
    public sealed class PetDef
    {
        public string Id, TemplateId, Name, SkillName, ArtId, MonsterType;
        public PetRarity Rarity;
        public PetPassive Passive;
        public double PassivePct, SkillPct;
        public PetSkillKind Skill, BasicFx = PetSkillKind.Strike;
        public int Price, UnlockLevel;
        public uint Tint = 0xFFFFFF;
        public float HeightUnits = 11f;
        public bool Legacy, Melee, Flies;
        /// <summary>The art is a stand-in (another pet's picture tinted) until this pet's own picture is drawn.</summary>
        public bool PlaceholderArt;
    }

    /// <summary>
    /// Pet Companion + pet gacha V1 (plans/261116-0040-pet-companion-and-fashion-gacha; ChatGPT 16/11/2026, docs/reference/gacha-discussion-chatgpt.md Lần 6 + Lần 7).
    /// Pets are the game's own monsters and bosses, shrunk (Lần 7): 8 in the gacha (N Frost Slime, Horn Rabbit; R Ice Goblin, Thorn Boar; SR Spectral Wolf, Marsh Stalker;
    /// SSR Crystal Golem; UR the boss Frostfang King) + the two old pets Spirit and Imp as Legacy (outside the gacha: Spirit when any hero reaches level 5, Imp 3000 gold).
    /// Owned by the ACCOUNT for good (AccountState.pets: no item, no durability, no trade); a hero fields a team of five (PlayerState.Team, PetTeam). Passive x 100-140 % by stars (PetRules).
    /// Every pet fights by itself: a basic attack every 3 s for 10 % of a plain hit, and its skill every 20 s, only on the monster its owner hit in the last 2 s within
    /// 6 tiles (Shield: after the owner lost real HP and is at 70 % or less) (HeroController.Pet.cs). Pet hits use the owner's Atk: no combo, crit, pet bonus or on-hit,
    /// and never count toward the damage share. Levels (Pet Food) and stars (shards + gold) are PetGrowth's; the full V2 spec is docs/pet-system-design.md.
    /// Frostfang King can also be had for 120 UR shards. Old durability pets (items "pet-spirit" / "pet-imp") become 1-star account pets (AccountPets.Migrate).
    /// The ORDER is the wire id (NetAppearance.PetId = index + 1) and must match PET_IDS in server-api/src/rpg/rpg-constants.js: append, never reorder.
    /// </summary>
    public static class PetConfig
    {
        public static readonly PetDef[] All =
        {
            new PetDef { Id = "spirit", TemplateId = "pet-spirit", Name = "Spirit", ArtId = "spirit", Legacy = true, Flies = true, BasicFx = PetSkillKind.Lightning,
                Passive = PetPassive.MaxHp, PassivePct = 0.05, Skill = PetSkillKind.Shield, SkillPct = 0.05, SkillName = "Spirit Shield", UnlockLevel = 5 },
            new PetDef { Id = "imp", TemplateId = "pet-imp", Name = "Imp", ArtId = "imp", Legacy = true, Flies = true, BasicFx = PetSkillKind.Fire,
                Passive = PetPassive.Damage, PassivePct = 0.03, Skill = PetSkillKind.Fire, SkillPct = 0.40, SkillName = "Fireball", Price = 3000 },
            new PetDef { Id = "frostSlime", Name = "Frost Slime", ArtId = "frostSlime", MonsterType = "frostSlime", Rarity = PetRarity.N, Melee = true,
                Passive = PetPassive.MaxHp, PassivePct = 0.05, Skill = PetSkillKind.Shield, SkillPct = 0.05, SkillName = "Crystal Shell" },
            new PetDef { Id = "hornRabbit", Name = "Horn Rabbit", ArtId = "hornRabbit", MonsterType = "hornRabbit", Rarity = PetRarity.N, Melee = true,
                Passive = PetPassive.MoveSpeed, PassivePct = 0.03, Skill = PetSkillKind.Strike, SkillPct = 0.40, SkillName = "Horn Charge" },
            new PetDef { Id = "iceGoblin", Name = "Ice Goblin", ArtId = "iceGoblin", MonsterType = "iceGoblin", Rarity = PetRarity.R, BasicFx = PetSkillKind.Ice,
                Passive = PetPassive.Damage, PassivePct = 0.03, Skill = PetSkillKind.Ice, SkillPct = 0.45, SkillName = "Ice Toss" },
            new PetDef { Id = "thornBoar", Name = "Thorn Boar", ArtId = "thornBoar", MonsterType = "thornBoar", Rarity = PetRarity.R, Melee = true,
                Passive = PetPassive.DamageTaken, PassivePct = 0.03, Skill = PetSkillKind.Strike, SkillPct = 0.45, SkillName = "Thorn Charge" },
            new PetDef { Id = "spectralWolf", Name = "Spectral Wolf", ArtId = "spectralWolf", MonsterType = "spectralWolf", Rarity = PetRarity.SR, Melee = true,
                Passive = PetPassive.AttackSpeed, PassivePct = 0.03, Skill = PetSkillKind.Strike, SkillPct = 0.55, SkillName = "Spectral Bite" },
            new PetDef { Id = "marshStalker", Name = "Marsh Stalker", ArtId = "marshStalker", MonsterType = "marshStalker", Rarity = PetRarity.SR, BasicFx = PetSkillKind.Poison,
                Passive = PetPassive.PotionHeal, PassivePct = 0.05, Skill = PetSkillKind.Poison, SkillPct = 0.55, SkillName = "Venom Spit" },
            new PetDef { Id = "crystalGolem", Name = "Crystal Golem", ArtId = "crystalGolem", MonsterType = "crystalGolem", Rarity = PetRarity.SSR, Melee = true, HeightUnits = 13f,
                Passive = PetPassive.DamageTaken, PassivePct = 0.04, Skill = PetSkillKind.Shield, SkillPct = 0.07, SkillName = "Crystal Bulwark" },
            new PetDef { Id = "frostfangKing", Name = "Frostfang King", ArtId = "frostfangKing", MonsterType = "frostfangKing", Rarity = PetRarity.UR, Melee = true, HeightUnits = 14f,
                Passive = PetPassive.Damage, PassivePct = 0.05, Skill = PetSkillKind.Ice, SkillPct = 0.75, SkillName = "Frost Cleave" },
        };

        /// <summary>The basic attack (ChatGPT Lần 8: every 2 s, 10 % of one plain hit), same target rules as the skill.</summary>
        public const int BasicAttackMs = 1500; // ChatGPT 16/11 06:41: first hit at once, then every 1.5 s (was 2 s)
        /// <summary>--pet-log: every attack cycle and pet state change goes to the log (capture runs).</summary>
        public static bool LogCycles;
        public const double BasicPct = 0.25; // of the owner's hit AFTER defence (was 10 % before defence: 1-2 damage, invisible)
        /// <summary>One attack cycle (Lần 8): the pet runs to the target at 160 % of its owner's speed, its Attack animation (4 frames, 10 FPS) lands the hit on frame 3 (300 ms), then it recovers.</summary>
        public const float ChaseSpeedMul = 1.6f, MeleeRangeUnits = 1.2f * 16f, RangedRangeUnits = 4f * 16f;
        public const int HitFrameMs = 300, AttackRecoverMs = 100, AttackAnimMs = 400;
        /// <summary>Following (Lần 8): rests 1.5 tiles behind the owner, 0.6 tiles to the side; starts walking past 2 tiles, stops within 0.25; 110 % of the owner's speed, 160 % past 3 tiles, teleports past 7.</summary>
        public const float FollowBehindUnits = 1.5f * 16f, FollowSideUnits = 0.6f * 16f, FollowStartUnits = 2f * 16f, FollowStopUnits = 0.25f * 16f, FollowSpeedMul = 1.1f, FollowFarUnits = 3f * 16f, TeleportUnits = 7f * 16f;

        /// <summary>Old names kept for call sites: the growth tables live in PetRules (stars 1-5, 100-140 %).</summary>
        public const int MaxStars = PetRules.MaxStars;
        public const int SkillCooldownMs = 20000;
        public const double OwnerHpLine = 0.70;
        public const int ShieldMs = 5000, FlightMs = 320, TargetMemoryMs = 4000; // FlightMs: a ranged skill's projectile flight; TargetMemoryMs: the mark lasts 4 s after the owner's last hit (Lần 8)
        public const float RangeUnits = 6 * 16f; // 6 tiles of 16 units, owner to target when it fires
        /// <summary>Without a mark from the owner the pets pick the nearest monster (not a boss) within 4 tiles of the owner themselves (HeroController.PickPetTarget).</summary>
        public const float AutoTargetUnits = 4 * 16f;
        /// <summary>Ice skills (Ice Goblin, Frostfang King): monsters slowed by 20 % for 2 s (bosses are immune).</summary>
        public const float IceSlowPct = 20f;
        public const int IceSlowMs = 2000;
        /// <summary>Paid once per kind to an account that owned an old durability pet (its bonus went down): shards of that kind + gold.</summary>
        public const int CompensationShards = 10, CompensationGold = 3000;

        public static PetDef Get(string id) { if (string.IsNullOrEmpty(id)) return null; foreach (PetDef d in All) if (d.Id == id) return d; return null; }
        public static PetDef ByTemplate(string templateId) { foreach (PetDef d in All) if (d.TemplateId != null && d.TemplateId == templateId) return d; return null; }
        public static PetDef ByIndex(int i) { return i >= 0 && i < All.Length ? All[i] : null; }
        public static int IndexOf(PetDef d) { return System.Array.IndexOf(All, d); }

        /// <summary>The pet an OLD item is (legacy durability pets, only met while migrating a save); null for anything else.</summary>
        public static PetDef Of(Item item) { return item != null && item.slot == Slots.Pet ? ByTemplate(item.templateId) : null; }

        /// <summary>The passive at these stars (0 for no pet or 0 stars).</summary>
        public static double PassiveAt(PetDef d, int stars) { return d != null && stars >= 1 ? d.PassivePct * PetRules.StarFactor(stars) : 0.0; }

        /// <summary>The passive of this kind for a worn pet at these stars, 0 when the pet raises something else.</summary>
        public static double Passive(PetDef d, int stars, PetPassive kind) { return d != null && d.Passive == kind ? PassiveAt(d, stars) : 0.0; }

        /// <summary>"+6% Max HP" / "-4% damage taken from monsters" at these stars.</summary>
        public static string PassiveText(PetDef d, int stars)
        {
            if (d == null) return "";
            string p = (PassiveAt(d, System.Math.Max(1, stars)) * 100).ToString("0.#", System.Globalization.CultureInfo.InvariantCulture);
            return PassiveName(d.Passive, p);
        }

        /// <summary>"+6% Max HP" from a kind and a formatted number.</summary>
        public static string PassiveName(PetPassive kind, string p)
        {
            switch (kind)
            {
                case PetPassive.MaxHp: return "+" + p + "% Max HP";
                case PetPassive.Damage: return "+" + p + "% damage to monsters";
                case PetPassive.MoveSpeed: return "+" + p + "% move speed";
                case PetPassive.DamageTaken: return "-" + p + "% damage taken from monsters";
                case PetPassive.PotionHeal: return "+" + p + "% HP potion healing";
                default: return "+" + p + "% attack speed";
            }
        }

        /// <summary>"Fireball: 40% of a plain hit on the monster you hit (6 tiles)". Same at every star.</summary>
        public static string SkillText(PetDef d)
        {
            if (d == null) return "";
            int p = RpgMath.Round(d.SkillPct * 100);
            switch (d.Skill)
            {
                case PetSkillKind.Shield: return d.SkillName + ": at 70% HP or less after a hit, a shield of " + p + "% max HP for 5 s";
                case PetSkillKind.Heal: return d.SkillName + ": at 70% HP or less after a hit, heals " + p + "% max HP";
                case PetSkillKind.Ice: return d.SkillName + ": " + p + "% of a plain hit, slows 20% for 2 s";
                case PetSkillKind.Strike: return d.SkillName + ": rushes the monster you hit for " + p + "% of a plain hit";
                default: return d.SkillName + ": " + p + "% of a plain hit on the monster you hit";
            }
        }

        public static bool IsAttack(PetSkillKind k) { return k == PetSkillKind.Fire || k == PetSkillKind.Ice || k == PetSkillKind.Poison || k == PetSkillKind.Lightning || k == PetSkillKind.Strike; }

        /// <summary>"N" ... "UR", or "Legacy" for the old pets outside the gacha.</summary>
        public static string RarityLabel(PetDef d) { return d == null ? "" : d.Legacy ? "Legacy" : d.Rarity.ToString(); }

        /// <summary>NetAppearance.PetId: 0 = none, otherwise index + 1.</summary>
        public static byte WireId(string petId) { PetDef d = Get(petId); return d != null ? (byte)(IndexOf(d) + 1) : (byte)0; }

        /// <summary>The pet id of a NetAppearance.PetId, null for none or an id this build does not know.</summary>
        public static string IdOfWire(byte wire) { PetDef d = ByIndex(wire - 1); return d != null ? d.Id : null; }

        /// <summary>Who sells pets: the general merchants, not the healer.</summary>
        public static bool SoldBy(string npcId) { return npcId == "merchantDale" || npcId == "apothecaryNara"; }

        /// <summary>Null when this hero may buy from this seller: it stocks pets, works on the hero's map and stands in reach. Gold and ownership are AccountPets.Buy's check.</summary>
        public static string Refusal(string npcId, string mapId, int petIndex, float distance)
        {
            if (!SoldBy(npcId) || PotionShops.MapOf(npcId) != mapId) return "Nobody sells that here";
            PetDef d = ByIndex(petIndex);
            if (d == null || d.Price <= 0) return "Not sold here";
            if (distance > PotionShops.ReachUnits) return "Too far from the shop";
            return null;
        }
    }
}
