using UnityEngine;
using ZenoniaProto.Net;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// Client entity lifecycle: Spawn / Despawn messages create and destroy the proxy heroes, monsters and drops, and every frame the interpolated
    /// snapshot state (SnapshotBuffer at the render time) is pushed into the proxies. The proxies carry no rules -- they only draw the server's world.
    /// </summary>
    public sealed class ProxySpawner
    {
        readonly SnapshotBuffer buffer;
        readonly Transform root;
        readonly ushort myHeroId;
        readonly NetStats stats;
        readonly System.Collections.Generic.HashSet<ushort> seenPlayers = new System.Collections.Generic.HashSet<ushort>();

        public ProxySpawner(SnapshotBuffer snapshotBuffer, Transform worldRoot, ushort localHeroId, NetStats netStats)
        {
            buffer = snapshotBuffer; root = worldRoot; myHeroId = localHeroId; stats = netStats;
        }

        public void OnSpawn(SpawnMsg m)
        {
            Zone zone = Zone.Main;
            if (zone == null || m.Id == 0) return;
            object existing = zone.Ids.Get(m.Id);
            var at = new Vector2(NetQuantize.Pos(m.X), NetQuantize.Pos(m.Y));
            switch (m.Kind)
            {
                case EntityKind.Hero:
                    HeroController h = existing as HeroController;
                    if (h == null)
                    {
                        HeroClassDef def = m.Type < HeroClassDef.All.Length ? HeroClassDef.All[m.Type] : null;
                        if (def == null) return;
                        h = GameSession.Instance.CreateProxyHero(def, m.Id, m.Id == myHeroId);
                        h.Teleport(at, m.Facing & 3);
                        if (seenPlayers.Add(m.Id)) stats.Max("heroesSeenDistinct", seenPlayers.Count);
                    }
                    h.PlayerId = m.PlayerId;
                    h.PlayerName = m.Name ?? "";
                    if (m.Id != myHeroId && NetMode.RendersFx) h.ProxyLook(m.Look);
                    break;
                case EntityKind.Monster:
                    if (existing != null) return;
                    MonsterDef md = MonsterDef.ByIndex(m.Type);
                    if (md == null) return;
                    Monster mo = Monster.CreateProxy(root, md, at, m.Id);
                    mo.ProxyApply(at, m.Facing, m.State, -1, false, false, NetQuantize.HpPct(m.Hp, m.MaxHp), 0f);
                    break;
                case EntityKind.Loot:
                    LootEntry e;
                    if (existing != null || !NetLootCodec.TryToEntry(m, out e)) return;
                    bool fresh = m.OwnerPlayerId != 0 && NetClient.ServerNowMs < m.FreeAtMs - LootOwnership.PriorityMs + 800f;
                    LootDrop.SpawnProxy(root, e, at, m.Id, m.OwnerPlayerId, m.FreeAtMs, fresh);
                    break;
            }
        }

        public void OnDespawn(DespawnMsg m)
        {
            Zone zone = Zone.Main;
            if (zone == null) return;
            object o = zone.Ids.Get(m.Id);
            buffer.Forget(m.Id);
            if (o == null || m.Id == myHeroId) return;
            HeroController h = o as HeroController;
            if (h != null) { h.Retire(); Object.Destroy(h.gameObject); return; }
            Monster mo = o as Monster;
            if (mo != null) { Object.Destroy(mo.gameObject); return; }
            LootDrop l = o as LootDrop;
            if (l != null) Object.Destroy(l.gameObject);
        }

        /// <summary>Pushes the state at `renderTime` into every proxy that has snapshot data.</summary>
        public void Apply(double renderTime)
        {
            Zone zone = Zone.Main;
            if (zone == null) return;
            System.Collections.Generic.IReadOnlyList<HeroController> heroes = zone.Heroes.All;
            for (int i = 0; i < heroes.Count; i++)
            {
                HeroController h = heroes[i];
                HeroSample s;
                if (!h.IsProxy || !buffer.TryHero(h.NetId, renderTime, out s)) continue;
                h.ProxyApply(new Vector2(s.X, s.Y), NetQuantize.Facing(s.FacingFlags), NetQuantize.Bits(s.FacingFlags), (HeroPose)s.Pose, s.Step, s.Progress, s.HpPct);
            }
            System.Collections.Generic.List<Monster> monsters = zone.Monsters;
            for (int i = 0; i < monsters.Count; i++)
            {
                Monster m = monsters[i];
                MonsterSample s;
                if (!m.IsProxy || !buffer.TryMonster(m.NetId, renderTime, out s)) continue;
                int ai = (s.AttackBits & 7) - 1;
                m.ProxyApply(new Vector2(s.X, s.Y), NetQuantize.Facing(s.FacingState), NetQuantize.Bits(s.FacingState), ai,
                    (s.AttackBits & MonsterBits.Airborne) != 0, (s.AttackBits & MonsterBits.Chilled) != 0, s.HpPct, s.DeathProgress);
            }
        }

        /// <summary>Removes every proxy (disconnect / leaving the online game).</summary>
        public void DestroyAll()
        {
            Zone zone = Zone.Main;
            if (zone == null) return;
            for (int i = zone.Monsters.Count - 1; i >= 0; i--) if (zone.Monsters[i] != null) Object.Destroy(zone.Monsters[i].gameObject);
            for (int i = zone.Loot.Count - 1; i >= 0; i--) if (zone.Loot[i] != null) Object.Destroy(zone.Loot[i].gameObject);
            for (int i = zone.Heroes.All.Count - 1; i >= 0; i--)
            {
                HeroController h = zone.Heroes.All[i];
                if (h != null && h != LocalPlayer.Hero) { h.Retire(); Object.Destroy(h.gameObject); }
            }
        }
    }
}
