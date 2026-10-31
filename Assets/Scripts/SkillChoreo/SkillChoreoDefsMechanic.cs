using System.Collections.Generic;

namespace ZenoniaProto
{
    /// <summary>
    /// Mechanic choreographies, transcribed from plans/261030-1521-skill-trees-implementation/vfx-choreography-mechanic.md (times ms, radii tiles). Power Shot and Rocket Punch
    /// fly at the sim's real speed so their pops land on the real hits. Plasma Barrage, Robot Assault and Orbital Cannon only add to the bodies the sim keeps drawing.
    /// Rocket Barrage keeps RocketBarrageUltimate and has no entry.
    /// </summary>
    public static class SkillChoreoDefsMechanic
    {
        const ChoreoRow C = ChoreoRow.Cast, L = ChoreoRow.Loop, I = ChoreoRow.Impact;
        const ChoreoAnchor Cas = ChoreoAnchor.Caster, Tra = ChoreoAnchor.Travel;

        public static void Register(List<Choreo> into)
        {
            into.Add(PowerShot()); into.Add(RocketPunch()); into.Add(PlasmaBarrage());
            into.Add(RobotAssault()); into.Add(TargetLock()); into.Add(EmpBurst()); into.Add(OrbitalCannon());
        }

        static Choreo PowerShot()
        {
            var c = new Choreo("mechanic", "power-shot", 480, 7f);
            c.Add(0, 75).Anchor(Cas).Spr(C, 0, 2, 1.6f).Glow(0xFFB13B, 0.75f).Embers(14, 0xFFB44A).Dust(5).Ring(0.6f, 75, 0xFFA341).Quake(70, 0.12f).Flare(0xFFD38A, 35).Zoom(0.012f, 80);
            c.Add(0, 290).Anchor(Tra).Spr(L, 0, 3, 1.3f).Cycle().Glow(0x58DFFF, 0.65f).Embers(18, 0x70E8FF).Embers(10, 0xFFB14B).Trail();
            c.Add(290, 390).Hits().Spr(I, 0, 2, 1.4f).Alpha(0.9f).Glow(0xB7F5FF, 0.50f).Embers(9, 0xFFCA65).Dust(3).Ring(0.8f, 90, 0x66D9FF).Quake(55, 0.05f);
            c.Add(290, 390).Spr(I, 2, 3, 1.2f).Alpha(0.55f).Glow(0x69CFEF, 0.30f).Embers(8, 0x8CEBFF).Ring(0.7f, 90, 0x6BD8F5);
            c.Add(390, 480).Spr(I, 3, 3, 1f).Alpha(0.12f).Glow(0x4C91AB, 0.08f).Embers(5, 0xA5B4BE).Trail();
            return c;
        }

        static Choreo RocketPunch()
        {
            var c = new Choreo("mechanic", "rocket-punch", 570, 4f);
            c.Add(0, 95).Anchor(Cas).Spr(C, 0, 2, 2.2f).Glow(0xFF8A2A, 0.75f).Embers(20, 0xFFB24D).Dust(8).Ring(0.9f, 100, 0xFF8D38).Quake(95, 0.18f).Flare(0xFFBB72, 50).Zoom(0.02f, 95);
            c.Add(0, 285).Anchor(Tra).Spr(L, 0, 3, 2f).Cycle().Glow(0xFF7424, 0.60f).Embers(22, 0xFF9735).Embers(12, 0xB6C5D0).Trail();
            c.Add(285, 405).Hits().Spr(I, 0, 2, 2.4f).Glow(0xFFC16B, 0.65f).Embers(18, 0xFFBA55).Debris(5).Dust(10).Ring(1.2f, 120, 0xFF9545).Quake(80, 0.08f);
            c.Add(285, 410).Spr(I, 2, 3, 2.2f).Alpha(0.7f).Glow(0xE98235, 0.38f).Embers(12, 0xFFA74D).Dust(8).Ring(1f, 120, 0xE88D42);
            c.Add(410, 570).Spr(I, 3, 3, 1.8f).Alpha(0.18f).Glow(0xA65A30, 0.10f).Embers(10, 0x89949E).Dust(4).Trail();
            return c;
        }

