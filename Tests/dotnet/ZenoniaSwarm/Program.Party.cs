using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using LiteNetLib;
using ZenoniaProto.Net;

/// <summary>
/// Crowd party mode (Tools/wanted-boss-crowd-playtest.ps1 -Party): a few swarm bots form a party with a named real player and keep it buffed.
///   --party-with NAME     the real player's character name (its window joins with --net-bot party --party-role joiner, which accepts the first invite)
///   --party-bots 4,9,14   swarm bot indexes in the party; the first one is the leader and invites the others and NAME (again every 15 s until they are in)
/// Party bots stay within 2 tiles of NAME's hero (the Cleric buffs reach 4 tiles, party members only: SkillSupport.Collect) and swing at monsters in reach.
/// Cleric party bots (index % 5 == 4) learn Heal / Spirit Blessing / Divine Aegis to rank 5 with their level-60 points, then cast Spirit Blessing (Atk +%) and
/// Divine Aegis (Def +%) every 24 s each (own timers, see PartyBits; Heal when NAME is below 45 %); the Clerics start evenly staggered so the 12 s buffs overlap.
/// No game rule changes: every request is one the real client sends (PartyRequest, InvAction SkillLearn, the Skill input bit).
/// </summary>
static partial class Program
{
    const int HealIndex = 0, BlessIndex = 1, AegisIndex = 2;
    const float FollowRange = 20f; // two Clerics on either side of NAME stay well inside the 4-tile (64 units) buff circle of each other

    static string partyWith;
    static int[] partyBots = new int[0];
    static ushort partyWithHero;          // hero net id of NAME, from its Spawn (0 = not seen yet)
    static ushort partyWithPlayer;        // player id of NAME (party requests address players)
    static readonly HashSet<string> partyMembers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    static int casts;
    const double CastEvery = 24.0; // per Cleric; with 3 Clerics a fresh 12 s buff lands every 8 s

    static void ReadPartyArgs(string[] args)
    {
        partyWith = Arg(args, "--party-with", null);
        string list = Arg(args, "--party-bots", null);
        if (list != null) partyBots = list.Split(',').Select(s => int.Parse(s.Trim(), CultureInfo.InvariantCulture)).ToArray();
    }

    static bool InParty(Bot b) { return partyWith != null && Array.IndexOf(partyBots, b.Index) >= 0; }
    static bool IsLeader(Bot b) { return partyWith != null && partyBots.Length > 0 && partyBots[0] == b.Index; }
    static bool IsCleric(Bot b) { return b.Index % 5 == 4; }

    static void SendParty(Bot b, PartyOp op, ushort target)
    {
        new PartyRequestMsg { Op = op, Target = target }.Write(b.W);
        b.Peer.Send(b.W, NetProtocol.ChannelReliable, DeliveryMethod.ReliableOrdered);
    }

    /// <summary>After Welcome: a Cleric party bot queues 5 ranks each of Heal, Spirit Blessing and Divine Aegis (prerequisites in that order).</summary>
    static void OnPartyBotJoined(Bot b)
    {
        if (!InParty(b) || !IsCleric(b)) return;
        for (int s = HealIndex; s <= AegisIndex; s++) for (int r = 0; r < 5; r++) b.LearnQueue.Enqueue((byte)s);
    }

    static void OnSpawn(Bot b, SpawnMsg m)
    {
        if (m.Kind == EntityKind.Hero) b.HeroNames[m.Id] = m.Name;
        if (m.Kind == EntityKind.Hero && partyWith != null && string.Equals(m.Name, partyWith, StringComparison.OrdinalIgnoreCase)) { partyWithHero = m.Id; partyWithPlayer = m.PlayerId; }
    }

