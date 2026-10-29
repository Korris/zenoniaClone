using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Player-build verification helper, active only when a command-line flag is present (normal play is unaffected):
    ///   --class warrior|wizard|mechanic   skip the class select
    ///   --tp tx,ty                        put the hero on a tile (and --face down|up|left|right)
    ///   --tier N / --enhanced             gear tier / enhanced art
    ///   --bot fight|attack|walk           fight: walk to the nearest monster and attack it; attack: hold attack; walk: walk east
    ///   --zoom N                          camera orthographic half-height (default 80) for close-ups
    ///   --shots 2,5,8 --shot-path P       ScreenCapture to P_1.png, P_2.png ... at those seconds, then quit
    ///   --solids                          start with the collision tint on
    ///   --skill-at 1.5,9                  press the class skill (L) at those seconds after start
    ///   --dummies N                       spawn N frost slimes in front of the hero (skill / AoE targets)
    ///   --god                             hero ignores damage
    /// </summary>
    public class DebugCapture : MonoBehaviour
    {
        GameSession session;
        string classArg, faceArg, shotPath, botArg;
        float[] shotTimes, skillTimes;
        int dummies;
        Vector2Int tpTile = new Vector2Int(-1, -1);
        int tier = -1;
        float zoom;
        bool enhanced, solids, god;

        public static void AttachIfRequested(GameObject root, GameSession gameSession)
        {
            string[] args = Environment.GetCommandLineArgs();
            if (Array.IndexOf(args, "--class") < 0 && Array.IndexOf(args, "--shots") < 0) return;
            var d = root.AddComponent<DebugCapture>();
            d.session = gameSession;
            d.Parse(args);
            d.StartCoroutine(d.Run());
        }

        static HeroClass ParseClass(string name)
        {
            foreach (HeroClassDef d in HeroClassDef.All) if (string.Equals(d.Name, name, StringComparison.OrdinalIgnoreCase)) return d.Id;
            return HeroClass.Warrior;
        }

        static string Arg(string[] a, string key)
        {
            int i = Array.IndexOf(a, key);
            return i >= 0 && i + 1 < a.Length ? a[i + 1] : null;
        }

        void Parse(string[] a)
        {
            classArg = Arg(a, "--class");
            faceArg = Arg(a, "--face");
            botArg = Arg(a, "--bot");
            shotPath = Arg(a, "--shot-path") ?? Path.Combine(Application.persistentDataPath, "shot");
            string tp = Arg(a, "--tp");
            if (tp != null) { string[] p = tp.Split(','); tpTile = new Vector2Int(int.Parse(p[0]), int.Parse(p[1])); }
            string z = Arg(a, "--zoom");
            if (z != null) zoom = float.Parse(z, System.Globalization.CultureInfo.InvariantCulture);
            string t = Arg(a, "--tier");
            if (t != null) tier = int.Parse(t);
            enhanced = Array.IndexOf(a, "--enhanced") >= 0;
            solids = Array.IndexOf(a, "--solids") >= 0;
            god = Array.IndexOf(a, "--god") >= 0;
            string dm = Arg(a, "--dummies");
            if (dm != null) dummies = int.Parse(dm);
            shotTimes = Times(Arg(a, "--shots"));
            skillTimes = Times(Arg(a, "--skill-at"));
        }

        static float[] Times(string list)
        {
            if (list == null) return null;
            string[] parts = list.Split(',');
            var t = new float[parts.Length];
            for (int i = 0; i < parts.Length; i++) t[i] = float.Parse(parts[i], System.Globalization.CultureInfo.InvariantCulture);
            return t;
        }

        IEnumerator CastSkills(float start)
        {
            foreach (float at in skillTimes)
            {
                while (Time.realtimeSinceStartup - start < at) yield return null;
                HeroController h = GameSession.Hero;
                Debug.Log("[ZenoniaProto] skill cast at " + at + " => " + (h != null && h.TryCastSkill()));
            }
        }

        void SpawnDummies(HeroController hero)
        {
            Transform root = GameObject.Find("ZenoniaProto").transform;
            Vector2[] dirs = { new Vector2(0f, -1f), new Vector2(0f, 1f), new Vector2(-1f, 0f), new Vector2(1f, 0f) };
            Vector2 fwd = dirs[hero.Facing], side = new Vector2(-fwd.y, fwd.x);
            for (int i = 0; i < dummies; i++)
            {
                Vector2 p = hero.Position + fwd * (30f + (i % 3) * 9f) + side * ((i - dummies * 0.5f) * 9f);
                Monster.Create(root, i % 3 == 2 ? MonsterDef.IceGoblin : MonsterDef.FrostSlime, p, -1);
            }
        }

        IEnumerator Run()
        {
            yield return null;
            if (classArg != null)
            {
                HeroClass c = ParseClass(classArg);
                session.Spawn(c);
                yield return null;
                HeroController hero = GameSession.Hero;
                if (tpTile.x >= 0) hero.Teleport(FrostHollowMap.TileFeet(tpTile.x, tpTile.y, 2f), faceArg == "up" ? 1 : faceArg == "left" ? 2 : faceArg == "right" ? 3 : 0);
                if (tier >= 0 || enhanced) hero.View.SetGear(Mathf.Max(tier, 0), enhanced);
                if (botArg == "fight") EnableFightBot();
                else if (botArg == "attack") InputAdapter.BotPressed = k => k == GameKey.Attack;   // hold attack in place (art / FX close-ups)
                else if (botArg == "walk") InputAdapter.BotHeld = k => k == GameKey.Right;          // walk east
                if (zoom > 0f) Camera.main.orthographicSize = zoom;
                HeroController.DebugGod = god;
                if (dummies > 0) SpawnDummies(hero);
            }
            if (solids) WorldBuilder.ToggleSolidOverlay();
            float start = Time.realtimeSinceStartup;
            if (skillTimes != null) StartCoroutine(CastSkills(start));
            if (shotTimes == null) yield break;
            for (int i = 0; i < shotTimes.Length; i++)
            {
                while (Time.realtimeSinceStartup - start < shotTimes[i]) yield return null;
                yield return new WaitForEndOfFrame();
                Save(shotPath + "_" + (i + 1) + ".png");
                HeroController h = GameSession.Hero;
                Debug.Log("[ZenoniaProto] shot " + (i + 1) + " t=" + Time.realtimeSinceStartup.ToString("F1") + " dt=" + GameClock.Dt + " hero=" + (h != null ? h.Position + " hp " + h.Hp + " dead " + h.IsDead : "none") + " monsters=" + Monster.All.Count);
                yield return null;
            }
            for (int i = 0; i < 10; i++) yield return null;
            Application.Quit();
        }

        /// <summary>End-of-frame backbuffer grab (includes the IMGUI HUD) written as PNG.</summary>
        static void Save(string path)
        {
            var tex = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Destroy(tex);
        }

        static bool HasLineOfSight(Vector2 a, Vector2 b)
        {
            int n = Mathf.CeilToInt(Vector2.Distance(a, b) / 6f);
            for (int i = 1; i < n; i++)
            {
                Vector2 p = Vector2.Lerp(a, b, i / (float)n);
                if (FrostHollowMap.RectBlocked(p.x - 3f, p.y, 6f, 6f)) return false;
            }
            return true;
        }

        /// <summary>Walks toward the nearest living monster (aligning on one axis first so the facing is right) and attacks in range.</summary>
        void EnableFightBot()
        {
            Vector2 move = Vector2.zero;
            bool attack = false;
            InputAdapter.BotHeld = k =>
            {
                Think(ref move, ref attack);
                return (k == GameKey.Left && move.x < 0f) || (k == GameKey.Right && move.x > 0f) || (k == GameKey.Down && move.y < 0f) || (k == GameKey.Up && move.y > 0f);
            };
            InputAdapter.BotPressed = k => k == GameKey.Attack && attack;
        }

        static void Think(ref Vector2 move, ref bool attack)
        {
            move = Vector2.zero; attack = false;
            HeroController hero = GameSession.Hero;
            if (hero == null || hero.IsDead) return;
            Monster best = null; float bestD = float.MaxValue;
            foreach (Monster m in Monster.All)
            {
                if (m.IsDying || m.MaxHp > 300f || !HasLineOfSight(hero.Position, m.Center)) continue; // skip the boss and anything behind a cliff
                float d = Vector2.Distance(m.Center, hero.Position);
                if (d < bestD) { bestD = d; best = m; }
            }
            if (best == null) return;
            Vector2 diff = best.Center - hero.Position - new Vector2(0f, 4f);
            float range = hero.Class.Ranged ? 80f : 24f;
            bool horizontal = Mathf.Abs(diff.x) >= Mathf.Abs(diff.y);
            float along = horizontal ? diff.x : diff.y, across = horizontal ? diff.y : diff.x;
            if (Mathf.Abs(across) > 5f) { if (horizontal) move.y = Mathf.Sign(across); else move.x = Mathf.Sign(across); }
            else if (Mathf.Abs(along) > range) { if (horizontal) move.x = Mathf.Sign(along); else move.y = Mathf.Sign(along); }
            else
            {
                attack = true; // keep the direction toward the target held so the hero faces it when the swing starts
                if (horizontal) move.x = Mathf.Sign(along); else move.y = Mathf.Sign(along);
            }
        }
    }
}
