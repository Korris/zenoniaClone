using UnityEngine;
using UnityEngine.UI;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// The states of the compact boss bar on top of the plain one:
    ///   cast      a thin purple line under the HP bar, "Frozen Ground Stomp 1.6s", filling until the skill lands. Skill and time left come
    ///             from Monster.WantedCast, i.e. the telegraph event's own strike instant (the same clock as the ground warning), never a
    ///             local timer. Only a Wanted Boss casts: a map boss has no telegraphed skills.
    ///   enraged   Monster.WantedEnraged (under 30 % HP): a red rim around the bar, a red tint on the portrait, a small ENRAGED label at the
    ///             right end of the name row
    ///   defeated  the boss the bar was showing died: grey portrait + DEFEATED for DefeatedSeconds, then the bar goes away
    /// </summary>
    public sealed partial class BossBarView
    {
        const float DefeatedSeconds = 2f, DefeatedW = 104f, DefeatedH = 20f, DefeatedWidth = Face + 2f + DefeatedW;
        const float CastH = 13f, CastGap = 2f, RimWidth = 1.5f;
        static readonly Color EnragedTint = new Color(1f, 0.62f, 0.58f), DefeatedTint = new Color(0.55f, 0.55f, 0.6f), CastFill = PaperTheme.Hex("#9A4DF0");
        static readonly Color EnragedRim = PaperTheme.Hex("#FF4A2A"), EnragedFill = PaperTheme.Hex("#FF3B1F");

        RectTransform enragedRt, defeatedRt;
        Image enrageRim;
        HudCompactBar cast;
        WantedSkillDef castSkill;
        bool enragedOn, castOn;
        int castTenths = -1;
        float defeatedLeft, pulse;

        void BuildEnrageRim()
        {
            RectTransform rt = Stretch(root, "EnragedRim", BarLeft - RimWidth, BarY - RimWidth, -RimWidth, BarH + RimWidth * 2f);
            enrageRim = PaperArt.Plate(rt, EnragedRim, Color.clear, BarH * 0.5f + RimWidth);
        }

        void BuildEnrageLabel()
        {
            enragedRt = UiWinKit.Anchored(root, "Enraged", new Vector2(1f, 1f), new Vector2(1f, 1f), -2f, 0f, 70f, NameH);
            Text t = PaperTheme.Title(enragedRt, "Text", 0f, 0f, 70f, NameH, "ENRAGED", PaperTheme.BodySize, EnragedRim, PaperInk.Outlined, TextAnchor.MiddleRight);
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
        }

        void BuildCast()
        {
            cast = new HudCompactBar(Stretch(root, "Cast", BarLeft + 10f, Height + CastGap, 10f, CastH), CastH, PaperPiece.HcCastFrame, PaperPiece.HcCastFill, CastFill, CastH * 0.17f, CastH * 0.17f, null, TextAnchor.MiddleCenter);
            cast.Root.gameObject.SetActive(false);
        }

        void BuildDefeated()
        {
            defeatedRt = UiKit.Rect(root, "Defeated", Face + 2f, (Height - DefeatedH) * 0.5f, DefeatedW, DefeatedH);
            HudCompact.Capsule(defeatedRt, null, HudCompact.Dark, Color.clear, DefeatedH * 0.5f);
            PaperTheme.Caption(defeatedRt, "DEFEATED", PaperTheme.BodySize, PaperTheme.TextLight);
            defeatedRt.gameObject.SetActive(false);
        }

        /// <summary>A boss just took the bar: everything back to the plain state.</summary>
        void BindStates()
        {
            enragedOn = castOn = false; castSkill = null; castTenths = -1;
            enrageRim.gameObject.SetActive(false);
            enragedRt.gameObject.SetActive(false);
            cast.Root.gameObject.SetActive(false);
            defeatedRt.gameObject.SetActive(false);
            bar.SetColor(PaperTheme.BarHp);
            portrait.color = Color.white;
            if (ring != null) ring.color = PaperArt.Has(PaperPiece.HcBossRing) ? Color.white : HudCompact.Rim;
        }

        void RefreshStates(Monster boss)
        {
            bool enraged = boss.WantedEnraged;
            if (enraged != enragedOn)
            {
                enragedOn = enraged; pulse = 0f;
                enrageRim.gameObject.SetActive(enraged);
                enragedRt.gameObject.SetActive(enraged);
                bar.SetColor(enraged ? EnragedFill : PaperTheme.BarHp);
                portrait.color = enraged ? EnragedTint : Color.white;
            }
            if (enraged)
            {
                pulse += Time.unscaledDeltaTime;
                HudCompact.SetAlpha(enrageRim, 0.7f + 0.3f * Mathf.Cos(pulse * 6f));
            }
            WantedSkillDef skill;
            float leftMs;
            bool casting = boss.WantedCast(out skill, out leftMs);
            if (casting != castOn) { castOn = casting; cast.Root.gameObject.SetActive(casting); castTenths = -1; }
            if (!casting) return;
            cast.Set(1f - Mathf.Clamp01(leftMs / Mathf.Max(1, skill.TelegraphMs)));
            int tenths = Mathf.CeilToInt(leftMs / 100f);
            if (tenths == castTenths && skill == castSkill) return;
            castTenths = tenths; castSkill = skill;
            cast.Value.text = skill.Name + " " + (tenths / 10) + "." + (tenths % 10) + "s";
        }

        /// <summary>No boss to show any more: it died (DEFEATED for a moment) or the hero walked away / the look was switched off (gone at once).</summary>
        void TickGone(bool on, float dt)
        {
            if (defeatedLeft > 0f)
            {
                defeatedLeft -= dt;
                if (defeatedLeft <= 0f || !on) Hide();
                return;
            }
            bool destroyed = shown == null; // Unity null: the monster object is already gone
            bool died = on && (destroyed ? lastFrac < 0.05f : shown.IsDying);
            if (!died) { Hide(); return; }
            defeatedLeft = DefeatedSeconds;
            bar.Root.gameObject.SetActive(false);
            nameRt.gameObject.SetActive(false);
            tagRt.gameObject.SetActive(false);
            enrageRim.gameObject.SetActive(false);
            enragedRt.gameObject.SetActive(false);
            cast.Root.gameObject.SetActive(false);
            castOn = false;
            defeatedRt.gameObject.SetActive(true);
            portrait.color = DefeatedTint;
            if (ring != null) ring.color = DefeatedTint;
            Layout(DefeatedWidth, Height);
        }
    }
}
