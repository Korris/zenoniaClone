using System;

namespace ZenoniaProto.Rpg
{
    /// <summary>
    /// Skill respec (user decision: costs gold, scaling with level). Cost = 50 gold per character level (Lv 10 = 500 G, Lv 60 = 3000 G).
    /// Refunds every point (ranks + magazine cleared; SP is derived, so Free == Level afterwards). Engine-free, shared by offline and the server.
    /// </summary>
    public static class SkillRespec
    {
        public const int GoldPerLevel = 50;

        public static int Cost(int level) { return GoldPerLevel * Math.Max(1, level); }

        public static ActionResult Apply(PlayerState p)
        {
            if (p == null) return ActionResult.Fail("No character");
            if (p.Skills.Sum == 0) return ActionResult.Fail("No skills to reset");
            int cost = Cost(p.Level);
            if (p.Gold < cost) return ActionResult.Fail("Need " + cost + " G to respec");
            p.Gold -= cost;
            p.Skills.Reset();
            p.Loadout.Clear();
            return ActionResult.Success("Skills reset (-" + cost + " G)");
        }
    }
}