        static Choreo PlasmaBarrage()
        {
            var c = new Choreo("mechanic", "plasma-barrage", 320) { KeepsSimArt = true };
            c.Add(0, 75).Anchor(Cas).Ring(0.5f, 85, 0x62DFFF).Embers(12, 0xA5F4FF).Quake(65, 0.08f).Flare(0xB9F5FF, 35);
            c.Add(75, 175).Anchor(Cas).Ring(1f, 110, 0x4FCFFF).Embers(22, 0x78E9FF).Embers(18, 0xB1F6FF).Quake(90, 0.12f);
            c.Add(175, 260).Anchor(Cas).Ring(0.8f, 100, 0x83EFFF).Embers(15, 0x66CFFF).Embers(9, 0xE0FAFF);
            c.Add(260, 320).Anchor(Cas).Embers(7, 0x78C5DD).Embers(4, 0xB4EFFF);
            return c;
        }

        static Choreo RobotAssault()
        {
            var c = new Choreo("mechanic", "robot-assault", 580) { KeepsSimArt = true };
            c.Add(0, 120).Anchor(Cas).Ring(0.6f, 120, 0x55DFFF).Embers(8, 0x83E9FF);
            c.Add(120, 280).Anchor(Cas).Ring(1.1f, 160, 0x6BDFFF).Embers(12, 0x7BD8FF).Embers(10, 0xFFB75B).Quake(90, 0.06f);
            c.Add(280, 430).Anchor(Cas).Decal(0x3C8EA8, 0.4f, 1.2f, 0.14f).Ring(1.4f, 170, 0x64CFFF).Embers(10, 0x8BEFFF).Trail();
            c.Add(430, 580).Anchor(Cas).Decal(0x3C8EA8, 1.2f, 0f, 0.14f).Embers(6, 0x79B8D0).Trail();
            return c;
        }

        static Choreo TargetLock()
        {
            var c = new Choreo("mechanic", "target-lock", 760);
            c.Add(0, 100).Anchor(Cas).Spr(C, 0, 1, 2f).Alpha(0.45f).Glow(0xFF9A35, 0.35f).Tele(0xF7A544, 1f, 7f).Ring(7f, 170, 0xF3A33B).Embers(12, 0xFFB95B);
            c.Add(100, 260).Anchor(Cas).Spr(L, 0, 3, 7f).Alpha(0.4f).Glow(0xF8A342, 0.30f).Tele(0xD88A35, 7f, 7f).Ring(7f, 210, 0xE7A34D).Embers(12, 0xFFBA62).Trail();
            c.Add(260, 430).Anchor(Cas).Spr(I, 0, 2, 5f).Alpha(0.35f).Glow(0xFFB65C, 0.28f).Decal(0xA76C32, 1f, 2f, 0.12f).Ring(5f, 170, 0xF3B15B).Embers(10, 0xFFCB79);
            c.Add(430, 610).Anchor(Cas).Spr(I, 2, 3, 4f).Alpha(0.22f).Glow(0xC98A42, 0.15f).Tele(0xB57C3C, 7f, 2f).Embers(8, 0xDFA75E).Trail();
            c.Add(610, 760).Anchor(Cas).Spr(I, 3, 3, 2.5f).Alpha(0.08f).Glow(0xA16A35, 0.06f).Tele(0xA16A35, 2f, 0f).Embers(4, 0xD7A061);
            return c;
        }

