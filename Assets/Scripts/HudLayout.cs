using System;
using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Which HUD the client shows. Mobile (Android / iOS, and the Windows dev exe by default): joystick, potion buttons, the attack disc
    /// with the skill arc, chat collapsed on the left above the joystick. Web (WebGL): no touch controls; bottom-centre hotbar
    /// (left mouse / Space attack, 1-2-3 skills, Q / E potions) with an EXP bar, chat panel bottom-left, Enter to type.
    /// Web / PC is the default on WebGL and on desktop (Windows / macOS / Linux exe and the editor; 21/11, the RO3 PC HUD profile of
    /// RoHud), mobile on phones and in the editor on a phone build target. The launch flag `--hud web|mobile` forces either (`--hud mobile` = the touch HUD on the desktop exe).
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
#if UNITY_ANDROID || UNITY_IOS
                web = false; // the editor on a phone build target previews the phone HUD
#else
                web = true;  // desktop: keyboard + mouse
#endif
                string[] args = Environment.GetCommandLineArgs();
                int i = Array.IndexOf(args, "--hud");
                if (i >= 0 && i + 1 < args.Length) web = !string.Equals(args[i + 1], "mobile", StringComparison.OrdinalIgnoreCase);
            }
            if (web) InputAdapter.UseWebBindings();
        }
    }
}
