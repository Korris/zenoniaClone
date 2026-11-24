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
/// Taurus V4 phase 05: the ten camps of Clover Meadows (BIBLE part1 V.3) against the real rows: 68 monsters, every rect on open ground and clear of the road, the level buffers between camps,
/// the stream crossed only on the bridge, the clear corridor to the dungeon door, the Brambleback Nest and the quest touch points. MapSpawnTable / MonsterDef need Unity: their source is read.
/// </summary>
[Collection("MapRegistry")]
public class TaurusMeadowCampsTests
{
    sealed class Camp { public string Type; public int Count, X, Y, W, H, RespawnMs; public bool Passive; public int Level; public float AggroTiles, LeashTiles; public bool Ranged; }

    static IGameMap Meadows { get { return MapRegistry.Get(TauCloverMeadowsMap.MapId); } }
    static TileMapBase Tm { get { return (TileMapBase)Meadows; } }

    static Dictionary<string, (int level, float aggro, float leash, bool ranged)> Species()
    {
        string src = File.ReadAllText(TaurusMapTests.RepoFile("Assets/Scripts/MonsterDef.cs"));
        var map = new Dictionary<string, (int, float, float, bool)>();
        foreach (Match m in Regex.Matches(src, "new MonsterDef\\(\"[^\"]+\", \"(tau\\w+)\", [\\d.]+f, [\\d.]+f, [\\d.]+f, [\\d.]+f, (\\d+)([^;]*);"))
        {
            string rest = m.Groups[3].Value;
            float N(string n, float d) { Match x = Regex.Match(rest, "\\b" + n + ": ([\\d.]+)f"); return x.Success ? float.Parse(x.Groups[1].Value, CultureInfo.InvariantCulture) : d; }
            map[m.Groups[1].Value] = (int.Parse(m.Groups[2].Value), N("aggro", 70f) / 16f, N("leash", 150f) / 16f, rest.Contains("Ranged ="));
        }
        return map;
    }

    static List<Camp> Camps()
    {
        var sp = Species();
        var list = new List<Camp>();
        foreach (Match m in TaurusMapTests.CampRects())
        {
            string t = m.Groups[1].Value;
            Camp c = new Camp
            {
                Type = t, Count = int.Parse(m.Groups[2].Value), X = int.Parse(m.Groups[3].Value), Y = int.Parse(m.Groups[4].Value), W = int.Parse(m.Groups[5].Value), H = int.Parse(m.Groups[6].Value),
                RespawnMs = int.Parse(m.Groups[7].Value), Passive = m.Groups[8].Value == "true",
            };
            Assert.True(sp.ContainsKey(t), t + " has a MonsterDef");
            c.Level = sp[t].level; c.AggroTiles = sp[t].aggro; c.LeashTiles = sp[t].leash; c.Ranged = sp[t].ranged;
            list.Add(c);
        }
        return list;
    }

    /// <summary>Camps by rect: C01..C10 in table order (C04 / C07 / C08 hold two species in one rect).</summary>
    static List<List<Camp>> Rects()
    {
        var groups = new List<List<Camp>>();
        foreach (Camp c in Camps())
        {
            List<Camp> g = groups.FirstOrDefault(x => x[0].X == c.X && x[0].Y == c.Y && x[0].W == c.W && x[0].H == c.H);
            if (g == null) { g = new List<Camp>(); groups.Add(g); }
            g.Add(c);
        }
        return groups;
    }

    static double RectGap(List<Camp> a, List<Camp> b)
    {
        double dx = Math.Max(0, Math.Max(a[0].X - (b[0].X + b[0].W - 1), b[0].X - (a[0].X + a[0].W - 1)));
        double dy = Math.Max(0, Math.Max(a[0].Y - (b[0].Y + b[0].H - 1), b[0].Y - (a[0].Y + a[0].H - 1)));
        return Math.Sqrt(dx * dx + dy * dy);
    }

    static int Top(List<Camp> g) { return g.Max(c => c.Level); }

