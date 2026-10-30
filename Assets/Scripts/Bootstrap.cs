using System;
using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Zero-setup entry point: runs after any scene loads (Play on the default empty scene works) and builds the whole
    /// prototype at runtime -- camera, Frost Hollow ground, monsters, HUD (IMGUI + UGUI RPG block) and the class select that spawns the hero.
    /// </summary>
    public static class Bootstrap
    {
        const string RootName = "ZenoniaProto";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Start()
        {
            if (GameObject.Find(RootName) != null) return; // already built (e.g. scene reloaded)

            string[] cmd = Environment.GetCommandLineArgs();
            NetMode.Current = NetMode.ParseMode(cmd);
            NetLaunchArgs.Parse(cmd);
            bool headless = NetMode.IsHeadless;
            bool paced = headless || (Application.isBatchMode && !NetMode.IsOffline); // dedicated server and unattended bot clients run at 30 fps, no vsync
            if (!NetMode.IsOffline) ZenoniaProto.Rpg.SaveStore.Enabled = false; // online play never touches the offline saves

            // Smoothness first: present on every vblank, never throttle, keep running when the Editor loses focus.
            // A dedicated server has no display: it paces itself at the 30 Hz sim tick instead.
            QualitySettings.vSyncCount = paced ? 0 : 1;
            Application.targetFrameRate = paced ? NetLaunchArgs.TickRate : Application.isMobilePlatform ? 60 : -1; // phones ignore vSyncCount and default to 30 fps
            if (Application.isMobilePlatform) Screen.sleepTimeout = SleepTimeout.NeverSleep;
            Application.runInBackground = true;

            // Static state survives play sessions when Domain Reload is disabled in Editor settings.
            Monster.ResetStatics();
            LocalPlayer.ResetState();
            GameClock.ResetState();
            DamageNumber.ResetState();
            CameraFollow.ResetState();
            GearOverlays.ResetState();
            HeldGlowBaker.ResetState();
            LootDrop.ResetStatics();
            NetEventBus.ResetState();
            LootSprites.ResetState();
            BagSprites.ResetState();
            UiKit.ResetState();
            ClassSelectGui.ResetState();
            InputAdapter.BotHeld = null;
            InputAdapter.BotPressed = null;
            FeelConfig.BloomOn = true;
            FeelConfig.FxOn = true;

            var root = new GameObject(RootName);
            Zone.CreateMain(root.transform);
            root.AddComponent<GameClock>();
            if (NetMode.NoViews) { StartWithoutViews(root); return; }
            root.AddComponent<DamageNumber>();

            Camera cam = SetupCamera();
            WorldBuilder.Build(root.transform);
            SlashEffect.Init(root.transform);
            SwordTrail.Init(root.transform);
            ImpactFx.Init(root.transform);
            BurstFx.Init(root.transform);
            ComboProjectile.Init(root.transform);
            VfxAnim.Init(root.transform);
            ShockRing.Init(root.transform);
            DaggerFx.Init(root.transform);
            RuneFx.Init(root.transform);
            HolySmite.Init(root.transform);
            UltimateFx.Init(root.transform);
            GameSfx.Init(root.transform);
            GameMusic.Init(root.transform);
            GameMusic.Play("frost-hollow"); // the only map so far; oakhaven-village waits for the village map
            ScreenFlash.ResetState();

            var follow = cam.GetComponent<CameraFollow>();
            if (follow == null) follow = cam.gameObject.AddComponent<CameraFollow>();
            follow.Target = null;
            follow.Configure();
            // Frame the entrance behind the class select screen until a hero exists.
            cam.transform.position = new Vector3(GameConfig.HeroStart.x + 40f, GameConfig.HeroStart.y + 30f, -10f);

            if (!NetMode.IsClient) MonsterSpawner.SpawnAll(root.transform); // a client shows the server's monsters instead
            GameSession session = GameSession.Create(root.transform, follow);
            root.AddComponent<NameTags>();

            // Bloom (Built-in pipeline OnRenderImage) + F6 / F hotkeys; the camera is switched to HDR by BloomEffect.
            var bloom = cam.GetComponent<BloomEffect>();
            if (bloom == null) bloom = cam.gameObject.AddComponent<BloomEffect>();
            root.AddComponent<FeelHotkeys>().Init(bloom);

            root.AddComponent<Hud>();
            RpgHud.Create(root.transform);
            TouchControls.Create(root.transform);
            session.StartFromCommandLine();
            DebugCapture.AttachIfRequested(root, session);
        }

        /// <summary>
        /// Dedicated server world: the zone, its monsters and the bolt pool, with no camera, ground sprite, FX, audio, HUD, touch controls or
        /// class select. Heroes arrive later as headless entities (HeroController.InitHeadless).
        /// </summary>
        static void StartWithoutViews(GameObject root)
        {
            ComboProjectile.Init(root.transform);
            if (NetMode.IsClient)
            {
                // Unattended bot client (-nographics): the real client code path -- NetClient, proxies as view-less entities, input sender, bots -- without a camera or any art.
                GameSession bot = GameSession.Create(root.transform, null);
                bot.StartFromCommandLine();
                DebugCapture.AttachIfRequested(root, bot);
                return;
            }
            if (!NetMode.IsHeadless) { Debug.LogError("[ZenoniaProto] --nographics needs --mode server or --mode client"); Application.Quit(); return; }
            MonsterSpawner.SpawnAll(root.transform);
            if (HeadlessBench.AttachIfRequested(root)) return; // benchmark runs its own heroes, no network
            if (ZoneServer.Begin(root) == null) Application.Quit();
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
