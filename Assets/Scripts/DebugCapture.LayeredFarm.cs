using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// --class C --record-layered-farm --shot-path DIR [--zoom Z]: a farming crowd for the layered heroes. 10 scripted heroes (2 per class,
    /// every one its own hair / outfit colour, most with a hat) stand in a ring around a pack of Frost Slimes / Ice Goblins, each facing the
    /// pack with its own Dir8 direction, and keep attacking; dead monsters are replaced so the fight goes on. Your hero joins the ring.
    /// Shots: farm-hud (HUD on: own ring + green name), farm-0..N (HUD hidden), then the app quits.
    /// </summary>
    public partial class DebugCapture
    {
        const int FarmHeroes = 10, FarmMonsters = 6;

        IEnumerator RecordLayeredFarm(HeroController hero)
        {
            Directory.CreateDirectory(shotPath);
            LootDrop.NoDrops = true;
            foreach (Monster m in new List<Monster>(Monster.All)) Destroy(m.gameObject);
            Vector2 centre = FindOpenSpot();
            Transform root = GameObject.Find("ZenoniaProto").transform;
            var heroes = new List<HeroController> { hero };
            hero.God = true;
            for (int i = 0; i < FarmHeroes; i++)
            {
                HeroController h = session.SpawnPartyMember(RealCrowd[i % 5], centre, 0);
                h.AiHeld = k => false;
                h.AiPressed = k => false;
                h.God = true;
                var look = new CharacterAppearanceData { stored = true, hairColorIndex = (i * 3 + 1) % 8, outfitColorIndex = (i * 5 + 2) % CharacterAppearanceData.OutfitColorCount,
                    equippedHeadgearId = i % 4 == 3 ? 0 : new[] { 2, 5, 16, 0, 9, 10, 6, 0, 12, 19 }[i] };
                h.View.SetAppearance(look, "layered-farm-" + i, false, false);
                heroes.Add(h);
            }
            // a ring around the pack (wider than tall: the camera looks down at an angle), everyone facing the centre
            for (int i = 0; i < heroes.Count; i++)
            {
                float t = Mathf.PI * 2f * i / heroes.Count + 0.2f;
                Vector2 at = centre + new Vector2(Mathf.Cos(t) * 46f, Mathf.Sin(t) * 30f);
                heroes[i].Teleport(at, 0);
                heroes[i].FaceDir8(Dir8Util.FromVector(centre - at));
            }
            SpawnFarmPack(root, centre);
            if (Camera.main != null) Camera.main.orthographicSize = zoom > 0f ? zoom : 50f;
            yield return new WaitForSeconds(0.8f);
            yield return Shot("farm-hud");
            Hud.CaptureClean = true;
            HideCanvases();
            foreach (HeroController h in heroes) if (h != hero) h.AiPressed = k => k == GameKey.Attack;
            InputAdapter.BotPressed = k => k == GameKey.Attack;
            for (int s = 0; s < 12; s++)
            {
                if (Monster.All.Count < FarmMonsters / 2) SpawnFarmPack(root, centre);
                for (int i = 0; i < heroes.Count; i++) heroes[i].FaceDir8(Dir8Util.FromVector(centre - heroes[i].Position)); // keep the ring facing in
                yield return new WaitForSeconds(0.11f);
                yield return Shot("farm-" + s);
            }
            InputAdapter.BotPressed = k => false;
            Application.Quit();
        }

        static void SpawnFarmPack(Transform root, Vector2 centre)
        {
            for (int i = Monster.All.Count; i < FarmMonsters; i++)
            {
                float t = Mathf.PI * 2f * i / FarmMonsters;
                Monster.Create(root, i % 3 == 2 ? MonsterDef.IceGoblin : MonsterDef.FrostSlime, centre + new Vector2(Mathf.Cos(t) * 12f, Mathf.Sin(t) * 8f), -1);
            }
        }
    }
}
