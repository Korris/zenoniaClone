using UnityEngine;
#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

namespace ZenoniaProto
{
    /// <summary>
    /// WebGL only: the chat is typed into an HTML input laid over the Unity field (Assets/Plugins/WebGL/ChatInput.jslib), so Vietnamese
    /// Telex / VNI and IMEs work in the browser. The jslib keeps every key typed into it away from Unity (the game never sees them).
    /// Answers arrive by SendMessage on the chat object (OnWebChatSubmit with URI-encoded text / OnWebChatCancel / OnWebChatTab).
    /// Elsewhere Enabled is false and the Unity InputField is used.
    /// </summary>
    public static class WebChatInput
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] static extern void ZenChatOpen(string objectName, string placeholder, int x, int y, int w, int h, int fontPx);
        [DllImport("__Internal")] static extern void ZenChatClose();
        public static bool Enabled { get { return true; } }
#else
        static void ZenChatOpen(string objectName, string placeholder, int x, int y, int w, int h, int fontPx) { }
        static void ZenChatClose() { }
        public static bool Enabled { get { return false; } }
#endif

        /// <summary>Shows the HTML input over `field` (screen-space overlay canvas) and focuses it.</summary>
        public static void Open(string objectName, RectTransform field, string placeholder, int fontUnits)
        {
            var c = new Vector3[4];
            field.GetWorldCorners(c); // overlay canvas: world = screen pixels, origin bottom-left
            float scale = Screen.height / UiKit.RefHeight;
            ZenChatOpen(objectName, placeholder, Mathf.RoundToInt(c[0].x), Mathf.RoundToInt(Screen.height - c[1].y),
                Mathf.RoundToInt(c[2].x - c[0].x), Mathf.RoundToInt(c[1].y - c[0].y), Mathf.RoundToInt(fontUnits * scale));
        }

        /// <summary>Hides the HTML input (chat collapsed / hidden by a window) and gives the keyboard back to the game canvas.</summary>
        public static void Close() { ZenChatClose(); }

        /// <summary>The submitted text as typed (the jslib URI-encodes it: SendMessage cut multi-byte UTF-8 strings short).</summary>
        public static string Decode(string sent)
        {
            try { return System.Uri.UnescapeDataString(sent ?? ""); }
            catch (System.Exception) { return sent ?? ""; }
        }
    }
}
