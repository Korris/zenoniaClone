using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using ZenoniaProto.Rpg;

namespace ZenoniaProto.EditorTools
{
    /// <summary>
    /// Headless smoke test:  Unity -batchmode -quit -projectPath P -executeMethod ZenoniaProto.EditorTools.FrostSmokeTest.Run
    /// For each class it builds the whole world in edit mode (Bootstrap.Start via reflection), spawns the hero, draws every
    /// animation / direction / frame through HeroRenderer.Show and checks the Frost Hollow map, spawner, art and the RPG wiring
    /// (kill => EXP + ground loot, pickup, equipment => paper doll, inventory pause). Saving is disabled for the run. Logs "SMOKE ..." lines
    /// and exits with code 1 on any failure.
    /// </summary>
    public static class FrostSmokeTest
    {
        static int failures;

        public static void Run()
        {
            failures = 0;
            bool saves = SaveStore.Enabled;
            SaveStore.Enabled = false; // never read or overwrite the developer's own saves
            try
            {
                RpgLogicCheck.Run(Expect);
                ZoneLogicCheck.RunPure(Expect);
                CheckMap();
                CheckTint();
                CheckVfxSets();
                foreach (HeroClassDef cls in HeroClassDef.All) CheckClass(cls);
            }
            catch (Exception e) { Fail("exception " + e); }
            finally { SaveStore.Enabled = saves; GameClock.Paused = false; }
            Debug.Log("SMOKE RESULT: " + (failures == 0 ? "PASS" : "FAIL (" + failures + ")"));
            EditorApplication.Exit(failures == 0 ? 0 : 1);
        }

        static void Fail(string msg) { failures++; Debug.LogError("SMOKE FAIL: " + msg); }
        static void Expect(bool ok, string what) { if (ok) Debug.Log("SMOKE ok: " + what); else Fail(what); }

        /// <summary>SpriteTint must keep alpha and push opaque pixels towards the tint colour (not white).</summary>
        static void CheckTint()
        {
            Texture2D src = SpriteSheet.Load("Sprites/fx/warrior-slashes-chibi");
            Texture2D red = SpriteTint.Colorize(src, ClassPalette.Red.Tint);
            Color32[] a = SpriteSheet.ReadPixels(src), b = SpriteSheet.ReadPixels(red);
            double r = 0, g = 0, bl = 0, n = 0, rs = 0, gs = 0, bs = 0;
            for (int i = 0; i < a.Length; i += 7) if (a[i].a > 200) { n++; r += b[i].r; g += b[i].g; bl += b[i].b; rs += a[i].r; gs += a[i].g; bs += a[i].b; }
            Debug.Log("SMOKE tint opaque px " + n + " src avg " + (rs / n) + "," + (gs / n) + "," + (bs / n) + " -> red avg " + (r / n) + "," + (g / n) + "," + (bl / n));
            Expect(n > 0 && r / n > 1.5 * (g / n), "tint: slash recolours towards red (R " + (r / n) + " vs G " + (g / n) + ")");
        }

        /// <summary>The sliced painted ranged-FX sets must load completely (3 rows x 4 frames each, pivots inside the sprite).</summary>
        static void CheckVfxSets()
        {
            foreach (string id in new[] { "cleric", "wizard", "mechanic-orange", "mechanic-blue" })
            {
                VfxSet set = VfxSprites.Get(id);
                bool ok = set != null && set.Rows.Length == 3;
                for (int r = 0; ok && r < 3; r++)
                    for (int f = 0; ok && f < 4; f++) { Sprite sp = set.Rows[r][f]; ok = sp != null && sp.pivot.x > 0f && sp.pivot.x < sp.rect.width && sp.pivot.y >= 0f && sp.pivot.y < sp.rect.height; }
                Expect(ok, "painted VFX set " + id + " loads 3x4 frames with pivots");
            }
        }