    static void OnPartyNotice(Bot b, PartyNoticeMsg m)
    {
        switch (m.Kind)
        {
            case PartyNoticeKind.InviteIn: // invites only ever come from the leader bot: accept
                SendParty(b, PartyOp.Accept, m.Other);
                break;
            case PartyNoticeKind.MemberJoined:
                if (IsLeader(b) && partyMembers.Add(m.Name)) Console.WriteLine("party: " + m.Name + " joined (" + (partyMembers.Count + 1) + " in the party)");
                break;
            case PartyNoticeKind.MemberLeft:
            case PartyNoticeKind.MemberKicked:
                if (IsLeader(b) && partyMembers.Remove(m.Name)) Console.WriteLine("party: " + m.Name + " left");
                break;
            case PartyNoticeKind.OutClosed: // the leader's own view of an accepted invite
                if (IsLeader(b) && m.Detail == (byte)PartyInviteEnd.Accepted && partyMembers.Add(m.Name)) Console.WriteLine("party: " + m.Name + " accepted (" + (partyMembers.Count + 1) + " in the party)");
                break;
            case PartyNoticeKind.Failed:
                if (IsLeader(b) && m.Detail == (byte)PartyFail.TargetInParty && !string.IsNullOrEmpty(m.Name)) { if (partyMembers.Add(m.Name)) Console.WriteLine("party: " + m.Name + " is in the party (" + (partyMembers.Count + 1) + ")"); }
                else if (IsLeader(b)) Console.WriteLine("party: request refused (" + (PartyFail)m.Detail + ") " + m.Name);
                break;
        }
    }

    static void OnSelfState(Bot b, SelfStateMsg m)
    {
        b.Mp = m.Mp; b.MaxMp = m.MaxMp;
        if (m.SkillCds != null) for (int i = 0; i < b.Cds.Length && i < m.SkillCds.Length; i++) b.Cds[i] = m.SkillCds[i];
        b.BuffCount = m.Buffs != null ? m.Buffs.Length : 0;
    }

    /// <summary>Once per pump for every joined bot: skill learning and the leader's invites. Reliable sends, not part of the input frame.</summary>
    static void PartyChores(Bot b, double now)
    {
        if (b.LearnQueue.Count > 0 && now >= b.NextLearn)
        {
            NetMessageWriter.InvAction(b.W, new InvActionMsg { ReqId = (uint)(100 + b.LearnQueue.Count), Op = InvOp.SkillLearn, ItemUid = "", Slot = b.LearnQueue.Dequeue() });
            b.Peer.Send(b.W, NetProtocol.ChannelReliable, DeliveryMethod.ReliableOrdered);
            b.NextLearn = now + 0.25;
        }
        if (!IsLeader(b) || now < b.NextInvite) return;
        b.NextInvite = now + 15.0;
        foreach (Bot other in partyBotsList)
            if (other != b && other.Joined && !partyMembers.Contains(other.Name)) SendParty(b, PartyOp.Invite, other.PlayerId);
        if (partyWithPlayer != 0 && !partyMembers.Contains(partyWith)) SendParty(b, PartyOp.Invite, partyWithPlayer);
    }

    static readonly List<Bot> partyBotsList = new List<Bot>();

