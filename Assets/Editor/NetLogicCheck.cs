using System;
using UnityEngine;
using ZenoniaProto.Net;
using ZenoniaProto.Rpg;

namespace ZenoniaProto.EditorTools
{
    /// <summary>
    /// Network layer checks (called from FrostSmokeTest): id registry quarantine, spawn / snapshot encoding of live entities and the proxy that
    /// reads them back, the loot wire codec, event emission from the real hit path, and the dev login rules.
    /// </summary>
    public static class NetLogicCheck
    {
        public static void RunPure(Action<bool, string> expect)
        {
            var ids = new NetEntityRegistry();
            object a = new object(), b = new object();
            ushort ia = ids.Alloc(a), ib = ids.Alloc(b);
            expect(ia != 0 && ib != 0 && ia != ib && ids.Get(ia) == a, "net: registry hands out distinct non-zero ids");
            ids.Release(ia, 1000f);
            ushort ic = ids.Alloc(new object());
            expect(ids.Get(ia) == null && ic != ia, "net: a released id is not reused right away");
            ids.Tick(1000f + NetEntityRegistry.QuarantineMs + 1f);
            expect(ids.Count == 2, "net: registry keeps live ids only (" + ids.Count + ")");

            expect(DevLoginStub.SanitizeName("  <b>Tay</b> Gân!!  ") == "bTayb Gn" && DevLoginStub.SanitizeName(new string('a', 40)).Length == 16, "net: names keep letters / digits / space only and are cut to 16");
            expect(DevLoginStub.SanitizeName("") == "Hero" && DevLoginStub.SanitizeName("\u0001\u0002") == "Hero", "net: empty / control-character names become 'Hero'");
            expect(DevLoginStub.KeyOk("k", "k") && !DevLoginStub.KeyOk("k", "x") && !DevLoginStub.KeyOk("k", null) && DevLoginStub.KeyOk("", "anything"), "net: dev key compare (empty = open server)");
            HeroClassDef def;
            expect(DevLoginStub.TryClass(0, out def) && !DevLoginStub.TryClass(99, out def), "net: class byte range check");

            var drop = new LootEntry { Kind = LootKind.Equipment, Item = new Item { slot = Slots.Weapon, classId = "wizard", itemLevel = 35, enhance = 9, rarity = "rare", uid = "x" } };
            var msg = new SpawnMsg();
            NetLootCodec.Fill(ref msg, drop);
            LootEntry back;
            bool ok = NetLootCodec.TryToEntry(msg, out back);
            expect(ok && back.Kind == LootKind.Equipment && back.Item.slot == Slots.Weapon && back.Item.classId == "wizard" && back.Item.itemLevel == 35 && back.Item.enhance == 9 && back.Item.rarity == "rare",
                "net: equipment drop survives the wire codec (slot / class / level / enhance / rarity)");
            var coin = new SpawnMsg();
            NetLootCodec.Fill(ref coin, new LootEntry { Kind = LootKind.Coin, Value = 25 });
            expect(NetLootCodec.TryToEntry(coin, out back) && back.Kind == LootKind.Coin && back.Value == 25, "net: coin drop survives the wire codec");
        }

        public static void RunWorld(Action<bool, string> expect, HeroController hero, GameObject root)
        {
            Zone zone = Zone.Main;
            RunMode mode = NetMode.Current;
            bool active = NetEventBus.Active;
            try
            {
                HeroSnap hs = hero.ToSnap();
                expect(hs.Id == hero.NetId && Mathf.Abs(NetQuantize.Pos(hs.X) - hero.Position.x) < 0.2f && Mathf.Abs(NetQuantize.Pos(hs.Y) - hero.Position.y) < 0.2f && hs.HpPct == 100, "net: hero snapshot entry carries id, position and hp percent");

                Monster m = null;
                foreach (Monster c in zone.Monsters) if (c.Type == "frostSlime" && !c.IsDying) { m = c; break; }
                MonsterSnap ms = m.ToSnap();
                SpawnMsg spawn;
                expect(ServerReplication.TryBuildSpawn(m, m.NetId, out spawn) && spawn.Kind == EntityKind.Monster && MonsterDef.ByIndex(spawn.Type) != null && spawn.MaxHp == 36, "net: monster spawn message names its definition and hp");
                Monster proxy = Monster.CreateProxy(root.transform, MonsterDef.ByIndex(spawn.Type), new Vector2(1f, 1f), 40000);
                proxy.ProxyApply(new Vector2(NetQuantize.Pos(ms.X), NetQuantize.Pos(ms.Y)), NetQuantize.Facing(ms.FacingState), NetQuantize.Bits(ms.FacingState), (ms.AttackBits & 7) - 1, false, false, ms.HpPct, 0f);
                expect(proxy.IsProxy && Vector2.Distance(proxy.Feet, m.Feet) < 0.2f && Mathf.Abs(proxy.Hp - m.Hp) < 1f && zone.Ids.Get(40000) == proxy, "net: a client proxy reads the monster snapshot back (position, hp, id registered)");
                proxy.ProxyHit(5, 20, Vector2.zero, false, false, true);
                expect(Mathf.Approximately(proxy.Hp, 20f), "net: a MonsterHit event sets the exact hp on the proxy");
                int before = zone.Monsters.Count;
                proxy.TakeHit(999, Vector2.zero, false, false, true, hero);
                expect(!proxy.IsDying && zone.Monsters.Count == before, "net: a proxy ignores local hits (the server decides)");
                UnityEngine.Object.DestroyImmediate(proxy.gameObject);

                NetEventBus.Active = true;
                NetEventBus.Pending.Clear();
                m.TakeHit(4, Vector2.right * 20f, true, false, true, hero);
                bool sawHit = false;
                foreach (NetEventBus.Queued q in NetEventBus.Pending)
                    if (q.Ev.Type == NetEventType.MonsterHit && q.Ev.Id == m.NetId && q.Ev.Other == hero.NetId && q.Ev.Dmg == 4 && (q.Ev.Flags & HitFlags.Crit) != 0 && q.Ev.HpLeft == Mathf.CeilToInt(m.Hp)) sawHit = true;
                expect(sawHit, "net: the real hit path emits a MonsterHit event (id, source hero, damage, crit, hp left)");
                NetEventBus.Pending.Clear();
                NetEventBus.Active = false;
                m.TakeHit(1, Vector2.zero, false, false, true, hero);
                expect(NetEventBus.Pending.Count == 0, "net: no events are collected when no server runs");

                var look = NetEventBus.AppearanceOf(hero);
                Item w, o;
                NetEventBus.ItemsFromAppearance(look, RpgSession.ClassId(hero.Class.Id), out w, out o);
                expect((w == null) == (look.WeaponTier == 255) && (w == null || ItemInfo.ArtTier(w) == look.WeaponTier) && (o == null) == (look.OffTier == 255), "net: appearance round-trips the equipped gear tiers");
            }
            finally { NetEventBus.Active = active; NetEventBus.Pending.Clear(); NetMode.Current = mode; }
        }
    }
}
