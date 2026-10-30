using System;
using UnityEngine;
using UnityEngine.UI;

namespace ZenoniaProto
{
    /// <summary>
    /// NPC dialogue strip at the bottom centre (win-dialogue frame): portrait on the left overlapping the frame, a name plate on the top edge,
    /// the page text with a typewriter reveal and a next arrow. Tap / click the box or press E / Enter: finish the page, then the next page, then
    /// close (onClosed runs after the last page). Esc closes at once. Input handling for the keys lives in WindowsHost.
    /// </summary>
    public sealed class NpcDialogueBox : MonoBehaviour
    {
        const float CharsPerSecond = 55f, BoxW = 560f, BoxH = 132f;
        static NpcDialogueBox instance;

        public static bool IsOpen { get { return instance != null && instance.open; } }

        RectTransform root, arrow;
        Text nameText, body;
        Image portrait;
        string[] pages;
        int page;
        float shown;
        bool open;
        Action onClosed;

        public static void Create(Transform canvas)
        {
            instance = UiKit.Fill(canvas, "NpcDialogueBox").gameObject.AddComponent<NpcDialogueBox>(); // full-screen layer: children anchor to the real screen
            instance.Build();
        }

        void OnDestroy() { if (instance == this) instance = null; }

        /// <summary>Opens the box with the given pages; `portraitSprite` may be null. onClosed runs once after the last page (not on Esc).</summary>
        public static void Open(string speaker, Sprite portraitSprite, string[] lines, Action done)
        {
            if (instance == null || lines == null || lines.Length == 0) return;
            instance.pages = lines; instance.page = 0; instance.shown = 0f;
            instance.onClosed = done;
            instance.nameText.text = speaker;
            instance.portrait.sprite = portraitSprite;
            instance.portrait.enabled = portraitSprite != null;
            instance.open = true;
            instance.root.gameObject.SetActive(true);
            instance.body.text = "";
        }

        /// <summary>Tap / E / Enter: complete the current page, else go to the next one, else close.</summary>
        public static void Advance()
        {
            if (!IsOpen) return;
            NpcDialogueBox d = instance;
            if (d.shown < d.pages[d.page].Length) { d.shown = d.pages[d.page].Length; d.Refresh(); return; }
            if (d.page + 1 < d.pages.Length) { d.page++; d.shown = 0f; d.Refresh(); return; }
            Action done = d.onClosed;
            Close();
            if (done != null) done();
        }

        /// <summary>Closes without running onClosed (Esc).</summary>
        public static void Close()
        {
            if (instance == null) return;
            instance.open = false;
            instance.onClosed = null;
            instance.root.gameObject.SetActive(false);
        }

        void Build()
        {
            root = UiWinKit.Anchored(transform, "Box", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), 30f, 10f, BoxW, BoxH);
            Image frame = UiWinKit.Sliced(root, "dialogue", BoxW / 841f, true); // the frame is also the tap target
            UiWinKit.MakeButton(frame, Advance).transition = Selectable.Transition.None;
            portrait = UiWinKit.Plain(UiKit.Rect(root, "Portrait", -112f, -34f, 150f, 135f), "portrait-elder");
            portrait.raycastTarget = false;
            RectTransform plate = UiKit.Rect(root, "NamePlate", 44f, -16f, 210f, 40f);
            UiWinKit.Sliced(plate, "nameplate", 40f / 92f);
            nameText = UiKit.Label(UiKit.Fill(plate, "Name"), "", 19, UiWinKit.Cream, TextAnchor.MiddleCenter, true);
            body = UiKit.Label(root, "Text", 44f, 30f, BoxW - 44f - 56f, BoxH - 42f, "", 17, UiWinKit.Cream, TextAnchor.UpperLeft);
            arrow = UiKit.Rect(root, "Next", BoxW - 50f, BoxH - 52f, 22f, 27f);
            UiWinKit.Plain(arrow, "next-arrow");
            root.gameObject.SetActive(false);
        }

        void Refresh()
        {
            string full = pages[page];
            int n = Mathf.Clamp((int)shown, 0, full.Length);
            body.text = full.Substring(0, n);
        }

        void Update()
        {
            if (!open) return;
            string full = pages[page];
            if (shown < full.Length)
            {
                shown += CharsPerSecond * Time.unscaledDeltaTime;
                Refresh();
            }
            // the arrow bobs once the page is complete: "tap to continue"
            bool done = shown >= full.Length;
            arrow.gameObject.SetActive(done);
            if (done) arrow.anchoredPosition = new Vector2(BoxW - 50f + Mathf.Sin(Time.unscaledTime * 6f) * 3f, -(BoxH - 52f));
        }
    }
}
