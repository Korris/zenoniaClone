using System.Collections.Generic;
using UnityEngine;
using ZenoniaProto.Net;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// Catching monsters on the authority (docs/pet-system-design.md 6). A living monster: the hero threw a ball at one it hit in the last 10 s, at 50 % HP or less, within
    /// 4 tiles (PetCapture rolls it; a catch removes the monster). A fallen boss: when a map boss (30 s) or Wanted Boss (45 s) dies, KillRewards opens a Corpse here with the
    /// heroes that had a valid share; each account gets one try per death, a catch takes nothing from the others. The monster object fades after 0.7 s, so the corpse is
    /// what a ball is thrown at (the client learns of it by NetEventBus.PetCapturable). The account change (ball spent, pet added) goes through PlayerRpg.ApplyCapture.
    /// </summary>
    public static class PetCaptureService
    {
        sealed class Corpse { public ushort Id; public PetDef Pet; public bool Wanted; public Vector2 Feet; public float UntilMs; public readonly HashSet<PlayerRpg> Eligible = new HashSet<PlayerRpg>(); public readonly HashSet<PlayerRpg> Tried = new HashSet<PlayerRpg>(); }
        static readonly List<Corpse> Corpses = new List<Corpse>();

        public static void ResetState() { Corpses.Clear(); }

        /// <summary>A boss just died: those with a valid share may throw at it for a while (nothing when the boss is no pet).</summary>
        public static void OpenCorpse(Zone zone, Monster m, List<HeroController> winners, IList<int> damage)
        {
            PetDef pet = PetOf(m);
            if (pet == null) return;
            var c = new Corpse { Id = m.NetId, Pet = pet, Wanted = m.IsWanted, Feet = m.Feet, UntilMs = zone.TimeMs + (m.IsWanted ? PetRules.WantedCorpseMs : PetRules.BossCorpseMs) };
            for (int i = 0; i < winners.Count; i++) if (winners[i].Owner != null && AccountRewards.IsValidBossShare(damage[i], m.MaxHp)) c.Eligible.Add(winners[i].Owner);
            if (c.Eligible.Count == 0) return;
            Corpses.Add(c);
            NetEventBus.PetCapturable(m, m.IsWanted, (uint)(m.IsWanted ? PetRules.WantedCorpseMs : PetRules.BossCorpseMs));
            if (NetMode.RendersFx) CaptureMarker.OpenCorpse(m.NetId, m.Feet, (uint)(m.IsWanted ? PetRules.WantedCorpseMs : PetRules.BossCorpseMs), m.IsWanted); // offline / host: no event comes back to this screen
        }

        /// <summary>The pet a monster becomes: the PetDef whose MonsterType is this monster's sheet; null when none (the three Wanted Bosses have none yet).</summary>
        public static PetDef PetOf(Monster m)
        {
            if (m == null) return null;
            foreach (PetDef d in PetConfig.All) if (d.MonsterType != null && d.MonsterType == m.Type) return d;
            return null;
        }

        /// <summary>A hero asks to throw a ball at monster `netId` (living, or a corpse). The answer comes through `done` once the account is stored.</summary>
        public static void Request(HeroController hero, Zone zone, ushort netId, bool rare, System.Action<ActionResult> done)
        {
            PlayerRpg owner = hero != null ? hero.Owner : null;
            if (owner == null || owner.Account == null || zone == null) { done(ActionResult.Fail("Not available")); return; }
            CaptureTarget t;
            Monster live = zone.Ids.Get(netId) as Monster;
            Corpse corpse = CorpseOf(netId, zone.TimeMs);
            if (corpse != null)
            {
                if (corpse.Tried.Contains(owner)) { done(ActionResult.Fail("You already tried that one")); return; }
                t = new CaptureTarget { Pet = corpse.Pet, Boss = true, Wanted = corpse.Wanted, HitRecently = corpse.Eligible.Contains(owner), InRange = Vector2.Distance(hero.Position, corpse.Feet) <= PetRules.CaptureRangeUnits };
            }
            else if (live != null && !live.IsDying)
            {
                t = new CaptureTarget { Pet = PetOf(live), Alive = true, HpShare = live.Hp / Mathf.Max(1f, live.MaxHp), HitRecently = hero.RecentlyHit(live, PetRules.CaptureHitMemoryMs), InRange = Vector2.Distance(hero.Position, live.Center) <= PetRules.CaptureRangeUnits };
                if (live.IsBoss) t.Pet = null; // a living boss is never caught: only its corpse
            }
            else { done(ActionResult.Fail("Too late, it is gone")); return; }
            string why = PetCapture.Refusal(owner.Account.Live, t, rare);
            if (why != null) { done(ActionResult.Fail(why)); return; }
            if (corpse != null) corpse.Tried.Add(owner);
            if (live != null) live.CaptureLocked = true; // nobody else throws while this one is being stored
            PetDef pet = t.Pet;
            bool wasOwned = owner.Account.Live.OwnsPet(pet.Id);
            owner.ApplyCapture(t, rare, outcome =>
            {
                if (live != null) live.CaptureLocked = false;
                if (outcome == CaptureOutcome.Caught)
                {
                    if (live != null && !live.IsDying) live.Captured(hero); // vanishes, no loot, no EXP
                    done(ActionResult.Success(PetCapture.CaughtText(owner.Account.Live, pet, wasOwned)));
                }
                else if (outcome == CaptureOutcome.Missed) done(ActionResult.Fail(pet.Name + " broke free"));
                else done(ActionResult.Fail("Could not throw"));
            });
        }

        static Corpse CorpseOf(ushort id, float nowMs)
        {
            for (int i = Corpses.Count - 1; i >= 0; i--)
            {
                if (Corpses[i].UntilMs < nowMs) { Corpses.RemoveAt(i); continue; }
                if (Corpses[i].Id == id) return Corpses[i];
            }
            return null;
        }
    }
}
