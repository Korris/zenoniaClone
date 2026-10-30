namespace ZenoniaProto
{
    /// <summary>
    /// Field loot priority (decided): the top damage dealer of the kill owns the drop for 3 s, then anyone may take it; when the owner is in a
    /// party the owner's party mates share that priority for 2 s more before it turns free for everybody. Offline drops have no owner (id 0).
    /// </summary>
    public static class LootOwnership
    {
        public const float PriorityMs = 3000f, PartyExtraMs = 2000f;

        /// <summary>Zone time at which the drop becomes free for everybody.</summary>
        public static float FreeAtMs(float dropTimeMs, bool ownerInParty) { return dropTimeMs + PriorityMs + (ownerInParty ? PartyExtraMs : 0f); }

        /// <summary>May `heroId` pick the drop up now? (ownerId 0 = no owner; party mates of the owner are free to pick at the end of the base window.)</summary>
        public static bool CanPick(ushort ownerId, float freeAtMs, ushort heroId, float nowMs, bool pickerIsOwnerPartyMate)
        {
            if (ownerId == 0 || heroId == ownerId || nowMs >= freeAtMs) return true;
            return pickerIsOwnerPartyMate && nowMs >= freeAtMs - PartyExtraMs;
        }
    }
}
