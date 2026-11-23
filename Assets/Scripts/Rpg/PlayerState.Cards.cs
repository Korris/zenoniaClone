namespace ZenoniaProto.Rpg
{
    /// <summary>PlayerState: the monster card collection (cards, Soul Gems, Album, deck). Saved by SaveSerializer, filled by CardDrops.</summary>
    public sealed partial class PlayerState
    {
        public readonly CardCollection Cards = new CardCollection();

        /// <summary>A card dropped (or was handed out): +1 copy and the Album mark. True when it is new to the Album.</summary>
        public bool AddCard(string id) { return Cards.Add(id, 1); }
    }
}
