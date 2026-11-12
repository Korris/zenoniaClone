using System;
using System.Collections;
using System.Globalization;
using UnityEngine;
using ZenoniaProto.Net;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// Wanted Boss test bots (Tools/wanted-boss-test.ps1), roles of --net-bot persist (real account, real zone). Every expectation is a "persist-e2e:" line; the script judges the numbers from the API.
    ///   --persist-role wanted-kill [--wanted-expect claim|none] [--wanted-target wanted|map] [--wanted-respawn-check] [--wanted-shots DIR]   stands near the boss (the script seeds the position), kills it (target map = the map boss, which must pay nothing), reports gold / stones / potions / WantedDay before and after,
    ///                                                                                  and checks the arrival chat line and (claim) the "defeated" chat line. --wanted-shots saves PNGs of the chat after joining and of the toast after the claim.
    ///   --persist-role wanted-board [--wanted-shots DIR]   walks to Guard Rook, talks to him (TownNpc.Talk, the E key's call) and saves a PNG of each board page.
    /// </summary>
    public partial class DebugCapture
    {
        static bool ChatHas(string text)
        {
            foreach (ChatMessage m in ChatHub.History) if (m.Text != null && m.Text.Contains(text)) return true;
            return false;
        }

        static string WantedLine(string tag)
        {
            PlayerState me = Me;
            return tag + " day=" + me.WantedDay + " gold=" + me.Gold + " stones=" + me.UpgradeStone + " hp=" + me.HpPotions + " mp=" + me.MpPotions + " level=" + me.Level;
        }

        IEnumerator WantedKill()
        {
            string[] a = Environment.GetCommandLineArgs();
            string shots = NetLaunchArgs.Arg(a, "--wanted-shots");
            bool mapBoss = NetLaunchArgs.Arg(a, "--wanted-target") == "map"; // kill the map's own boss instead of the event boss: it must pay nothing
            bool expectClaim = NetLaunchArgs.Arg(a, "--wanted-expect") != "none";
            bool watchRespawn = NetLaunchArgs.HasFlag("--wanted-respawn-check");
            int day0 = Me.WantedDay;
            P(WantedLine("wanted-before"));
            if (NetMode.RendersFx) P((ChatHas("Wanted today:") || ChatHas("Wanted reward claimed today") ? "PASS" : "FAIL") + " the arrival chat line is there"); // a headless client has no chat
            if (shots != null)
            {
                System.IO.Directory.CreateDirectory(shots);
                ChatWindow.SetExpandedState(true);
                yield return new WaitForSeconds(1.5f);
                yield return Shot(shots, "wanted-chat-arrival");
            }
            EnableNavBot(true, BotOffset(GameSession.Hero.PlayerName), false, false, !mapBoss);
            float start = Time.realtimeSinceStartup;
            bool sawBoss = false, killed = false, fightShot = false, heroDied = false;
            float lastHpPct = 1f;
            Monster target = null;
            float nextLog = start + 10f;
            while (!killed && Time.realtimeSinceStartup - start < 300f)
            {
                Monster boss = mapBoss ? Monster.Boss : Monster.Wanted;
                if (boss != null && !boss.IsDying)
                {
                    if (!sawBoss) P("boss in sight: " + boss.DisplayName + " hp " + Mathf.CeilToInt(boss.Hp) + " / " + (int)boss.MaxHp + " after " + (Time.realtimeSinceStartup - start).ToString("F0", CultureInfo.InvariantCulture) + " s");
                    sawBoss = true; target = boss; lastHpPct = boss.Hp / boss.MaxHp;
                    if (shots != null && !fightShot && boss.Hp < boss.MaxHp * 0.7f && Hud.BossBarVisible()) { fightShot = true; yield return Shot(shots, "wanted-boss-fight"); }
                }
                else if (sawBoss) { killed = lastHpPct < 0.3f; if (!killed) break; } // gone while healthy = our hero died and was moved away, not a kill
                if (Time.realtimeSinceStartup > nextLog) { nextLog += 10f; P("fight: boss " + (target != null ? Mathf.CeilToInt(target.Hp) + " / " + (int)target.MaxHp : "-") + ", our hp " + Mathf.CeilToInt(Me.Hp) + " / " + Me.MaxHp + ", at " + GameSession.Hero.Position.x.ToString("F0", CultureInfo.InvariantCulture) + "," + GameSession.Hero.Position.y.ToString("F0", CultureInfo.InvariantCulture)); }
                if (GameSession.Hero.IsDead && !heroDied) { heroDied = true; P("our hero died"); }
                yield return null;
            }
            InputAdapter.BotHeld = k => false; InputAdapter.BotPressed = k => false; // stop fighting (a respawned boss must not be attacked again); null would hand the keys to the real keyboard
            P((killed && !heroDied ? "PASS" : "FAIL") + " the " + (mapBoss ? "map" : "wanted") + " boss died after " + (Time.realtimeSinceStartup - start).ToString("F0", CultureInfo.InvariantCulture) + " s (seen=" + sawBoss + ")");
            float wait = Time.realtimeSinceStartup;
            while (Me.WantedDay == day0 && Time.realtimeSinceStartup - wait < (expectClaim ? 10f : 6f)) yield return null;
            yield return new WaitForSeconds(0.7f);
            P(WantedLine("wanted-after"));
            if (expectClaim)
            {
                P((Me.WantedDay > day0 ? "PASS" : "FAIL") + " the client saw WantedDay rise " + day0 + " -> " + Me.WantedDay);
                if (NetMode.RendersFx) P((ChatHas("Wanted boss defeated!") ? "PASS" : "FAIL") + " the claim chat line is there");
                if (shots != null) yield return Shot(shots, "wanted-claim-toast-chat");
            }
            else
            {
                P((Me.WantedDay == day0 ? "PASS" : "FAIL") + " no claim: WantedDay stays " + day0);
                if (NetMode.RendersFx) P((!ChatHas("Wanted boss defeated!") ? "PASS" : "FAIL") + " no claim chat line");
            }
            if (watchRespawn)
            {
                float died = Time.realtimeSinceStartup;
                Monster again = null;
                while (Time.realtimeSinceStartup - died < 120f && again == null) { Monster w = Monster.Wanted; if (w != null && !w.IsDying && w != target) again = w; yield return null; }
                P((again != null ? "PASS" : "FAIL") + " the wanted boss is back after " + (Time.realtimeSinceStartup - died).ToString("F0", CultureInfo.InvariantCulture) + " s");
            }
            yield return new WaitForSeconds(1f);
            Application.Quit();
        }

        IEnumerator WantedBoard()
        {
            string shots = NetLaunchArgs.Arg(Environment.GetCommandLineArgs(), "--wanted-shots");
            if (shots != null) System.IO.Directory.CreateDirectory(shots);
            P(WantedLine("wanted-before"));
            Vector2 goal = Vector2.zero;
            foreach (MapNpc n in MapRegistry.Current.Npcs) if (n.Id == "guardRook") goal = FrostHollowMap.TileFeet(n.Tx, n.Ty, 2f);
            P((goal != Vector2.zero ? "PASS" : "FAIL") + " Guard Rook stands on " + MapRegistry.Current.Id);
            Vector2 move = Vector2.zero;
            InputAdapter.BotHeld = k => (k == GameKey.Left && move.x < 0f) || (k == GameKey.Right && move.x > 0f) || (k == GameKey.Down && move.y < 0f) || (k == GameKey.Up && move.y > 0f);
            InputAdapter.BotPressed = k => false;
            float start = Time.realtimeSinceStartup;
            while (!TownNpc.InRange && Time.realtimeSinceStartup - start < 60f)
            {
                move = StepToward(GameSession.Hero.Position, goal);
                yield return null;
            }
            move = Vector2.zero;
            InputAdapter.BotHeld = k => false; // not null: that would hand the keys to the real keyboard
            P((TownNpc.InRange ? "PASS" : "FAIL") + " in talking range of the guard");
            ChatWindow.SetExpandedState(true);
            TownNpc.Talk();
            P((NpcDialogueBox.IsOpen ? "PASS" : "FAIL") + " the board opened");
            for (int page = 1; NpcDialogueBox.IsOpen && page <= 4; page++)
            {
                yield return new WaitForSeconds(2.5f); // the text types out
                if (shots != null) yield return Shot(shots, "wanted-board-page" + page);
                NpcDialogueBox.Advance();
                NpcDialogueBox.Advance();
            }
            yield return new WaitForSeconds(1f);
            Application.Quit();
        }
    }
}