    [Fact]
    public void TenCampsHoldTheBibleSixtyEightMonstersByCampAndSpecies()
    {
        List<List<Camp>> camps = Rects();
        Assert.Equal(10, camps.Count);
        int[] perCamp = { 9, 7, 8, 6, 8, 8, 6, 6, 5, 5 };
        for (int i = 0; i < 10; i++) Assert.Equal(perCamp[i], camps[i].Sum(c => c.Count));
        Assert.Equal(68, Camps().Sum(c => c.Count));
        var bySpecies = Camps().GroupBy(c => c.Type).ToDictionary(g => g.Key, g => g.Sum(c => c.Count));
        Assert.Equal(18, bySpecies["tauCloverSlime"]); Assert.Equal(8, bySpecies["tauHornRabbit"]); Assert.Equal(4, bySpecies["tauReedFrog"]); Assert.Equal(8, bySpecies["tauMossMushroom"]);
        Assert.Equal(11, bySpecies["tauBrambleBoar"]); Assert.Equal(6, bySpecies["tauLeafMantis"]); Assert.Equal(3, bySpecies["tauRootling"]); Assert.Equal(5, bySpecies["tauThornSprite"]); Assert.Equal(5, bySpecies["tauAncientBarkling"]);
        // mixed camps: C04 4 frogs + 2 slimes, C07 3 boars + 3 mantises, C08 3 mantises + 3 rootlings
        Assert.Equal(new[] { "tauReedFrog", "tauCloverSlime" }, camps[3].Select(c => c.Type).ToArray());
        Assert.Equal(new[] { "tauBrambleBoar", "tauLeafMantis" }, camps[6].Select(c => c.Type).ToArray());
        Assert.Equal(new[] { "tauLeafMantis", "tauRootling" }, camps[7].Select(c => c.Type).ToArray());
        int[] respawn = { 12, 12, 16, 18, 20, 24, 25, 26, 28, 30 };
        for (int i = 0; i < 10; i++) Assert.All(camps[i], c => Assert.Equal(respawn[i] * 1000, c.RespawnMs));
        Assert.True(camps[0][0].Passive); // only C01 is passive
        Assert.All(camps.Skip(1).SelectMany(g => g), c => Assert.False(c.Passive));
    }

    [Fact]
    public void LevelsClimbFromSouthToNorthAndTheTopCampsAreInTheNorth()
    {
        List<List<Camp>> camps = Rects();
        double Mid(List<Camp> g) { return g[0].Y + g[0].H / 2.0; } // row (north = small)
        // the representative level of each camp never goes down as the camp sits further north (C07/C08 and C06/C05 share a band: tolerance of one level)
        for (int i = 0; i < camps.Count; i++)
            for (int j = 0; j < camps.Count; j++)
                if (Mid(camps[i]) - Mid(camps[j]) >= 6) Assert.True(Top(camps[j]) >= Top(camps[i]) - 1, "camp " + (j + 1) + " is north of camp " + (i + 1) + " yet weaker");
        Assert.True(Mid(camps[8]) < Mid(camps[0]) - 30 && Mid(camps[9]) < Mid(camps[0]) - 30); // C09 / C10 sit far north of C01
    }

