using ZenoniaProto.Rpg;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ZenoniaProto
{
    public partial class TouchControls
    {
        GameObject spBadge;
        Text spBadgeText;
        int shownSp = -1;

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

            // Skills (skill tree window): gold-trimmed disc left of the Bag with a red badge counting the free skill points.
            RectTransform skills = Place(parent, "Skills", tr, -232f, -50f, size);
            Img(skills, UiArt.SkillFrame(), Color.white, true);
            UiKit.Label(UiKit.Fill(skills, "Label"), "Skills", 15, UiWinKit.Gold, TextAnchor.MiddleCenter, true);
            skills.gameObject.AddComponent<TapButton>().onTap = () => SkillTreeWindow.Toggle(transform.parent);
            RectTransform badge = Place(skills, "Badge", tr, -8f, -8f, 28f);
            Img(badge, UiSprites.Circle(), new Color(0.85f, 0.15f, 0.15f, 1f));
            spBadgeText = UiKit.Label(UiKit.Fill(badge, "Count"), "", 15, Color.white, TextAnchor.MiddleCenter, true);
            spBadge = badge.gameObject;
        }

        /// <summary>Swipes the skill magazine one page (dir +1 / -1), like a swipe on the cluster (scripted captures, tests).</summary>
        public static void CycleMagazine(int dir) { if (instance != null && instance.magazine != null) instance.magazine.Cycle(dir); }

        /// <summary>Shows the free skill points on the Skills button badge (hidden at 0).</summary>
        void UpdateSkillBadge(PlayerState p)
        {
            int sp = p != null ? p.SkillPointsFree : 0;
            if (sp == shownSp) return;
            shownSp = sp;
            spBadge.SetActive(sp > 0);
            spBadgeText.text = sp.ToString();
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
