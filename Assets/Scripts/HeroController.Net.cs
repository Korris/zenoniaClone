using UnityEngine;
using ZenoniaProto.Net;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// Network face of a hero. Server / host: packs pose, position and hp percent into a snapshot entry. Client: every remote hero is a proxy -- no
    /// input, no combat rules -- whose position and pose come from interpolated snapshots and whose animation frame is chosen from pose / combo
    /// step / progress by the same Render code the simulated hero uses. The local hero starts as a proxy too, then AttachPredictor (see
    /// HeroController.Predicted.cs) makes it simulate itself.
    /// </summary>
    public partial class HeroController
    {
        /// <summary>Account-level id of the player controlling this hero (0 for the host's own / debug heroes) and the shown name.</summary>
        public ushort PlayerId;
        public string PlayerName = "";

        bool proxy;
        HeroPose proxyPose;
        int proxyStep;
        float proxyProgress;

        public bool IsProxy { get { return proxy; } }

        /// <summary>Client: a hero driven by the server. `local` = the hero this player controls (HUD, camera and inventory follow it).</summary>
        public void InitProxy(HeroClassDef def, HeroRenderer renderer, PlayerState state, ushort netId, bool local)
        {
            proxy = true;
            if (local) LocalPlayer.Set(this);
            view = renderer; // null on a -nographics bot client: the hero exists for the controls / bots, nothing is drawn
            // The look picked at character creation (kept on this device). Only the local hero: other players' proxies get style from the snapshot, and the style then
            // reaches them through SyncHairStyleOnline. AccountFlow.EnteredId is set before the zone join and cleared only by LeaveWorld, so it is still set here.
            if (local && AccountFlow.EnteredId != null) CharacterLookPrefs.ApplyTo(view, AccountFlow.EnteredId);
            Setup(def, state, netId);
            RefreshGearLabel();
            Render();
        }

        /// <summary>Pushes the equipped weapon / offhand of this hero's own RPG state to its paper doll (host: remote players with a view).</summary>
        public void SyncGearView()
        {
            if (view == null) return;
            view.SetEquipmentGear(rpg.GetEquipped(Slots.Weapon), rpg.GetEquipped(Slots.Offhand));
            ArmorLooks.FromWorn(view, rpg);
            view.SetPet(PetConfig.IdOf(rpg.GetEquipped(Slots.Pet)));
            RefreshGearLabel();
        }

        /// <summary>Client: shows the replicated paper-doll data of a hero.</summary>
        public void ProxyLook(NetAppearance look)
        {
            if (view == null) return;
            Item w, o;
            NetEventBus.ItemsFromAppearance(look, RpgSession.ClassId(cls.Id), out w, out o);
            view.SetEquipmentGear(w, o);
            view.SetOutfit(NetEventBus.TierOfWire(look.ArmorTier));
            view.SetPet(PetConfig.IdOfWire(look.PetId));
            view.HairStyle = look.HairStyle; // the renderer wraps a style this build has no art for
        }

        /// <summary>Server side: the head layer style this hero shows to others (InvOp.HairStyle from its client; the host's own view directly).</summary>
        [System.NonSerialized] public byte NetHairStyle;
        int sentHairStyle = -1;

        /// <summary>Local hero online: sends the chosen hairstyle (H key, --hairstyle) whenever it differs from what the server last got.</summary>
        void SyncHairStyleOnline()
        {
            if (this != LocalPlayer.Hero || view == null || NetMode.IsOffline || view.HairStyle == sentHairStyle) return;
            if (NetMode.IsServerSide) { NetHairStyle = (byte)view.HairStyle; NetEventBus.Appearance(this); sentHairStyle = view.HairStyle; return; } // host: its own hero is the server's
            if (NetClient.SendInvAction(InvOp.HairStyle, "", (byte)view.HairStyle)) sentHairStyle = view.HairStyle;
        }

        /// <summary>Client: swaps the mirrored RPG state for a fresh one from an RpgState push.</summary>
        public void ProxyReplaceState(PlayerState state) { rpg = state; }

        /// <summary>Server snapshot entry: position (1/8 unit), facing + flags, pose / combo step / progress for the animation, hp percent.</summary>
        public HeroSnap ToSnap()
        {
            HeroPose pose;
            int step = 0;
            float progress = 0f;
            if (dead) { pose = HeroPose.Death; progress = deathTimer / RespawnMs; }
            else if (hurt > 0f) { pose = HeroPose.Hurt; progress = 1f - hurt / HurtMs; }
            else if (castLeft > 0f) { pose = HeroPose.Cast; progress = 1f - castLeft / castTotal; }
            else if (dashLeft > 0f) pose = HeroPose.Dash;
            else if (combo.IsAttacking) { pose = HeroPose.Attack; step = combo.Step; progress = combo.PhaseProgress; }
            else pose = moving ? HeroPose.Walk : HeroPose.Idle;
            int flags = (dead ? HeroFlags.Dead : 0) | (dashLeft > 0f ? HeroFlags.Dash : 0) | (moving ? HeroFlags.Moving : 0) | (invuln > 0f ? HeroFlags.Invuln : 0) | (hurt > 0f ? HeroFlags.Hurt : 0);
            return new HeroSnap
            {
                Id = NetId, X = NetQuantize.Pos(x), Y = NetQuantize.Pos(y), FacingFlags = NetQuantize.PackFacing(facing, flags), Pose = (byte)pose,
                Step = (byte)step, Progress = NetQuantize.Progress(progress), HpPct = NetQuantize.HpPct(hp, MaxHp),
            };
        }

        /// <summary>Client: applies one interpolated snapshot sample.</summary>
        public void ProxyApply(Vector2 pos, int facingDir, int flags, HeroPose pose, int step, float progress, byte hpPct)
        {
            x = pos.x; y = pos.y;
            facing = facingDir & 3;
            bool nowDead = (flags & HeroFlags.Dead) != 0;
            if (nowDead && !dead) deathTimer = 0f;
            dead = nowDead;
            moving = (flags & HeroFlags.Moving) != 0;
            proxyPose = pose; proxyStep = step; proxyProgress = progress;
            hurt = pose == HeroPose.Hurt ? HurtMs * (1f - progress) : 0f;
            castTotal = 1000f;
            castLeft = pose == HeroPose.Cast ? castTotal * (1f - progress) : 0f;
            dashLeft = pose == HeroPose.Dash ? 1f : 0f;
            if (this != LocalPlayer.Hero) hp = MaxHp * NetQuantize.HpFraction(hpPct);
        }

        /// <summary>Client: HUD numbers pushed by the server for the local hero.</summary>
        public void ProxySetSelf(SelfStateMsg m)
        {
            rpg.Hp = m.Hp; rpg.Mp = m.Mp;
            if (m.Level != rpg.Level || m.MaxHp != rpg.MaxHp) { rpg.Level = Mathf.Max(1, m.Level); rpg.Exp = (int)m.Exp; rpg.RecomputeStats(); rpg.Hp = m.Hp; rpg.Mp = m.Mp; }
            rpg.Exp = (int)m.Exp;
            Item pet = rpg.GetEquipped(Slots.Pet);
            if (pet != null && m.PetDurability > 0 && pet.durability != m.PetDurability) { pet.durability = m.PetDurability; InventoryPanel.RefreshIfOpen(); }
            if (!predicted) ApplySelfCooldowns(m.SkillCds); // the predicted hero gets its cooldowns with the sim state
            HeroBuffs.SetClientView(m.Buffs);
        }

        /// <summary>Client: the server says this hero was hit / healed / died (pure presentation hooks for events).</summary>
        public void ProxyPlayHurt(int dmg)
        {
            hurt = Mathf.Max(hurt, HurtMs * 0.5f);
            HeroFx.Hurt(new Vector2(x, y), dmg, this == LocalPlayer.Hero);
        }

        void ProxyTick(float dtMs)
        {
            if (this == LocalPlayer.Hero && view != null && HeroHeadLayer.CycleKeyPressed()) view.HairStyle = view.HairStyle + 1; // H online too, SyncHairStyleOnline sends it
            if (this == LocalPlayer.Hero && view != null && HeroHeadLayer.DyeKeyPressed()) view.HairDye = view.HairDye + 1; // Shift+H: dye, local only for now
            idleTimer += dtMs;
            if (dashLeft > 0f || moving) animTimer += dtMs; else animTimer = 0f;
            if (dead) deathTimer += dtMs;
            transform.position = new Vector3(x, y, 0f);
            Render();
        }

        // ---- client: replay of server events (pure presentation) ----

        /// <summary>Melee swing picture of combo step `step` (echo = second stab), at this hero's current spot.</summary>
        public void PlaySwingFx(int step, bool echo, int facingDir)
        {
            if (cls.Hits == null || step < 1 || step > cls.Hits.Length) return;
            ComboHit h = cls.Hits[step - 1];
            int f = facingDir & 3;
            Vector2 v = DirVec[f];
            float offset = h.Reach * 0.5f + 4f;
            HeroFx.Swing(cls, new Vector2(x + v.x * offset, y + 6f + v.y * offset), new Vector2(x, y), DirAngle[f], h, step, step == combo.HitCount, echo, view != null ? view.WeaponGlow : null);
        }

        /// <summary>Cast flash + bolt picture of a ranged shot that left `muzzle` at `angle`; the bolt id lets BoltEnd find it again.</summary>
        public void PlayBoltFx(int step, Vector2 muzzle, float angle, bool finisher, ushort boltId)
        {
            if (cls.Hits == null || step < 1 || step > cls.Hits.Length || cls.Hits[step - 1].Bolt == null) return;
            ComboHit h = cls.Hits[step - 1];
            float rad = angle * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
            ClassPalette pal = cls.ActivePalette;
            // The Mechanic's pistol bolts leave the gun muzzle and fly their range minus how far it is ahead of the body (RangedAimRules.MechanicBoltOrigin)
            float ox, oy, cut = 0f;
            Vector2 fv = DirVec[facing & 3];
            if (cls.Id == HeroClass.Mechanic) RangedAimRules.MechanicBoltOrigin(step, facing & 3, x, y, fv.x, fv.y, out ox, out oy, out cut);
            else if (cls.Id == HeroClass.Cleric) RangedAimRules.ClericBoltOrigin(step, facing & 3, x, y, fv.x, fv.y, out ox, out oy, out cut); // staff crystal / shield
            ComboProjectile shot = ComboProjectile.Fire(muzzle, dir, angle, h.Bolt, 0f, 0f, 0f, finisher, cls.FxFolder, pal, 0f, null, boltId, cut);
            if (cls.Id == HeroClass.Cleric && shot != null) shot.KeepBehindShooter(y, RangedAimRules.ClericBehindTop, RangedAimRules.ClericStartsBehind(step, facing & 3));
            bool painted = VfxSprites.For(h.Bolt.Style) != null;
            int shotFacing = Mathf.Abs(dir.x) > Mathf.Abs(dir.y) ? (dir.x < 0f ? 2 : 3) : (dir.y > 0f ? 1 : 0);
            float figure = view != null ? view.FigureScale : 1f;
            int tipFacing = cls.Id == HeroClass.Cleric ? facing & 3 : shotFacing; // the Cleric's flash sits on her staff / shield, which follow her pose, not the aim
            Vector2 tip = HeroFx.CastTipFor(cls, step, tipFacing, new Vector2(x, y), new Vector2(x + dir.x * 10f, y + 12f + dir.y * 6f), figure);
            if (painted && !HeroFx.MechanicPistolFlash(cls, h, step, facing & 3, new Vector2(x, y), angle, figure)) HeroFx.CastWindup(cls, h, tip, angle, finisher, this);
            HeroFx.BoltLaunched(cls, h, finisher, pal, new Vector2(x, y), tip, dir, angle, painted);
        }

        /// <summary>Another player's (or the server-confirmed own) skill cast: replays the skill with the server's rank and cast salt (view only, the server owns the effects).</summary>
        public void PlaySkillFx(int index, int rank, int salt) { SkillExecutor.Cast(this, index, rank, salt); }
    }
}

