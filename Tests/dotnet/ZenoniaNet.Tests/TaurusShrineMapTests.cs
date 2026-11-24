using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using Xunit;
using ZenoniaProto;

/// <summary>
/// Taurus V4 phase 06 greybox: the Ancient Root Shrine, 96x64, three rooms in a line (BIBLE part2 I-II), its 18 + 1 monsters, the doors to Clover Meadows, the two crystals and the Memory Amber.
/// MapSpawnTable / MonsterDef / DungeonPoints are not in this project: their source text is read (like TaurusMeadowCampsTests); the engine-free rules are in ZenoniaSkills.Tests.
/// </summary>
[Collection("MapRegistry")]
public class TaurusShrineMapTests
{
    const string ShrineId = "tauAncientRootShrine";
    static IGameMap Shrine { get { return MapRegistry.Get(ShrineId); } }
    static IGameMap Meadows { get { return MapRegistry.Get(TauCloverMeadowsMap.MapId); } }
    static string Src(string rel) { return File.ReadAllText(TaurusMapTests.RepoFile(rel)); }

    sealed class Camp { public string Type; public int Count, X, Y, W, H; public float Respawn; }

    static List<Camp> Camps()
    {
        string src = Src("Assets/Scripts/MapSpawnTable.cs");
        int a = src.IndexOf("TauAncientRootShrine =", StringComparison.Ordinal);
        Assert.True(a > 0);
        string block = src.Substring(a, src.IndexOf("};", a, StringComparison.Ordinal) - a);
        return Regex.Matches(block, "new FrostHollowMap\\.Zone\\(\"(\\w+)\", (\\d+), (\\d+), (\\d+), (\\d+), (\\d+), (\\d+)f\\)")
            .Cast<Match>().Select(m => new Camp { Type = m.Groups[1].Value, Count = int.Parse(m.Groups[2].Value), X = int.Parse(m.Groups[3].Value), Y = int.Parse(m.Groups[4].Value), W = int.Parse(m.Groups[5].Value), H = int.Parse(m.Groups[6].Value), Respawn = float.Parse(m.Groups[7].Value, CultureInfo.InvariantCulture) }).ToList();
    }

    [Fact]
    public void TheShrineIsLiveAndBigEnoughForTheThreeRooms()
    {
        Assert.NotNull(Shrine);
        Assert.Contains(MapRegistry.Live, m => m.Id == ShrineId);
        Assert.Equal(96, Shrine.Width); Assert.Equal(64, Shrine.Height); Assert.True(Shrine.Hostile);
        Assert.True(Shrine.Width <= GameConfig.MaxTilesW && Shrine.Height <= GameConfig.MaxTilesH);
        int open = 0; for (int y = 0; y < Shrine.Height; y++) for (int x = 0; x < Shrine.Width; x++) if (!Shrine.IsSolidTile(x, y)) open++;
        Assert.InRange(open, 1800, 2400); // 24x22 + 28x24 + a 30x28 ellipse + corridors
    }

    [Fact]
    public void EveryOpenTileIsReachableAndTheHudRowsAreBlocked()
    {
        Vector2Int s = TaurusMapTests.StartTile(Shrine);
        Assert.False(Shrine.IsSolidTile(s.x, s.y));
        var reach = TaurusMapTests.Reach(Shrine, s.x, s.y);
        for (int y = 0; y < Shrine.Height; y++)
            for (int x = 0; x < Shrine.Width; x++)
            {
                if (!Shrine.IsSolidTile(x, y)) Assert.True(reach.Contains(y * Shrine.Width + x), "open but unreachable " + x + "," + y);
                if (y >= Shrine.Height - 3) Assert.True(Shrine.IsSolidTile(x, y), "HUD row open " + x + "," + y);
            }
    }

