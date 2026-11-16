using UnityEngine;
using UnityEngine.UI;

namespace ZenoniaProto
{
    /// <summary>
    /// The states of the compact status block (hud-compact-parts-board-states-v2.png, section 1, plus the two kept from the first HUD):
    ///   Low HP (under 30 %)   a red rim around the HP bar and around the portrait pulse softly
    ///   No MP                 the MP bar, its name and its numbers go grey
    ///   Shield active         a thin blue rim around the HP bar, while a Shield buff is on the hero
    ///   Damage taken          the lost part of the HP bar stays bright for 0.4 s, then drains; "-142" rises beside the bars
    /// Every switch touches the UI only when its state flips; the pulse and the drain only change colours / anchors (no allocations).
    /// </summary>
    public sealed partial class PlayerStatusBadge
    {
        public const float LowHpFraction = 0.3f;
        const float ChaseHoldSeconds = 0.4f, ChaseDrainPerSecond = 0.9f, HitSeconds = 0.9f, HitRise = 14f, RimWidth = 1.5f;
        static readonly Color ChaseColor = PaperTheme.Hex("#FFE2B8"), HitColor = PaperTheme.Hex("#FF5B45"), Greyed = new Color(0.55f, 0.55f, 0.6f, 1f);

        Image lowRim, lowFace, shieldRim, chase;
        RectTransform chaseRt, hitRt;
        Text hitText;
        Vector2 hitBase;
        Color mpFrameColor;
        bool lowOn, mpEmptyOn, shieldOn;
        float pulse, chaseFrac, chaseShown = -1f, chaseHold, hitAge = -1f;

        /// <summary>The coloured rims, built first so they lie under the bars and the portrait and show only as an outline.</summary>
        void BuildRims(Sizes z)
        {
            shieldRim = Rim("ShieldRim", z.BarX, z.HpY, z.BarW, z.BarH, PaperTheme.Sky);
            lowRim = Rim("LowHpRim", z.BarX, z.HpY, z.BarW, z.BarH, PaperTheme.BarHp);
            lowFace = Rim("LowHpFace", 0f, (z.Height - z.Face) * 0.5f, z.Face, z.Face, PaperTheme.BarHp);
        }

        Image Rim(string name, float x, float y, float w, float h, Color color)
        {
            RectTransform rt = UiKit.Rect(Root, name, x - RimWidth, y - RimWidth, w + RimWidth * 2f, h + RimWidth * 2f);
            Image img = PaperArt.Plate(rt, color, Color.clear, h * 0.5f + RimWidth);
            img.gameObject.SetActive(false);
            return img;
        }

        void BuildStates(Sizes z)
        {
            // The part of the HP bar just lost: a bright fill under the red one, in the bar's own track.
            chaseRt = UiKit.Fill(hp.Track, "Chase");
            chaseRt.SetAsFirstSibling();
            chase = PaperArt.Draw(chaseRt, PaperPiece.HcBarFill, ChaseColor, Color.clear, z.BarH * 0.4f);
            chase.color = ChaseColor;
            chase.enabled = false;
            mpFrameColor = mp.Frame.color;
            // The number rises beside the right end of the bars, from the MP bar up to the level row (the block sits at the top edge of
            // the screen, so it cannot start above the HP bar).
            hitText = PaperTheme.Title(Root, "Hit", z.Width + 4f, z.MpY - 2f, 60f, 18f, "", PaperTheme.SectionSize, HitColor, PaperInk.Outlined);
            hitText.horizontalOverflow = HorizontalWrapMode.Overflow;
            hitRt = hitText.rectTransform;
            hitBase = hitRt.anchoredPosition;
            hitText.gameObject.SetActive(false);
        }

        /// <summary>HP went down by `amount`; `fromFrac` = the bar before the hit.</summary>
        void ShowHit(int amount, float fromFrac)
        {
            if (chaseFrac < fromFrac) chaseFrac = Mathf.Clamp01(fromFrac);
            chaseHold = ChaseHoldSeconds;
            hitText.text = "-" + amount;
            hitAge = 0f;
            hitText.gameObject.SetActive(true);
        }

        /// <summary>Another hero took the block (class change): nothing of the old one keeps animating.</summary>
        void ResetStates()
        {
            lastHp = -1;
            chaseFrac = 0f;
            hitAge = -1f;
            hitText.gameObject.SetActive(false);
        }

        void TickStates(float dt, float hpFrac, bool alive, bool mpEmpty, bool shielded)
        {
            bool low = alive && hpFrac < LowHpFraction;
            if (low != lowOn) { lowOn = low; pulse = 0f; lowRim.gameObject.SetActive(low); lowFace.gameObject.SetActive(low); }
            bool shieldShown = shielded && !low; // one rim at a time: the red warning wins
            if (shieldShown != shieldOn) { shieldOn = shieldShown; shieldRim.gameObject.SetActive(shieldShown); }
            if (mpEmpty != mpEmptyOn)
            {
                mpEmptyOn = mpEmpty;
                mp.Frame.color = mpEmpty ? mpFrameColor * Greyed : mpFrameColor;
                mp.Label.color = mp.Value.color = mpEmpty ? PaperTheme.DisabledText : PaperTheme.TextLight;
            }
            if (low)
            {
                pulse += dt;
                float a = 0.6f + 0.4f * Mathf.Cos(pulse * 5f); // about 0.8 pulses a second: noticeable, not a strobe
                HudCompact.SetAlpha(lowRim, a);
                HudCompact.SetAlpha(lowFace, a);
            }

            if (chaseHold > 0f) chaseHold -= dt;
            else if (chaseFrac > hpFrac) chaseFrac = Mathf.MoveTowards(chaseFrac, hpFrac, dt * ChaseDrainPerSecond);
            if (chaseFrac < hpFrac) chaseFrac = hpFrac; // healed past it
            if (!Mathf.Approximately(chaseFrac, chaseShown))
            {
                chaseShown = chaseFrac;
                chaseRt.anchorMax = new Vector2(chaseFrac, 1f);
            }
            bool showChase = chaseFrac > hpFrac + 0.002f;
            if (chase.enabled != showChase) chase.enabled = showChase;

            if (hitAge < 0f) return;
            hitAge += dt;
            float k = hitAge / HitSeconds;
            if (k >= 1f) { hitAge = -1f; hitText.gameObject.SetActive(false); return; }
            hitRt.anchoredPosition = hitBase + new Vector2(0f, HitRise * k);
            HudCompact.SetAlpha(hitText, k < 0.6f ? 1f : 1f - (k - 0.6f) / 0.4f);
        }
    }
}
