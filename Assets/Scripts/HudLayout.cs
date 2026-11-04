using System;
using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Which HUD the client shows. Mobile (Android / iOS, and the Windows dev exe by default): joystick, potion buttons, the attack disc
    /// with the skill arc, chat collapsed on the left above the joystick. Web (WebGL): no touch controls; bottom-centre hotbar
    /// (left mouse / Space attack, 1-2-3 skills, Q / E potions) with an EXP bar, chat panel bottom-left, Enter to type.
    /// The dev exe can show either with the launch flag `--hud web|mobile` (screenshots, tests).
    /// </summary>
    public static class HudLayout
    {
        static bool resolved, web;

        public static bool IsWeb
        {
            get
            {
                if (!resolved) Resolve();
                return web;
            }
        }

        static void Resolve()
        {
            resolved = true;
            if (Application.platform == RuntimePlatform.WebGLPlayer) web = true;
            else if (Application.isMobilePlatform) web = false;
            else
            {
                string[] args = Environment.GetCommandLineArgs();
                int i = Array.IndexOf(args, "--hud");
                web = i >= 0 && i + 1 < args.Length && string.Equals(args[i + 1], "web", StringComparison.OrdinalIgnoreCase);
            }
            if (web) InputAdapter.UseWebBindings();
        }
    }
}
