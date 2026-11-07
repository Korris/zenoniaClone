using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Online only: the monster you are fighting = the one your own hero hit last (Monster.LastHitByMe), for HoldSeconds after the hit.
    /// A thin red ring of four arcs under its feet (y-sorted like its shadow; red and broken so it never reads as the gold ring of your hero)
    /// and a fixed red HP bar at the top centre of the screen, under the boss bar when that one shows. Nothing when you hit nothing.
    /// </summary>
    public class TargetMarker : MonoBehaviour
    {
        const float HoldSeconds = 4f;     // the marker stays this long after your last hit
        const float MaxDistance = 200f;   // world units from your hero: a monster you walked away from is no longer the target
        static readonly Color Red = new Color(1f, 0.22f, 0.18f, 1f);

        SpriteRenderer ring;
        GUIStyle text;
        Monster shownFor;
        int shownHp = -1;
        string label;

        void Start()
        {
            ring = ProceduralSprites.Child(transform, "TargetRing", RingSprite());
        }

        /// <summary>The current target, or null.</summary>
        static Monster Target()
        {
            if (NetMode.IsOffline) return null;
            Monster m = Monster.LastHitByMe;
            HeroController hero = LocalPlayer.Hero;
            if (m == null || m.IsDying || m.Hp <= 0f || hero == null || hero.IsDead || Time.time - Monster.LastHitByMeAt > HoldSeconds) return null;
            return (m.Feet - hero.Position).sqrMagnitude < MaxDistance * MaxDistance ? m : null;
        }

        void LateUpdate()
        {
            Monster m = Target();
            ring.enabled = m != null;
            if (m == null) return;
            float w = Mathf.Max(28f, m.Box.width * 2.4f);   // the hit box is narrower than the sprite: the ring must reach past the body to be seen
            ring.transform.localScale = new Vector3(w, w, 1f);
            ring.transform.position = new Vector3(m.Feet.x, m.Feet.y, -0.1f); // ties with the monster's shadow order: the nearer z wins
            ring.sortingOrder = GameConfig.OrderBase(m.Feet.y);
            float spin = Time.time * 40f;                      // slow turn: the arcs read as a lock-on, not as decoration
            ring.color = new Color(Red.r, Red.g, Red.b, 0.85f + 0.15f * Mathf.Sin(Time.time * 6f));
            ring.sprite = RingSprite(spin);
        }

        void OnGUI()
        {
            if (Event.current.type != EventType.Repaint || TradeWindow.IsOpen || HeroChangeScreen.IsOpen) return;
            Monster m = Target();
            if (m == null || (m == Monster.Boss && Hud.BossBarVisible())) return; // the boss already has its big bar
            if (text == null) text = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            float s = Screen.height / 640f;
            text.fontSize = Mathf.RoundToInt(12f * s);
            int hp = Mathf.CeilToInt(m.Hp);
            if (m != shownFor || hp != shownHp) { shownFor = m; shownHp = hp; label = m.DisplayName + "   " + hp + " / " + (int)m.MaxHp; }
            float w = 240f * s, h = 18f * s, x = (Screen.width - w) * 0.5f, y = (Hud.BossBarVisible() ? 48f : 14f) * s;
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

        static Sprite[] frames;

        /// <summary>Four thin arcs on an ellipse (1 x 0.5 units at scale 1), white inside a dark edge, rotated by `spinDeg` (pre-baked every 15 degrees).</summary>
        static Sprite RingSprite(float spinDeg = 0f)
        {
            const int steps = 6; // 4 arcs repeat every 90 degrees: 6 frames of 15 degrees
            if (frames == null)
            {
                frames = new Sprite[steps];
                for (int i = 0; i < steps; i++) frames[i] = BakeRing(i * 15f);
            }
            return frames[Mathf.FloorToInt(Mathf.Repeat(spinDeg, 90f) / 15f) % steps];
        }

        static Sprite BakeRing(float offsetDeg)
        {
            const int w = 128, h = 64;
            var px = new Color32[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float dx = (x + 0.5f - w * 0.5f) / (w * 0.5f), dy = (y + 0.5f - h * 0.5f) / (h * 0.5f);
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    if (d < 0.74f || d > 0.98f) continue;
                    float ang = Mathf.Repeat(Mathf.Atan2(dy, dx) * Mathf.Rad2Deg - offsetDeg, 90f);
                    if (ang > 62f) continue;                                                  // the gap between two arcs
                    bool edge = d < 0.79f || d > 0.92f || ang < 4f || ang > 58f;
                    px[y * w + x] = edge ? new Color32(60, 0, 0, 220) : new Color32(255, 255, 255, 255);
                }
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.SetPixels32(px);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), w, 0, SpriteMeshType.FullRect);
        }
    }
}
