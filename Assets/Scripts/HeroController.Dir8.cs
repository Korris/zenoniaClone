using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// View-only 8-way facing for the RO Soft sheets (HeroRenderer.Soft.cs). The sim, hit boxes, RangedAimRules and the network keep `facing` (4-dir).
    /// Own / AI hero: the held move input while walking (the dash direction while dashing). Proxy: the interpolated motion while Moving / Dash.
    /// Otherwise the last value is kept; it snaps to the facing's cardinal when the facing turns while standing, and whenever it stops agreeing with
    /// the facing (an attack keeps a diagonal only if one of its axes is the attack's facing).
    /// </summary>
    public partial class HeroController
    {
        const float ProxyDir8MinTravel = 1.5f; // world units of interpolated motion before a proxy's direction is read (snapshot jitter stays out)
        int dir8 = -1, dir8Facing = -1;
        Vector2 dir8LastPos, dir8Travel;
        bool dir8HasPos;

        /// <summary>Current view direction (Dir8Util row) of the soft sheets; follows the 4-dir facing on the classic renderer.</summary>
        public int ViewDir8 { get { return dir8 >= 0 ? dir8 : Dir8Util.FromCardinal(facing); } }

        /// <summary>Debug / screenshot helper: face one of the 8 view directions (the sim facing becomes one of its axes).</summary>
        public void FaceDir8(int d)
        {
            if (d < 0 || d > 7) return;
            if (!Dir8Util.Matches(d, facing)) facing = d == 0 || d == 1 || d == 7 ? 0 : d == 2 ? 3 : d == 6 ? 2 : 1;
            dir8 = d;
            dir8Facing = facing;
        }

        void UpdateDir8()
        {
            bool moved = !dead && (moving || dashLeft > 0f);
            int want = -1;
            if (!proxy)
            {
                if (moved) want = Dir8Util.FromVector(dashLeft > 0f ? dashDir : ReadMove());
            }
            else
            {
                var pos = new Vector2(x, y);
                if (moved && dir8HasPos)
                {
                    dir8Travel += pos - dir8LastPos;
                    if (dir8Travel.magnitude >= ProxyDir8MinTravel) { want = Dir8Util.FromVector(dir8Travel); dir8Travel = Vector2.zero; }
                }
                else dir8Travel = Vector2.zero;
                dir8LastPos = pos;
                dir8HasPos = true;
            }
            if (want >= 0) dir8 = want;
            else if (facing != dir8Facing && !moved) dir8 = Dir8Util.FromCardinal(facing); // turned on the spot (attack, teleport, server facing): the cardinal
            if (dir8 < 0 || !Dir8Util.Matches(dir8, facing)) dir8 = Dir8Util.FromCardinal(facing);
            dir8Facing = facing;
        }
    }
}
