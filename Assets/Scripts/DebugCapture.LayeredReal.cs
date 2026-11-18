using System.Collections;
using System.IO;
using UnityEngine;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// --class C --record-layered-real --shot-path DIR [--zoom Z]: the real layered art (step D, default hero style). Your hero in the middle of
    /// 10 scripted heroes, 2 per class (Warrior, Wizard, Mechanic: male body; Assassin, Cleric: female), each with its own palette colours.
    /// Shots: real-idle-dirs (every hero another Dir8 dir), real-idle-down-right, real-walk-N (down-left), real-attack-N, real-walk-right-N,
    /// real-hud (HUD kept: own ring + green name). Monsters removed, no AI.
    /// </summary>
    public partial class DebugCapture
    {
        static readonly HeroClass[] RealCrowd = { HeroClass.Warrior, HeroClass.Wizard, HeroClass.Mechanic, HeroClass.Assassin, HeroClass.Cleric };

        IEnumerator RecordLayeredReal(HeroController hero)
        {
            Directory.CreateDirectory(shotPath);
            LootDrop.NoDrops = true;
            foreach (Monster m in new System.Collections.Generic.List<Monster>(Monster.All)) Destroy(m.gameObject);
            Vector2 spot = FindOpenSpot();
            hero.God = true;
            hero.Teleport(spot + new Vector2(-62f, -2f), 0);
            hero.FaceDir8(0);
            var crowd = new HeroController[10];
            for (int i = 0; i < crowd.Length; i++)
            {
                Vector2 at = spot + new Vector2(new[] { -2f, -24f, -100f, -122f, -144f }[i % 5], i < 5 ? 18f : -20f);
                if (i % 5 == 0) at.x = spot.x + 20f;
                HeroController h = session.SpawnPartyMember(RealCrowd[i % 5], at, 0);
                h.AiHeld = k => false;
                h.AiPressed = k => false;
                h.God = true;
                var look = new CharacterAppearanceData { stored = true, hairColorIndex = (i * 3) % 8, outfitColorIndex = (i * 5 + 1) % CharacterAppearanceData.OutfitColorCount,
                    equippedHeadgearId = i % 5 == 4 ? 0 : new[] { 2, 5, 16, 3, 9, 10, 6, 1, 12, 19 }[i] }; // hats from art batch 3 on most (5 nun veil, 9 aviator: hair hidden)
                h.View.SetAppearance(look, "layered-real-" + i, false, false);
                h.FaceDir8(i % 8);
                crowd[i] = h;
                Debug.Log("[ZenoniaProto] layered real hero " + i + " " + RealCrowd[i % 5] + ": layered " + h.View.IsLayered);
            }
            Debug.Log("[ZenoniaProto] layered real own hero: layered " + hero.View.IsLayered);
            if (Camera.main != null) Camera.main.orthographicSize = zoom > 0f ? zoom : 52f;
            yield return new WaitForSeconds(1f);
            yield return Shot("real-hud");
            Hud.CaptureClean = true;
            HideCanvases();
            yield return new WaitForSeconds(0.2f);
            yield return Shot("real-idle-dirs");
            foreach (HeroController h in crowd) h.FaceDir8(1);
            yield return new WaitForSeconds(0.3f);
            yield return Shot("real-idle-down-right");
            foreach (HeroController h in crowd) h.AiHeld = k => k == GameKey.Down || k == GameKey.Left;
            for (int s = 0; s < 4; s++) { yield return new WaitForSeconds(0.12f); yield return Shot("real-walk-" + s); }
            foreach (HeroController h in crowd) h.AiHeld = k => k == GameKey.Right;
            for (int s = 0; s < 3; s++) { yield return new WaitForSeconds(0.12f); yield return Shot("real-walk-right-" + s); }
            foreach (HeroController h in crowd) { h.AiHeld = k => false; h.AiPressed = k => k == GameKey.Attack; }
            for (int s = 0; s < 8; s++) { yield return new WaitForSeconds(0.09f); yield return Shot("real-attack-" + s); }
            foreach (HeroController h in crowd) { h.AiPressed = k => false; h.AiHeld = k => k == GameKey.Up; }
            for (int s = 0; s < 2; s++) { yield return new WaitForSeconds(0.12f); yield return Shot("real-walk-up-" + s); }
            Application.Quit();
        }
    }
}
