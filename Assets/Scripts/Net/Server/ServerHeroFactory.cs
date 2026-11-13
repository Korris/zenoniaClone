using UnityEngine;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>Builds the hero of a connected player inside the zone: a headless entity on a dedicated server, a rendered one on a host (the host screen shows everybody).</summary>
    public static class ServerHeroFactory
    {
        const float EntranceJitter = 18f;

        /// <summary>Dev server: a new character on every join, with whatever cheats the server was started with. Never used for an account.</summary>
        public static void CreateDev(NetPlayer p, HeroClassDef def, string name)
        {
            PlayerState state = PlayerState.NewGame(RpgSession.ClassId(def.Id));
            if (NetLaunchArgs.DevLevel > state.Level)
            {
                state.Level = NetLaunchArgs.DevLevel; // test servers only (--dev-level): skill points to spend
                state.RecomputeStats();
                state.Hp = state.MaxHp; state.Mp = state.MaxMp;
            }
            if (NetLaunchArgs.DevNoCooldowns) ZenoniaProto.Rpg.SkillCooldowns.DevNone = true; // test servers only
            if (NetLaunchArgs.DevHurtPct > 0 && def.Id != HeroClass.Cleric) state.Hp = state.MaxHp * NetLaunchArgs.DevHurtPct / 100f; // test servers only: somebody for a cleric to heal
            GrantDevGoods(state);
            Build(p, def, name, state, EntrancePoint());
        }

        /// <summary>A character from the API, already restored and validated, in the current zone: at its saved spot if that spot is free, else at the entrance. Its boss gates come from the save.</summary>
        public static void CreateLoaded(NetPlayer p, HeroClassDef def, string name, PlayerState state, SaveData save)
        {
            Zone zone = Zone.Main;
            bool sameMap = zone.Map != null && save.mapId == zone.Map.Id;
            Vector2 at = save.hasPosition && sameMap && !FrostHollowMap.RectBlocked(save.x - 5f, save.y, 10f, 8f) ? new Vector2(save.x, save.y) : EntrancePoint();
            Build(p, def, name, state, at);
            p.Bosses.KingDefeated = save.bossDefeated;
            foreach (string map in save.defeatedBossMaps) if (!p.Bosses.OwnBossMaps.Contains(map)) p.Bosses.OwnBossMaps.Add(map);
            p.Portals.Disarm(); // saved standing on a portal: it has to step off first
        }

        static void Build(NetPlayer p, HeroClassDef def, string name, PlayerState state, Vector2 at)
        {
            Zone zone = Zone.Main;
            var go = new GameObject("Net_" + name);
            go.transform.SetParent(zone.Root, false);
            HeroController hero = go.AddComponent<HeroController>();
            if (NetMode.IsHeadless) hero.InitHeadless(def, state);
            else
            {
                var view = go.AddComponent<HeroRenderer>();
                view.Init(def, HeldGearData.Load(def));
                hero.Init(view, state, true);
                hero.SyncGearView();
            }
            hero.Owner = new PlayerRpg(state);
            hero.PlayerId = p.PlayerId;
            hero.PlayerName = name;
            hero.AiHeld = p.Input.Held;
            hero.AiPressed = p.Input.Pressed;
            hero.StepGate = p.Input.Advance;
            hero.ExtraStepGate = p.Input.AdvanceExtra;
            hero.InputSeq = () => p.Input.State.LastSeq;
            hero.Teleport(at, 0);
            p.Hero = hero;
            p.Rpg = hero.Owner;
            p.Name = name;
            p.BindRpgEvents();
            p.RpgDirty = true;
        }

        /// <summary>Test servers only (--dev-gold, --dev-items): something to trade. The first item is a +9 weapon of the hero's class, the rest one of each slot in turn at rising rarity.</summary>
        static void GrantDevGoods(PlayerState state)
        {
            state.Gold += NetLaunchArgs.DevGold;
            string[] rarities = ItemDefs.RarityOrder;
            for (int i = 0; i < NetLaunchArgs.DevItems; i++)
            {
                string slot = i == 0 ? Slots.Weapon : Slots.Generated[i % Slots.Generated.Length];
                Item item = ItemFactory.Generate(RpgRandom.Default, 10 + i * 5, slot, rarities[(i + 1) % rarities.Length], state.ClassId);
                if (i == 0) item.enhance = 9;
                state.AddToInventory(item);
            }
        }

        /// <summary>Spawn point near the entrance, spread so arriving players do not stack on one pixel.</summary>
        static Vector2 EntrancePoint()
        {
            for (int i = 0; i < 12; i++)
            {
                Vector2 c = new Vector2(GameConfig.HeroStart.x, GameConfig.HeroStart.y) + Random.insideUnitCircle * EntranceJitter;
                if (!FrostHollowMap.RectBlocked(c.x - 5f, c.y, 10f, 8f)) return c;
            }
            return new Vector2(GameConfig.HeroStart.x, GameConfig.HeroStart.y);
        }

        public static void Destroy(NetPlayer p)
        {
            if (p.Hero == null) return;
            Zone zone = p.Hero.HomeZone;
            if (zone != null && zone.Pump != null)
            {
                Zone.Enter(zone);
                zone.Pump.DestroyOwnedBy(p.Hero);
                ComboProjectile.RetireFor(p.Hero);
            }
            p.Hero.Retire();
            Object.Destroy(p.Hero.gameObject);
            p.Hero = null;
        }
    }
}
