using System.Collections.Generic;

namespace ZenoniaProto
{
    /// <summary>
    /// Warrior choreographies, transcribed from plans/261030-1521-skill-trees-implementation/vfx-choreography-warrior.md (times ms, radii tiles). Earth Break and Blade Cyclone keep
    /// their hand-made ultimates and have no entry. Arc widths stay near the real hit area: the sprite sits half a radius ahead of the hero, so the arc's own radius is its limit.
    /// </summary>
    public static class SkillChoreoDefsWarrior
    {
        const ChoreoRow C = ChoreoRow.Cast, L = ChoreoRow.Loop, I = ChoreoRow.Impact;
        const ChoreoAnchor Cas = ChoreoAnchor.Caster;

        public static void Register(List<Choreo> into)
        {
            into.Add(PowerSlash()); into.Add(Whirlwind()); into.Add(FlameCleave()); into.Add(ChargingStrike());
        }

        static Choreo PowerSlash()
        {
            var c = new Choreo("warrior", "power-slash", 360);
            c.Add(0, 90).Face().Spr(I, 0, 2, 3.6f).Glow(0xFF6725, 0.55f).Embers(12, 0xFF7025).Debris(4).Dust(9).Ring(1.8f, 100, 0xE97B42).HitStop(35).Quake(100, 0.20f).Flare(0xFFAA65, 45).Zoom(0.025f, 100).Sound("impact");
            c.Add(90, 230).Face().Spr(L, 0, 3, 3.4f).Alpha(0.75f).Glow(0xFF4D19, 0.30f).Embers(10, 0xFF6B26).Debris(3).Dust(6);
            c.Add(230, 360).Face().Spr(I, 2, 3, 3.2f).Alpha(0.35f).Glow(0xA83417, 0.12f).Embers(5, 0xD94C20).Dust(4);
            return c;
        }

        static Choreo Whirlwind()
        {
            var c = new Choreo("warrior", "whirlwind", 950);
            c.Add(0, 100).Anchor(Cas).Spr(L, 0, 1, 5f).Alpha(0.8f).Glow(0xFF6725, 0.40f).Decal(0x8D3820, 2.5f, 2.5f).Embers(12, 0xFF8B31).Debris(3).Dust(10).Ring(2f, 100, 0xD96229).Quake(65, 0.06f).Sound("cast");
            c.Add(100, 250).Anchor(Cas).Spr(L, 1, 2, 5f).Alpha(0.85f).Glow(0xFF541A, 0.45f).Decal(0x963B1D, 2.5f, 2.5f).Embers(14, 0xFF7525).Debris(2).Dust(8);
            c.Add(250, 350).Anchor(Cas).Spr(L, 2, 3, 5f).Alpha(0.9f).Glow(0xFF7A20, 0.50f).Decal(0xA3421B, 2.5f, 2.5f).Embers(15, 0xFF9A35).Debris(4).Dust(10).Ring(2.3f, 100, 0xE86A28).Quake(70, 0.07f);
            c.Add(350, 500).Anchor(Cas).Spr(L, 0, 2, 5f).Alpha(0.9f).Glow(0xFF631C, 0.50f).Decal(0xA8421B, 2.5f, 2.5f).Embers(18, 0xFF8126).Debris(3).Dust(12);
            c.Add(500, 650).Anchor(Cas).Spr(I, 0, 2, 5.2f).Glow(0xFFAB35, 0.65f).Decal(0xB4471B, 2.5f, 2.5f).Embers(25, 0xFF982B).Debris(9).Dust(18).Ring(2.7f, 160, 0xFF8A32).HitStop(35).Quake(150, 0.25f).Flare(0xFFAB65, 65).Zoom(0.035f, 130).Sound("impact");
            c.Add(650, 750).Anchor(Cas).Spr(I, 2, 3, 5f).Alpha(0.55f).Glow(0xE75B20, 0.25f).Decal(0x71301C, 2.5f, 2.5f).Embers(10, 0xFF792B).Dust(12);
            c.Add(750, 950).Anchor(Cas).Spr(I, 3, 3, 4.5f).Alpha(0.2f).Glow(0xA63A1A, 0.10f).Decal(0x5B281C, 2f, 2f).Embers(6, 0xD95520).Dust(8);
            return c;
        }

        static Choreo FlameCleave()
        {
            var c = new Choreo("warrior", "flame-cleave", 700);
            c.Add(0, 100).Face().Spr(I, 0, 1, 4.2f).Glow(0xFF7B16, 0.70f).Decal(0xC54B16, 3f, 3f).Embers(22, 0xFF9B25).Debris(8).Dust(12).Ring(2f, 100, 0xFF7220).HitStop(40).Quake(130, 0.24f).Flare(0xFF9C42, 70).Zoom(0.035f, 130).Sound("impact");
            c.Add(100, 280).Face().Spr(L, 0, 3, 4.5f).Alpha(0.95f).Glow(0xFF8A13, 0.60f).Decal(0xA33D16, 3f, 3f).Embers(28, 0xFFAB2A).Debris(8).Dust(15).Quake(100, 0.08f);
            c.Add(280, 450).Face().Spr(I, 1, 2, 4.5f).Alpha(0.85f).Glow(0xFF6315, 0.50f).Decal(0xB74716, 3f, 3f).Embers(24, 0xFF8122).Debris(5).Dust(12).Quake(100, 0.06f);
            c.Add(450, 700).Face().Spr(I, 2, 3, 4.2f).Alpha(0.35f).Glow(0xE84A12, 0.20f).Decal(0x6F2814, 3f, 3f).Embers(14, 0xFF6A1C).Dust(8);
            return c;
        }

        static Choreo ChargingStrike()
        {
            var c = new Choreo("warrior", "charging-strike", 650);
            c.Add(0, 100).Anchor(Cas).Spr(C, 0, 2, 2f).Alpha(0.8f).Glow(0xF44720, 0.35f).Embers(10, 0xFF6A2A).Debris(3).Dust(12).Quake(65, 0.05f).Sound("cast");
            c.Add(100, 250).Anchor(Cas).Spr(L, 0, 3, 3f).Alpha(0.8f).Glow(0xFF5725, 0.50f).Embers(20, 0xFF7131).Debris(6).Dust(18).Quake(110, 0.07f);
            c.Add(250, 390).Anchor(Cas).Spr(I, 0, 2, 3.5f).Glow(0xFFAA4A, 0.65f).Decal(0x9D4123, 1.5f, 1.5f).Embers(24, 0xFF8B39).Debris(15).Dust(24).Ring(2f, 170, 0xF18A42).HitStop(55).Quake(190, 0.38f).Flare(0xFFB16A, 90).Zoom(0.055f, 160).Sound("impact");
            c.Add(390, 520).Anchor(Cas).Spr(I, 2, 3, 3f).Alpha(0.55f).Glow(0xC64A23, 0.20f).Decal(0x71321D, 1.5f, 1.5f).Embers(10, 0xE86C2C).Debris(4).Dust(14).Quake(70, 0.05f);
            c.Add(520, 650).Anchor(Cas).Spr(I, 3, 3, 2.5f).Alpha(0.2f).Glow(0xA64224, 0.08f).Embers(4, 0xC85A2A).Dust(10);
            return c;
        }
    }
}