    /// <summary>Input frame of a party bot: follow NAME, cast the Cleric buffs when they are off cooldown, else swing at a monster in reach.</summary>
    static ushort PartyBits(Bot b, double now)
    {
        ushort bits = 0;
        if (b.MyHpPct > 0 && b.MyHpPct < 55 && now >= b.NextPotion) { bits |= InputBits.HpPotion; b.NextPotion = now + 1.2; }
        HeroSnap? lead = null;
        foreach (HeroSnap h in b.Snap.Heroes) if (h.Id == partyWithHero && partyWithHero != 0) { lead = h; break; }
        // Buffs first (Self-targeted: they need no position, only the party within 4 tiles). Own timers, not just SelfState: dev-start.json's noCooldowns
        // leaves the cooldown at 0 and a press every 0.4 s would spend all the MP on one buff. Each Cleric casts both every CastEvery s, the Clerics evenly
        // staggered, so a fresh 12 s buff lands every CastEvery / Clerics s. Measured: one Cleric every 16 s lost ~0.9 MP/s net (rank-5 costs), so 3 Clerics x 24 s
        // stay about even with the 2.2 MP/s regen. Heal only when NAME is below 45 %.
        if (IsCleric(b) && b.LearnQueue.Count == 0 && b.CastFrom > 0 && now >= b.CastFrom && now >= b.NextCastTry)
        {
            int skill = -1;
            if (now >= b.NextBless && b.Cds[BlessIndex] == 0) skill = BlessIndex;
            else if (now >= b.NextAegis && b.Cds[AegisIndex] == 0) skill = AegisIndex;
            else if (lead.HasValue && lead.Value.HpPct > 0 && lead.Value.HpPct < 45 && now >= b.NextHeal && b.Cds[HealIndex] == 0) skill = HealIndex;
            if (skill >= 0)
            {
                b.NextCastTry = now + 0.5;
                if (skill == BlessIndex) b.NextBless = now + CastEvery;
                else if (skill == AegisIndex) b.NextAegis = now + CastEvery;
                else b.NextHeal = now + 3.0;
                casts++;
                return InputBits.WithSkill(bits, skill);
            }
        }
        if (now < b.StuckUntil) return (ushort)(bits | b.Detour);
        ushort step = 0;
        if (lead.HasValue && b.HasPos)
        {
            float tx = NetQuantize.Pos(lead.Value.X) - b.X, ty = NetQuantize.Pos(lead.Value.Y) - b.Y;
            b.LeadDist = (float)Math.Sqrt(tx * tx + ty * ty);
            if (tx * tx + ty * ty > FollowRange * FollowRange) step = StepTo(tx, ty);
        }
        else if (b.HasPos) { b.LeadDist = -1f; step = StepTo(goalX - b.X, goalY - b.Y); } // NAME not in sight yet: wait at the boss
        if (step != 0)
        {
            if (now >= b.StuckCheck) // same wall slide as the crowd: no progress in 0.8 s = a random side step for a moment
            {
                bool stuck = b.HasPos && Math.Abs(b.X - b.StuckX) + Math.Abs(b.Y - b.StuckY) < 4f;
                b.StuckX = b.X; b.StuckY = b.Y; b.StuckCheck = now + 0.8;
                if (stuck) { b.Detour = (ushort)(1 << brainRng.Next(0, 4)); b.StuckUntil = now + 0.5 + brainRng.NextDouble(); return (ushort)(bits | b.Detour); }
            }
            return (ushort)(bits | step);
        }
        // a monster in reach (the boss, usually): swing without leaving NAME
        int cls = b.Index % 5;
        float reach = cls == 1 || cls == 2 || cls == 4 ? 45f : 28f;
        foreach (MonsterSnap m in b.Monsters)
        {
            float dx = NetQuantize.Pos(m.X) - b.X, dy = NetQuantize.Pos(m.Y) - b.Y;
            if (m.DeathProgress == 0 && dx * dx + dy * dy <= reach * reach && now >= b.NextAttack) { b.NextAttack = now + 0.3; return (ushort)(bits | InputBits.Attack); }
        }
        return bits;
    }

    static string PartyReport()
    {
        if (partyWith == null) return "";
        string clerics = string.Join(" ", partyBotsList.Where(IsCleric).Select(c => c.Name + " dist " + c.LeadDist.ToString("F0", CultureInfo.InvariantCulture) + " mp " + c.Mp + "/" + c.MaxMp + " cd " + c.Cds[BlessIndex] + "/" + c.Cds[AegisIndex] + " buffs " + c.BuffCount));
        return ", party " + (partyMembers.Count + 1) + " (" + string.Join(",", partyMembers) + "), " + partyWith + (partyWithHero != 0 ? " seen" : " not seen") + ", buff casts " + casts + ", " + clerics;
    }
}
