using UnityEngine;
using ZenoniaProto.Net;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// Network face of a monster. Server: packs the state into a snapshot entry. Client: the monster is a proxy (no AI, no hit rules); snapshots set its
    /// position / pose / hp percent and MonsterHit events replay the flash, damage number and sparks.
    /// </summary>
    public partial class Monster
    {
        int proxyAttack = -1;          // attack frame (0..3) from the snapshot, -1 = none
        float exactHp = -1f;           // exact hp of the last MonsterHit event (the snapshot only carries a percent)

        public bool IsProxy { get { return proxy; } }
        public byte NetStateCode { get { return (byte)state; } }
        public byte DefIndex { get { return (byte)MonsterDef.IndexOf(def); } }
        public float FadeProgress { get { return Mathf.Clamp01(deathTimer / FadeMs); } }

        /// <summary>Snapshot entry of this monster (server). State code in the facing byte, attack frame + airborne + status code + mark in the bits byte.</summary>
        public MonsterSnap ToSnap()
        {
            int ai = state == State.Dying ? -1 : AttackIndex();
            int bits = (ai + 1) | (airborneLeft > 0f ? MonsterBits.Airborne : 0) | (status.Has(StatusKind.Mark) ? MonsterBits.Marked : 0);
            bits = MonsterBits.WithStatus(bits, StatusCode(state == State.Dying ? StatusKind.None : status.Dominant));
            return new MonsterSnap
            {
                Id = NetId, X = NetQuantize.Pos(x), Y = NetQuantize.Pos(y), FacingState = NetQuantize.PackFacing(facing, (int)state), AttackBits = (byte)bits,
                HpPct = NetQuantize.HpPct(hp, def.Hp), DeathProgress = NetQuantize.Progress(state == State.Dying ? deathTimer / FadeMs : 0f),
            };
        }

        /// <summary>Client: applies one interpolated snapshot sample.</summary>
        public void ProxyApply(Vector2 pos, int facingDir, int stateCode, int attackIdx, bool airborne, int statusCode, bool marked, byte hpPct, float deathProgress)
        {
            x = pos.x; y = pos.y;
            facing = facingDir & 3;
            state = stateCode >= 0 && stateCode <= (int)State.Dying ? (State)stateCode : State.Idle;
            proxyAttack = attackIdx;
            if (airborne) { if (airborneLeft <= 0f) airborneLeft = LaunchMs; } else airborneLeft = 0f;
            status.ShowReplicated(StatusOfCode(statusCode), marked);
            if (exactHp < 0f || NetQuantize.HpPct(exactHp, def.Hp) != hpPct) { hp = def.Hp * NetQuantize.HpFraction(hpPct); exactHp = -1f; }
            else hp = exactHp;
            if (state == State.Dying) deathTimer = deathProgress * FadeMs;
        }

        /// <summary>Client: a MonsterHit event (replayed at render time): exact hp, flash, number and sparks.</summary>
        public void ProxyHit(int dmg, int hpLeft, Vector2 knockVel, bool crit, bool finisher, bool quiet, bool otherPlayer = false)
        {
            exactHp = hpLeft;
            hp = hpLeft;
            HitFx(dmg, knockVel, crit, finisher, quiet, otherPlayer);
        }

        // Dominant status <-> 3-bit snapshot code (0 none, 1 freeze, 2 stun, 3 burn, 4 poison, 5 slow / chill; the mark has its own bit).
        static int StatusCode(StatusKind k)
        {
            switch (k)
            {
                case StatusKind.Freeze: return 1;
                case StatusKind.Stun: return 2;
                case StatusKind.Burn: return 3;
                case StatusKind.Poison: return 4;
                case StatusKind.Slow:
                case StatusKind.Chill: return 5;
                default: return 0;
            }
        }

        static StatusKind StatusOfCode(int code)
        {
            switch (code)
            {
                case 1: return StatusKind.Freeze;
                case 2: return StatusKind.Stun;
                case 3: return StatusKind.Burn;
                case 4: return StatusKind.Poison;
                case 5: return StatusKind.Chill;
                default: return StatusKind.None;
            }
        }

        void ProxyUpdate(float dtMs)
        {
            if (airborneLeft > 0f) airborneLeft = Mathf.Max(1f, airborneLeft - dtMs);
            transform.position = new Vector3(x, y, 0f);
            Render();
        }
    }
}
