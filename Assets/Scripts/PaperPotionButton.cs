using UnityEngine;
using UnityEngine.UI;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// Quick potion button of the compact HUD (touch: left of the attack button; PC: a hotbar slot): thin ring, the potion of the size
    /// it would drink, "x20" under it (PC: in the slot's lower right). States of hud-compact-parts-board-states-v2.png: ready, empty
    /// (grey flask, red "x0") and cooldown (dark sweep + seconds). The game has no potion cooldown yet, so that state is only shown by
    /// ShowCooldown (capture runs) until a rule exists. Holds its GameKey like the old disc; texts change only when their number changes.
    /// </summary>
    public sealed class PaperPotionButton
    {
        static readonly Color EmptyIcon = new Color(0.62f, 0.62f, 0.66f, 0.55f);

        public readonly RectTransform Root;
        readonly bool hp;
        readonly Image icon, sweep, ring;
        readonly Text count, cdText;
        int shownSize = -1, shownCount = -1, shownTenths;
        float cdLeft, cdTotal;

        /// <summary>
        /// `corner` = anchor of (x, y) in the parent; d = diameter of the ring in canvas units (the touch cell is never under 44).
        /// `square` = the PC hotbar's rounded slot, with the count inside it.
        /// </summary>
        public PaperPotionButton(Transform parent, string name, Vector2 corner, float x, float y, float d, bool hp, bool square = false)
        {
            this.hp = hp;
            RectTransform art;
            Root = PaperHudCompact.Cell(parent, name, corner, x, y, Mathf.Max(d, 44f), d, out art);
            float radius = square ? 8f : d * 0.5f;
            // Round: the cut ring is hollow and lies over the icon. Square: the cut hotbar slot has a dark middle, so it lies under the well.
            Sprite ringArt = PaperArt.Get(square ? PaperPiece.CHotbarSlot : PaperPiece.CPotionRing);
            if (ringArt != null && square) TouchControls.Img(UiKit.Fill(art, "Rim"), ringArt, Color.white);
            RectTransform well = PaperHudCompact.MaskedWell(art, radius, ringArt == null, square ? 3f : 2f);
            icon = TouchControls.Child(well, "Icon", LootSprites.Potion(hp), d * 0.66f, Color.white);
            icon.preserveAspect = true;
            sweep = PaperHudCompact.CooldownSweep(well, d, square);
            if (ringArt != null && !square) ring = TouchControls.Img(UiKit.Fill(art, "Ring"), ringArt, Color.white);
            cdText = PaperTheme.Caption(UiKit.Fill(art, "CdText"), "", Mathf.RoundToInt(d * 0.3f), PaperTheme.TextLight);
            RectTransform c = TouchControls.Place(art, "Count", new Vector2(0.5f, 0.5f), square ? d * 0.16f : 0f, square ? -d * 0.3f : -d * 0.56f, d);
            c.sizeDelta = new Vector2(d, 14f);
            count = PaperTheme.Caption(c, "", square ? PaperTheme.BarSize : 12, PaperTheme.TextLight);
            PaperHudPress.Hold(Root.gameObject, hp ? GameKey.HpPotion : GameKey.MpPotion);
        }

        /// <summary>Capture runs: the cooldown look, counting down from `seconds` again and again.</summary>
        public void ShowCooldown(float seconds) { cdLeft = cdTotal = seconds; }

        public void Tick(PlayerState p, float dt)
        {
            int size = p.QuickPotionSize(hp), n = p.PotionCount(hp, size);
            if (size != shownSize) { shownSize = size; icon.sprite = LootSprites.Potion(hp, size); }
            if (n != shownCount)
            {
                bool wasEmpty = shownCount == 0;
                shownCount = n;
                count.text = "x" + n;
                count.color = n > 0 ? PaperTheme.TextLight : PaperHudCompact.NoMpRed;
                icon.color = n > 0 ? Color.white : EmptyIcon;
                if (ring != null && wasEmpty != (n == 0))
                {
                    Sprite empty = n == 0 ? PaperArt.Get(PaperPiece.CPotionRingEmpty) : null;
                    ring.sprite = empty != null ? empty : PaperArt.Get(PaperPiece.CPotionRing);
                    icon.enabled = empty == null; // the cut "empty" ring is a whole plate with its own grey flask
                }
            }
            if (cdTotal <= 0f) return;
            cdLeft -= dt;
            if (cdLeft <= 0f) cdLeft = cdTotal;
            sweep.enabled = true;
            sweep.fillAmount = cdLeft / cdTotal;
            int tenths = Mathf.CeilToInt(cdLeft * 10f);
            if (tenths != shownTenths) { shownTenths = tenths; cdText.text = PaperSkillSlot.Seconds(tenths); }
        }
    }
}
