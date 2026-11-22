using System.Collections.Generic;
using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>Monster sprite / bar presentation.</summary>
    public partial class Monster
    {
        const int WalkCell = 192, AttackCell = 256, SheetRows = 4; // rows: 0 down, 1 up, 2 left, 3 right (the facing code)
        const float IdleFrameMs = 260f; // 4-frame breathing loop of an optional idle sheet (1.04 s a cycle)
        const string SheetFolder = "Sprites/monsters/";

        /// <summary>
        /// Loads the walk and attack sheets. A missing sheet would not crash (Render draws nothing, SpriteSheet logs an error), but the monster would be
        /// invisible, so a definition with a FallbackSheet draws that one tinted until its own walk sheet exists in the 4-row layout. Its own walk sheet
        /// without an attack sheet just plays no attack frames.
        /// </summary>
        void LoadSheets()
        {
            string sheet = def.Sheet;
            bool hasFallback = def.FallbackSheet != null;
            int walkCell = def.WalkCellPx, attackCell = def.AttackCellPx; // own art: the def's cell sizes (event bosses: 384 / 512 cells at doubled pixels per unit, so a cell covers the same world size)
            if (hasFallback && !SheetUsable(def.Sheet, walkCell)) { sheet = def.FallbackSheet; artTint = def.FallbackTint; walkCell = WalkCell; attackCell = AttackCell; }
            walk = SpriteSheet.Grid(SheetFolder + sheet, walkCell, walkCell, GameConfig.FramePpu * walkCell / WalkCell, new Vector2(0.5f, 0f));
            // Optional standing loop in the walk layout (Mire Hydra Queen v2, 2026-11-19): drawn while the monster stands, the walk sheet only while it moves.
            if (Resources.Load<Texture2D>(SheetFolder + sheet + "-idle") != null)
                idle = SpriteSheet.Grid(SheetFolder + sheet + "-idle", walkCell, walkCell, GameConfig.FramePpu * walkCell / WalkCell, new Vector2(0.5f, 0f));
            if (hasFallback && Resources.Load<Texture2D>(SheetFolder + sheet + "-attack") == null) return;
            // Attack cells keep the feet 1/8 of the cell above the bottom (256 cell: 224 px down, 512 cell: 448 px down) => pivot 0.125.
            attack = SpriteSheet.Grid(SheetFolder + sheet + "-attack", attackCell, attackCell, GameConfig.FramePpu * attackCell / AttackCell, new Vector2(0.5f, 32f / AttackCell));
        }

        /// <summary>True when the sheet exists and slices into all the facing rows (a smaller sheet leaves whole facings invisible).</summary>
        public static bool SheetUsable(string sheet, int cell)
        {
            Texture2D tex = Resources.Load<Texture2D>(SheetFolder + sheet);
            if (tex == null) return false;
            if (tex.height / cell >= SheetRows && tex.width >= cell) return true;
            Debug.LogWarning("[ZenoniaProto] Resources/" + SheetFolder + sheet + ".png is " + tex.width + "x" + tex.height + ", expected " + cell * 4 + "x" + cell * 4 + " (4x4 cells of " + cell + "): using the fallback sheet");
            return false;
        }

        /// <summary>Attack frame index (0..3) like the web renderer, or -1 when not striking.</summary>
        int AttackIndex()
        {
            if (state == State.Windup) return timer > def.WindupMs * 0.5f ? 0 : 1;
            if (state == State.Cooldown)
            {
                float elapsed = def.CooldownMs - timer;
                if (elapsed >= 0f && elapsed < RecoveryMs) return 2 + Mathf.FloorToInt(elapsed / (RecoveryMs * 0.5f));
            }
            return -1;
        }

        /// <summary>
        /// Hit reaction: with effects on the sprite flashes pure white via the SpriteFlash shader (held through the hit-stop freeze)
        /// and jitters a little; off = the old red tint.
        /// </summary>
        float flashLen = FeelConfig.FlashMs, flashCap = FeelConfig.FlashMaxAmount; // set per hit (HitFx)

        void ApplyHitReaction(float alpha)
        {
            bool fx = FeelConfig.FxOn && flash > 0f;
            Color tint = StatusTint * artTint;
            body.color = !fx && flash > 0f ? new Color(1f, 0.55f, 0.55f, alpha) : new Color(tint.r, tint.g, tint.b, alpha);
            FeelMaterials.SetFlash(body, fx ? Mathf.Clamp01(flash / (flashLen * 0.6f)) : 0f, flashCap);
            float jitter = fx && !def.IsBoss ? (Time.frameCount % 2 == 0 ? FeelConfig.HitJitterUnits : -FeelConfig.HitJitterUnits) : 0f; // a boss is too heavy to jitter (it is not shoved either)
            body.transform.localPosition = new Vector3(jitter, AirLift(), 0f);
        }

        void Render()
        {
            if (headless) return;
            int order = GameConfig.OrderBase(y);
            float alpha = 1f;
            if (state == State.Dying)
            {
                alpha = 1f - Mathf.Clamp01(deathTimer / FadeMs);
                if (alpha <= 0f) { body.enabled = false; shadow.enabled = false; barBack.enabled = false; barFill.enabled = false; SetRoFootLabel(false); return; }
            }
            Sprite[] attackRow = SpriteSheet.Row(attack, facing);
            Sprite[] walkRow = SpriteSheet.Row(walk, facing);
            int ai = proxy ? proxyAttack : AttackIndex();
            bool wantedCasting = state == State.Telegraph || state == State.Execute; // skill telegraph / impact: Monster.WantedTelegraphVisuals.cs
            Sprite s = null;
            if (wantedCasting && TryWantedCastFrame(out s)) { }
            else if (wantedCasting && attackRow != null && attackRow.Length > 0) s = attackRow[0]; // no dedicated cast row: a plain striking pose
            // 2026-11-15 (docs/reference/combo-knockback-chatgpt.md): a hit no longer swaps the windup / strike / follow-through pose for the standing
            // frame (that hid the 350 ms warning); the white flash and jitter still play over it. Idle / walking monsters still show the standing frame.
            else if (ai >= 0 && attackRow != null && ai < attackRow.Length) s = attackRow[ai];
            else if (walkRow != null && walkRow.Length > 0)
            {
                bool still = state == State.Idle || state == State.Cooldown || state == State.Windup;
                Sprite[] idleRow = still ? SpriteSheet.Row(idle, facing) : null;
                if (idleRow != null && idleRow.Length > 0) s = idleRow[Mathf.FloorToInt(animTimer / IdleFrameMs) % idleRow.Length];
                else s = walkRow[still ? 0 : Mathf.FloorToInt(animTimer / WalkFrameMs) % walkRow.Length];
            }
            body.sprite = s;
            ApplyHitReaction(alpha);
            body.sortingOrder = order + 3;
            shadow.sortingOrder = order;
            shadow.color = new Color(1f, 1f, 1f, 0.3f * alpha);
            UpdateStatusOverlays(order);

            bool showBar = state != State.Dying && hp < def.Hp;
            if (UiScreens.UseNew(UiScreen.Boss)) { SetRoFootLabel(false); RenderPaperBar(order, showBar); return; } // Adventure Paper frames, boss name and cast tab: Monster.PaperOverheadBar.cs
            if (paperBar) ClearPaperBar();
            if (UiStyle.Ro && !def.IsBoss && !def.IsWanted) // RO Soft HP bar + "Lv.N Name" under the feet (Monster.RoFootLabel.cs): only the target / a monster you just hit, unless Settings shows all (MonsterLabelPrefs, 22/11)
            {
                barBack.enabled = barFill.enabled = false;
                if (MonsterLabelPrefs.Shows(this, hitByMeAt)) RenderRoFootLabel(); else SetRoFootLabel(false);
                return;
            }
            SetRoFootLabel(false);
            barBack.enabled = showBar; barFill.enabled = showBar;
            if (def.UsesSkills && state != State.Dying)
            {
                float wantedTop = def.BodyHeightUnits * def.RenderScale + 2f + AirLift();
                RenderWantedCastBar(order, wantedTop);
                RenderWantedEnragedIcon(order, wantedTop + 3f);
            }
            if (showBar)
            {
                float top = def.BodyHeightUnits * def.RenderScale + 2f + AirLift();
                barBack.transform.localPosition = new Vector3(-7f, top, 0f);
                barBack.transform.localScale = new Vector3(14f, 2f, 1f);
                barFill.transform.localPosition = new Vector3(-7f, top, 0f);
                barFill.transform.localScale = new Vector3(14f * BarHp / def.Hp, 1.4f, 1f);
                barBack.sortingOrder = order + 6;
                barFill.sortingOrder = order + 7;
            }
        }

        /// <summary>Painted body height on screen, world units (RenderScale included): CameraFollow frames a Wanted Boss by it.</summary>
        public float DrawnHeight { get { return def.BodyHeightUnits * def.RenderScale; } }

        /// <summary>True when a hero standing at `feet` is drawn behind this monster's body (north of its feet, inside its drawn sprite): the 5x Wanted
        /// Boss hides the heroes on its far side, so NameTags leaves their names off instead of stacking them on the boss.</summary>
        public bool HidesHeroAt(Vector2 feet)
        {
            if (body == null || !body.enabled || feet.y <= y) return false;
            Bounds b = body.bounds;
            float headY = feet.y + 18f; // the hero's head: hidden when it is under the body too
            return feet.x >= b.min.x && feet.x <= b.max.x && headY >= b.min.y && headY <= b.max.y;
        }

        /// <summary>Highest a damage number starts above the feet, world units (1.5 hero frames).</summary>
        const float NumberLiftCap = 36f;

        /// <summary>The monster your own hero hit last, and when (Time.time); read by TargetMarker.</summary>
        public static Monster LastHitByMe;
        public static float LastHitByMeAt;
        float hitByMeAt = -99f; // Time.time of this monster's last hit by your own hero: MonsterLabelPrefs shows its label for a moment

        /// <summary>Hit presentation shared by the real hit (authority) and the replayed MonsterHit event (client): white flash, damage number, sparks.</summary>
        /// <summary>petKind: 0 = the hero's own hit, 1 = a pet's basic attack (teal #66D5D0, small), 2 = a pet's skill (pale gold #FFD77A); pet hits never shake, stop or mark the target (ChatGPT Lần 8).</summary>
        void HitFx(int dmg, Vector2 knockVel, bool crit, bool finisher, bool quietFx, bool otherPlayer = false, int petKind = 0)
        {
            bool fx = FeelConfig.FxOn;
            // Only your own hits flash / jitter the monster (2026-11-17): with 40 players on a boss, everyone's hits kept it white and shaking non-stop.
            if (!otherPlayer && petKind == 0)
            {
                // Warrior player (lock-target slash, 22/11 V2): a short, whiter flash (65 ms, up to 0.7) reads as the impact without a white blob
                bool sharp = fx && LocalPlayer.Hero != null && LocalPlayer.Hero.LocksTarget;
                flashLen = sharp ? WarriorSlashFeel.FlashMs : FeelConfig.FlashMs;
                flashCap = sharp ? WarriorSlashFeel.FlashCap : FeelConfig.FlashMaxAmount;
                flash = fx ? flashLen : HitFlashMs;
            }
            float bodyH = def.BodyHeightUnits * def.RenderScale;
            // Numbers sit above a normal monster's head, but a big boss (Wanted: 4.5 hero bodies) would push them off the top of the screen:
            // cap the lift at NumberLiftCap so they pop around its chest, near the hero hitting it (how ARPGs show damage on huge bosses).
            bodyH = Mathf.Min(bodyH, NumberLiftCap);
            Color c = crit ? new Color(1f, 0.85f, 0.2f) : finisher ? new Color(1f, 0.69f, 0.23f) : Color.white;
            if (petKind != 0) { if (!otherPlayer || PetViewPrefs.ShowOthers) DamageNumber.Spawn(x, y + bodyH + 2f, dmg.ToString(), petKind == 2 ? new Color(1f, 0.84f, 0.48f) : new Color(0.4f, 0.84f, 0.82f), false, false, otherPlayer); return; } // a pet's number (DamageNumberKind.Pet): its own colour, beside the owner's, no sparks
            if (otherPlayer) DamageNumber.Spawn(x, y + bodyH + 4f, dmg.ToString(), Color.white, false, false, true); // another player's hit: small, white, half alpha
            else
            {
                LastHitByMe = this; LastHitByMeAt = hitByMeAt = Time.time; // TargetMarker: the monster you are fighting
                // Multi-hit numbers (SkillHitSplit, 21/11): a skill hit pops as several numbers summing to dmg; the HP bar steps down with them.
                float partDelay, partInterval;
                int[] parts = DamageNumber.SpawnOwnHit(x, y + bodyH + 4f, dmg, crit, finisher, quietFx, hp <= 0f, this, out partDelay, out partInterval);
                ShowBarParts(parts, partDelay, partInterval);
            }
            ImpactFx.Spawn(x, y + bodyH * 0.5f, knockVel, crit, finisher, null, quietFx);
        }
    }
}
