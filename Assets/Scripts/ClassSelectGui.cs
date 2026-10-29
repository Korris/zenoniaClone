using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>IMGUI class select screen: five cards (Warrior / Wizard / Mechanic / Assassin / Cleric), click or press 1-5.</summary>
    public static class ClassSelectGui
    {
        static GUIStyle title, name, body, key;

        /// <summary>Draws the screen and returns the clicked class index (0..4) or -1.</summary>
        public static int Draw()
        {
            float s = Screen.height / 640f;
            if (title == null)
            {
                title = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
                name = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
                body = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.UpperCenter, wordWrap = true };
                key = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            }
            title.fontSize = Mathf.RoundToInt(26f * s);
            name.fontSize = Mathf.RoundToInt(20f * s);
            body.fontSize = Mathf.RoundToInt(13f * s);
            key.fontSize = Mathf.RoundToInt(15f * s);

            Fill(new Rect(0f, 0f, Screen.width, Screen.height), new Color(0.02f, 0.05f, 0.1f, 0.78f));
            title.normal.textColor = Color.white;
            GUI.Label(new Rect(0f, 40f * s, Screen.width, 40f * s), "Choose your hero  -  Frost Hollow", title);

            int n = HeroClassDef.All.Length;
            float cw = 190f * s, ch = 330f * s, gap = 14f * s;
            float x0 = (Screen.width - (n * cw + (n - 1) * gap)) * 0.5f, y0 = 110f * s;
            int clicked = -1;
            for (int i = 0; i < HeroClassDef.All.Length; i++)
            {
                HeroClassDef c = HeroClassDef.All[i];
                var r = new Rect(x0 + i * (cw + gap), y0, cw, ch);
                bool hover = r.Contains(Event.current.mousePosition);
                Fill(r, hover ? new Color(0.22f, 0.36f, 0.55f, 0.95f) : new Color(0.1f, 0.18f, 0.3f, 0.92f));
                name.normal.textColor = new Color(1f, 0.9f, 0.55f);
                GUI.Label(new Rect(r.x, r.y + 14f * s, r.width, 32f * s), c.Name, name);
                body.normal.textColor = new Color(0.9f, 0.95f, 1f);
                var inner = new Rect(r.x + 14f * s, r.y + 60f * s, r.width - 28f * s, r.height - 120f * s);
                GUI.Label(inner, c.Tagline + "\n\nHP " + (int)c.MaxHp + "   ATK " + (int)c.Atk + "   DEF " + (int)c.Def + "   SPD " + (int)c.Speed + "\n\nAttack: " + c.ComboLabel + (c.SkillName != null ? "\n[L] " + c.SkillName : ""), body);
                key.normal.textColor = new Color(0.55f, 1f, 0.7f);
                GUI.Label(new Rect(r.x, r.yMax - 46f * s, r.width, 30f * s), "[" + (i + 1) + "]  Play", key);
                if (GUI.Button(r, GUIContent.none, GUIStyle.none)) clicked = i;
            }
            body.normal.textColor = new Color(1f, 1f, 1f, 0.8f);
            GUI.Label(new Rect(0f, y0 + ch + 20f * s, Screen.width, 24f * s), "Keys 1-5 or click.  In game: F1-F5 respawn as Warrior / Wizard / Mechanic / Assassin / Cleric.", body);
            return clicked;
        }

        static void Fill(Rect r, Color c)
        {
            GUI.color = c;
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = Color.white;
        }
    }
}
