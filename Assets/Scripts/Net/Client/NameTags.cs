using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>Player names above every named hero (online client and host), IMGUI over the world: the local hero in gold, the others in white.</summary>
    public class NameTags : MonoBehaviour
    {
        GUIStyle style;

        void OnGUI()
        {
            if (Event.current.type != EventType.Repaint || NetMode.IsOffline) return;
            Camera cam = Camera.main;
            Zone zone = Zone.Main;
            if (cam == null || zone == null) return;
            if (style == null) style = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            style.fontSize = Mathf.Max(10, Mathf.RoundToInt(Screen.height / 640f * 13f));
            var rect = new Rect(0f, 0f, 160f, 20f);
            for (int i = 0; i < zone.Heroes.All.Count; i++)
            {
                HeroController h = zone.Heroes.All[i];
                if (h == null || h.IsDead || string.IsNullOrEmpty(h.PlayerName)) continue;
                Vector3 sp = cam.WorldToScreenPoint(new Vector3(h.Position.x, h.Position.y + 30f, 0f));
                if (sp.z < 0f || sp.x < -80f || sp.x > Screen.width + 80f) continue;
                rect.center = new Vector2(sp.x, Screen.height - sp.y);
                bool me = h == LocalPlayer.Hero;
                style.normal.textColor = Color.black;
                var shadow = rect; shadow.x += 1f; shadow.y += 1f;
                GUI.Label(shadow, h.PlayerName, style);
                style.normal.textColor = me ? new Color(1f, 0.9f, 0.5f) : Color.white;
                GUI.Label(rect, h.PlayerName, style);
            }
        }
    }
}
