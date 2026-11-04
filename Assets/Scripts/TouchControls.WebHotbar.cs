using UnityEngine;
using UnityEngine.UI;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// Web HUD (HudLayout.IsWeb) instead of the joystick / attack disc / skill arc: a gold-framed hotbar at the bottom centre
    /// (LMB attack, the three skill slots of the current magazine page on 1 / 2 / 3 with the cooldown sweep, HP / MP potions on Q / E),
    /// a key label under each slot, and a thin EXP bar under it. Tab (or a click on the page label) shows the next page.
    /// Every slot is also clickable. The hotbar moves right when the window is too narrow for it to clear the chat panel.
    /// </summary>
    public partial class TouchControls
    {
        const float BarPitch = 66f, BarSlotY = 8f, BarH = 88f, BarBottom = 30f, ChatRight = 346f;
        static readonly string[] BarKeys = { "LMB", "1", "2", "3", "Q", "E" };
        static readonly Vector2 Mid = new Vector2(0.5f, 0.5f);

        RectTransform bar, expRoot, expFill;
        Text pageLabel;
        readonly SkillMagazineSlot[] barSlots = new SkillMagazineSlot[SkillMagazine.SlotCount];
        HeroClassDef barClass;
        int barVersion, barPage = -1, shownExp = -1, shownLevel = -1;

        float BarWidth { get { return BarPitch * BarKeys.Length + 28f; } }

        void BuildWebHotbar(Transform parent)
        {
            float w = BarWidth;
            bar = UiWinKit.Anchored(parent, "WebHotbar", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), 0f, BarBottom, w, BarH);
            UiWinKit.Sliced(bar, "panel", 0.3f, true).color = new Color(1f, 1f, 1f, 0.94f); // raycast: a click on the bar never attacks
            for (int i = 0; i < BarKeys.Length; i++)
            {
                float x = -w * 0.5f + 14f + BarPitch * (i + 0.5f);
                if (i == 0) BarAttack(x);
                else if (i <= 3) BarSkill(i - 1, x);
                else if (i == 4) hpCount = BarPotion("HpPotion", x, true, out hpIcon);
                else mpCount = BarPotion("MpPotion", x, false, out mpIcon);
                KeyLabel(x, BarKeys[i]);
            }
            RectTransform page = Place(bar, "Page", new Vector2(1f, 1f), -34f, 10f, 56f);
            page.sizeDelta = new Vector2(64f, 18f);
            Img(page, null, new Color(0.03f, 0.05f, 0.1f, 0.85f), true);
            pageLabel = UiKit.Label(UiKit.Fill(page, "Text"), "", 11, UiWinKit.Cream, TextAnchor.MiddleCenter, true);
            page.gameObject.AddComponent<TapButton>().onTap = () => CycleWebPage();

            expRoot = UiWinKit.Anchored(parent, "WebExp", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), 0f, 10f, w + 40f, 14f);
            Image back = UiKit.Panel(expRoot, new Color(0.03f, 0.05f, 0.1f, 0.9f));
            back.gameObject.AddComponent<Outline>().effectColor = new Color(0.86f, 0.67f, 0.29f, 0.9f);
            UiKit.Label(UiKit.Rect(expRoot, "Caption", 8f, 0f, 34f, 14f), "EXP", 10, UiWinKit.Cream, TextAnchor.MiddleLeft, true);
            RectTransform track = UiKit.Fill(expRoot, "Track", 3f);
            track.offsetMin = new Vector2(40f, 3f);
            expFill = UiKit.Fill(track, "Fill");
            UiKit.Panel(expFill, new Color(0.98f, 0.76f, 0.16f));
            expFill.anchorMax = new Vector2(0f, 1f);
        }

        void BarAttack(float x)
        {
            Image icon, ring; Transform masked;
            RectTransform b = DiscButton(bar, "Attack", Mid, x, BarSlotY, 28f, UiArt.AttackFrame(), out icon, out ring, out masked);
            icon.sprite = UiArt.AttackIcon();
            icon.rectTransform.sizeDelta = new Vector2(40f, 40f);
            TouchButton.Add(b.gameObject, GameKey.Attack, true);
        }

        void BarSkill(int i, float x)
        {
            RectTransform holder = Place(bar, "SkillHolder" + i, Mid, x, BarSlotY, 72f);
            holder.localScale = new Vector3(0.78f, 0.78f, 1f); // the 72-unit magazine slot drawn at 56
            SkillMagazineSlot s = barSlots[i] = SkillMagazineSlot.Build(holder, i, Vector2.zero, 0f, Mid);
            s.HideMp();
            s.root.gameObject.AddComponent<TapButton>().onTap = () =>
            {
                if (s.entry.Locked) return;
                if (s.entry.IsSkill) SkillMagazine.CastRequested(s.entry.SkillIndex);
                else Press(s.entry.Key);
            };
        }

        Text BarPotion(string name, float x, bool hp, out Image icon)
        {
            Image ring; Transform masked;
            RectTransform b = DiscButton(bar, name, Mid, x, BarSlotY, 28f, UiArt.PotionFrame(), out icon, out ring, out masked);
            icon.sprite = LootSprites.Potion(hp);
            icon.rectTransform.sizeDelta = new Vector2(36f, 36f);
            Text count = UiKit.Label(Place(b, "Count", Mid, 20f, -13f, 40f), "", 11, Color.white, TextAnchor.MiddleCenter, true);
            count.rectTransform.sizeDelta = new Vector2(40f, 14f);
            TouchButton.Add(b.gameObject, hp ? GameKey.HpPotion : GameKey.MpPotion, false);
            return count;
        }

        void KeyLabel(float x, string key)
        {
            RectTransform k = Place(bar, "Key" + key, Mid, x, BarSlotY - 33f, 10f);
            k.sizeDelta = new Vector2(key.Length > 1 ? 34f : 22f, 15f);
            Image plate = Img(k, null, new Color(0.04f, 0.07f, 0.14f, 0.95f));
            plate.gameObject.AddComponent<Outline>().effectColor = new Color(0.86f, 0.67f, 0.29f, 0.9f);
            UiKit.Label(UiKit.Fill(k, "Text"), key, 11, Color.white, TextAnchor.MiddleCenter, true);
        }

        void CycleWebPage()
        {
            HeroController hero = GameSession.Hero;
            if (hero != null) SkillMagazine.Cycle(hero.Class, 1);
        }

        void TickWebHotbar(HeroController hero)
        {
            HeroClassDef c = hero.Class;
            int v = SkillMagazine.Version(), page = SkillMagazine.CurrentPage(c);
            if (c != barClass || v != barVersion || page != barPage)
            {
                barClass = c; barVersion = v; barPage = page;
                for (int i = 0; i < barSlots.Length; i++) { barSlots[i].Bind(SkillMagazine.Slot(c, page, i), c); barSlots[i].Pose(0f, 1f, 1f); }
                pageLabel.text = "Tab  " + (page + 1) + "/" + SkillMagazine.PageCount(c);
            }
            foreach (SkillMagazineSlot s in barSlots) s.Live(hero, c);

            // keep clear of the chat panel on narrow windows: shift right until the left edge of the EXP bar (the wider one) passes it
            float half = ((RectTransform)layer.parent).rect.width * 0.5f, shift = Mathf.Max(0f, ChatRight + (BarWidth + 40f) * 0.5f - half);
            bar.anchoredPosition = new Vector2(shift, BarBottom);
            expRoot.anchoredPosition = new Vector2(shift, 10f);

            PlayerState p = hero.Rpg;
            if (p == null || (p.Exp == shownExp && p.Level == shownLevel)) return;
            shownExp = p.Exp; shownLevel = p.Level;
            expFill.anchorMax = new Vector2(p.Level >= Leveling.MaxLevel ? 1f : Mathf.Clamp01(Leveling.ProgressRatio(p.Level, p.Exp)), 1f);
        }
    }
}
