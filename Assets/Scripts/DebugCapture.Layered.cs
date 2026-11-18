using System.Collections;
using System.IO;
using UnityEngine;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// --class warrior --record-layered --shot-path DIR [--zoom Z]: the layered path with the synthetic test set (HeroRenderer.Layered.cs).
    /// Your Warrior (RO Soft sheets, unchanged) in the middle of 8 layered test heroes: hair colour 0-7, outfit colour i % 6, a hat on every
    /// third, each facing another Dir8 dir. Shots: layered-idle-dirs (varied dirs), layered-idle-same (all down-right, to compare colours),
    /// layered-walk-N (all walking down-left), layered-attack-N (all attacking). The heroes are scripted (no AI), monsters are removed.
    /// </summary>
    public partial class DebugCapture
    {
        IEnumerator RecordLayered(HeroController hero)
        {
            HeroRenderer.LayeredTestEnabled = true;
            Directory.CreateDirectory(shotPath);
            Hud.CaptureClean = true;
            HideCanvases();
            LootDrop.NoDrops = true;
            foreach (Monster m in new System.Collections.Generic.List<Monster>(Monster.All)) Destroy(m.gameObject);
            Vector2 spot = FindOpenSpot();
            hero.God = true;
            hero.Teleport(spot + new Vector2(-62f, 0f), 0); // the camera follows it: the crowd stands both sides
            hero.FaceDir8(1);
            var crowd = new HeroController[8];
            for (int i = 0; i < crowd.Length; i++)
            {
                Vector2 at = spot + new Vector2(new[] { -10f, -34f, -90f, -114f }[i % 4], i < 4 ? 14f : -16f);
                HeroController h = session.SpawnPartyMember(HeroClass.Warrior, at, 0);
                h.AiHeld = k => false;
                h.AiPressed = k => false;
                h.God = true;
                var look = new CharacterAppearanceData { stored = true, hairColorIndex = i, outfitColorIndex = i % CharacterAppearanceData.OutfitColorCount, equippedHeadgearId = i % 3 == 2 ? 1 : 0 };
                h.View.SetAppearance(look, "layered-capture-" + i, false, false);
                h.FaceDir8(i);
                crowd[i] = h;
                Debug.Log("[ZenoniaProto] layered test hero " + i + ": layered " + h.View.IsLayered + ", hair " + i + ", outfit " + look.outfitColorIndex + ", hat " + look.equippedHeadgearId);
            }
            if (Camera.main != null) Camera.main.orthographicSize = zoom > 0f ? zoom : 48f;
            yield return new WaitForSeconds(1f);
            yield return Shot("layered-idle-dirs");
            foreach (HeroController h in crowd) h.FaceDir8(1);
            yield return new WaitForSeconds(0.3f);
            yield return Shot("layered-idle-same");
            foreach (HeroController h in crowd) h.AiHeld = k => k == GameKey.Down || k == GameKey.Left;
            for (int s = 0; s < 4; s++) { yield return new WaitForSeconds(0.12f); yield return Shot("layered-walk-" + s); }
            foreach (HeroController h in crowd) { h.AiHeld = k => false; h.AiPressed = k => k == GameKey.Attack; }
            for (int s = 0; s < 6; s++) { yield return new WaitForSeconds(0.08f); yield return Shot("layered-attack-" + s); }
            Application.Quit();
        }

        IEnumerator Shot(string name)
        {
            yield return new WaitForEndOfFrame();
            Save(Path.Combine(shotPath, name + ".png"));
            Debug.Log("[ZenoniaProto] layered shot " + name);
        }
    }
}
