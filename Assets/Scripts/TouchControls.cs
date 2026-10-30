using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// On-screen controls, gold-trimmed dark-navy style: a floating joystick bottom-left (gold ring, diamond studs, steel knob), HP / MP potion buttons
    /// to its right, bottom-right the big attack disc (equipped weapon icon) with the 3-slot skill magazine on an arc around it (SkillMagazineView:
    /// Dash [K], class skill [L] with the cooldown sweep and MP cost, locked slots; swipe up / down or Tab changes the page); top-right the Bag [B] and Settings (hero change) buttons (TouchControls.Menu.cs). Works with touch and mouse (multi-touch: move + attack at once).
    /// It only feeds GameKeys into InputAdapter, so the game logic is the same as the keyboard. Holding attack keeps the combo going.
    /// Shown while a hero plays and no menu is open; F7 hides / shows it (remembered in PlayerPrefs).
    /// </summary>
    [DefaultExecutionOrder(-900)] // after GameClock, before the hero: latches this frame's button presses
    public partial class TouchControls : MonoBehaviour
    {
        const string PrefKey = "zenonia.touchControls";
        static readonly int KeyCount = Enum.GetValues(typeof(GameKey)).Length;
        static readonly bool[] held = new bool[KeyCount], pending = new bool[KeyCount], pressed = new bool[KeyCount];
        static TouchControls instance;
        internal static readonly Color Ready = new Color(1f, 1f, 1f, 0f), NotReady = new Color(0.45f, 0.52f, 0.62f, 0.95f); // accent ring: hidden when ready, grey when not

        RectTransform layer;
        Image attackIcon;
        Text hpCount, mpCount;
        SkillMagazineView magazine;
        Item shownWeapon;
        int shownWeaponEnhance = -1, shownHp = -1, shownMp = -1;
        HeroClass shownClass = (HeroClass)(-1);
        bool visible = true;

        /// <summary>True while an on-screen control holds the key (D-pad direction, attack ...).</summary>
        public static bool Held(GameKey k) { return instance != null && held[(int)k]; }

        /// <summary>True on the frame after a button went down (latched in Update so every reader sees it); held attack repeats.</summary>
        public static bool Pressed(GameKey k)
        {
            if (instance == null) return false;
            return pressed[(int)k] || (k == GameKey.Attack && held[(int)GameKey.Attack]);
        }

        internal static void Press(GameKey k) { pending[(int)k] = true; }
        static void SetHeld(GameKey k, bool on) { held[(int)k] = on; }

        public static TouchControls Create(Transform parent)
        {
            var go = new GameObject("TouchControls");
            go.transform.SetParent(parent, false);
            instance = go.AddComponent<TouchControls>();
            try { instance.visible = PlayerPrefs.GetInt(PrefKey, 1) == 1; } catch (Exception) { instance.visible = true; }
            instance.Build(UiKit.Canvas(parent).transform);
            HeroChangeScreen.Create(parent); // the Settings button opens it
            return instance;
        }

        void OnDestroy()
        {
            if (instance != this) return;
            instance = null;
            ClearAll();
        }

        static void ClearAll()
        {
            for (int i = 0; i < KeyCount; i++) { held[i] = false; pending[i] = false; pressed[i] = false; }
        }

        void Update()
        {
            for (int i = 0; i < KeyCount; i++) { pressed[i] = pending[i]; pending[i] = false; }
            if (InputAdapter.Pressed(GameKey.TouchToggle))
            {
                visible = !visible;
                try { PlayerPrefs.SetInt(PrefKey, visible ? 1 : 0); } catch (Exception) { /* prefs are a convenience */ }
            }
            HeroController hero = GameSession.Hero;
            bool show = visible && hero != null && !InventoryPanel.IsOpen && !HeroChangeScreen.IsOpen && !UiWindows.ModalOpen && !Hud.CaptureClean;
            if (layer.gameObject.activeSelf != show)
            {
                layer.gameObject.SetActive(show);
                if (!show) { for (int i = 0; i < KeyCount; i++) held[i] = false; } // never leave a direction stuck
            }
            if (show && InputAdapter.Pressed(GameKey.SkillPage)) magazine.Cycle(1); // Tab: next skill page (same as a swipe up)
            if (show) Refresh(hero);
        }

        // ---- build ----

        internal static RectTransform Place(Transform parent, string name, Vector2 corner, float x, float y, float size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = corner; rt.anchorMax = corner; rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(x, y);
            rt.sizeDelta = new Vector2(size, size);
            return rt;
        }

        internal static Image Img(RectTransform rt, Sprite s, Color c, bool raycast = false)
        {
            Image i = rt.gameObject.AddComponent<Image>();
            i.sprite = s; i.color = c; i.raycastTarget = raycast;
            return i;
        }

        internal static Image Child(Transform parent, string name, Sprite s, float size, Color c)
        {
            return Img(Place(parent, name, new Vector2(0.5f, 0.5f), 0f, 0f, size), s, c);
        }

        /// <summary>Web disc + accent ring + a circular icon mask; returns the button root.</summary>
        internal static RectTransform DiscButton(Transform parent, string name, Vector2 corner, float x, float y, float radius, Sprite frame, out Image icon, out Image ring, out Transform masked)
        {
            RectTransform rt = Place(parent, name, corner, x, y, radius * 2f);
            Img(rt, frame, Color.white, true);
            ring = Child(rt, "Ring", UiSprites.Ring(), radius * 2f, Ready);
            Image mask = Child(rt, "Mask", UiSprites.Circle(), radius * 2f * 0.82f, Color.white);
            mask.gameObject.AddComponent<Mask>().showMaskGraphic = false;
            masked = mask.transform;
            icon = Child(masked, "Icon", null, radius * 2f * 0.82f, Color.white);
            icon.preserveAspect = true;
            return rt;
        }

        void Build(Transform canvas)
        {
            layer = UiKit.Fill(canvas, "Touch");
            layer.SetAsFirstSibling(); // under the HUD block, toasts and the inventory window

            // Floating joystick (Zenonia 5): the whole lower-left area is the touch zone; the base rests in the corner and jumps
            // under the finger. Added first so the potion buttons (later siblings) still win their own touches.
            RectTransform zone = UiKit.Fill(layer, "JoystickZone");
            zone.anchorMin = Vector2.zero; zone.anchorMax = new Vector2(0.42f, 0.72f);
            zone.offsetMin = zone.offsetMax = Vector2.zero; zone.pivot = Vector2.zero;
            Img(zone, null, new Color(0f, 0f, 0f, 0f), true); // invisible, but catches touches
            RectTransform stickBase = Place(zone, "Base", Vector2.zero, 128f, 128f, 200f); // rest spot like the mockup: bottom-left, clear of the edge
            Img(stickBase, UiArt.JoystickBase(), Color.white);
            Image knob = Child(stickBase, "Knob", UiArt.JoystickKnob(), 80f, Color.white);
            zone.gameObject.AddComponent<TouchJoystick>().Init(zone, stickBase, knob.rectTransform, 62f,
                (up, right, down, left) => { SetHeld(GameKey.Up, up); SetHeld(GameKey.Right, right); SetHeld(GameKey.Down, down); SetHeld(GameKey.Left, left); });

            // Potions (right of the D-pad)
            hpCount = PotionButton("HpPotion", 272f, 162f, true);
            mpCount = PotionButton("MpPotion", 272f, 92f, false);

            // Attack + skill arc (bottom-right): a big attack disc (r78) kept off the screen edge, the small buttons on an arc around it
            Vector2 br = new Vector2(1f, 0f);
            Image ring; Transform masked;
            RectTransform attack = DiscButton(layer, "Attack", br, -146f, 112f, 78f, UiArt.AttackFrame(), out attackIcon, out ring, out masked);
            attackIcon.rectTransform.sizeDelta = new Vector2(110f, 110f);
            TouchButton.Add(attack.gameObject, GameKey.Attack, true);

            magazine = SkillMagazineView.Build(layer); // Dash / class skill / locked slots on the arc, swipe up / down to change page

            BuildMenuButtons(layer);
        }

        Text PotionButton(string name, float x, float y, bool hp)
        {
            Image icon, ring; Transform masked;
            RectTransform b = DiscButton(layer, name, Vector2.zero, x, y, 29f, UiArt.PotionFrame(), out icon, out ring, out masked);
            icon.sprite = LootSprites.Potion(hp);
            icon.rectTransform.sizeDelta = new Vector2(36f, 36f);
            Text count = UiKit.Label(Place(b, "Count", new Vector2(0.5f, 0.5f), 26f, -24f, 40f), "", 12, Color.white, TextAnchor.MiddleCenter, true);
            count.rectTransform.sizeDelta = new Vector2(40f, 16f);
            TouchButton.Add(b.gameObject, hp ? GameKey.HpPotion : GameKey.MpPotion, false);
            return count;
        }

        // ---- per-frame state ----

        internal static Sprite SkillSprite(HeroClassDef cls, int index)
        {
            if (index < 0) return null;
            var tex = Resources.Load<Texture2D>("Sprites/skill-icons/" + cls.IconClass + "-" + index);
            return tex != null ? Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f) : null;
        }

        void Refresh(HeroController hero)
        {
            HeroClassDef cls = hero.Class;
            if (cls.Id != shownClass)
            {
                shownClass = cls.Id;
                shownWeapon = null; shownWeaponEnhance = -1;
            }
            PlayerState p = hero.Rpg;
            Item weapon = p != null ? p.GetEquipped(Slots.Weapon) : null;
            int enh = weapon != null ? weapon.enhance : 0;
            if (weapon != shownWeapon || enh != shownWeaponEnhance)
            {
                shownWeapon = weapon; shownWeaponEnhance = enh;
                attackIcon.sprite = weapon != null ? LootSprites.Icon(weapon) : null;
                attackIcon.enabled = attackIcon.sprite != null;
            }
            magazine.Tick(hero);
            if (p != null && (p.HpPotions != shownHp || p.MpPotions != shownMp))
            {
                shownHp = p.HpPotions; shownMp = p.MpPotions;
                hpCount.text = "x" + p.HpPotions;
                mpCount.text = "x" + p.MpPotions;
            }
        }

        // ---- input components ----

        /// <summary>Button that holds / presses one GameKey while a finger (or the mouse) is down on it.</summary>
        sealed class TouchButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
        {
            GameKey key;
            int pointer = int.MinValue;

            public static void Add(GameObject go, GameKey key, bool big) { go.AddComponent<TouchButton>().key = key; }

            public void OnPointerDown(PointerEventData e)
            {
                if (pointer != int.MinValue) return;
                pointer = e.pointerId;
                SetHeld(key, true);
                Press(key);
                transform.localScale = new Vector3(0.92f, 0.92f, 1f);
            }

            public void OnPointerUp(PointerEventData e)
            {
                if (e.pointerId != pointer) return;
                Release();
            }

            void Release()
            {
                pointer = int.MinValue;
                SetHeld(key, false);
                transform.localScale = Vector3.one;
            }

            void OnDisable() { if (pointer != int.MinValue) Release(); }
        }
    }
}