        static void CheckMap()
        {
            Vector2 start = FrostHollowMap.PlayerStart;
            Expect(!FrostHollowMap.RectBlocked(start.x - 5f, start.y, 10f, 8f), "player start walkable " + start);
            Vector2 boss = FrostHollowMap.TileFeet(FrostHollowMap.BossTile.x, FrostHollowMap.BossTile.y, 0f);
            Expect(!FrostHollowMap.RectBlocked(boss.x - 11f, boss.y, 22f, 14f), "boss post walkable " + boss);
            Expect(FrostHollowMap.IsSolidTile(18, 0) && FrostHollowMap.IsSolidTile(0, 0), "river + cliff tiles solid");
            Expect(!FrostHollowMap.IsSolidTile(19, 12) || FrostHollowMap.CharAt(19, 12) == 'B', "bridge row walkable");
            foreach (FrostHollowMap.Zone z in FrostHollowMap.Zones)
            {
                Vector2 spot;
                Expect(FrostHollowMap.TryZoneSpot(z, Vector2.zero, 0f, out spot), "zone spot " + z.Type + " " + spot);
            }
            // the bridge must let the hero cross the river: step down from the path at (17,11) onto the bridge row, then walk east
            Vector2 p = FrostHollowMap.TileFeet(17, 11, 2f);
            bool bx = false, by = false;
            for (int i = 0; i < 14; i++) p = FrostHollowMap.Move(p, 0f, -1f, 10f, 8f, out bx, out by);
            for (int i = 0; i < 100; i++) p = FrostHollowMap.Move(p, 1f, 0f, 10f, 8f, out bx, out by);
            Expect(p.x > 22f * 16f, "hero crosses the bridge (x=" + p.x + ")");
            Vector2 q = FrostHollowMap.TileFeet(17, 12, 2f);
            for (int i = 0; i < 100; i++) q = FrostHollowMap.Move(q, 0f, -1f, 10f, 8f, out bx, out by);
            Expect(by && q.y >= (FrostHollowMap.TilesH - 13) * 16f, "river banks block (y=" + q.y + ")");
        }

        /// <summary>Signature skill: casts (MP / cooldown / root), builds its effect object without exceptions, and the monster status hooks behave.</summary>
        static void CheckSkill(HeroClassDef cls, HeroController hero, GameObject root)
        {
            if (cls.SkillName == null) return;
            Monster near = null;
            foreach (Monster m in Monster.All) if (!m.IsBoss) { near = m; break; }
            if (cls.Id == HeroClass.Assassin) // shadow step needs a target in range and blocks a cast with nothing to strike
            {
                Expect(!hero.TryCastSkill() && hero.Mp >= hero.MaxMp - 0.01f, cls.Name + ": no target => cast refused and free");
                hero.Teleport(near.Center + new Vector2(-30f, -6f), 3);
            }
            if (cls.Id == HeroClass.Cleric) // heal needs missing HP
            {
                Expect(!hero.TryCastSkill(), cls.Name + ": full HP => heal refused");
                hero.TakeDamage(40);
            }
            float mp = hero.Mp, hpBefore = hero.Hp;
            Expect(hero.TryCastSkill(), cls.Name + ": " + cls.SkillName + " cast ok");
            Expect(hero.Mp < mp && hero.SkillCooldownLeftMs > 0f && hero.IsCasting, cls.Name + ": MP spent, cooldown running, hero rooted");
            Expect(!hero.TryCastSkill(), cls.Name + ": second cast blocked by cooldown");
            if (cls.Id == HeroClass.Cleric) Expect(hero.Hp > hpBefore, cls.Name + ": heal restored HP " + hpBefore + " -> " + hero.Hp);
            if (cls.Id == HeroClass.Assassin) { bool any = false; foreach (Monster m in Monster.All) any |= m.Hp < m.MaxHp; Expect(any, cls.Name + ": shadow step damaged its target"); }
            string fx = cls.Id == HeroClass.Warrior ? "EarthBreak" : cls.Id == HeroClass.Wizard ? "IceStorm" : null;
            if (fx != null) Expect(root.transform.Find(fx) != null, cls.Name + ": " + fx + " effect object created");
            Monster slime = null;
            foreach (Monster m in Monster.All) if (!m.IsBoss && !m.IsDying) slime = m; // last one: the shadow step test may have killed the first
            Expect(slime != null && slime.Launch() && slime.IsAirborne, cls.Name + ": regular monster can be launched");
            Expect(Monster.Boss != null && !Monster.Boss.Launch(), cls.Name + ": boss is immune to launch");
        }

