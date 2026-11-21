using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Colour sets of the two Cleric ally buffs, RO3 Soft Fantasy V1 (2026-11-21, user: "làm giống bên RO3 luôn đi"): Spirit Blessing (+ATK) =
    /// the Holy palette (gold #F2D078, ivory #FFF1BC, core #FFFBE5), Divine Aegis (+DEF) = Protection (pale blue #9DDAEC, gold accent #FFF0B8),
    /// replacing the bold Midgard amber / cyan of 3248c37f. Shapes come from ClericLightLook.M; this keeps what the streams and swirls need:
    /// the light head and the ribbon, plus the maintained ring round the body (BodyRing, shader BuffBodyRing; user 21/11 10:25: a ring round the
    /// body instead of the head emblem, clearly visible): ATK body #F3D16B / segments #FFF4C9, DEF body #9DDAEC / gold-accent segments #FFF0B8,
    /// each with a thin dark rim of its own hue (#9C7424 / #3F7F96) so it reads on snow and on grass, body alpha ~0.7.
    /// </summary>
    public static class ClericBuffStatusLook
    {
        /// <summary>One buff's set. Main = body colour, Light = highlight / particles, Deep = darker rim.</summary>
        public sealed class Set
        {
            public ClericLightLook.Pal P;
            public Color Main, Light, Deep;
            public Material Head, Trail;
            /// <summary>Maintained ring round the body (null when the BuffBodyRing shader is missing: then no ring, the rest still draws).</summary>
            public Material BodyRing;
        }

        public static Set Atk, Def;
        static bool built, failed;

        public static bool Ready { get { Build(); return !failed; } }

        /// <summary>Builds the materials ahead of the first cast (class spawn) so the cast itself never hitches.</summary>
        public static void Load() { if (NetMode.RendersFx) Build(); }

        static void Build()
        {
            if (built) return;
            built = true;
            if (!ClericLightLook.Ready)
            {
                Debug.LogWarning("[ZenoniaProto] Cleric buff status FX: shader / texture missing, Spirit Blessing / Divine Aegis draw no status");
                failed = true;
                return;
            }
            Shader ring = Resources.Load<Shader>("Shaders/BuffBodyRing");
            if (ring == null || !ring.isSupported) { Debug.LogWarning("[ZenoniaProto] Cleric buff status FX: BuffBodyRing shader missing, no body rings"); ring = null; }
            Atk = Make(ClericLightLook.Holy);
            Def = Make(ClericLightLook.Guard);
            // Additive core kept low and narrow (rule 5): gold keeps B low, blue keeps R low, so a pale road does not burn them white
            Atk.BodyRing = Ring(ring, "Atk", 0xF3D16B, 0.74f, 0x9C7424, 0xFFF4C9, new Color(0.5f, 0.4f, 0.16f, 1f));
            Def.BodyRing = Ring(ring, "Def", 0x9DDAEC, 0.7f, 0x3F7F96, 0xFFF0B8, new Color(0.18f, 0.36f, 0.42f, 1f));
        }

        static Set Make(ClericLightLook.Pal p)
        {
            var s = new Set { P = p, Main = p.Main, Light = p.Light, Deep = p.Rim };
            s.Head = ClericLightLook.M(ClericLightLook.Core, p, ClericLightLook.Role.Flash);
            s.Trail = ClericLightLook.Trail(p);
            return s;
        }

        static Color Hex(int rgb, float a) { return new Color(((rgb >> 16) & 255) / 255f, ((rgb >> 8) & 255) / 255f, (rgb & 255) / 255f, a); }

        /// <summary>Ring material: solid body (alpha `bodyA`), dark same-hue rim 0.85, two bright segments (30 % of the ring) of `hi`, thin core.</summary>
        static Material Ring(Shader sh, string n, int body, float bodyA, int rim, int hi, Color core)
        {
            if (sh == null) return null;
            var m = new Material(sh) { name = "ClericStatus_" + n + "BodyRing" };
            m.SetColor("_BodyColor", Hex(body, bodyA));
            m.SetColor("_RimColor", Hex(rim, 0.85f));
            m.SetColor("_HiColor", Hex(hi, 0.95f));
            m.SetColor("_CoreColor", core);
            m.SetFloat("_Segs", 2f);
            m.SetFloat("_SegLen", 0.3f);
            return m;
        }
    }
}
