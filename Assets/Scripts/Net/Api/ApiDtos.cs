using System;
using ZenoniaProto.Rpg;

namespace ZenoniaProto.Api
{
    // JsonUtility DTOs for server-api (server-api/README.md). Field names are the wire names; do not rename.

    [Serializable] public class RegisterRequest { public string username, email, password; }
    [Serializable] public class LoginRequest { public string login, password; } // login = username or email
    [Serializable] public class RefreshRequest { public string refreshToken; }

    [Serializable] public class ApiUser { public string id, username, email; }

    /// <summary>Response of /auth/register, /auth/login and /auth/refresh.</summary>
    [Serializable]
    public class AuthResponse
    {
        public ApiUser user;
        public string accessToken, refreshToken;
        public int expiresIn; // access token lifetime, seconds
    }

    /// <summary>Row of GET /characters (no inventory).</summary>
    [Serializable]
    public class CharacterSummary
    {
        public string id, name, classId, mapId, createdAt;
        public int level, rev;
        public bool online;
    }

    [Serializable] public class CharacterListResponse { public CharacterSummary[] characters; }

    [Serializable] public class CreateCharacterRequest { public string name, classId; }

    /// <summary>Summary plus the full persisted state (SaveData shape, same JSON as the offline save).</summary>
    [Serializable]
    public class CharacterDetail
    {
        public string id, name, classId, mapId, createdAt;
        public int level, rev;
        public bool online;
        public SaveData state;
    }

    [Serializable] public class CharacterDetailResponse { public CharacterDetail character; }

    /// <summary>Response of POST /characters/:id/enter: what the zone Hello needs.</summary>
    [Serializable]
    public class EnterResponse
    {
        public string ticket, zoneId, zoneHost;
        public int zonePort, expiresIn; // ticket is single use and valid for expiresIn seconds (60)
    }

    /// <summary>Error envelope { "error": { "code", "message" } }.</summary>
    [Serializable] public class ApiErrorEnvelope { public ApiErrorInfo error; }
    [Serializable] public class ApiErrorInfo { public string code, message; }

    /// <summary>Stable error codes the UI may branch on (anything else: show the message).</summary>
    public static class ApiErrorCodes
    {
        public const string Network = "network";            // client side: no connection / timeout
        public const string NotLoggedIn = "not_logged_in";    // client side: no tokens
        public const string BadResponse = "bad_response";     // client side: unreadable body
        public const string InvalidCredentials = "invalid_credentials";
        public const string UsernameTaken = "username_taken";
        public const string EmailTaken = "email_taken";
        public const string ValidationError = "validation_error";
        public const string RateLimited = "rate_limited";     // also account lockout
        public const string NameTaken = "name_taken";
        public const string ClassTaken = "class_taken";
        public const string AlreadyOnline = "already_online";
        public const string InvalidRefreshToken = "invalid_refresh_token";
    }
}
