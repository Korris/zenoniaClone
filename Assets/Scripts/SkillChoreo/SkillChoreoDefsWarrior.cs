using System.Collections.Generic;

namespace ZenoniaProto
{
    /// <summary>
    /// Warrior choreographies, transcribed from plans/261030-1521-skill-trees-implementation/vfx-choreography-warrior.md (times ms, radii tiles). Earth Break keeps
    /// EarthBreakUltimate and has no entry. Arc widths stay near the real hit area: the sprite sits half a radius ahead of the hero, so the arc's own radius is its limit.
    /// </summary>
    public static class SkillChoreoDefsWarrior
    {
        const ChoreoRow C = ChoreoRow.Cast, L = ChoreoRow.Loop, I = ChoreoRow.Impact;
        const ChoreoAnchor Cas = ChoreoAnchor.Caster;

        public static void Register(List<Choreo> into)
        {
            into.Add(PowerSlash()); into.Add(Whirlwind()); into.Add(FlameCleave()); into.Add(ChargingStrike());
            into.Add(BladeCyclone()); into.Add(InfernoCrash()); into.Add(BerserkerAwakening());
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

        static Choreo BladeCyclone()
        {
            var c = new Choreo("warrior", "blade-cyclone", 2900);
            c.Add(0, 120).Anchor(Cas).Spr(L, 0, 1, 5f).Alpha(0.75f).Glow(0xEF4C22, 0.35f).Decal(0x7C3020, 2.5f, 2.5f).Embers(12, 0xFF6D32).Debris(4).Dust(12).Ring(2f, 110, 0xC95230).Quake(70, 0.05f).Sound("cast");
            c.Add(120, 500).Anchor(Cas).Spr(L, 0, 3, 5f).Cycle().Alpha(0.78f).Glow(0xF65A23, 0.42f).Decal(0x8D3520, 2.5f, 2.5f).Embers(22, 0xFF762D).Dust(23).Trail();
            c.Add(500, 620).Anchor(Cas).Spr(L, 1, 3, 5f).Alpha(0.82f).Glow(0xFF7029, 0.48f).Decal(0x923A20, 2.5f, 2.5f).Embers(14, 0xFF8533).Debris(4).Dust(12).Ring(2.2f, 110, 0xDB6232).Quake(70, 0.06f);
            c.Add(620, 1000).Anchor(Cas).Spr(L, 0, 3, 5f).Cycle().Alpha(0.75f).Glow(0xF85A24, 0.42f).Decal(0x85331E, 2.5f, 2.5f).Embers(16, 0xFF762E).Dust(14).Trail();
            c.Add(1000, 1120).Anchor(Cas).Spr(L, 0, 2, 5f).Alpha(0.85f).Glow(0xFF782C, 0.50f).Decal(0x963C20, 2.5f, 2.5f).Embers(16, 0xFF9135).Debris(5).Dust(13).Ring(2.3f, 110, 0xE66B32).Quake(75, 0.07f);
            c.Add(1120, 1500).Anchor(Cas).Spr(L, 0, 3, 5f).Cycle().Alpha(0.78f).Glow(0xF85B24, 0.45f).Decal(0x85331E, 2.5f, 2.5f).Embers(18, 0xFF7A30).Dust(16).Trail();
            c.Add(1500, 1620).Anchor(Cas).Spr(L, 1, 3, 5f).Alpha(0.9f).Glow(0xFF812C, 0.52f).Decal(0xA04020, 2.5f, 2.5f).Embers(18, 0xFF9638).Debris(6).Dust(15).Ring(2.4f, 120, 0xE87535).Quake(80, 0.08f);
            c.Add(1620, 2000).Anchor(Cas).Spr(L, 0, 3, 5f).Cycle().Alpha(0.8f).Glow(0xFA6326, 0.48f).Decal(0x8C361D, 2.5f, 2.5f).Embers(20, 0xFF8232).Dust(18).Trail();
            c.Add(2000, 2200).Anchor(Cas).Spr(I, 0, 2, 5.2f).Glow(0xFFAB42, 0.65f).Decal(0xB34720, 2.5f, 2.5f).Embers(32, 0xFFA03A).Debris(12).Dust(24).Ring(2.8f, 200, 0xFF9140).HitStop(40).Quake(190, 0.32f).Flare(0xFFAA6A, 80).Zoom(0.045f, 180).Sound("impact");
            c.Add(2200, 2500).Anchor(Cas).Spr(L, 2, 3, 5f).Cycle().Alpha(0.5f).Glow(0xDC4A20, 0.25f).Decal(0x79301C, 2.5f, 2.5f).Embers(12, 0xE96C2C).Dust(16).Trail();
            c.Add(2500, 2900).Anchor(Cas).Spr(I, 2, 3, 4.5f).Alpha(0.2f).Glow(0xA83A1D, 0.08f).Embers(8, 0xC95726).Dust(12);
            return c;
        }

        static Choreo InfernoCrash()
        {
            var c = new Choreo("warrior", "inferno-crash", 1150);
            c.Add(0, 160).Anchor(Cas).Spr(C, 0, 2, 2.2f).Alpha(0.9f).Glow(0xFF681B, 0.50f).Decal(0xA63B18, 0.8f, 0.8f).Embers(18, 0xFF8C25).Debris(4).Dust(12).Quake(80, 0.07f).Sound("cast");
            c.Add(160, 300).Anchor(Cas).Spr(L, 0, 2, 2.4f).Alpha(0.95f).Glow(0xFF8420, 0.65f).Embers(22, 0xFFA02B).Dust(5).Trail();
            c.Add(160, 400).Tele(0xE75A1C, 3f, 3f);
            c.Add(300, 400).Anchor(Cas).Spr(L, 2, 3, 2.6f).Glow(0xFFB13B, 0.85f).Decal(0xA94417, 1f, 1f).Embers(28, 0xFFB63C).Dust(8).Quake(90, 0.14f).Trail();
            c.Add(400, 570).Spr(I, 0, 2, 6f).Glow(0xFF9C22, 0.95f).Decal(0xD64A16, 3f, 3f).Embers(65, 0xFF9B28).Debris(32).Dust(28).Ring(3f, 190, 0xFF8B2B).Ring(3.8f, 260, 0xE65B20).HitStop(100).Quake(420, 0.78f).Flare(0xFFB568, 150).Zoom(0.085f, 280).Sound("impact");
            c.Add(570, 700).Spr(I, 2, 3, 6f).Alpha(0.85f).Glow(0xFF6418, 0.55f).Decal(0xA73B17, 3f, 3f).Embers(30, 0xFF8424).Debris(12).Dust(20).Ring(3.5f, 180, 0xE96C24).Quake(150, 0.18f);
            c.Add(700, 900).Spr(I, 3, 3, 5.5f).Alpha(0.5f).Glow(0xD74B16, 0.30f).Decal(0x792A17, 3f, 3f).Embers(24, 0xFF7422).Debris(4).Dust(16).Quake(100, 0.08f);
            c.Add(900, 1150).Spr(I, 3, 3, 5f).Alpha(0.2f).Glow(0xA83B15, 0.10f).Decal(0x54251A, 2.8f, 2.8f).Embers(12, 0xD95A1B).Dust(14);
            return c;
        }

        static Choreo BerserkerAwakening()
        {
            var c = new Choreo("warrior", "berserker-awakening", 12000);
            c.Add(0, 180).Anchor(Cas).Spr(C, 0, 2, 2.5f).Alpha(0.55f).Glow(0xFF4C20, 0.55f).Decal(0x9B261A, 1.2f, 1.2f).Embers(20, 0xFF762A).Debris(4).Dust(10).Ring(1.6f, 170, 0xD84A26).Quake(130, 0.18f).Flare(0xFF784A, 60).Zoom(0.025f, 130).Sound("cast");
            c.Add(180, 420).Anchor(Cas).Spr(C, 2, 3, 3f).Alpha(0.5f).Glow(0xFF6A1E, 0.50f).Decal(0xA52E18, 1.4f, 1.4f).Embers(26, 0xFF9232).Debris(6).Dust(12).Ring(2f, 220, 0xEF5926).Quake(170, 0.24f);
            c.Add(420, 600).Anchor(Cas).Spr(L, 0, 1, 3f).Alpha(0.35f).Glow(0xFF8A30, 0.35f).Decal(0xA8321B, 1.5f, 1.5f, 0.3f).Embers(18, 0xFF9D3D).Debris(3).Dust(8).Quake(100, 0.12f);
            c.Add(600, 10500).Anchor(Cas).Spr(L, 0, 3, 3f).Cycle().Alpha(0.25f).Glow(0xE9471B, 0.22f).Decal(0x7A2418, 1.2f, 1.2f, 0.25f).Embers(60, 0xFF7830).Trail();
            c.Add(10500, 11500).Anchor(Cas).Spr(L, 0, 3, 3f).Cycle().Alpha(0.2f).Glow(0xD8421A, 0.16f).Decal(0x642019, 1.2f, 1.2f, 0.2f).Embers(4, 0xE96628).Trail();
            c.Add(11500, 12000).Anchor(Cas).Spr(I, 0, 3, 2.8f).Alpha(0.15f).Glow(0xB83A18, 0.12f).Decal(0x4D1A16, 1.2f, 1.2f, 0.15f).Embers(8, 0xC95725).Dust(4);
            return c;
        }
    }
}
