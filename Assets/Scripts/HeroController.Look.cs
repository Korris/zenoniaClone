using ZenoniaProto.Net;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// The saved look of a hero (CharacterAppearanceData) on its renderer. Local hero: from its own save (offline) or its first RpgState (online),
    /// where an old device-only choice (CharacterLookPrefs: style + dye) is moved to the server once with InvOp.Appearance. Other heroes: from
    /// NetAppearance (ProxyLook), colours not chosen are picked from the name hash (same on every client, every map). `--layered-test` draws
    /// every non-local hero with the synthetic layered set (HeroRenderer.Layered.cs).
    /// </summary>
    public partial class HeroController
    {
        bool ownLookKnown; // online client: the hairstyle is not sent before the saved look arrived, or a new device would overwrite it with style 0

        /// <summary>Seed of the deterministic colour pick and the crowd variation: the player name, else the net id, else this object.</summary>
        string LookSeed { get { return CharacterAppearanceData.SeedFor(PlayerName, NetId != 0 ? NetId : GetInstanceID()); } }

        /// <summary>Called once the renderer and the state exist (Init / InitProxy).</summary>
        void SetupLook(bool local)
        {
            if (view == null) return;
            if (HeroRenderer.LayeredTestEnabled && !local) view.UseLayered(HeroRenderer.LayeredTestSet);
            view.SetAppearance(rpg != null ? rpg.Appearance : null, LookSeed, local, false);
        }

        /// <summary>Client: another hero's replicated look (after its name is known, so the pick is the same everywhere).</summary>
        void ProxyAppearance(NetAppearance look)
        {
            CharacterAppearanceData a = CharacterAppearanceData.FromWire(rpg != null ? rpg.ClassId : null, look.HairStyle, look.HairColor, look.OutfitColor, look.Headgear);
            view.SetAppearance(a, LookSeed, false, true);
        }

        /// <summary>
        /// Online client, first RpgState of the local hero: the saved look. A device that still has a CharacterLookPrefs entry sends it to the
        /// server when it differs (style as is, dye -> nearest palette hair colour); a device without one shows the saved hairstyle.
        /// </summary>
        public void OnOwnAppearance(CharacterAppearanceData saved)
        {
            if (ownLookKnown) return;
            ownLookKnown = true;
            if (saved == null) return;
            CharacterAppearanceData want = saved;
            string id = AccountFlow.EnteredId;
            if (id != null && CharacterLookPrefs.Has(id))
            {
                int style, dye;
                CharacterLookPrefs.Get(id, out style, out dye);
                want = CharacterAppearanceData.FromLegacyLook(saved, style, HeroPalettes.LegacyDyeRgb(cls, dye), HeroPalettes.HairRgb());
                if (!want.SameLook(saved)) NetClient.SendInvAction(InvOp.Appearance, want.ToOpText(), 0);
            }
            else if (saved.stored && view != null) view.HairStyle = saved.hairStyle;
            if (view != null) view.SetAppearance(want, LookSeed, true, false);
        }

        /// <summary>The local client may send its hairstyle (SyncHairStyleOnline) once the saved look is known; the host's own hero always may.</summary>
        bool MaySendHairStyle { get { return ownLookKnown || NetMode.IsServerSide; } }
    }
}
