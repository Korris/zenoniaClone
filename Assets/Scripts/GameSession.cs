using System.Collections.Generic;
using UnityEngine;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// Owns the playable hero: shows the class select screen first, then spawns the chosen class (hero + renderer + robot companion)
    /// and lets F1..F5 respawn as another class at any time. Each class has its own RPG save (RpgSession): switching saves the current hero first.
    /// Online (host / client) the class is fixed for the session and there are no saves; GameSession.Online.cs switches the process between modes.
    /// </summary>
    public partial class GameSession : MonoBehaviour
    {
        public static GameSession Instance { get; private set; }
        public static HeroController Hero { get { return LocalPlayer.Hero; } }
        /// <summary>True while the class select screen is up (no hero yet).</summary>
        public static bool Selecting { get { return Instance != null && Instance.hero == null; } }

        Transform root;
        CameraFollow follow;
        HeroController hero;
        GameObject heroGo;
        RpgSession rpg;
        /// <summary>Name shown above the local hero when hosting.</summary>
        string localName = "";

        public static GameSession Create(Transform parent, CameraFollow cameraFollow)
        {
            var s = parent.gameObject.AddComponent<GameSession>();
            s.root = parent;
            s.follow = cameraFollow;
            s.rpg = RpgSession.Create(parent);
            Instance = s;
            return s;
        }

        void OnDestroy() { if (Instance == this) Instance = null; }

        /// <summary>Saves + destroys the current hero (if any) and spawns the given class from its save (or a new character at the entrance).</summary>
        public void Spawn(HeroClass id)
        {
            SaveData save;
            PlayerState state = rpg.Begin(id, out save);
            if (hero != null) hero.Retire(); // Destroy is deferred: the old hero must not take loot / aggro for the rest of this frame
            if (heroGo != null) Destroy(heroGo);
            HeroClassDef def = HeroClassDef.Get(id);
            heroGo = new GameObject("Hero_" + def.Name);
            heroGo.transform.SetParent(root, false);
            var view = heroGo.AddComponent<HeroRenderer>();
            view.Init(def, HeldGearData.Load(def));
            ClassPalette.Current = def.ActivePalette;
            if (id == HeroClass.Warrior) EarthBreakUltimate.Preload(); // first-cast hitch fix: load the skill art + warm the FX pools now
            if (id == HeroClass.Wizard) IceStormUltimate.Preload();
            if (id == HeroClass.Mechanic) RocketBarrageUltimate.Preload();
            VfxSprites.Preload(id);
            UltimateFx.Prewarm();
            hero = heroGo.AddComponent<HeroController>();
            hero.Init(view, state);
            hero.PlayerName = localName; // host: remote players see this name above the hero
            rpg.Attach(hero, save);
            if (def.CompanionFolder != null) HeroCompanion.Create(root, hero);
            follow.Target = heroGo.transform;
            follow.Configure();
        }

        /// <summary>Debug capture: an extra AI-driven hero of another class at `feet` (no save, not followed by the camera or the monsters).</summary>
        public HeroController SpawnPartyMember(HeroClass id, Vector2 feet, int dir)
        {
            HeroClassDef def = HeroClassDef.Get(id);
            var go = new GameObject("Party_" + def.Name);
            go.transform.SetParent(root, false);
            var view = go.AddComponent<HeroRenderer>();
            view.Init(def, HeldGearData.Load(def));
            if (id == HeroClass.Warrior) EarthBreakUltimate.Preload();
            if (id == HeroClass.Wizard) IceStormUltimate.Preload();
            if (id == HeroClass.Mechanic) RocketBarrageUltimate.Preload();
            VfxSprites.Preload(id);
            HeroController h = go.AddComponent<HeroController>();
            h.Init(view, null, true);
            h.Teleport(feet, dir);
            if (def.CompanionFolder != null) HeroCompanion.Create(root, h);
            return h;
        }

        /// <summary>
        /// Online client: a hero the server drives. `local` = the hero this player controls (camera, HUD, inventory); remote ones are just drawn.
        /// The server's Spawn message supplies the id; the mirrored RPG state starts as a new character until the first RpgState push.
        /// </summary>
        public HeroController CreateProxyHero(HeroClassDef def, ushort netId, bool local)
        {
            var go = new GameObject((local ? "Hero_" : "Remote_") + def.Name);
            go.transform.SetParent(root, false);
            bool views = NetMode.RendersFx;
            HeroRenderer view = null;
            if (views)
            {
                view = go.AddComponent<HeroRenderer>();
                view.Init(def, HeldGearData.Load(def));
                if (def.Id == HeroClass.Warrior) EarthBreakUltimate.Preload();
                if (def.Id == HeroClass.Wizard) IceStormUltimate.Preload();
                if (def.Id == HeroClass.Mechanic) RocketBarrageUltimate.Preload();
                VfxSprites.Preload(def.Id);
            }
            PlayerState state = PlayerState.NewGame(RpgSession.ClassId(def.Id));
            HeroController h = go.AddComponent<HeroController>();
            h.InitProxy(def, view, state, netId, local);
            if (views && def.CompanionFolder != null) HeroCompanion.Create(root, h);
            if (!local) return h;
            if (hero != null) { hero.Retire(); Destroy(heroGo); }
            hero = h;
            heroGo = go;
            ClassPalette.Current = def.ActivePalette;
            if (views) UltimateFx.Prewarm();
            rpg.AttachRemote(state, h);
            if (follow != null) { follow.Target = go.transform; follow.Configure(); }
            return h;
        }

        void Update()
        {
            if (hero == null)
            {
                if (ConnectScreenGui.Visible || NetMode.IsClient) return; // typing a name / waiting for the server: the digit keys must not pick a class
                for (int i = 0; i < HeroClassDef.All.Length; i++) if (InputAdapter.Pressed(i < 3 ? GameKey.Class1 + i : GameKey.Class4 + (i - 3))) { Spawn((HeroClass)i); return; }
                return;
            }
            if (InventoryPanel.IsOpen) return;
            if (NetMode.IsOffline)
                for (int i = 0; i < HeroClassDef.All.Length; i++) if (InputAdapter.Pressed(i < 3 ? GameKey.Switch1 + i : GameKey.Switch4 + (i - 3))) { Spawn((HeroClass)i); return; }
            if (InputAdapter.Pressed(GameKey.SolidDebug)) WorldBuilder.ToggleSolidOverlay();
        }

        void OnGUI()
        {
            if (hero != null) return;
            if (ConnectScreenGui.Visible) { ConnectScreenGui.Draw(this); return; }
            if (NetMode.IsClient) return;
            int picked = ClassSelectGui.Draw();
            if (picked >= 0) { Spawn((HeroClass)picked); return; }
            ConnectScreenGui.DrawOpenButton();
        }
    }
}
