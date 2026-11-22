using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Lock ring, Ragnarok 3 style (ChatGPT, Tay chat 6afffe38, 2026-11-20): a smooth warm-gold ellipse under the FEET of the monster the local
    /// hero is locked on / walking to (online also the one it hit last), sized from the foot core (Monster.FootRadius), not the sprite height, so
    /// the Mire Hydra Queen's necks do not swell it. Line 2.5 px on screen at 1280x860 (feathered mesh, no jagged pixels), alpha pulse 75-100 %
    /// every 1.5 s, no spin. Only your own target is drawn. Wanted bosses get it too (brighter gold). Online it also draws a fixed red HP bar at
    /// the top centre of the screen, under the boss bar when that one shows.
    /// </summary>
    public class TargetMarker : MonoBehaviour
    {
        const float HoldSeconds = 4f;     // the marker stays this long after your last hit
        const float MaxDistance = 200f;   // world units from your hero: a monster you walked away from is no longer the target
        static readonly Color Gold = new Color(0xE7 / 255f, 0xC6 / 255f, 0x6B / 255f, 1f);       // #E7C66B normal monsters
        static readonly Color WantedGold = new Color(0xF3 / 255f, 0xD1 / 255f, 0x7A / 255f, 1f); // #F3D17A Wanted bosses
        const float PulsePeriod = 1.5f, PulseMin = 0.75f;

        TargetRingMesh ring;
        TargetArrowMesh arrow;
        GUIStyle text;
        Monster shownFor;
        int shownHp = -1;
        string label;

        /// <summary>Bottom edge of the fixed HP bar in canvas units below the safe area's top, 0 while it is not drawn: the toast area starts under it.</summary>
        public static float Bottom { get; private set; }

        void Start()
        {
            ring = TargetRingMesh.Create(transform);
            arrow = TargetArrowMesh.Create(transform);
        }

        /// <summary>The current target, or null: the monster a lock-target hero (Warrior) walks to or is locked on (online and offline), else online the monster you hit last.</summary>
        public static Monster Target()
        {
            HeroController hero = LocalPlayer.Hero;
            if (hero == null || hero.IsDead) return null;
            Monster m = AttackChase.Target;
            if (m == null) m = ClickMove.Target; // PC: the monster a ranged / non-lock hero was clicked onto
            if (m == null) m = hero.RecentLockTarget(HoldSeconds);
            if (m == null && !NetMode.IsOffline && Time.time - Monster.LastHitByMeAt <= HoldSeconds) m = Monster.LastHitByMe;
            if (m == null || m.IsDying || m.Hp <= 0f) return null;
            return (m.Feet - hero.Position).sqrMagnitude < MaxDistance * MaxDistance ? m : null;
        }

        /// <summary>The target the ring was last drawn for (this frame's LateUpdate, or the previous frame's): MonsterLabelPrefs shows its foot label
        /// without re-running the target pick in the middle of the frame.</summary>
        public static Monster Shown { get; private set; }

        void LateUpdate()
        {
            Monster m = Target();
            Shown = m;
            ring.Visible = m != null;
            arrow.Visible = m != null && m.IsWanted;
            if (m == null) return;
            float rx = RingRadius(m);
            Camera cam = Camera.main;
            float ortho = cam != null && cam.orthographic ? cam.orthographicSize : 90f;
            float px = 2f * ortho / ReferenceScreenHeight; // world units per screen pixel at the 1280x860 reference (scales with the window like the art)
            float pulse = PulseMin + (1f - PulseMin) * (0.5f + 0.5f * Mathf.Sin(Time.time * (2f * Mathf.PI / PulsePeriod)));
            Color c = m.IsWanted ? WantedGold : Gold;
            ring.Draw(new Vector3(m.Feet.x, m.Feet.y, -0.1f), rx, rx * 0.5f, (m.IsWanted ? WantedLineScreenPx : LineScreenPx) * px, px, new Color(c.r, c.g, c.b, pulse), GameConfig.OrderBase(m.Feet.y));
            if (!m.IsWanted) return;
            // Wanted boss: the ring is often buried under 40 heroes and skill VFX, so a small gold arrow marks it from above the head too.
            float bob = 2f * px * Mathf.Sin(Time.time * (2f * Mathf.PI / PulsePeriod));
            float tipY = m.Feet.y + m.DrawnHeight * ArrowHeadFraction + ArrowGapScreenPx * px;
            if (cam != null && cam.orthographic) tipY = Mathf.Min(tipY, cam.transform.position.y + ortho * (1f - 2f * ArrowTopReserve) - ArrowScreenPx * px); // under the boss bar: a tall boss's head is off screen / behind the HUD
            arrow.Draw(new Vector3(m.Feet.x, tipY + bob, -0.2f), ArrowScreenPx * px, new Color(c.r, c.g, c.b, pulse));
        }

        const float ArrowTopReserve = 0.1f; // top tenth of the screen = the boss bar
        const float ArrowScreenPx = 22f, ArrowGapScreenPx = 6f, ArrowHeadFraction = 0.9f; // arrow width, gap over the head, head height per drawn body height
        const float ReferenceScreenHeight = 860f, LineScreenPx = 2.5f, WantedLineScreenPx = 3f; // a Wanted boss's ring is long and sits under skill telegraphs: the top of the 2-3 px band

        /// <summary>Ring half width: just outside the foot core (feet + a margin), at least a small monster's footprint.</summary>
        public static float RingRadius(Monster m) { return Mathf.Max(8f, m.FootRadius * 1.15f + 2f); }

        void OnGUI()
        {
            if (Event.current.type != EventType.Repaint || TradeWindow.IsOpen || HeroChangeScreen.IsOpen || NetMode.IsOffline) return; // offline: the ring only (the IMGUI bar sits over the compact HUD's icon row)
            Bottom = 0f;
            Monster m = Target();
            if (m == null || (m == Hud.BarBoss())) return; // the boss already has its big bar
            if (text == null) text = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            float s = Screen.height / 640f;
            text.fontSize = Mathf.RoundToInt(12f * s);
            int hp = Mathf.CeilToInt(m.Hp);
            if (m != shownFor || hp != shownHp) { shownFor = m; shownHp = hp; label = m.DisplayName + "   " + hp + " / " + (int)m.MaxHp; }
            // Adventure Paper boss bar (BossBarView): it is taller than the IMGUI one and has rows of its own, so this bar goes under its real bottom edge.
            bool paperBar = UiScreens.UseNew(UiScreen.Boss) && BossBarView.Bottom > 0f;
            float top = paperBar ? BossBarView.Bottom + 6f : Hud.BossBarVisible() ? 48f : 14f;
            Bottom = top + 20f;
            float inset = paperBar ? Screen.height - Screen.safeArea.yMax : 0f; // BossBarView measures from the safe area's top
            float w = 240f * s, h = 18f * s, x = (Screen.width - w) * 0.5f, y = top * s + inset;
            Fill(new Rect(x - 2f, y - 2f, w + 4f, h + 4f), new Color(0.06f, 0.02f, 0.02f, 0.9f));
            Fill(new Rect(x, y, w * Mathf.Clamp01(m.Hp / m.MaxHp), h), new Color(0.85f, 0.16f, 0.14f));
            text.normal.textColor = Color.white;
            GUI.Label(new Rect(x, y, w, h), label, text);
        }

        static void Fill(Rect r, Color c)
        {
            GUI.color = c;
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

    }
}