        static Choreo EmpBurst()
        {
            var c = new Choreo("mechanic", "emp-burst", 820);
            c.Add(0, 130).Anchor(Cas).Spr(I, 0, 2, 6f).Glow(0xB7F5FF, 0.90f).Decal(0x3B9EC0, 0.3f, 3f, 0.30f).Embers(35, 0xA9F3FF).Debris(5).Dust(8).Ring(3f, 180, 0x75E5FF).HitStop(65).Quake(200, 0.45f).Flare(0xC8F7FF, 110).Zoom(0.055f, 200);
            c.Add(130, 310).Anchor(Cas).Spr(L, 0, 3, 6.2f).Alpha(0.8f).Glow(0x66DFFF, 0.60f).Decal(0x397E9C, 3f, 3.2f, 0.22f).Ring(3.5f, 230, 0x8EEFFF).Embers(32, 0x8BEFFF).Embers(16, 0xE2FAFF).Quake(130, 0.12f).Trail();
            c.Add(310, 450).Anchor(Cas).Spr(I, 1, 2, 6f).Alpha(0.5f).Glow(0x59BFD8, 0.35f).Decal(0x326A85, 3.2f, 3f, 0.18f).Embers(20, 0x9FEAFF).Embers(12, 0xD0F7FF).Trail();
            c.Add(450, 630).Anchor(Cas).Spr(I, 2, 3, 5.5f).Alpha(0.25f).Glow(0x4C9DB7, 0.18f).Decal(0x325B72, 3f, 2f, 0.12f).Embers(12, 0x8AC9DE).Trail();
            c.Add(630, 820).Anchor(Cas).Spr(I, 3, 3, 4.5f).Alpha(0.08f).Glow(0x427C96, 0.06f).Decal(0x325B72, 2f, 0f, 0.12f).Embers(6, 0x8CB8C9).Trail();
            return c;
        }

        static Choreo OrbitalCannon()
        {
            var c = new Choreo("mechanic", "orbital-cannon", 1750) { KeepsSimArt = true };
            c.Add(0, 120).Tele(0xFFAD42, 0f, 4f).Decal(0xA76C32, 0f, 2f, 0.20f).Ring(1.5f, 120, 0xFFB75A).Embers(12, 0xFFC56C).Quake(65, 0.04f);
            c.Add(120, 250).Tele(0xF2A43C, 2f, 4f).Decal(0x9B6530, 2f, 4f, 0.18f).Embers(12, 0xFFBC5D).Trail();
            c.Add(250, 370).Tele(0xF6AD4C, 4f, 4f).Ring(2f, 130, 0xFFB34F).Embers(14, 0xFFC16B).Dust(3).Quake(70, 0.05f);
            c.Add(370, 500).Decal(0xA36A34, 4f, 4f, 0.20f).Embers(10, 0xFFB65A).Trail();
            c.Add(500, 620).Ring(2.3f, 140, 0xFFBB5E).Embers(16, 0xFFCC75).Dust(4).Quake(75, 0.06f);
            c.Add(620, 750).Tele(0xFFC15D, 4f, 4f).Decal(0xB77734, 4f, 4f, 0.24f).Embers(14, 0xFFC46C).Trail();
            c.Add(750, 870).Ring(2.6f, 150, 0xFFC16B).Embers(18, 0xFFD080).Dust(4).Quake(80, 0.07f);
            c.Add(870, 1000).Tele(0xFFD17C, 4f, 4f).Decal(0xC3843E, 4f, 4f, 0.26f).Embers(18, 0xFFD38A).Trail();
            c.Add(1000, 1120).Ring(3f, 170, 0xFFD17C).Embers(22, 0xFFD890).Dust(5).Quake(85, 0.08f);
            c.Add(1120, 1250).Tele(0xFFE09A, 4f, 4f).Decal(0xD08A3C, 4f, 4f, 0.30f).Ring(3.5f, 130, 0xFFD58A).Embers(28, 0xFFE1A1).Quake(100, 0.08f).Trail();
            c.Add(1250, 1450).Decal(0xC87C32, 4f, 4f, 0.32f).Ring(4f, 220, 0xFFE09A).Ring(4.8f, 300, 0xFFB65A).Embers(55, 0xFFD38B).Debris(18).Dust(24).HitStop(40).Quake(380, 0.82f).Flare(0xFFE4AF, 160).Zoom(0.085f, 300);
            c.Add(1450, 1600).Decal(0x9D6334, 4f, 3f, 0.22f).Ring(4.5f, 240, 0xE8A35D).Embers(24, 0xFFBF73).Dust(12).Quake(110, 0.12f).Trail();
            c.Add(1600, 1750).Tele(0xA36A3B, 4f, 0f).Decal(0x8D5930, 3f, 0f, 0.18f).Embers(10, 0xC9A17D).Trail();
            return c;
        }
    }
}
