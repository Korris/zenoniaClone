using System.Collections.Generic;

namespace ZenoniaProto
{
    /// <summary>
    /// Assassin choreographies, transcribed from plans/261030-1521-skill-trees-implementation/vfx-choreography-assassin.md (times ms, radii tiles).
    /// Shadow Clone only adds a spawn pulse: SkillMinion keeps drawing the clone and its strikes.
    /// </summary>
    public static class SkillChoreoDefsAssassin
    {
        const ChoreoRow C = ChoreoRow.Cast, L = ChoreoRow.Loop, I = ChoreoRow.Impact;
        const ChoreoAnchor Cas = ChoreoAnchor.Caster;

        public static void Register(List<Choreo> into)
        {
            into.Add(BladeDance());
            into.Add(VenomDaggers()); into.Add(ShadowClone()); into.Add(DeathMark());
        }

        static Choreo BladeDance()
        {
            var c = new Choreo("assassin", "blade-dance", 780);
            c.Add(0, 90).Anchor(Cas).Spr(L, 0, 1, 4f).Alpha(0.72f).Glow(0x8B45D0, 0.35f).Decal(0x35214F, 1f, 2f, 0.22f).Embers(8, 0xA078DB).Dust(3).Quake(55, 0.04f);
            c.Add(90, 150).Anchor(Cas).Spr(L, 1, 2, 4f).Alpha(0.65f).Glow(0x7840B6, 0.28f).Embers(6, 0x7954AC).Trail();
            c.Add(150, 240).Anchor(Cas).Spr(L, 2, 3, 4f).Alpha(0.82f).Glow(0xA35CE8, 0.42f).Embers(10, 0xBA8CF0).Ring(1.7f, 100, 0x814CBF).Quake(60, 0.05f);
            c.Add(240, 300).Anchor(Cas).Spr(L, 0, 1, 4f).Alpha(0.68f).Glow(0x8342B8, 0.30f).Embers(7, 0x8656B8).Trail();
            c.Add(300, 390).Anchor(Cas).Spr(L, 1, 3, 4f).Alpha(0.88f).Glow(0xB46CF0, 0.48f).Embers(12, 0xC69AF2).Ring(1.9f, 100, 0x9A5DDB).Quake(65, 0.07f);
            c.Add(390, 450).Anchor(Cas).Spr(C, 2, 3, 3.5f).Alpha(0.6f).Glow(0xA35AE0, 0.35f).Embers(8, 0xA16ACF).Trail();
            c.Add(450, 570).Anchor(Cas).Spr(I, 0, 2, 4.2f).Glow(0xD19AFF, 0.65f).Embers(20, 0xDBB7FF).Dust(5).Ring(2.2f, 150, 0xB975F2).HitStop(35).Quake(145, 0.27f).Flare(0xB88AEF, 65).Zoom(0.035f, 140).Sound("impact");
            c.Add(570, 600).Anchor(Cas).Spr(I, 2, 3, 4f).Alpha(0.45f).Glow(0x8550B2, 0.20f).Embers(6, 0x9B6AC5).Trail();
            c.Add(600, 780).Anchor(Cas).Spr(I, 3, 3, 3.5f).Alpha(0.12f).Glow(0x593477, 0.06f).Embers(4, 0x8C65AE);
            return c;
        }

        static Choreo VenomDaggers()
        {
            var c = new Choreo("assassin", "venom-daggers", 470);
            c.Add(0, 100).Face().Spr(I, 0, 2, 3.5f).Alpha(0.9f).Glow(0x8C43B8, 0.40f).Embers(10, 0x7ACB80).Dust(2).Ring(1f, 90, 0x7356A8).HitStop(30).Quake(90, 0.14f).Flare(0x9863B5, 35).Zoom(0.015f, 85).Sound("impact");
            c.Add(100, 230).Face().Spr(L, 0, 3, 3.5f).Alpha(0.65f).Glow(0x6E9C52, 0.28f).Decal(0x395B38, 0.5f, 2f, 0.18f).Embers(16, 0x6CBF76).Trail();
            c.Add(230, 350).Face().Spr(I, 2, 3, 3.2f).Alpha(0.35f).Glow(0x527F49, 0.16f).Embers(9, 0x78AF73).Dust(2);
            c.Add(350, 470).Face().Spr(I, 3, 3, 3f).Alpha(0.1f).Glow(0x465B48, 0.05f).Embers(5, 0x65956A).Trail();
            return c;
        }

        static Choreo ShadowClone()
        {
            var c = new Choreo("assassin", "shadow-clone", 600) { KeepsSimArt = true };
            c.Add(0, 120).Anchor(Cas).Decal(0x241733, 0.3f, 0.8f).Ring(0.7f, 110, 0x69408E).Embers(6, 0x8756B0).Dust(2);
            c.Add(120, 280).Anchor(Cas).Decal(0x38204D, 0.8f, 1.1f, 0.32f).Ring(1.2f, 160, 0x8754B7).Embers(12, 0x8D5DB9).Trail();
            c.Add(280, 430).Anchor(Cas).Decal(0x352048, 1.1f, 0.7f, 0.22f).Ring(1.4f, 150, 0x6F419C).Embers(8, 0x9C71C5);
            c.Add(430, 600).Anchor(Cas).Decal(0x241733, 0.7f, 0f, 0.22f).Embers(6, 0x6C4A8D).Trail();
            return c;
        }

        static Choreo DeathMark()
        {
            var c = new Choreo("assassin", "death-mark", 850);
            c.Add(0, 130).Spr(C, 0, 2, 1.8f).Alpha(0.45f).Glow(0xA15CDE, 0.30f).Decal(0x45215E, 0.2f, 0.6f, 0.25f).Ring(0.6f, 110, 0x9252CB).Embers(7, 0xB180DF).Sound("cast");
            c.Add(130, 320).Spr(L, 0, 3, 1.8f).Alpha(0.5f).Glow(0xBC75F0, 0.35f).Ring(0.9f, 170, 0xA968E0).Embers(9, 0xAF7BD7).Trail();
            c.Add(320, 530).Spr(I, 0, 2, 2f).Alpha(0.45f).Glow(0xA65DD8, 0.30f).Decal(0x4E2469, 0.6f, 1f, 0.18f).Embers(8, 0xC193E8);
            c.Add(530, 700).Spr(I, 2, 3, 1.6f).Alpha(0.25f).Glow(0x7B3FA8, 0.15f).Embers(6, 0x9867BC).Trail();
            c.Add(700, 850).Spr(I, 3, 3, 1.2f).Alpha(0.08f).Glow(0x603282, 0.06f).Embers(3, 0x8656A8);
            return c;
        }
    }
}
