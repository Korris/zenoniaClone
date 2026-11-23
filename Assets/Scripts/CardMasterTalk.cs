using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Talking to Card Master Orin (Oakhaven, by the south gate; UiScreens.BindNpc id): the dialogue box pages explain the Monster Card system (drops, My Cards, Album,
    /// Soul Gems, Card Duel v2 "winner takes 1 card from the loser's deck", Ranked), and after the last page the Card window opens. Esc leaves without opening it.
    /// No duels with the NPC: Card Duel is player vs player only (user rule 23/11).
    /// </summary>
    public static class CardMasterTalk
    {
        public const string NpcId = "cardMaster";

        public static void Open(string name, Sprite portrait, string[] pages)
        {
            if (pages == null || pages.Length == 0) return;
            NpcDialogueBox.Open(string.IsNullOrEmpty(name) ? "Card Master" : name, portrait, pages, () => CardWindow.Open(CardWindow.Page.MyCards));
        }
    }
}
