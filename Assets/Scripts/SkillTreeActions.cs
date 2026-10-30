using ZenoniaProto.Net;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// What the skill tree window can ask for. Offline it edits the local PlayerState and saves; online the server owns the state and the
    /// window only sends requests (phase 04 wires the real InvOp messages into OnlineActions; until then it refuses with a clear message).
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

        sealed class OnlineActions : ISkillTreeActions
        {
            const string NotYet = "Skill changes online arrive with the netcode update";
            public ActionResult Learn(int skillIndex) { return ActionResult.Fail(NotYet); }
            public ActionResult Assign(int slot, int skillIndex) { return ActionResult.Fail(NotYet); }
            public ActionResult Respec() { return ActionResult.Fail(NotYet); }
        }
    }
}
