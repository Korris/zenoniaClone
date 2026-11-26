using System.Collections;
using System.IO;
using UnityEngine;

namespace ZenoniaProto
{
    public partial class DebugCapture
    {
        /// <summary>
        /// --record-rig --class warrior --shot-path DIR [--hero-rig | --hero-rig-mesh] [--zoom N]: fixed 60 fps scene facing the camera for the bone-rig TEST
        /// (HeroRenderer.Rig.cs): 120 frames idle, 90 frames walking down, then 150 frames holding attack at a slime placed below the hero
        /// (re-spawned when it dies). Same scene with and without --hero-rig = rig vs frame art. HUD hidden. One PNG per frame, then quits.
        /// </summary>
        IEnumerator RecordRig()
        {
            Time.captureFramerate = 60;
            Directory.CreateDirectory(shotPath);
            Hud.CaptureClean = true;
            session.Spawn(classArg != null ? ParseClass(classArg) : HeroClass.Warrior);
            yield return null;
            HeroController hero = GameSession.Hero;
            hero.God = true;
            if (hairStyle >= 0) hero.View.HairStyle = hairStyle;
            HideCanvases();
            LootDrop.NoDrops = true;
            foreach (Monster m in new System.Collections.Generic.List<Monster>(Monster.All)) Destroy(m.gameObject);
            Vector2 spot = FindOpenSpot();
            hero.Teleport(spot, 0);
            Camera.main.orthographicSize = zoom > 0f ? zoom : 35f;
            int frame = 0;
            const int IdleEnd = 120, WalkEnd = 210, End = 360;
            InputAdapter.BotHeld = k => k == GameKey.Down && frame >= IdleEnd && frame < WalkEnd;
            Transform root = GameObject.Find("ZenoniaProto").transform;
            for (; frame < End; frame++)
            {
                if (frame == WalkEnd) { InputAdapter.BotHeld = null; hero.Teleport(spot, 0); }
                if (frame >= WalkEnd + 10)
                {
                    bool alive = false;
                    foreach (Monster m in Monster.All) if (m != null && !m.IsDying) alive = true;
                    if (!alive) Monster.Create(root, MonsterDef.FrostSlime, hero.Position + new Vector2(0f, -(dummyDist > 0f ? dummyDist : 20f)), -1);
                    InputAdapter.BotPressed = k => k == GameKey.Attack; // pressed every frame = held attack, as RecordCompare
                }
                yield return new WaitForEndOfFrame();
                Save(Path.Combine(shotPath, "frame_" + frame.ToString("D5") + ".png"));
            }
            InputAdapter.BotHeld = null;
            InputAdapter.BotPressed = null;
            Debug.Log("[ZenoniaProto] recorded rig scene " + frame + " frames, rig " + HeroRenderer.RigEnabled + ", mesh rig " + HeroRenderer.RigMeshEnabled);
            Application.Quit();
        }
    }
}