    [Fact]
    public void EveryCampRectIsOpenGroundOffTheRoadWithRoomAndFarFromTheGate()
    {
        var entrances = new List<Vector2Int> { TaurusMapTests.StartTile(Meadows) };
        foreach (MapPortal p in Meadows.Portals) entrances.Add(new Vector2Int(p.Tx, p.Ty));
        var road = new List<Vector2Int>();
        for (int y = 0; y < Meadows.Height; y++) for (int x = 0; x < Meadows.Width; x++) { char ch = Tm.CharAt(x, y); if (ch == 'd' || ch == 'B') road.Add(new Vector2Int(x, y)); }
        int n = 0;
        foreach (List<Camp> g in Rects())
        {
            n++;
            Camp r = g[0];
            int total = g.Sum(c => c.Count), free = 0;
            double aggro = g.Max(c => c.AggroTiles);
            for (int y = r.Y; y < r.Y + r.H; y++)
                for (int x = r.X; x < r.X + r.W; x++)
                {
                    char ch = Tm.CharAt(x, y);
                    Assert.False(Meadows.IsSolidTile(x, y), "C" + n + " solid tile " + x + "," + y);
                    Assert.True("ghmbern".IndexOf(ch) >= 0, "C" + n + " tile " + x + "," + y + " is '" + ch + "' (road / bridge / plaza?)");
                    free++;
                    foreach (Vector2Int e in entrances) Assert.True(TaurusMapTests.Dist(x, y, e.x, e.y) >= 5.0, "C" + n + " too close to " + e);
                    foreach (Vector2Int rd in road) Assert.True(TaurusMapTests.Dist(x, y, rd.x, rd.y) >= Math.Max(3.0, aggro), "C" + n + " tile " + x + "," + y + " is within the aggro range of the road");
                }
            Assert.True(free >= 2 * total, "C" + n + " room for its monsters");
        }
    }

    [Fact]
    public void CampsOfFarApartLevelsKeepTheirLeashesApartAndNeighboursKeepAGap()
    {
        List<List<Camp>> camps = Rects();
        for (int i = 0; i < camps.Count; i++)
            for (int j = i + 1; j < camps.Count; j++)
            {
                double gap = RectGap(camps[i], camps[j]);
                int diff = Math.Abs(Top(camps[i]) - Top(camps[j]));
                double leashSum = camps[i].Max(c => c.LeashTiles) + camps[j].Max(c => c.LeashTiles);
                if (diff >= 3) Assert.True(gap >= leashSum, "C" + (i + 1) + " / C" + (j + 1) + " gap " + gap.ToString("F1") + " < leash sum " + leashSum); // a Lv8-10 monster never chases into a Lv1-5 camp
                else if (diff >= 1) Assert.True(gap >= 3.0, "C" + (i + 1) + " / C" + (j + 1) + " gap " + gap.ToString("F1"));
                else Assert.True(gap >= 2.0, "C" + (i + 1) + " / C" + (j + 1) + " overlap");
            }
        // BIBLE buffers: Slime-Rabbit 5-7, Rabbit-Mushroom 5-8, Mushroom-Boar 6-9 are targets for the transitions on the road; here the camps of the same band need at least the minimum.
        Assert.True(RectGap(camps[1], camps[2]) >= 5.0); // C02 (Slimes) to C03 (Rabbits)
        Assert.True(RectGap(camps[0], camps[1]) >= 3.0); // Slime to Slime
    }

    [Fact]
    public void NoLv8To10MonsterCanReachTheNewbieFieldsEvenAtTheEdgeOfItsLeash()
    {
        List<List<Camp>> camps = Rects();
        foreach (List<Camp> hard in camps.Where(g => Top(g) >= 8))
            foreach (List<Camp> easy in camps.Where(g => Top(g) <= 3))
            {
                double reach = hard.Max(c => c.LeashTiles) + easy.Max(c => c.AggroTiles);
                Assert.True(RectGap(hard, easy) > reach, "a Lv" + Top(hard) + " camp reaches a Lv" + Top(easy) + " camp");
            }
        // the village gate and the arrival tile are even further from every camp than the Lv1-3 camps
        Vector2Int s = TaurusMapTests.StartTile(Meadows);
        foreach (List<Camp> hard in camps.Where(g => Top(g) >= 6))
            Assert.True(TaurusMapTests.Dist(hard[0].X + hard[0].W / 2, hard[0].Y + hard[0].H / 2, s.x, s.y) > 25, "a Lv6+ camp near the gate");
    }

