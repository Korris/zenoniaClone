using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.Collections;
using UnityEngine;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    public partial class DebugCapture
    {
        string choreoArg;

        /// <summary>One scripted cast of the choreography video: who casts which tree skill, where the monster group stands (units ahead of the hero, spread radius), how long to hold.</summary>
        struct ChoreoShot { public HeroClass Cls; public string Skill; public float Ahead, Spread, Hold; public int Count; public bool Line; public float BreakAt; }

        static readonly ChoreoShot[] ChoreoShots =
        {
            new ChoreoShot { Cls = HeroClass.Warrior, Skill = "Earth Break", Ahead = 36f, Spread = 22f, Count = 5, Hold = 3.4f },
            new ChoreoShot { Cls = HeroClass.Wizard, Skill = "Arcane Bolt", Ahead = 76f, Spread = 8f, Count = 3, Hold = 2.0f },
            new ChoreoShot { Cls = HeroClass.Wizard, Skill = "Fireball", Ahead = 84f, Spread = 16f, Count = 4, Hold = 2.2f },
            new ChoreoShot { Cls = HeroClass.Wizard, Skill = "Arcane Lance", Ahead = 44f, Spread = 90f, Count = 4, Line = true, Hold = 2.2f },
            new ChoreoShot { Cls = HeroClass.Wizard, Skill = "Mana Shield", Ahead = 0f, Spread = 40f, Count = 3, Hold = 3.2f, BreakAt = 2.0f },
            new ChoreoShot { Cls = HeroClass.Wizard, Skill = "Frost Nova", Ahead = 0f, Spread = 40f, Count = 6, Hold = 2.6f },
            new ChoreoShot { Cls = HeroClass.Wizard, Skill = "Elemental Burst", Ahead = 64f, Spread = 20f, Count = 5, Hold = 2.8f },
            new ChoreoShot { Cls = HeroClass.Wizard, Skill = "Astral Cataclysm", Ahead = 80f, Spread = 46f, Count = 7, Hold = 4.0f },
            new ChoreoShot { Cls = HeroClass.Mechanic, Skill = "Rocket Barrage", Ahead = 55f, Spread = 28f, Count = 6, Hold = 3.6f },
        };

        /// <summary>
        /// --record-choreo [filter]: scripted comparison video. Earth Break (warrior), each wizard tree skill, then Rocket Barrage (mechanic) are cast at a monster group, one after the other.
        /// filter = comma list of skill names to keep (default all). 60 fps fixed step: frame_#####.png + audio.wav (AudioRenderer) to --shot-path.
        /// </summary>
        IEnumerator RecordChoreo(string filter)
        {
            Time.captureFramerate = 60;
            Directory.CreateDirectory(shotPath);
            Hud.CaptureClean = true;
            var audio = new List<float>(48000 * 2 * 40);
            int channels = AudioSettings.speakerMode == AudioSpeakerMode.Mono ? 1 : 2, frame = 0;
            AudioRenderer.Start();
            HeroClass? current = null;
            foreach (ChoreoShot s in ChoreoShots)
            {
                if (filter != "all" && !("," + filter.ToLowerInvariant() + ",").Contains("," + s.Skill.ToLowerInvariant().Replace(' ', '-') + ",")) continue;
                if (current != s.Cls) { session.Spawn(s.Cls); current = s.Cls; yield return null; }
                HeroController hero = GameSession.Hero;
                hero.God = true;
                foreach (Monster m in new List<Monster>(Monster.All)) Destroy(m.gameObject); // incl. the Frostfang King: it would walk into the shot
                HideCanvases();
                Vector2 spot = FindOpenSpot();
                hero.Teleport(spot, 2);
                Debug.Log("[ZenoniaProto] choreo spot " + spot + " for " + s.Skill);
                if (zoom > 0f) Camera.main.orthographicSize = zoom;
                PlaceGroup(hero, s);
                yield return null;
                int castAt = frame + 24, endAt = castAt + Mathf.RoundToInt(s.Hold * 60f), breakAt = s.BreakAt > 0f ? castAt + Mathf.RoundToInt(s.BreakAt * 60f) : -1;
                for (; frame < endAt; frame++)
                {
                    if (frame == castAt) Debug.Log("[ZenoniaProto] choreo cast " + s.Skill + " => " + CastByName(hero, s.Skill));
                    if (frame == breakAt) { GameObject go = GameObject.Find("Choreo_mana-shield"); if (go != null) go.GetComponent<SkillChoreoPlayer>().End(true); }
                    yield return new WaitForEndOfFrame();
                    Save(Path.Combine(shotPath, "frame_" + frame.ToString("D5") + ".png"));
                    int n = AudioRenderer.GetSampleCountForCaptureFrame() * channels;
                    var buf = new NativeArray<float>(n, Allocator.Temp);
                    AudioRenderer.Render(buf);
                    for (int i = 0; i < n; i++) audio.Add(buf[i]);
                    buf.Dispose();
                }
            }
            AudioRenderer.Stop();
            WriteWav(Path.Combine(shotPath, "audio.wav"), audio, channels, AudioSettings.outputSampleRate);
            Debug.Log("[ZenoniaProto] recorded choreo " + frame + " frames");
            Application.Quit();
        }

        /// <summary>uGUI HUD / chat off so the frames show only the world.</summary>
        static void HideCanvases() { foreach (Canvas c in FindObjectsByType<Canvas>(FindObjectsSortMode.None)) c.enabled = false; }

        /// <summary>Open snow west of the hero: free run of 150 units to the west, room north / south of it, nearest to the usual capture tile.</summary>
        static Vector2 FindOpenSpot()
        {
            Vector2 home = FrostHollowMap.TileFeet(11, 6, 2f), best = home;
            float bestD = float.MaxValue; bool found = false;
            for (int tx = 4; tx < GameConfig.TilesW - 4; tx++)
                for (int ty = 4; ty < GameConfig.TilesH - 4; ty++)
                {
                    Vector2 p = FrostHollowMap.TileFeet(tx, ty, 2f);
                    bool ok = true;
                    for (float d = -150f; d <= 16f && ok; d += 10f)
                        for (float dy = -24f; dy <= 24f && ok; dy += 24f) ok = !FrostHollowMap.RectBlocked(p.x + d - 4f, p.y + dy, 8f, 6f);
                    float dist = (p - home).sqrMagnitude;
                    if (ok && dist < bestD) { bestD = dist; best = p; found = true; }
                }
            if (!found) Debug.LogWarning("[ZenoniaProto] choreo: no open spot, using the default tile");
            return best;
        }

        /// <summary>Casts the tree skill with that display name at rank 3 (ignores MP / cooldown: the video is about the art).</summary>
        static bool CastByName(HeroController hero, string name)
        {
            foreach (SkillDef d in SkillTable.ForClass(hero.Rpg.ClassId)) if (d.Name == name) return SkillExecutor.Cast(hero, d.Index, 3);
            return false;
        }

        /// <summary>Monsters ahead of the (west-facing) hero: a ring around the point `Ahead` units away, or a row along the aim line.</summary>
        static void PlaceGroup(HeroController hero, ChoreoShot s)
        {
            Transform root = GameObject.Find("ZenoniaProto").transform;
            Vector2 fwd = new Vector2(-1f, 0f), centre = hero.Position + fwd * s.Ahead;
            for (int i = 0; i < s.Count; i++)
            {
                Vector2 p;
                if (s.Line) p = hero.Position + fwd * (s.Ahead + s.Spread * i / (s.Count - 1)) + new Vector2(0f, (i % 2) * 2f);
                else
                {
                    float a = i * Mathf.PI * 2f / s.Count + 0.4f;
                    p = centre + new Vector2(Mathf.Cos(a), Mathf.Sin(a) * 0.8f) * s.Spread * (i % 2 == 0 ? 1f : 0.55f);
                }
                Monster.Create(root, i % 3 == 2 ? MonsterDef.IceGoblin : MonsterDef.FrostSlime, FreeSpotNear(hero.Position, p), -1);
            }
        }
    }
}
