using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    public partial class DebugCapture
    {
        // Isolated live integration check; the capture framework disables player saves.
        IEnumerator RunPowerSlashCheck(HeroController hero)
        {
            hero.God = true;
            foreach (Monster old in new List<Monster>(Monster.All)) Destroy(old.gameObject);
            yield return null;
            Vector2 origin = hero.Position;
            // Dummies must start on clear ground: the normal capture tile can sit beside stairs/walls.
            bool foundArena = false;
            IGameMap map = MapRegistry.Current;
            for (int ty = 4; ty < map.Height - 4 && !foundArena; ty++)
                for (int tx = 4; tx < map.Width - 4; tx++)
                {
                    Vector2 candidate = FrostHollowMap.TileFeet(tx, ty, 2f);
                    if (map.RectBlocked(candidate.x - 56f, candidate.y - 56f, 112f, 112f)) continue;
                    origin = candidate; foundArena = true; break;
                }
            if (!foundArena) { Debug.LogError("POWER SLASH CHECK: no clear test arena"); Application.Quit(1); yield break; }
            int testFacing = faceArg == "up" ? 1 : faceArg == "left" ? 2 : faceArg == "right" ? 3 : 0;
            hero.Teleport(origin, testFacing);
            Vector2 dir = HeroSkillBody.DirOf(hero.Facing), side = new Vector2(-dir.y, dir.x);
            var normal = new MonsterDef("Gather dummy", "frostSlime", 5000f, 0f, 0f, 0f, 1, aggro: 0f);
            var heavy = new MonsterDef("Heavy dummy", "frostSlime", 5000f, 0f, 0f, 0f, 1, aggro: 0f) { IsBoss = true };
            Vector2[] centers = { origin + dir * 38f + side * 20f, origin + dir * 38f - side * 20f, origin + dir * 10f + side * 32f };
            var targets = new List<Monster>();
            foreach (Vector2 center in centers) targets.Add(Monster.Create(hero.transform.parent, normal, center - Vector2.up * 6f, -1));
            Monster rear = Monster.Create(hero.transform.parent, normal, origin - dir * 30f - Vector2.up * 6f, -1);
            Monster boss = Monster.Create(hero.transform.parent, heavy, origin + dir * 35f - Vector2.up * 6f, -1);
            Vector2 bossBefore = boss.Center;
            bool cast = hero.TryCastSkill(0), damaged = true;
            foreach (Monster m in targets) damaged &= m.Hp < m.MaxHp;
            bool rearSafe = rear.Hp == rear.MaxHp, bossHit = boss.Hp < boss.MaxHp;
            float simMs = 0f;
            while (simMs < 135f) { yield return null; simMs += GameClock.Dt * 1000f; }
            Vector2 focus = origin + dir * 20f;
            bool gathered = true;
            for (int i = 0; i < targets.Count; i++)
            {
                float moved = Vector2.Distance(centers[i], targets[i].Center);
                bool closer = Vector2.Distance(targets[i].Center, focus) < Vector2.Distance(centers[i], focus);
                gathered &= moved <= 12.5f && closer;
                Debug.Log("POWER SLASH target " + i + " moved=" + moved + " closer=" + closer);
            }
            bool bossStill = Vector2.Distance(bossBefore, boss.Center) < 0.1f;
            bool pass = cast && damaged && gathered && rearSafe && bossHit && bossStill;
            Debug.Log("POWER SLASH CHECK: " + (pass ? "PASS" : "FAIL") + " cast=" + cast + " multiHit=" + damaged + " gathered=" + gathered + " rearSafe=" + rearSafe + " bossHit=" + bossHit + " bossStill=" + bossStill);
            if (!string.IsNullOrEmpty(shotPath)) { yield return new WaitForEndOfFrame(); Save(shotPath + "_gather.png"); }
            Application.Quit(pass ? 0 : 1);
        }
    }
}
