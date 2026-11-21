using System.Collections.Generic;
using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Scene side of the 2.5D view (View25D), on the game root of every drawing window (idle on 2D maps). Late every frame, after the game has placed
    /// everything on the x/y plane: stands heroes, monsters, pets and loot up as billboards on their feet (with a sun shadow each), lights them
    /// (View25DLighting: sun tint, canopy shade, shader globals), runs the ambient FX (View25DAmbientFx: motes, falling leaves) and adds the atmosphere
    /// pass to the camera. When the map on screen is not a 2.5D map it puts everything back and does nothing.
    /// </summary>
    [DefaultExecutionOrder(5000)]
    public class View25DWorld : MonoBehaviour
    {
        readonly Dictionary<Transform, View25DDropShadow> units = new Dictionary<Transform, View25DDropShadow>();
        readonly HashSet<Transform> seen = new HashSet<Transform>();
        readonly List<Transform> gone = new List<Transform>();
        View25DAmbientFx ambient;
        Camera cam;
        bool wasOn;

        View25DAtmosphere atmosphere;

        /// <summary>On every drawing window (2.5D maps are data: View25D.Maps); idle while the map on screen is 2D.</summary>
        public static void AttachIfRequested(GameObject root, Camera cam)
        {
            if (!View25D.Available) return;
            var w = root.AddComponent<View25DWorld>();
            if (cam != null)
            {
                w.atmosphere = cam.GetComponent<View25DAtmosphere>() ?? cam.gameObject.AddComponent<View25DAtmosphere>();
                w.atmosphere.enabled = false; // only while a 2.5D map is on screen: no full-screen blit on 2D maps
                if (cam.GetComponent<View25DCameraInput>() == null) cam.gameObject.AddComponent<View25DCameraInput>(); // player tilt / yaw (idle off the RO3 maps)
            }
            if (View25D.Requested) Debug.Log("[ZenoniaProto] View25D prototype requested: tilt " + View25D.TiltDeg + " deg, fov " + View25D.FovDeg + " deg");
        }

        void LateUpdate()
        {
            Zone zone = Zone.Main;
            bool on = View25D.Active && zone != null;
            if (atmosphere != null && atmosphere.enabled != on) atmosphere.enabled = on;
            HideHudIfAsked();
            if (!on) { if (wasOn) TurnOff(); return; }
            wasOn = true;
            View25D.TickStanding(); // player camera tilt / yaw (View25DCameraControl): standing props turn with it
            Quaternion tilt = View25D.Tilt, untilt = Quaternion.Inverse(tilt);
            cam = Camera.main;
            seen.Clear();
            IReadOnlyList<HeroController> heroes = zone.Heroes.All;
            for (int i = 0; i < heroes.Count; i++)
            {
                HeroController h = heroes[i];
                if (h == null) continue;
                Stand(h.transform, tilt, untilt, true);
                HeroPet pet = HeroPet.Of(h);
                if (pet != null) Stand(pet.transform, tilt, untilt, false);
            }
            for (int i = 0; i < zone.Monsters.Count; i++) if (zone.Monsters[i] != null) Stand(zone.Monsters[i].transform, tilt, untilt, true);
            for (int i = 0; i < zone.Loot.Count; i++) if (zone.Loot[i] != null && zone.Loot[i].transform.rotation != tilt) zone.Loot[i].transform.rotation = tilt;
            gone.Clear();
            foreach (KeyValuePair<Transform, View25DDropShadow> kv in units) if (kv.Key == null || !seen.Contains(kv.Key)) gone.Add(kv.Key);
            foreach (Transform t in gone) { units[t].Remove(); units.Remove(t); }
            View25DLighting.EndFrame(seen);
            View25DLighting.TickGlobals(cam);
            View25DPropFade.Tick(cam);
            if (ambient == null) ambient = new View25DAmbientFx(transform);
            ambient.Tick(cam, heroes.Count);
        }

        void Stand(Transform t, Quaternion tilt, Quaternion untilt, bool compensate)
        {
            seen.Add(t);
            View25DDropShadow s;
            if (!units.TryGetValue(t, out s))
            {
                Material m = View25D.ShadowMaterial;
                if (m == null) { t.rotation = tilt; return; }
                s = new View25DDropShadow(t, m);
                units[t] = s;
            }
            s.Tick(tilt, untilt, compensate ? View25D.UnitScale(cam, t.position) : 0f);
            View25DLighting.TickUnit(t);
        }

        void TurnOff()
        {
            wasOn = false;
            foreach (KeyValuePair<Transform, View25DDropShadow> kv in units) kv.Value.Remove();
            units.Clear();
            View25DPropFade.Reset();
            View25DLighting.Reset();
            View25D.ClearStanding();
            if (ambient != null) { ambient.Destroy(); ambient = null; }
        }

        /// <summary>`--no-hud` (test captures): hides every screen-space canvas and IMGUI HUD so a shot shows only the world (RO3 check: "turn the HUD off,
        /// the Cleric standing in the forest must look lit and shadowed"). Re-applied every second: windows built later hide too.</summary>
        static void HideHudIfAsked()
        {
            if (!noHud || Time.unscaledTime < nextHudHide) return;
            nextHudHide = Time.unscaledTime + 1f;
            foreach (Canvas c in FindObjectsByType<Canvas>(FindObjectsSortMode.None)) if (c.isRootCanvas && c.renderMode != RenderMode.WorldSpace && c.enabled) c.enabled = false;
            Hud.CaptureClean = true; // IMGUI layer: boss bar only (its debug lines are hidden by default anyway)
        }
        static bool noHud = !ReleaseBuild.IsRelease && System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "--no-hud") >= 0;
        static float nextHudHide;
    }
}
