using UnityEngine;
using UnityEngine.UI;

namespace ZenoniaProto
{
    /// <summary>
    /// Touch skill magazine of the compact HUD (hud-compact-mobile-v1.png): three round slots with thin rims on an arc over the attack
    /// button, three page dots at the outer end of the arc. No belt. Same behaviour as SkillMagazineView, which it replaces while
    /// UiScreens.UseNew(UiScreen.Hud) is on: tap = the slot's action (on release), hold = the Warrior's farm repeat, a vertical swipe on
    /// a slot or on the strip around the arc changes the page with the slide + "clack" animation. Pages come from SkillMagazine.
    /// </summary>
    public sealed class PaperSkillArc
    {
        const float Size = HudAnchors.SkillSize, Arc = HudAnchors.SkillArc, DotSize = 9f, DotPitch = 13f;
        const float Dur = 0.18f, Stagger = 0.025f, Slide = 30f;
        static readonly Vector2 BottomRight = new Vector2(1f, 0f);
        static readonly float[] Angles = { 150f, 100f, 50f }; // degrees around the attack button, slot 1 on the left

        readonly PaperSkillSlot[] slots = new PaperSkillSlot[SkillMagazine.SlotCount];
        readonly Image[] dots = new Image[SkillMagazine.MaxPages];
        HeroClassDef cls;
        int page, animDir, version, shownDot = -1;
        float animT = -1f;

        /// <summary>`attack` = centre of the attack button from the bottom-right corner of `layer`.</summary>
        public PaperSkillArc(RectTransform layer, Vector2 attack)
        {
            // Invisible swipe strip around the arc, lowest sibling so the slots win their own touches.
            RectTransform strip = TouchControls.Place(layer, "SkillSwipeStrip", BottomRight, attack.x - 8f, attack.y + Arc * 0.75f, 100f);
            strip.sizeDelta = new Vector2(Arc * 2f + Size + 24f, Size + Arc * 0.6f);
            TouchControls.Img(strip, null, Color.clear, true);
            strip.gameObject.AddComponent<SwipeDetector>().onSwipe = Cycle;

            for (int i = 0; i < slots.Length; i++)
            {
                float a = Angles[i] * Mathf.Deg2Rad;
                AddSlot(layer, i, attack + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * Arc);
            }
            float last = Angles[Angles.Length - 1] * Mathf.Deg2Rad; // dots: a short column right of the last slot, page 1 on top
            Vector2 dot = attack + new Vector2(Mathf.Cos(last) * Arc + Size * 0.5f + 12f, Mathf.Sin(last) * Arc);
            for (int i = 0; i < dots.Length; i++)
            {
                RectTransform rt = TouchControls.Place(layer, "PageDot" + i, BottomRight, dot.x, dot.y - (i - (dots.Length - 1) * 0.5f) * DotPitch, DotSize);
                dots[i] = PaperArt.Draw(rt, PaperPiece.CPageDotOff, PaperHudCompact.Dark, PaperHudCompact.Rim, DotSize * 0.5f);
            }
        }

        void AddSlot(RectTransform layer, int i, Vector2 pos)
        {
            PaperSkillSlot s = slots[i] = new PaperSkillSlot(layer, i, BottomRight, pos, Size);
            bool holdingFarm = false;
            var sw = s.root.gameObject.AddComponent<SwipeDetector>();
            sw.onSwipe = Cycle;
            sw.onTap = () => Tap(s);
            sw.onHold = () =>
            {
                if (animT >= 0f || cls == null || cls.Id != HeroClass.Warrior || !s.entry.IsSkill || s.entry.SkillIndex != 0) return false;
                TouchControls.SetHeld(GameKey.Skill1, true);
                holdingFarm = true;
                Tap(s);
                return true;
            };
            sw.onPress = () => { s.root.localScale = new Vector3(0.92f, 0.92f, 1f); s.SetPressed(true); };
            sw.onRelease = () =>
            {
                s.root.localScale = Vector3.one;
                s.SetPressed(false);
                if (holdingFarm) TouchControls.SetHeld(GameKey.Skill1, false);
                holdingFarm = false;
            };
        }

        /// <summary>Switch page by dir (+1 = up / next, -1 = down / previous, wraps). Ignored mid-animation.</summary>
        public void Cycle(int dir)
        {
            if (cls == null || animT >= 0f || SkillMagazine.PageCount(cls) < 2) return;
            page = SkillMagazine.Cycle(cls, dir);
            animDir = dir; animT = 0f;
            foreach (PaperSkillSlot s in slots) s.swapped = false;
            ShowDots();
        }

        void Tap(PaperSkillSlot s)
        {
            if (animT >= 0f || s.entry.Locked) return;
            if (s.entry.IsSkill) SkillMagazine.CastRequested(s.entry.SkillIndex);
            else TouchControls.Press(s.entry.Key);
        }

        /// <summary>Per frame while the touch layer is shown.</summary>
        public void Tick(HeroController hero)
        {
            HeroClassDef c = hero.Class;
            int v = SkillMagazine.Version(); // ranks / loadout / class changed (skill window, server push): rebind without animation
            if (c != cls || v != version)
            {
                cls = c; version = v; page = SkillMagazine.CurrentPage(c); animT = -1f;
                for (int i = 0; i < slots.Length; i++) { slots[i].Bind(c, page, i); slots[i].Pose(0f, 1f, 1f); }
                ShowDots();
            }
            if (animT >= 0f) Animate();
            foreach (PaperSkillSlot s in slots) s.Live(hero);
        }

        /// <summary>Out (first half: slide + fade along the swipe), content swap, in (second half: slide from the other side + scale punch).</summary>
        void Animate()
        {
            animT += Time.unscaledDeltaTime;
            bool running = false;
            for (int i = 0; i < slots.Length; i++)
            {
                PaperSkillSlot s = slots[i];
                float p = Mathf.Clamp01((animT - i * Stagger) / Dur);
                if (p >= 0.5f && !s.swapped) { s.swapped = true; s.Bind(cls, page, i); }
                if (p < 0.5f)
                {
                    float q = Ease(p * 2f);
                    s.Pose(animDir * Slide * q, 1f - q, 1f);
                }
                else
                {
                    float q = (p - 0.5f) * 2f;
                    s.Pose(-animDir * Slide * (1f - Ease(q)), Ease(q), 1f + 0.14f * Mathf.Sin(Mathf.PI * q));
                }
                if (p < 1f) running = true;
            }
            if (!running) animT = -1f;
        }

        static float Ease(float t) { return 1f - (1f - t) * (1f - t); }

        void ShowDots()
        {
            if (page == shownDot) return;
            shownDot = page;
            ShowPage(dots, page);
        }

        /// <summary>Lights dot `page` of a row of page dots (also the PC hotbar's).</summary>
        internal static void ShowPage(Image[] dots, int page)
        {
            for (int i = 0; i < dots.Length; i++)
                PaperArt.Redraw(dots[i], i == page ? PaperPiece.CPageDotOn : PaperPiece.CPageDotOff, i == page ? PaperTheme.Gold : PaperHudCompact.Dark, i == page ? Color.clear : PaperHudCompact.Rim, DotSize * 0.5f);
        }
    }
}
