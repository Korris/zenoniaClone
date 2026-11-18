using System.Collections.Generic;
using UnityEngine;

/// <summary>Minimal prototype HUD: map name, FPS, Wanted boss bar, control help and floating damage numbers (ChatGPT v6 style: rounded bold,
/// dark-brown 2 px outline, normal #FFF4DC, crit #FFD35A x0.8 with a small CRIT tag above, hero hurt #FF7770 floating low, others dim).</summary>
public class Hud3D : MonoBehaviour
{
    struct PopNum { public Vector3 At; public int Value; public bool Crit, Dim, Hurt; public float Born, Side; }
    public string MapName = "Frost Hollow";
    public static bool ShowBoss;
    public static int Kills;
    static readonly List<PopNum> pops = new List<PopNum>();
    static long dealt;
    const long BossHp = 49500000;
    GUIStyle num, label;
    HeroController3D hero;
    Slime3D framed; float shownHp, switchedAt = -1f; Texture2D targetIcon;   // v19 target frame
    float fps, fpsTime; int fpsFrames;   // frame rate averaged over 0.5 s windows (unscaled, so hit stop does not skew it)

    void Update()
    {
        fpsFrames++; fpsTime += Time.unscaledDeltaTime;
        if (fpsTime >= 0.5f) { fps = fpsFrames / fpsTime; fpsFrames = 0; fpsTime = 0f; }
    }

    public static void Pop(Vector3 at, int value, bool crit, bool dim = false, bool hurt = false)
    {
        if (pops.Count > 60) pops.RemoveAt(0);
        float born = Time.time;   // v17: numbers on the same spot appear 0.12 s apart instead of in one clump
        foreach (var q in pops) if ((q.At - at).sqrMagnitude < 0.5f && q.Born + 0.12f > born) born = q.Born + 0.12f;
        pops.Add(new PopNum { At = at, Value = value, Crit = crit, Dim = dim, Hurt = hurt, Born = born, Side = Random.Range(-0.35f, 0.35f)   /* v16: wider spread so hits do not stack */ });
        if (hurt) HeroStats3D.Hurt(value); else dealt += value;   // slime hits drain the hero's HP bar (SkillBar3D draws it)
    }

