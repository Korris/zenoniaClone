using UnityEngine;
using L = ZenoniaProto.ClericLightLook;

namespace ZenoniaProto
{
    /// <summary>
    /// Light FX of the dungeon chests and warp crystals (Rootburrow V5): shader quads + particles on the Cleric light toolkit (ClericLightFx), no sprite frames
    /// (docs/sprite-rules.md, VFX rules). The chest's lid opening flashes warm gold out of the chest (rare: gold + pink, bigger) and lights up the dark alcove
    /// (DungeonLights.Flash); attuning a crystal rings cyan; a warp draws a rune circle under the hero during the 3 s channel and ends in a light column.
    /// </summary>
    public static class DungeonPointFx
    {
        static readonly L.Pal Gold = new L.Pal("ChestGold", 0xF6C76A, 0xFFE7A6, 0xFFF8DE, 0xA8722E, new Color(0.18f, 0.12f, 0.03f));
        static readonly L.Pal Rose = new L.Pal("ChestRose", 0xF2A6DA, 0xFFD9F1, 0xFFF4FB, 0xA0598C, new Color(0.16f, 0.06f, 0.13f));
        static readonly L.Pal Warp = new L.Pal("WarpCyan", 0x7FE3FF, 0xC9F5FF, 0xF2FDFF, 0x3E8FB0, new Color(0.04f, 0.12f, 0.16f));
        const float Round = 1f; // the 2.5D camera foreshortens the ground itself

        static bool Ok { get { return NetMode.RendersFx && FeelConfig.FxOn && L.Drawable && LightSkillAssets.Ready; } }

        /// <summary>Lid flies open: a flash core out of the chest, a short light pillar, a ground ring, sparkles rising like coins.</summary>
        public static void ChestOpened(Vector2 foot, bool rare)
        {
            DungeonLights.Flash(foot + new Vector2(0f, 12f), rare ? new Color(1f, 0.8f, 0.9f) : new Color(1f, 0.85f, 0.5f), (rare ? 5f : 4f) * GameConfig.TileUnits, rare ? 1.5f : 1.25f, 650f);
            if (!Ok) return;
            L.Pal p = rare ? Rose : Gold;
            float k = rare ? 1.3f : 1f;
            Vector2 lid = new Vector2(0f, 14f);
            ClericLightFx fx = ClericLightFx.Create("ChestOpenFx", foot, null, 1100f, 1f);
            fx.Upright(L.M(L.Core, p, L.Role.Flash), ClericLightFx.Band.Over, false, 0f, 240f, 10f * k, 42f * k, 10f * k, 42f * k, 0.95f, lid);
            fx.Upright(L.M(L.Core, Gold, L.Role.Fill), ClericLightFx.Band.Over, true, 40f, 700f, 12f * k, 20f * k, 8f, 52f * k, 0.55f, new Vector2(0f, 8f)).Env(90f, 380f).Kind(LayerKind.Glow);
            fx.Ground(L.M(L.Band, p, L.Role.Line), 0f, 520f, 10f, 46f * k, 0.9f, Round).Phases(0f, 140f, 1f, 0.45f);
            fx.Ground(L.M(L.Band, Gold, L.Role.Fill), 0f, 600f, 16f, 40f * k, 0.5f, Round).Kind(LayerKind.Glow);
            fx.At(30f, f => f.Puff(true, foot + lid, rare ? 22 : 14, 9f, 30f, 1.8f, 3.4f, L.A(Gold.Light, 0.95f), 0.95f));
            if (rare) fx.At(120f, f => f.Ring(true, foot + new Vector2(0f, 6f), 14, 55f, 2f, 3.4f, L.A(Rose.Light, 0.95f), 0.7f));
        }

        /// <summary>First touch of a crystal: a rune ring draws in under it and bursts, cyan sparks spiral up.</summary>
        public static void CrystalAttuned(Vector2 foot)
        {
            DungeonLights.Flash(foot + new Vector2(0f, 20f), new Color(0.55f, 0.92f, 1f), 5f * GameConfig.TileUnits, 1.4f, 700f);
            if (!Ok) return;
            ClericLightFx fx = ClericLightFx.Create("CrystalAttuneFx", foot, null, 1300f, 1f);
            fx.Ground(L.M(L.RuneRing, Warp, L.Role.Arc), 0f, 900f, 20f, 52f, 0.9f, Round, 60f).Reveal(380f).Env(60f, 300f);
            fx.Ground(L.M(L.Band, Warp, L.Role.Line), 300f, 900f, 14f, 70f, 0.85f, Round).Phases(0f, 160f, 1f, 0.4f);
            fx.Upright(L.M(L.Core, Warp, L.Role.Flash), ClericLightFx.Band.Over, false, 280f, 560f, 14f, 50f, 14f, 50f, 0.9f, new Vector2(0f, 26f));
            fx.At(300f, f => f.Ring(true, foot + new Vector2(0f, 4f), 18, 60f, 2.2f, 3.8f, L.A(Warp.Light, 0.95f), 0.8f));
            fx.At(340f, f => f.Puff(true, foot + new Vector2(0f, 20f), 14, 10f, 34f, 1.8f, 3f, L.A(Warp.Main, 0.9f), 1f));
        }

        /// <summary>The 3 s warp channel under the hero (follows it): a rune circle draws itself while light gathers. Destroy the returned FX to cancel.</summary>
        public static ClericLightFx WarpChannel(HeroController hero, float ms)
        {
            if (!Ok || hero == null) return null;
            ClericLightFx fx = ClericLightFx.Create("WarpChannelFx", hero.Position, hero, ms + 200f, 1f);
            fx.Ground(L.M(L.RuneRing, Warp, L.Role.Arc), 0f, ms + 150f, 30f, 34f, 0.85f, Round, 45f).Reveal(ms * 0.8f).Arc(2.2f, 0f, 2).Env(120f, 150f);
            fx.Ground(L.M(L.Band, Warp, L.Role.Fill), ms * 0.4f, ms + 150f, 14f, 30f, 0.5f, Round).Env(ms * 0.5f, 120f).Kind(LayerKind.Glow);
            for (int i = 0; i < 6; i++) fx.At(ms * i / 6f, f => f.Puff(true, f.Anchor + new Vector2(0f, 2f), 4, 12f, 26f, 1.6f, 2.6f, L.A(Warp.Light, 0.9f), 0.8f));
            return fx;
        }

        /// <summary>The moment of the warp: a light column on the hero (the screen fades right after).</summary>
        public static void WarpFlash(Vector2 at)
        {
            DungeonLights.Flash(at + new Vector2(0f, 16f), new Color(0.6f, 0.95f, 1f), 5f * GameConfig.TileUnits, 1.6f, 500f);
            if (!Ok) return;
            ClericLightFx fx = ClericLightFx.Create("WarpFlashFx", at, null, 700f, 1f);
            fx.Upright(L.M(L.Core, Warp, L.Role.Fill), ClericLightFx.Band.Over, true, 0f, 520f, 18f, 30f, 20f, 90f, 0.8f, Vector2.zero).Env(60f, 300f).Kind(LayerKind.Glow);
            fx.Upright(L.M(L.Core, Warp, L.Role.Flash), ClericLightFx.Band.Over, false, 0f, 260f, 16f, 56f, 16f, 56f, 0.95f, new Vector2(0f, 14f));
        }
    }
}
