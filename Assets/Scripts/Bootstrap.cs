using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Zero-setup entry point: runs after any scene loads (Play on the default empty scene works) and builds the whole
    /// prototype at runtime -- camera, Frost Hollow ground, monsters, HUD and the class select that spawns the hero.
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
            GearOverlays.ResetState();
            InputAdapter.BotHeld = null;
            InputAdapter.BotPressed = null;
            FeelConfig.BloomOn = true;
            FeelConfig.FxOn = true;

            var root = new GameObject(RootName);
            root.AddComponent<GameClock>();
            root.AddComponent<DamageNumber>();

            Camera cam = SetupCamera();
            WorldBuilder.Build(root.transform);
            SlashEffect.Init(root.transform);
            SwordTrail.Init(root.transform);
            ImpactFx.Init(root.transform);
            BurstFx.Init(root.transform);
            ComboProjectile.Init(root.transform);
            ShockRing.Init(root.transform);
            UltimateFx.Init(root.transform);
            ScreenFlash.ResetState();

            var follow = cam.GetComponent<CameraFollow>();
            if (follow == null) follow = cam.gameObject.AddComponent<CameraFollow>();
            follow.Target = null;
            follow.Configure();
            // Frame the entrance behind the class select screen until a hero exists.
            cam.transform.position = new Vector3(GameConfig.HeroStart.x + 40f, GameConfig.HeroStart.y + 30f, -10f);

            MonsterSpawner.SpawnAll(root.transform);
            GameSession session = GameSession.Create(root.transform, follow);

            // Bloom (Built-in pipeline OnRenderImage) + A/B hotkeys; the camera is switched to HDR by BloomEffect.
            var bloom = cam.GetComponent<BloomEffect>();
            if (bloom == null) bloom = cam.gameObject.AddComponent<BloomEffect>();
            root.AddComponent<FeelHotkeys>().Init(bloom);

            root.AddComponent<Hud>();
            DebugCapture.AttachIfRequested(root, session);
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