    void OnGUI()
    {
        if (num == null)
        {
            num = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            label = new GUIStyle(GUI.skin.label) { fontSize = 16, fontStyle = FontStyle.Bold };
        }
        float k = Screen.height / 1080f;
        // v18 target frame (ChatGPT HUD feedback): selected slime's name + HP bar under the map title
        if (hero == null) hero = FindFirstObjectByType<HeroController3D>();
        if (hero != null && hero.Target != null && hero.Target.Alive)
        {
            if (hero.Target != framed) { framed = hero.Target; shownHp = framed.Hp01; switchedAt = Time.time; }   // v19: switch flash 0.08 s
            shownHp = Mathf.MoveTowards(shownHp, framed.Hp01, Time.unscaledDeltaTime / 0.15f);   // HP fill eases over ~0.15 s
            if (targetIcon == null) targetIcon = Resources.Load<Texture2D>("ProtoHud/target-leafdrop");
            float tw = 300 * k, tx = (Screen.width - tw) / 2f, ty = 60 * k;
            GUI.color = new Color(0.1f, 0.13f, 0.18f, 0.8f); GUI.DrawTexture(new Rect(tx - 6 * k, ty, tw + 12 * k, 38 * k), Texture2D.whiteTexture);
            GUI.color = new Color(0.25f, 0.1f, 0.1f, 1f); GUI.DrawTexture(new Rect(tx, ty + 22 * k, tw, 10 * k), Texture2D.whiteTexture);
            GUI.color = new Color(0.86f, 0.3f, 0.32f, 1f); GUI.DrawTexture(new Rect(tx, ty + 22 * k, tw * shownHp, 10 * k), Texture2D.whiteTexture);
            if (targetIcon != null) { GUI.color = Color.white; GUI.DrawTexture(new Rect(tx - 50 * k, ty - 4 * k, 46 * k, 46 * k), targetIcon); }
            if (Time.time - switchedAt < 0.08f) { GUI.color = new Color(1f, 1f, 1f, 0.45f); GUI.DrawTexture(new Rect(tx - 6 * k, ty, tw + 12 * k, 38 * k), Texture2D.whiteTexture); }
            GUI.color = Color.white; label.fontSize = Mathf.RoundToInt(15 * k);
            GUI.Label(new Rect(tx, ty + 1 * k, tw, 22 * k), "Leafdrop Slime  Lv. 12", label);
        }
        if (!ShowBoss) { GUI.color = new Color(0.1f, 0.13f, 0.18f, 0.75f); GUI.DrawTexture(new Rect(Screen.width / 2f - 260 * k, 10 * k, 520 * k, 44 * k), Texture2D.whiteTexture); label.fontSize = Mathf.RoundToInt(22 * k); GUI.color = Color.white; GUI.Label(new Rect(Screen.width / 2f - 250 * k, 16 * k, 600 * k, 40 * k), MapName + "   -   Slime đã hạ: " + Kills, label); }
        else {
        // boss bar
        float w = 760 * k, x = (Screen.width - w) / 2, y = 24 * k;
        GUI.color = new Color(0.12f, 0.16f, 0.22f, 0.9f); GUI.DrawTexture(new Rect(x - 6, y - 6, w + 12, 54 * k), Texture2D.whiteTexture);
        float frac = Mathf.Clamp01(1f - (float)dealt / BossHp);
        GUI.color = new Color(0.86f, 0.3f, 0.36f); GUI.DrawTexture(new Rect(x, y + 22 * k, w * frac, 18 * k), Texture2D.whiteTexture);
        GUI.color = Color.white;
        label.fontSize = Mathf.RoundToInt(18 * k);
        GUI.Label(new Rect(x, y - 2, w, 24 * k), "WANTED  Mire Hydra Queen  Lv.40      " + (BossHp - dealt).ToString("N0") + " / " + BossHp.ToString("N0"), label);
        }
        label.fontSize = Mathf.RoundToInt(20 * k);
        GUI.color = new Color(0.1f, 0.13f, 0.18f, 0.75f); GUI.DrawTexture(new Rect(Screen.width - 150 * k, 10 * k, 140 * k, 40 * k), Texture2D.whiteTexture);
        GUI.color = fps >= 55f ? new Color(0.6f, 1f, 0.6f) : fps >= 30f ? new Color(1f, 0.9f, 0.4f) : new Color(1f, 0.45f, 0.4f);
        GUI.Label(new Rect(Screen.width - 140 * k, 15 * k, 130 * k, 34 * k), "FPS " + Mathf.RoundToInt(fps), label);
        label.fontSize = Mathf.RoundToInt(15 * k);
        GUI.color = new Color(0.1f, 0.13f, 0.18f, 0.75f); GUI.DrawTexture(new Rect(8 * k, Screen.height - 160 * k, 480 * k, 152 * k), Texture2D.whiteTexture); GUI.color = Color.white;   // narrow box, left of the skill bar (SkillBar3D starts at x 499)
        if (hero == null) hero = FindFirstObjectByType<HeroController3D>();
        string auto = hero != null && hero.Auto ? "BẬT" : "tắt";
        GUI.Label(new Rect(16 * k, Screen.height - 156 * k, 470 * k, 150 * k), "Chuột trái: chọn quái và tự đánh\nJ / Space: đánh quái gần nhất    Tab: Auto " + auto + "\n1 2 3 4: skill    WASD: đi (huỷ mục tiêu)    Shift: chạy\nCuộn: zoom 6 mức    V: xem mặt\nGiữ chuột phải kéo / Q E: xoay camera\nHome: camera mặc định    H: bị đánh    K: gục    Esc: thoát", label);
        if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape) Application.Quit();
        // damage numbers: pop up 0.1 m in 0.08 s, then drift to ~0.55 m by 0.65 s with a little sideways offset, fading over the last 0.18 s
        Camera cam = Camera.main;
        for (int i = pops.Count - 1; i >= 0; i--)
        {
            PopNum p = pops[i];
            float age = Time.time - p.Born;
            if (age < 0f) continue;   // staggered, not shown yet
            if (age > 0.65f) { pops.RemoveAt(i); continue; }
            float rise = 0.4f + (age < 0.08f ? 0.1f * age / 0.08f : 0.1f + 0.45f * (age - 0.08f) / 0.57f);   // v16: start 0.4 m higher, off the head
            if (p.Hurt) rise *= 0.5f;
            Vector3 s = cam.WorldToScreenPoint(p.At + Vector3.up * rise + cam.transform.right * p.Side);
            if (s.z < 0) continue;
            float a = 1f - Mathf.Clamp01((age - 0.47f) / 0.18f), pop = age < 0.08f ? (p.Crit ? 1.38f : 1.2f) : age < 0.16f && p.Crit ? 1.15f : 1f;   // v18: crit bounces +15-20 %
            int size = Mathf.RoundToInt((p.Dim ? 20 : p.Hurt ? 24 : p.Crit ? 36 : 30) * k * pop);
            Color fill = p.Hurt ? Hex(0xFF7770, a) : p.Dim ? new Color(1f, 1f, 1f, 0.55f * a) : p.Crit ? Hex(0xFFD35A, a) : Hex(0xFFF4DC, a);
            Color edge = p.Crit ? Hex(0x533A29, a) : Hex(0x59402D, a);
            Rect r = new Rect(s.x - 150, Screen.height - s.y - 30, 300, 60);
            Outlined(r, p.Value.ToString("N0"), size, fill, edge, 2f * k * 1.5f);
            if (p.Crit) Outlined(new Rect(r.x, r.y - size * 0.8f, r.width, r.height), "CRIT", Mathf.RoundToInt(size * 0.75f), fill, edge, 2f * k * 1.5f);
        }
    }

    static Color Hex(int rgb, float a) => new Color(((rgb >> 16) & 255) / 255f, ((rgb >> 8) & 255) / 255f, (rgb & 255) / 255f, a);

    /// <summary>Label with an 8-direction outline `px` pixels wide (2 px at 720p ~ 3 px at 1080p).</summary>
    void Outlined(Rect r, string text, int size, Color fill, Color edge, float px)
    {
        num.fontSize = size;
        num.normal.textColor = edge;
        for (int dx = -1; dx <= 1; dx++) for (int dy = -1; dy <= 1; dy++)
            if (dx != 0 || dy != 0) GUI.Label(new Rect(r.x + dx * px, r.y + dy * px, r.width, r.height), text, num);
        num.normal.textColor = fill;
        GUI.Label(r, text, num);
    }
}
