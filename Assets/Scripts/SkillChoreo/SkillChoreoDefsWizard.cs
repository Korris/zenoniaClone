using System.Collections.Generic;

namespace ZenoniaProto
{
    /// <summary>
    /// Wizard choreographies, transcribed from plans/261030-1521-skill-trees-implementation/vfx-choreography-wizard.md (times ms, radii tiles, sprite
    /// widths tuned against the 16-unit tile and the ~17-unit chibi hero). Ice Storm keeps IceStormUltimate and has no entry here.
    /// </summary>
    public static class SkillChoreoDefsWizard
    {
        const ChoreoRow C = ChoreoRow.Cast, L = ChoreoRow.Loop, I = ChoreoRow.Impact;
        const ChoreoAnchor Cas = ChoreoAnchor.Caster, Tra = ChoreoAnchor.Travel, Sky = ChoreoAnchor.Sky;

        public static void Register(List<Choreo> into)
        {
            into.Add(ArcaneBolt()); into.Add(Fireball()); into.Add(ArcaneLance()); into.Add(ManaShield());
            into.Add(FrostNova()); into.Add(ElementalBurst()); into.Add(AstralCataclysm());
        }

        static Choreo ArcaneBolt()
        {
            var c = new Choreo("wizard", "arcane-bolt", 480, 7f);
            c.Add(0, 100).Anchor(Cas).Spr(C, 0, 3, 1.8f).Glow(0x29BFFF, 0.5f).Embers(6, 0x59DFFF).Sound("cast");
            c.Add(100, 310).Anchor(Tra).Spr(L, 0, 3, 3.2f).Cycle().Glow(0x39CFFF, 0.65f).Embers(8, 0x70EFFF).Trail();
            c.Add(310, 410).Spr(I, 0, 3, 1.8f).Glow(0x4CCFFF, 0.5f).Ring(0.8f, 110, 0x4CCFFF).Embers(12, 0xB0F8FF).HitStop(20).Quake(65, 0.07f).Sound("impact");
            c.Add(410, 480).Embers(5, 0x70CFFF);
            return c;
        }

        static Choreo Fireball()
        {
            var c = new Choreo("wizard", "fireball", 580, 7f);
            c.Add(0, 110).Anchor(Cas).Spr(C, 0, 3, 1.8f).Glow(0xFF8B18, 0.5f).Embers(9, 0xFFB02A).Sound("cast");
            c.Add(110, 340).Anchor(Tra).Spr(L, 0, 3, 2.8f).Cycle().Glow(0xFF6B00, 0.75f).Embers(14, 0xFF8C18).Trail();
            c.Add(340, 470).Spr(I, 0, 2, 3f).Glow(0xFF8500, 0.45f).Ring(1.5f, 150, 0xFF8500).Embers(26, 0xFF9A12).Debris(5).Dust(8, 0x6A5A50)
                .HitStop(35).Quake(120, 0.17f).Flare(0xFF9A36, 70).Zoom(0.025f, 110).Sound("impact");
            c.Add(470, 580).Spr(I, 3, 3, 3f).Decal(0xA83B12, 1.0f, 1.0f, 0.35f).Embers(8, 0xFF6315);
            return c;
        }

        static Choreo ArcaneLance()
        {
            var c = new Choreo("wizard", "arcane-lance", 520, 8f);
            c.Add(0, 130).Anchor(Cas).Spr(C, 0, 3, 2f).Glow(0xB342FF, 0.5f).Line(0xA537FF, 8f).Embers(10, 0xC875FF).Sound("cast");
            c.Add(130, 300).Anchor(Tra).Spr(L, 0, 3, 3.6f).Cycle().Glow(0xD065FF, 0.9f).Embers(16, 0xA94CFF).Trail();
            c.Add(300, 410).Hits().Spr(I, 0, 2, 1.8f).Glow(0xC36AFF, 0.5f).Ring(1.0f, 90, 0xC36AFF).Embers(20, 0xE7B0FF)
                .HitStop(30).Quake(90, 0.12f).Flare(0xA944FF, 45).Sound("impact");
            c.Add(410, 520).Hits().Spr(I, 3, 3, 1.8f).Glow(0xA84BFF, 0.25f).Embers(7, 0xD18CFF);
            return c;
        }

