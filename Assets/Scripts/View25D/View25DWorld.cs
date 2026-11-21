using System.Collections.Generic;
using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Scene side of the 2.5D view (View25D), on the game root of every drawing window (idle on 2D maps). Late every frame, after the game has placed
    /// everything on the x/y plane: stands heroes, monsters, pets and loot up as billboards on their feet (with a sun shadow each), keeps the
    /// ambient motes floating at different heights around the camera, and adds the atmosphere pass to the camera. When the map on screen is not the
    /// prototype map it puts everything back and does nothing.
    /// </summary>
    [DefaultExecutionOrder(5000)]
    public class View25DWorld : MonoBehaviour
    {
        readonly Dictionary<Transform, View25DDropShadow> units = new Dictionary<Transform, View25DDropShadow>();
        readonly HashSet<Transform> seen = new HashSet<Transform>();
        readonly List<Transform> gone = new List<Transform>();
        ParticleSystem motes;
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
            }
            if (View25D.Requested) Debug.Log("[ZenoniaProto] View25D prototype requested: tilt " + View25D.TiltDeg + " deg, fov " + View25D.FovDeg + " deg");
        }

        void LateUpdate()
        {
            Zone zone = Zone.Main;
            bool on = View25D.Active && zone != null;
            if (atmosphere != null && atmosphere.enabled != on) atmosphere.enabled = on;
            if (!on) { if (wasOn) TurnOff(); return; }
            wasOn = true;
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
            View25DPropFade.Tick(cam);
            TickMotes();
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
        }

        void TurnOff()
        {
            wasOn = false;
            foreach (KeyValuePair<Transform, View25DDropShadow> kv in units) kv.Value.Remove();
            units.Clear();
            View25DPropFade.Reset();
            if (motes != null) { Destroy(motes.gameObject); motes = null; }
        }

        // ---- ambient motes: soft green-gold specks drifting at 4-70 units above the ground, so the perspective gives them parallax ----

        const float MoteRate = 3.5f, MoteAreaW = 520f, MoteAreaD = 380f;
        float moteDebt;

        void TickMotes()
        {
            Camera cam = Camera.main;
            if (cam == null || !FeelConfig.FxOn) return;
            if (motes == null)
            {
                Material m = MoteMaterial();
                if (m == null) return;
                motes = WantedFxParticles.Create(transform, "View25DMotes", m, FeelConfig.FxOrder - 40, 400, 0f);
                var main = motes.main;
                main.loop = true; // emitted by hand forever: keep the system alive past its 5 s duration
            }
            // centre of the visible ground: straight ahead of the camera on z = 0
            Vector3 c = cam.transform.position, f = cam.transform.forward;
            if (f.z <= 0.01f) return;
            Vector3 g = c + f * (-c.z / f.z);
            moteDebt += MoteRate * Time.deltaTime;
            while (moteDebt >= 1f)
            {
                moteDebt -= 1f;
                float height = Random.Range(4f, 70f);
                var p = new ParticleSystem.EmitParams
                {
                    position = new Vector3(g.x + Random.Range(-0.5f, 0.5f) * MoteAreaW, g.y + Random.Range(-0.45f, 0.55f) * MoteAreaD, -height),
                    velocity = new Vector3(Random.Range(-4f, 4f), Random.Range(-2f, 3f), -Random.Range(1f, 4f)),
                    startSize = Random.Range(1.6f, 3.4f),
                    startColor = Random.value < 0.6f ? new Color(0.75f, 1f, 0.55f, 0.9f) : new Color(1f, 0.9f, 0.55f, 0.9f),
                    startLifetime = Random.Range(3.5f, 6.5f),
                    applyShapeToPosition = false,
                };
                motes.Emit(p, 1);
            }
        }

        static Material moteMat;
        static Material MoteMaterial()
        {
            if (moteMat != null) return moteMat;
            Shader s = Resources.Load<Shader>("Shaders/LightParticle");
            Texture2D tex = Resources.Load<Texture2D>("Sprites/fx/light-skill/portal-mote");
            if (s == null || !s.isSupported || tex == null) return null;
            moteMat = new Material(s) { name = "View25D_Mote" };
            moteMat.SetTexture("_MainTex", tex);
            moteMat.SetColor("_EmissionColor", new Color(0.9f, 1.3f, 0.6f, 1f));
            moteMat.SetFloat("_CoreWhite", 0.45f);
            moteMat.SetColor("_BodyColor", new Color(0.85f, 1f, 0.6f, 0.55f));
            return moteMat;
        }
    }
}
