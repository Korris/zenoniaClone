using System.Collections.Generic;
using UnityEngine;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// The Pet Companion beside a hero, view only (HeroRenderer.PetId says which; a headless server never creates one). ChatGPT Lần 8 (docs/pet-system-design.md):
    /// a real walker, not a picture sliding after the hero. FOLLOW: rests 1.5 tiles behind the owner, 0.6 to the side, walks (MoveTowards, Walk frames) when further than
    /// 2 tiles from that spot at 110 % of the owner's speed, 160 % when far, teleports past 7 tiles. CHASE / ATTACK (Attack(): the server's PetSkill event): runs to the monster
    /// the owner is fighting, stops at its range, plays the Attack frames (hit on frame 3) and walks back. Facing follows its own movement, idle keeps the last.
    /// Gacha pets draw the monster's own walk / attack sheets (HeroPet.Sheets.cs); Spirit / Imp keep their one picture with a small bob. No HP, no collider, no aggro.
    /// </summary>
    public sealed partial class HeroPet : MonoBehaviour
    {
        enum State { Follow, Chase, Attack, Guard }

        static readonly Dictionary<HeroController, HeroPet> ByHero = new Dictionary<HeroController, HeroPet>();
        static readonly Vector2[] Dirs = { new Vector2(0f, -1f), new Vector2(0f, 1f), new Vector2(-1f, 0f), new Vector2(1f, 0f) }; // facing: down, up, left, right
        const float MaxStepMs = 100f, BobPeriodMs = 1400f, LegacyBob = 2f, SideFlipMs = 400f;

        HeroController hero;
        SpriteRenderer sr, shadow;
        PetDef def;
        string shownId;
        State state;
        Vector2 pos;
        int facing = 0;
        float ageMs, attackMs, stuckMs, guardMs, chaseStallMs, chaseLastDist;
        Monster target;
        PetSkillKind attackKind;
        bool attackBasic, attackFired;

        public static HeroPet Create(Transform parent, HeroController owner)
        {
            if (!NetMode.RendersFx) return null;
            var go = new GameObject("Pet");
            go.transform.SetParent(parent, false);
            var r = go.AddComponent<HeroPet>();
            r.hero = owner;
            r.sr = go.AddComponent<SpriteRenderer>();
            r.sr.sharedMaterial = FeelMaterials.SpriteFlash;
            r.shadow = ProceduralSprites.Child(go.transform, "Shadow", ProceduralSprites.Shadow());
            r.pos = owner.Position;
            ByHero[owner] = r;
            return r;
        }

        /// <summary>The pet flying with hero `h`, or null (View25DWorld stands it up as a billboard).</summary>
        public static HeroPet Of(HeroController h) { HeroPet p; return h != null && ByHero.TryGetValue(h, out p) ? p : null; }

        void OnDestroy() { HeroPet p; if ((object)hero != null && ByHero.TryGetValue(hero, out p) && p == this) ByHero.Remove(hero); } // (object): a destroyed hero still has to leave the table (sprite-rules VFX 11)

        /// <summary>Where this hero's pet is drawn (its projectiles start there); null without a drawn pet.</summary>
        public static Vector2? DrawnAt(HeroController h)
        {
            HeroPet p;
            return h != null && ByHero.TryGetValue(h, out p) && p != null && p.sr.sprite != null ? (Vector2?)(p.pos + new Vector2(0f, p.Height * 0.5f)) : null;
        }

        /// <summary>The server started an attack cycle (NetEventBus.PetSkill): run to `foe` and swing; `kind` picks the FX (PetSkillFx) and `basic` its size.</summary>
        public static void Attack(HeroController h, Monster foe, PetSkillKind kind, bool basic)
        {
            HeroPet p = null;
            if (h == null || foe == null || !ByHero.TryGetValue(h, out p) || p == null || p.sr.sprite == null) { if (PetConfig.LogCycles) Debug.Log("[ZenoniaProto] pet attack ignored: hero " + (h != null) + " foe " + (foe != null) + " pet " + (p != null) + " sprite " + (p != null && p.sr.sprite != null)); return; }
            if (PetConfig.LogCycles) Debug.Log("[ZenoniaProto] pet attack " + kind + " state " + p.state + " dist " + Vector2.Distance(p.pos, foe.Feet).ToString("F0"));
            p.target = foe; p.attackKind = kind; p.attackBasic = basic; p.attackFired = false;
            if (p.state != State.Attack) { p.state = State.Chase; p.chaseStallMs = 0f; p.chaseLastDist = float.MaxValue; }
        }

        float Height { get { return def != null ? def.HeightUnits : 11f; } }
        float OwnerSpeed { get { return Mathf.Max(40f, hero.Rpg != null ? hero.Rpg.Speed : 80f); } }

        void LateUpdate()
        {
            if (hero == null) { Destroy(gameObject); return; }
            HeroRenderer view = hero.View;
            string id = view != null && !hero.IsDead && PetViewPrefs.Shows(hero) ? view.PetId : null; // Settings: other players' pets can be hidden here
            if (id != shownId) Swap(id);
            if (def == null) { sr.enabled = false; shadow.enabled = false; return; }
            float dtMs = Mathf.Min(MaxStepMs, GameClock.Dt * 1000f);
            ageMs += dtMs;
            bool moved = state == State.Attack ? false : state == State.Chase ? Chase(dtMs) : state == State.Guard ? Guard(dtMs) : Follow(dtMs);
            if (state == State.Attack) attackMs += dtMs;
            if (state == State.Attack && attackMs >= PetConfig.AttackAnimMs) { state = State.Guard; guardMs = 0f; } // stays beside its target until the fight is over (ChatGPT 16/11 06:41)
            Draw(moved);
        }

        /// <summary>A pet just put on appears beside the hero; it does not walk in from where the last one stood.</summary>
        void Swap(string id)
        {
            shownId = id;
            def = PetConfig.Get(id);
            LoadArt(def);
            state = State.Follow; target = null;
            pos = hero.Position + RestOffset();
            facing = hero.Facing;
        }

        /// <summary>The resting spot: 1.5 tiles behind the owner (opposite its facing), 0.6 tiles to its left.</summary>
        Vector2 RestOffset()
        {
            Vector2 back = -Dirs[hero.Facing], side = new Vector2(-back.y, back.x);
            return back * PetConfig.FollowBehindUnits + side * PetConfig.FollowSideUnits;
        }

        bool Follow(float dtMs)
        {
            Vector2 dest = hero.Position + RestOffset();
            float toOwner = Vector2.Distance(pos, hero.Position), toDest = Vector2.Distance(pos, dest);
            if (toOwner > PetConfig.TeleportUnits) { pos = hero.Position - Dirs[hero.Facing] * 16f; return false; }
            bool walking = toDest > PetConfig.FollowStopUnits && (toOwner > PetConfig.FollowStartUnits || stuckMs > 0f);
            if (!walking) { stuckMs = 0f; return false; }
            float speed = OwnerSpeed * (toOwner > PetConfig.FollowFarUnits ? PetConfig.ChaseSpeedMul : PetConfig.FollowSpeedMul);
            Step(dest, speed, dtMs);
            stuckMs = 1f; // keeps walking until it reaches the spot, so it does not stop a hair past 2 tiles
            return true;
        }

        /// <summary>Where it stands to hit `foe`: at the monster's side (the side it comes from), a hair in front of its feet, never on top of or behind the body it bites.</summary>
        Vector2 SpotBeside(Vector2 foe)
        {
            float range = def.Melee ? PetConfig.MeleeRangeUnits : PetConfig.RangedRangeUnits;
            return foe + new Vector2(pos.x < foe.x ? -range : range, -2f);
        }

        bool Chase(float dtMs)
        {
            if (target == null || target.IsDying) { state = State.Follow; target = null; return false; }
            float range = def.Melee ? PetConfig.MeleeRangeUnits : PetConfig.RangedRangeUnits;
            Vector2 foe = target.Feet, spot = SpotBeside(foe);
            float dist = Vector2.Distance(pos, spot);
            if (dist <= PetConfig.FollowStopUnits || Vector2.Distance(pos, foe) <= range * 0.6f) { BeginAttack(foe); return false; }
            // Watchdog: a ledge or wall between it and the monster (no pathfinding for a pet) would leave it sliding in place for ever: after 250 ms without progress it appears at the spot.
            if (dist < chaseLastDist - 1f) { chaseLastDist = dist; chaseStallMs = 0f; } else chaseStallMs += dtMs;
            if (chaseStallMs > 250f) { pos = spot; BeginAttack(foe); return false; }
            Step(spot, OwnerSpeed * PetConfig.ChaseSpeedMul, dtMs);
            return true;
        }

        /// <summary>Between swings: stays at the monster's side (steps after it when it moves) while the fight goes on; back to the owner when the target is dead or the owner left (6 tiles) or 4 s passed.</summary>
        bool Guard(float dtMs)
        {
            guardMs += dtMs;
            if (target == null || target.IsDying || guardMs > PetConfig.TargetMemoryMs || Vector2.Distance(hero.Position, target.Center) > PetConfig.RangeUnits) { state = State.Follow; target = null; return false; }
            Vector2 spot = SpotBeside(target.Feet);
            facing = FacingOf(target.Feet - pos);
            if (Vector2.Distance(pos, spot) <= PetConfig.FollowStopUnits) return false;
            Step(spot, OwnerSpeed * PetConfig.FollowSpeedMul, dtMs);
            facing = FacingOf(target.Feet - pos);
            return true;
        }

        void BeginAttack(Vector2 foe)
        {
            state = State.Attack; attackMs = 0f; attackFired = false;
            if (PetConfig.LogCycles) Debug.Log("[ZenoniaProto] pet swings at " + pos.ToString("F0"));
            facing = FacingOf(foe - pos);
            PetSkillFx.Strike(hero, target, attackKind, attackBasic, pos + new Vector2(0f, Height * 0.5f), def.Melee); // ranged: the projectile; melee: the burst on frame 3
        }

        /// <summary>One MoveTowards step along the map (walls slide), facing from the step.</summary>
        void Step(Vector2 dest, float speed, float dtMs)
        {
            Vector2 d = dest - pos;
            float len = d.magnitude, step = speed * dtMs / 1000f;
            if (len < 0.001f) return;
            Vector2 move = len <= step ? d : d / len * step;
            bool bx, by;
            Vector2 next = FrostHollowMap.Move(pos, move.x, move.y, 6f, 4f, out bx, out by);
            if (bx && by && len > step) { pos = dest; return; } // boxed in: no pathfinding for a pet, it just appears where it was going
            pos = next;
            facing = FacingOf(d);
        }

        static int FacingOf(Vector2 d) { return Mathf.Abs(d.x) >= Mathf.Abs(d.y) ? (d.x < 0f ? 2 : 3) : (d.y < 0f ? 0 : 1); }
    }
}
