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
        const string RefreshKey = "zenonia.api.refresh";
        const string UserKey = "zenonia.api.username";

        public static string LoadRefreshToken() { return PlayerPrefs.GetString(RefreshKey, ""); }
        public static string LoadUsername() { return PlayerPrefs.GetString(UserKey, ""); }

        public static void Save(string username, string refreshToken)
        {
            PlayerPrefs.SetString(UserKey, username ?? "");
            PlayerPrefs.SetString(RefreshKey, refreshToken ?? "");
            PlayerPrefs.Save();
        }

        public static void Clear()
        {
            PlayerPrefs.DeleteKey(RefreshKey);
            PlayerPrefs.DeleteKey(UserKey);
            PlayerPrefs.Save();
        }
    }
}
