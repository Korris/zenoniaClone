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
    ///   --nobloom                         disable the bloom post effect (A/B for blown-out FX)
    ///   --run-seconds N                   quit after N seconds (no screenshots); with --perf logs every frame slower than 16 ms
    ///   --mech-blue                       mechanic FX start in blue (default orange)
    ///   --dummy-dist N                    distance of the first dummy in front of the hero (default 30)
    ///   --god                             hero ignores damage
    ///   --record-combo wizard|mechanic|cleric  scripted basic-attack video: hero holds attack at dummies (3 s lead-in of idle, then ~6 s of combos), same frame dump as below
    ///                                     (--mech-blue for the blue mechanic, --zoom for a close-up)
    ///   --record-ultimate warrior|wizard  scripted video scene (monsters + King nearby, basic combo, then the skill, tail), 60 fps fixed-step,
    ///                                     writes frame_00000.png... to --shot-path (a folder), then quits; encode with ffmpeg
    /// </summary>
    public partial class DebugCapture : MonoBehaviour
    {
        GameSession session;
        string recordArg, classArg, faceArg, shotPath, botArg;
        float[] shotTimes, skillTimes;
        int dummies;
        float dummyDist = 30f;
        float runSeconds, lastCastTime = -100f;
        bool perf;
        Vector2Int tpTile = new Vector2Int(-1, -1);
        int tier = -1;
        float zoom;
        bool enhanced, solids, god;

        public static void AttachIfRequested(GameObject root, GameSession gameSession)
        {
            string[] args = Environment.GetCommandLineArgs();
            if (Array.IndexOf(args, "--class") < 0 && Array.IndexOf(args, "--shots") < 0 && Array.IndexOf(args, "--record-ultimate") < 0 && Array.IndexOf(args, "--record-combo") < 0) return;
            var d = root.AddComponent<DebugCapture>();
            d.session = gameSession;
            d.Parse(args);
            d.StartCoroutine(d.Run());
        }

        void Update()
        {
            if (!perf || Time.frameCount < 30) return;
            float ms = Time.unscaledDeltaTime * 1000f;
            if (ms > 16f) Debug.Log("[ZenoniaProto] HITCH " + ms.ToString("F0") + " ms at t=" + Time.realtimeSinceStartup.ToString("F2") + " (" + (Time.realtimeSinceStartup - lastCastTime).ToString("F2") + " s after the last skill cast)");
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
            recordArg = Arg(a, "--record-ultimate");
            comboArg = Arg(a, "--record-combo");
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
            ClassPalette.MechanicBlue = Array.IndexOf(a, "--mech-blue") >= 0;
            string dd = Arg(a, "--dummy-dist");
            if (dd != null) dummyDist = float.Parse(dd, System.Globalization.CultureInfo.InvariantCulture);
            perf = Array.IndexOf(a, "--perf") >= 0;
            string rs = Arg(a, "--run-seconds");
            if (rs != null) runSeconds = float.Parse(rs, System.Globalization.CultureInfo.InvariantCulture);
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
                lastCastTime = Time.realtimeSinceStartup;
            }
        }

        void SpawnDummies(HeroController hero)
        {
            Transform root = GameObject.Find("ZenoniaProto").transform;
            Vector2[] dirs = { new Vector2(0f, -1f), new Vector2(0f, 1f), new Vector2(-1f, 0f), new Vector2(1f, 0f) };
            Vector2 fwd = dirs[hero.Facing], side = new Vector2(-fwd.y, fwd.x);
            for (int i = 0; i < dummies; i++)
            {
                Vector2 p = hero.Position + fwd * (dummyDist + (i % 3) * 9f) + side * ((i - dummies * 0.5f) * 9f);
                Monster.Create(root, i % 3 == 2 ? MonsterDef.IceGoblin : MonsterDef.FrostSlime, p, -1);
            }
        }

        /// <summary>Fixed 60 fps scripted scene: idle, basic combo, skill, then hold until the effect has played out. One PNG per frame.</summary>
        IEnumerator RecordUltimate(string who)
        {
            Time.captureFramerate = 60;
            Directory.CreateDirectory(shotPath);
            session.Spawn(ParseClass(who));
            yield return null;
            HeroController hero = GameSession.Hero;
            HeroController.DebugGod = true;
            hero.Teleport(FrostHollowMap.TileFeet(11, 6, 2f), 2); // facing west: group ahead, Frostfang King beyond
            dummies = 5;
            SpawnDummies(hero);
            int frame = 0, attackFrom = 45, attackTo = 135, castAt = 150, endAt = castAt + 160 + 100;
            InputAdapter.BotPressed = k => k == GameKey.Attack && frame >= attackFrom && frame < attackTo;
            for (; frame < endAt; frame++)
            {
                if (frame == castAt) Debug.Log("[ZenoniaProto] record cast => " + hero.TryCastSkill());
                yield return new WaitForEndOfFrame();
                Save(Path.Combine(shotPath, "frame_" + frame.ToString("D5") + ".png"));
            }
            Debug.Log("[ZenoniaProto] recorded " + frame + " frames");
            Application.Quit();
        }

        IEnumerator Run()
        {
            yield return null;
            if (comboArg != null) { yield return RecordCombo(comboArg); yield break; }
            string rec = recordArg;
            if (rec != null) { yield return RecordUltimate(rec); yield break; }
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
                if (Array.IndexOf(Environment.GetCommandLineArgs(), "--nobloom") >= 0) { FeelConfig.BloomOn = false; var b = Camera.main.GetComponent<BloomEffect>(); if (b != null) b.enabled = false; }
                if (dummies > 0) SpawnDummies(hero);
            }
            if (solids) WorldBuilder.ToggleSolidOverlay();
            float start = Time.realtimeSinceStartup;
            if (skillTimes != null) StartCoroutine(CastSkills(start));
            if (runSeconds > 0f) { while (Time.realtimeSinceStartup - start < runSeconds) yield return null; Application.Quit(); yield break; }
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
