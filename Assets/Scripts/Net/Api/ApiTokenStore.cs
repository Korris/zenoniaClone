using UnityEngine;

namespace ZenoniaProto.Api
{
    /// <summary>
    /// Remembers the refresh token on the device so the player only types the password on a new device (phase 3 decision).
    /// PlayerPrefs is plain text on disk: acceptable for a rotating, revocable refresh token; swap for the platform keychain later.
    /// The access token is never stored (15 min lifetime, re-minted by refresh).
    /// </summary>
    public static class ApiTokenStore
    {
        const string RefreshKeyBase = "zenonia.api.refresh";
        const string UserKeyBase = "zenonia.api.username";

        /// <summary>Test bots only (--test-token-store): the real store under keys of its own, so a test can check what logout leaves behind without touching the player's session.</summary>
        public static bool TestStore { get { return NetLaunchArgs.HasFlag("--test-token-store"); } }
        /// <summary>Test bots (--account) and several windows on one machine (--no-remember): PlayerPrefs is per machine, so they would log in as each other and overwrite the player's own session.</summary>
        public static bool Disabled { get { return !TestStore && (NetLaunchArgs.HasFlag("--account") || NetLaunchArgs.HasFlag("--no-remember")); } }

        static string RefreshKey { get { return TestStore ? RefreshKeyBase + ".test" : RefreshKeyBase; } }
        static string UserKey { get { return TestStore ? UserKeyBase + ".test" : UserKeyBase; } }

        public static string LoadRefreshToken() { return Disabled ? "" : PlayerPrefs.GetString(RefreshKey, ""); }
        public static string LoadUsername() { return Disabled ? "" : PlayerPrefs.GetString(UserKey, ""); }

        public static void Save(string username, string refreshToken)
        {
            if (Disabled) return;
            PlayerPrefs.SetString(UserKey, username ?? "");
            PlayerPrefs.SetString(RefreshKey, refreshToken ?? "");
            PlayerPrefs.Save();
        }

        public static void Clear()
        {
            if (Disabled) return;
            PlayerPrefs.DeleteKey(RefreshKey);
            PlayerPrefs.DeleteKey(UserKey);
            PlayerPrefs.Save();
        }
    }
}
