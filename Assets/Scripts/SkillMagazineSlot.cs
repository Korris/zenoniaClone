using UnityEngine;
using UnityEngine.UI;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// One of the three round slots of the skill magazine. `root` is the fixed hit area, `face` (frame, icon, cooldown sweep, MP text, lock) is what
    /// slides / punches during a page change. Shows the bound action of its entry; locked entries are a dimmed frame with a padlock.
    /// </summary>
    public sealed class SkillMagazineSlot
    {
        public RectTransform root, face;
        public SkillSlot entry = SkillSlot.Lock;
        public bool swapped;                 // content already switched during the current page animation
        SkillDef def; int mpCost;            // tree skill shown (null for Dash / empty) and its MP at the learned rank
        CanvasGroup group;
        Image icon, ring, cd, lockImg;
        Text cdText, mpText;
        float baseAlpha = 1f;

        public static SkillMagazineSlot Build(Transform parent, int index, Vector2 pos, float mpOffsetY)
        {
            var s = new SkillMagazineSlot();
            Vector2 br = new Vector2(1f, 0f);
            s.root = TouchControls.Place(parent, "Slot" + index, br, pos.x, pos.y, 72f);
            TouchControls.Img(s.root, null, new Color(0f, 0f, 0f, 0f), true); // invisible hit disc, stays put while the face slides
            Transform masked;
            s.face = TouchControls.DiscButton(s.root, "Face", new Vector2(0.5f, 0.5f), 0f, 0f, 36f, UiArt.SkillFrame(), out s.icon, out s.ring, out masked);
            s.group = s.face.gameObject.AddComponent<CanvasGroup>();
            s.group.blocksRaycasts = false; // the root takes the touches
            s.cd = TouchControls.Child(masked, "Cooldown", UiSprites.Circle(), 58f, new Color(0.01f, 0.05f, 0.1f, 0.73f));
            s.cd.type = Image.Type.Filled; s.cd.fillMethod = Image.FillMethod.Radial360; s.cd.fillOrigin = (int)Image.Origin360.Top; s.cd.fillClockwise = true;
            s.lockImg = TouchControls.Child(s.face, "Lock", LockGlyph.Get(), 30f, new Color(0.78f, 0.7f, 0.5f, 0.9f));
            s.cdText = UiKit.Label(UiKit.Fill(s.face, "CdText"), "", 17, Color.white, TextAnchor.MiddleCenter, true);
            s.mpText = UiKit.Label(TouchControls.Place(s.face, "Mp", new Vector2(0.5f, 0.5f), 0f, mpOffsetY, 70f), "", 10, Color.white, TextAnchor.MiddleCenter, true);
            s.mpText.rectTransform.sizeDelta = new Vector2(70f, 14f);
            return s;
        }

        /// <summary>Shows `e` (the entry of the page now visible) for class `cls`.</summary>
        public void Bind(SkillSlot e, HeroClassDef cls)
        {
            entry = e;
            bool locked = e.Locked;
            lockImg.enabled = locked;
            baseAlpha = locked ? 0.5f : 1f;
            icon.enabled = !locked;
            icon.sprite = null;
            mpText.text = ""; cdText.text = ""; cd.enabled = false; ring.color = TouchControls.Ready; icon.color = Color.white;
            def = SkillMagazine.Def(e);
            if (locked) return;
            if (def != null)
            {
                PlayerState p = RpgSession.Player;
                mpCost = def.MpAt(p != null ? p.Skills[def.Index] : 1);
                icon.sprite = SkillIcons.Get(def);
                mpText.text = mpCost + " MP";
            }
            else if (e.Key == GameKey.Dash) icon.sprite = TouchControls.SkillSprite(cls, cls.DashIcon);
        }

        /// <summary>Per-frame cooldown sweep / MP state of a tree skill, read from the hero's own cooldowns (server-confirmed online).</summary>
        public void Live(HeroController hero, HeroClassDef cls)
        {
            if (def == null) return;
            float left = hero.SkillCdLeftMs(def.Index);
            bool enoughMp = hero.Mp >= mpCost, ready = left <= 0f && enoughMp;
            icon.color = new Color(1f, 1f, 1f, ready ? 1f : 0.4f);
            ring.color = ready ? TouchControls.Ready : TouchControls.NotReady;
            cd.fillAmount = left > 0f ? Mathf.Clamp01(left / Mathf.Max(1f, def.CooldownMs)) : 0f;
            cd.enabled = left > 0f;
            cdText.text = left > 0f ? (left / 1000f).ToString("F1") : "";
            mpText.color = enoughMp ? new Color(0.67f, 0.88f, 0.98f) : new Color(1f, 0.54f, 0.59f);
        }

        /// <summary>Animation pose: offset along Y, alpha factor and scale of the face.</summary>
        public void Pose(float offsetY, float alpha, float scale)
        {
            face.anchoredPosition = new Vector2(0f, offsetY);
            group.alpha = baseAlpha * alpha;
            face.localScale = new Vector3(scale, scale, 1f);
        }
    }
}
