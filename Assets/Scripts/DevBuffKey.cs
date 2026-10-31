#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem;
#endif
using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// F10 in the editor and development builds, offline only: the hero gets the cleric's Spirit Blessing and Divine Aegis, so the buffed look can be seen without a second player. Temporary test key.
    /// </summary>
    public static class DevBuffKey
    {
        static bool Pressed()
        {
#if ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(KeyCode.F10);
#elif ENABLE_INPUT_SYSTEM
            Keyboard kb = Keyboard.current;
            return kb != null && kb[Key.F10].wasPressedThisFrame;
#else
            return false;
#endif
        }

        public static void Tick(HeroController hero)
        {
            if (!Application.isEditor && !Debug.isDebugBuild) return;
            if (NetMode.IsClient || !Pressed()) return;
            ClericBuffFx.DevGrantBoth(hero);
        }
    }
}