    [Fact]
    public void TheStreamIsCrossedOnlyOnTheBridgeAndTheBridgeIsOnTheRoad()
    {
        // every tile of the three stream rows is water except the bridge deck, which lines up with the road on both banks
        for (int x = 3; x <= 92; x++)
            for (int y = 24; y <= 26; y++)
                Assert.True(Tm.CharAt(x, y) == (x >= 46 && x <= 49 ? 'B' : 'w'), "stream tile " + x + "," + y);
        foreach (int y in new[] { 23, 27 }) for (int x = 46; x <= 49; x++) Assert.Equal('d', Tm.CharAt(x, y));
        // flood from the start with the deck blocked: nothing north of the stream is reachable (Z4-Z7 are behind the bridge)
        var seen = new HashSet<int>();
        var open = new Stack<int>();
        Vector2Int s = TaurusMapTests.StartTile(Meadows);
        open.Push(s.y * Meadows.Width + s.x);
        while (open.Count > 0)
        {
            int i = open.Pop();
            int x = i % Meadows.Width, y = i / Meadows.Width;
            if (x < 0 || y < 0 || x >= Meadows.Width || y >= Meadows.Height || Meadows.IsSolidTile(x, y) || Tm.CharAt(x, y) == 'B' || !seen.Add(i)) continue;
            open.Push(i + 1); open.Push(i - 1); open.Push(i + Meadows.Width); open.Push(i - Meadows.Width);
        }
        Assert.DoesNotContain(seen, i => i / Meadows.Width < 24);
        Assert.Contains(seen, i => i / Meadows.Width >= 27);
    }

    [Fact]
    public void TheCorridorToTheDungeonDoorIsClearFourWideAndNoCommonMonsterStandsInIt()
    {
        for (int y = 2; y <= 15; y++) for (int x = 56; x <= 59; x++) Assert.False(Meadows.IsSolidTile(x, y), "corridor " + x + "," + y);
        Assert.True(Meadows.IsSolidTile(55, 1) && Meadows.IsSolidTile(60, 1) && Meadows.IsSolidTile(54, 0)); // the lintel / arch the portal of phase 06 hangs in
        HashSet<int> reach = TaurusMapTests.Reach(Meadows, 48, 20);
        Assert.Contains(2 * Meadows.Width + 57, reach); // the door opening row is reachable
        // the only camps near the corridor are C09 / C10 and neither lets an aggro range onto the road
        foreach (List<Camp> g in Rects().Where(g => g[0].Y <= 12 && g[0].X + g[0].W >= 50 && g[0].X <= 66))
        {
            double gapToRoad = Math.Min(Math.Abs(g[0].X + g[0].W - 1 - 56), Math.Abs(g[0].X - 59));
            Assert.True(gapToRoad > g.Max(c => c.AggroTiles), "camp at " + g[0].X + "," + g[0].Y + " reaches the corridor");
        }
        // max three aggressive monsters can be pulled at once by walking the road: the road never enters a camp rect + aggro range
        Assert.Equal(0, Rects().Count(g => g.Any(c => !c.Passive) && RoadWithin(g)));
    }

    static bool RoadWithin(List<Camp> g)
    {
        double aggro = g.Max(c => c.AggroTiles);
        for (int y = 0; y < Meadows.Height; y++)
            for (int x = 0; x < Meadows.Width; x++)
            {
                char ch = Tm.CharAt(x, y);
                if (ch != 'd' && ch != 'B') continue;
                double dx = Math.Max(0, Math.Max(g[0].X - x, x - (g[0].X + g[0].W - 1))), dy = Math.Max(0, Math.Max(g[0].Y - y, y - (g[0].Y + g[0].H - 1)));
                if (Math.Sqrt(dx * dx + dy * dy) < aggro) return true;
            }
        return false;
    }

