using UnityEngine;
using UnityEngine.UI;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// Touch controls of the compact HUD (hud-compact-mobile-v1.png), mobile layout. Bottom-left: the joystick, one thin
    /// semi-transparent ring with a knob (TouchJoystick and its touch zone are those of the old controls). Bottom-right: the attack
    /// button, the three skill slots on an arc over it (PaperSkillArc), Dash as its own button on its right, the HP / MP potions on its
    /// left. Sizes and places are in HudAnchors. Like the old controls it only feeds GameKeys into InputAdapter.
    /// </summary>
    public sealed class PaperTouchPad
    {
        static readonly Vector2 BottomRight = new Vector2(1f, 0f), Mid = new Vector2(0.5f, 0.5f);
        static readonly Vector2 Attack = new Vector2(-HudAnchors.AttackX, HudAnchors.AttackY); // centre of the attack button from the bottom-right
        static readonly Color AttackFill = new Color(0.72f, 0.16f, 0.13f, 0.86f), AttackHeld = new Color(0.86f, 0.52f, 0.12f, 0.92f), AttackRim = new Color(1f, 0.84f, 0.5f, 0.9f);
        static readonly Color RingFill = new Color(0.06f, 0.07f, 0.1f, 0.22f), KnobFill = new Color(0.78f, 0.82f, 0.88f, 0.72f);
        static readonly Color RingArtTint = new Color(1f, 1f, 1f, 0.38f), KnobArtTint = new Color(1f, 1f, 1f, 0.8f);
        const float KnobSize = 40f, KnobTravel = 34f, SideGap = 10f, SideDrop = 22f; // side buttons sit a little lower than the attack button's centre

        readonly PaperPotionButton hp, mp;
        readonly PaperSkillArc arc;
        readonly Image dashIcon;
        HeroClassDef dashClass;

        public PaperTouchPad(RectTransform layer)
        {
            arc = new PaperSkillArc(layer, Attack); // first: its swipe strip lies under the buttons

            // Fixed joystick: the ring stays in the corner and only a touch around it steers (see TouchJoystick).
            RectTransform zone = UiKit.Fill(layer, "JoystickZone");
            zone.anchorMin = zone.anchorMax = zone.pivot = Vector2.zero;
            zone.anchoredPosition = Vector2.zero;
            zone.sizeDelta = new Vector2(256f, 256f);
            TouchControls.Img(zone, null, Color.clear, true); // invisible, but catches touches
            RectTransform ring = TouchControls.Place(zone, "Base", Vector2.zero, HudAnchors.JoystickX, HudAnchors.JoystickY, HudAnchors.JoystickSize);
            Image ringFace = PaperArt.Draw(ring, PaperPiece.CJoyRing, RingFill, PaperHudCompact.Rim, HudAnchors.JoystickSize * 0.5f);
            RectTransform knob = TouchControls.Place(ring, "Knob", Mid, 0f, 0f, KnobSize);
            Image knobFace = PaperArt.Draw(knob, PaperPiece.CJoyKnob, KnobFill, PaperHudCompact.Rim, KnobSize * 0.5f);
            if (ringFace.sprite != PaperArt.Round()) ringFace.color = RingArtTint; // the cut ring is an opaque disc: the world must show through it
            if (knobFace.sprite != PaperArt.Round()) knobFace.color = KnobArtTint;
            zone.gameObject.AddComponent<TouchJoystick>().Init(ring, knob, KnobTravel, (up, right, down, left) =>
            {
                TouchControls.SetHeld(GameKey.Up, up); TouchControls.SetHeld(GameKey.Right, right);
                TouchControls.SetHeld(GameKey.Down, down); TouchControls.SetHeld(GameKey.Left, left);
            });

            const float P = HudAnchors.PotionSize, D = HudAnchors.DashSize, A = HudAnchors.AttackSize;
            float sideY = Attack.y - SideDrop, mpX = Attack.x - A * 0.5f - SideGap - P * 0.5f;
            hp = new PaperPotionButton(layer, "HpPotion", BottomRight, mpX - P - 6f, sideY, P, true);
            mp = new PaperPotionButton(layer, "MpPotion", BottomRight, mpX, sideY, P, false);
            BuildAttack(layer, BottomRight, Attack, A, A);
            dashIcon = BuildDash(layer, BottomRight, new Vector2(Attack.x + A * 0.5f + SideGap + D * 0.5f, sideY), D, D * 0.5f);
        }

        /// <summary>
        /// The attack button: a touch cell of `cell` units holding GameKey.Attack, the art (`size` units) swapping to its pressed piece
        /// while held. Until the art is cut: a red disc with the old crossed-swords emblem. Also the PC hotbar's.
        /// </summary>
        internal static RectTransform BuildAttack(Transform parent, Vector2 corner, Vector2 pos, float cell, float size)
        {
            RectTransform art;
            RectTransform root = PaperHudCompact.Cell(parent, "Attack", corner, pos.x, pos.y, cell, size, out art);
            float radius = size * 0.5f;
            Image face = PaperArt.Draw(art, PaperPiece.CAttack, AttackFill, AttackRim, radius);
            if (face.sprite == PaperArt.Round()) TouchControls.Child(art, "Emblem", UiArt.AttackIcon(), size * 0.74f, Color.white);
            PaperHudPress.Hold(root.gameObject, GameKey.Attack, down =>
                PaperArt.Redraw(face, down ? PaperPiece.CAttack + PaperPiece.Pressed : PaperPiece.CAttack, down ? AttackHeld : AttackFill, AttackRim, radius));
            return root;
        }

        /// <summary>
        /// The Dash button (a tap presses GameKey.Dash, as the Dash slot of the magazine does). Returns the image for the class's dash
        /// icon, or null when the cut button carries its own glyph. Also the PC hotbar's (`radius` = its slot's corner).
        /// </summary>
        internal static Image BuildDash(Transform parent, Vector2 corner, Vector2 pos, float size, float radius)
        {
            RectTransform art;
            RectTransform root = PaperHudCompact.Cell(parent, "Dash", corner, pos.x, pos.y, Mathf.Max(size, 44f), size, out art);
            Image face = PaperHudCompact.Plate(art, PaperPiece.CDash, radius);
            PaperHudPress.Tap(root.gameObject, () => TouchControls.Press(GameKey.Dash), down => PaperHudCompact.Press(face, PaperPiece.CDash, radius, down));
            if (face.sprite != PaperArt.Round()) return null;
            RectTransform well = UiKit.Fill(art, "IconMask", 2f);
            PaperArt.Plate(well, Color.white, Color.clear, Mathf.Max(1f, radius - 2f));
            well.gameObject.AddComponent<Mask>().showMaskGraphic = false;
            return TouchControls.Child(well, "Icon", null, size - 4f, Color.white);
        }

        /// <summary>Shows `cls`'s dash icon on a button made by BuildDash (no-op when the art has its own glyph or the class did not change).</summary>
        internal static void ShowDash(Image icon, HeroClassDef cls, ref HeroClassDef shown)
        {
            if (icon == null || cls == shown) return;
            shown = cls;
            icon.sprite = TouchControls.SkillSprite(cls, cls.DashIcon);
            icon.enabled = icon.sprite != null;
        }

        public void CyclePage(int dir) { arc.Cycle(dir); }

        public PaperPotionButton Potion(bool hpPotion) { return hpPotion ? hp : mp; }

        public void Tick(HeroController hero, float dt)
        {
            arc.Tick(hero);
            ShowDash(dashIcon, hero.Class, ref dashClass);
            PlayerState p = hero.Rpg;
            if (p == null) return;
            hp.Tick(p, dt);
            mp.Tick(p, dt);
        }
    }
}
