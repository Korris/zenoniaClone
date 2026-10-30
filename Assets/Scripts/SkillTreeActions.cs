using ZenoniaProto.Net;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// What the skill tree window can ask for. Offline it edits the local PlayerState and saves; online the server owns the state and the
    /// window only sends requests (InvOp.SkillLearn / SkillAssign / SkillRespec) and redraws when the server's RpgState arrives.
    /// </summary>
    public interface ISkillTreeActions
    {
        ActionResult Learn(int skillIndex);               // spend 1 SP: learn or rank up
        ActionResult Assign(int slot, int skillIndex);    // magazine slot 0-7 (skillIndex -1 = clear)
        ActionResult Respec();
    }

    public static class SkillTreeActions
    {
        public static readonly ISkillTreeActions Offline = new OfflineActions(), Online = new OnlineActions();

        public static ISkillTreeActions Current { get { return NetMode.IsClient ? Online : Offline; } }

        /// <summary>Player-facing reason a skill cannot be learned / ranked up (null when it can).</summary>
        public static string Reason(PlayerState p, SkillDef d)
        {
            switch (p.Skills.CanLearn(d, p.Level))
            {
                case LearnResult.Ok: return null;
                case LearnResult.MaxRank: return "Max rank";
                case LearnResult.LevelTooLow: return "Requires Lv " + d.UnlockLevel;
                case LearnResult.NoSp: return "No skill points left";
                default: return "Needs " + PrereqText(d);
            }
        }

        public static string PrereqText(SkillDef d)
        {
            string s = "";
            foreach (SkillReq r in d.Prereqs) s += (s.Length > 0 ? ", " : "") + SkillTable.Get(d.Class, r.Index).Name + " rank " + r.Rank;
            return s;
        }

        sealed class OfflineActions : ISkillTreeActions
        {
            public ActionResult Learn(int skillIndex)
            {
                PlayerState p = RpgSession.Player;
                if (p == null) return ActionResult.Fail("No character");
                string why = Reason(p, SkillTable.Get(p.ClassId, skillIndex));
                if (why != null) return ActionResult.Fail(why);
                p.LearnSkill(skillIndex);
                return Saved(ActionResult.Success(p.Skills[skillIndex] == 1 ? "Learned" : "Rank " + p.Skills[skillIndex]));
            }

            public ActionResult Assign(int slot, int skillIndex)
            {
                PlayerState p = RpgSession.Player;
                if (p == null) return ActionResult.Fail("No character");
                if (skillIndex < 0) { p.Loadout.Unassign(slot); return Saved(ActionResult.Success("Slot cleared")); }
                return p.Loadout.Place(slot, skillIndex, p.Skills) ? Saved(ActionResult.Success("Assigned")) : ActionResult.Fail("Learn the skill first");
            }

            public ActionResult Respec() { return Saved(SkillRespec.Apply(RpgSession.Player)); }

            static ActionResult Saved(ActionResult r)
            {
                if (r.Ok && RpgSession.Instance != null) RpgSession.Instance.SaveNow();
                return r;
            }
        }

        /// <summary>Online: requests travel as inventory ops; the server validates, applies and answers with the full RpgState (toast + redraw).</summary>
        sealed class OnlineActions : ISkillTreeActions
        {
            public ActionResult Learn(int skillIndex)
            {
                PlayerState p = RpgSession.Player;
                if (p == null) return ActionResult.Fail("No character");
                string why = Reason(p, SkillTable.Get(p.ClassId, skillIndex));
                if (why != null) return ActionResult.Fail(why);
                NetClient.SendInvAction(InvOp.SkillLearn, "", (byte)skillIndex);
                return ActionResult.Success("Learning...");
            }

            public ActionResult Assign(int slot, int skillIndex)
            {
                NetClient.SendInvAction(InvOp.SkillAssign, slot.ToString(), skillIndex < 0 ? PlayerRpg.NoSkill : (byte)skillIndex);
                return ActionResult.Success(skillIndex < 0 ? "Clearing..." : "Assigning...");
            }

            public ActionResult Respec()
            {
                NetClient.SendInvAction(InvOp.SkillRespec, "", 0);
                return ActionResult.Success("Resetting...");
            }
        }
    }
}
