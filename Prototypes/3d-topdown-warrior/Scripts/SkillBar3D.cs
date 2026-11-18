using UnityEngine;

/// <summary>
/// HUD V2 (ChatGPT, Ragnarok 3 / Midgard Senki silver style: docs/reference/3d-warrior-prototype/hud-3d-chatgpt/): portrait, level, HP and MP
/// bars at the top left; the 4 skill slots at the bottom centre with key tags, MP cost, cooldown sweep + seconds, "not enough MP" look,
/// a press flash (0.12 s, x1.05) and a cyan "ready" flash (0.18 s). Positions are ChatGPT's pixel table for 1920x1080, scaled by screen height;
/// the skill bar stays centred on wider screens. Pieces are drawn with OnGUI (the prototype has no uGUI package); stretchable pieces are
/// 3-sliced so their rounded / bevelled ends keep their shape. Strings are rebuilt only when their number changes.
/// </summary>
public class SkillBar3D : MonoBehaviour
{
    // ChatGPT V2 colours
    static readonly Color TextDark = Hex(0x2E3652), Outline = Hex(0x344052), NoMp = Hex(0xFF6473), NoMpVeil = new Color(0x49 / 255f, 0x3F / 255f, 0x60 / 255f, 0.35f);
    static readonly Color CdVeil = new Color(1f, 1f, 1f, 0.68f);   // CooldownSweep paints #08121C; GUI alpha gives ChatGPT's 68 %

    Texture2D ring, face, crest, lvTag, panel, groove, hpFill, mpFill, heart, drop, barPanel, slot, keyTag, pill, pressed, ready;
    GUIStyle text;
    readonly CooldownSweep[] sweeps = new CooldownSweep[4];
    readonly string[] cdText = new string[4], mpText = new string[4];
    readonly int[] cdTenths = { -1, -1, -1, -1 };
    readonly float[] readyFlashAt = { -9f, -9f, -9f, -9f };
    readonly bool[] wasCooling = new bool[4];
    string hpText, mpBarText, lvText; int hpShown = -1, mpShown = -1;

    static Texture2D Load(string n) { return Resources.Load<Texture2D>("ProtoHud/" + n); }

    void Start()
    {
        ring = Load("portrait-ring"); face = Load("portrait-warrior"); crest = Load("class-crest-warrior"); lvTag = Load("lv-tag");
        panel = Load("status-panel"); groove = Load("bar-groove"); hpFill = Load("bar-fill-hp"); mpFill = Load("bar-fill-mp");
        heart = Load("icon-heart"); drop = Load("icon-mana-drop"); barPanel = Load("skillbar-panel"); slot = Load("slot-frame");
        keyTag = Load("key-tag"); pill = Load("mp-cost-pill"); pressed = Load("slot-pressed"); ready = Load("slot-ready-flash");
        WarriorSkillDefs3D.LoadIcons();
        for (int i = 0; i < 4; i++) { sweeps[i] = new CooldownSweep(); mpText[i] = WarriorSkillDefs3D.All[i].Mp.ToString(); }
    }

