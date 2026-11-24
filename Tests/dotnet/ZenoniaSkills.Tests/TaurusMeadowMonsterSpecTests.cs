using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using Xunit;
using Xunit.Abstractions;
using ZenoniaProto;
using ZenoniaProto.Rpg;

/// <summary>
/// Taurus V4 phase 05: the nine Clover Meadows species against the BIBLE tables (part3 XII: speed factor, aggro m, attack interval, leash family; part1 VI: level, size) and a time-to-kill
/// estimate for Warrior / Wizard / Cleric at the camp level with the starter weapon. MonsterDef needs Unity, so its source text is read (like the map tests read MapSpawnTable).
/// </summary>
public class TaurusMeadowMonsterSpecTests
{
    readonly ITestOutputHelper log;
    public TaurusMeadowMonsterSpecTests(ITestOutputHelper log) { this.log = log; }

    internal sealed class Spec { public string Id; public float Hp, Atk, Def, Speed, Aggro = 70f, Leash = 150f, Windup = 350f, Cooldown = 1200f, HitW = 12f; public int Level; public bool Ranged; }

    static string Repo(string rel)
    {
        string dir = AppContext.BaseDirectory;
        while (dir != null && !Directory.Exists(Path.Combine(dir, "Assets"))) dir = Path.GetDirectoryName(dir);
        return Path.Combine(dir, rel);
    }

