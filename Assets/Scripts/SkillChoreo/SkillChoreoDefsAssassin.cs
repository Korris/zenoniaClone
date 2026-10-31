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
            into.Add(ShadowStrike()); into.Add(ShadowStep()); into.Add(BladeDance()); into.Add(PhantomRush());
            into.Add(VenomDaggers()); into.Add(ShadowClone()); into.Add(DeathMark()); into.Add(NightfallExecution());
        }

        static Choreo ShadowStrike()
        {
            var c = new Choreo("assassin", "shadow-strike", 420);
            c.Add(0, 65).Face().Spr(I, 0, 1, 2.6f).Alpha(0.85f).Glow(0x8C45D8, 0.40f).Embers(8, 0xA47BE6).Dust(2).Ring(0.7f, 65, 0x7946B5).Quake(45, 0.04f).Sound("impact");
            c.Add(65, 120).Face().Spr(C, 0, 2, 2.6f).Alpha(0.5f).Glow(0x6433A6, 0.22f).Embers(5, 0x67409D).Trail();
            c.Add(120, 205).Face().Spr(I, 2, 3, 2.8f).Glow(0xBD73FF, 0.55f).Embers(12, 0xC69AFF).Dust(3).Ring(1.2f, 100, 0xA65CE8).HitStop(30).Quake(95, 0.16f).Flare(0xA976E6, 40).Zoom(0.018f, 90);
            c.Add(205, 300).Anchor(Cas).Spr(L, 0, 2, 2.5f).Alpha(0.35f).Glow(0x6E3B9B, 0.15f).Embers(5, 0x74509C).Trail();
            c.Add(300, 420).Face().Spr(I, 3, 3, 2.6f).Alpha(0.12f).Glow(0x5A347C, 0.06f).Embers(3, 0x8655B3);
            return c;
        }

        static Choreo ShadowStep()
        {
            var c = new Choreo("assassin", "shadow-step", 430);
            c.Add(0, 80).Anchor(Cas).Spr(C, 0, 3, 2f).Alpha(0.65f).Glow(0x6734A0, 0.30f).Decal(0x321B4C, 0.4f, 0.9f, 0.45f).Embers(8, 0x8855BD).Ring(0.8f, 90, 0x6A3BA3).Dust(2).Sound("cast");
            c.Add(0, 80).Spr(L, 0, 1, 1.8f).Alpha(0.5f).Glow(0x8750BD, 0.25f).Decal(0x28173F, 0.3f, 0.8f).Embers(7, 0x70439B).Trail();
            c.Add(80, 175).Face().Spr(I, 0, 2, 2.6f).Glow(0xC185FF, 0.65f).Embers(14, 0xD4ADFF).Ring(1f, 100, 0xA05BE3).HitStop(45).Quake(120, 0.24f).Flare(0xA46AE0, 55).Zoom(0.025f, 120).Sound("impact");
            c.Add(175, 260).Spr(I, 2, 3, 2.3f).Alpha(0.45f).Glow(0x67358E, 0.18f).Embers(8, 0x69438C).Dust(2).Trail();
            c.Add(260, 430).Spr(I, 3, 3, 1.8f).Alpha(0.1f).Glow(0x46265E, 0.05f).Embers(3, 0x7A54A0);
            return c;
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

        static Choreo PhantomRush()
        {
            var c = new Choreo("assassin", "phantom-rush", 850);
            c.Add(0, 180).Anchor(Cas).Spr(L, 0, 3, 2f).Alpha(0.7f).Glow(0x7542B2, 0.32f).Decal(0x342048, 0.3f, 0.7f, 0.25f).Embers(12, 0x8A5CC2).Dust(3).Trail();
            c.Add(180, 245).Anchor(Cas).Spr(I, 0, 1, 2.5f).Alpha(0.85f).Glow(0xA96BE8, 0.45f).Embers(9, 0xBB90F0).Ring(0.8f, 80, 0x8E58C9).Quake(55, 0.05f);
            c.Add(180, 360).Anchor(Cas).Spr(L, 0, 3, 2f).Alpha(0.75f).Glow(0x8550C0, 0.38f).Embers(14, 0x9B6DD0).Dust(3).Trail();
            c.Add(360, 425).Anchor(Cas).Spr(I, 1, 2, 2.6f).Alpha(0.9f).Glow(0xB57AF0, 0.50f).Embers(11, 0xCAA0F5).Ring(0.9f, 90, 0xA26ADD).Quake(65, 0.07f);
            c.Add(360, 540).Anchor(Cas).Spr(L, 0, 3, 2.2f).Alpha(0.82f).Glow(0x9B5BD8, 0.45f).Embers(16, 0xB07DE5).Dust(4).Trail();
            c.Add(540, 660).Anchor(Cas).Spr(I, 0, 3, 3f).Glow(0xD39BFF, 0.70f).Embers(22, 0xDBB5FF).Dust(6).Ring(1.4f, 140, 0xB87AEC).HitStop(40).Quake(160, 0.32f).Flare(0xB78AEE, 75).Zoom(0.04f, 150).Sound("impact");
            c.Add(660, 850).Anchor(Cas).Spr(I, 3, 3, 2.8f).Alpha(0.18f).Glow(0x69418C, 0.10f).Embers(8, 0x8255A8).Trail();
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

        static Choreo NightfallExecution()
        {
            var c = new Choreo("assassin", "nightfall-execution", 1380);
            c.Add(0, 75).Anchor(Cas).Spr(L, 0, 1, 5.5f).Alpha(0.55f).Glow(0x7136A8, 0.30f).Decal(0x21152E, 0f, 2f, 0.35f).Embers(10, 0x8B5CC0).Quake(50, 0.03f).Sound("cast");
            c.Add(75, 120).Anchor(Cas).Spr(C, 0, 1, 5.5f).Alpha(0.35f).Glow(0x643197, 0.18f).Embers(7, 0x74509C).Trail();
            c.Add(120, 195).Anchor(Cas).Spr(L, 1, 2, 5.7f).Alpha(0.62f).Glow(0x8744BE, 0.35f).Embers(11, 0xA275D0).Ring(1.5f, 100, 0x70409E).Quake(55, 0.04f);
            c.Add(195, 240).Anchor(Cas).Spr(C, 1, 2, 5.7f).Alpha(0.35f).Glow(0x6C349C, 0.20f).Embers(8, 0x8456B0).Trail();
            c.Add(240, 315).Anchor(Cas).Spr(L, 2, 3, 5.8f).Alpha(0.68f).Glow(0x9852CE, 0.40f).Embers(12, 0xB183DC).Ring(1.8f, 110, 0x824DB0).Quake(60, 0.05f);
            c.Add(315, 360).Anchor(Cas).Spr(C, 2, 3, 5.8f).Alpha(0.38f).Glow(0x7136A0, 0.22f).Embers(8, 0x9260BB).Trail();
            c.Add(360, 435).Anchor(Cas).Spr(L, 0, 2, 6f).Alpha(0.72f).Glow(0xA15AD8, 0.43f).Embers(14, 0xB990E5).Ring(2f, 110, 0x9255C4).Quake(65, 0.06f);
            c.Add(435, 480).Anchor(Cas).Spr(C, 0, 1, 6f).Alpha(0.4f).Glow(0x7B3BA8, 0.24f).Embers(9, 0x9A68C5).Trail();
            c.Add(480, 555).Anchor(Cas).Spr(L, 1, 3, 6f).Alpha(0.76f).Glow(0xAE68E2, 0.46f).Embers(15, 0xC39BED).Ring(2.2f, 120, 0x9D60D0).Quake(70, 0.07f);
            c.Add(555, 600).Anchor(Cas).Spr(C, 1, 2, 6f).Alpha(0.42f).Glow(0x8440B3, 0.25f).Embers(10, 0xA474D0).Trail();
            c.Add(600, 675).Anchor(Cas).Spr(L, 0, 3, 6f).Alpha(0.8f).Glow(0xB774EB, 0.50f).Embers(16, 0xCCA5F2).Ring(2.4f, 120, 0xAB6BDD).Quake(75, 0.08f);
            c.Add(675, 720).Anchor(Cas).Spr(C, 2, 3, 6f).Alpha(0.45f).Glow(0x9047BD, 0.28f).Embers(12, 0xB180DC).Trail();
            c.Add(720, 795).Anchor(Cas).Spr(L, 1, 3, 6f).Alpha(0.88f).Glow(0xCA89F5, 0.55f).Embers(20, 0xDAB8FA).Ring(2.7f, 130, 0xBA7BEA).Quake(80, 0.08f);
            c.Add(795, 840).Anchor(Cas).Spr(C, 3, 3, 5.5f).Alpha(0.48f).Glow(0xA05CCB, 0.35f).Tele(0x8C45C0, 1.5f, 3f).Embers(16, 0xC293E9).Trail();
            c.Add(840, 1020).Anchor(Cas).Spr(I, 0, 2, 6.2f).Glow(0xDDA9FF, 0.85f).Decal(0x482160, 1.5f, 3f).Embers(48, 0xD6A5FF).Debris(8).Dust(10).Ring(3f, 190, 0xC185F5).Ring(3.6f, 260, 0x8E4AC8).HitStop(60).Quake(360, 0.75f).Flare(0xC7A1F0, 140).Zoom(0.085f, 290).Sound("impact");
            c.Add(1020, 1190).Anchor(Cas).Spr(I, 2, 3, 6f).Alpha(0.55f).Glow(0x9D55CF, 0.35f).Decal(0x351C4A, 3f, 2f, 0.28f).Embers(22, 0xB984E3).Dust(8).Ring(3.2f, 170, 0x8145B0).Quake(110, 0.10f);
            c.Add(1190, 1380).Anchor(Cas).Spr(I, 3, 3, 5.5f).Alpha(0.12f).Glow(0x65358A, 0.10f).Decal(0x241631, 2f, 0f, 0.18f).Embers(12, 0x8052A4).Trail();
            return c;
        }
    }
}
