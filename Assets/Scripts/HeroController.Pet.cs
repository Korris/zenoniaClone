using System.Collections.Generic;
using UnityEngine;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// Pet combat on the authority (offline / zone server), PetConfig + PetRules + ChatGPT Lần 8 (docs/pet-system-design.md 2-3). Only the monster the owner hit in the last
    /// 4 s within 6 tiles is ever a target. The combat pet: a basic attack every 2 s for 10 % of a plain hit and its skill every 20 s; each attack is a cycle, the pet runs to
    /// the target (melee 1.2 tiles, ranged 4) at 160 % of the owner's speed and its Attack animation lands the hit on frame 3 (300 ms); the server times that from the
    /// owner-to-target distance, tells everyone with NetEventBus.PetSkill at the start (HeroPet runs the same picture) and lands the damage through Monster.TakePetHit.
    /// The two support pets cast their skill every 20 s at half strength, without a body (their projectile leaves the owner). Damage grows with the pet's level.
    /// The last monster this hero itself hit is also what a capture ball may be thrown at (RecentlyHit).
    /// </summary>
    public partial class HeroController
    {
        struct PetShot { public Monster Target; public float LeftMs; public int Dmg; public PetSkillKind Kind; public bool Slows, Basic; }

        readonly List<PetShot> petShots = new List<PetShot>(4);
        readonly float[] supportCdMs = new float[2];
        float petCdMs, petBasicMs, petMarkAgeMs, petBusyMs;
        Monster petAuto; // the monster the pets picked themselves while the owner has no valid mark (PickPetTarget)
        Monster petMark, petGuarding; // petGuarding: the mark the combat pet already stands beside (HeroPet Guard state), so the next hit needs no run-up

        PetDef ActivePet { get { return Owner != null && NetMode.IsAuthority && !proxy && !dead ? rpg.Pet : null; } }

        /// <summary>Capture runs (--pet-cd): the skill starts on this cooldown, so a screenshot series can catch the first one.</summary>
        public void DebugPetCooldown(float ms) { petCdMs = ms; }

        /// <summary>This hero's attack hit a monster (Monster.TakeHit; not DoT ticks, not pet hits): it becomes the pets' mark and the capture target.</summary>
        public void PetAttackLanded(Monster m)
        {
            if (m == null || Owner == null || !NetMode.IsAuthority || proxy) return;
            petMark = m;
            petMarkAgeMs = 0f;
        }

        /// <summary>The hero itself hit `m` within `ms` (a capture ball needs 10 s, PetRules.CaptureHitMemoryMs).</summary>
        public bool RecentlyHit(Monster m, int ms) { return m != null && petMark == m && petMarkAgeMs <= ms; }

        /// <summary>A monster took real HP from this hero, who survives it: Shield / Heal skills (combat pet, support pets at half) answer at 70 % HP or less.</summary>
        void PetOwnerHurt()
        {
            if (Owner == null || !NetMode.IsAuthority || proxy || hp > MaxHp * PetConfig.OwnerHpLine) return;
            PetDef pet = ActivePet;
            if (pet != null && petCdMs <= 0f && Guard(pet, 1.0)) { petCdMs = PetConfig.SkillCooldownMs; PetSkillFx.Play(this, null, pet.Skill); NetEventBus.PetSkill(this, null, pet.Skill); }
            for (int i = 0; i < 2; i++)
            {
                PetDef s = rpg.SupportPet(i);
                if (s != null && supportCdMs[i] <= 0f && Guard(s, PetRules.SupportSkillShare)) { supportCdMs[i] = PetConfig.SkillCooldownMs; PetSkillFx.Play(this, null, s.Skill, true); NetEventBus.PetSkill(this, null, s.Skill, true); }
            }
        }

        bool Guard(PetDef pet, double share)
        {
            if (pet.Skill == PetSkillKind.Shield) HeroBuffs.For(this).Add(BuffKind.Shield, Mathf.Max(1f, MaxHp * (float)(pet.SkillPct * share)), PetConfig.ShieldMs);
            else if (pet.Skill == PetSkillKind.Heal) hp = Mathf.Min(MaxHp, hp + Mathf.Max(1f, Mathf.Round(MaxHp * (float)(pet.SkillPct * share))));
            else return false;
            return true;
        }

        /// <summary>The marked monster is still valid: hit by the owner in the last 4 s, alive, within 6 tiles.</summary>
        bool PetMarkValid { get { return petMark != null && !petMark.IsDying && petMarkAgeMs <= PetConfig.TargetMemoryMs && Vector2.Distance(Position, petMark.Center) <= PetConfig.RangeUnits; } }

        /// <summary>
        /// What the pets attack now: the owner's mark while it is valid; otherwise a monster of the pet's own choice (user 16/11: a level 60 hero kills in one hit, so a pet
        /// that waits for the owner's mark never fights): the one it already picked while that lives within 6 tiles of the owner, else the nearest within AutoTargetUnits.
        /// Never a boss / Wanted Boss by itself (the owner starts that fight). Null = nothing to attack.
        /// </summary>
        Monster PickPetTarget()
        {
            if (PetMarkValid) return petMark;
            if (petAuto != null && !petAuto.IsDying && Vector2.Distance(Position, petAuto.Center) <= PetConfig.RangeUnits) return petAuto;
            petAuto = null;
            float best = PetConfig.AutoTargetUnits;
            IReadOnlyList<Monster> monsters = Monster.All;
            for (int i = 0; i < monsters.Count; i++)
            {
                Monster m = monsters[i];
                if (m == null || m.IsDying || m.IsBoss || m.IsWanted) continue;
                float d = Vector2.Distance(Position, m.Center);
                if (d <= best) { best = d; petAuto = m; }
            }
            return petAuto;
        }

        /// <summary>How long the combat pet needs to reach its attack range from beside the owner, then the hit frame.</summary>
        float PetHitDelayMs(PetDef pet, Monster foe)
        {
            float range = pet.Melee ? PetConfig.MeleeRangeUnits : PetConfig.RangedRangeUnits;
            float run = Mathf.Max(0f, Vector2.Distance(Position, foe.Feet) - range);
            return run / (Mathf.Max(40f, rpg.Speed) * PetConfig.ChaseSpeedMul) * 1000f + PetConfig.HitFrameMs;
        }

        /// <summary>One attack cycle on `foe`. support = a support pet's skill (half strength, flies from the owner, no running pet).</summary>
        void StartPetAttack(PetDef pet, Monster foe, PetSkillKind kind, double pct, bool basic, bool support)
        {
            float mul = (float)(PetRules.LevelDamageMul(rpg.PetLevel) * (support ? PetRules.SupportSkillShare : 1.0));
            int dmg = Mathf.Max(1, Mathf.RoundToInt(CombatMath.Damage(rpg.Atk, 1f, foe.Def) * (float)pct * mul)); // a share of a plain hit AFTER defence (ChatGPT 16/11 06:41): defence is never taken twice
            float at = support ? PetConfig.FlightMs : petGuarding == foe ? PetConfig.HitFrameMs : PetHitDelayMs(pet, foe); // already beside its target: only the swing
            if (!support) petGuarding = foe;
            petShots.Add(new PetShot { Target = foe, LeftMs = at, Dmg = dmg, Kind = kind, Slows = kind == PetSkillKind.Ice, Basic = basic });
            if (PetConfig.LogCycles) Debug.Log("[ZenoniaProto] pet cycle " + pet.Id + " " + kind + (basic ? " basic" : " skill") + (foe == petMark ? "" : " (own target)") + " dmg=" + dmg + " lands in " + at.ToString("F0") + " ms, owner-target " + Vector2.Distance(Position, foe.Feet).ToString("F0"));
            if (!support) petBusyMs = at + PetConfig.AttackRecoverMs; // the pet finishes its swing before the next cycle
            PetSkillFx.Play(this, foe, kind, basic, support);
            NetEventBus.PetSkill(this, foe, kind, basic, support);
        }

        /// <summary>Every frame on the authority: cooldowns, the mark's age, cycles that are due, shots landing (a target that died meanwhile is simply missed).</summary>
        void TickPet(float dtMs)
        {
            if (!NetMode.IsAuthority || proxy) return;
            petCdMs = Mathf.Max(0f, petCdMs - dtMs); petBasicMs = Mathf.Max(0f, petBasicMs - dtMs); petBusyMs = Mathf.Max(0f, petBusyMs - dtMs);
            supportCdMs[0] = Mathf.Max(0f, supportCdMs[0] - dtMs); supportCdMs[1] = Mathf.Max(0f, supportCdMs[1] - dtMs);
            petMarkAgeMs += dtMs;
            for (int i = petShots.Count - 1; i >= 0; i--)
            {
                PetShot s = petShots[i];
                s.LeftMs -= dtMs;
                if (s.LeftMs > 0f) { petShots[i] = s; continue; }
                petShots.RemoveAt(i);
                if (s.Target == null || s.Target.IsDying) continue;
                if (s.Slows && !s.Target.IsBoss) s.Target.ApplyStatus(new StatusSpec(StatusKind.Slow, PetConfig.IceSlowPct, PetConfig.IceSlowMs), null); // bosses shrug the slow off
                s.Target.TakePetHit(s.Dmg, this, !s.Basic);
            }
            if (Owner == null || dead || rpg.Pet == null && rpg.SupportPet(0) == null && rpg.SupportPet(1) == null) { petGuarding = null; petAuto = null; return; }
            Monster foe = PickPetTarget();
            if (petGuarding != foe) petGuarding = null;
            if (foe == null) return;
            for (int i = 0; i < 2; i++)
            {
                PetDef s = rpg.SupportPet(i);
                if (s != null && supportCdMs[i] <= 0f && PetConfig.IsAttack(s.Skill)) { supportCdMs[i] = PetConfig.SkillCooldownMs; StartPetAttack(s, foe, s.Skill, s.SkillPct, false, true); }
            }
            PetDef pet = ActivePet;
            if (pet == null || petBusyMs > 0f) return;
            if (petCdMs <= 0f && PetConfig.IsAttack(pet.Skill)) { petCdMs = PetConfig.SkillCooldownMs; StartPetAttack(pet, foe, pet.Skill, pet.SkillPct, false, false); }
            else if (petBasicMs <= 0f) { petBasicMs = PetConfig.BasicAttackMs; StartPetAttack(pet, foe, pet.BasicFx, PetConfig.BasicPct, true, false); }
        }
    }
}