        /// <summary>RPG wiring: stats come from the PlayerState, a kill grants EXP and scatters loot, pickups apply, equipment drives the held art.</summary>
        static void CheckRpg(HeroClassDef cls, HeroController hero, GameObject root)
        {
            PlayerState p = hero.Rpg;
            Expect(p != null && p.ClassId == RpgSession.ClassId(cls.Id) && RpgSession.Player == p, cls.Name + ": hero bound to its RPG state");
            Expect(Mathf.Approximately(hero.MaxHp, p.MaxHp) && Mathf.Approximately(hero.AtkStat, p.Atk) && p.GetEquipped(Slots.Weapon) != null, cls.Name + ": stats from PlayerState, starter weapon equipped");

            Monster target = null;
            foreach (Monster m in Monster.All) if (!m.IsBoss && !m.IsDying) target = m;
            int exp = p.Exp, level = p.Level, drops = LootDrop.All.Count;
            target.TakeHit(99999, Vector2.zero, false, true);
            Expect(p.Exp > exp || p.Level > level, cls.Name + ": kill grants EXP (" + exp + " -> " + p.Exp + ")");
            Expect(LootDrop.All.Count > drops, cls.Name + ": kill scatters loot (" + (LootDrop.All.Count - drops) + " drops)");

            int gold = p.Gold;
            Expect(RpgSession.Pickup(new LootEntry { Kind = LootKind.Coin, Value = 7 }).Ok && p.Gold == gold + 7, cls.Name + ": coin pickup adds gold");
            int pots = p.HpPotions;
            Expect(RpgSession.Pickup(new LootEntry { Kind = LootKind.HpPotion, Value = 1 }).Ok && p.HpPotions == pots + 1, cls.Name + ": potion pickup");

            // Paper doll: a tier 9 (+9) weapon of this class shows tier 9 enhanced art; unequipping the offhand hides that hand / companion.
            Item best = ItemFactory.Generate(RpgRandom.Default, 95, Slots.Weapon, "epic", p.ClassId);
            best.enhance = 9;
            p.Inventory.Clear();
            Expect(p.Equip(best).Ok, cls.Name + ": equip a tier 9 weapon");
            RpgSession.SyncGear();
            HeroRenderer view = hero.View;
            int maxTier = view.Gear != null ? view.Gear.TierCount - 1 : 0;
            Expect(!view.DebugLook && view.Tier >= Mathf.Min(9, maxTier) && view.Enhanced, cls.Name + ": weapon art follows the equipped item (tier " + view.Tier + ", enhanced " + view.Enhanced + ")");
            Expect(p.Unequip(Slots.Offhand).Ok, cls.Name + ": unequip offhand");
            RpgSession.SyncGear();
            Expect(view.CompanionEmpty, cls.Name + ": no offhand => companion / offhand hidden");
            view.Show("idle", 0, 0, 0, 1f); // draws with an empty hand without throwing

            InventoryPanel.Open(root.transform);
            Expect(InventoryPanel.IsOpen && GameClock.Paused, cls.Name + ": inventory opens and pauses the game");
            InventoryPanel.Close();
            Expect(!InventoryPanel.IsOpen && !GameClock.Paused, cls.Name + ": inventory closes and resumes");
        }

        static void CheckClass(HeroClassDef cls)
        {
            var start = typeof(Bootstrap).GetMethod("Start", BindingFlags.NonPublic | BindingFlags.Static);
            start.Invoke(null, null);
            GameObject root = GameObject.Find("ZenoniaProto");
            Expect(root != null, cls.Name + ": world built");
            try
            {
                Expect(Monster.All.Count == 19, cls.Name + ": monsters spawned = " + Monster.All.Count + " (expect 18 zone + boss)");
                Expect(Monster.Boss != null, cls.Name + ": boss alive");
                GameSession.Instance.Spawn(cls.Id);
                HeroController hero = GameSession.Hero;
                Expect(hero != null && hero.Class == cls, cls.Name + ": hero spawned");
                HeroRenderer view = hero.View;
                Expect(view.Gear != null, cls.Name + ": gear data loaded, tiers " + (view.Gear != null ? view.Gear.TierCount : 0));
                string[] anims = { "idle", "walk", "attack1", "attack2", "attack3", "dash", "hurt", "death" };
                int shown = 0, gearFrames = 0;
                for (int tier = 0; tier < 10; tier += 9)
                {
                    for (int enh = 0; enh < 2; enh++)
                    {
                        view.SetGear(tier, enh == 1);
                        foreach (string a in anims)
                            for (int d = 0; d < 4; d++)
                            {
                                int n = view.FrameCount(a, d);
                                if (n == 0) { Fail(cls.Name + ": no frames " + a + "/" + d); continue; }
                                for (int f = 0; f < n; f++) { view.Show(a, d, f, 0, 1f); shown++; if (view.transform.Find(cls.Parts[0].Id).GetComponent<SpriteRenderer>().sprite != null) gearFrames++; }
                            }
                    }
                }
                Expect(shown > 0 && gearFrames > 0, cls.Name + ": drew " + shown + " frames, " + gearFrames + " with weapon overlay");
                CheckSkill(cls, hero, root);
                CheckRpg(cls, hero, root);
                if (cls.Id == HeroClass.Warrior) ZoneLogicCheck.RunWorld(Expect, hero, root);
                if (cls.CompanionFolder != null) Expect(root.transform.Find("Companion") != null, cls.Name + ": companion created");
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }
    }
}
