using UnityEngine;
using UnityEngine.UI;

namespace ZenoniaProto
{
    /// <summary>
    /// NotificationToast in the RO Soft HUD (RoHud.Active; user 19/11: toasts ChatGPT-painted like 02-hud.png, sheet E ro-hud-e-chat-toast):
    ///   one line (ShowLine)  the ivory pill with a star on each end (hud-toast-line), or the soft coral pill (hud-toast-warn) for a red
    ///                        warning colour; ink text centred, padded clear of the stars, the pill as wide as its text
    ///   full (Show)          the ivory card with the yellow title band (hud-toast-full): title in the band, sub line under it, the item
    ///                        icon in the gold ring (hud-toast-icon) on the left
    /// Mobile: bottom centre above the chat bar (as the compact pill). PC: top centre under the menu row / boss bar.
    /// </summary>
    public sealed partial class NotificationToast
    {
        const float RoLineH = 34f, RoFullH = 72f, RoMinW = 160f, RoMaxW = 460f, RoStarPad = 40f, RoPadX = 14f, RoRing = 52f, RoGap = 10f;
        const float RoBand = 26f; // the yellow band of hud-toast-full at RoFullH (40 of 100 art px, its rim included)
        const float RoPcTop = 100f; // PC: under the menu row (MenuTop + plate + caption) so the toast never covers it

        bool roShown;
        RectTransform roRoot;
        Image roPlate, roRing, roIcon;

        void ShowRoParts(bool on)
        {
            if (!on) { if (roRoot != null) roRoot.gameObject.SetActive(false); return; }
            if (roRoot == null)
            {
                roRoot = UiKit.Fill(root, "Ro");
                NameTags.Occlude(root);
                roRoot.SetAsFirstSibling(); // under the title / sub texts
                roPlate = RoHud.Art(UiKit.Fill(roRoot, "Plate"), "hud-toast-line");
                RectTransform ring = UiKit.Rect(roRoot, "Ring", 0f, 0f, RoRing, RoRing);
                roRing = RoHud.Art(ring, "hud-toast-icon");
                roIcon = UiKit.Fill(ring, "Icon", RoRing * 0.18f).gameObject.AddComponent<Image>();
                roIcon.preserveAspect = true; roIcon.raycastTarget = false;
            }
            roRoot.gameObject.SetActive(true);
            frame.enabled = false;
            if (pill != null) pill.gameObject.SetActive(false);
            iconSlot.gameObject.SetActive(false);
            icon.enabled = false;
        }

        /// <summary>
        /// RO HUD: a one-line toast on its own pill art with an icon piece, white outlined text (the Wanted "X has fallen!" on the navy pill
        /// with the crossed swords). Other HUD looks: the plain coloured line.
        /// </summary>
        public static void ShowStyled(string text, string plate, string iconPiece, Color fallback)
        {
            if (!RoHud.Active) { ShowLine(text, fallback); return; }
            Enqueue(new Entry { Title = text, Plate = plate, Icon = RoHud.Piece(iconPiece), TitleColor = Color.white });
        }

        static bool IsWarning(Color c) { return c.r > 0.75f && c.g < 0.55f && c.b < 0.55f; }