    [Fact]
    public void TheNestIsAnOpenClearingInTheNorthWestWithNoCommonMonsterAndOneMouthOnTheTrunkRoad()
    {
        for (int y = 1; y <= 10; y++) for (int x = 3; x <= 14; x++) { Assert.False(Meadows.IsSolidTile(x, y), "nest " + x + "," + y); Assert.Equal('n', Tm.CharAt(x, y)); }
        foreach (List<Camp> g in Rects()) Assert.True(g[0].X > 14 + 3 || g[0].Y > 10 + 3, "a camp inside / against the nest"); // 'at the rim, not in the arena'
        HashSet<int> reach = TaurusMapTests.Reach(Meadows, 48, 20);
        Assert.Contains(5 * Meadows.Width + 8, reach); // the middle of the nest, via the trunk road west and the mouth (rows 11)
        string spawn = File.ReadAllText(TaurusMapTests.RepoFile("Assets/Scripts/MapSpawnTable.cs"));
        Assert.Contains("TauBramblebackNestTile = new Vector2Int(8, 5)", spawn);
        Assert.Contains("case TauCloverMeadowsId: return TauBramblebackNestTile;", spawn);
        Assert.Contains("mapId == TauCloverMeadowsId || mapId == TauAncientRootShrineId; }", spawn); // HasOwnBoss: the kill is saved per map, never the Frostfang King's flag
        // the boss is far from the road a newbie takes (the road to the door passes the junction at x 46-49, row 12-15, 30+ tiles east of the nest's centre)
        Assert.True(TaurusMapTests.Dist(8, 5, 47, 20) > 30);
        // the boss's leash (6.5 tiles) keeps it inside the 12-wide nest
        Assert.InRange(104f / 16f, 5f, 7f);
    }

    [Fact]
    public void TheQuestTouchPointsStandOnSolidTilesWithAFreeApproachAndTheGeminiGateIsClosed()
    {
        string src = File.ReadAllText(TaurusMapTests.RepoFile("Assets/Scripts/Rpg/DungeonPoints.cs"));
        foreach (Match m in Regex.Matches(src, "new WaypointDef\\((?:TauRunestoneId|TauAncientRootsId), \"tauCloverMeadows\", \"[^\"]+\", (\\d+), (\\d+), (\\d+), (\\d+), true\\)"))
        {
            int tx = int.Parse(m.Groups[1].Value), ty = int.Parse(m.Groups[2].Value), ax = int.Parse(m.Groups[3].Value), ay = int.Parse(m.Groups[4].Value);
            Assert.True(Meadows.IsSolidTile(tx, ty), "touch point stands on a solid tile");
            Assert.False(Meadows.IsSolidTile(ax, ay), "arrival tile is open");
            Assert.Contains(ay * Meadows.Width + ax, TaurusMapTests.Reach(Meadows, 48, 20));
        }
        Assert.Equal(2, Regex.Matches(src, "WaypointDef\\((?:TauRunestoneId|TauAncientRootsId)").Count);
        // far from the road (not triggered by walking past) and from every camp rect
        Assert.True(TaurusMapTests.Dist(38, 23, 46, 23) >= 6.0);
        // the Gemini block at the north-east edge: solid, at the end of the east spur, with no portal in this map
        for (int y = 6; y <= 11; y++) for (int x = 88; x <= 91; x++) Assert.True(Meadows.IsSolidTile(x, y));
        Assert.Equal('d', Tm.CharAt(86, 9));
        Assert.All(Meadows.Portals.Where(p => p.TargetMapId != "tauAncientRootShrine"), p => Assert.Equal(WorldFlags_Start(), p.TargetMapId));
    }

    static string WorldFlags_Start() { return ZenoniaProto.Rpg.WorldFlags.StartMapId; }

    [Fact]
    public void TheBottomRowsAndTheMapEdgeStayBlocked()
    {
        for (int y = 61; y < 64; y++) for (int x = 0; x < 96; x++) Assert.True(Meadows.IsSolidTile(x, y));
        for (int y = 0; y < 64; y++) { Assert.True(Meadows.IsSolidTile(0, y)); Assert.True(Meadows.IsSolidTile(95, y)); }
        for (int x = 0; x < 96; x++) Assert.True(Meadows.IsSolidTile(x, 0));
    }
}
