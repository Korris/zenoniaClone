using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Colour sets of the two Cleric ally buffs, RO3 Soft Fantasy V1 (2026-11-21, user: "làm giống bên RO3 luôn đi"): Spirit Blessing (+ATK) =
    /// the Holy palette (gold #F2D078, ivory #FFF1BC, core #FFFBE5), Divine Aegis (+DEF) = Protection (pale blue #9DDAEC, gold accent #FFF0B8),
    /// replacing the bold Midgard amber / cyan of 3248c37f. Shapes come from ClericLightLook.M; this keeps what the streams, swirls and the
    /// maintained body rings need: the stream / swirl head, the ribbon, and the body ring material (BuffBodyRing shader) toned down for the
    /// maintained state (RO3 rule 10: 15-30 % of the cast's brightness; ClericBuffStatusFx scales it further).
    /// </summary>
    public static class ClericBuffStatusLook
    {
        /// <summary>One buff's set. Main = body colour, Light = highlight / particles, Deep = darker rim.</summary>
        public sealed class Set
        {
            public ClericLightLook.Pal P;
            public Color Main, Light, Deep;
            public Material Head, Trail, BodyRing;
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
            Shader ringShader = Resources.Load<Shader>("Shaders/BuffBodyRing");
            if (!ClericLightLook.Ready || ringShader == null || !ringShader.isSupported)
            {
                Debug.LogWarning("[ZenoniaProto] Cleric buff status FX: shader / texture missing, Spirit Blessing / Divine Aegis draw no status");
                failed = true;
                return;
            }
            Atk = Make(ClericLightLook.Holy, ringShader, new Color(0.3f, 0.24f, 0.08f, 1f));
            Def = Make(ClericLightLook.Guard, ringShader, new Color(0.1f, 0.24f, 0.3f, 1f));
        }

        static Set Make(ClericLightLook.Pal p, Shader ringShader, Color core)
        {
            var s = new Set { P = p, Main = p.Main, Light = p.Light, Deep = p.Rim };
            s.Head = ClericLightLook.M(ClericLightLook.Core, p, ClericLightLook.Role.Flash);
            s.Trail = ClericLightLook.Trail(p);
            // Maintained ring round the body: pastel body, slightly darker rim (snow), soft bright segments, a faint core line
            s.BodyRing = new Material(ringShader) { name = "ClericStatus_" + p.Name + "BodyRing" };
            s.BodyRing.SetColor("_BodyColor", ClericLightLook.A(p.Main, 0.7f));
            s.BodyRing.SetColor("_RimColor", ClericLightLook.A(p.Rim, 0.7f));
            s.BodyRing.SetColor("_HiColor", ClericLightLook.A(p.Light, 0.6f));
            s.BodyRing.SetColor("_CoreColor", core);
            s.BodyRing.SetFloat("_Segs", 2f);
            s.BodyRing.SetFloat("_SegLen", 0.3f);
            return s;
        }
    }
}
