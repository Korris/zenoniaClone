using System.Collections.Generic;

namespace ZenoniaProto
{
    /// <summary>
    /// Cleric choreographies, transcribed from plans/261030-1521-skill-trees-implementation/vfx-choreography-cleric.md (times ms, radii tiles). Anything drawn over a hero
    /// stays translucent: sprite alpha at most 0.5 in a burst, 0.35 in a sustain. SkillSupport adds a small sparkle on allies the choreography does not draw on.
    /// </summary>
    public static class SkillChoreoDefsCleric
    {
        const ChoreoRow C = ChoreoRow.Cast, L = ChoreoRow.Loop, I = ChoreoRow.Impact;
        const ChoreoAnchor Cas = ChoreoAnchor.Caster;

        public static void Register(List<Choreo> into)
        {
            into.Add(Heal()); into.Add(DivineLight());
            into.Add(Sanctuary()); into.Add(GuardianSpirit()); into.Add(Purification()); into.Add(DivineAscension());
        }

        static Choreo Heal()
        {
            var c = new Choreo("cleric", "heal", 550);
            c.Add(0, 100).Spr(I, 0, 1, 2.2f).Alpha(0.45f).Glow(0xFFF2B8, 0.48f).Decal(0xA8E8C2, 0.2f, 0.8f, 0.25f).Embers(12, 0xFFF1B2).Ring(0.9f, 130, 0xBCEFCF);
            c.Add(100, 270).Spr(L, 0, 3, 2f).Alpha(0.32f).Glow(0xFFE8A5, 0.30f).Embers(16, 0xFFF6D6).Ring(1.1f, 180, 0xD7F5D8).Trail();
            c.Add(270, 420).Spr(I, 1, 3, 1.8f).Alpha(0.22f).Glow(0xE7F9D9, 0.18f).Decal(0xBCE8C6, 0.8f, 0.5f, 0.16f).Embers(9, 0xF9F4DC).Trail();
            c.Add(420, 550).Spr(I, 3, 3, 1.5f).Alpha(0.08f).Glow(0xFFF0BE, 0.08f).Embers(4, 0xF4F5D8).Trail();
            return c;
        }

        static Choreo DivineLight()
        {
            var c = new Choreo("cleric", "divine-light", 780);
            c.Add(0, 130).Spr(I, 0, 2, 4f).Alpha(0.95f).Glow(0xFFF4C8, 0.75f).Decal(0xE8E9BC, 0.5f, 2f, 0.28f).Embers(26, 0xFFF6D8).Ring(2f, 180, 0xFFF0BB).HitStop(40).Quake(130, 0.24f).Flare(0xFFF5DC, 90).Zoom(0.035f, 140);
            c.Add(130, 330).Spr(L, 0, 3, 4.2f).Alpha(0.75f).Glow(0xFFE9AC, 0.55f).Decal(0xE6EDC5, 2f, 2.2f, 0.22f).Ring(2.4f, 230, 0xF4E9B9).Embers(24, 0xFFF5D5).Trail();
            c.Add(330, 550).Spr(I, 2, 3, 4f).Alpha(0.38f).Glow(0xF5E9BD, 0.25f).Decal(0xD9E7BD, 2.2f, 2f, 0.16f).Embers(18, 0xFFF8E4).Trail();
            c.Add(550, 780).Spr(I, 3, 3, 3.2f).Alpha(0.1f).Glow(0xE9DDAF, 0.08f).Decal(0xD9E7BD, 2f, 0f, 0.16f).Embers(8, 0xF6F0D8).Trail();
            return c;
        }

