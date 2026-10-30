using System.Collections;
using System.IO;
using UnityEngine;

namespace ZenoniaProto
{
    public partial class DebugCapture
    {
        string comboArg;

        /// <summary>Fixed 60 fps scripted scene for basic-attack FX: hero faces west holding attack at dummies that are re-spawned every 1.5 s. One PNG per frame.</summary>
        IEnumerator RecordCombo(string who)
        {
            Time.captureFramerate = 60;
            Directory.CreateDirectory(shotPath);
            session.Spawn(ParseClass(who));
            yield return null;
            HeroController hero = GameSession.Hero;
            hero.God = true;
            hero.Teleport(FrostHollowMap.TileFeet(11, 6, 2f), 2);
            if (zoom > 0f) Camera.main.orthographicSize = zoom;
            if (dummies <= 0) dummies = 2;
            if (zoom <= 0f) { zoom = 55f; Camera.main.orthographicSize = zoom; }
            if (dummyDist == 30f) dummyDist = 85f; // far enough that the shot is visible in flight
            int frame = 0, attackFrom = 30, endAt = 30 + 420;
            InputAdapter.BotPressed = k => k == GameKey.Attack && frame >= attackFrom;
            for (; frame < endAt; frame++)
            {
                if (frame >= attackFrom - 10 && (frame - attackFrom + 10) % 90 == 0) SpawnDummies(hero); // fresh targets (monsters die and respawn far away)
                yield return new WaitForEndOfFrame();
                Save(Path.Combine(shotPath, "frame_" + frame.ToString("D5") + ".png"));
            }
            Debug.Log("[ZenoniaProto] recorded combo " + frame + " frames");
            Application.Quit();
        }
    }
}
