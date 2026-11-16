using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>What the Wanted UI shows of today's boss for the local hero. A paid bounty wins over alive / respawning (the hero has nothing left to earn today).</summary>
    public enum WantedPhase { Alive, Respawning, Claimed }

    /// <summary>
    /// What the Adventure Paper Wanted UI shares (board, HUD card, notices, reward panel, guide): the state it reads from WantedBossTracker and the small
    /// pieces every view draws (boss picture in a ring, reward slots, clocks). The UI only exists while the server's WantedStatus is known: offline, or on a
    /// dev host that never sent one, Known is false and nothing of it shows.
    /// </summary>
    public static class PaperWanted
    {
        public static readonly Color PortraitBack = PaperTheme.Hex("#1B2A44"), TitleRed = PaperTheme.Hex("#7B2A1B"), AliveGreen = PaperTheme.Hex("#8FE04A");
        const int BossCellPx = 384; // walk cell of every Wanted boss (MonsterDef.Wanted)
        static readonly Dictionary<string, Sprite> Cells = new Dictionary<string, Sprite>();

        /// <summary>The server told this client about today's boss and a hero with a state is playing.</summary>
        public static bool Known
        {
            get { HeroController h = GameSession.Hero; return WantedBossTracker.Status.Known && h != null && h.Rpg != null; }
        }

        public static bool Claimed
        {
            get { HeroController h = GameSession.Hero; return h != null && h.Rpg != null && WantedBossTracker.ClaimedToday(h.Rpg.WantedDay); }
        }

        public static WantedPhase Phase { get { return Claimed ? WantedPhase.Claimed : WantedBossTracker.Status.Alive ? WantedPhase.Alive : WantedPhase.Respawning; } }

        /// <summary>Whole seconds left of a countdown, rounded up (a clock shows 00:01 until the moment itself).</summary>
        public static int Seconds(long ms) { return (int)((ms + 999L) / 1000L); }

        /// <summary>"07:42:15" (hours) or "18:24". Builds a string: call it when the second changed, not every frame.</summary>
        public static string Clock(int seconds, bool hours)
        {
            int m = seconds / 60 % 60, s = seconds % 60;
            return hours ? (seconds / 3600).ToString("00") + ":" + m.ToString("00") + ":" + s.ToString("00") : (seconds / 60).ToString("00") + ":" + s.ToString("00");
        }

        /// <summary>The whole first walk frame (facing down) of a Wanted boss, for the poster and the schedule cards; null when the sheet is missing.</summary>
        public static Sprite BossCell(string type)
        {
            Sprite s;
            if (Cells.TryGetValue(type, out s) && s != null) return s;
            Texture2D tex = Resources.Load<Texture2D>("Sprites/monsters/" + type);
            if (tex == null || tex.width < BossCellPx || tex.height < BossCellPx) return null;
            s = Sprite.Create(tex, new Rect(0, tex.height - BossCellPx, BossCellPx, BossCellPx), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
            Cells[type] = s;
            return s;
        }

        /// <summary>The boss's face (BossPortraits, phase 03) for the rings of the card, the banners and the reward panel.</summary>
        public static Sprite BossFace(string type) { return BossPortraits.Get(type, BossCellPx); }

        /// <summary>A piece at a top-left based place, unsliced pieces keeping their drawn shape; the flat fallback is a plate of `fill`.</summary>
        public static Image Piece(Transform parent, string name, float x, float y, float w, float h, string piece, Color fill, bool raycast = false)
        {
            return PaperArt.Draw(UiKit.Rect(parent, name, x, y, w, h), piece, fill, Color.clear, Mathf.Min(w, h) * 0.3f, raycast);
        }

        /// <summary>
        /// Small red flag with a caption of any length ("REWARD", "Today's Wanted"): the kit's label art stretched over the rect, where
        /// PaperKit.RibbonLabel keeps the flag's short drawn shape and lets a long caption spill out of it.
        /// </summary>
        public static Text Label(Transform parent, string name, float x, float y, float w, float h, string text)
        {
            RectTransform rt = UiKit.Rect(parent, name, x, y, w, h);
            PaperArt.Draw(rt, PaperPiece.LabelRed, PaperTheme.BadgeRed, Color.clear, 4f);
            Text t = PaperTheme.Caption(rt, text, PaperTheme.BarSize, PaperTheme.TextLight);
            t.rectTransform.offsetMax = new Vector2(-w * 0.08f, 0f); // the flag is notched on the right
            return t;
        }

        /// <summary>Icon piece keeping its aspect inside a square of `size`; hidden when the art is missing.</summary>
        public static Image Icon(Transform parent, string name, float x, float y, float size, string piece)
        {
            Image img = UiKit.Icon(parent, name, x, y, size);
            img.sprite = piece != null ? PaperArt.Get(piece) : null;
            img.enabled = img.sprite != null;
            return img;
        }

        /// <summary>
        /// A boss face behind the ring of a banner / card: `plate` is the white window drawn on the same canvas as the ring art (so it is laid over the
        /// same rect `w` x `h`), the face sits in the square (fx, fy, fsize) of that rect. Returns the face image (set its sprite with SetFace).
        /// </summary>
        public static Image RingPortrait(Transform parent, float w, float h, string plate, float fx, float fy, float fsize)
        {
            RectTransform p = UiKit.Rect(parent, "Plate", 0f, 0f, w, h);
            Image back = PaperArt.Draw(p, plate, PortraitBack, Color.clear, h * 0.5f);
            back.color = PortraitBack;
            p.gameObject.AddComponent<Mask>();
            return UiKit.Icon(p, "Face", fx, fy, fsize);
        }

        public static void SetFace(Image face, string bossType)
        {
            face.sprite = BossFace(bossType);
            face.enabled = face.sprite != null;
        }
    }

    /// <summary>
    /// The four squares of the fixed daily bounty (gold, upgrade stones, HP potions, MP potions of the boss's potion size) with their amounts,
    /// as on the poster, the login banner and the reward panel. Set(def) rebuilds the texts only for another boss; SetClaimed greys them and ticks them off.
    /// </summary>
    public sealed class WantedRewardRow
    {
        readonly Image[] icons = new Image[4];
        readonly Text[] amounts = new Text[4];
        readonly GameObject[] ticks = new GameObject[4];
        readonly CanvasGroup[] groups = new CanvasGroup[4];
        WantedBossDef shown;

        public WantedRewardRow(Transform parent, float x, float y, float size, float gap)
        {
            for (int i = 0; i < 4; i++)
            {
                RectTransform slot = UiKit.Rect(parent, "Reward" + i, x + i * (size + gap), y, size, size);
                PaperArt.Draw(slot, PaperPiece.WantedRewardSlot, PaperTheme.Plate, PaperTheme.BtnGoldEdge, size * 0.2f);
                RectTransform body = UiKit.Fill(slot, "Body");
                groups[i] = body.gameObject.AddComponent<CanvasGroup>();
                groups[i].blocksRaycasts = false;
                icons[i] = UiKit.Icon(body, "Icon", size * 0.17f, size * 0.1f, size * 0.66f);
                amounts[i] = PaperTheme.Body(body, "Amount", 0f, size * 0.56f, size, size * 0.38f, "", size >= 40f ? PaperTheme.BodySize : PaperTheme.BarSize, PaperTheme.TextLight, PaperInk.Outlined, TextAnchor.MiddleCenter, true);
                amounts[i].horizontalOverflow = HorizontalWrapMode.Overflow;
                ticks[i] = PaperKit.Mark(slot, PaperMark.Check, size * 0.5f, size * 0.42f, size * 0.5f);
                ticks[i].SetActive(false);
            }
        }

        public void Set(WantedBossDef d)
        {
            if (d == shown) return;
            shown = d;
            icons[0].sprite = LootSprites.Coin(); amounts[0].text = d.Gold.ToString("#,0") + " G";
            icons[1].sprite = LootSprites.Stone(); amounts[1].text = "x" + d.Stones;
            icons[2].sprite = LootSprites.Potion(true, d.PotionSize); amounts[2].text = "x" + d.HpPotions;
            icons[3].sprite = LootSprites.Potion(false, d.PotionSize); amounts[3].text = "x" + d.MpPotions;
        }

        public void SetClaimed(bool claimed)
        {
            for (int i = 0; i < 4; i++)
            {
                groups[i].alpha = claimed ? 0.45f : 1f;
                ticks[i].SetActive(claimed);
            }
        }
    }
}
