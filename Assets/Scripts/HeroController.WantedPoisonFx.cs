using UnityEngine;
using ZenoniaProto.Net;

namespace ZenoniaProto
{
    /// <summary>
    /// On-body look of a hero poisoned by a Wanted Boss (ChatGPT Tay chat 6aff091a, 2026-11-19, asked about a 40-player fight): yes, but graded by
    /// who is looking, and never a pulsing tint or a skull wisp:
    ///  - your own hero: olive tint 20 %, 2 small venom bubbles a second rising off the body;
    ///  - a party member: tint 12 %, 1 bubble a second;
    ///  - anyone else: tint 8 %, no particles;
    ///  - drained by Venom Drain: a 120 ms green flash on that hero (the tether to the boss is MireHydraLightFx's).
    /// One shared ParticleSystem for every hero (WantedFxParticles.Emit at each body), so 40 poisoned heroes cost one system, not 40.
    /// Reads PoisonMsLeft (replicated: NetEventType.HeroStatus), runs wherever NetMode.RendersFx.
    /// </summary>
    public partial class HeroController
    {
        const float PoisonTintSelf = 0.20f, PoisonTintParty = 0.12f, PoisonTintOther = 0.08f, DrainFlashMs = 120f, DrainFlashTint = 0.7f;
        static ParticleSystem poisonBubbles;
        float poisonBubbleDebt;

        void UpdatePoisonFx()
        {
            if (!NetMode.RendersFx || view == null || dead) return;
            bool flash = MsSinceDrained >= 0f && MsSinceDrained < DrainFlashMs;
            if (PoisonMsLeft <= 0f && !flash) return;
            bool self = this == LocalPlayer.Hero, party = !self && IsPartyMate();
            float tint = flash ? DrainFlashTint : self ? PoisonTintSelf : party ? PoisonTintParty : PoisonTintOther;
            view.SetWantedPoisonTint(tint);
            float rate = PoisonMsLeft > 0f ? (self ? 2f : party ? 1f : 0f) : 0f;
            if (rate <= 0f || !MireHydraFxLook.Ready || Zone.Main == null) return;
            if (poisonBubbles == null) poisonBubbles = WantedFxParticles.Create(Zone.Main.Root, "PoisonBodyBubbles", MireHydraFxLook.Bubble, FeelConfig.FxOrder - 30, 64, 0f);
            poisonBubbleDebt += rate * GameClock.Dt;
            for (; poisonBubbleDebt >= 1f; poisonBubbleDebt -= 1f)
            {
                Vector2 p = Position + new Vector2(Random.Range(-5f, 5f), Random.Range(6f, 16f));
                WantedFxParticles.Emit(poisonBubbles, p, new Vector2(Random.Range(-1.5f, 1.5f), Random.Range(7f, 11f)), Random.Range(2.4f, 3.4f), MireHydraFxLook.Moss, Random.Range(1f, 1.3f));
            }
        }

        /// <summary>Another hero in the local player's party (client roster): the drain tethers and poison grading favour them.</summary>
        public bool IsPartyMateOfLocal { get { return this != LocalPlayer.Hero && IsPartyMate(); } }

        bool IsPartyMate()
        {
            PartyClientState party = NetClient.Party;
            if (party == null || !party.InParty || PlayerId == 0) return false;
            for (int i = 0; i < party.Members.Count; i++) if (party.Members[i].PlayerId == PlayerId) return true;
            return false;
        }
    }
}