    [Fact]
    public void TheRoomsFollowTheBibleSizesAndCorridorWidths()
    {
        // arena (rows 0-29): the ellipse is 30 wide and 28 tall
        int minX = 99, maxX = 0, minY = 99, maxY = 0;
        for (int y = 0; y < 30; y++) for (int x = 0; x < Shrine.Width; x++) if (!Shrine.IsSolidTile(x, y)) { minX = Math.Min(minX, x); maxX = Math.Max(maxX, x); minY = Math.Min(minY, y); maxY = Math.Max(maxY, y); }
        Assert.True(maxX - minX + 1 >= 30 && maxY - minY + 1 >= 28, "arena " + (maxX - minX + 1) + " x " + (maxY - minY + 1));
        Assert.True(TaurusMapTests.StartTile(Shrine).x < 10 && TaurusMapTests.StartTile(Shrine).y > 36); // heroes arrive far south-west of the boss, in the entrance room
        Func<int, int, int, int> across = (x0, x1, y) => Enumerable.Range(x0, x1 - x0 + 1).Count(x => !Shrine.IsSolidTile(x, y)); // open tiles in a row stretch
        Assert.Equal(4, Enumerable.Range(36, 20).Count(y => !Shrine.IsSolidTile(62, y))); // corridor B: x 62 is open for rows 46-49 only
        Assert.Equal(4, across(70, 80, 31)); // the neck to the sanctuary
        Assert.Equal(4, across(2, 12, 35));  // the entrance stub
        Assert.Equal(4, Enumerable.Range(40, 15).Count(y => !Shrine.IsSolidTile(27, y))); // corridor A: x 27 is open for rows 46-49 only
    }

    [Fact]
    public void TheDoorsConnectBothWaysAndLandOnOpenGround()
    {
        var down = Meadows.Portals.Where(p => p.TargetMapId == ShrineId).ToList();
        var up = Shrine.Portals.Where(p => p.TargetMapId == TauCloverMeadowsMap.MapId).ToList();
        Assert.Equal(4, down.Count); Assert.Equal(4, up.Count);
        foreach (MapPortal p in down) { Assert.Equal(2, p.Ty); Assert.InRange(p.Tx, 56, 59); Assert.False(Meadows.IsSolidTile(p.Tx, p.Ty)); Assert.False(Shrine.IsSolidTile(p.TargetTx, p.TargetTy)); }
        foreach (MapPortal p in up) { Assert.False(Shrine.IsSolidTile(p.Tx, p.Ty)); Assert.False(Meadows.IsSolidTile(p.TargetTx, p.TargetTy)); Assert.True(TaurusMapTests.Dist(p.Tx, p.Ty, p.TargetTx, p.TargetTy) > 0); Assert.True(p.TargetTy >= 5); }
        Assert.Equal(TauAncientRootShrineMap.ArrivalTy, TaurusMapTests.StartTile(Shrine).y);
        string spawn = Src("Assets/Scripts/MapSpawnTable.cs");
        Assert.Contains("case TauAncientRootShrineId: return MonsterDef.TauElderRootGuardian;", spawn);
        Assert.Contains("mapId == TauCloverMeadowsId || mapId == TauAncientRootShrineId; }", spawn); // HasOwnBoss: the Elder's kill is saved per map
    }

    [Fact]
    public void EighteenCommonMonstersOnOpenGroundFarFromTheEntranceAndASlowRespawn()
    {
        List<Camp> camps = Camps();
        Assert.Equal(18, camps.Sum(c => c.Count));
        Func<string, int> count = t => camps.Where(c => c.Type == t).Sum(c => c.Count);
        Assert.Equal(3, count("tauRootling")); Assert.Equal(3, count("tauAncientBarkling")); Assert.Equal(4, count("tauAmberSporeling")); Assert.Equal(3, count("tauThornSprite")); Assert.Equal(5, count("tauRuneBarkGuardian"));
        Vector2Int start = TaurusMapTests.StartTile(Shrine);
        foreach (Camp c in camps)
        {
            int free = 0; double nearest = 999;
            for (int y = c.Y; y < c.Y + c.H; y++) for (int x = c.X; x < c.X + c.W; x++) { if (!Shrine.IsSolidTile(x, y)) free++; nearest = Math.Min(nearest, TaurusMapTests.Dist(x, y, start.x, start.y)); }
            Assert.True(free == c.W * c.H && free >= 2 * c.Count, c.Type + " rect: " + free + " free of " + c.W * c.H);
            Assert.True(nearest >= 5, c.Type + " only " + nearest + " tiles from the arrival");
            Assert.Equal(120000f, c.Respawn); // the plan's slow respawn (a public map, no sessions)
        }
        Assert.All(camps.Where(c => c.Type == "tauRootling" || c.Type == "tauAncientBarkling"), c => Assert.InRange(c.X, 2, 25));      // Area 01
        Assert.All(camps.Where(c => c.Type == "tauAmberSporeling" || c.Type == "tauThornSprite"), c => Assert.InRange(c.X, 30, 57));  // Area 02
        Assert.Contains(camps, c => c.Type == "tauRuneBarkGuardian" && c.Count == 3 && c.Y >= 31 && c.Y + c.H <= 41);                // the antechamber
    }