        static Choreo ManaShield()
        {
            var c = new Choreo("wizard", "mana-shield", 8000);
            c.Add(0, 160).Anchor(Cas).Spr(C, 0, 2, 2.4f).Alpha(0.5f).Decal(0x36CFFF, 0.6f, 1.2f).Glow(0x5ADFFF, 0.3f).Embers(8, 0x8CEFFF).Sound("cast");
            c.Add(160, 350).Anchor(Cas).Spr(C, 3, 3, 2.4f).Alpha(0.5f).Ring(1.3f, 180, 0x42DFFF).Embers(12, 0xA0F7FF).Decal(0x36CFFF, 1.2f, 1.2f, 0.3f);
            c.Add(350, 8000).Anchor(Cas).Spr(L, 0, 3, 2.3f).Cycle().Alpha(0.28f).Glow(0x38BFFF, 0.16f).Decal(0x2C9FFF, 1.2f, 1.2f, 0.25f).Embers(32, 0x8ADFFF).Trail().Sound("loop");
            c.Break(0, 240).Anchor(Cas).Spr(I, 0, 3, 3f).Alpha(0.4f).Glow(0x6CEAFF, 0.3f).Ring(1.5f, 170, 0x6CEAFF).Embers(18, 0xA5F4FF).Debris(8)
                .Quake(90, 0.10f).Flare(0x6EDFFF, 50).Sound("break");
            c.Expire(0, 180).Anchor(Cas).Spr(I, 2, 3, 2.6f).Alpha(0.5f).Glow(0x7CCFFF, 0.25f).Embers(6, 0x7CCFFF);
            return c;
        }

        static Choreo FrostNova()
        {
            var c = new Choreo("wizard", "frost-nova", 850);
            c.Add(0, 200).Spr(C, 0, 3, 3.2f).Alpha(0.7f).Tele(0x35E9FF, 0.8f, 3.0f).Glow(0x6CEFFF, 0.25f).Embers(12, 0xB7F9FF).Sound("cast");
            c.Add(200, 420).Spr(L, 0, 3, 6f).Cycle().Alpha(0.75f).Decal(0x5CDFFF, 0.8f, 3.0f).Ring(3.0f, 220, 0x76F2FF).Embers(20, 0xA5F5FF);
            c.Add(420, 630).Spr(I, 0, 2, 6.4f).Alpha(0.8f).Glow(0xB9F6FF, 0.18f).Ring(3.2f, 180, 0xB8FFFF).Debris(14).Dust(18, 0xDDF4FF).Embers(25, 0xD2FFFF)
                .HitStop(55).Quake(170, 0.28f).Flare(0xB9F6FF, 90).Zoom(0.035f, 160).Sound("impact");
            c.Add(630, 850).Spr(I, 3, 3, 6.4f).Alpha(0.7f).Decal(0x8ADBEF, 3.0f, 3.0f, 0.35f).Dust(10, 0xDDF4FF).Embers(8, 0xB8F5FF);
            return c;
        }

        static Choreo ElementalBurst()
        {
            var c = new Choreo("wizard", "elemental-burst", 1150);
            c.Add(0, 280).Spr(C, 0, 3, 3.4f).Alpha(0.7f).Tele(0x68DFFF, 2.5f, 2.5f).Glow(0xBFEFFF, 0.15f).Embers(12, 0xFF791A).Embers(12, 0x6DEFFF).Embers(12, 0xB15AFF).Sound("cast");
            c.Add(280, 540).Spr(L, 0, 3, 4f).Cycle().Glow(0xBFEFFF, 0.35f).Ring(1.2f, 160, 0x7ADFFF).Embers(15, 0xFF8A22).Embers(15, 0x77EFFF).Embers(15, 0xC06AFF);
            c.Add(540, 850).Spr(I, 0, 2, 5f).Alpha(0.85f).Glow(0xBFEFFF, 0.2f).Ring(2.6f, 180, 0xE0FFFF).Ring(3.0f, 260, 0xA15AFF)
                .Embers(28, 0xFF851A).Embers(28, 0x72EAFF).Embers(24, 0xBC65FF).Debris(18).Dust(20, 0xDDF4FF)
                .HitStop(85).Quake(300, 0.52f).Zoom(0.065f, 230).Flare(0xC7F5FF, 130).Sound("impact");
            c.Add(850, 1150).Spr(I, 3, 3, 5f).Alpha(0.7f).Decal(0x5484C8, 2.5f, 2.5f, 0.3f).Embers(10, 0xFF8A22).Embers(10, 0x77EFFF).Embers(10, 0xC06AFF).Dust(8, 0xDDF4FF);
            return c;
        }

