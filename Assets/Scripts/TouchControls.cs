using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// On-screen controls in the web HUD style: an floating joystick bottom-left, and bottom-right the big attack disc (equipped weapon icon) with the
    /// skill slots on an arc around it (web hud-renderer drawSkillSlots positions): Dash [K], class skill [L] with the cooldown sweep and MP cost,
    /// Bag [B]; HP / MP potion buttons next to the D-pad. Works with touch and mouse (multi-touch: move + attack at once).
    /// It only feeds GameKeys into InputAdapter, so the game logic is the same as the keyboard. Holding attack keeps the combo going.
    /// Shown while a hero plays and no menu is open; F7 hides / shows it (remembered in PlayerPrefs).
    /// </summary>
    [DefaultExecutionOrder(-900)] // after GameClock, before the hero: latches this frame's button presses
    public class TouchControls : MonoBehaviour
    {
        const string PrefKey = "zenonia.touchControls";
        static readonly int KeyCount = Enum.GetValues(typeof(GameKey)).Length;
        static readonly bool[] held = new bool[KeyCount], pending = new bool[KeyCount], pressed = new bool[KeyCount];
        static TouchControls instance;
        static readonly Color Ready = new Color(0.61f, 0.89f, 1f), NotReady = new Color(0.4f, 0.49f, 0.55f);

        RectTransform layer;
        Image attackIcon, skillIcon, skillCd, skillRing, dashIcon;
        Text skillCdText, skillMpText, hpCount, mpCount;
        GameObject skillButton;
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

        static void Press(GameKey k) { pending[(int)k] = true; }
        static void SetHeld(GameKey k, bool on) { held[(int)k] = on; }

        public static TouchControls Create(Transform parent)
        {
            var go = new GameObject("TouchControls");
            go.transform.SetParent(parent, false);
            instance = go.AddComponent<TouchControls>();
            try { instance.visible = PlayerPrefs.GetInt(PrefKey, 1) == 1; } catch (Exception) { instance.visible = true; }
            instance.Build(UiKit.Canvas(parent).transform);
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
            bool show = visible && hero != null && !InventoryPanel.IsOpen && !Hud.CaptureClean;
            if (layer.gameObject.activeSelf != show)
            {
                layer.gameObject.SetActive(show);
                if (!show) { for (int i = 0; i < KeyCount; i++) held[i] = false; } // never leave a direction stuck
            }
            if (show) Refresh(hero);
        }

        // ---- build ----

        static RectTransform Place(Transform parent, string name, Vector2 corner, float x, float y, float size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = corner; rt.anchorMax = corner; rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(x, y);
            rt.sizeDelta = new Vector2(size, size);
            return rt;
        }

        static Image Img(RectTransform rt, Sprite s, Color c, bool raycast = false)
        {
            Image i = rt.gameObject.AddComponent<Image>();
            i.sprite = s; i.color = c; i.raycastTarget = raycast;
            return i;
        }

        static Image Child(Transform parent, string name, Sprite s, float size, Color c)
        {
            return Img(Place(parent, name, new Vector2(0.5f, 0.5f), 0f, 0f, size), s, c);
        }

        /// <summary>Web disc + accent ring + a circular icon mask; returns the button root.</summary>
        static RectTransform DiscButton(Transform parent, string name, Vector2 corner, float x, float y, float radius, out Image icon, out Image ring, out Transform masked)
        {
            RectTransform rt = Place(parent, name, corner, x, y, radius * 2f);
            Img(rt, UiSprites.Disc(), Color.white, true);
            ring = Child(rt, "Ring", UiSprites.Ring(), radius * 2f, Ready);
            Image mask = Child(rt, "Mask", UiSprites.Circle(), radius * 2f * 0.8f, Color.white);
            mask.gameObject.AddComponent<Mask>().showMaskGraphic = false;
            masked = mask.transform;
            icon = Child(masked, "Icon", null, radius * 2f * 0.8f, Color.white);
            icon.preserveAspect = true;
            return rt;
        }

        static void KeyBadge(Transform button, float radius, string key)
        {
            RectTransform b = Place(button, "Key", new Vector2(0.5f, 0.5f), radius * 0.7f, -radius * 0.67f, 24f);
            Img(b, UiSprites.Circle(), new Color(0.09f, 0.16f, 0.23f, 1f));
            Child(b, "Ring", UiSprites.Ring(), 28f, new Color(0.65f, 0.74f, 0.79f));
            UiKit.Label(UiKit.Fill(b, "Text"), key, 11, Color.white, TextAnchor.MiddleCenter, true);
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
            RectTransform stickBase = Place(zone, "Base", Vector2.zero, 120f, 120f, 176f);
            Img(stickBase, UiSprites.Circle(), new Color(0.03f, 0.08f, 0.14f, 0.35f));
            Child(stickBase, "Rim", UiSprites.Ring(), 176f, new Color(0.65f, 0.85f, 0.94f, 0.9f));
            Image knob = Child(stickBase, "Knob", UiSprites.Disc(), 84f, Color.white);
            Child(knob.transform, "KnobRing", UiSprites.Ring(), 84f, Ready);
            zone.gameObject.AddComponent<TouchJoystick>().Init(zone, stickBase, knob.rectTransform, 64f,
                (up, right, down, left) => { SetHeld(GameKey.Up, up); SetHeld(GameKey.Right, right); SetHeld(GameKey.Down, down); SetHeld(GameKey.Left, left); });

            // Potions (right of the D-pad)
            hpCount = PotionButton("HpPotion", 240f, 140f, true);
            mpCount = PotionButton("MpPotion", 240f, 76f, false);

            // Attack + skill arc (bottom-right): a big attack disc (r78) kept off the screen edge, the small buttons on an arc around it
            Vector2 br = new Vector2(1f, 0f);
            Image ring; Transform masked;
            RectTransform attack = DiscButton(layer, "Attack", br, -146f, 112f, 78f, out attackIcon, out ring, out masked);
            ring.color = new Color(0.65f, 0.85f, 0.94f);
            attackIcon.rectTransform.sizeDelta = new Vector2(110f, 110f);
            UiKit.Label(Place(attack, "Caption", new Vector2(0.5f, 0.5f), 0f, -50f, 100f), "J / SPACE", 10, new Color(1f, 1f, 1f, 0.85f), TextAnchor.MiddleCenter, true)
                .rectTransform.sizeDelta = new Vector2(100f, 14f);
            TouchButton.Add(attack.gameObject, GameKey.Attack, true);

            RectTransform dash = DiscButton(layer, "Dash", br, -284f, 104f, 36f, out dashIcon, out ring, out masked);
            KeyBadge(dash, 36f, "K");
            TouchButton.Add(dash.gameObject, GameKey.Dash, false);

            RectTransform skill = DiscButton(layer, "Skill", br, -259f, 201f, 36f, out skillIcon, out skillRing, out masked);
            skillCd = Child(masked, "Cooldown", UiSprites.Circle(), 58f, new Color(0.01f, 0.05f, 0.1f, 0.73f));
            skillCd.type = Image.Type.Filled;
            skillCd.fillMethod = Image.FillMethod.Radial360;
            skillCd.fillOrigin = (int)Image.Origin360.Top;
            skillCd.fillClockwise = true;
            skillCdText = UiKit.Label(UiKit.Fill(skill, "CdText"), "", 17, Color.white, TextAnchor.MiddleCenter, true);
            skillMpText = UiKit.Label(Place(skill, "Mp", new Vector2(0.5f, 0.5f), 0f, -46f, 70f), "", 10, Color.white, TextAnchor.MiddleCenter, true);
            skillMpText.rectTransform.sizeDelta = new Vector2(70f, 14f);
            KeyBadge(skill, 36f, "L");
            TouchButton.Add(skill.gameObject, GameKey.Skill, false);
            skillButton = skill.gameObject;

            Image bagIcon;
            RectTransform bag = DiscButton(layer, "Bag", br, -160f, 243f, 30f, out bagIcon, out ring, out masked);
            bagIcon.enabled = false;
            UiKit.Label(UiKit.Fill(bag, "Text"), "BAG", 13, new Color(0.95f, 0.88f, 0.6f), TextAnchor.MiddleCenter, true);
            KeyBadge(bag, 30f, "B");
            TouchButton.Add(bag.gameObject, GameKey.Inventory, false);
        }

        Text PotionButton(string name, float x, float y, bool hp)
        {
            Image icon, ring; Transform masked;
            RectTransform b = DiscButton(layer, name, Vector2.zero, x, y, 26f, out icon, out ring, out masked);
            icon.sprite = LootSprites.Potion(hp);
            icon.rectTransform.sizeDelta = new Vector2(34f, 34f);
            ring.color = hp ? new Color(1f, 0.55f, 0.6f) : new Color(0.5f, 0.75f, 1f);
            Text count = UiKit.Label(Place(b, "Count", new Vector2(0.5f, 0.5f), 22f, -18f, 40f), "", 12, Color.white, TextAnchor.MiddleCenter, true);
            count.rectTransform.sizeDelta = new Vector2(40f, 16f);
            KeyBadge(b, 26f, hp ? "1" : "2");
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
                skillIcon.sprite = SkillSprite(cls, cls.SkillIcon);
                dashIcon.sprite = SkillSprite(cls, cls.DashIcon);
                skillButton.SetActive(cls.SkillName != null);
                skillMpText.text = cls.SkillName != null ? (int)cls.SkillMp + " MP" : "";
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
            if (cls.SkillName != null)
            {
                float cd = hero.SkillCooldownLeftMs;
                bool enoughMp = hero.Mp >= cls.SkillMp, ready = cd <= 0f && enoughMp;
                skillIcon.color = new Color(1f, 1f, 1f, ready ? 1f : 0.4f);
                skillRing.color = ready ? Ready : NotReady;
                skillCd.fillAmount = cd > 0f ? Mathf.Clamp01(cd / cls.SkillCooldownMs) : 0f;
                skillCd.enabled = cd > 0f;
                skillCdText.text = cd > 0f ? (cd / 1000f).ToString("F1") : "";
                skillMpText.color = enoughMp ? new Color(0.67f, 0.88f, 0.98f) : new Color(1f, 0.54f, 0.59f);
            }
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
