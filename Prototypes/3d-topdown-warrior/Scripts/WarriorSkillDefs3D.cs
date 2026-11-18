using UnityEngine;

/// <summary>
/// The four prototype Warrior skills as ChatGPT specified them (docs/reference/3d-warrior-prototype/hud-3d-chatgpt/chatgpt-hud3d-spec.txt):
/// key, MP, cooldown, colours and the gameplay numbers that WarriorSkills3D uses. Names come from the 2D game's Warrior skill list; the
/// numbers, timelines and art are the 3D prototype's own. Icons are cut from ChatGPT's HUD sheet (Resources/ProtoHud/icon-<slug>.png).
/// </summary>
public static class WarriorSkillDefs3D
{
    public class Def
    {
        public string Name, Slug; public int Mp; public float Cooldown, Lock;
        public string Main, Core, Rim;   // effect colours (hex)
        public Texture2D Icon;
    }

    public const int PowerSlash = 0, Whirlwind = 1, ChargingStrike = 2, InfernoCrash = 3;

    public static readonly Def[] All =
    {
        // 120° fan, R 2.5 m, one hit of 180 % at 0.22 s; crescent shows 0.12-0.34 s; the swing ends at 0.48 s
        new Def { Name = "Power Slash", Slug = "power-slash", Mp = 18, Cooldown = 4f, Lock = 0.48f, Main = "19A9E8", Core = "E2FFFF", Rim = "0868BF" },
        // spin in place, circle R 2.0 m, 3 hits of 65 % at 0.25 / 0.50 / 0.75 s, wind gone by 1.05 s
        new Def { Name = "Whirlwind", Slug = "whirlwind", Mp = 28, Cooldown = 8f, Lock = 1.05f, Main = "38D878", Core = "D6FFE4", Rim = "127D45" },
        // dash 4 m during 0.10-0.33 s, 220 % once to each slime on the path (0.8 m wide), stops at walls; after-images gone by 0.55 s
        new Def { Name = "Charging Strike", Slug = "charging-strike", Mp = 40, Cooldown = 10f, Lock = 0.45f, Main = "9D4DE0", Core = "F4D8FF", Rim = "6730A4" },
        // leap 0.15-0.65 s (peak 1.3 m at 0.40), slam at 0.65: circle R 2.8 m 300 % + 50 % inside R 0.8; fire ring 0.68-0.85, gone 1.10, embers 1.35
        new Def { Name = "Inferno Crash", Slug = "inferno-crash", Mp = 80, Cooldown = 18f, Lock = 0.75f, Main = "FF722B", Core = "FFF1B0", Rim = "B93625" },
    };

    /// <summary>Loads the skill icons once (missing files just leave the slot without an icon).</summary>
    public static void LoadIcons()
    {
        foreach (var d in All) if (d.Icon == null) d.Icon = Resources.Load<Texture2D>("ProtoHud/icon-" + d.Slug);
    }
}
