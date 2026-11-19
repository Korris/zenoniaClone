using System.Collections.Generic;
using UnityEngine;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// Owns the playable hero: shows the class select screen first, then spawns the chosen class (hero + renderer + pet)
    /// and lets F1..F5 respawn as another class at any time. Each offline hero has its own slot save (RpgSession, SaveStore): switching saves the current hero first.
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

        /// <summary>
        /// Dev class keys, host and capture runs: the class's first offline save (SaveStore), else a new hero of it in the first free slot, else unsaved.
        /// The character select screen goes through EnterSlot / CreateInSlot instead.
        /// </summary>
        public void Spawn(HeroClass id, CharacterAppearanceData newLook = null)
        {
            int slot = SaveStore.SlotOfClass(RpgSession.ClassId(id));
            SpawnInSlot(id, slot >= 0 ? slot : SaveStore.FirstFreeSlot(), newLook, null);
        }

        /// <summary>The offline hero saved in `slot` (ENTER on the character select screen). False when the slot has no readable save.</summary>
        public bool EnterSlot(int slot)
        {
            SaveData save = SaveStore.Load(slot);
            int i = save != null ? ClassIndex(save.classId) : -1;
            if (i < 0 || i >= HeroClassDef.All.Length) return false;
            SpawnInSlot((HeroClass)i, slot, null, null);
            return true;
        }

        /// <summary>A new offline hero of `id` named `name` with the picked look, saved in the empty `slot` (the creation flow).</summary>
        public void CreateInSlot(int slot, HeroClass id, CharacterAppearanceData look, string name) { SpawnInSlot(id, slot, look, name); }

        /// <summary>
        /// Saves + destroys the current hero (if any) and spawns the given class from the save in `slot` (or a new character at the entrance; slot -1 = unsaved).
        /// newLook / newName: picked on the creation screens, used only when the slot has no save yet (a saved hero keeps its own); the new save is written right away (RpgSession.Attach).
        /// </summary>
        void SpawnInSlot(HeroClass id, int slot, CharacterAppearanceData newLook, string newName)
        {
            SaveData save;
            PlayerState state = rpg.Begin(id, slot, newName, out save);
            if (save == null && newLook != null && newLook.stored) state.Appearance = CharacterAppearanceData.Sanitize(newLook, state.ClassId);
            if (hero != null) hero.Retire(); // Destroy is deferred: the old hero must not take loot / aggro for the rest of this frame
            if (heroGo != null) Destroy(heroGo);
            HeroClassDef def = HeroClassDef.Get(id);
            heroGo = new GameObject("Hero_" + def.Name);
            heroGo.transform.SetParent(root, false);
            var view = heroGo.AddComponent<HeroRenderer>();
            view.Init(def, HeldGearData.Load(def)); view.FigureScale = DevStartConfig.HeroScale(); // TEST switch in dev-start.json
            ClassPalette.Current = def.ActivePalette;
            if (id == HeroClass.Warrior) { EarthBreakUltimate.Preload(); BladeCycloneUltimate.Preload(); InfernoCrashUltimate.Preload(); BerserkerAwakeningFx.Preload(); } else if (id == HeroClass.Assassin) NightfallExecutionUltimate.Preload(); // first-cast hitch fix: load the skill art + warm the FX pools now
            if (id == HeroClass.Cleric) ClericBuffFx.Preload();
            if (id == HeroClass.Wizard) IceStormUltimate.Preload();
            if (id == HeroClass.Mechanic) RocketBarrageUltimate.Preload();
            VfxSprites.Preload(id);
            UltimateFx.Prewarm();
            hero = heroGo.AddComponent<HeroController>();
            hero.Init(view, state);
            hero.PlayerName = localName; // host: remote players see this name above the hero
            rpg.Attach(hero, save);
            HeroPet.Create(root, hero);
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
            view.Init(def, HeldGearData.Load(def)); view.FigureScale = DevStartConfig.HeroScale(); // TEST switch in dev-start.json
            if (id == HeroClass.Warrior) { EarthBreakUltimate.Preload(); BladeCycloneUltimate.Preload(); InfernoCrashUltimate.Preload(); BerserkerAwakeningFx.Preload(); } else if (id == HeroClass.Assassin) NightfallExecutionUltimate.Preload();
            if (id == HeroClass.Cleric) ClericBuffFx.Preload();
            if (id == HeroClass.Wizard) IceStormUltimate.Preload();
            if (id == HeroClass.Mechanic) RocketBarrageUltimate.Preload();
            VfxSprites.Preload(id);
            HeroController h = go.AddComponent<HeroController>();
            h.Init(view, null, true);
            h.Teleport(feet, dir);
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
                view.Init(def, HeldGearData.Load(def)); view.FigureScale = DevStartConfig.HeroScale(); // TEST switch in dev-start.json
                if (def.Id == HeroClass.Warrior) { EarthBreakUltimate.Preload(); BladeCycloneUltimate.Preload(); InfernoCrashUltimate.Preload(); BerserkerAwakeningFx.Preload(); } else if (def.Id == HeroClass.Assassin) NightfallExecutionUltimate.Preload();
                if (def.Id == HeroClass.Cleric) ClericBuffFx.Preload();
                if (def.Id == HeroClass.Wizard) IceStormUltimate.Preload();
                if (def.Id == HeroClass.Mechanic) RocketBarrageUltimate.Preload();
                VfxSprites.Preload(def.Id);
            }
            PlayerState state = PlayerState.NewGame(RpgSession.ClassId(def.Id));
            HeroController h = go.AddComponent<HeroController>();
            h.InitProxy(def, view, state, netId, local);
            if (views) HeroPet.Create(root, h);
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

        /// <summary>Class select / in-game switch key per HeroClass index (the GameKey enum grew in steps, so no arithmetic on it).</summary>
        static readonly GameKey[] ClassKeys = { GameKey.Class1, GameKey.Class2, GameKey.Class3, GameKey.Class4, GameKey.Class5, GameKey.Class6 };
        static readonly GameKey[] SwitchKeys = { GameKey.Switch1, GameKey.Switch2, GameKey.Switch3, GameKey.Switch4, GameKey.Switch5, GameKey.Switch6 };

        void Update()
        {
            if (hero == null)
            {
                if (ConnectScreenGui.Visible || HeroSelectScreen.Creating || CharacterListScreen.IsShown || CharacterCustomizeScreen.OfflineOpen || NetMode.IsClient) return; // typing a name / waiting for the server: the digit keys must not pick a class
                for (int i = 0; i < HeroClassDef.All.Length; i++) if (InputAdapter.Pressed(ClassKeys[i])) { Spawn((HeroClass)i); return; }
                return;
            }
            if (InventoryPanel.IsOpen) return;
            if (NetMode.IsOffline)
                for (int i = 0; i < HeroClassDef.All.Length; i++) if (InputAdapter.Pressed(SwitchKeys[i])) { Spawn((HeroClass)i); return; }
            if (InputAdapter.Pressed(GameKey.SolidDebug)) WorldBuilder.ToggleSolidOverlay();
        }

        void OnGUI()
        {
            if (hero != null) { if (AccountScreenGui.MenuOpen) AccountScreenGui.DrawMenu(); return; }
            if (ConnectScreenGui.Visible) ConnectScreenGui.Draw(this); // the hero select itself is HeroSelectScreen
        }
    }
}
