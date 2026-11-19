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
        // Bust crop in world units of a stage column (24 wide, 30 high, feet 2 above the bottom; RO Fantasy bodies are 20.5 units feet to crown).
        const float CropSide = 17f, CropCentreY = HeroPreviewStage.FeetFromBottom + 14.8f;
        static readonly Vector3 Origin = new Vector3(-38000f, -26000f, 0f); // apart from the character screens, the hero select and the bag portrait

        static HeroPreviewStage stage;
        static readonly HeroRenderer[] stripped = new HeroRenderer[Slots]; // the stage hero whose hands were emptied, per slot

        /// <summary>The stage, built on first use with `main` (the player's portrait: its screen size sets the texture size).</summary>
        static HeroPreviewStage Stage(RawImage main)
        {
            if (stage != null) return stage;
            stage = HeroPreviewStage.Create(null, main, Slots, HeroPreviewStage.SlotW, Origin);
            Object.DontDestroyOnLoad(stage.gameObject);
            main.uvRect = Crop(0);
            return stage;
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

        /// <summary>The slot's column, cropped to the bust.</summary>
        static Rect Crop(int slot)
        {
            float colW = HeroPreviewStage.SlotW * Slots;
            return new Rect(((slot + 0.5f) * HeroPreviewStage.SlotW - CropSide * 0.5f) / colW, (CropCentreY - CropSide * 0.5f) / HeroPreviewStage.ViewH,
                CropSide / colW, CropSide / HeroPreviewStage.ViewH);
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
            if (s != null && stripped[slot] != s) { stripped[slot] = s; s.SetEquipmentGear(null, null); }
            stage.SetPose(slot, 0);
            stage.SetActive(true);
            raw.enabled = true;
            return true;
        }

        /// <summary>No portrait shows (HUD hidden): stop filming.</summary>
        public static void Idle() { if (stage != null) stage.SetActive(false); }
    }
}
