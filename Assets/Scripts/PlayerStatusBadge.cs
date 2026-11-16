using UnityEngine;
using UnityEngine.UI;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// Compact player status (docs/reference/ui-redesign-261116/chatgpt/hud-compact/hud-compact-mobile-v1.png and the size table of
    /// hud-compact-parts-board-states-v2.png: 8-9 % of the screen height, 16-19 % of a 16:9 width), top-left of the safe area: a small round
    /// portrait, "Lv. 60" beside it, slim HP / MP bars with their numbers, a thinner EXP line without a number, and under it a row of up to
    /// six buff icons with their seconds. Two sizes picked by HudLayout: mobile (with EXP) and PC (no EXP: on PC it sits under the hotbar).
    /// RpgHud builds it when UiScreens.UseNew(UiScreen.Hud) and calls Refresh every frame; texts are rebuilt only when their number changes.
    /// The states (low HP, MP empty, shield, damage taken) are in PlayerStatusBadge.States.cs.
    /// </summary>
    public sealed partial class PlayerStatusBadge
    {
        /// <summary>One size of the block, in canvas units from its top-left (the canvas is 640 units high: 1 % = 6.4).</summary>
        struct Sizes
        {
            public float Width, Height, Face, BarX, BarH, LevelH, BarGap;
            public bool Exp;
            public float BarW { get { return Width - BarX; } }
            public float HpY { get { return LevelH + 1f; } }
            public float MpY { get { return HpY + BarH + BarGap; } }
        }

        static readonly Sizes Mobile = new Sizes { Width = 198f, Height = 56f, Face = 54f, BarX = 57f, BarH = 14f, LevelH = 13f, BarGap = 2f, Exp = true };
        static readonly Sizes Web = new Sizes { Width = 198f, Height = 54f, Face = 54f, BarX = 57f, BarH = 16f, LevelH = 16f, BarGap = 3f };
        const float Left = 8f, Top = 6f;                       // gap to the safe area's corner, as the old block
        const float BuffGap = 4f, BuffSize = 24f, BuffRowH = BuffSize + 12f; // 3.75 % icons, "12s" under each
        const float BarPad = 0.14f, ExpPad = 0.2f, RingHole = 0.06f; // the frames' rims as a share of the bar height; the portrait ring's window
        static readonly Color ExpColor = PaperTheme.Hex("#F5C542");

        static Sizes Pick { get { return HudLayout.IsWeb ? Web : Mobile; } }

        /// <summary>Right edge of the block from the safe area's left, and its bottom edge from the top (buff row included): layout anchors
        /// for the boss bar, the chat panel and the debug HUD.</summary>
        public static float Right { get { return Left + Pick.Width; } }
        public static float Bottom { get { return Top + Pick.Height + BuffGap + BuffRowH; } }

        public readonly RectTransform Root;
        readonly HudCompactBar hp, mp, exp;
        readonly Image face;
        readonly Text level;
        readonly HudBuffRow buffs;
        int lastHp = -1, lastMaxHp, lastMp = -1, lastMaxMp, lastLevel, lastExp = -1;
        string lastClass;

        public PlayerStatusBadge(Transform safe)
        {
            Sizes z = Pick;
            Root = UiKit.Rect(safe, "PlayerStatusBadge", Left, Top, z.Width, z.Height);
            BuildRims(z); // behind the bars and the portrait

            RectTransform lv = UiKit.Rect(Root, "Level", z.BarX, 0f, 52f, z.LevelH);
            HudCompact.Capsule(lv, PaperPiece.HcLvPill, HudCompact.Veil, Color.clear, z.LevelH * 0.5f);
            level = PaperTheme.Title(lv, "Text", 6f, 0f, 60f, z.LevelH, "", PaperTheme.BodySize, PaperTheme.TextLight, PaperInk.Outlined);
            level.horizontalOverflow = HorizontalWrapMode.Overflow;

            hp = Bar("Hp", z.BarX, z.HpY, z.BarW, z.BarH, PaperPiece.HcBarFrame, PaperPiece.HcBarFill, PaperTheme.BarHp, "HP");
            mp = Bar("Mp", z.BarX, z.MpY, z.BarW, z.BarH, PaperPiece.HcBarFrame, PaperPiece.HcBarFill, PaperTheme.BarMp, "MP");
            if (z.Exp)
            {
                float y = z.MpY + z.BarH;
                Text name = PaperTheme.Title(Root, "ExpName", z.BarX + 6f, y, 26f, z.Height - y, "EXP", PaperTheme.BarSize, PaperTheme.TextLight, PaperInk.Outlined);
                name.horizontalOverflow = HorizontalWrapMode.Overflow;
                exp = Bar("Exp", z.BarX + 32f, y + (z.Height - y - 5f) * 0.5f + 1f, z.BarW - 34f, 5f, PaperPiece.HcExpFrame, PaperPiece.HcExpFill, ExpColor, null);
            }
            BuildStates(z);
            Image ring;
            face = HudCompact.Portrait(Root, "Portrait", 0f, (z.Height - z.Face) * 0.5f, z.Face, PaperPiece.HcPortraitRing, PaperPiece.HcPortraitPlate, RingHole, out ring);
            buffs = new HudBuffRow(Root, 2f, z.Height + BuffGap, BuffSize);
        }

        HudCompactBar Bar(string name, float x, float y, float w, float h, string frame, string fill, Color color, string label)
        {
            float pad = h * (label != null ? BarPad : ExpPad);
            return new HudCompactBar(UiKit.Rect(Root, name, x, y, w, h), h, frame, fill, color, pad, pad, label, label != null ? TextAnchor.MiddleRight : (TextAnchor?)null);
        }

        public void SetActive(bool on) { if (Root.gameObject.activeSelf != on) Root.gameObject.SetActive(on); }

        /// <summary>Every frame while a hero exists. dt = unscaled seconds (the HUD keeps animating through hit-stop and dialogues).</summary>
        public void Refresh(HeroController hero, PlayerState p, float dt)
        {
            if (p.Level != lastLevel || hero.Class.Name != lastClass)
            {
                bool swapped = hero.Class.Name != lastClass;
                lastLevel = p.Level; lastClass = hero.Class.Name;
                level.text = "Lv. " + p.Level;
                if (swapped) { face.sprite = RpgHud.FaceOf(hero.Class); face.enabled = face.sprite != null; ResetStates(); }
                lastExp = -1;
            }
            int hpNow = Mathf.CeilToInt(p.Hp), mpNow = Mathf.FloorToInt(p.Mp);
            float hpFrac = p.MaxHp > 0 ? Mathf.Clamp01(p.Hp / p.MaxHp) : 0f;
            if (hpNow != lastHp || p.MaxHp != lastMaxHp)
            {
                if (lastHp >= 0 && p.MaxHp == lastMaxHp && hpNow < lastHp) ShowHit(lastHp - hpNow, (float)lastHp / p.MaxHp);
                lastHp = hpNow; lastMaxHp = p.MaxHp;
                hp.Value.text = hpNow + " / " + p.MaxHp;
            }
            if (mpNow != lastMp || p.MaxMp != lastMaxMp) { lastMp = mpNow; lastMaxMp = p.MaxMp; mp.Value.text = mpNow + " / " + p.MaxMp; }
            hp.Set(hpFrac);
            mp.Set(p.MaxMp > 0 ? p.Mp / p.MaxMp : 0f);
            if (exp != null && p.Exp != lastExp)
            {
                lastExp = p.Exp;
                exp.Set(p.Level >= Leveling.MaxLevel ? 1f : Leveling.ProgressRatio(p.Level, p.Exp));
            }
            buffs.Refresh(hero);
            TickStates(dt, hpFrac, hpNow > 0, mpNow <= 0, buffs.Shielded);
        }
    }
}