        static Choreo Sanctuary()
        {
            var c = new Choreo("cleric", "sanctuary", 8000);
            c.Add(0, 160).Spr(C, 0, 2, 5.5f).Alpha(0.3f).Glow(0xDDF5CA, 0.32f).Decal(0xBCE8C8, 0f, 3f, 0.22f).Ring(3f, 200, 0xE5F5D4).Embers(16, 0xFFF5D8);
            c.Add(160, 450).Spr(C, 2, 3, 5.5f).Alpha(0.28f).Glow(0xFFF0C4, 0.22f).Decal(0xB9E5C3, 3f, 3f, 0.18f).Ring(3.1f, 260, 0xE8F2D4).Embers(12, 0xF8F7E1).Trail();
            c.Add(450, 1000).Spr(L, 0, 3, 5.5f).Cycle().Alpha(0.18f).Glow(0xD8F0CB, 0.12f).Decal(0xB8E7C5, 3f, 3f, 0.14f).Embers(3, 0xFFF7E1).Trail();
            for (int n = 1000; n <= 7000; n += 1000)
            {
                c.Add(n, n + 120).Spr(L, 0, 1, 5.5f).Alpha(0.25f).Glow(0xF2EFCB, 0.20f).Decal(0xBCE8C8, 3f, 3f, 0.17f).Ring(2.8f, 170, 0xE4F5D6).Embers(7, 0xFFF5D9);
                if (n < 7000) c.Add(n + 120, n + 1000).Spr(L, 0, 3, 5.5f).Cycle().Alpha(0.16f).Glow(0xD8F0CB, 0.10f).Decal(0xB8E7C5, 3f, 3f, 0.14f).Embers(5, 0xFFF7E1).Trail();
            }
            c.Add(7120, 7700).Spr(L, 0, 3, 5.5f).Cycle().Alpha(0.14f).Glow(0xD8F0CB, 0.08f).Decal(0xB8E7C5, 3f, 3f, 0.12f).Embers(2, 0xFFF7E1).Trail();
            c.Add(7700, 8000).Spr(I, 2, 3, 5.5f).Alpha(0.12f).Glow(0xDDEBCB, 0.08f).Decal(0xB8E7C5, 3f, 0f, 0.12f).Embers(8, 0xF8F3DF).Trail();
            return c;
        }

        static Choreo GuardianSpirit()
        {
            var c = new Choreo("cleric", "guardian-spirit", 8000);
            c.Add(0, 160).Anchor(Cas).Spr(C, 0, 2, 3.5f).Alpha(0.45f).Glow(0xFFF0BE, 0.42f).Decal(0xD4EBC8, 0.4f, 1.5f, 0.20f).Ring(2f, 170, 0xF5E9BC).Embers(14, 0xFFF4D8);
            c.Add(160, 420).Anchor(Cas).Spr(C, 2, 3, 4f).Alpha(0.48f).Glow(0xF9EDC7, 0.38f).Decal(0xD4EBC8, 1.5f, 2f, 0.18f).Ring(3f, 220, 0xF2E9C4).Embers(16, 0xFFF7E2).Trail();
            c.Add(420, 800).Anchor(Cas).Spr(I, 0, 2, 3.5f).Alpha(0.3f).Glow(0xF3E8C4, 0.20f).Decal(0xD2E6C5, 2f, 1.2f, 0.12f).Embers(10, 0xFFF3D8).Trail();
            c.Add(800, 7500).Anchor(Cas).Spr(L, 0, 3, 2.8f).Cycle().Alpha(0.18f).Glow(0xE9E8C9, 0.12f).Decal(0xCDE5C6, 1.2f, 1.2f, 0.08f).Embers(20, 0xFFF5DE).Trail();
            c.Add(7500, 8000).Anchor(Cas).Spr(I, 2, 3, 2.5f).Alpha(0.12f).Glow(0xE8E2C3, 0.08f).Decal(0xCDE5C6, 1.2f, 0f, 0.08f).Embers(5, 0xF7F0D8).Trail();
            c.Break(0, 110).Anchor(Cas).Spr(I, 0, 1, 3.2f).Alpha(0.5f).Glow(0xFFF5D6, 0.48f).Ring(1.5f, 120, 0xFFF0C7).Embers(14, 0xFFF8E6).Sound("break");
            c.Break(110, 260).Anchor(Cas).Spr(I, 1, 3, 3.5f).Alpha(0.28f).Glow(0xF4E8C9, 0.18f).Ring(2f, 180, 0xE8E7CB).Embers(16, 0xFFF7E6).Trail();
            c.Break(260, 420).Anchor(Cas).Spr(I, 3, 3, 2.8f).Alpha(0.1f).Glow(0xE8E0BF, 0.06f).Embers(6, 0xF6F1DD).Trail();
            c.Expire(0, 140).Anchor(Cas).Spr(I, 2, 3, 2.8f).Alpha(0.18f).Glow(0xF3E9CC, 0.12f).Ring(1.2f, 150, 0xE7E9D1).Embers(7, 0xFFF4DE);
            c.Expire(140, 320).Anchor(Cas).Spr(I, 3, 3, 2.4f).Alpha(0.1f).Glow(0xE7E2C7, 0.06f).Embers(8, 0xF8F2DF).Trail();
            return c;
        }

