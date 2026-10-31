using UnityEngine;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>Builds the hero of a connected player inside the zone: a headless entity on a dedicated server, a rendered one on a host (the host screen shows everybody).</summary>
    public static class ServerHeroFactory
    {
        const float EntranceJitter = 18f;

        public static void Create(NetPlayer p, HeroClassDef def, string name)
        {
            Zone zone = Zone.Main;
            var go = new GameObject("Net_" + name);
            go.transform.SetParent(zone.Root, false);
            HeroController hero = go.AddComponent<HeroController>();
            PlayerState state = PlayerState.NewGame(RpgSession.ClassId(def.Id));
            if (NetLaunchArgs.DevLevel > state.Level)
            {
                state.Level = NetLaunchArgs.DevLevel; // test servers only (--dev-level): skill points to spend
                state.RecomputeStats();
                state.Hp = state.MaxHp; state.Mp = state.MaxMp;
            }
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
            hero.Teleport(EntrancePoint(), 0);
            p.Hero = hero;
            p.Rpg = hero.Owner;
            p.Name = name;
            p.BindRpgEvents();
            p.RpgDirty = true;
            // Same as an offline save positioned past a gate: a hero that begins beyond it (--map) can walk back through it.
            string passed = zone.Map != null ? MapSpawnTable.GatePassedOn(zone.Map.Id) : null;
            if (passed != null) p.Bosses.Mark(passed, MapSpawnTable.HasOwnBoss(passed));
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
