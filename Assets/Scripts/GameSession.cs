using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Owns the playable hero: shows the class select screen first, then spawns the chosen class (hero + renderer + robot companion)
    /// and lets F1 / F2 / F3 respawn as another class at any time.
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

        public static GameSession Create(Transform parent, CameraFollow cameraFollow)
        {
            var s = parent.gameObject.AddComponent<GameSession>();
            s.root = parent;
            s.follow = cameraFollow;
            Instance = s;
            return s;
        }

        void OnDestroy() { if (Instance == this) Instance = null; }

        /// <summary>Destroys the current hero (if any) and starts a fresh one of the given class at the entrance.</summary>
        public void Spawn(HeroClass id)
        {
            if (heroGo != null) Destroy(heroGo);
            HeroClassDef def = HeroClassDef.Get(id);
            heroGo = new GameObject("Hero_" + def.Name);
            heroGo.transform.SetParent(root, false);
            var view = heroGo.AddComponent<HeroRenderer>();
            view.Init(def, HeldGearData.Load(def));
            hero = heroGo.AddComponent<HeroController>();
            hero.Init(view);
            if (def.HasRobot) RobotCompanion.Create(root, hero);
            follow.Target = heroGo.transform;
            follow.Configure();
        }

        void Update()
        {
            if (hero == null)
            {
                for (int i = 0; i < 3; i++) if (InputAdapter.Pressed(GameKey.Class1 + i)) { Spawn((HeroClass)i); return; }
                return;
            }
            for (int i = 0; i < 3; i++) if (InputAdapter.Pressed(GameKey.Switch1 + i)) { Spawn((HeroClass)i); return; }
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
