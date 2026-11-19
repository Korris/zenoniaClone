using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ZenoniaProto.Net;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// --persist-role wanted-showcase --wanted-shots DIR (Tools/wanted-boss-playtest.ps1 -Role wanted-showcase, start the hero OUT of aggro range with
    /// -StartDx): timed screenshots of every motion and skill of the daily Wanted Boss, for checking new boss art / FX by eye (Mire Hydra Queen v2, 2026-11-19).
    ///  1 idle: the hero waits out of reach, 10 shots 120 ms apart while the boss breathes at its post;
    ///  2 walk: the hero steps just inside aggro range and stands; shots while the boss slithers in;
    ///  3 attack: the hero stands still in reach; shots while the boss strikes;
    ///  4 skills: the hero fights (nav bot); every telegraph gets a series at fixed points of ITS OWN countdown (90 / 60 / 30 / 5 % left) and
    ///    after the strike (+0.1 / +0.3 / +0.55 / +0.85 s), until each of the 4 skills was shot once (or 240 s). Names say skill id + time.
    /// </summary>
    public partial class DebugCapture
    {
        IEnumerator WantedShowcase()
        {
            string dir = NetLaunchArgs.Arg(Environment.GetCommandLineArgs(), "--wanted-shots") ?? "shots";
            System.IO.Directory.CreateDirectory(dir);
            ChatWindow.SetExpandedState(false);
            float start = Time.realtimeSinceStartup;
            while (Monster.Wanted == null && Time.realtimeSinceStartup - start < 20f) yield return null;
            Monster boss = Monster.Wanted;
            P((boss != null ? "PASS" : "FAIL") + " showcase: wanted boss in sight");
            if (boss == null) { Application.Quit(); yield break; }
            yield return new WaitForSeconds(2f);
            for (int k = 0; k < 10; k++) { yield return Shot(dir, "a-idle-" + k.ToString("00")); yield return new WaitForSeconds(0.12f); }

            // walk: step until the boss is 95 units away (inside its 110 aggro), then stand and let it come
            Vector2 move = Vector2.zero;
            InputAdapter.BotHeld = key => (key == GameKey.Left && move.x < 0f) || (key == GameKey.Right && move.x > 0f) || (key == GameKey.Down && move.y < 0f) || (key == GameKey.Up && move.y > 0f);
            InputAdapter.BotPressed = key => false;
            start = Time.realtimeSinceStartup;
            while (boss != null && Vector2.Distance(GameSession.Hero.Position, boss.Feet) > 95f && Time.realtimeSinceStartup - start < 15f) { move = StepToward(GameSession.Hero.Position, boss.Feet); yield return null; }
            move = Vector2.zero;
            for (int k = 0; k < 16; k++) { yield return Shot(dir, "b-walk-" + k.ToString("00")); yield return new WaitForSeconds(0.15f); }
            for (int k = 0; k < 20; k++) { yield return Shot(dir, "c-attack-" + k.ToString("00")); yield return new WaitForSeconds(0.12f); }

            // skills: fight, shoot each telegraph on its own countdown
            EnableNavBot(true, BotOffset(GameSession.Hero.PlayerName), false, false, true);
            var seen = new HashSet<int>();
            float[] before = { 0.9f, 0.6f, 0.3f, 0.05f }, after = { 0.1f, 0.3f, 0.55f, 0.85f };
            start = Time.realtimeSinceStartup;
            while (seen.Count < 4 && Time.realtimeSinceStartup - start < 240f)
            {
                boss = Monster.Wanted;
                WantedSkillDef skill = boss != null && !boss.IsDying ? boss.WantedTelegraphSkill : null;
                float remain;
                if (skill == null || seen.Contains(skill.Id) || !WantedSkillTelegraphFx.StrikeIn(boss.WantedBossId, skill, out remain)) { yield return null; continue; }
                seen.Add(skill.Id);
                P("showcase: skill " + skill.Id + " " + skill.Name + " telegraph " + skill.TelegraphMs + " ms, " + remain.ToString("F0") + " ms left when seen");
                string tag = "d-skill" + skill.Id + "-";
                uint at = (uint)Mathf.RoundToInt(NetClient.ServerNowMs + remain);
                foreach (float f in before)
                {
                    while (at - NetClient.ServerNowMs > skill.TelegraphMs * f) yield return null;
                    yield return Shot(dir, tag + "m" + Mathf.RoundToInt(f * skill.TelegraphMs).ToString("0000"));
                }
                foreach (float s in after)
                {
                    while (NetClient.ServerNowMs - at < s * 1000f) yield return null;
                    yield return Shot(dir, tag + "p" + Mathf.RoundToInt(s * 1000f).ToString("0000"));
                }
            }
            P((seen.Count == 4 ? "PASS" : "FAIL") + " showcase: shot " + seen.Count + " / 4 skills");
            InputAdapter.BotHeld = key => false; InputAdapter.BotPressed = key => false;
            yield return new WaitForSeconds(1f);
            Application.Quit();
        }
    }
}
