using System.Collections.Generic;
using UnityEngine;

/// <summary>Minimal prototype HUD: Wanted boss bar, control help and floating damage numbers (own hits large, crits gold, other players' dim).</summary>
public class Hud3D : MonoBehaviour
{
    struct PopNum { public Vector3 At; public int Value; public bool Crit, Dim; public float Born; }
    static readonly List<PopNum> pops = new List<PopNum>();
    static long dealt;
    const long BossHp = 49500000;
    GUIStyle num, label;

    public static void Pop(Vector3 at, int value, bool crit, bool dim = false)
    {
        if (pops.Count > 60) pops.RemoveAt(0);
        pops.Add(new PopNum { At = at, Value = crit ? value * 2 : value, Crit = crit, Dim = dim, Born = Time.time });
        dealt += crit ? value * 2 : value;
    }

    void OnGUI()
    {
        if (num == null)
        {
            num = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            label = new GUIStyle(GUI.skin.label) { fontSize = 16, fontStyle = FontStyle.Bold };
        }
        float k = Screen.height / 1080f;
        // boss bar
        float w = 760 * k, x = (Screen.width - w) / 2, y = 24 * k;
        GUI.color = new Color(0.12f, 0.16f, 0.22f, 0.9f); GUI.DrawTexture(new Rect(x - 6, y - 6, w + 12, 54 * k), Texture2D.whiteTexture);
        float frac = Mathf.Clamp01(1f - (float)dealt / BossHp);
        GUI.color = new Color(0.86f, 0.3f, 0.36f); GUI.DrawTexture(new Rect(x, y + 22 * k, w * frac, 18 * k), Texture2D.whiteTexture);
        GUI.color = Color.white;
        label.fontSize = Mathf.RoundToInt(18 * k);
        GUI.Label(new Rect(x, y - 2, w, 24 * k), "WANTED  Mire Hydra Queen  Lv.40      " + (BossHp - dealt).ToString("N0") + " / " + BossHp.ToString("N0"), label);
        label.fontSize = Mathf.RoundToInt(16 * k);
        GUI.color = new Color(0.1f, 0.13f, 0.18f, 0.75f); GUI.DrawTexture(new Rect(8 * k, Screen.height - 128 * k, 760 * k, 64 * k), Texture2D.whiteTexture); GUI.color = Color.white;
        GUI.Label(new Rect(16 * k, Screen.height - 120 * k, 900 * k, 110 * k), "WASD / mũi tên: đi    Shift: chạy    J / Space / chuột trái: chém\nQ / E: xoay camera    Cuộn chuột: zoom    H: bị đánh    K: gục / đứng dậy    Esc: thoát", label);
        if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape) Application.Quit();
        // damage numbers
        Camera cam = Camera.main;
        for (int i = pops.Count - 1; i >= 0; i--)
        {
            PopNum p = pops[i];
            float age = Time.time - p.Born;
            if (age > 0.9f) { pops.RemoveAt(i); continue; }
            Vector3 s = cam.WorldToScreenPoint(p.At + Vector3.up * age * 0.8f);
            if (s.z < 0) continue;
            int size = Mathf.RoundToInt((p.Dim ? 20 : p.Crit ? 44 : 32) * k * (age < 0.08f ? 1.3f : 1f));
            num.fontSize = size;
            float a = 1f - Mathf.Clamp01((age - 0.6f) / 0.3f);
            Color c = p.Dim ? new Color(1f, 1f, 1f, 0.55f * a) : p.Crit ? new Color(1f, 0.78f, 0.25f, a) : new Color(1f, 1f, 1f, a);
            Rect r = new Rect(s.x - 150, Screen.height - s.y - 30, 300, 60);
            num.normal.textColor = new Color(0.1f, 0.12f, 0.2f, c.a); GUI.Label(new Rect(r.x + 2, r.y + 2, r.width, r.height), (p.Crit ? "CRIT " : "") + p.Value.ToString("N0"), num);
            num.normal.textColor = c; GUI.Label(r, (p.Crit ? "CRIT " : "") + p.Value.ToString("N0"), num);
        }
    }
}
