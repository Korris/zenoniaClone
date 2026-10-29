using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Zero-setup entry point: runs after any scene loads (Play on the default empty scene works) and builds the whole
    /// prototype at runtime -- camera, ground, warrior, monsters, HUD.
    /// </summary>
    public static class Bootstrap
    {
        const string RootName = "ZenoniaProto";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Start()
        {
            if (GameObject.Find(RootName) != null) return; // already built (e.g. scene reloaded)

            // Smoothness first: present on every vblank, never throttle, keep running when the Editor loses focus.
            QualitySettings.vSyncCount = 1;
            Application.targetFrameRate = -1;
            Application.runInBackground = true;

            // Static state survives play sessions when Domain Reload is disabled in Editor settings.
            Monster.ResetStatics();
            GameClock.ResetState();
            DamageNumber.ResetState();
            CameraFollow.ResetState();

            var root = new GameObject(RootName);
            root.AddComponent<GameClock>();
            root.AddComponent<DamageNumber>();

            Camera cam = SetupCamera();
            WorldBuilder.Build(root.transform);
            SlashEffect.Init(root.transform);

            HeldGearData gear = HeldGearData.Load(); // null => baked armed frames only (still playable)
            var heroGo = new GameObject("Warrior");
            heroGo.transform.SetParent(root.transform, false);
            var view = heroGo.AddComponent<WarriorRenderer>();
            view.Init(gear);
            var hero = heroGo.AddComponent<WarriorController>();
            hero.Init(view);

            MonsterSpawner.SpawnAll(root.transform);

            var follow = cam.GetComponent<CameraFollow>();
            if (follow == null) follow = cam.gameObject.AddComponent<CameraFollow>();
            follow.Target = heroGo.transform;
            follow.Configure();

            root.AddComponent<Hud>().Init(hero);
        }

        static Camera SetupCamera()
        {
            Camera cam = Camera.main;
            if (cam == null)
            {
                var go = new GameObject("Main Camera");
                go.tag = "MainCamera";
                cam = go.AddComponent<Camera>();
                go.AddComponent<AudioListener>();
            }
            return cam;
        }
    }
}
