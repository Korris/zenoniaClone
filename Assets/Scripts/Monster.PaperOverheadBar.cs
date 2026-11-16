using UnityEngine;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// Adventure Paper bars above a monster's head (boss-hp-bar.png, section 6 "Other HP Bars (In-World)"), drawn instead of the plain
    /// black / red quads while UiScreens.UseNew(UiScreen.Boss):
    ///   monster   the small leather frame with a red fill, only once it lost HP (as before)
    ///   boss      the wider gold-tipped frame, its name on a dark tab above and, while a Wanted Boss telegraphs a skill, a purple cast
    ///             tab "Frozen Ground Stomp 1.6s" below
    /// (the elite frame is cut but the game has no elite monsters yet). Same SpriteRenderers as the old bar, so the old code keeps showing /
    /// hiding them; switching the look back restores the white quads. Also what the top boss bar needs to read from a monster.
    /// </summary>
    public partial class Monster
    {
        const string PaperBarFolder = "Sprites/ui/paper/";
        const float PaperBarWidth = 15f, PaperBossBarWidth = 24f, WorldTextHeight = 2.6f;
        static readonly Color PaperBarFill = new Color(0.9f, 0.19f, 0.16f), PaperCastFill = new Color(0.6f, 0.3f, 0.94f), PaperTab = new Color(0.17f, 0.08f, 0.05f, 0.82f);

        /// <summary>Capture runs only (--boss-bar-demo cast): every Wanted Boss shows this skill being cast, on a loop.</summary>
        public static WantedSkillDef DebugCastDemo;

        public int Level { get { return def.ItemLevel; } }
        public int WalkCellPx { get { return def.WalkCellPx; } }

        bool paperBar;
        float paperFrameAspect; // texture height / width of the frame in use
        TextMesh paperName, paperCastText;
        SpriteRenderer paperNameTab, paperCastTab, paperCastFill;
        int paperCastTenths = -1;

        /// <summary>
        /// The skill a Wanted Boss is telegraphing and the ms until it lands. The authority reads its own state timer; a client proxy has no
        /// timer, so it asks the ground warning for the event's locked strike instant (WantedSkillTelegraphFx): bar and warning cannot drift apart.
        /// </summary>
        public bool WantedCast(out WantedSkillDef skill, out float leftMs)
        {
            skill = null; leftMs = 0f;
            if (!def.IsWanted || state == State.Dying) return false;
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
            Sprite frame = PaperBarSprite(def.IsBoss ? PaperPiece.OverheadBoss : PaperPiece.OverheadNormal, out paperFrameAspect);
            if (frame != null) { barBack.sprite = frame; barBack.color = Color.white; } // art missing: the old dark quad stays as the frame
            barFill.color = PaperBarFill;
            if (!def.IsBoss) return;
            paperNameTab = ProceduralSprites.Child(transform, "PaperNameTab", ProceduralSprites.White(false));
            paperNameTab.color = PaperTab;
            paperName = WorldText("PaperName", def.Name, PaperTheme.TextLight);
            float w = paperName.GetComponent<MeshRenderer>().bounds.size.x + 3f;
            paperNameTab.transform.localScale = new Vector3(w, WorldTextHeight + 1f, 1f);
            if (!def.IsWanted) return;
            paperCastTab = ProceduralSprites.Child(transform, "PaperCastTab", ProceduralSprites.White(true));
            paperCastTab.color = PaperTab;
            paperCastFill = ProceduralSprites.Child(transform, "PaperCastFill", ProceduralSprites.White(true));
            paperCastFill.color = PaperCastFill;
            paperCastText = WorldText("PaperCastText", "", PaperTheme.TextLight);
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
            foreach (Component c in new Component[] { paperName, paperCastText, paperNameTab, paperCastTab, paperCastFill })
                if (c != null) Destroy(c.gameObject);
        }

        void RenderPaperBar(int order, bool showBar)
        {
            if (!paperBar) BuildPaperBar();
            bool boss = def.IsBoss, art = barBack.sprite != ProceduralSprites.White(true);
            float w = boss ? PaperBossBarWidth : PaperBarWidth, h = w * paperFrameAspect;
            float top = def.BodyHeightUnits * def.RenderScale + 2f + AirLift();
            barBack.enabled = showBar; barFill.enabled = showBar;
            if (showBar)
            {
                // Groove of the cut frames: normal x 26..247 of 273, 29 px high; boss x 24..290 of 333, 34 px high.
                float x0 = boss ? 24f / 333f : 26f / 273f, span = boss ? 266f / 333f : 221f / 273f, fillH = boss ? 34f / 333f : 29f / 273f;
                barBack.transform.localPosition = new Vector3(-w * 0.5f, top, 0f);
                barBack.transform.localScale = art ? new Vector3(w, w, 1f) : new Vector3(w, h, 1f);
                barFill.transform.localPosition = new Vector3(-w * 0.5f + w * x0, top, 0f);
                barFill.transform.localScale = new Vector3(w * span * Mathf.Clamp01(hp / def.Hp), w * fillH, 1f);
                barBack.sortingOrder = order + 6;
                barFill.sortingOrder = order + 7;
            }
            if (!boss) return;
            bool alive = state != State.Dying;
            Place(paperNameTab, paperName, alive && showBar, top + h * 0.5f + WorldTextHeight * 0.5f + 0.8f, order);
            if (paperCastTab == null) return;
            WantedSkillDef skill;
            float leftMs;
            bool casting = WantedCast(out skill, out leftMs); // false once it is dying
            float castY = top - h * 0.5f - WorldTextHeight * 0.5f - 0.9f;
            Place(paperCastTab, paperCastText, casting, castY, order);
            paperCastFill.enabled = casting;
            if (!casting) return;
            int tenths = Mathf.CeilToInt(leftMs / 100f);
            if (tenths != paperCastTenths)
            {
                paperCastTenths = tenths;
                paperCastText.text = skill.Name + " " + (tenths / 10) + "." + (tenths % 10) + "s";
            }
            float cw = Mathf.Max(w, paperCastText.GetComponent<MeshRenderer>().bounds.size.x + 3f), ch = WorldTextHeight + 1f;
            paperCastTab.transform.localPosition = paperCastFill.transform.localPosition = new Vector3(-cw * 0.5f, castY, 0f);
            paperCastTab.transform.localScale = new Vector3(cw, ch, 1f);
            paperCastFill.transform.localScale = new Vector3(cw * (1f - Mathf.Clamp01(leftMs / Mathf.Max(1, skill.TelegraphMs))), ch, 1f);
            paperCastFill.sortingOrder = order + 7;
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
