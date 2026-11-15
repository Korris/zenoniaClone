using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Anti-overlap layout of the floating numbers (ChatGPT V3 "Comic Impact"): screen-space boxes instead of fixed lanes.
    /// A new number tries, in order, top-centre, top-left, top-right, then the same three one tier higher (8 px gap) and takes the
    /// first spot whose box does not hit a live number within 120 px; a crit (box includes burst + banner) only dodges other crits
    /// and pushes the numbers already in its box aside. EXP waits 150 ms, sits beside the hero and merges kills that land within 250 ms.
    /// When no spot is free, another player's number is dropped (lowest priority).
    /// </summary>
    public partial class DamageNumber
    {
        const float Gap = 8f, NearPx = 120f, ExpMergeMs = 250f, ExpDelayMs = 150f;
        static readonly Vector2[] Spots = { new Vector2(0f, 0f), new Vector2(-1f, 0f), new Vector2(1f, 0f), new Vector2(0f, 1f), new Vector2(-1f, 1f), new Vector2(1f, 1f) };

        /// <summary>Sets the layout offset (and delay) of a new entry; false = no room and low priority: do not show it.</summary>
        static bool Place(ref Entry e)
        {
            if (e.Kind == DamageNumberKind.Exp) { e.AgeMs = -ExpDelayMs; e.OffX = 30f; e.OffY = -6f; }
            else if (e.Kind == DamageNumberKind.Heal || e.Kind == DamageNumberKind.PotionHp || e.Kind == DamageNumberKind.PotionMp) e.OffY = 12f;
            Camera c = instance != null ? instance.cam : null;
            if (c == null) c = Camera.main;
            if (c == null) return true;
            float px = Screen.height / 720f;
            Vector2 anchor = GuiPoint(c, e.X, e.Y);
            Vector2 size = BoxSize(ref e) * px;
            float baseX = e.OffX, baseY = e.OffY;
            bool crit = e.Kind == DamageNumberKind.Crit; // a crit only dodges other crits, then pushes the rest aside
            for (int s = 0; s < Spots.Length; s++)
            {
                e.OffX = baseX + Spots[s].x * (size.x / px + Gap);
                e.OffY = baseY + Spots[s].y * (size.y / px + Gap);
                Vector2 at = anchor + new Vector2(e.OffX, -e.OffY) * px;
                if (Hits(c, at, size, px, crit)) continue;
                if (crit) ClearForCrit(c, at, size, px);
                return true;
            }
            if (crit) { ClearForCrit(c, anchor + new Vector2(e.OffX, -e.OffY) * px, size, px); return true; }
            if (e.Kind == DamageNumberKind.Other || e.Kind == DamageNumberKind.OtherHurt) return false;
            return true; // keep the last (highest) spot for the player's own numbers: every hit must be felt
        }

        /// <summary>A crit keeps the centre: live numbers already inside its box (number + burst + banner) step out sideways.</summary>
        static void ClearForCrit(Camera c, Vector2 p, Vector2 size, float px)
        {
            for (int i = 0; i < Capacity; i++)
            {
                if (!Pool[i].Active || Pool[i].Kind == DamageNumberKind.Crit) continue;
                DamageNumberMotion m = DamageNumberMotion.Of(Pool[i].Kind);
                Vector2 q = GuiPoint(c, Pool[i].X, Pool[i].Y) + new Vector2(Pool[i].OffX, -(Pool[i].OffY + m.Rise(Mathf.Max(0f, Pool[i].AgeMs)))) * px;
                Vector2 o = BoxSize(ref Pool[i]) * px;
                if (Mathf.Abs(q.x - p.x) * 2f >= o.x + size.x || Mathf.Abs(q.y - p.y) * 2f >= o.y + size.y) continue;
                float side = q.x < p.x ? -1f : 1f, need = (o.x + size.x) * 0.5f + Gap * px - Mathf.Abs(q.x - p.x);
                Pool[i].OffX += side * need / px;
            }
        }

        static Vector2 GuiPoint(Camera c, float x, float y)
        {
            Vector3 sp = c.WorldToScreenPoint(new Vector3(x, y, 0f));
            return new Vector2(sp.x, Screen.height - sp.y);
        }

        /// <summary>Box of a number in 720p px: glyph run width + height; a crit also covers its burst and CRITICAL banner.</summary>
        static Vector2 BoxSize(ref Entry e)
        {
            float h = DamageNumberMotion.Of(e.Kind).HeightPx;
            float w = h * 0.62f * Mathf.Max(1, e.Text.Length) + 4f;
            if (e.Kind == DamageNumberKind.Crit) return new Vector2(Mathf.Max(w, 110f), h * 2.1f);
            return new Vector2(w, h * 1.1f);
        }

        /// <summary>True when the box at centre p overlaps a live number nearby (its current position, rise included).</summary>
        static bool Hits(Camera c, Vector2 p, Vector2 size, float px, bool critsOnly)
        {
            for (int i = 0; i < Capacity; i++)
            {
                if (!Pool[i].Active || (critsOnly && Pool[i].Kind != DamageNumberKind.Crit)) continue;
                DamageNumberMotion m = DamageNumberMotion.Of(Pool[i].Kind);
                Vector2 q = GuiPoint(c, Pool[i].X, Pool[i].Y) + new Vector2(Pool[i].OffX, -(Pool[i].OffY + m.Rise(Mathf.Max(0f, Pool[i].AgeMs)))) * px;
                if ((q - p).sqrMagnitude > (NearPx + size.x) * (NearPx + size.x) * px * px) continue;
                Vector2 o = BoxSize(ref Pool[i]) * px;
                if (Mathf.Abs(q.x - p.x) * 2f < o.x + size.x && Mathf.Abs(q.y - p.y) * 2f < o.y + size.y) return true;
            }
            return false;
        }

        /// <summary>True when another crit started within the window (only the first crit in 300 ms gets the big burst).</summary>
        static bool RecentCrit(float windowMs)
        {
            for (int i = 0; i < Capacity; i++)
                if (Pool[i].Active && Pool[i].Kind == DamageNumberKind.Crit && Pool[i].AgeMs < windowMs) return true;
            return false;
        }

        static int CountActive(DamageNumberKind a, DamageNumberKind b)
        {
            int n = 0;
            for (int i = 0; i < Capacity; i++) if (Pool[i].Active && (Pool[i].Kind == a || Pool[i].Kind == b)) n++;
            return n;
        }

        /// <summary>"+N EXP" within 250 ms of another EXP number: add into that one (one number for a multi-kill).</summary>
        static bool MergeExp(string text)
        {
            int add;
            if (!TryExp(text, out add)) return false;
            for (int i = 0; i < Capacity; i++)
            {
                if (!Pool[i].Active || Pool[i].Kind != DamageNumberKind.Exp || Pool[i].AgeMs > ExpMergeMs) continue;
                int have;
                if (!TryExp(Pool[i].Text, out have)) continue;
                Pool[i].Text = "+" + (have + add) + " EXP";
                return true;
            }
            return false;
        }

        static bool TryExp(string text, out int value)
        {
            value = 0;
            return text.StartsWith("+") && text.EndsWith(" EXP") && int.TryParse(text.Substring(1, text.Length - 5), out value);
        }
    }
}
