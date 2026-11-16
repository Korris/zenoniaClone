using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// A Pet Companion skill hit (ChatGPT "CHỐT V1 — GACHA PET"): the same damage, aggro, FX and death as TakeHit, but no push, no on-hit hooks (marks, next-hit buffs),
    /// no pet trigger, and NOT credited to the damage table, so a pet never decides the EXP split, loot ownership or a valid boss share (AccountRewards).
    /// </summary>
    public partial class Monster
    {
        public void TakePetHit(int dmg, HeroController owner, bool skill = false)
        {
            int petKind = skill ? 2 : 1;
            if (IsDying || proxy || dmg <= 0) return;
            if (owner != null && !owner.IsDead) target = owner;
            hp -= dmg;
            if (state == State.Idle || state == State.Wander || state == State.Return) { state = State.Chase; timer = 0f; }
            HitFx(dmg, Vector2.zero, false, false, false, !NetMode.IsOffline && owner != LocalPlayer.Hero, petKind);
            NetEventBus.MonsterHit(this, owner, dmg, Vector2.zero, false, false, false, petKind);
            if (hp > 0f) return;
            hp = 0f;
            state = State.Dying;
            deathTimer = 0f;
            target = null;
            if (homeZone != null) homeZone.OnMonsterDied(this, zone);
        }
    }
}