        void BeginRo(Entry e)
        {
            bool full = !string.IsNullOrEmpty(e.Sub), hasIcon = e.Icon != null, warn = !full && IsWarning(e.TitleColor); // no sub line: the one-line pill (with its icon)
            title.font = sub.font = RoSoftTheme.Bold;
            sub.font = RoSoftTheme.Body;
            title.fontStyle = sub.fontStyle = FontStyle.Normal;
            foreach (Text t in new[] { title, sub }) { Shadow sh = t.GetComponent<Shadow>(); if (sh != null) sh.enabled = false; }
            title.text = e.Title;
            sub.text = full ? e.Sub : "";
            title.color = warn ? RoSoftTheme.Hex("#8E2F36") : full ? RoSoftTheme.Hex("#8A5A12") : RoHud.Ink; // brown on the yellow band
            sub.color = RoHud.Ink;
            title.fontSize = full ? 15 : 14;
            sub.fontSize = 13;

            bool styled = !full && e.Plate != null;
            roPlate.sprite = RoHud.Piece(styled ? e.Plate : full ? "hud-toast-full" : warn ? "hud-toast-warn" : "hud-toast-line");
            roRing.enabled = !styled; // a styled pill shows its icon bare (no gold ring)
            Outline ol = title.GetComponent<Outline>();
            if (ol == null) ol = title.gameObject.AddComponent<Outline>();
            if (styled) title.color = Color.white;
            ol.enabled = styled; ol.effectColor = new Color(0.1f, 0.15f, 0.3f, 0.8f); ol.effectDistance = new Vector2(1f, -1f);
            roPlate.type = Image.Type.Sliced;
            float h = full ? RoFullH : styled ? RoLineH + 6f : RoLineH;
            roPlate.pixelsPerUnitMultiplier = full ? 1.4f : 56f / h; // pill: the art's height = the pill's, round ends; card: corners ~20 units
            roRing.gameObject.SetActive(hasIcon);
            roIcon.sprite = e.Icon;

            float textX, textW, w;
            if (full)
            {
                textX = hasIcon ? RoPadX + RoRing + RoGap : RoPadX + 4f;
                textW = Mathf.Max(title.preferredWidth, sub.preferredWidth) + 2f;
                w = Mathf.Clamp(textX + textW + RoPadX + 4f, RoMinW + 60f, RoMaxW);
                textW = w - textX - RoPadX - 4f;
                ((RectTransform)roRing.transform).sizeDelta = new Vector2(RoRing, RoRing);
                ((RectTransform)roRing.transform).anchoredPosition = new Vector2(RoPadX, -(RoBand + (h - RoBand - RoRing) * 0.5f) + 4f); // in the body, its top over the band
                Place(title, textX, 1f, textW, RoBand - 2f, TextAnchor.MiddleLeft);
                Place(sub, textX, RoBand + 2f, textW, h - RoBand - 10f, TextAnchor.MiddleLeft);
            }
            else
            {
                textW = title.preferredWidth + 2f;
                float pad = warn || styled ? RoPadX + 6f : RoStarPad, ring = h - 4f, lead = hasIcon ? ring + 4f : 0f; // the star pill keeps its stars clear of the text
                w = Mathf.Clamp(textW + lead + pad * 2f, RoMinW, RoMaxW);
                RectTransform rr = (RectTransform)roRing.transform;
                rr.sizeDelta = new Vector2(ring, ring);
                rr.anchoredPosition = new Vector2(pad - 6f, -2f);
                Place(title, pad + lead, 0f, w - pad * 2f - lead, h, hasIcon ? TextAnchor.MiddleLeft : TextAnchor.MiddleCenter);
                Place(sub, 0f, 0f, 0f, 0f, TextAnchor.MiddleCenter);
            }
            title.horizontalOverflow = sub.horizontalOverflow = HorizontalWrapMode.Wrap;
            title.resizeTextForBestFit = sub.resizeTextForBestFit = true; // a very long line shrinks inside the max width instead of spilling
            title.resizeTextMinSize = sub.resizeTextMinSize = 10;
            title.resizeTextMaxSize = title.fontSize; sub.resizeTextMaxSize = sub.fontSize;
            float ri = ((RectTransform)roRing.transform).sizeDelta.x * 0.18f; // the icon inside the ring's gold rim
            roIcon.rectTransform.offsetMin = new Vector2(ri, ri); roIcon.rectTransform.offsetMax = new Vector2(-ri, -ri);
            root.sizeDelta = new Vector2(w, h);
            Vector2 anchor = new Vector2(0.5f, RoHud.Pc ? 1f : 0f);
            root.anchorMin = anchor; root.anchorMax = anchor; root.pivot = anchor;
            PlaceRo();
        }

        static void Place(Text t, float x, float y, float w, float h, TextAnchor a)
        {
            RectTransform rt = t.rectTransform;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(x, -y);
            rt.sizeDelta = new Vector2(w, h);
            t.alignment = a;
        }

        /// <summary>Mobile: above the chat bar. PC: top centre, under the menu row and any boss / target bar.</summary>
        void PlaceRo()
        {
            float y = RoHud.Pc ? -Mathf.Max(AreaTop, RoPcTop) : PillAboveBottom + (Screen.safeArea.yMin / Screen.height) * ((RectTransform)transform).rect.height;
            if (!Mathf.Approximately(root.anchoredPosition.y, y)) root.anchoredPosition = new Vector2(0f, y);
        }
    }
}
