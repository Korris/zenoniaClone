using UnityEngine;
using ZenoniaProto.Net;

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

        /// <summary>Snapshot entry of this monster (server). State code in the facing byte, attack frame + airborne + chilled in the bits byte.</summary>
        public MonsterSnap ToSnap()
        {
            int ai = state == State.Dying ? -1 : AttackIndex();
            int bits = (ai + 1) | (airborneLeft > 0f ? MonsterBits.Airborne : 0) | (chillLeft > 0f ? MonsterBits.Chilled : 0);
            return new MonsterSnap
            {
                Id = NetId, X = NetQuantize.Pos(x), Y = NetQuantize.Pos(y), FacingState = NetQuantize.PackFacing(facing, (int)state), AttackBits = (byte)bits,
                HpPct = NetQuantize.HpPct(hp, def.Hp), DeathProgress = NetQuantize.Progress(state == State.Dying ? deathTimer / FadeMs : 0f),
            };
        }

        /// <summary>Client: applies one interpolated snapshot sample.</summary>
        public void ProxyApply(Vector2 pos, int facingDir, int stateCode, int attackIdx, bool airborne, bool chilled, byte hpPct, float deathProgress)
        {
            x = pos.x; y = pos.y;
            facing = facingDir & 3;
            state = stateCode >= 0 && stateCode <= (int)State.Dying ? (State)stateCode : State.Idle;
            proxyAttack = attackIdx;
            if (airborne) { if (airborneLeft <= 0f) airborneLeft = LaunchMs; } else airborneLeft = 0f;
            chillLeft = chilled ? 1000f : 0f;
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

        void ProxyUpdate(float dtMs)
        {
            if (airborneLeft > 0f) airborneLeft = Mathf.Max(1f, airborneLeft - dtMs);
            transform.position = new Vector3(x, y, 0f);
            Render();
        }
    }
}