        static Choreo AstralCataclysm()
        {
            var c = new Choreo("wizard", "astral-cataclysm", 3100);
            c.Add(0, 450).Spr(C, 0, 2, 8f).Alpha(0.5f).Tele(0x9238FF, 4.0f, 4.0f).Glow(0xA951FF, 0.45f).Embers(24, 0xC17AFF).Sound("cast");
            c.Add(450, 850).Spr(C, 3, 3, 8f).Alpha(0.5f).Tele(0xB34BFF, 4.0f, 4.0f).Glow(0xA951FF, 0.4f).Ring(3.0f, 300, 0xA850FF).Embers(30, 0xE2A5FF).Quake(200, 0.16f);
            c.Add(850, 1250).Anchor(Sky).Spr(L, 0, 3, 3f).Cycle().Glow(0xD68CFF, 0.9f).Tele(0xD175FF, 4.0f, 4.0f).Embers(32, 0xD17AFF).Trail();
            c.Add(1250, 1480).Spr(I, 0, 1, 5.5f).Glow(0xB86AFF, 0.5f).Ring(3.0f, 200, 0xC55AFF).Debris(18).Dust(14, 0xC9B8E8).Embers(35, 0xC976FF)
                .HitStop(45).Quake(220, 0.45f).Flare(0xB86AFF, 80).Sound("impact");
            float[] ox = { -1.5f, 1.3f, 0.6f }, oy = { 0.5f, -0.8f, 1.5f };
            int[] hitAt = { 1510, 1690, 1870 };
            for (int i = 0; i < 3; i++)
            {
                c.Add(hitAt[i] - 200, hitAt[i]).Anchor(Sky).Off(ox[i], oy[i]).Spr(L, 0, 3, 2.2f).Cycle().Glow(0xD68CFF, 0.8f).Embers(10, 0xD17AFF).Trail();
                c.Add(hitAt[i], hitAt[i] + 140).Off(ox[i], oy[i]).Spr(I, 0, 1, 3.4f).Glow(0xCE79FF, 0.4f).Ring(1.8f, 120, 0xA84FFF).Debris(9).Dust(8, 0xC9B8E8)
                    .Embers(18, 0xCE79FF).HitStop(20).Quake(110, 0.22f).Sound("impact");
            }
            c.Add(1950, 2250).Anchor(Sky).Spr(L, 2, 3, 4.05f).Cycle().Glow(0xF2B7FF, 0.6f).Tele(0xE18CFF, 4.0f, 4.0f).Embers(45, 0xDB9AFF).Quake(300, 0.28f).Trail();
            c.Add(2250, 2550).Spr(I, 0, 2, 9f).Alpha(0.85f).Glow(0xF0C8FF, 0.4f).Ring(4.0f, 240, 0xF0B0FF).Ring(5.0f, 360, 0xA43AFF).Debris(42).Dust(35, 0xC9B8E8).Embers(90, 0xC66BFF)
                .HitStop(125).Quake(480, 0.90f).Zoom(0.105f, 340).Flare(0xE5B6FF, 180).Sound("impact");
            c.Add(2550, 3100).Spr(I, 2, 3, 9f).Alpha(0.7f).Decal(0x7130B2, 4.0f, 4.0f, 0.35f).Ring(4.5f, 380, 0xA65AFF).Embers(28, 0xB67AFF).Dust(18, 0xC9B8E8).Quake(180, 0.12f);
            return c;
        }
    }
}
