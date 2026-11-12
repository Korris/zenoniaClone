using System.Collections.Generic;
using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>Monster sprite / bar presentation.</summary>
    public partial class Monster
    {
        const int WalkCell = 192, AttackCell = 256, SheetRows = 4; // rows: 0 down, 1 up, 2 left, 3 right (the facing code)
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
            if (hasFallback && !WalkSheetUsable(def.Sheet, walkCell)) { sheet = def.FallbackSheet; artTint = def.FallbackTint; walkCell = WalkCell; attackCell = AttackCell; }
            walk = SpriteSheet.Grid(SheetFolder + sheet, walkCell, walkCell, GameConfig.FramePpu * walkCell / WalkCell, new Vector2(0.5f, 0f));
            if (hasFallback && Resources.Load<Texture2D>(SheetFolder + sheet + "-attack") == null) return;
            // Attack cells keep the feet 1/8 of the cell above the bottom (256 cell: 224 px down, 512 cell: 448 px down) => pivot 0.125.
            attack = SpriteSheet.Grid(SheetFolder + sheet + "-attack", attackCell, attackCell, GameConfig.FramePpu * attackCell / AttackCell, new Vector2(0.5f, 32f / AttackCell));
        }

        /// <summary>True when the sheet exists and slices into all the facing rows (a smaller sheet leaves whole facings invisible).</summary>
        static bool WalkSheetUsable(string sheet, int cell)
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
        void ApplyHitReaction(float alpha)
        {
            bool fx = FeelConfig.FxOn && flash > 0f;
            Color tint = StatusTint * artTint;
            body.color = !fx && flash > 0f ? new Color(1f, 0.55f, 0.55f, alpha) : new Color(tint.r, tint.g, tint.b, alpha);
            FeelMaterials.SetFlash(body, fx ? Mathf.Clamp01(flash / (FeelConfig.FlashMs * 0.6f)) : 0f);
            float jitter = fx ? (Time.frameCount % 2 == 0 ? FeelConfig.HitJitterUnits : -FeelConfig.HitJitterUnits) : 0f;
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
                if (alpha <= 0f) { body.enabled = false; shadow.enabled = false; barBack.enabled = false; barFill.enabled = false; return; }
            }
            Sprite[] attackRow = SpriteSheet.Row(attack, facing);
            Sprite[] walkRow = SpriteSheet.Row(walk, facing);
            int ai = proxy ? proxyAttack : AttackIndex();
            Sprite s = null;
            if (flash <= 0f && ai >= 0 && attackRow != null && ai < attackRow.Length) s = attackRow[ai];
            else if (walkRow != null && walkRow.Length > 0)
            {
                bool still = state == State.Idle || state == State.Cooldown || state == State.Windup;
                s = walkRow[still ? 0 : Mathf.FloorToInt(animTimer / WalkFrameMs) % walkRow.Length];
            }
            body.sprite = s;
            ApplyHitReaction(alpha);
            body.sortingOrder = order + 3;
            shadow.sortingOrder = order;
            shadow.color = new Color(1f, 1f, 1f, 0.3f * alpha);
            UpdateStatusOverlays(order);

            bool showBar = state != State.Dying && hp < def.Hp;
            barBack.enabled = showBar; barFill.enabled = showBar;
            if (showBar)
            {
                float top = def.BodyHeightUnits * def.RenderScale + 2f + AirLift();
                barBack.transform.localPosition = new Vector3(-7f, top, 0f);
                barBack.transform.localScale = new Vector3(14f, 2f, 1f);
                barFill.transform.localPosition = new Vector3(-7f, top, 0f);
                barFill.transform.localScale = new Vector3(14f * hp / def.Hp, 1.4f, 1f);
                barBack.sortingOrder = order + 6;
                barFill.sortingOrder = order + 7;
            }
        }

        /// <summary>The monster your own hero hit last, and when (Time.time); read by TargetMarker.</summary>
        public static Monster LastHitByMe;
        public static float LastHitByMeAt;

        /// <summary>Hit presentation shared by the real hit (authority) and the replayed MonsterHit event (client): white flash, damage number, sparks.</summary>
        void HitFx(int dmg, Vector2 knockVel, bool crit, bool finisher, bool quietFx, bool otherPlayer = false)
        {
            bool fx = FeelConfig.FxOn;
            flash = fx ? FeelConfig.FlashMs : HitFlashMs;
            float bodyH = def.BodyHeightUnits * def.RenderScale;
            Color c = crit ? new Color(1f, 0.85f, 0.2f) : finisher ? new Color(1f, 0.69f, 0.23f) : Color.white;
            if (otherPlayer) DamageNumber.Spawn(x, y + bodyH + 4f, dmg.ToString(), Color.white, false, false, true); // another player's hit: small, white, half alpha
            else
            {
                LastHitByMe = this; LastHitByMeAt = Time.time; // TargetMarker: the monster you are fighting
                DamageNumber.Spawn(x, y + bodyH + 4f, crit && fx ? dmg + "!" : dmg.ToString(), c, crit || finisher, crit);
            }
            ImpactFx.Spawn(x, y + bodyH * 0.5f, knockVel, crit, finisher, null, quietFx);
        }
    }
}
