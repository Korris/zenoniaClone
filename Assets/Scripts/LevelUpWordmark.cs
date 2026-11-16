using UnityEngine;
using UnityEngine.UI;

namespace ZenoniaProto
{
    /// <summary>
    /// LEVEL UP of the compact HUD (user, 16/11/2026: "just a yellow LEVEL UP in the middle of the screen, in a nice font"): the gold word
    /// alone, a little above the centre of the screen so it does not cover the hero. No ribbon, no frame, no level number and no line about
    /// skill points, HP or MP. The word is one painted sprite (levelup/levelup-wordmark); until it is delivered the kit's title font draws
    /// it in gold with a dark brown outline. Pops in (80 -> 105 -> 100 % with a fade, 0.25 s), holds 1 s, fades 0.5 s; it never takes
    /// input. Only the local player's level-up shows it (RpgHud.LevelUp); with the HUD switch off the old banner shows instead.
    /// </summary>
    public sealed class LevelUpWordmark
    {
        const float PopSeconds = 0.25f, PeakAt = 0.17f, HoldSeconds = 1f, FadeSeconds = 0.5f, Total = PopSeconds + HoldSeconds + FadeSeconds;
        const float CentreFromTop = 0.38f; // share of the screen height: its lower edge ends just above the head of a hero in the middle of the screen
        const float Height = 50f;         // 7.8 % of the 640-unit canvas
        const int FallbackSize = 58;      // Baloo 2 at this size has capitals about 7 % of the screen high

        readonly RectTransform root;
        readonly CanvasGroup group;
        float age = -1f;

        public bool Showing { get { return age >= 0f; } }

        public LevelUpWordmark(RectTransform safe)
        {
            Vector2 at = new Vector2(0.5f, 1f - CentreFromTop), mid = new Vector2(0.5f, 0.5f);
            root = UiWinKit.Anchored(safe, "LevelUpWordmark", at, mid, 0f, 0f, 420f, Height);
            group = root.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false; group.interactable = false;
            Sprite art = PaperArt.Get(PaperPiece.LevelUpWordmark);
            if (art != null)
            {
                root.sizeDelta = new Vector2(Height * art.rect.width / art.rect.height, Height);
                Image img = root.gameObject.AddComponent<Image>();
                img.sprite = art;
                img.raycastTarget = false;
            }
            else
            {
                Text t = PaperTheme.Caption(root, "LEVEL UP!", FallbackSize, PaperTheme.TitleGold);
                t.GetComponent<Outline>().effectDistance = new Vector2(2f, -2f); // a thicker rim for this size: readable on snow and on dark ground
                Shadow drop = t.gameObject.AddComponent<Shadow>();
                drop.effectColor = new Color(0f, 0f, 0f, 0.55f);
                drop.effectDistance = new Vector2(0f, -4f);
            }
            root.gameObject.SetActive(false);
        }

        public void Show()
        {
            age = 0f;
            root.gameObject.SetActive(true);
            Apply();
        }

        public void Hide()
        {
            age = -1f;
            root.gameObject.SetActive(false);
        }

        /// <summary>Every frame while Showing; dt = unscaled seconds.</summary>
        public void Tick(float dt)
        {
            age += dt;
            if (age >= Total) Hide(); else Apply();
        }

        void Apply()
        {
            float scale = age < PeakAt ? Mathf.Lerp(0.8f, 1.05f, age / PeakAt) : Mathf.Lerp(1.05f, 1f, Mathf.Clamp01((age - PeakAt) / (PopSeconds - PeakAt)));
            root.localScale = new Vector3(scale, scale, 1f);
            float fadeFrom = PopSeconds + HoldSeconds;
            group.alpha = age < fadeFrom ? Mathf.Clamp01(age / (PopSeconds * 0.8f)) : 1f - Mathf.Clamp01((age - fadeFrom) / FadeSeconds);
        }
    }
}
