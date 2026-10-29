using UnityEngine;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// Owns the playable hero: shows the class select screen first, then spawns the chosen class (hero + renderer + robot companion)
    /// and lets F1..F5 respawn as another class at any time. Each class has its own RPG save (RpgSession): switching saves the current hero first.
    /// </summary>
    public class GameSession : MonoBehaviour
    {
        public static GameSession Instance { get; private set; }
        public static HeroController Hero { get { return Instance != null ? Instance.hero : null; } }
        /// <summary>True while the class select screen is up (no hero yet).</summary>
        public static bool Selecting { get { return Instance != null && Instance.hero == null; } }

        Transform root;
        CameraFollow follow;
        HeroController hero;
        GameObject heroGo;
        RpgSession rpg;

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
            if (heroGo != null) Destroy(heroGo);
            HeroClassDef def = HeroClassDef.Get(id);
            heroGo = new GameObject("Hero_" + def.Name);
            heroGo.transform.SetParent(root, false);
            var view = heroGo.AddComponent<HeroRenderer>();
            view.Init(def, HeldGearData.Load(def));
            ClassPalette.Current = def.ActivePalette;
            if (id == HeroClass.Warrior) EarthBreakUltimate.Preload(); // first-cast hitch fix: load the skill art + warm the FX pools now
            if (id == HeroClass.Wizard) IceStormUltimate.Preload();
            VfxSprites.Preload(id);
            UltimateFx.Prewarm();
            hero = heroGo.AddComponent<HeroController>();
            hero.Init(view, state);
            rpg.Attach(hero, save);
            if (def.CompanionFolder != null) HeroCompanion.Create(root, hero);
            follow.Target = heroGo.transform;
            follow.Configure();
        }

        void Update()
        {
            if (hero == null)
            {
                for (int i = 0; i < HeroClassDef.All.Length; i++) if (InputAdapter.Pressed(i < 3 ? GameKey.Class1 + i : GameKey.Class4 + (i - 3))) { Spawn((HeroClass)i); return; }
                return;
            }
            if (InventoryPanel.IsOpen) return;
            for (int i = 0; i < HeroClassDef.All.Length; i++) if (InputAdapter.Pressed(i < 3 ? GameKey.Switch1 + i : GameKey.Switch4 + (i - 3))) { Spawn((HeroClass)i); return; }
            if (InputAdapter.Pressed(GameKey.SolidDebug)) WorldBuilder.ToggleSolidOverlay();
        }

        void OnGUI()
        {
            if (hero != null) return;
            int picked = ClassSelectGui.Draw();
            if (picked >= 0) Spawn((HeroClass)picked);
        }
    }
}
