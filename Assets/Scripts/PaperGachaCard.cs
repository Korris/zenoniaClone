using UnityEngine;
using UnityEngine.UI;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// One result of a x10 pull on Lumi's counter (pet-gacha-result-states mockup, state 2): the white-rimmed card with its rim tinted by what the pull was,
    /// the pet with its stars and name; a duplicate is dimmed and says the shards it gave, the shard result shows the shard; a new pet wears
    /// the green NEW ribbon. Everything comes from the pull's history record.
    /// </summary>
    public sealed class PaperGachaCard
    {
        public const float W = 104f, H = 112f;
        static readonly Color ShardTint = new Color(0.72f, 0.5f, 1f), RareTint = new Color(1f, 0.8f, 0.3f), NewTint = new Color(0.5f, 0.95f, 0.5f);

        public readonly RectTransform Root;
        readonly Image frame, icon, rarity;
        readonly Text text;
        readonly PaperStars stars;
        readonly GameObject fresh;

        public PaperGachaCard(Transform parent, string name, float x, float y)
        {
            Root = UiKit.Rect(parent, name, x, y, W, H);
            frame = PaperArt.Draw(UiKit.Fill(Root, "Frame"), null, Color.white, Color.clear);
            icon = UiKit.Icon(Root, "Icon", (W - 58f) * 0.5f, 10f, 58f);
            stars = new PaperStars(Root, "Stars", (W - 5 * 11f) * 0.5f, 64f, 5, 11f);
            text = PaperTheme.Body(Root, "Text", 4f, 76f, W - 8f, 30f, "", PaperTheme.BarSize, PaperTheme.TextLight, PaperInk.World, TextAnchor.MiddleCenter, true);
            text.lineSpacing = 0.85f;
            rarity = UiKit.Icon(Root, "Rarity", -6f, -9f, 38f);
            RectTransform ribbon = UiKit.Rect(Root, "New", 36f, -10f, 66f, 22f);
            PaperArt.Draw(ribbon, PaperPiece.GachaRibbonNew, PaperTheme.LabelGreen, Color.clear, 4f).preserveAspect = true;
            Text cap = PaperTheme.Caption(ribbon, "NEW!", PaperTheme.BarSize, PaperTheme.TextLight);
            cap.rectTransform.offsetMin = new Vector2(0f, 6f); // the band sits above the ribbon's tails
            fresh = ribbon.gameObject;
        }

        /// <summary>`ownedStars` = the account's stars of that pet now (0 for the shard result).</summary>
        public void Set(PullRecord r, int ownedStars)
        {
            PetDef d = PetConfig.Get(r.id);
            // Rim as on the mockup: purple = shards, gold = SSR or better, green = a new pet, the card's own white rim = a duplicate.
            Color tint = d == null ? ShardTint : d.Rarity >= PetRarity.SSR ? RareTint : r.dup ? Color.white : NewTint;
            if (PaperArt.Redraw(frame, PaperPiece.GachaResultCard, PaperTheme.Navy, tint)) frame.color = tint; // the cut card is blue with a white rim: the tint colours the rim
            icon.sprite = d != null ? PetArt.Icon(d) : PaperArt.Get(PaperPiece.IconShard);
            icon.enabled = icon.sprite != null;
            Color c = d != null ? PetArt.TintOf(d) : Color.white;
            icon.color = r.dup ? new Color(c.r * 0.6f, c.g * 0.6f, c.b * 0.6f, 0.8f) : c;
            LumiCounterWindow.ShowRarity(rarity, d);
            stars.Root.gameObject.SetActive(d != null);
            stars.Set(ownedStars);
            text.text = d == null ? "+" + r.shards + "\nShared Shards" : r.dup ? d.Name + "\n<color=#ffd27a>+" + r.shards + " shards</color>" : d.Name;
            fresh.SetActive(d != null && !r.dup);
        }
    }
}
