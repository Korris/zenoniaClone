using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Online client test tooling (command line only):
    ///   --net-bot fight|boss|wander   drive the joined hero through InputAdapter, so the real NetInputSender / server path is exercised
    ///                                 (fight = nearest monsters, boss = the Frostfang King first, wander = random walking)
    ///   --record-net S                record S seconds of THIS client's screen in real time (jpg frames + fps.txt + audio.wav in --shot-path),
    ///                                 optional --record-lead L waits L seconds after joining first; encode with ffmpeg afterwards
    /// </summary>
    public partial class DebugCapture
    {
        /// <summary>Waits for the server to place the local hero, then starts the requested bot and / or the recording.</summary>
        IEnumerator RunNetClient()
        {
            string[] a = Environment.GetCommandLineArgs();
            string bot = NetLaunchArgs.NetBot;
            string rec = NetLaunchArgs.Arg(a, "--record-net");
            if (shotTimes != null && NetLaunchArgs.Host == null) { yield return NetShots(); yield break; } // connect screen screenshots
            float t0 = Time.realtimeSinceStartup;
            while ((NetClient.Active == null || NetClient.Active.State != ClientState.Joined || GameSession.Hero == null) && Time.realtimeSinceStartup - t0 < 60f) yield return null;
            if (GameSession.Hero == null) { Debug.LogWarning("[ZenoniaProto] net bot: never joined (" + (NetClient.Active != null ? NetClient.Active.Status : "no client") + ")"); yield break; }
            if (zoom > 0f && Camera.main != null) Camera.main.orthographicSize = zoom;
            Vector2 offset = BotOffset(NetLaunchArgs.Name);
            if (rec != null) StartCoroutine(RecordNet(float.Parse(rec, System.Globalization.CultureInfo.InvariantCulture), NetLaunchArgs.Arg(a, "--record-lead"))); // recording runs alongside the bot
            float botDelay;
            if (float.TryParse(NetLaunchArgs.Arg(a, "--bot-delay"), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out botDelay))
            {
                float until = Time.realtimeSinceStartup + botDelay; // everybody stands at the entrance until the whole party has joined
                while (Time.realtimeSinceStartup < until) yield return null;
            }
            switch (bot)
            {
                case "fight": EnableNavBot(false, offset); break;
                case "boss": EnableNavBot(true, offset); break;
                case "wander": EnableWanderBot(); break;
                case "twitch": EnableTwitchBot(); break;
            }
            Debug.Log("[ZenoniaProto] net bot: " + (bot ?? "off") + " as " + GameSession.Hero.PlayerName + " (hero " + GameSession.Hero.NetId + ")");
        }

        /// <summary>Screenshots at the --shots times without joining anything (connect screen / class select captures).</summary>
        IEnumerator NetShots()
        {
            float start = Time.realtimeSinceStartup;
            for (int i = 0; i < shotTimes.Length; i++)
            {
                while (Time.realtimeSinceStartup - start < shotTimes[i]) yield return null;
                yield return new WaitForEndOfFrame();
                Save(shotPath + "_" + (i + 1) + ".png");
            }
            Application.Quit();
        }

        /// <summary>Stable spot around the target per bot name (so a pack of bots does not stack on one pixel).</summary>
        static Vector2 BotOffset(string name)
        {
            int h = 17;
            foreach (char c in name ?? "") h = h * 31 + c;
            float ang = (h & 1023) / 1024f * Mathf.PI * 2f;
            return new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * 18f;
        }

        void EnableWanderBot()
        {
            int dir = 0;
            float next = 0f;
            InputAdapter.BotHeld = k =>
            {
                if (Time.time >= next) { next = Time.time + 1f + UnityEngine.Random.value * 1.5f; dir = UnityEngine.Random.Range(0, 5); }
                return (dir == 0 && k == GameKey.Up) || (dir == 1 && k == GameKey.Down) || (dir == 2 && k == GameKey.Left) || (dir == 3 && k == GameKey.Right);
            };
            InputAdapter.BotPressed = k => k == GameKey.Attack && dir == 4;
        }

        /// <summary>
        /// Responsiveness demo / input latency probe: runs 0.5 s in one direction, stands 0.7 s (swinging the weapon twice), then turns around.
        /// The felt latency shows as the delay between the key going down and the hero moving.
        /// </summary>
        void EnableTwitchBot()
        {
            float t0 = Time.time;
            const float cycle = 1.2f;
            System.Func<int> phase = () => (int)((Time.time - t0) / cycle);
            InputAdapter.BotHeld = k =>
            {
                float t = (Time.time - t0) % cycle;
                if (t > 0.5f) return false;
                int dirIndex = phase() % 4; // right, up, left, down
                return (dirIndex == 0 && k == GameKey.Right) || (dirIndex == 1 && k == GameKey.Up) || (dirIndex == 2 && k == GameKey.Left) || (dirIndex == 3 && k == GameKey.Down);
            };
            int lastSwing = -1;
            InputAdapter.BotPressed = k =>
            {
                if (k != GameKey.Attack) return false;
                float t = (Time.time - t0) % cycle;
                int slot = t > 0.62f && t < 0.78f ? 1 : t > 0.9f && t < 1.05f ? 2 : 0;
                int id = phase() * 4 + slot;
                if (slot == 0 || id == lastSwing) return false;
                lastSwing = id;
                return true;
            };
        }

        /// <summary>Real-time capture at ~30 fps: this is a live network client, so frames follow the wall clock (fps.txt holds the achieved rate).</summary>
        IEnumerator RecordNet(float seconds, string lead)
        {
            Directory.CreateDirectory(shotPath);
            Hud.CaptureClean = true;
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 30;
            float leadS = 0f;
            if (lead != null) float.TryParse(lead, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out leadS);
            float wait = Time.realtimeSinceStartup + leadS;
            while (Time.realtimeSinceStartup < wait) yield return null;
            AudioTap tap = Camera.main.gameObject.AddComponent<AudioTap>();
            tap.Begin();
            float start = Time.realtimeSinceStartup;
            int frame = 0;
            while (Time.realtimeSinceStartup - start < seconds)
            {
                yield return new WaitForEndOfFrame();
                var tex = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
                tex.Apply();
                File.WriteAllBytes(Path.Combine(shotPath, "frame_" + frame.ToString("D5") + ".jpg"), tex.EncodeToJPG(92));
                Destroy(tex);
                frame++;
            }
            float elapsed = Time.realtimeSinceStartup - start;
            List<float> samples = tap.End();
            File.WriteAllText(Path.Combine(shotPath, "fps.txt"), (frame / Mathf.Max(0.1f, elapsed)).ToString("F3", System.Globalization.CultureInfo.InvariantCulture));
            WriteWav(Path.Combine(shotPath, "audio.wav"), samples, tap.Channels, AudioSettings.outputSampleRate);
            Debug.Log("[ZenoniaProto] recorded online client: " + frame + " frames in " + elapsed.ToString("F1") + " s, heroes=" + Zone.Main.Heroes.Count + " monsters=" + Monster.All.Count);
            Application.Quit();
        }
    }

    /// <summary>Copies the mixed game audio of the listener while recording (real time, no fixed frame rate).</summary>
    public class AudioTap : MonoBehaviour
    {
        readonly List<float> samples = new List<float>(48000 * 2 * 20);
        readonly object gate = new object();
        volatile bool on;
        public int Channels { get; private set; } = 2;

        public void Begin() { on = true; }

        public List<float> End()
        {
            on = false;
            lock (gate) return new List<float>(samples);
        }

        void OnAudioFilterRead(float[] data, int channels)
        {
            if (!on) return;
            Channels = channels;
            lock (gate) samples.AddRange(data);
        }
    }
}
