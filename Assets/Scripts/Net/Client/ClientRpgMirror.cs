using UnityEngine;
using ZenoniaProto.Net;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// The client's copy of its own RPG state. SelfState (hp / mp / exp / level / cooldown, a few times a second) feeds the HUD; RpgState (bag,
    /// equipment, gold, potions -- the full state, after every change and every inventory request) replaces the mirrored PlayerState wholesale.
    /// The client never edits the state itself: the server is the only writer.
    /// </summary>
    public sealed class ClientRpgMirror
    {
        readonly NetStats stats;
        public ClientRpgMirror(NetStats netStats) { stats = netStats; }

        public void OnSelfState(SelfStateMsg m)
        {
            HeroController hero = LocalPlayer.Hero;
            if (hero != null) hero.ProxySetSelf(m);
            stats.Add("selfStates", 1);
        }

        /// <summary>Applies a full state push; returns false for unreadable data (ignored, the next push repairs it).</summary>
        public bool OnRpgState(RpgStateMsg m)
        {
            stats.Add("rpgStates", 1);
            stats.Add("rpgStateBytes", m.SaveJson != null ? m.SaveJson.Length : 0);
            HeroController hero = LocalPlayer.Hero;
            if (hero == null) return false;
            SaveData save = SaveSerializer.Parse(m.SaveJson);
            if (save == null) return false;
            PlayerState state = SaveSerializer.Restore(save);
            if (RpgSession.Instance != null) RpgSession.Instance.AttachRemote(state, hero);
            if (NetMode.RendersFx && m.ReqId != 0 && !string.IsNullOrEmpty(m.Text)) RpgHud.Toast(m.Text, m.Result == 1 ? new Color(0.7f, 1f, 0.7f) : new Color(1f, 0.7f, 0.5f));
            return true;
        }
    }
}
