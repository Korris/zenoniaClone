using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Warrior lock-target basic attack picture, RO3 Soft Fantasy V1 (2026-11-21; replaces the painted crescent WarriorLockSlashFx /
    /// fx/attack/lock-slash.png): one curved Mesh Trail (ArcRibbonFx, Blade palette) sweeping round the chest through the contact point on the
    /// locked monster, in the sword's swing direction, + a 4-point hit spark and 3 sparks where the blade meets the body. Plays at the combo Hit event
    /// (the blow, ComboConfigs.WarriorHits: windup 120 ms), so it is timed on the swing by construction: sweep 90 ms = 120-210 ms, the 8-frame
    /// attack art's f2 (blade over the top) -> f3 (at the target) -> start of f4 (low across), tail gone 150 ms later (rule 9). Ribbon 0.2 H wide over a soft underlay (WarriorLightSkills.Slash); smaller than every skill (basic attack). Up-screen slashes draw behind the
    /// hero like his sword. Crowd LOD from ClericLightFx (another player's: ribbon dimmer / thinner, no sparks).
    /// </summary>
    public static class WarriorSlashLightFx
    {
        const float SweepMs = 70f, TailMs = 150f, Span = 62f, Squash = 0.62f, Width = 5.6f, ChestY = 10f; // grand pass 21/11: 0.28 H body (basic attack stays under every skill)

        /// <summary>`angleDeg` = toward the contact point (0 right, 90 up); `dist` = chest to it (0 = no target).</summary>
        public static void Spawn(HeroController owner, Vector2 feet, float angleDeg, float dist, float figureScale)
        {
            if (!WarriorLightLook.Ready) return;
            float k = Mathf.Max(0.5f, figureScale);
            ClericLightLook.Pal p = WarriorLightLook.Blade;
            var fx = ClericLightFx.Create("WarSlash", feet, null, SweepMs + TailMs + 60f, ClericLightFx.DimFor(owner, null));
            Vector2 chest = new Vector2(0f, ChestY * k);
            float mid = WarriorLightLook.ParamDeg(angleDeg, Squash), a0 = mid + Span, a1 = mid - Span;
            float reach = (dist > 0f ? Mathf.Clamp(dist + 3f, 11f, 28f) : 16f) * k;      // the arc's middle lands just past the contact point
            float norm = new Vector2(Mathf.Cos(mid * Mathf.Deg2Rad), Squash * Mathf.Sin(mid * Mathf.Deg2Rad)).magnitude;
            float rx = Mathf.Min(reach / Mathf.Max(0.3f, norm), 34f * k);
            // Sweep the way the painted sword swings (8-dir heavy slash art 22/11, Tools/ro_fantasy_attack8_grips.py SWORD): facing right / down-right /
            // up-right the blade goes clockwise on screen (over the top, down to the target); facing down, up and every left dir it goes counter-clockwise
            // (down: left shoulder -> toward the camera -> low right; up: high right -> over -> low left).
            if (Mathf.Cos(angleDeg * Mathf.Deg2Rad) <= 0.3f) { float s = a0; a0 = a1; a1 = s; }
            bool up = Mathf.Sin(angleDeg * Mathf.Deg2Rad) > 0.35f;
            ArcRibbonFx.PathFn path = ArcRibbonFx.Arc(chest, rx, Squash, a0, a1);
            WarriorLightSkills.Slash(fx, path, 0f, SweepMs, TailMs, Width * k, p, 0.95f, up ? RibbonSort.Behind : RibbonSort.Over, 0, owner);
            Vector3 hp = path(0.5f); // 2.5D / 3D maps: ground point (x, y) + height (z) of the arc's middle
            Vector2 hit = new Vector2(hp.x, hp.y + hp.z);
            fx.Upright(WarriorLightLook.Spark(p), ClericLightFx.Band.Over, false, 35f, 180f, 6f * k, 13f * k, 6f * k, 13f * k, 0.95f, hit, 2).Floor(hp.z);
            fx.At(40f, f => { if (f.Own) f.Ring(true, f.Anchor + hit, 3, 30f, 1.4f, 2.2f, p.Light, 0.22f); });
        }
    }
}
