using System.Collections;
using System.IO;
using UnityEngine;

namespace ZenoniaProto
{
    public partial class DebugCapture
    {
        string comboArg, walkArg;

        /// <summary>Fixed 60 fps scripted scene for the walk cycle (--record-walk CLASS --face DIR): the hero holds the move key of that
        /// facing from frame 30 for 3 s, one PNG per frame (Tools/wizard-combo-ingame-frame-strip.py style strips; the camera keeps the hero centred).</summary>
        IEnumerator RecordWalk(string who)
        {
            Time.captureFramerate = 60;
            Directory.CreateDirectory(shotPath);
            session.Spawn(ParseClass(who));
            yield return null;
            HeroController hero = GameSession.Hero;
            hero.God = true;
            if (hairStyle >= 0) hero.View.HairStyle = hairStyle;
            int face = faceArg == "down" ? 0 : faceArg == "up" ? 1 : faceArg == "right" ? 3 : 2;
            hero.Teleport(tpTile.x >= 0 ? FrostHollowMap.TileFeet(tpTile.x, tpTile.y, 2f) : FrostHollowMap.TileFeet(40, 30, 2f), face);
            Camera.main.orthographicSize = zoom > 0f ? zoom : 30f;
            GameKey key = face == 0 ? GameKey.Down : face == 1 ? GameKey.Up : face == 3 ? GameKey.Right : GameKey.Left;
            int frame = 0, walkFrom = 30, endAt = 30 + 180;
            InputAdapter.BotHeld = k => k == key && frame >= walkFrom;
            for (; frame < endAt; frame++)
            {
                yield return new WaitForEndOfFrame();
                Save(Path.Combine(shotPath, "frame_" + frame.ToString("D5") + ".png"));
            }
            Debug.Log("[ZenoniaProto] recorded walk " + frame + " frames");
            Application.Quit();
        }

        /// <summary>Fixed 60 fps scripted scene for basic-attack FX: hero faces west holding attack at dummies that are re-spawned every 1.5 s. One PNG per frame.</summary>
        IEnumerator RecordCombo(string who)
        {
            Time.captureFramerate = 60;
            Directory.CreateDirectory(shotPath);
            session.Spawn(ParseClass(who));
            yield return null;
            HeroController hero = GameSession.Hero;
            hero.God = true;
            if (hairStyle >= 0) hero.View.HairStyle = hairStyle; // --hairstyle: attack anims draw the head layer too
            int face = faceArg == "down" ? 0 : faceArg == "up" ? 1 : faceArg == "right" ? 3 : 2; // --face, west when absent
            hero.Teleport(tpTile.x >= 0 ? FrostHollowMap.TileFeet(tpTile.x, tpTile.y, 2f) : FrostHollowMap.TileFeet(11, 6, 2f), face); // --tp tile
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