    void OnGUI()
    {
        if (Event.current.type != EventType.Repaint) return;
        if (text == null) text = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, clipping = TextClipping.Overflow };
        float k = Screen.height / 1080f;
        DrawStatus(k);
        if (WarriorSkills3D.I != null) DrawSkills(k);
        GUI.color = Color.white;
    }

    void DrawStatus(float k)
    {
        var st = HeroStats3D.I; if (st == null) return;
        int hp = Mathf.CeilToInt(st.Hp), mp = Mathf.FloorToInt(st.Mp);
        if (hp != hpShown) { hpShown = hp; hpText = hp.ToString("N0") + " / " + st.MaxHp.ToString("N0"); }
        if (mp != mpShown) { mpShown = mp; mpBarText = mp.ToString("N0") + " / " + st.MaxMp.ToString("N0"); }
        if (lvText == null) lvText = "Lv. " + st.Level;

        Slice3(R(k, 194, 75, 495, 115), panel, 40, 120, Color.white);
        Slice3(R(k, 185, 29, 195, 52), lvTag, 40, 110, Color.white);
        Label(R(k, 222, 29, 120, 52), lvText, 30 * k, TextDark, false, k);
        Bar(k, 90, groove, hpFill, st.Hp / st.MaxHp, hpText);
        Bar(k, 139, groove, mpFill, st.Mp / st.MaxMp, mpBarText);
        Draw(R(k, 226, 88, 45, 45), heart, Color.white);
        Draw(R(k, 226, 135, 45, 48), drop, Color.white);
        Draw(R(k, 37, 28, 164, 164), face, Color.white);      // the painted face already sits on its own pearl disc
        Draw(R(k, 24, 15, 190, 190), ring, Color.white);
        Draw(R(k, 148, 155, 70, 75), crest, Color.white);
    }

    /// <summary>Groove 365x38 at x 272, fill 353x28 inset 6/5 px (ChatGPT table), value centred on the bar.</summary>
    void Bar(float k, float y, Texture2D grooveTex, Texture2D fillTex, float frac, string value)
    {
        Slice3(R(k, 272, y, 365, 38), grooveTex, 38, 38, Color.white);
        frac = Mathf.Clamp01(frac);
        if (frac > 0.005f) Slice3(R(k, 278, y + 5, 353 * frac, 28), fillTex, 30, 30, Color.white);
        Label(R(k, 272, y - 1, 365, 38), value, 25 * k, Color.white, true, k);
    }

    void DrawSkills(float k)
    {
        var ws = WarriorSkills3D.I;
        float x0 = Screen.width / 2f - 960f * k;   // the table is for a 1920-wide screen: keep the bar centred
        Slice3(new Rect(x0 + 499 * k, 872 * k, 925 * k, 197 * k), barPanel, 130, 130, Color.white);
        for (int i = 0; i < 4; i++)
        {
            var d = WarriorSkillDefs3D.All[i];
            float sx = x0 + (581 + 199 * i) * k, now = Time.time;
            float left = ws.CooldownLeft(i);
            bool cooling = left > 0f, lowMp = !cooling && !HeroStats3D.HasMp(d.Mp);
            if (wasCooling[i] && !cooling) readyFlashAt[i] = now;
            wasCooling[i] = cooling;
            float press = now - ws.PressedAt[i], grow = press < 0.12f ? 1.05f : 1f;
            Rect slotR = Grow(new Rect(sx, 897 * k, 166 * k, 166 * k), grow);
            Rect iconR = Grow(new Rect(sx + 6 * k, 903 * k, 154 * k, 154 * k), grow);

            if (d.Icon != null) Draw(iconR, d.Icon, lowMp ? new Color(0.5f, 0.5f, 0.55f) : Color.white);
            if (lowMp) Draw(iconR, Texture2D.whiteTexture, NoMpVeil);
            if (cooling)
            {
                sweeps[i].Set(left / d.Cooldown);
                Draw(iconR, sweeps[i].Tex, CdVeil);
                int tenths = Mathf.CeilToInt(left * 10f);
                if (tenths != cdTenths[i]) { cdTenths[i] = tenths; cdText[i] = tenths >= 100 ? Mathf.CeilToInt(left).ToString() : (tenths / 10f).ToString("0.0"); }
                Label(new Rect(iconR.x, iconR.y - 8 * k, iconR.width, iconR.height), cdText[i], 40 * k, Color.white, true, k);
            }
            Draw(slotR, slot, Color.white);
            if (press < 0.12f) Draw(slotR, pressed, new Color(1f, 1f, 1f, 1f - press / 0.12f));
            float rf = now - readyFlashAt[i];
            if (rf < 0.18f) Draw(slotR, ready, new Color(1f, 1f, 1f, 1f - rf / 0.18f));
            if (now - ws.DeniedAt[i] < 0.25f) Draw(slotR, ready, new Color(1f, 0.4f, 0.45f, 0.8f));   // key pressed but cooling / no MP

            Slice3(new Rect(sx + 55 * k, 858 * k, 56 * k, 43 * k), keyTag, 40, 40, Color.white);
            Label(new Rect(sx + 55 * k, 858 * k, 56 * k, 43 * k), KeyNames[i], 28 * k, TextDark, false, k);
            Slice3(new Rect(sx + 30 * k, 1030 * k, 106 * k, 38 * k), pill, 80, 45, Color.white);
            Label(new Rect(sx + 66 * k, 1030 * k, 62 * k, 38 * k), mpText[i], 25 * k, lowMp ? NoMp : Color.white, true, k);
        }
    }

    static readonly string[] KeyNames = { "1", "2", "3", "4" };

    static Rect R(float k, float x, float y, float w, float h) { return new Rect(x * k, y * k, w * k, h * k); }
    static Rect Grow(Rect r, float s) { return s == 1f ? r : new Rect(r.center.x - r.width * s / 2f, r.center.y - r.height * s / 2f, r.width * s, r.height * s); }
    static Color Hex(int rgb) { return new Color(((rgb >> 16) & 255) / 255f, ((rgb >> 8) & 255) / 255f, (rgb & 255) / 255f, 1f); }

    static void Draw(Rect r, Texture2D t, Color c) { if (t == null) return; GUI.color = c; GUI.DrawTexture(r, t, ScaleMode.StretchToFill, true); }

    /// <summary>Horizontal 3-slice: the end caps (`capL` / `capR` texture pixels) keep their aspect at the drawn height, the middle stretches.</summary>
    static void Slice3(Rect r, Texture2D t, float capL, float capR, Color c)
    {
        if (t == null || r.width <= 0f) return;
        GUI.color = c;
        float s = r.height / t.height, l = Mathf.Min(capL * s, r.width / 2f), rr = Mathf.Min(capR * s, r.width / 2f);
        float ul = capL / t.width, ur = capR / t.width;
        GUI.DrawTextureWithTexCoords(new Rect(r.x, r.y, l, r.height), t, new Rect(0f, 0f, ul * (l / (capL * s)), 1f));
        GUI.DrawTextureWithTexCoords(new Rect(r.x + l, r.y, r.width - l - rr, r.height), t, new Rect(ul, 0f, 1f - ul - ur, 1f));
        float cut = 1f - rr / (capR * s);
        GUI.DrawTextureWithTexCoords(new Rect(r.xMax - rr, r.y, rr, r.height), t, new Rect(1f - ur + ur * cut, 0f, ur * (1f - cut), 1f));
    }

    /// <summary>Bold label; `outlined` adds the dark #344052 rim ChatGPT asked for on light-on-colour numbers.</summary>
    void Label(Rect r, string s, float size, Color fill, bool outlined, float k)
    {
        text.fontSize = Mathf.Max(8, Mathf.RoundToInt(size));
        GUI.color = Color.white;
        if (outlined)
        {
            float px = Mathf.Max(1f, 2f * k);
            text.normal.textColor = Outline;
            for (int dx = -1; dx <= 1; dx++) for (int dy = -1; dy <= 1; dy++)
                if (dx != 0 || dy != 0) GUI.Label(new Rect(r.x + dx * px, r.y + dy * px, r.width, r.height), s, text);
        }
        text.normal.textColor = fill;
        GUI.Label(r, s, text);
    }
}
