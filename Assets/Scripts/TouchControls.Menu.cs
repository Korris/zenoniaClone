using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ZenoniaProto
{
    public partial class TouchControls
    {
        /// <summary>Top-right round menu buttons (art cut from hud-bag-settings-buttons.png): Bag opens the inventory, Settings the hero change screen.</summary>
        void BuildMenuButtons(Transform parent)
        {
            Vector2 tr = new Vector2(1f, 1f);
            const float size = 80f;
            RectTransform bag = Place(parent, "Bag", tr, -142f, -50f, size);
            Img(bag, MenuSprite("hud-bag"), Color.white, true);
            TouchButton.Add(bag.gameObject, GameKey.Inventory, false);

            RectTransform settings = Place(parent, "Settings", tr, -52f, -50f, size);
            Img(settings, MenuSprite("hud-settings"), Color.white, true);
            settings.gameObject.AddComponent<TapButton>().onTap = HeroChangeScreen.Open;
        }

        static Sprite MenuSprite(string name)
        {
            var tex = Resources.Load<Texture2D>("Sprites/ui/" + name);
            if (tex == null) { Debug.LogWarning("[ZenoniaProto] missing HUD sprite Sprites/ui/" + name); return null; }
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
        }

        /// <summary>One-shot tap (fires on release over the button), with the same press squash as the key buttons.</summary>
        sealed class TapButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
        {
            public System.Action onTap;

            public void OnPointerDown(PointerEventData e) { transform.localScale = new Vector3(0.92f, 0.92f, 1f); }
            public void OnPointerUp(PointerEventData e) { transform.localScale = Vector3.one; }

            public void OnPointerClick(PointerEventData e)
            {
                if (onTap != null) onTap();
            }

            void OnDisable() { transform.localScale = Vector3.one; }
        }
    }
}