    [Fact]
    public void CrystalsAndTheMemoryAmberStandOnSolidTilesWithAFreeTileBeside()
    {
        string src = Src("Assets/Scripts/Rpg/DungeonPoints.cs");
        var ws = Regex.Matches(src, "new WaypointDef\\((\\w+), \"" + ShrineId + "\", \"[^\"]+\", (\\d+), (\\d+), (\\d+), (\\d+)(, true)?\\)").Cast<Match>().ToList();
        Assert.Equal(3, ws.Count);
        foreach (Match w in ws)
        {
            int tx = int.Parse(w.Groups[2].Value), ty = int.Parse(w.Groups[3].Value), ax = int.Parse(w.Groups[4].Value), ay = int.Parse(w.Groups[5].Value);
            Assert.True(Shrine.IsSolidTile(tx, ty), w.Groups[1].Value + " must be a solid tile");
            Assert.False(Shrine.IsSolidTile(ax, ay), w.Groups[1].Value + " arrival must be walkable");
            Assert.Equal(1, Math.Abs(ax - tx) + Math.Abs(ay - ty));
        }
        Assert.Equal(new[] { "TauShrineGateId", "TauShrineDeepId", "TauMemoryAmberId" }, ws.Select(w => w.Groups[1].Value).ToArray());
        Assert.Equal(new[] { false, false, true }, ws.Select(w => w.Groups[6].Success).ToArray()); // the Memory Amber is quest-only
        Assert.InRange(int.Parse(ws[1].Groups[2].Value), 58, 66); // the checkpoint lies in corridor B (x 58-66): behind Area 02, before the guards' room
        Assert.Equal(TauCloverMeadowsMap.DoorArrivalTx, int.Parse(Regex.Match(src, "ShrineExitTx = (\\d+)").Groups[1].Value)); // crystal exit = the door arrival
        Assert.Equal(TauCloverMeadowsMap.DoorArrivalTy, int.Parse(Regex.Match(src, "ShrineExitTy = (\\d+)").Groups[1].Value));
    }

    [Fact]
    public void TheWireIndexesAppendTheThreeNewDefsLastAndTheElderIsDrawnFromTheSharpSheets()
    {
        string src = Src("Assets/Scripts/MonsterDef.cs").Replace("\r\n", "\n");
        Assert.Contains("TauAncientBarkling, TauBramblebackMatriarch,\n            TauAmberSporeling, TauRuneBarkGuardian, TauElderRootGuardian }; // Taurus: append only", src);
        foreach (string id in new[] { "tauAmberSporeling", "tauRuneBarkGuardian", "tauElderRootGuardian" }) Assert.Contains("case \"" + id + "\": return", src);
        string elder = Regex.Match(src, "TauElderRootGuardian = new MonsterDef[^;]*;[^;]*;", RegexOptions.Singleline).Value;
        Assert.Contains("WalkCellPx = 384", elder); Assert.Contains("AttackCellPx = 512", elder); Assert.Contains("SkillBoss = true", elder); Assert.Contains("EnrageRate = 0.87f", elder); Assert.Contains("RenderScale = 3.34f", elder);
        Assert.Contains("Ranged = Rpg.MonsterRangedProfile.AmberSpore", src);
        // the field boss: 3x the toughest common monster (ctor numbers; both are doubled by the farm rule when shown: 2826 vs 942)
        Assert.Equal(3.0, float.Parse(Regex.Match(src, "\"tauBramblebackMatriarch\", ([\\d.]+)f").Groups[1].Value, CultureInfo.InvariantCulture)
            / float.Parse(Regex.Match(src, "\"tauAncientBarkling\", ([\\d.]+)f").Groups[1].Value, CultureInfo.InvariantCulture), 2);
        foreach (string id in new[] { "tauElderRootGuardian", "tauAmberSporeling", "tauRuneBarkGuardian" })
            foreach (string f in new[] { "", "-attack" }) Assert.True(File.Exists(TaurusMapTests.RepoFile("Assets/Resources/Sprites/monsters/taurus/" + id + "/" + id + f + ".png")), id + f);
        foreach (string sk in new[] { "-sweep", "-pulse", "-awaken" }) Assert.True(File.Exists(TaurusMapTests.RepoFile("Assets/Resources/Sprites/monsters/taurus/tauElderRootGuardian/tauElderRootGuardian" + sk + ".png")), sk);
    }
}
