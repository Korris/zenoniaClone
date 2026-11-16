using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.EventSystems;
using ZenoniaProto.Net;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// Capture flags of the Adventure Paper Wanted UI (phase 11; shoot with --ui-new hud,boss,wanted). The Wanted Boss is online only, so an
    /// offline capture feeds the client the message a zone would send: the UI below it runs its real code (WantedBossTracker.OnStatus / Observe).
    ///   --wanted-demo alive|respawn|claimed   a WantedStatus arrives: the boss stands / is back in 18:24 / stands and this hero was paid today
    ///   --wanted-demo-boss TYPE               rimeclawTyrant (Frost Hollow, the capture map: boss on this map) | glacialColossus | mireHydraQueen;
    ///                                         default = today's boss by the device clock. The rotation shows unless the type is not today's.
    ///   --wanted-demo-respawn-sec N           seconds until the boss returns (default 1104 = 18:24)
    ///   --wanted-demo-flip S                  at S seconds the boss dies / returns (a second WantedStatus): the real Respawn and Already triggers
    ///   --open-wanted-board [S]               opens the board through UiScreens (at S seconds; default at once)
    ///   --wanted-talk NPC@S                   talks to guardRook / bountyLeila at S seconds through WantedBossBoard.TryOpen (TownNpc.Talk's call)
    ///   --wanted-notice login|respawn|area|warning|claimed|already|all [--wanted-notice-at S]   raises that notice at S seconds (default 1);
    ///                                         claimed = WantedDay rises through WantedBossTracker.Observe (the real BountyPaid path);
    ///                                         all = every notice in the same frame: the log lists them in priority order ("wanted notice: ...")
    ///   --wanted-panel-ok S                   at S seconds: presses OK on the bounty panel (the notices waiting behind it follow)
    ///   --wanted-guide                        the guide is on (arrow + metres; the minimap skull shows on the boss's map either way)
    ///   --wanted-tap-card S                   at S seconds: a real pointer tap where the HUD card is (EventSystem raycast), logs what it hit
    /// Nothing is saved: capture runs never write the player's files.
    /// </summary>
    public partial class DebugCapture
    {
        partial void DebugUiWanted()
        {
            string demo = Val("--wanted-demo");
            if (demo != null) StartCoroutine(WantedDemo(demo));
        }

        static float WantedArg(string key, float fallback)
        {
            string v = Val(key);
            return v != null ? float.Parse(v, CultureInfo.InvariantCulture) : fallback;
        }

        static void SendWantedStatus(string bossType, int day, bool alive, float respawnSec)
        {
            WantedBossTracker.OnStatus(new WantedStatusMsg { BossType = bossType, DayKey = day, Alive = alive, RespawnInMs = alive ? 0u : (uint)(respawnSec * 1000f), ResetInMs = 27735000u }); // 07:42:15, the mockups' clock
        }

        IEnumerator WantedDemo(string demo)
        {
            while (GameSession.Hero == null || GameSession.Hero.Rpg == null) yield return null;
            PlayerState p = GameSession.Hero.Rpg;
            long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            int day = WantedBoss.DayKey(now);
            string boss = Val("--wanted-demo-boss") ?? WantedBoss.Today(now).BossType;
            float respawnSec = WantedArg("--wanted-demo-respawn-sec", 1104f);
            bool alive = demo != "respawn";
            WantedBossTracker.Observe(demo == "claimed" ? day : 0, false); // the arrival: what the first RpgState of a connection does
            p.WantedDay = demo == "claimed" ? day : 0;
            SendWantedStatus(boss, day, alive, respawnSec);
            WantedGuide.On = Flag("--wanted-guide");
            float start = Time.realtimeSinceStartup;
            var steps = new List<KeyValuePair<float, Action>>();
            string open = Val("--open-wanted-board"), talk = Val("--wanted-talk"), notice = Val("--wanted-notice");
            float openAt;
            if (Flag("--open-wanted-board")) steps.Add(Step(open != null && float.TryParse(open, NumberStyles.Float, CultureInfo.InvariantCulture, out openAt) ? openAt : 0f, () => UiScreens.Open(UiScreen.Wanted)));
            if (talk != null) steps.Add(Step(float.Parse(talk.Split('@')[1], CultureInfo.InvariantCulture), () => WantedTalk(talk.Split('@')[0])));
            if (Val("--wanted-demo-flip") != null) steps.Add(Step(WantedArg("--wanted-demo-flip", 0f), () => { alive = !alive; SendWantedStatus(boss, day, alive, respawnSec); }));
            if (notice != null) steps.Add(Step(WantedArg("--wanted-notice-at", 1f), () => WantedNoticeDemo(notice, p, day)));
            if (Val("--wanted-tap-card") != null) steps.Add(Step(WantedArg("--wanted-tap-card", 0f), WantedTapCard));
            if (Val("--wanted-panel-ok") != null) steps.Add(Step(WantedArg("--wanted-panel-ok", 0f), WantedPanelOk));
            steps.Sort((a, b) => a.Key.CompareTo(b.Key));
            foreach (var step in steps)
            {
                while (Time.realtimeSinceStartup - start < step.Key) yield return null;
                step.Value();
            }
        }

        static KeyValuePair<float, Action> Step(float at, Action run) { return new KeyValuePair<float, Action>(at, run); }

        static void WantedTalk(string npcId)
        {
            NpcDef def = NpcDefs.Get(npcId);
            bool board = def != null && WantedBossBoard.TryOpen(npcId, def.Name, null, def.Lines);
            Debug.Log("[ZenoniaProto] wanted-talk " + npcId + " => board " + board + ", new board open " + UiScreens.IsOpen(UiScreen.Wanted) + ", dialogue " + NpcDialogueBox.IsOpen);
        }

        static void WantedNoticeDemo(string notice, PlayerState p, int day)
        {
            bool all = notice == "all";
            if (all || notice == "already") WantedHud.DebugNotice(WantedNotice.Already);
            if (all || notice == "login") WantedHud.DebugNotice(WantedNotice.Login);
            if (all || notice == "area") WantedHud.DebugNotice(WantedNotice.Area);
            if (all || notice == "respawn") WantedHud.DebugNotice(WantedNotice.Respawn);
            if (all || notice == "warning") WantedHud.DebugNotice(WantedNotice.Warning);
            if (all || notice == "claimed")
            {
                p.WantedDay = day; // what the zone's RpgState push does after the kill
                WantedBossTracker.Observe(day, true);
            }
        }

        static void WantedPanelOk()
        {
            GameObject ok = GameObject.Find("PaperWantedReward/Safe/Panel/Ok");
            if (ok != null) ok.GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            Debug.Log("[ZenoniaProto] wanted-panel-ok => " + (ok != null ? "pressed" : "no panel"));
        }

        /// <summary>Pointer down / up / click on whatever the EventSystem finds under the card's centre: proves the card is reachable under the control layers.</summary>
        static void WantedTapCard()
        {
            RectTransform card = WantedHud.CardRoot;
            if (card == null || !card.gameObject.activeInHierarchy || EventSystem.current == null) { Debug.Log("[ZenoniaProto] wanted-tap-card: no card on screen"); return; }
            Vector3[] corners = new Vector3[4];
            card.GetWorldCorners(corners);
            Vector2 at = new Vector2(Mathf.Lerp(corners[0].x, corners[2].x, 0.6f), Mathf.Lerp(corners[0].y, corners[2].y, 0.35f)); // on the plank, under the ribbon
            var e = new PointerEventData(EventSystem.current) { position = at, pointerId = -1, button = PointerEventData.InputButton.Left };
            var hits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(e, hits);
            GameObject top = hits.Count > 0 ? hits[0].gameObject : null;
            if (top != null)
            {
                ExecuteEvents.ExecuteHierarchy(top, e, ExecuteEvents.pointerDownHandler);
                ExecuteEvents.ExecuteHierarchy(top, e, ExecuteEvents.pointerUpHandler);
                ExecuteEvents.ExecuteHierarchy(top, e, ExecuteEvents.pointerClickHandler);
            }
            Debug.Log("[ZenoniaProto] wanted-tap-card at " + at + " hit " + (top != null ? top.transform.parent.name + "/" + top.name : "nothing") + " => board open " + UiScreens.IsOpen(UiScreen.Wanted));
        }
    }
}
