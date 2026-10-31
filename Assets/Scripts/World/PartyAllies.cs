namespace ZenoniaProto
{
    /// <summary>
    /// Who an Ally skill effect may touch: the caster and its party. Offline there is no party system, and the only other heroes are the debug party members (--record-party), which
    /// stay allies. The server asks the party rules; a client asks its own roster, which only draws the replay: the HP and buffs are the server's.
    /// </summary>
    public static class PartyAllies
    {
        public static bool Are(HeroController a, HeroController b)
        {
            if (a == b) return true;
            if (a == null || b == null) return false;
            if (NetMode.IsOffline) return true;
            if (NetMode.IsClient) return NetClient.Party.SameParty(a.PlayerId, b.PlayerId);
            PartyService service = PartyService.Active;
            return service != null && service.Book.SameParty(a.PlayerId, b.PlayerId);
        }
    }
}
