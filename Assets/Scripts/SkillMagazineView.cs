using UnityEngine;
using UnityEngine.UI;

namespace ZenoniaProto
{
    /// <summary>
    /// Touch "skill magazine": three r36 slots on an arc around the attack button (left, upper-left, above). A vertical swipe that starts on a
    /// slot (or on the invisible strip around the arc) cycles the page up / down like swapping a magazine: the icons slide out along the swipe,
    /// the next page slides in with a small scale "clack". A tap triggers the slot's action (on release, so a swipe never fires it).
    /// Dots next to the arc show the page. Pages come from SkillMagazine; the keys K / L still work whichever page is visible.
    /// </summary>
    public sealed class SkillMagazineView
    {
        const float Dur = 0.18f, Stagger = 0.025f, Slide = 30f;
        static readonly Vector2[] Pos = { new Vector2(-284f, 104f), new Vector2(-259f, 201f), new Vector2(-146f, 236f) }; // from the bottom-right
        static readonly float[] MpY = { -46f, -46f, 46f }; // MP label under the slot, above the top one (the attack disc is below it)
        static readonly Color DotOn = new Color(1f, 0.85f, 0.4f, 1f), DotOff = new Color(0.5f, 0.56f, 0.66f, 0.55f);

        readonly SkillMagazineSlot[] slots = new SkillMagazineSlot[SkillMagazine.SlotCount];
        readonly Image[] dots = new Image[SkillMagazine.MaxPages];
        HeroClassDef cls;
        int page, animDir, version;
        float animT = -1f;

        public static SkillMagazineView Build(RectTransform layer)
        {
            var v = new SkillMagazineView();
            Vector2 br = new Vector2(1f, 0f);
            // Thin invisible swipe strip around the arc, lowest sibling so the attack disc and the slots win their own touches.
            RectTransform strip = TouchControls.Place(layer, "SkillSwipeStrip", br, -215f, 160f, 240f);
            strip.sizeDelta = new Vector2(250f, 240f);
            TouchControls.Img(strip, null, new Color(0f, 0f, 0f, 0f), true);
            strip.gameObject.AddComponent<SwipeDetector>().onSwipe = v.Cycle;
            strip.SetAsFirstSibling();
            for (int i = 0; i < v.slots.Length; i++)
            {
                SkillMagazineSlot s = v.slots[i] = SkillMagazineSlot.Build(layer, i, Pos[i], MpY[i]);
                var sw = s.root.gameObject.AddComponent<SwipeDetector>();
                sw.onSwipe = v.Cycle;
                sw.onTap = () => v.Tap(s);
                sw.onPress = () => s.root.localScale = new Vector3(0.92f, 0.92f, 1f);
                sw.onRelease = () => s.root.localScale = Vector3.one;
            }
            for (int i = 0; i < v.dots.Length; i++)
            {
                float y = 236f - (i - (v.dots.Length - 1) * 0.5f) * 20f; // page 1 on top
                RectTransform d = TouchControls.Place(layer, "PageDot" + i, br, -84f, y, 18f);
                TouchControls.Img(d, UiSprites.Circle(), new Color(0.04f, 0.08f, 0.15f, 0.85f)); // dark plate so the dot reads on snow
                v.dots[i] = TouchControls.Child(d, "Dot", UiSprites.Circle(), 11f, DotOff);
            }
            return v;
        }

        /// <summary>Switch page by dir (+1 = up / next, -1 = down / previous, wraps). Ignored mid-animation or with a single page.</summary>
        public void Cycle(int dir)
        {
            if (cls == null || animT >= 0f || SkillMagazine.PageCount(cls) < 2) return;
            page = SkillMagazine.Cycle(cls, dir);
            animDir = dir; animT = 0f;
            foreach (SkillMagazineSlot s in slots) s.swapped = false;
            ShowDots();
        }

        void Tap(SkillMagazineSlot s)
        {
            if (animT >= 0f || s.entry.Locked) return;
            if (s.entry.IsSkill) SkillMagazine.CastRequested(s.entry.SkillIndex);
            else TouchControls.Press(s.entry.Key);
        }

        /// <summary>Per frame while the touch layer is shown.</summary>
        public void Tick(HeroController hero)
        {
            HeroClassDef c = hero.Class;
            int v = SkillMagazine.Version(); // ranks / loadout / class changed (skill tree, server push): rebind without animation
            if (c != cls || v != version)
            {
                cls = c; version = v; page = SkillMagazine.CurrentPage(c); animT = -1f;
                for (int i = 0; i < slots.Length; i++) { slots[i].Bind(SkillMagazine.Slot(c, page, i), c); slots[i].Pose(0f, 1f, 1f); }
                ShowDots();
            }
            if (animT >= 0f) Animate();
            foreach (SkillMagazineSlot s in slots) s.Live(hero, c);
        }

        /// <summary>Out (first half: slide + fade along the swipe), content swap, in (second half: slide from the other side + scale punch).</summary>
        void Animate()
        {
            animT += Time.unscaledDeltaTime;
            bool running = false;
            for (int i = 0; i < slots.Length; i++)
            {
                SkillMagazineSlot s = slots[i];
                float p = Mathf.Clamp01((animT - i * Stagger) / Dur);
                if (p >= 0.5f && !s.swapped) { s.swapped = true; s.Bind(SkillMagazine.Slot(cls, page, i), cls); }
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
            int n = SkillMagazine.PageCount(cls);
            for (int i = 0; i < dots.Length; i++)
            {
                dots[i].transform.parent.gameObject.SetActive(n > 1 && i < n);
                dots[i].color = i == page ? DotOn : DotOff;
            }
        }
    }
}
