using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Colour sets of the two Cleric ally buffs, RO3 Soft Fantasy V1 (2026-11-21, user: "làm giống bên RO3 luôn đi"): Spirit Blessing (+ATK) =
    /// the Holy palette (gold #F2D078, ivory #FFF1BC, core #FFFBE5), Divine Aegis (+DEF) = Protection (pale blue #9DDAEC, gold accent #FFF0B8),
    /// replacing the bold Midgard amber / cyan of 3248c37f. Shapes come from ClericLightLook.M; this keeps what the streams and swirls need:
    /// the light head and the ribbon.
    /// </summary>
    public static class ClericBuffStatusLook
    {
        /// <summary>One buff's set. Main = body colour, Light = highlight / particles, Deep = darker rim.</summary>
        public sealed class Set
        {
            public ClericLightLook.Pal P;
            public Color Main, Light, Deep;
            public Material Head, Trail;
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
            Atk = Make(ClericLightLook.Holy);
            Def = Make(ClericLightLook.Guard);
        }

        static Set Make(ClericLightLook.Pal p)
        {
            var s = new Set { P = p, Main = p.Main, Light = p.Light, Deep = p.Rim };
            s.Head = ClericLightLook.M(ClericLightLook.Core, p, ClericLightLook.Role.Flash);
            s.Trail = ClericLightLook.Trail(p);
            return s;
        }
    }
}
