using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.Collections;
using UnityEngine;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// --record-compare attack|skills --shot-path DIR: the same fixed scene for comparing Warrior art between two builds, wearing the starter weapon and shield at +15.
    /// --class picks the hero (default Warrior). dash = a dash left, right, down, up; attack = the combo facing left, right, down; skills = Earth Break, then Inferno Crash, facing left. 60 fps fixed step: frame_#####.png + audio.wav.
    /// </summary>
    public partial class DebugCapture
    {
        public const int CompareAttackSegment = 216, CompareAttackHold = 170;
        string compareArg;

        IEnumerator RecordCompare(string what)
        {
            Time.captureFramerate = 60;
            Directory.CreateDirectory(shotPath);
            Hud.CaptureClean = true;
            session.Spawn(classArg != null ? ParseClass(classArg) : HeroClass.Warrior);
            yield return null;
            HeroController hero = GameSession.Hero;
            hero.God = true;
            PlayerState p = hero.Rpg;
            foreach (string slot in new[] { Slots.Weapon, Slots.Offhand }) { Item it = p.GetEquipped(slot); if (it != null) it.enhance = 15; }
            p.RecomputeStats();
            RpgSession.SyncGear();
            Debug.Log("[ZenoniaProto] compare gear: tier " + ItemInfo.ArtTier(p.GetEquipped(Slots.Weapon)) + " +" + p.GetEquipped(Slots.Weapon).enhance);
            HideCanvases();
            Vector2 spot = FindOpenSpot();
            var audio = new List<float>(48000 * 2 * 30);
            int channels = AudioSettings.speakerMode == AudioSpeakerMode.Mono ? 1 : 2, frame = 0;
            AudioRenderer.Start();
            System.Action capture = () =>
            {
                Save(Path.Combine(shotPath, "frame_" + frame.ToString("D5") + ".png"));
                int n = AudioRenderer.GetSampleCountForCaptureFrame() * channels;
                var buf = new NativeArray<float>(n, Allocator.Temp);
                AudioRenderer.Render(buf);
                for (int i = 0; i < n; i++) audio.Add(buf[i]);
                buf.Dispose();
            };
            if (what == "dash")
            {
                Camera.main.orthographicSize = zoom > 0f ? zoom : 42f;
                foreach (GameKey key in new[] { GameKey.Left, GameKey.Right, GameKey.Down, GameKey.Up })
                {
                    hero.Teleport(spot + new Vector2(key == GameKey.Left ? 30f : key == GameKey.Right ? -30f : 0f, key == GameKey.Up ? -30f : key == GameKey.Down ? 30f : 0f), 0);
                    int local = 0;
                    InputAdapter.BotHeld = k => k == key && local >= 16 && local < 40;
                    InputAdapter.BotPressed = k => k == GameKey.Dash && local == 18;
                    for (; local < 70; local++, frame++)
                    {
                        if (local % 20 == 0) foreach (Monster m in new List<Monster>(Monster.All)) Destroy(m.gameObject);
                        yield return new WaitForEndOfFrame();
                        capture();
                    }
                }
                InputAdapter.BotHeld = null;
                InputAdapter.BotPressed = null;
            }
            else if (what == "attack")
            {
                Camera.main.orthographicSize = zoom > 0f ? zoom : 42f;
                foreach (int dir in new[] { 2, 3, 0 })
                {
                    hero.Teleport(spot, dir);
                    int local = 0;
                    InputAdapter.BotPressed = k => k == GameKey.Attack && local >= 20 && local < CompareAttackHold;
                    for (; local < CompareAttackSegment; local++, frame++)
                    {
                        if (local % 20 == 0) foreach (Monster m in new List<Monster>(Monster.All)) Destroy(m.gameObject); // nothing in the shot, and nothing to turn towards
                        yield return new WaitForEndOfFrame();
                        capture();
                    }
                }
                InputAdapter.BotPressed = null;
            }
            else
            {
                Camera.main.orthographicSize = zoom > 0f ? zoom : 62f;
                foreach (string skill in new[] { "Earth Break", "Inferno Crash" })
                {
                    foreach (Monster m in new List<Monster>(Monster.All)) Destroy(m.gameObject);
                    hero.Teleport(spot, 2);
                    foreach (ChoreoShot s in ChoreoShots) if (s.Skill == skill) PlaceGroup(hero, s);
                    yield return null;
                    for (int local = 0; local < 300; local++, frame++)
                    {
                        if (local == 30) Debug.Log("[ZenoniaProto] compare cast " + skill + " => " + CastByName(hero, skill));
                        yield return new WaitForEndOfFrame();
                        capture();
                    }
                }
            }
            AudioRenderer.Stop();
            WriteWav(Path.Combine(shotPath, "audio.wav"), audio, channels, AudioSettings.outputSampleRate);
            Debug.Log("[ZenoniaProto] recorded compare " + frame + " frames");
            Application.Quit();
        }
    }
}
