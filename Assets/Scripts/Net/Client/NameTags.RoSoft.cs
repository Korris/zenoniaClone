using UnityEngine;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// NameTags: the RO Soft HUD's world labels (02-hud.png). Villagers get an ivory name pill with an icon (hammer = the gear smiths,
    /// the service face = everyone else) above their heads; your own green name gets the gold emblem on its left and a small HP bar
    /// under it. IMGUI like the other tags; pills are drawn in three parts (caps keep their round shape, the middle stretches), so
    /// the ChatGPT pill art never squashes.
    /// </summary>
    public partial class NameTags
    {
        const float TopBand = 96f, VillagerHeadY = 30f, PillPadX = 12f, PillGap = 4f; // world units above the feet; pill box model in canvas units
        GUIStyle pillText;

        static Texture2D Tex(string piece)
        {
            Sprite s = RoSoftSprites.Named(piece);
            return s != null ? s.texture : null;
        }

        /// <summary>A horizontal pill: left cap, stretched middle, right cap (cap = half the texture height, in texture pixels).</summary>
        static void Pill(Rect r, Texture2D tex)
        {
            if (tex == null) return;
            float capU = tex.height * 0.5f / tex.width, capW = r.height * 0.5f;
            GUI.DrawTextureWithTexCoords(new Rect(r.x, r.y, capW, r.height), tex, new Rect(0f, 0f, capU, 1f));
            GUI.DrawTextureWithTexCoords(new Rect(r.x + capW, r.y, r.width - capW * 2f, r.height), tex, new Rect(capU, 0f, 1f - capU * 2f, 1f));
            GUI.DrawTextureWithTexCoords(new Rect(r.xMax - capW, r.y, capW, r.height), tex, new Rect(1f - capU, 0f, capU, 1f));
        }

        void DrawVillagerPills(Camera cam)
        {
            Texture2D pill = Tex("hud-name-pill");
            if (pill == null) return;
            float s = Screen.height / 640f;
            if (pillText == null) pillText = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleLeft, fontStyle = FontStyle.Bold, font = RoSoftTheme.Bold };
            pillText.fontSize = Mathf.Max(10, Mathf.RoundToInt(13f * s));
            pillText.normal.textColor = RoSoftTheme.Ink;
            float h = 22f * s, icon = 14f * s, pad = PillPadX * s, gap = PillGap * s;
            foreach (TownNpc n in TownNpc.Villagers)
            {
                if (n == null || n.Def == null) continue;
                Vector3 sp = cam.WorldToScreenPoint(new Vector3(n.Feet.x, n.Feet.y + VillagerHeadY, 0f));
                if (sp.z < 0f || sp.x < -100f || sp.x > Screen.width + 100f || sp.y < -40f || sp.y > Screen.height + 40f) continue;
                var label = new GUIContent(n.Def.Name);
                float tw = pillText.CalcSize(label).x, w = pad + icon + gap + tw + pad;
                var r = new Rect(sp.x - w * 0.5f, Screen.height - sp.y - h, w, h);
                if (r.y < TopBand * s) continue; // IMGUI draws over the HUD: no pill under the status / menu / minimap row
                Pill(r, pill);
                Texture2D ic = Tex(GearShops.IsSeller(n.Def.Id) ? "hud-icon-hammer" : "hud-icon-service");
                if (ic != null) GUI.DrawTexture(new Rect(r.x + pad, r.y + (h - icon) * 0.5f, icon, icon), ic, ScaleMode.ScaleToFit);
                GUI.Label(new Rect(r.x + pad + icon + gap, r.y, tw + 2f, h), label, pillText);
            }
        }

        /// <summary>Own plate extras (RO HUD): emblem left of the name rect `name`, HP bar under it.</summary>
        static void DrawOwnPlate(Rect name, HeroController me)
        {
            float s = Screen.height / 640f, e = 23f * s;
            Texture2D emblem = Tex("hud-emblem");
            if (emblem != null) GUI.DrawTexture(new Rect(name.x - e - 2f * s, name.center.y - e * 0.5f, e, e), emblem, ScaleMode.ScaleToFit);
            Texture2D track = Tex("hud-name-hp-track"), fill = Tex("hud-name-hp-fill");
            if (track == null || fill == null || me.MaxHp <= 0f) return;
            float w = 88f * s, h = 8f * s;
            var r = new Rect(name.center.x - w * 0.5f, name.yMax + 6f * s, w, h);
            Pill(r, track);
            float v = Mathf.Clamp01(me.Hp / me.MaxHp);
            if (v > 0f) Pill(new Rect(r.x + 1f, r.y + 1f, Mathf.Max(h, (w - 2f) * v), h - 2f), fill);
        }
    }
}
