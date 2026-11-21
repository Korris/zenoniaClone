using UnityEngine;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// Adventure Paper bars of a monster (boss-hp-bar.png, section 6 "Other HP Bars (In-World)"), drawn instead of the plain black / red quads
    /// while UiScreens.UseNew(UiScreen.Boss). Ragnarok-style (user 17/11): a monster shows its small leather-framed HP bar and "Lv.N Name" UNDER
    /// ITS FEET, always while alive; a boss shows nothing on its body: its name, level, HP and Wanted cast line live in the top BossBarView only.
    /// (the elite frame is cut but the game has no elite monsters yet). Same SpriteRenderers as the old bar, so the old code keeps showing /
    /// hiding them; switching the look back restores the white quads. Also what the top boss bar needs to read from a monster.
    /// </summary>
    public partial class Monster
    {
        const string PaperBarFolder = "Sprites/ui/paper/";
        const float PaperBarWidth = 15f, WorldTextHeight = 2.6f, FootBarY = -3f, FootTextHeight = 2.2f;
        /// <summary>Foot bars and names draw over every monster and hero (Ragnarok: a monster in front never hides the label of one behind), under the skill FX.</summary>
        const int FootLabelOrder = FeelConfig.FxOrder - 60;
        static readonly Color PaperBarFill = new Color(0.9f, 0.19f, 0.16f);

        /// <summary>Capture runs only (--boss-bar-demo cast): every Wanted Boss shows this skill being cast, on a loop.</summary>
        public static WantedSkillDef DebugCastDemo;

        public int Level { get { return def.ItemLevel; } }
        public int WalkCellPx { get { return def.WalkCellPx; } }

        bool paperBar;
        float paperFrameAspect; // texture height / width of the frame in use
        TextMesh paperName;
        SpriteRenderer paperNameTab;

        /// <summary>
        /// The skill a Wanted Boss is telegraphing and the ms until it lands. The authority reads its own state timer; a client proxy has no
        /// timer, so it asks the ground warning for the event's locked strike instant (WantedSkillTelegraphFx): bar and warning cannot drift apart.
        /// </summary>
        public bool WantedCast(out WantedSkillDef skill, out float leftMs)
        {
            skill = null; leftMs = 0f;
            if (!def.UsesSkills || state == State.Dying) return false;
            if (DebugCastDemo != null)
            {
                skill = DebugCastDemo;
                leftMs = skill.TelegraphMs - Time.unscaledTime * 1000f % skill.TelegraphMs;
                return true;
            }
            if (state != State.Telegraph) return false;
            skill = proxy ? CurrentWantedSkill() : pendingSkill;
            if (skill == null) return false;
            if (proxy) return WantedSkillTelegraphFx.StrikeIn(def.Sheet, skill, out leftMs);
            leftMs = Mathf.Max(0f, timer);
            return true;
        }

        /// <summary>Capture runs only: a boss at this share of its HP (the bar states: 57 %, enraged under 30 %); 0 kills it through the normal path.</summary>
        public void DebugSetHpFraction(float fraction)
        {
            if (proxy || IsDying) return;
            if (fraction <= 0f) { TakeHit(Mathf.CeilToInt(hp) + 1, Vector2.zero, false, false, true); return; }
            hp = Mathf.Max(1f, def.Hp * Mathf.Clamp01(fraction));
        }

        static Sprite PaperBarSprite(string piece, out float aspect)
        {
            Texture2D tex = SpriteSheet.TryLoad(PaperBarFolder + piece);
            aspect = tex != null ? (float)tex.height / tex.width : 0.2f;
            return tex == null ? null : SpriteSheet.Whole(PaperBarFolder + piece, tex.width, new Vector2(0f, 0.5f)); // 1 unit wide, pivot at its left end
        }

        void BuildPaperBar()
        {
            paperBar = true;
            Sprite frame = PaperBarSprite(PaperPiece.OverheadNormal, out paperFrameAspect);
            if (frame != null) { barBack.sprite = frame; barBack.color = Color.white; } // art missing: the old dark quad stays as the frame
            barFill.color = PaperBarFill;
            if (def.IsBoss) return; // nothing on a boss's body: the top bar shows it
            paperNameTab = ProceduralSprites.Child(transform, "PaperNameTab", ProceduralSprites.White(false));
            paperNameTab.color = new Color(0f, 0f, 0f, 0.45f);
            paperName = WorldText("PaperName", "<color=#FDC04E>Lv." + def.ItemLevel + "</color> " + def.Name, Color.white); // gold level like the top boss bar, white name (user 17/11: one colour read as one blur)
            paperName.richText = true;
            paperName.characterSize *= FootTextHeight / WorldTextHeight;
            float tw = paperName.GetComponent<MeshRenderer>().bounds.size.x + 2f;
            paperNameTab.transform.localScale = new Vector3(tw, FootTextHeight + 0.6f, 1f);
        }

        /// <summary>Text in the world (kit title font), centred on its position and about WorldTextHeight units high.</summary>
        TextMesh WorldText(string name, string text, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            TextMesh t = go.AddComponent<TextMesh>();
            t.font = PaperTheme.TitleFont;
            t.fontSize = 32; // glyphs rasterised near their on-screen size at 1080p, sharp enough at 1440p
            t.characterSize = WorldTextHeight * 10f / t.fontSize;
            t.anchor = TextAnchor.MiddleCenter;
            t.alignment = TextAlignment.Center;
            t.color = color;
            t.text = text;
            go.GetComponent<MeshRenderer>().sharedMaterial = t.font.material;
            return t;
        }

        /// <summary>The look was switched back at run time: the old quads again (the old code places and colours the rest every frame).</summary>
        void ClearPaperBar()
        {
            paperBar = false;
            barBack.sprite = ProceduralSprites.White(true);
            barBack.color = new Color(0f, 0f, 0f, 0.7f);
            barFill.color = new Color(0.9f, 0.2f, 0.25f, 1f);
            foreach (Component c in new Component[] { paperName, paperNameTab })
                if (c != null) Destroy(c.gameObject);
        }

        void RenderPaperBar(int order, bool showBar)
        {
            if (!paperBar) BuildPaperBar();
            if (def.IsBoss) { barBack.enabled = barFill.enabled = false; return; } // the top BossBarView carries the boss's name, level, HP and cast line
            bool art = barBack.sprite != ProceduralSprites.White(true);
            showBar = state != State.Dying; // Ragnarok-style: always shown under a live monster's feet, not only once hurt
            float w = PaperBarWidth, h = w * paperFrameAspect, y = FootBarY; // under the feet (the transform sits on them)
            barBack.enabled = showBar; barFill.enabled = showBar;
            if (showBar)
            {
                // Groove of the cut frame: x 26..247 of 273, 29 px high.
                float x0 = 26f / 273f, span = 221f / 273f, fillH = 29f / 273f;
                barBack.transform.localPosition = new Vector3(-w * 0.5f, y, 0f);
                barBack.transform.localScale = art ? new Vector3(w, w, 1f) : new Vector3(w, h, 1f);
                barFill.transform.localPosition = new Vector3(-w * 0.5f + w * x0, y, 0f);
                barFill.transform.localScale = new Vector3(w * span * Mathf.Clamp01(hp / def.Hp), w * fillH, 1f);
                barBack.sortingOrder = FootLabelOrder + 2;
                barFill.sortingOrder = FootLabelOrder + 3;
            }
            Place(paperNameTab, paperName, showBar, y - h * 0.5f - FootTextHeight * 0.5f - 0.6f, FootLabelOrder - 6);
        }

        static void Place(SpriteRenderer tab, TextMesh text, bool show, float y, int order)
        {
            MeshRenderer mr = text.GetComponent<MeshRenderer>();
            tab.enabled = show; mr.enabled = show;
            if (!show) return;
            Vector3 at = new Vector3(tab.transform.localPosition.x, y, 0f);
            tab.transform.localPosition = at;
            text.transform.localPosition = new Vector3(0f, y, 0f);
            tab.sortingOrder = order + 6;
            mr.sortingOrder = order + 8;
        }
    }
}
