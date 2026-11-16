using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// HudEntryButtons: the strip of the "..." icon, a small dark capsule under the icon row with Gacha and Settings. A touch anywhere
    /// else closes it (and does nothing else: the veil under the strip swallows it), so does Esc, picking an entry, or a window opening.
    /// Gacha presses its key (G: the same path, with its rules); Settings opens its window as the old round button did.
    /// </summary>
    public sealed partial class HudEntryButtons
    {
        const float StripPad = 3f, StripW = Pitch * 2f + StripPad * 2f, StripH = Pitch + StripPad * 2f;

        RectTransform strip;

        void BuildStrip()
        {
            strip = UiKit.Fill(root, "MoreStrip");
            RectTransform veil = UiKit.Fill(strip, "Outside");
            veil.offsetMin = new Vector2(-400f, -400f); veil.offsetMax = new Vector2(400f, 400f); // past the safe area: the whole screen
            UiKit.Panel(veil, Color.clear, true);
            PaperHudPress.Tap(veil.gameObject, null, down => { if (down) CloseMore(); });

            float keyRoom = HudLayout.IsWeb ? HudAnchors.IconKeyHeight - 2f : 0f;
            RectTransform back = UiWinKit.Anchored(strip, "Back", TopRight, TopRight, -(HudAnchors.MinimapMargin - StripPad), -(HudAnchors.EntryBottom + 2f), StripW, StripH + keyRoom);
            PaperArt.Plate(back, PaperHudCompact.Dark, PaperHudCompact.Rim, 14f, true);
            float y = -(StripPad + Pitch * 0.5f);
            Icon(back, "Gacha", TopRight, -(StripPad + Pitch * 1.5f), y, InputAdapter.KeyLabel(GameKey.PetGacha), () => { CloseMore(); TouchControls.Press(GameKey.PetGacha); });
            Icon(back, "Settings", TopRight, -(StripPad + Pitch * 0.5f), y, InputAdapter.KeyLabel(GameKey.Close), () => { CloseMore(); SettingsWindow.Open(); });
            strip.gameObject.SetActive(false);
        }

        public bool MoreOpen { get { return strip.gameObject.activeSelf; } }

        public void ToggleMore() { if (MoreOpen) CloseMore(); else OpenMore(); }

        public void OpenMore() { strip.gameObject.SetActive(true); }

        public void CloseMore() { if (MoreOpen) strip.gameObject.SetActive(false); }
    }
}
