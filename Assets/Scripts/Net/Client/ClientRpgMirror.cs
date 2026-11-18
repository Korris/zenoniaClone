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
        bool questsKnown;
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
            SaveData save = SaveSerializer.Parse(m.SaveJson, FrostHollowLayout.RowCount);
            if (save == null) return false;
            PlayerState state = SaveSerializer.Restore(save);
            AccountState account = null;
            try { account = string.IsNullOrEmpty(m.AccountJson) ? null : AccountState.Sanitize(JsonUtility.FromJson<AccountState>(m.AccountJson)); }
            catch (System.Exception) { account = null; } // unreadable: the windows show no account until the next push
            if (account != null) state.AttachAccount(account); // the pet's stars, tickets, pity: read-only here, the server is the writer
            else state.RecomputeStats();
            if (RpgSession.Instance != null) { RpgSession.Instance.AttachRemote(state, hero); RpgSession.Instance.ApplyRemoteBossFlags(save); }
            hero.OnOwnAppearance(state.Appearance); // first push only: the saved look, an old device-only look moved to the server
            if (NetMode.RendersFx) { QuestTracker.Observe(state.Quests, questsKnown); WantedBossTracker.Observe(state.WantedDay, questsKnown); }
            questsKnown = true; // the first push of a connection is the character arriving, not news
            if (NetMode.RendersFx && m.ReqId != 0 && !string.IsNullOrEmpty(m.Text)) RpgHud.Toast(m.Text, m.Result == 1 ? new Color(0.7f, 1f, 0.7f) : new Color(1f, 0.7f, 0.5f));
            return true;
        }
    }
}
