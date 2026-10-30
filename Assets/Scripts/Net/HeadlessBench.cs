using System;
using System.Collections.Generic;
using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Server tick cost probe: `--mode server --headless-bench N [--bench-seconds S] [--bench-brawl]` spawns N headless heroes (no views, scripted
    /// input, spread over the map so every monster is awake), runs the sim uncapped for S seconds (default 10) and logs the per-frame time
    /// (avg / p50 / p99 / max, ms). Each frame is exactly one 30 Hz tick of work, so this is the CPU cost of a tick with N heroes.
    /// </summary>
    public class HeadlessBench : MonoBehaviour
    {
        const int WarmupFrames = 60;
        int heroCount, frame;
        float seconds = 10f, startTime, last;
        bool brawl;
        readonly List<float> samples = new List<float>(4096);

        public static void AttachIfRequested(GameObject root)
        {
            string[] args = Environment.GetCommandLineArgs();
            int i = Array.IndexOf(args, "--headless-bench");
            if (i < 0 || i + 1 >= args.Length) return;
            var b = root.AddComponent<HeadlessBench>();
            int.TryParse(args[i + 1], out b.heroCount);
            int s = Array.IndexOf(args, "--bench-seconds");
            if (s >= 0 && s + 1 < args.Length) float.TryParse(args[s + 1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out b.seconds);
            b.brawl = Array.IndexOf(args, "--bench-brawl") >= 0;
            Application.targetFrameRate = -1; // measure the work, not the 30 Hz sleep
        }

        void Start()
        {
            SaveStoreOff();
            for (int i = 0; i < heroCount; i++) SpawnHero(i);
            Debug.Log("[ZenoniaProto] bench: " + heroCount + " headless heroes, " + Monster.All.Count + " monsters, " + (brawl ? "brawl" : "idle") + ", " + seconds + " s");
        }

        static void SaveStoreOff() { ZenoniaProto.Rpg.SaveStore.Enabled = false; }

        void SpawnHero(int i)
        {
            HeroClassDef def = HeroClassDef.All[i % HeroClassDef.All.Length];
            var go = new GameObject("BenchHero_" + i);
            go.transform.SetParent(transform, false);
            HeroController h = go.AddComponent<HeroController>();
            h.InitHeadless(def, null);
            h.Owner = new PlayerRpg(h.Rpg);
            h.God = !brawl; // idle heroes are targets nobody should kill; brawlers may die and respawn like real players
            bool attackNow = false;
            h.AiHeld = k => brawl && ((k == GameKey.Right && (frame / 40 + i) % 4 == 0) || (k == GameKey.Left && (frame / 40 + i) % 4 == 2));
            h.AiPressed = k => { if (!brawl || k != GameKey.Attack) return false; attackNow = !attackNow; return attackNow; };
            Vector2 feet;
            var zones = FrostHollowMap.Zones;
            if (FrostHollowMap.TryZoneSpot(zones[i % zones.Length], Vector2.zero, 0f, out feet)) h.Teleport(feet, i % 4);
        }

        void Update()
        {
            float now = Time.realtimeSinceStartup;
            frame++;
            if (frame == WarmupFrames) { startTime = now; last = now; return; }
            if (frame < WarmupFrames) return;
            samples.Add((now - last) * 1000f);
            last = now;
            if (now - startTime < seconds) return;
            Report();
            Application.Quit();
            enabled = false;
        }

        void Report()
        {
            samples.Sort();
            float sum = 0f;
            foreach (float s in samples) sum += s;
            int n = samples.Count;
            string line = "[ZenoniaProto] bench RESULT heroes=" + heroCount + " monsters=" + Monster.All.Count + " frames=" + n +
                " avg=" + (sum / n).ToString("F3") + "ms p50=" + samples[n / 2].ToString("F3") + "ms p99=" + samples[Mathf.Min(n - 1, (int)(n * 0.99f))].ToString("F3") +
                "ms max=" + samples[n - 1].ToString("F3") + "ms mode=" + (brawl ? "brawl" : "idle");
            Debug.Log(line);
        }
    }
}
