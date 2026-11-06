namespace ZenoniaProto.Rpg
{
    /// <summary>Warrior farm prototype. Other classes retain their existing combat tuning until this trial is approved.</summary>
    public static class FarmSkillRules
    {
        public static bool IsRepeatable(string classId, int index) { return classId == "warrior" && index == 0; }

        public static int CooldownMs(string classId, int index, int designedMs)
        {
            if (classId != "warrior") return (int)(designedMs * SkillTable.CooldownScale);
            return designedMs;
        }
    }
}
