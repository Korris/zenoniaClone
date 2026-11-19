using UnityEngine;
using UnityEngine.UI;

namespace ZenoniaProto
{
    /// <summary>
    /// Boss HP bar of the RO Soft HUD: top centre under the menu row (the mockup has no boss in view; this keeps the classic bar's job,
    /// Hud.BarBoss: the Wanted Boss or the map boss near the hero). The navy name pill over the ChatGPT bar track with the red fill
    /// and "hp / max" on it. Hud.DrawBossBar (IMGUI) stays quiet while RoHud.Active.
    /// </summary>
    public sealed class RoHudBossBar
    {
        const float W = 300f, BarH = 18f, Top = 90f, PillH = 22f;
        readonly RectTransform root;
        readonly RoHud.Bar bar;
        readonly Text name, value;
        int shownHp = -1;
        Monster shownBoss;

        public RoHudBossBar(RectTransform safe)
        {
            root = RoHud.Box(safe, "RoHudBossBar", new Vector2(0.5f, 1f), 0f, -(Top + (PillH + BarH) * 0.5f), W, PillH + BarH + 2f);
            RectTransform pill;
            name = RoHud.Pill(root, "Name", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), 0f, 0f, PillH, "hud-lv-badge", "hud-icon-skull", 15f, "", 12, Color.white, out pill);
            bar = new RoHud.Bar(root, "Hp", 0f, PillH + 2f, W, BarH, "hud-bar-track", "hud-bar-red");
            value = RoHud.Label(UiKit.Fill(bar.Root, "Value"), "", 12, Color.white, TextAnchor.MiddleCenter, true, true);
            root.gameObject.SetActive(false);
        }

        public void Tick(bool visible)
        {
            Monster boss = visible ? Hud.BarBoss() : null;
            bool on = boss != null;
            if (root.gameObject.activeSelf != on) { root.gameObject.SetActive(on); if (on) bar.Relayout(); }
            if (!on) { shownBoss = null; return; }
            if (boss != shownBoss) { shownBoss = boss; name.text = boss.DisplayName; shownHp = -1; }
            int hp = Mathf.CeilToInt(boss.Hp);
            if (hp == shownHp) return;
            shownHp = hp;
            value.text = hp + " / " + (int)boss.MaxHp;
            bar.Set(boss.MaxHp > 0f ? boss.Hp / boss.MaxHp : 0f);
        }
    }
}
