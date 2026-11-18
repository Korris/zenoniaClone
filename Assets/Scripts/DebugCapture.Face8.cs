using System;
using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// RO Soft 8-direction captures (HeroRenderer.Soft.cs), with --class:
    ///   --face8 down-right                 stand facing one of the 8 view directions (Dir8Util.Names; the sim facing becomes one of its axes)
    ///   --bot walk8 up-left                walk along one of the 8 directions (holds one or two arrow keys)
    /// Compare with the paper doll: add --hero-style classic.
    /// </summary>
    public partial class DebugCapture
    {
        void ApplyFace8Flags(HeroController hero)
        {
            string[] a = Environment.GetCommandLineArgs();
            int face = Dir8Util.Parse(Arg(a, "--face8"));
            if (face >= 0) hero.FaceDir8(face);
            if (botArg != "walk8") return;
            int walk = Dir8Util.Parse(Arg(a, "walk8"));
            if (walk < 0) { Debug.LogWarning("[ZenoniaProto] --bot walk8 needs a direction, e.g. --bot walk8 down-right"); return; }
            // Rows: 0 down, 1 down-right, 2 right, 3 up-right, 4 up, 5 up-left, 6 left, 7 down-left.
            bool down = walk == 0 || walk == 1 || walk == 7, up = walk >= 3 && walk <= 5;
            bool right = walk >= 1 && walk <= 3, left = walk >= 5 && walk <= 7;
            InputAdapter.BotHeld = k => (k == GameKey.Down && down) || (k == GameKey.Up && up) || (k == GameKey.Right && right) || (k == GameKey.Left && left);
            Debug.Log("[ZenoniaProto] walk8 " + Dir8Util.Names[walk]);
        }
    }
}
