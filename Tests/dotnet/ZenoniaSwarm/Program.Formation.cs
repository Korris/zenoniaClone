using System;
using ZenoniaProto;
using ZenoniaProto.Net;

/// <summary>
/// --formation (with --goal): the crowd stands in rings around the target monster instead of piling onto its feet, like the target picture
/// docs/reference/ro-soft-fantasy-5-class-redesign/40-players-new-classes-vs-mire-hydra-queen.png. Each bot owns one slot, spread evenly by angle within
/// its ring (360 degrees), and walks back to it when a lunge or a knockback pushed it off; at the slot it turns to the boss (one direction tap,
/// only when its replicated facing is wrong, so it never steps) and swings / shoots from there.
///   ring 1  melee (Warrior 0, Assassin 3): 24 units outside the hit box (their swing box reaches 30-34 ahead)
///   ring 2  Wizard 1, Cleric 4: 48 units out (bolt range 60 from the body)
///   ring 3  Mechanic 2: 66 units out (bullet range 82)
/// Rings are ellipses around the hit box (wanted boss 43 x 25 units, MonsterDef.WantedHitW / WantedHitH): wider than deep, as on screen.
/// A slot on a solid tile (pillar, prop footprint; --map rows) slides along its ring to the nearest open angle.
/// </summary>
static partial class Program
{
    static bool formation;
    static int botTotal;
    const float BossHalfW = 43f * 0.5f, BossHitH = 25f; // MonsterDef.WantedHitW / WantedHitH (not compiled into the swarm)
    const float SlotEnter = 5f, SlotLeave = 14f;        // units: arrive within 5, walk back once pushed beyond 14

    static int Ring(int cls) { return cls == 0 || cls == 3 ? 0 : cls == 2 ? 2 : 1; }
    static float RingOffset(int ring) { return ring == 0 ? 24f : ring == 1 ? 48f : 66f; }

    /// <summary>Rank of bot `index` among the bots of its ring, and how many bots share that ring (classes cycle by index % 5).</summary>
    static void RingRank(int index, out int rank, out int count)
    {
        int ring = Ring(index % 5);
        rank = 0; count = 0;
        for (int i = 0; i < botTotal; i++)
        {
            if (Ring(i % 5) != ring) continue;
            if (i < index) rank++;
            count++;
        }
    }

    /// <summary>World point of the bot's slot around a monster standing at (mx, my) (feet).</summary>
    static void SlotOf(Bot b, float mx, float my, out float sx, out float sy)
    {
        int ring = Ring(b.Index % 5), rank, count;
        RingRank(b.Index, out rank, out count);
        float cx = mx, cy = my + BossHitH * 0.5f;                       // hit box centre
        float rx = BossHalfW + RingOffset(ring), ry = BossHitH * 0.5f + RingOffset(ring) * 0.8f;
        double start = ring * 0.41;                                     // rings out of step, so nobody stands right behind another
        double step = 2.0 * Math.PI / Math.Max(1, count);
        for (int tryNo = 0; tryNo < 12; tryNo++)
        {
            double a = start + step * rank + (tryNo == 0 ? 0 : (tryNo % 2 == 1 ? 1 : -1) * ((tryNo + 1) / 2) * 0.12);
            sx = cx + (float)Math.Cos(a) * rx; sy = cy + (float)Math.Sin(a) * ry;
            if (Walkable(sx, sy)) return;
        }
        double a0 = start + step * rank;
        sx = cx + (float)Math.Cos(a0) * rx; sy = cy + (float)Math.Sin(a0) * ry;
    }

    static bool Walkable(float x, float y)
    {
        if (flowMap == null) return true;
        int tx = TileX(flowMap, x), ty = TileY(flowMap, y);
        if (tx < 0 || ty < 0 || tx >= flowMap.Width || ty >= flowMap.Height) return false;
        return flowDist[ty * flowMap.Width + tx] >= 0;
    }

    /// <summary>Formation brain for one frame against the live target monster `m`.</summary>
    static ushort FormationBits(Bot b, MonsterSnap m, double now)
    {
        float mx = NetQuantize.Pos(m.X), my = NetQuantize.Pos(m.Y);
        float sx, sy;
        SlotOf(b, mx, my, out sx, out sy);
        float dx = sx - b.X, dy = sy - b.Y, d = (float)Math.Sqrt(dx * dx + dy * dy);
        if (b.AtSlot ? d > SlotLeave : d > SlotEnter) { b.AtSlot = false; return StepTo(dx, dy); }
        b.AtSlot = true;
        if (now < b.NextAttack) return 0;
        b.NextAttack = now + 0.3;
        // Face the hit box centre along its dominant axis (the sim's 4-dir facing); the client then turns the view to the 8-way direction of the boss.
        float tx = mx - b.X, ty = my + BossHitH * 0.5f - b.Y;
        int want = Math.Abs(tx) >= Math.Abs(ty) ? (tx > 0f ? 3 : 2) : (ty > 0f ? 1 : 0); // 0 down, 1 up, 2 left, 3 right
        ushort turn = 0;
        if (b.Facing != want) turn = want == 0 ? InputBits.Down : want == 1 ? InputBits.Up : want == 2 ? InputBits.Left : InputBits.Right;
        return (ushort)(InputBits.Attack | turn); // direction + attack in one frame: the hero turns and starts the swing without stepping
    }
}