    static float Named(string text, string name, float fallback)
    {
        Match m = Regex.Match(text, "\\b" + name + ": ([\\d.]+)f");
        return m.Success ? float.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture) : fallback;
    }

    internal static Spec Read(string id)
    {
        string src = File.ReadAllText(Repo("Assets/Scripts/MonsterDef.cs"));
        int i = src.IndexOf("new MonsterDef(\"", src.IndexOf("\"" + id + "\", ", StringComparison.Ordinal) - 40, StringComparison.Ordinal);
        Assert.True(i > 0, id);
        string stmt = src.Substring(i, src.IndexOf(';', i) - i);
        Match m = Regex.Match(stmt, "new MonsterDef\\(\"[^\"]+\", \"" + id + "\", ([\\d.]+)f, ([\\d.]+)f, ([\\d.]+)f, ([\\d.]+)f, (\\d+)");
        Assert.True(m.Success, id);
        float F(int g) { return float.Parse(m.Groups[g].Value, CultureInfo.InvariantCulture); }
        return new Spec
        {
            Id = id, Hp = F(1) * 2f, Atk = F(2), Def = F(3), Speed = F(4), Level = int.Parse(m.Groups[5].Value),
            Aggro = Named(stmt, "aggro", 70f), Leash = Named(stmt, "leash", 150f), Windup = Named(stmt, "windupMs", 350f), Cooldown = Named(stmt, "cooldownMs", 1200f), HitW = Named(stmt, "hitW", 12f),
            Ranged = stmt.Contains("Ranged ="),
        };
    }

    // id, speed factor, aggro metres, attack interval s, leash family tiles (min, max), level
    static readonly object[][] Table =
    {
        new object[] { "tauCloverSlime", 0.65f, 2.0f, 2.0f, 4f, 5f, 1 },
        new object[] { "tauHornRabbit", 1.10f, 2.5f, 1.7f, 4f, 5f, 3 },
        new object[] { "tauReedFrog", 0.90f, 2.3f, 1.9f, 4f, 5f, 4 },
        new object[] { "tauMossMushroom", 0.65f, 4.0f, 2.4f, 5f, 6f, 5 },
        new object[] { "tauBrambleBoar", 0.85f, 3.0f, 2.2f, 5f, 6f, 6 },
        new object[] { "tauLeafMantis", 1.05f, 3.0f, 1.5f, 5f, 6f, 7 },
        new object[] { "tauRootling", 0.60f, 2.5f, 2.6f, 6f, 7f, 8 },
        new object[] { "tauThornSprite", 0.95f, 4.5f, 2.1f, 6f, 7f, 9 },
        new object[] { "tauAncientBarkling", 0.55f, 3.0f, 2.8f, 6f, 7f, 10 },
    };

    [Fact]
    public void EverySpeciesMatchesTheBibleSpeedAggroIntervalAndLeashFamily()
    {
        Spec slime = Read("tauCloverSlime");
        foreach (object[] row in Table)
        {
            Spec s = Read((string)row[0]);
            Assert.InRange(s.Speed / (slime.Speed / 0.65f), (float)row[1] * 0.97f, (float)row[1] * 1.03f);
            Assert.InRange(s.Aggro / 16f, (float)row[2] - 0.15f, (float)row[2] + 0.15f);
            Assert.InRange((s.Windup + s.Cooldown) / 1000f, (float)row[3] - 0.01f, (float)row[3] + 0.01f);
            Assert.InRange(s.Leash / 16f, (float)row[4], (float)row[5]);
            Assert.Equal((int)row[6], s.Level);
        }
    }

    [Fact]
    public void OnlyMushroomAndThornSpriteShootAndTheRoleOrderOfHpHolds()
    {
        foreach (object[] row in Table) Assert.Equal((string)row[0] == "tauMossMushroom" || (string)row[0] == "tauThornSprite", Read((string)row[0]).Ranged);
        Assert.True(Read("tauBrambleBoar").Hp > Read("tauHornRabbit").Hp);   // BIBLE: Boar HP above Rabbit
        Assert.True(Read("tauLeafMantis").Hp < Read("tauBrambleBoar").Hp);   // Mantis lower than Boar
        Assert.True(Read("tauRootling").Hp > Read("tauBrambleBoar").Hp);     // Rootling: tank
        foreach (object[] row in Table) if ((string)row[0] != "tauAncientBarkling") Assert.True(Read("tauAncientBarkling").Hp > Read((string)row[0]).Hp, (string)row[0]); // Barkling: highest of the common monsters
    }

    [Fact]
    public void FieldBossIsAtLeastThreeTimesTheToughestCommonMonsterWithTheBibleMultipliers()
    {
        Spec boss = Read("tauBramblebackMatriarch"), boar = Read("tauBrambleBoar");
        Assert.Equal(9, boss.Level);
        // BIBLE: HP 3.0x, ATK 1.25x, DEF 1.1x, speed 0.9x of a Bramble Boar raised to Lv 9: the boar's role factor (its HP over the Lv6 line 14+22L) on the Lv9 line
        float boarHp9 = (14f + 22f * 9f) * (boar.Hp / (14f + 22f * 6f)), boarAtk9 = 3f + 4f * 9f;
        Assert.InRange(boss.Hp / boarHp9, 3.0f, 5.0f);                                   // ChatGPT: 3.0x the boar; raised a little so the 30-60 s solo target holds (see the test below)
        Assert.InRange(boss.Hp / Read("tauAncientBarkling").Hp, 3.0f, 3.02f);       // the user (24/11): about 3x the map's toughest normal monster
        Assert.InRange(boss.Atk / boarAtk9, 1.2f, 1.3f);
        Assert.InRange(boss.Def, 10f, 12f);
        Assert.InRange(boss.Speed / boar.Speed, 0.85f, 0.95f);
        Assert.InRange(boss.Leash / 16f, 5f, 8f); // the nest is 12 wide: the boss never leaves it
    }

    // --- time to kill: Warrior / Wizard / Cleric at the camp level, starter weapon, sustained basic attacks, 75 % uptime (walking, dodging) --------------------------

    internal static double Dps(string cls, int level, float monDef)
    {
        PlayerState p = PlayerState.NewGame(cls);
        p.Level = level; p.RecomputeStats();
        ComboHit[] hits = cls == "warrior" ? ComboConfigs.WarriorHits : cls == "wizard" ? ComboConfigs.WizardHits : ComboConfigs.ClericHits;
        double dmg = 0, ms = 0;
        foreach (ComboHit h in hits) { dmg += Math.Max(1, p.Atk * h.Mult - monDef * 0.5); ms += h.TotalMs; }
        return dmg / (ms / 1000.0) * 0.75;
    }

    // species -> BIBLE time-to-kill target (s), part3 XII.4
    static readonly Dictionary<string, double[]> Ttk = new Dictionary<string, double[]>
    {
        { "tauCloverSlime", new[] { 2.0, 4.0 } }, { "tauHornRabbit", new[] { 3.0, 5.0 } }, { "tauReedFrog", new[] { 3.0, 5.0 } }, { "tauMossMushroom", new[] { 4.0, 6.0 } },
        { "tauBrambleBoar", new[] { 5.0, 8.0 } }, { "tauLeafMantis", new[] { 4.0, 6.0 } }, { "tauRootling", new[] { 6.0, 9.0 } }, { "tauThornSprite", new[] { 5.0, 7.0 } },
        { "tauAncientBarkling", new[] { 8.0, 12.0 } },
    };

    [Fact]
    public void TimeToKillAtTheCampLevelIsNearTheBibleTarget()
    {
        foreach (object[] row in Table)
        {
            Spec s = Read((string)row[0]);
            double[] t = Ttk[s.Id];
            var line = new List<string>();
            foreach (string cls in new[] { "warrior", "wizard", "cleric" })
            {
                double sec = s.Hp / Dps(cls, s.Level, s.Def);
                line.Add(cls + " " + sec.ToString("F1"));
                // an estimate (no equipment drops, no skills, no movement model): generous band round the BIBLE target, the real numbers come from play
                Assert.InRange(sec, t[0] * 0.45, t[1] * 1.6);
            }
            log.WriteLine(s.Id + " Lv" + s.Level + " HP " + s.Hp + " target " + t[0] + "-" + t[1] + " s : " + string.Join(", ", line));
        }
    }

    [Fact]
    public void ASoloLv9To10HeroNeedsHalfAMinuteToAMinuteForTheFieldBoss()
    {
        Spec boss = Read("tauBramblebackMatriarch");
        foreach (string cls in new[] { "warrior", "wizard" })
        {
            double sec = boss.Hp / Dps(cls, 10, boss.Def); // BIBLE: 30-60 s when the skills are dodged well; the 0.75 uptime already stands for some dodging
            log.WriteLine("boss vs " + cls + " Lv10: " + sec.ToString("F0") + " s");
            Assert.InRange(sec, 22.0, 90.0);
        }
    }
}
