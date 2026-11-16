using UnityEngine;
using UnityEngine.UI;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// One skill slot of the compact HUD (touch arc: round, thin rim; PC hotbar: rounded square), states of
    /// hud-compact-parts-board-states-v2.png:
    ///   ready        the skill icon in the thin ring, "17 MP" on the small pill at its foot
    ///   cooldown     icon dimmed under a dark sweep, "6.6s" in the middle
    ///   no MP        icon greyed blue, the cost red
    ///   locked       padlock: nothing to put in this magazine slot yet
    ///   empty        "+": the slot is free and a learned skill is not in the magazine (assign it in the skill window)
    /// `root` is the fixed hit area; `face` is what slides / punches during a page change (as SkillMagazineSlot, whose data it shares:
    /// SkillMagazine pages, the hero's own cooldowns). Texts are rebuilt only when their value changes.
    /// </summary>
    public sealed class PaperSkillSlot
    {
        static readonly Color CooldownTint = new Color(0.5f, 0.48f, 0.46f, 1f), NoMpTint = new Color(0.5f, 0.6f, 0.78f, 0.75f);
        const string NoMpRed = "<color=#FF6A5E>";

        public readonly RectTransform root, face;
        public SkillSlot entry = SkillSlot.Lock;
        public bool swapped; // content already switched during the current page animation
        readonly float radius;
        readonly string ringPiece;
        readonly CanvasGroup group;
        readonly GameObject wellGo, rimGo, pill, plus, padlock;
        readonly Image icon, sweep, state, ring;
        readonly Text cdText, cost;
        SkillDef def;
        int mpCost, shownTenths = -1, shownLook = -1;

        /// <summary>`corner` = anchor of `pos` in the parent; d = size in canvas units; `square` = the hotbar's rounded square instead of the ring.</summary>
        public PaperSkillSlot(Transform parent, int index, Vector2 corner, Vector2 pos, float d, bool square = false)
        {
            radius = square ? 8f : d * 0.5f;
            ringPiece = square ? PaperPiece.CHotbarSlot : PaperPiece.CSkillRing;
            Vector2 mid = new Vector2(0.5f, 0.5f);
            root = TouchControls.Place(parent, "Slot" + index, corner, pos.x, pos.y, d);
            TouchControls.Img(root, null, Color.clear, true); // invisible hit square, stays put while the face slides
            face = TouchControls.Place(root, "Face", mid, 0f, 0f, d);
            group = face.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false; // the root takes the touches

            // Locked / empty: one plate with its mark. The cut pieces carry the padlock / "+" themselves.
            state = PaperHudCompact.Plate(UiKit.Fill(face, "State"), PaperPiece.CSkillLocked, radius);
            Image lockMark = TouchControls.Child(state.transform, "Padlock", PaperArt.Get(PaperPiece.MarkLock), d * 0.4f, new Color(1f, 1f, 1f, 0.8f));
            lockMark.enabled = lockMark.sprite != null;
            padlock = lockMark.gameObject;
            plus = PaperTheme.Caption(state.transform, "+", Mathf.RoundToInt(d * 0.5f), PaperTheme.TextLight).gameObject;

            // Round: the cut ring is hollow and lies over the icon. Square: the cut hotbar slot has a dark middle, so it lies under the well.
            Sprite ringArt = PaperArt.Get(ringPiece);
            if (ringArt != null && square) rimGo = TouchControls.Img(UiKit.Fill(face, "Rim"), ringArt, Color.white).gameObject;
            RectTransform well = PaperHudCompact.MaskedWell(face, radius, ringArt == null, square ? 3f : 2f);
            wellGo = well.gameObject;
            if (ringArt == null) rimGo = face.Find("Rim").gameObject;
            icon = TouchControls.Child(well, "Icon", null, d - 4f, Color.white);
            sweep = PaperHudCompact.CooldownSweep(well, d, square);
            if (ringArt != null && !square) ring = TouchControls.Img(UiKit.Fill(face, "Ring"), ringArt, Color.white);

            cdText = PaperTheme.Caption(UiKit.Fill(face, "CdText"), "", Mathf.RoundToInt(d * 0.3f), PaperTheme.TextLight);
            RectTransform p = TouchControls.Place(face, "Cost", mid, 0f, -d * 0.4f, d);
            p.sizeDelta = new Vector2(Mathf.Max(34f, d * 0.7f), 13f);
            PaperArt.Draw(p, PaperPiece.CCostPill, new Color(0.03f, 0.04f, 0.06f, 0.72f), Color.clear, 6.5f);
            cost = PaperTheme.Caption(p, "", PaperTheme.BarSize, PaperTheme.TextLight, PaperInk.Plain);
            pill = p.gameObject;
        }

        /// <summary>"6.8s" from tenths of a second.</summary>
        public static string Seconds(int tenths) { return (tenths / 10) + "." + (tenths % 10) + "s"; }

        /// <summary>The ring's pressed piece while a finger is on the slot (when both pieces are cut; the arc also squashes the slot).</summary>
        public void SetPressed(bool down)
        {
            if (ring == null) return;
            Sprite s = down ? PaperArt.Get(ringPiece + PaperPiece.Pressed) : null;
            ring.sprite = s != null ? s : PaperArt.Get(ringPiece);
        }

        /// <summary>Shows slot `index` of magazine page `page` for class `cls`.</summary>
        public void Bind(HeroClassDef cls, int page, int index)
        {
            entry = SkillMagazine.Slot(cls, page, index);
            def = SkillMagazine.Def(entry);
            shownTenths = -1; shownLook = -1;
            bool locked = entry.Locked;
            state.gameObject.SetActive(locked);
            wellGo.SetActive(!locked);
            if (rimGo != null) rimGo.SetActive(!locked);
            if (ring != null) ring.gameObject.SetActive(!locked);
            pill.SetActive(def != null);
            cdText.text = "";
            sweep.enabled = false;
            icon.color = Color.white;
            if (locked)
            {
                bool free = CanAssign();
                bool art = PaperArt.Redraw(state, free ? PaperPiece.CSkillEmpty : PaperPiece.CSkillLocked, PaperHudCompact.Dark, PaperHudCompact.Rim, radius);
                plus.SetActive(free && !art);
                padlock.SetActive(!free && !art);
                return;
            }
            if (def != null)
            {
                PlayerState p = RpgSession.Player;
                mpCost = def.MpAt(p != null ? p.Skills[def.Index] : 1);
                icon.sprite = SkillIcons.Get(def);
            }
            else icon.sprite = entry.Key == GameKey.Dash ? TouchControls.SkillSprite(cls, cls.DashIcon) : null;
            icon.enabled = icon.sprite != null;
        }

        /// <summary>A learned skill is not in the magazine: a free slot invites it ("+") instead of showing the padlock.</summary>
        static bool CanAssign()
        {
            PlayerState p = RpgSession.Player;
            if (p == null) return false;
            for (int i = 0; i < SkillDef.PerClass; i++) if (p.Skills[i] > 0 && p.Loadout.SlotOf(i) < 0) return true;
            return false;
        }

        /// <summary>Per-frame cooldown / MP state of a tree skill, read from the hero's own cooldowns (server-confirmed online).</summary>
        public void Live(HeroController hero)
        {
            if (def == null) return;
            float left = hero.SkillCdLeftMs(def.Index);
            bool enoughMp = hero.Mp >= mpCost, cooling = left > 0f;
            int look = (cooling ? 1 : 0) + (enoughMp ? 0 : 2);
            if (look != shownLook)
            {
                shownLook = look;
                icon.color = cooling ? CooldownTint : enoughMp ? Color.white : NoMpTint;
                cost.text = enoughMp ? mpCost + " MP" : NoMpRed + mpCost + " MP</color>";
                sweep.enabled = cooling;
            }
            if (cooling) sweep.fillAmount = Mathf.Clamp01(left / Mathf.Max(1f, def.CooldownMs));
            int tenths = cooling ? Mathf.CeilToInt(left / 100f) : 0;
            if (tenths == shownTenths) return;
            shownTenths = tenths;
            cdText.text = tenths > 0 ? Seconds(tenths) : "";
        }

        /// <summary>Animation pose: offset along Y, alpha factor and scale of the face.</summary>
        public void Pose(float offsetY, float alpha, float scale)
        {
            face.anchoredPosition = new Vector2(0f, offsetY);
            group.alpha = alpha;
            face.localScale = new Vector3(scale, scale, 1f);
        }
    }
}
