using UnityEngine;
using UnityEngine.UI;
using ZenoniaProto.Net;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// Live portraits of the RO Soft HUD (user 19/11: "the portrait must show my real look"): the real layered hero (class body, hairstyle,
    /// hair / outfit colour through HeroRecolor, the worn hat) standing idle facing down on one HeroPreviewStage far off every map, one column
    /// per slot (0 = the player, 1..4 = the party rows). Each portrait is a RawImage cropped to its column's head and shoulders (a bust like
    /// 02-hud.png), inside the round mask of RoHudStatus / RoHudParty. The stage re-dresses a slot only when its look changes (a new hairstyle,
    /// colour or hat), and its camera runs only while a portrait shows. Hands are left empty so a staff or a sword never crosses the face.
    /// A class without layered art, or a teammate out of sight (no NetAppearance on this client), keeps the painted face (HeroPortraitLayers).
    /// </summary>
    public static class RoHudPortraits
    {
        public const int Slots = 1 + PartyBook.MaxMembers - 1;
        // Bust crop in world units above the feet of a stage column (24 wide, 30 high; RO Fantasy bodies are 20.5 units feet to crown). User 20/11:
        // the head and shoulders fill the circle like 02-hud.png: shoulders at the circle's bottom, the hair at its top edge. The crop is
        // a square filling the mask's box, of which the disc of hud-portrait-mask shows the middle 75.5 %. A hat or hair reaching above
        // MinTop (RoHudPortraitHeadTop) raises the top up to MaxTop, so a tall hat zooms the bust out instead of being cut off.
        const float VisibleOfBox = 0.755f, HeadRoom = 0.3f;
        static float shoulderY = 9.7f, minTop = 20.3f, maxTop = 26f;
        static readonly Vector3 Origin = new Vector3(-38000f, -26000f, 0f); // apart from the character screens, the hero select and the bag portrait

        static HeroPreviewStage stage;
        static readonly HeroRenderer[] stripped = new HeroRenderer[Slots]; // the stage hero whose hands were emptied, per slot
        static readonly float[] headTop = new float[Slots];                 // highest head point seen for the slot's look (held: the idle breath bobs it)
        static readonly CharacterAppearanceData[] measuredLook = new CharacterAppearanceData[Slots];

        /// <summary>The stage, built on first use with `main` (the player's portrait: its screen size sets the texture size).</summary>
        static HeroPreviewStage Stage(RawImage main)
        {
            if (stage != null) return stage;
            stage = HeroPreviewStage.Create(null, main, Slots, HeroPreviewStage.SlotW, Origin);
            Object.DontDestroyOnLoad(stage.gameObject);
            main.uvRect = Crop(0);
            return stage;
        }

        /// <summary>Capture runs (--portrait-crop shoulders,minTop,maxTop): another bust crop, to tune it without a rebuild.</summary>
        public static void SetCrop(float shoulders, float top, float highest)
        {
            shoulderY = shoulders; minTop = top; maxTop = highest;
        }

        /// <summary>
        /// The round inside of a portrait ring (user 20/11: the map showed through around the head): the ChatGPT disc hud-portrait-mask
        /// as the clipping Mask, filling `box` exactly like the ring art does (same sprite size, both preserveAspect, so the disc edge sits
        /// under the ring's band), with an opaque soft sky gradient behind the hero (RoundBackdrop). Children added to the returned rect
        /// (the live portrait, the painted face) are clipped to the circle.
        /// </summary>
        public static RectTransform Frame(RectTransform box)
        {
            RectTransform m = UiKit.Fill(box, "Mask");
            Image mask = RoHud.Art(m, "hud-portrait-mask");
            mask.color = Color.white;
            m.gameObject.AddComponent<Mask>().showMaskGraphic = true;
            RawImage back = UiKit.Fill(m, "Backdrop").gameObject.AddComponent<RawImage>();
            back.texture = RoundBackdrop.Texture;
            back.raycastTarget = false;
            return m;
        }

        /// <summary>A RawImage for a portrait, filling `parent` (the mask). Slot 0 must be made first.</summary>
        public static RawImage Make(RectTransform parent, int slot)
        {
            RawImage raw = UiKit.Fill(parent, "Live").gameObject.AddComponent<RawImage>();
            raw.raycastTarget = false;
            raw.enabled = false;
            if (slot == 0) Stage(raw);
            else if (stage != null) raw.texture = stage.Texture;
            raw.uvRect = Crop(slot);
            return raw;
        }

        /// <summary>The slot's column, cropped to the bust: from the shoulders up to the head's top (hat, hair) within MinTop..MaxTop.</summary>
        static Rect Crop(int slot)
        {
            float top = Mathf.Clamp(headTop[slot] + HeadRoom, minTop, maxTop);
            float side = (top - shoulderY) / VisibleOfBox, centre = HeroPreviewStage.FeetFromBottom + (top + shoulderY) * 0.5f;
            float colW = HeroPreviewStage.SlotW * Slots;
            return new Rect(((slot + 0.5f) * HeroPreviewStage.SlotW - side * 0.5f) / colW, (centre - side * 0.5f) / HeroPreviewStage.ViewH,
                side / colW, side / HeroPreviewStage.ViewH);
        }

        /// <summary>Follows how high the stage hero's head reaches; a new look (class, hairstyle, colours, hat) measures afresh.</summary>
        static void MeasureHead(int slot, HeroRenderer s, CharacterAppearanceData look)
        {
            CharacterAppearanceData m = measuredLook[slot];
            if (m == null || !m.SameLook(look))
            {
                measuredLook[slot] = look.Clone();
                headTop[slot] = float.NegativeInfinity;
            }
            if (s == null) return;
            float t = RoHudPortraitHeadTop.Above(s.LayeredView, s.transform.position.y);
            if (t > headTop[slot]) headTop[slot] = t;
        }

        /// <summary>
        /// Shows `hero` in `raw` (slot's column). False when the hero has no layered look to film (no renderer, paper doll class): the caller
        /// shows the painted face instead. Call every frame the portrait is visible.
        /// </summary>
        public static bool Show(RawImage raw, int slot, HeroController hero)
        {
            HeroRenderer v = hero != null ? hero.View : null;
            if (stage == null || v == null || !v.IsLayered || v.Appearance == null) { raw.enabled = false; return false; }
            if (raw.texture != stage.Texture) raw.texture = stage.Texture;
            stage.Set(slot, hero.Class.Id, v.Appearance, hero.PlayerName, 1f);
            HeroRenderer s = stage.ViewIn(slot);
            if (s != null && stripped[slot] != s) { stripped[slot] = s; s.SetEquipmentGear(null, null); measuredLook[slot] = null; } // another class's hero
            MeasureHead(slot, s, v.Appearance);
            raw.uvRect = Crop(slot);
            stage.SetPose(slot, 0);
            stage.SetActive(true);
            raw.enabled = true;
            return true;
        }

        /// <summary>No portrait shows (HUD hidden): stop filming.</summary>
        public static void Idle() { if (stage != null) stage.SetActive(false); }
    }
}
