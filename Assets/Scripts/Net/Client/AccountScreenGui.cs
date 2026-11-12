using System.Collections.Generic;
using UnityEngine;
using ZenoniaProto.Api;

namespace ZenoniaProto
{
    /// <summary>
    /// Log in, register, forgotten password, deleting a hero, and the small account menu in the world (the character list and Customize and Name are uGUI: CharacterListScreen, CharacterCustomizeScreen). Plain IMGUI over the UI kit's window pieces until the screen gets its own art; IMGUI text fields also bring up the phone keyboard by themselves.
    /// Everything it does is a call on AccountFlow. Typed passwords are dropped as soon as they have been sent or the page changes.
    /// </summary>
    public static class AccountScreenGui
    {
        const float RefW = 480f;
        static readonly Color Gold = new Color(1f, 0.87f, 0.45f), Soft = new Color(0.85f, 0.92f, 1f), Bad = new Color(1f, 0.6f, 0.5f);

        static GUIStyle title, label, field, button, primary, smallButton, smallPrimary, status, row, link;
        static string login = "", email = "", password = "", passwordAgain = "", deleteName = "";
        static AccountPage lastPage;
        static Texture2D panelTex;

        static void EnsureStyles(float s)
        {
            if (title == null)
            {
                Font font = UiKit.Font;
                title = new GUIStyle(GUI.skin.label) { font = font, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
                label = new GUIStyle(GUI.skin.label) { font = font, alignment = TextAnchor.MiddleRight };
                field = new GUIStyle(GUI.skin.textField) { font = font, alignment = TextAnchor.MiddleLeft, padding = new RectOffset(10, 10, 4, 4) };
                button = KitButton(font, "btn-navy", Color.white);
                primary = KitButton(font, "btn-gold", new Color(0.25f, 0.12f, 0.02f));
                smallButton = new GUIStyle(button) { border = new RectOffset(18, 18, 0, 0) }; // the wide caps of the full buttons overlap on a narrow one
                smallPrimary = new GUIStyle(primary) { border = new RectOffset(18, 18, 0, 0) };
                status = new GUIStyle(GUI.skin.label) { font = font, alignment = TextAnchor.MiddleCenter, wordWrap = true };
                row = new GUIStyle(GUI.skin.label) { font = font, alignment = TextAnchor.MiddleLeft, wordWrap = true };
                link = new GUIStyle(GUI.skin.label) { font = font, alignment = TextAnchor.MiddleRight };
                link.normal.textColor = link.hover.textColor = link.active.textColor = Gold;
                panelTex = UiArt.Win("panel").texture;
            }
            title.fontSize = Mathf.RoundToInt(24f * s);
            label.fontSize = field.fontSize = Mathf.RoundToInt(14f * s);
            button.fontSize = primary.fontSize = Mathf.RoundToInt(15f * s);
            smallButton.fontSize = smallPrimary.fontSize = Mathf.RoundToInt(13f * s);
            status.fontSize = Mathf.RoundToInt(13f * s);
            row.fontSize = Mathf.RoundToInt(14f * s);
            link.fontSize = Mathf.RoundToInt(13f * s);
        }

        static GUIStyle KitButton(Font font, string piece, Color text)
        {
            var style = new GUIStyle(GUI.skin.button) { font = font, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, border = new RectOffset(50, 50, 0, 0) };
            Texture2D tex = UiArt.Win(piece).texture;
            style.normal.background = style.hover.background = style.active.background = style.focused.background = tex;
            style.normal.textColor = style.hover.textColor = style.active.textColor = style.focused.textColor = text;
            return style;
        }

        public static void Draw(GameSession session)
        {
            float s = Screen.height / 640f;
            EnsureStyles(s);
            GUI.color = new Color(0.02f, 0.05f, 0.1f, 0.92f);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;

            float w = Mathf.Min(RefW * s, Screen.width - 16f * s), h = 560f * s, x = (Screen.width - w) * 0.5f, y = (Screen.height - h) * 0.5f;
            float slice = 48f;
            GUI.DrawTexture(new Rect(x, y, w, h), panelTex, ScaleMode.StretchToFill, true, 0f, Color.white, new Vector4(slice, slice, slice, slice), Vector4.zero);

            AccountPage page = AccountFlow.Page;
            if (page != lastPage) { password = passwordAgain = deleteName = ""; lastPage = page; }
            NetClient client = NetClient.Active;
            bool connecting = client != null && client.State == ClientState.Connecting;
            bool busy = AccountFlow.Busy || connecting;
            float px = x + 34f * s, pw = w - 68f * s, py = y + 30f * s;
            GUI.enabled = !busy;
            switch (page)
            {
                case AccountPage.Login: DrawLogin(session, px, pw, py, s); break;
                case AccountPage.Register: DrawRegister(px, pw, py, s); break;
                case AccountPage.Forgot: DrawForgot(px, pw, py, s); break;
                case AccountPage.ConfirmDelete: DrawConfirmDelete(px, pw, py, s); break;
                default: break; // Characters and Customize are uGUI screens (CharacterListScreen, CharacterCustomizeScreen)
            }
            GUI.enabled = true;

            string text = connecting ? client.Status : AccountFlow.Status;
            status.normal.textColor = AccountFlow.StatusBad && !connecting ? Bad : new Color(0.8f, 0.9f, 1f, 0.9f);
            GUI.Label(new Rect(px, y + h - 68f * s, pw, 44f * s), text, status);
        }

        static void Title(string text, float x, float w, ref float y, float s)
        {
            title.normal.textColor = Gold;
            GUI.Label(new Rect(x, y, w, 40f * s), text, title);
            y += 52f * s;
        }

        static string Field(string name, string value, int max, float x, float w, ref float y, float s, bool secret = false)
        {
            float rowH = 36f * s, lw = 104f * s;
            label.normal.textColor = Soft;
            GUI.Label(new Rect(x, y, lw, rowH), name, label);
            var r = new Rect(x + lw + 8f * s, y, w - lw - 8f * s, rowH);
            value = secret ? GUI.PasswordField(r, value ?? "", '*', max, field) : GUI.TextField(r, value ?? "", max, field);
            y += rowH + 10f * s;
            return value;
        }

        static bool Enter()
        {
            Event e = Event.current;
            return GUI.enabled && e.type == EventType.KeyDown && (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter);
        }

        static void DrawLogin(GameSession session, float x, float w, float y, float s)
        {
            Title(ReleaseBuild.LoginTitle, x, w, ref y, s);
            if (AccountFlow.ServerEditable)
            {
                string apiUrl = Field("Server", AccountFlow.ApiUrl, 120, x, w, ref y, s);
                if (apiUrl != AccountFlow.ApiUrl) AccountFlow.UseServer(apiUrl);
            }
            login = Field("Username", login, 254, x, w, ref y, s);
            password = Field("Password", password, 200, x, w, ref y, s, true);
            if (GUI.Button(new Rect(x + w - 170f * s, y - 8f * s, 170f * s, 26f * s), "Forgot password?", link)) AccountFlow.ShowForgot();
            y += 22f * s;
            bool submit = Enter();
            if (GUI.Button(new Rect(x, y, w, 46f * s), "Log in", primary) || submit)
            {
                AccountFlow.Login(login, password);
                password = "";
            }
            y += 54f * s;
            float half = (w - 8f * s) * 0.5f;
            if (GUI.Button(new Rect(x, y, half, 42f * s), "New account", button)) AccountFlow.ShowRegister();
            if (GUI.Button(new Rect(x + half + 8f * s, y, half, 42f * s), "Play offline", button)) ConnectScreenGui.LeaveToOffline(session);
            y += 50f * s;
            if (ConnectScreenGui.DevPageAvailable && GUI.Button(new Rect(x, y, w, 34f * s), "Direct connect (dev)", button)) ConnectScreenGui.ShowDevPage();
        }

        static void DrawRegister(float x, float w, float y, float s)
        {
            Title("New account", x, w, ref y, s);
            login = Field("Username", login, 20, x, w, ref y, s);
            email = Field("Email", email, 254, x, w, ref y, s);
            password = Field("Password", password, 72, x, w, ref y, s, true);
            passwordAgain = Field("Again", passwordAgain, 72, x, w, ref y, s, true);
            y += 6f * s;
            bool submit = Enter();
            if (GUI.Button(new Rect(x, y, w, 46f * s), "Create account", primary) || submit)
            {
                AccountFlow.Register(login, email, password, passwordAgain);
                if (string.IsNullOrEmpty(AccountFlow.RegisterProblem(login, email, password, passwordAgain))) password = passwordAgain = "";
            }
            y += 54f * s;
            if (GUI.Button(new Rect(x, y, w, 42f * s), "Back to log in", button)) AccountFlow.ShowLogin();
        }

        static void DrawForgot(float x, float w, float y, float s)
        {
            Title("Forgot password", x, w, ref y, s);
            status.normal.textColor = Soft;
            GUI.Label(new Rect(x, y, w, 44f * s), "Enter the email of your account. We will send a link to a page where you can set a new password.", status);
            y += 54f * s;
            email = Field("Email", email, 254, x, w, ref y, s);
            y += 6f * s;
            bool submit = Enter();
            if (GUI.Button(new Rect(x, y, w, 46f * s), "Send reset link", primary) || submit) AccountFlow.ForgotPassword(email);
            y += 54f * s;
            if (GUI.Button(new Rect(x, y, w, 42f * s), "Back to log in", button)) AccountFlow.ShowLogin();
        }

        static void DrawConfirmDelete(float x, float w, float y, float s)
        {
            CharacterSummary c = AccountFlow.Deleting;
            if (c == null) { AccountFlow.ShowCharacters(); return; }
            Title("Delete " + c.name + "?", x, w, ref y, s);
            status.normal.textColor = Soft;
            GUI.Label(new Rect(x, y, w, 64f * s), HeroClassName(c.classId) + ", level " + c.level + ". This cannot be undone: the hero, its items and its gold are gone for good.\nType the name " + c.name + " to confirm.", status);
            y += 74f * s;
            deleteName = Field("Name", deleteName, 16, x, w, ref y, s);
            y += 6f * s;
            bool enabled = GUI.enabled;
            GUI.enabled = enabled && AccountFlow.DeleteProblem(deleteName) == null;
            bool submit = Enter();
            if (GUI.Button(new Rect(x, y, w, 46f * s), "Delete this hero", primary) || submit) AccountFlow.Delete(deleteName);
            GUI.enabled = enabled;
            y += 54f * s;
            if (GUI.Button(new Rect(x, y, w, 42f * s), "Keep this hero", button)) AccountFlow.ShowCharacters();
        }

        // ---- the account menu in the world, behind the Settings button ----

        public static bool MenuOpen { get; private set; }
        public static void OpenMenu() { MenuOpen = true; }
        public static void CloseMenu() { MenuOpen = false; }

        public static void DrawMenu()
        {
            GUI.depth = -100; // over the HUD's own IMGUI
            float s = Screen.height / 640f;
            EnsureStyles(s);
            Event e = Event.current;
            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape) { CloseMenu(); e.Use(); return; }
            GUI.color = new Color(0.02f, 0.05f, 0.1f, 0.6f);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;

            float w = Mathf.Min(360f * s, Screen.width - 16f * s), h = 300f * s, x = (Screen.width - w) * 0.5f, y = (Screen.height - h) * 0.5f;
            GUI.color = new Color(0.03f, 0.07f, 0.14f, 0.96f); // the panel art is open in the middle
            GUI.DrawTexture(new Rect(x + 14f * s, y + 14f * s, w - 28f * s, h - 28f * s), Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.DrawTexture(new Rect(x, y, w, h), panelTex, ScaleMode.StretchToFill, true, 0f, Color.white, new Vector4(48f, 48f, 48f, 48f), Vector4.zero);
            float px = x + 34f * s, pw = w - 68f * s, py = y + 26f * s;
            Title("Menu", px, pw, ref py, s);
            GUI.enabled = !AccountFlow.Busy;
            if (GUI.Button(new Rect(px, py, pw, 46f * s), "Change character", primary)) AccountFlow.LeaveWorld(false);
            py += 54f * s;
            if (GUI.Button(new Rect(px, py, pw, 46f * s), "Log out", button)) AccountFlow.LeaveWorld(true);
            py += 54f * s;
            if (GUI.Button(new Rect(px, py, pw, 42f * s), "Back to the game", button)) CloseMenu();
            GUI.enabled = true;
        }

        static string HeroClassName(string classId)
        {
            foreach (HeroClassDef d in HeroClassDef.All) if (RpgSession.ClassId(d.Id) == classId) return d.Name;
            return classId;
        }
    }
}
