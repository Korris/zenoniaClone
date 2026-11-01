using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// A public release build: PlayerBuildScript with -zenoniaRelease sets the scripting define ZENONIA_RELEASE for that one build and bakes the https API address into Resources/Data/release-api.txt.
    /// In such a build there is no dev-start.json, no F10, no direct connect page, no capture or bot flags, and the login screen has no Server field. Test builds do not have the define and stay as they are.
    /// </summary>
    public static class ReleaseBuild
    {
        const string UrlResource = "Data/release-api";

#if ZENONIA_RELEASE
        public static bool IsRelease { get { return true; } }
#else
        public static bool IsRelease { get { return false; } }
#endif

        /// <summary>What the login screen calls the game. The window title comes from the product name, which the build script sets for a release.</summary>
        public static string LoginTitle { get { return IsRelease ? "Pocket Valor" : "Zenonia Online"; } }

        static string apiUrl;

        /// <summary>The baked address; null in a test build. The command line and PlayerPrefs have no say over it.</summary>
        public static string ApiUrl
        {
            get
            {
                if (!IsRelease) return null;
                if (apiUrl == null)
                {
                    TextAsset asset = Resources.Load<TextAsset>(UrlResource);
                    apiUrl = asset != null ? asset.text.Trim().TrimEnd('/') : "";
                    if (apiUrl.Length == 0) Debug.LogError("[ZenoniaProto] release build without a baked API address");
                }
                return apiUrl;
            }
        }
    }
}
