using System.Collections;
using System.IO;
using UnityEngine;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// --class C --record-layered-breath --shot-path DIR [--zoom Z]: the idle breath of the layered heroes in the world. For each of the 5 classes,
    /// 8 scripted heroes (one per Dir8 dir, 2 rows of 4, same seed so they breathe in step) stand idle; shots at the breath top and bottom twice
    /// (breath-&lt;class&gt;-N-top|bottom) and "layered-breath: PASS/FAIL" lines: weapon, offhand, fist and hair ride the breath with the body.
    /// The camera is parked on the group (no follow); your own hero waits off to the side. Monsters removed, no AI.
    /// </summary>
    public partial class DebugCapture
    {
        IEnumerator RecordLayeredBreath(HeroController hero)
        {
            Directory.CreateDirectory(shotPath);
            LootDrop.NoDrops = true;
            foreach (Monster m in new System.Collections.Generic.List<Monster>(Monster.All)) Destroy(m.gameObject);
            Vector2 spot = FindOpenSpot();
            hero.God = true;
            hero.Teleport(spot + new Vector2(60f, 0f), 0);
            Camera cam = Camera.main;
            var follow = cam.GetComponent<CameraFollow>();
            if (follow != null) follow.enabled = false;
            cam.orthographicSize = zoom > 0f ? zoom : 34f;
            cam.transform.position = new Vector3(spot.x - 57f, spot.y + 9f, cam.transform.position.z);
            Hud.CaptureClean = true;
            HideCanvases();
            foreach (HeroClass id in RealCrowd)
            {
                var group = new HeroController[8];
                var views = new HeroRenderer[8];
                for (int d = 0; d < 8; d++)
                {
                    Vector2 at = spot + new Vector2(-6f - (d % 4) * 34f, d < 4 ? 18f : -20f);
                    HeroController h = session.SpawnPartyMember(id, at, 0);
                    h.AiHeld = k => false;
                    h.AiPressed = k => false;
                    h.God = true;
                    var look = new CharacterAppearanceData { stored = true, hairStyle = d % 3, hairColorIndex = d % 8, outfitColorIndex = 0, equippedHeadgearId = 0 };
                    h.View.SetAppearance(look, "layered-breath", false, false); // one seed: one breath phase for the group
                    h.FaceDir8(d);
                    group[d] = h;
                    views[d] = h.View;
                }
                yield return new WaitForSeconds(0.8f);
                string cls = HeroClassDef.Get(id).Name.ToLowerInvariant();
                yield return BreathCheck(views, cls, "breath-" + cls, line => Debug.Log("[ZenoniaProto] layered-breath: " + line));
                foreach (HeroController h in group) Destroy(h.gameObject);
                yield return null;
            }
            Application.Quit();
        }
    }
}
