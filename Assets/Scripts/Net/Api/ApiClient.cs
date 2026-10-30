using System;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace ZenoniaProto.Api
{
    /// <summary>
    /// Thin client for server-api (account + character endpoints used by the game client). Plain class, main-thread only, await-friendly:
    /// <c>var r = await api.LoginAsync("name", "pw"); if (!r.Ok) show(r.Message);</c>
    /// Keeps the access / refresh tokens, retries a call once after an automatic refresh when the access token has expired.
    /// Use https:// base URLs outside local development (passwords travel in the request body).
    /// </summary>
    public sealed class ApiClient
    {
        const int TimeoutSeconds = 15;

        readonly string baseUrl;
        Task<ApiResult<AuthResponse>> refreshInFlight;

        public string AccessToken { get; private set; }
        public string RefreshToken { get; private set; }
        public ApiUser User { get; private set; }
        public bool IsLoggedIn { get { return !string.IsNullOrEmpty(AccessToken); } }

        public ApiClient(string baseUrl) { this.baseUrl = (baseUrl ?? "").TrimEnd('/'); }

        // ---- auth ----

        public async Task<ApiResult<AuthResponse>> RegisterAsync(string username, string email, string password)
        {
            return Adopt(await SendAsync<AuthResponse>("POST", "/auth/register", JsonUtility.ToJson(new RegisterRequest { username = username, email = email, password = password }), false));
        }

        public async Task<ApiResult<AuthResponse>> LoginAsync(string usernameOrEmail, string password)
        {
            return Adopt(await SendAsync<AuthResponse>("POST", "/auth/login", JsonUtility.ToJson(new LoginRequest { login = usernameOrEmail, password = password }), false));
        }

        /// <summary>Silent login with the refresh token remembered on the device (see ApiTokenStore). Fails with not_logged_in when none.</summary>
        public Task<ApiResult<AuthResponse>> ResumeAsync()
        {
            RefreshToken = ApiTokenStore.LoadRefreshToken();
            return RefreshAsync();
        }

        /// <summary>Rotates the refresh token and mints a new access token. Concurrent callers share one request.</summary>
        public Task<ApiResult<AuthResponse>> RefreshAsync()
        {
            if (string.IsNullOrEmpty(RefreshToken)) return Task.FromResult(ApiResult<AuthResponse>.Fail(0, ApiErrorCodes.NotLoggedIn, "No saved session"));
            if (refreshInFlight == null) refreshInFlight = DoRefreshAsync();
            return refreshInFlight;
        }

        async Task<ApiResult<AuthResponse>> DoRefreshAsync()
        {
            try
            {
                var r = await SendAsync<AuthResponse>("POST", "/auth/refresh", JsonUtility.ToJson(new RefreshRequest { refreshToken = RefreshToken }), false);
                if (!r.Ok && r.Code == ApiErrorCodes.InvalidRefreshToken) ClearSession(); // revoked / expired: the player must log in again
                return Adopt(r);
            }
            finally { refreshInFlight = null; }
        }

        /// <summary>Revokes the refresh token on the server (best effort) and forgets the session on this device.</summary>
        public async Task LogoutAsync()
        {
            string token = RefreshToken;
            ClearSession();
            if (!string.IsNullOrEmpty(token)) await SendAsync<ApiOk>("POST", "/auth/logout", JsonUtility.ToJson(new RefreshRequest { refreshToken = token }), false);
        }

        // ---- characters ----

        public async Task<ApiResult<CharacterListResponse>> ListCharactersAsync()
        {
            return await SendAsync<CharacterListResponse>("GET", "/characters", null, true);
        }

        public async Task<ApiResult<CharacterDetailResponse>> CreateCharacterAsync(string name, string classId)
        {
            return await SendAsync<CharacterDetailResponse>("POST", "/characters", JsonUtility.ToJson(new CreateCharacterRequest { name = name, classId = classId }), true);
        }

        /// <summary>Asks for a single-use 60 s zone ticket + the zone address; the client then sends the ticket in its zone Hello.</summary>
        public async Task<ApiResult<EnterResponse>> EnterAsync(string characterId)
        {
            return await SendAsync<EnterResponse>("POST", "/characters/" + UnityWebRequest.EscapeURL(characterId) + "/enter", "{}", true);
        }

        // ---- plumbing ----

        [Serializable] class ApiOk { public bool ok = true; }

        void ClearSession()
        {
            AccessToken = null; RefreshToken = null; User = null;
            ApiTokenStore.Clear();
        }

        ApiResult<AuthResponse> Adopt(ApiResult<AuthResponse> r)
        {
            if (!r.Ok || r.Data == null) return r;
            AccessToken = r.Data.accessToken; RefreshToken = r.Data.refreshToken; User = r.Data.user;
            ApiTokenStore.Save(User != null ? User.username : "", RefreshToken);
            return r;
        }

        async Task<ApiResult<T>> SendAsync<T>(string method, string path, string jsonBody, bool authed) where T : class
        {
            if (authed && !IsLoggedIn && !string.IsNullOrEmpty(RefreshToken)) await RefreshAsync();
            if (authed && !IsLoggedIn) return ApiResult<T>.Fail(0, ApiErrorCodes.NotLoggedIn, "Not logged in");

            ApiResult<T> r = await SendOnceAsync<T>(method, path, jsonBody, authed);
            if (authed && !r.Ok && r.Status == 401 && (r.Code == "token_expired" || r.Code == "invalid_token") && !string.IsNullOrEmpty(RefreshToken))
            {
                ApiResult<AuthResponse> refreshed = await RefreshAsync();
                if (!refreshed.Ok) return ApiResult<T>.From(refreshed);
                r = await SendOnceAsync<T>(method, path, jsonBody, true); // retry once with the new token
            }
            return r;
        }

        async Task<ApiResult<T>> SendOnceAsync<T>(string method, string path, string jsonBody, bool authed) where T : class
        {
            using (var req = new UnityWebRequest(baseUrl + path, method))
            {
                req.timeout = TimeoutSeconds;
                req.downloadHandler = new DownloadHandlerBuffer();
                if (jsonBody != null)
                {
                    req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(jsonBody));
                    req.SetRequestHeader("Content-Type", "application/json");
                }
                if (authed) req.SetRequestHeader("Authorization", "Bearer " + AccessToken);

                UnityWebRequestAsyncOperation op = req.SendWebRequest();
                while (!op.isDone) await Task.Yield();

                int status = (int)req.responseCode;
                string text = req.downloadHandler != null ? req.downloadHandler.text : "";
                if (req.result == UnityWebRequest.Result.ConnectionError)
                    return ApiResult<T>.Fail(0, ApiErrorCodes.Network, "Cannot reach the server: " + req.error);
                if (status >= 200 && status < 300) return Parse<T>(text, status);
                return ParseError<T>(text, status, req.error);
            }
        }

        static ApiResult<T> Parse<T>(string text, int status) where T : class
        {
            try
            {
                T data = JsonUtility.FromJson<T>(text);
                if (data != null) return ApiResult<T>.Success(data, status);
            }
            catch (Exception) { /* fall through to bad_response */ }
            return ApiResult<T>.Fail(status, ApiErrorCodes.BadResponse, "Unreadable server response");
        }

        static ApiResult<T> ParseError<T>(string text, int status, string transportError) where T : class
        {
            try
            {
                ApiErrorEnvelope env = JsonUtility.FromJson<ApiErrorEnvelope>(text);
                if (env != null && env.error != null && !string.IsNullOrEmpty(env.error.code)) return ApiResult<T>.Fail(status, env.error.code, env.error.message);
            }
            catch (Exception) { /* not our envelope (proxy page, HTML...) */ }
            return ApiResult<T>.Fail(status, status == 0 ? ApiErrorCodes.Network : "http_" + status, string.IsNullOrEmpty(transportError) ? "Request failed" : transportError);
        }
    }
}
