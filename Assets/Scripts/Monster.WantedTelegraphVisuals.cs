using UnityEngine;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// Client display of a Wanted Boss's current skill (RendersFx only): which cast-sheet row to show (set here by the server's own local view
    /// and by NetEventPlayer for every proxy -- one setter, so both draw the same thing), the cast bar under the HP bar, and the enraged icon.
    /// The ground telegraph itself (warning shape, countdown fill) and the impact picture are WantedSkillTelegraphFx / WantedSkillVfx: this file
    /// only draws the BOSS, exactly like Monster.Visuals.cs draws its plain attack.
    /// </summary>
    public partial class Monster
    {
        int telegraphSkillId = -1;
        SpriteRenderer castBarBack, castBarFill, enragedIcon;
        float castBarAspect = 0.25f, enragedAspect = 1f; // texture height / width, read once when each icon first loads

        /// <summary>A whole-texture sprite scaled so its WIDTH is exactly 1 world unit at scale 1 (ppu = its own pixel width): callers then set
        /// localScale.x to the wanted width in units directly, like ProceduralSprites' 1-unit quads.</summary>
        static Sprite LoadWantedIcon(string path, Vector2 pivot, out float aspect)
        {
            Texture2D tex = SpriteSheet.TryLoad(path);
            aspect = tex != null ? (float)tex.height / tex.width : 1f;
            return tex == null ? null : SpriteSheet.Whole(path, tex.width, pivot);
        }

        /// <summary>NetEventPlayer (proxy) and Monster.WantedSkills.cs's own BroadcastWantedTelegraph (the local / host view) call this so every
        /// viewer picks the same cast-sheet row; which row depends only on the skill id and the shared WantedSkillTable, never sent on the wire.</summary>
        public void ShowWantedTelegraph(int skillId) { telegraphSkillId = skillId; }

        /// <summary>The skill this boss is telegraphing (null when none) and its boss id (def.Sheet): read by the capture bot (DebugCapture.NetWantedShowcase).</summary>
        public WantedSkillDef WantedTelegraphSkill { get { return state == State.Telegraph ? CurrentWantedSkill() : null; } }
        public string WantedBossId { get { return def.Sheet; } }

        WantedSkillDef CurrentWantedSkill()
        {
            if (telegraphSkillId < 0) return null;
            WantedSkillDef[] table = WantedSkillTable.For(def.Sheet);
            return table != null && telegraphSkillId < table.Length ? table[telegraphSkillId] : null;
        }

        /// <summary>A frame of "&lt;sheet&gt;-cast.png" for the current skill's CastRow, or false when it has none (skill 1's cone falls back to
        /// the ordinary attack sheet in Render; Glacial Colossus / Mire Hydra Queen have no cast sheet at all yet).</summary>
        bool TryWantedCastFrame(out Sprite s)
        {
            s = null;
            WantedSkillDef skill = CurrentWantedSkill();
            if (skill != null && skill.PoseSheetSuffix != null) return TryPoseSheetFrame(skill, out s);
            if (skill == null || skill.CastRow < 0) return false;
            int cell = def.AttackCellPx;
            Sprite[][] cast = SpriteSheet.Grid(SheetFolder + def.Sheet + "-cast", cell, cell, GameConfig.FramePpu * cell / AttackCell, new Vector2(0.5f, 0.125f));
            Sprite[] row = SpriteSheet.Row(cast, skill.CastRow);
            if (row == null || row.Length == 0) return false;
            float progress = state == State.Telegraph ? 1f - Mathf.Clamp01(timer / Mathf.Max(1, skill.TelegraphMs)) : 1f;
            s = row[Mathf.Clamp(Mathf.FloorToInt(progress * row.Length), 0, row.Length - 1)];
            return true;
        }

        State poseState = State.Idle;
        float poseStartedAt;

        /// <summary>The skill's OWN pose sheet (Matriarch -charge / -stomp, Elder -sweep / -pulse / -awaken): facing row, pose by WantedSkillPose (F1-F2 warning, F3 impact / launch, F4 held).
        /// Execute elapsed time is measured here from the first frame this state is drawn, so the server's own view and a proxy agree without a timer on the wire.</summary>
        bool TryPoseSheetFrame(WantedSkillDef skill, out Sprite s)
        {
            s = null;
            if (state != poseState) { poseState = state; poseStartedAt = Time.time; }
            string path = SheetFolder + def.File + skill.PoseSheetSuffix;
            if (SpriteSheet.TryLoad(path) == null) return false;
            int cell = def.AttackCellPx;
            Sprite[] row = SpriteSheet.Row(SpriteSheet.Grid(path, cell, cell, GameConfig.FramePpu * cell / AttackCell, new Vector2(0.5f, 32f / AttackCell)), facing);
            if (row == null || row.Length < 4) return false;
            float progress = state == State.Telegraph ? 1f - Mathf.Clamp01(timer / Mathf.Max(1, skill.TelegraphMs)) : 1f;
            s = row[WantedSkillPose.Frame(state == State.Telegraph, progress, (Time.time - poseStartedAt) * 1000f)];
            return true;
        }

        /// <summary>Thin bar just under the HP bar, running from empty to full as the telegraph counts down (design doc: "thanh niệm chú").</summary>
        void RenderWantedCastBar(int order, float top)
        {
            WantedSkillDef skill = state == State.Telegraph ? CurrentWantedSkill() : null;
            if (skill == null)
            {
                if (castBarBack != null) { castBarBack.enabled = false; castBarFill.enabled = false; }
                return;
            }
            const float Width = 12f;
            if (castBarBack == null)
            {
                castBarBack = ProceduralSprites.Child(transform, "WantedCastBarBack", LoadWantedIcon("Sprites/fx/wanted/warning-castbar-frame", new Vector2(0.5f, 0.5f), out castBarAspect));
                castBarFill = ProceduralSprites.Child(transform, "WantedCastBarFill", LoadWantedIcon("Sprites/fx/wanted/warning-castbar-fill", new Vector2(0f, 0.5f), out _));
                castBarFill.color = new Color(1f, 0.6f, 0.2f, 0.95f);
            }
            float progress = 1f - Mathf.Clamp01(timer / Mathf.Max(1, skill.TelegraphMs));
            float y = top - 2.4f, h = Width * castBarAspect;
            castBarBack.enabled = true; castBarFill.enabled = true;
            castBarBack.transform.localPosition = new Vector3(0f, y, 0f);
            castBarBack.transform.localScale = new Vector3(Width, h, 1f);
            castBarFill.transform.localPosition = new Vector3(-Width * 0.5f, y, 0f);
            castBarFill.transform.localScale = new Vector3(Width * 0.92f * progress, h * 0.7f, 1f);
            castBarBack.sortingOrder = order + 8;
            castBarFill.sortingOrder = order + 7;
        }

        /// <summary>Small badge above the HP bar while enraged (below 30 % hp): informational for everyone, not just the local player.</summary>
        void RenderWantedEnragedIcon(int order, float top)
        {
            bool show = WantedEnraged;
            if (!show) { if (enragedIcon != null) enragedIcon.enabled = false; return; }
            if (enragedIcon == null) enragedIcon = ProceduralSprites.Child(transform, "WantedEnraged", LoadWantedIcon("Sprites/fx/wanted/warning-enraged", new Vector2(0.5f, 0.5f), out enragedAspect));
            enragedIcon.enabled = true;
            enragedIcon.color = new Color(1f, 0.4f, 0.3f, 0.95f);
            enragedIcon.transform.localPosition = new Vector3(9f, top, 0f);
            enragedIcon.transform.localScale = new Vector3(5f, 5f * enragedAspect, 1f);
            enragedIcon.sortingOrder = order + 8;
        }
    }
}