        static Choreo Purification()
        {
            var c = new Choreo("cleric", "purification", 760);
            c.Add(0, 120).Anchor(Cas).Spr(I, 0, 2, 6f).Alpha(0.48f).Glow(0xFFF5D2, 0.55f).Decal(0xBFECD8, 0.3f, 3f, 0.25f).Embers(24, 0xFFF7DF).Ring(3f, 180, 0xE7F9E3).HitStop(35).Quake(110, 0.20f).Flare(0xF5FFE9, 75).Zoom(0.025f, 120);
            c.Add(120, 300).Anchor(Cas).Spr(L, 0, 3, 6f).Alpha(0.42f).Glow(0xE7F6D6, 0.35f).Decal(0xC5EED8, 3f, 3.2f, 0.20f).Ring(3.4f, 230, 0xF3F7DC).Embers(24, 0xFFF7E5).Trail();
            c.Add(300, 450).Anchor(Cas).Spr(I, 1, 2, 5.5f).Alpha(0.28f).Glow(0xE6F0D7, 0.20f).Decal(0xC9EBD6, 3.2f, 3f, 0.14f).Embers(16, 0xF6F9E8).Trail();
            c.Add(450, 610).Anchor(Cas).Spr(I, 2, 3, 5f).Alpha(0.18f).Glow(0xF1E9CF, 0.12f).Decal(0xD0E8D6, 3f, 2f, 0.10f).Embers(10, 0xFFF5E1).Trail();
            c.Add(610, 760).Anchor(Cas).Spr(I, 3, 3, 4.5f).Alpha(0.06f).Glow(0xE5E6CE, 0.05f).Decal(0xD0E8D6, 2f, 0f, 0.10f).Embers(5, 0xF5F2DD).Trail();
            return c;
        }

        static Choreo DivineAscension()
        {
            var c = new Choreo("cleric", "divine-ascension", 1650);
            c.Add(0, 100).Anchor(Cas).Spr(C, 0, 2, 7f).Alpha(0.5f).Glow(0xFFF7D8, 0.65f).Decal(0xD5F2D5, 0.5f, 5f, 0.30f).Embers(36, 0xFFF8E7).Ring(5f, 250, 0xEAF8D9).Flare(0xFFF8E5, 60).Zoom(0.025f, 150);
            c.Add(100, 260).Anchor(Cas).Spr(I, 0, 2, 9f).Alpha(0.5f).Glow(0xFFF1BA, 0.90f).Decal(0xE8E5BC, 5f, 5f, 0.30f).Embers(65, 0xFFF2C9).Dust(4).Ring(5f, 240, 0xFFF1C5).Ring(5.8f, 330, 0xD7F1D2).HitStop(90).Quake(430, 0.72f).Flare(0xFFF5D9, 170).Zoom(0.085f, 310);
            c.Add(260, 520).Anchor(Cas).Spr(L, 0, 2, 8f).Alpha(0.45f).Glow(0xFFF2C8, 0.55f).Decal(0xD7EFD3, 5f, 5f, 0.22f).Ring(5.3f, 310, 0xF1EECF).Embers(42, 0xFFF8E8).Trail();
            c.Add(520, 900).Anchor(Cas).Spr(L, 0, 3, 7f).Alpha(0.32f).Glow(0xF4EFCF, 0.42f).Decal(0xD5E9CF, 5f, 4.5f, 0.18f).Embers(48, 0xFFF5D8).Trail();
            c.Add(900, 1200).Anchor(Cas).Spr(I, 1, 2, 7f).Alpha(0.24f).Glow(0xF2E7C7, 0.28f).Decal(0xD3E7D0, 4.5f, 4f, 0.14f).Ring(4.5f, 260, 0xE9EED5).Embers(30, 0xFFF8E6).Trail();
            c.Add(1200, 1450).Anchor(Cas).Spr(I, 2, 3, 6.5f).Alpha(0.16f).Glow(0xEDE7D0, 0.15f).Decal(0xD5E7D1, 4f, 2.5f, 0.10f).Embers(22, 0xF8F4E3).Trail();
            c.Add(1450, 1650).Anchor(Cas).Spr(I, 3, 3, 5.5f).Alpha(0.06f).Glow(0xEEE4C9, 0.06f).Decal(0xD5E7D1, 2.5f, 0f, 0.10f).Embers(10, 0xF6F0DC).Trail();
            return c;
        }
    }
}
