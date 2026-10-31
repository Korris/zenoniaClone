using UnityEngine;
using UnityEngine.UI;

namespace ZenoniaProto
{
    /// <summary>
    /// Map changes on screen. Offline, Update() watches the local hero's feet tile for a portal of the current map; Go() fades to black, swaps the map
    /// (collision data, ground / props / NPCs, monsters, music), puts the same hero (stats, bag, buffs untouched) on the arrival tile and fades back
    /// in. Instant() is the same swap without the fade, for loading a save and for entering online play. The simulation is paused while fading.
    /// Online the server decides (ZoneTransferService): a client never triggers a portal, it runs the same fade when told (GoOnline, see README, "Maps").
    /// </summary>
    [DefaultExecutionOrder(-840)]
    public sealed class MapTransfer : MonoBehaviour
    {
        const float FadeSeconds = 0.4f;
        static MapTransfer instance;

        Transform world;
        Image veil;
        float timer;
        int phase; // 0 idle, 1 fading out, 2 fading in
        MapPortal portal;
        System.Action onlineArrive;

        /// <summary>True while a fade is running (input is ignored by the hero because the clock is paused).</summary>
        public static bool Busy { get { return instance != null && instance.phase != 0; } }

        public static void Create(Transform parent)
        {
            var go = new GameObject("MapTransfer");
            go.transform.SetParent(parent, false);
            instance = go.AddComponent<MapTransfer>();
            instance.world = parent;
            instance.BuildVeil();
        }

        void OnDestroy() { if (instance == this) instance = null; }

        void BuildVeil()
        {
            var canvasGo = new GameObject("FadeCanvas");
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 200; // above the HUD, windows and toasts
            var imgGo = new GameObject("Veil", typeof(RectTransform));
            imgGo.transform.SetParent(canvasGo.transform, false);
            veil = imgGo.AddComponent<Image>();
            veil.color = Color.clear;
            veil.raycastTarget = false;
            RectTransform r = veil.rectTransform;
            r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = r.offsetMax = Vector2.zero;
        }

        /// <summary>Fade out, swap to the portal's target map, fade in. Ignored while another transfer runs or when not offline.</summary>
        public static void Go(MapPortal p)
        {
            if (instance == null || instance.phase != 0 || !NetMode.IsOffline || MapRegistry.Get(p.TargetMapId) == null) return;
            instance.portal = p;
            instance.phase = 1;
            instance.timer = 0f;
        }

        /// <summary>
        /// Online client: the server moved the hero. Fades out, calls `arrive` at full black (it swaps the world) and fades back in. False when this process draws nothing
        /// (a bot client without graphics): the caller swaps right away.
        /// </summary>
        public static bool GoOnline(System.Action arrive)
        {
            if (instance == null) return false;
            instance.onlineArrive = arrive;
            if (instance.phase != 1)
            {
                instance.timer = instance.phase == 2 ? Mathf.Max(0f, FadeSeconds - instance.timer) : 0f; // told again while fading in: back out from the same darkness
                instance.phase = 1;
            }
            GameClock.Paused = true; // this very frame: the hero updates after the client
            return true;
        }

        /// <summary>Puts the camera on its target without the smoothing lag.</summary>
        static void SnapCamera()
        {
            Camera cam = Camera.main;
            CameraFollow follow = cam != null ? cam.GetComponent<CameraFollow>() : null;
            if (follow != null) follow.Configure(); // re-centres on the target without the smoothing lag
        }

        /// <summary>Swaps the current map at once (no fade, no hero move): the caller positions the hero afterwards.</summary>
        public static void Instant(string mapId)
        {
            if (MapRegistry.Get(mapId) == null) return;
            ClearEntities();
            MapRegistry.SetCurrent(mapId);
            WorldBuilder.Rebuild();
            IGameMap map = MapRegistry.Current;
            if (map.Hostile && NetMode.IsOffline && instance != null) SpawnMonsters(instance.world);
            GameMusic.Play(map.Music);
        }

        /// <summary>Removes the old map's monsters, drops and spawner (heroes and the Zone itself stay).</summary>
        static void ClearEntities()
        {
            Zone z = Zone.Main;
            if (z == null) return;
            for (int i = z.Monsters.Count - 1; i >= 0; i--) if (z.Monsters[i] != null) Destroy(z.Monsters[i].gameObject);
            for (int i = z.Loot.Count - 1; i >= 0; i--) if (z.Loot[i] != null) Destroy(z.Loot[i].gameObject);
            if (z.Spawner != null) Destroy(z.Spawner.gameObject);
            z.Spawner = null; z.Boss = null;
            z.Monsters.Clear(); z.Loot.Clear();
            z.BossTimer.Cancel();
        }

        /// <summary>Fills a hostile map; its boss stays dead when this save already killed it (offline a boss never returns).</summary>
        static void SpawnMonsters(Transform parent)
        {
            MonsterSpawner.SpawnAll(parent);
            if (RpgSession.BossDefeatedOn(MapRegistry.Current.Id) && Monster.Boss != null) Destroy(Monster.Boss.gameObject);
        }

        void Update()
        {
            if (phase == 0) { CheckPortal(); return; }
            GameClock.Paused = true; // frozen sim: the hero neither walks nor takes hits during the fade
            timer += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(timer / FadeSeconds);
            if (phase == 1)
            {
                veil.color = new Color(0f, 0f, 0f, t);
                if (t >= 1f) { Arrive(); phase = 2; timer = 0f; }
            }
            else
            {
                veil.color = new Color(0f, 0f, 0f, 1f - t);
                if (t >= 1f) { phase = 0; GameClock.Paused = false; }
            }
        }

        void CheckPortal()
        {
            HeroController h = GameSession.Hero;
            if (h == null || h.IsDead || !NetMode.IsOffline || UiWindows.ModalOpen) return;
            Vector2 p = h.Position;
            MapPortal found;
            if (MapPortals.TryFind(MapRegistry.Current.Portals, p.x, p.y, out found)) Go(found);
        }

        /// <summary>At full black: swap the map, move the hero to the arrival tile and snap the camera onto it.</summary>
        void Arrive()
        {
            if (onlineArrive != null)
            {
                System.Action arrive = onlineArrive;
                onlineArrive = null;
                arrive();
                SnapCamera();
                return;
            }
            Instant(portal.TargetMapId);
            HeroController h = GameSession.Hero;
            if (h == null) return;
            h.Teleport(FrostHollowMap.TileFeet(portal.TargetTx, portal.TargetTy, 2f), 0);
            SnapCamera();
        }
    }
}
