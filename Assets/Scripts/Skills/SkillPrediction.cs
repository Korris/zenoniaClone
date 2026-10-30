using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// Which skills the online owner may start at the key press (before the server confirms): pure pictures and cast roots whose outcome does not
    /// depend on the world state. Motion skills move the hero on the server only, and NearestEnemy / LowestAlly skills need a target the
    /// client may see differently, so those wait for the server's SkillCast event (and its snapshot position).
    /// </summary>
    public static class SkillPrediction
    {
        public static bool Predicts(SkillDef def)
        {
            if (def == null) return false;
            if (def.Targeting == SkillTargeting.NearestEnemy || def.Targeting == SkillTargeting.LowestAlly) return false;
            for (int i = 0; i < def.Effects.Length; i++) if (def.Effects[i].Kind == EffectKind.Motion) return false;
            return true;
        }
    }
}
