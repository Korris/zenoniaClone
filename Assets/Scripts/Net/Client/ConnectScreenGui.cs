using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// IMGUI connect screen of the class-select flow: player name, server address, hero class, then [Join] a server, [Host (dev)] (this process runs the
    /// zone and plays in it) or [Offline] (back to the single-player class select). Dev login: name + class only, no passwords yet.
    /// </summary>
    public static class ConnectScreenGui
    {
        const string PrefName = "zenonia.name", PrefAddress = "zenonia.address", PrefClass = "zenonia.class";
        public static bool Visible { get; private set; }

        static string playerName, address;
        static int heroClass = -1;
        static GUIStyle title, label, field, button, status;

        public static void Open()
        {
            Visible = true;
            if (heroClass < 0)
            {
                heroClass = Mathf.Clamp(PlayerPrefs.GetInt(PrefClass, GameSession.ClassIndex(NetLaunchArgs.ClassName)), 0, HeroClassDef.All.Length - 1);
                playerName = NetLaunchArgs.Name ?? PlayerPrefs.GetString(PrefName, "Hero" + Random.Range(100, 999));
                address = NetLaunchArgs.Host != null ? NetLaunchArgs.Host + ":" + NetLaunchArgs.Port : PlayerPrefs.GetString(PrefAddress, "127.0.0.1:" + NetLaunchArgs.Port);
            }
        }

        /// <summary>Small button under the class select cards that opens this screen.</summary>
        public static void DrawOpenButton()
        {
            float s = Screen.height / 640f;
            EnsureStyles(s);
            if (GUI.Button(new Rect((Screen.width - 260f * s) * 0.5f, Screen.height - 54f * s, 260f * s, 34f * s), "Play online / host (dev)", button)) Open();
        }

        static void EnsureStyles(float s)
        {
            if (title == null)
            {
                title = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
                label = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleRight };
                field = new GUIStyle(GUI.skin.textField) { alignment = TextAnchor.MiddleLeft };
                button = new GUIStyle(GUI.skin.button) { fontStyle = FontStyle.Bold };
                status = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, wordWrap = true };
            }
            title.fontSize = Mathf.RoundToInt(24f * s);
            label.fontSize = Mathf.RoundToInt(14f * s);
            field.fontSize = Mathf.RoundToInt(14f * s);
            button.fontSize = Mathf.RoundToInt(15f * s);
            status.fontSize = Mathf.RoundToInt(13f * s);
        }

        public static void Draw(GameSession session)
        {
            float s = Screen.height / 640f;
            EnsureStyles(s);
            GUI.color = new Color(0.02f, 0.05f, 0.1f, 0.88f);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;
            title.normal.textColor = Color.white;
            GUI.Label(new Rect(0f, 36f * s, Screen.width, 40f * s), "Play online", title);

            NetClient client = NetClient.Active;
            bool busy = client != null && client.State == ClientState.Connecting;
            float w = 420f * s, x = (Screen.width - w) * 0.5f, y = 100f * s, rowH = 34f * s, lw = 110f * s;
            label.normal.textColor = new Color(0.85f, 0.92f, 1f);
            GUI.enabled = !busy;
            GUI.Label(new Rect(x, y, lw, rowH), "Name", label);
            playerName = GUI.TextField(new Rect(x + lw + 8f * s, y, w - lw - 8f * s, rowH), playerName ?? "", 16, field);
            y += rowH + 10f * s;
            GUI.Label(new Rect(x, y, lw, rowH), "Server", label);
            address = GUI.TextField(new Rect(x + lw + 8f * s, y, w - lw - 8f * s, rowH), address ?? "", 64, field);
            y += rowH + 14f * s;

            GUI.Label(new Rect(x, y, lw, rowH), "Hero", label);
            float cw = (w - lw - 8f * s) / HeroClassDef.All.Length;
            int bigFont = button.fontSize;
            button.fontSize = Mathf.RoundToInt(10f * s); // five class names share one row
            for (int i = 0; i < HeroClassDef.All.Length; i++)
            {
                bool on = heroClass == i;
                GUI.color = on ? new Color(1f, 0.9f, 0.5f) : Color.white;
                if (GUI.Button(new Rect(x + lw + 8f * s + i * cw, y, cw - 4f * s, rowH), HeroClassDef.All[i].Name, button)) heroClass = i;
            }
            GUI.color = Color.white;
            button.fontSize = bigFont;
            y += rowH + 22f * s;

            float bw = (w - 16f * s) / 3f;
            if (GUI.Button(new Rect(x, y, bw, rowH * 1.3f), "Join", button)) TryJoin(session);
            if (GUI.Button(new Rect(x + bw + 8f * s, y, bw, rowH * 1.3f), "Host (dev)", button)) { Save(); Visible = false; session.HostServer(playerName); }
            GUI.enabled = true;
            if (GUI.Button(new Rect(x + 2f * (bw + 8f * s), y, bw, rowH * 1.3f), busy ? "Cancel" : "Offline", button))
            {
                Visible = false;
                session.ExitOnline();
            }
            y += rowH * 1.3f + 18f * s;

            string text = client != null ? client.Status : "Join a server by address (host:port), host one from this window for friends on the LAN, or play offline.";
            bool failed = client != null && client.State == ClientState.Failed;
            status.normal.textColor = failed ? new Color(1f, 0.6f, 0.5f) : new Color(0.8f, 0.9f, 1f, 0.9f);
            GUI.Label(new Rect(x, y, w, 60f * s), text, status);

            if (client != null && client.State == ClientState.Joined) Visible = false;
        }

        static void TryJoin(GameSession session)
        {
            string host = (address ?? "").Trim();
            int port = NetLaunchArgs.Port;
            int c = host.LastIndexOf(':');
            if (c > 0 && int.TryParse(host.Substring(c + 1), out port)) host = host.Substring(0, c);
            else if (c > 0) return;
            if (host.Length == 0) host = "127.0.0.1";
            Save();
            session.JoinServer(host, port, playerName, heroClass, NetLaunchArgs.DevKey);
        }

        static void Save()
        {
            PlayerPrefs.SetString(PrefName, playerName ?? "");
            PlayerPrefs.SetString(PrefAddress, address ?? "");
            PlayerPrefs.SetInt(PrefClass, heroClass);
        }
    }
}
