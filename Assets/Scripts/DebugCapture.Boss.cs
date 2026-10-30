using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.Collections;
using UnityEngine;

namespace ZenoniaProto
{
    public partial class DebugCapture
    {
        const int BossFrames = 60 * 12;
        string bossArg;
        bool party;

        /// <summary>
        /// --record-boss &lt;class&gt;: one hero fights the Frostfang King (god mode); the fight bot keeps it chasing / facing the boss and
        /// holding attack, the class skill goes off every 4 s. 12 s at a fixed 60 fps: frames + game audio (see RecordFrames).
        /// </summary>
        IEnumerator RecordBoss(string who)
        {
            session.Spawn(ParseClass(who));
            yield return null;
            HeroController hero = GameSession.Hero;
            hero.God = true;
            Hud.CaptureClean = true;
            hero.Teleport(FrostHollowMap.TileFeet(11, 6, 2f), 2); // facing west, inside the King's aggro radius
            EnableFightBot(true);
            if (zoom > 0f) Camera.main.orthographicSize = zoom;
            yield return RecordFrames(frame => { if (frame >= 90 && frame % 240 == 90) hero.TryCastSkill(); });
        }

        /// <summary>--record-party: the warrior (player) plus wizard / mechanic / assassin / cleric as AI party members, all on the King.</summary>
        IEnumerator RecordParty()
        {
            session.Spawn(HeroClass.Warrior);
            yield return null;
            Hud.CaptureClean = true;
            var heroes = new List<HeroController> { GameSession.Hero };
            Vector2 home = FrostHollowMap.TileFeet(11, 6, 2f);
            heroes[0].Teleport(home, 2);
            // Everyone starts on the open ground around the player (east of the King's lair) and fights from that side: the cliffs
            // north / south of the lair trap a hero that tries to walk around it. Melee in front, casters behind at their range.
            HeroClass[] others = { HeroClass.Wizard, HeroClass.Mechanic, HeroClass.Assassin, HeroClass.Cleric };
            Vector2[] starts = { new Vector2(26f, 12f), new Vector2(26f, -12f), new Vector2(10f, -16f), new Vector2(40f, 0f) };
            for (int i = 0; i < others.Length; i++)
                heroes.Add(session.SpawnPartyMember(others[i], FreeSpotNear(home, home + starts[i]), 2));
            Vector2[] offsets = { new Vector2(16f, 0f), new Vector2(56f, 14f), new Vector2(60f, -10f), new Vector2(14f, -12f), new Vector2(72f, 2f) };
            for (int i = 0; i < heroes.Count; i++) { heroes[i].God = true; DriveByAi(heroes[i], offsets[i]); }
            if (zoom > 0f) Camera.main.orthographicSize = zoom;
            yield return RecordFrames(frame =>
            {
                for (int i = 0; i < heroes.Count; i++)
                    if (frame >= 90 + i * 25 && (frame - 90 - i * 25) % 240 == 0) heroes[i].TryCastSkill(); // staggered skills
            });
        }

        /// <summary>`want` if a hero box fits there, else the first free point stepping back toward `from` (always walkable).</summary>
        static Vector2 FreeSpotNear(Vector2 from, Vector2 want)
        {
            for (int i = 0; i <= 10; i++)
            {
                Vector2 p = Vector2.Lerp(want, from, i / 10f);
                if (!FrostHollowMap.RectBlocked(p.x - 4f, p.y, 8f, 6f)) return p;
            }
            return from;
        }

        /// <summary>Per-hero fight bot (the boss while it lives, then the nearest monster), read through HeroController.AiHeld / AiPressed.</summary>
        static void DriveByAi(HeroController hero, Vector2 offset)
        {
            Vector2 move = Vector2.zero;
            bool attack = false;
            int thoughtFrame = -1;
            Action think = () =>
            {
                if (thoughtFrame == Time.frameCount) return;
                thoughtFrame = Time.frameCount;
                Think(hero, offset, ref move, ref attack, true);
            };
            hero.AiHeld = k =>
            {
                think();
                return (k == GameKey.Left && move.x < 0f) || (k == GameKey.Right && move.x > 0f) || (k == GameKey.Down && move.y < 0f) || (k == GameKey.Up && move.y > 0f);
            };
            hero.AiPressed = k => { think(); return k == GameKey.Attack && attack; };
        }

        /// <summary>BossFrames frames at a fixed 60 fps to --shot-path/frame_00000.png..., game audio (AudioRenderer, frame-synced) to audio.wav, then quit.</summary>
        IEnumerator RecordFrames(Action<int> perFrame)
        {
            Time.captureFramerate = 60;
            Directory.CreateDirectory(shotPath);
            var audio = new List<float>(48000 * 2 * 13);
            int channels = AudioSettings.speakerMode == AudioSpeakerMode.Mono ? 1 : 2;
            AudioRenderer.Start();
            for (int frame = 0; frame < BossFrames; frame++)
            {
                perFrame(frame);
                yield return new WaitForEndOfFrame();
                Save(Path.Combine(shotPath, "frame_" + frame.ToString("D5") + ".png"));
                int n = AudioRenderer.GetSampleCountForCaptureFrame() * channels;
                var buf = new NativeArray<float>(n, Allocator.Temp);
                AudioRenderer.Render(buf);
                for (int i = 0; i < n; i++) audio.Add(buf[i]);
                buf.Dispose();
            }
            AudioRenderer.Stop();
            WriteWav(Path.Combine(shotPath, "audio.wav"), audio, channels, AudioSettings.outputSampleRate);
            Monster boss = Monster.Boss;
            Debug.Log("[ZenoniaProto] recorded boss fight " + BossFrames + " frames, boss " + (boss != null ? "hp " + boss.Hp : "down"));
            Application.Quit();
        }

        /// <summary>16-bit PCM WAV.</summary>
        static void WriteWav(string path, List<float> samples, int channels, int rate)
        {
            using (var w = new BinaryWriter(File.Create(path)))
            {
                int bytes = samples.Count * 2;
                w.Write(System.Text.Encoding.ASCII.GetBytes("RIFF")); w.Write(36 + bytes);
                w.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt ")); w.Write(16); w.Write((short)1); w.Write((short)channels);
                w.Write(rate); w.Write(rate * channels * 2); w.Write((short)(channels * 2)); w.Write((short)16);
                w.Write(System.Text.Encoding.ASCII.GetBytes("data")); w.Write(bytes);
                foreach (float s in samples) w.Write((short)(Mathf.Clamp(s, -1f, 1f) * 32767f));
            }
        }
    }
}
