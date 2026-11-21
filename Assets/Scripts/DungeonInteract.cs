using System;
using UnityEngine;
using ZenoniaProto.Net;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// Using the dungeon chests and warp crystals (Rootburrow V5, ChatGPT docs/reference/rootburrow-dark-25d/floors-purpose-chatgpt.md): the HUD's Talk / E button turns into
    /// "Open" next to a full chest and "Warp" next to an attuned crystal (WindowsHost). A chest opens after a 1.2 s hold, a warp after a 3 s channel (WaypointWindow
    /// picks the target); both break when the hero walks off or is hit (DungeonChannelBar shows the progress). Then the action goes to whoever owns the state: the zone
    /// server online (InvOp ChestOpen / WaypointWarp, it checks distance, refill time, attunement and the last hit again) or PlayerRpg + MapTransfer offline.
    /// </summary>
    public sealed class DungeonInteract : MonoBehaviour
    {
        const float WalkOffUnits = 6f; // a warp channel breaks once the hero moved this far from where it began

        static DungeonInteract instance;
        string label;
        float durationMs, elapsedMs, hurtAtStart;
        Vector2 startPos;
        DungeonPointView target;
        Action done;
        ClericLightFx channelFx;

        /// <summary>A chest / crystal the local hero can use right now (null = none): a full chest or an attuned crystal within reach.</summary>
        public static DungeonPointView Nearest()
        {
            HeroController h = GameSession.Hero;
            if (h == null || h.IsDead) return null;
            DungeonPointView best = null;
            float bestDist = DungeonPoints.ReachUnits;
            foreach (DungeonPointView v in DungeonPointView.Views)
            {
                if (v.Chest != null ? v.StateOn : !v.StateOn) continue; // an emptied chest / a crystal not attuned yet has nothing to offer
                float d = Vector2.Distance(h.Position, v.Foot);
                if (d <= bestDist) { best = v; bestDist = d; }
            }
            return best;
        }

        public static bool InRange { get { return Nearest() != null && !Channeling; } }
        public static bool Channeling { get { return instance != null && instance.done != null; } }

        /// <summary>Text of the HUD button while InRange.</summary>
        public static string Prompt
        {
            get
            {
                DungeonPointView v = Nearest();
                string key = Application.isMobilePlatform ? "" : "  [E]";
                return (v != null && v.Crystal != null ? "Warp" : "Open") + key;
            }
        }

        /// <summary>E / the button next to a chest or crystal.</summary>
        public static void Use()
        {
            DungeonPointView v = Nearest();
            if (v == null || Channeling) return;
            if (v.Crystal != null) { WaypointWindow.Open(v.Crystal); return; }
            string chestId = v.Chest.Id;
            Begin("Opening the chest...", DungeonPoints.ChestOpenMs, v, () => Send(InvOp.ChestOpen, chestId));
        }

        /// <summary>WaypointWindow picked a target: refuse early what the server would refuse, else channel 3 s and warp.</summary>
        public static void StartWarp(WaypointDef from, string toId)
        {
            HeroController h = GameSession.Hero;
            if (h == null || h.Rpg == null || Channeling) return;
            string why = DungeonPoints.WarpRefusal(h.Rpg, from.Id, toId, h.MsSinceHurt);
            if (why != null) { RpgHud.Toast(why, new Color(1f, 0.7f, 0.5f)); return; }
            DungeonPointView at = null;
            foreach (DungeonPointView v in DungeonPointView.Views) if (v.Crystal == from) at = v;
            Begin("Warping...", DungeonPoints.WarpChannelMs, at, () => Warp(from.Id, toId));
            instance.channelFx = DungeonPointFx.WarpChannel(h, DungeonPoints.WarpChannelMs);
        }

        /// <summary>Walking up to a crystal (DungeonPointView): attune it.</summary>
        public static void Attune(string crystalId) { Send(InvOp.WaypointTouch, crystalId); }

        static void Begin(string text, float ms, DungeonPointView v, Action action)
        {
            HeroController h = GameSession.Hero;
            if (h == null) return;
            if (instance == null) instance = new GameObject("DungeonInteract").AddComponent<DungeonInteract>();
            instance.label = text; instance.durationMs = ms; instance.elapsedMs = 0f; instance.target = v; instance.done = action;
            instance.startPos = h.Position; instance.hurtAtStart = h.LastHurtAtMs;
        }

        static void Send(InvOp op, string uid)
        {
            if (NetMode.IsClient) { NetClient.SendInvAction(op, uid, 0); return; } // the RpgState answer brings the change (and its toast)
            PlayerRpg rpg = LocalPlayer.Rpg;
            if (rpg == null) return;
            ActionResult r = rpg.ApplyInvAction(op, uid, 0);
            if (op == InvOp.ChestOpen && !string.IsNullOrEmpty(r.Text))
            {
                if (r.Ok) { RpgHud.Toast("Chest Opened", r.Text.Replace("Chest: ", ""), UiArt.Win("quest-icon")); ChatHub.System(r.Text); }
                else RpgHud.Toast(r.Text, new Color(1f, 0.7f, 0.5f));
            }
        }

        static void Warp(string fromId, string toId)
        {
            HeroController h = GameSession.Hero;
            if (h != null) DungeonPointFx.WarpFlash(h.Position);
            if (NetMode.IsClient) { NetClient.SendInvAction(InvOp.WaypointWarp, fromId + ">" + toId, 0); return; }
            string mapId = null; int tx = 0, ty = 0;
            string why = h == null || h.Rpg == null ? "No hero" : DungeonPoints.WarpRefusal(h.Rpg, fromId, toId, h.MsSinceHurt);
            if (why == null && !DungeonPoints.Target(toId, out mapId, out tx, out ty)) why = "No such crystal";
            else if (why == null) { MapTransfer.Go(new MapPortal(0, 0, mapId, tx, ty)); return; }
            RpgHud.Toast(why, new Color(1f, 0.7f, 0.5f));
        }

        void Cancel(string why)
        {
            done = null;
            if (channelFx != null) Destroy(channelFx.gameObject);
            channelFx = null;
            DungeonChannelBar.Hide();
            if (why != null) RpgHud.Toast(why, new Color(1f, 0.75f, 0.55f));
        }

        void Update()
        {
            if (done == null) return;
            HeroController h = GameSession.Hero;
            if (h == null || h.IsDead || MapTransfer.Busy) { Cancel(null); return; }
            if (h.LastHurtAtMs != hurtAtStart) { Cancel("Interrupted!"); return; }
            bool chest = target != null && target.Chest != null;
            if (chest ? Vector2.Distance(h.Position, target.Foot) > DungeonPoints.ReachUnits + 4f : Vector2.Distance(h.Position, startPos) > WalkOffUnits) { Cancel(chest ? null : "Warp cancelled"); return; }
            elapsedMs += Time.deltaTime * 1000f;
            DungeonChannelBar.Show(label, Mathf.Clamp01(elapsedMs / durationMs), chest);
            if (elapsedMs < durationMs) return;
            Action a = done;
            done = null; channelFx = null; // the channel FX ends on its own timeline
            DungeonChannelBar.Hide();
            a();
        }
    }
}
