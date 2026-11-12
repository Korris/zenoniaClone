using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using ZenoniaProto.Api;

namespace ZenoniaProto
{
    public enum AccountPage { Login, Register, Characters, Customize, Forgot, ConfirmDelete }

    /// <summary>
    /// The way into the online world, without any drawing: log in or register, list the account's characters, create one, ask for a ticket and join the zone with it. AccountScreenGui draws it; the
    /// test bots call the same methods. All on the main thread (ApiClient). A password only passes through here on its way to ApiClient and is never kept or logged. Only the refresh token stays on
    /// the device (ApiTokenStore), so a returning player lands on the character list.
    /// </summary>
    public static partial class AccountFlow
    {
        const string PrefApi = "zenonia.apiurl";
        const string DefaultApi = "http://127.0.0.1:3000";
        const int EnterAttempts = 10, EnterRetryMs = 2000;
        const float LeaveWaitSeconds = 20f;
        const int LeavePollMs = 500;
        public const string ForgotSentMessage = "If this email has an account, a reset link was sent.";

        static ApiClient api;
        static string apiUrl;
        static bool resumed;
        static string enteredId;

        /// <summary>Heroes one account may hold, of any classes. The server enforces it (MAX_CHARACTERS); this only sizes the list.</summary>
        public const int MaxCharacters = 5;

        public static AccountPage Page { get; private set; }
        public static bool Busy { get; private set; }
        public static string Status { get; private set; } = "";
        public static bool StatusBad { get; private set; }
        public static readonly List<CharacterSummary> Characters = new List<CharacterSummary>();
        /// <summary>The class being customized and named on the Customize page.</summary>
        public static HeroClass CustomizeClass { get; private set; }
        /// <summary>Hairstyle picked on the Customize page (wraps to the head art of the class).</summary>
        public static int PendingStyle { get; set; }
        /// <summary>Hair colour picked there: -1 = the style's own colour, 0..8 = hair-dyes.json presets.</summary>
        public static int PendingDye { get; set; } = CharacterLookPrefs.DefaultDye;
        /// <summary>Id of the character the player entered the world with, null outside the world. Set before the zone join, cleared by LeaveWorld.</summary>
        public static string EnteredId { get { return enteredId; } }
        /// <summary>The hero on the ConfirmDelete page.</summary>
        public static CharacterSummary Deleting { get; private set; }
        public static ApiClient Api { get { return api; } }
        public static string ApiUrl { get { return apiUrl; } }
        public static string Username { get { return api != null && api.User != null ? api.User.username : ""; } }
        /// <summary>A release build talks to its baked address, and the player has no say in it.</summary>
        public static bool ServerEditable { get { return !ReleaseBuild.IsRelease; } }
        public static bool LoggedIn { get { return api != null && api.IsLoggedIn; } }

        /// <summary>The login screen comes up. A remembered session goes straight to the character list.</summary>
        public static void Open()
        {
            if (api == null) UseServer(!string.IsNullOrEmpty(NetLaunchArgs.ApiUrl) ? NetLaunchArgs.ApiUrl : PlayerPrefs.GetString(PrefApi, DefaultApi));
            NetClient client = NetClient.Active;
            if (api.IsLoggedIn)
            {
                Page = AccountPage.Characters;
                if (client != null && client.State == ClientState.Failed) Say(client.Status, true);
                Refresh();
                return;
            }
            Page = AccountPage.Login;
            if (!resumed && !string.IsNullOrEmpty(ApiTokenStore.LoadRefreshToken())) Resume();
        }

        public static void UseServer(string url)
        {
            if (ReleaseBuild.IsRelease) url = ReleaseBuild.ApiUrl;
            url = (url ?? "").Trim().TrimEnd('/');
            if (url == apiUrl && api != null) return;
            apiUrl = url;
            api = new ApiClient(url);
            resumed = false;
            if (!ApiTokenStore.Disabled && !ApiTokenStore.TestStore && !ReleaseBuild.IsRelease) PlayerPrefs.SetString(PrefApi, url);
        }

        public static void ShowLogin() { Page = AccountPage.Login; Say("", false); }
        public static void ShowRegister() { Page = AccountPage.Register; Say("", false); }
        public static void ShowForgot() { Page = AccountPage.Forgot; Say("", false); }
        public static void ShowCharacters() { Page = AccountPage.Characters; Say("", false); }
        /// <summary>Opens the Customize page for a class with a fresh look (first style, own colour).</summary>
        public static void ShowCustomize(HeroClass id)
        {
            CustomizeClass = id;
            PendingStyle = CharacterLookPrefs.DefaultStyle;
            PendingDye = CharacterLookPrefs.DefaultDye;
            Page = AccountPage.Customize;
            Say("", false);
        }
        public static void ShowDelete(CharacterSummary c) { Deleting = c; Page = AccountPage.ConfirmDelete; Say("", false); }

        static void Say(string text, bool bad) { Status = text ?? ""; StatusBad = bad; }

        /// <summary>A plain sentence for a failed call. Anything without one here is the server's own message.</summary>
        static string Friendly<T>(ApiResult<T> r) where T : class
        {
            switch (r.Code)
            {
                case ApiErrorCodes.Network: return "Cannot reach the login server";
                case ApiErrorCodes.InvalidCredentials: return "Wrong username or password";
                case ApiErrorCodes.UsernameTaken: return "That username is taken";
                case ApiErrorCodes.EmailTaken: return "That email already has an account";
                case ApiErrorCodes.RateLimited: return "Too many tries: " + WaitText(r.RetryAfterSec);
                case ApiErrorCodes.NameTaken: return "That name is taken";
                case ApiErrorCodes.ClassTaken: return "You already have a hero of this class";
                case ApiErrorCodes.SlotsFull: return "You already have " + MaxCharacters + " heroes: delete one to make another";
                case ApiErrorCodes.AlreadyOnline: return "This character is already online";
                case ApiErrorCodes.CharacterOnline: return "This hero is still in the world. Leave the game with it first, or wait up to 2 minutes if it was closed suddenly.";
                case ApiErrorCodes.NotFound: return "That hero no longer exists";
                case ApiErrorCodes.ValidationError: return FieldProblem(r.InvalidField);
                case ApiErrorCodes.NotLoggedIn:
                case ApiErrorCodes.InvalidRefreshToken: return "Please log in again";
                default:
                    if (r.Status >= 500) return "The login server has a problem: try again in a moment";
                    return string.IsNullOrEmpty(r.Message) ? "Something went wrong" : r.Message;
            }
        }

        static string WaitText(int seconds)
        {
            if (seconds <= 0) return "wait a while and try again";
            if (seconds < 90) return "wait " + seconds + " seconds and try again";
            return "wait " + Mathf.CeilToInt(seconds / 60f) + " minutes and try again";
        }

        /// <summary>The server refused one field: the same sentence the checks on this side give, in case they let it through.</summary>
        static string FieldProblem(string field)
        {
            switch (field)
            {
                case "username": return "Username: 3 to 20 letters, digits or _";
                case "email": return "That email does not look right";
                case "password": return "Password: 8 characters or more, and not too long";
                case "name": return "Name: 3 to 16 letters, digits or _";
                case "login": return "Enter your username and password";
                default: return "Check what you typed";
            }
        }

        /// <summary>The session ended underneath us: back to the login page.</summary>
        static void BackToLoginIfLoggedOut()
        {
            if (api != null && !api.IsLoggedIn) { Page = AccountPage.Login; Characters.Clear(); }
        }

        /// <summary>Why this input cannot be sent, or null. The server checks it all again.</summary>
        public static string RegisterProblem(string username, string email, string password, string again)
        {
            username = (username ?? "").Trim();
            if (username.Length < 3 || username.Length > 20 || !IsName(username)) return "Username: 3 to 20 letters, digits or _";
            string emailProblem = EmailProblem(email);
            if (emailProblem != null) return emailProblem;
            if ((password ?? "").Length < 8) return "Password: at least 8 characters";
            if (System.Text.Encoding.UTF8.GetByteCount(password) > 72) return "Password: too long";
            if (password != again) return "The two passwords differ";
            return null;
        }

        public static string EmailProblem(string email)
        {
            string e = (email ?? "").Trim();
            int at = e.IndexOf('@');
            return e.Length > 254 || at < 1 || e.LastIndexOf('.') < at + 2 || e.EndsWith(".", StringComparison.Ordinal) ? "That email does not look right" : null;
        }

        public static string NameProblem(string name)
        {
            name = (name ?? "").Trim();
            return name.Length < 3 || name.Length > 16 || !IsName(name) ? "Name: 3 to 16 letters, digits or _" : null;
        }

        /// <summary>Deleting needs the hero's exact name typed: a mistyped or empty box does nothing.</summary>
        public static string DeleteProblem(string typed)
        {
            return Deleting == null || (typed ?? "").Trim() != Deleting.name ? "Type the hero's name exactly to delete it" : null;
        }

        static bool IsName(string s)
        {
            foreach (char c in s) if (!(c >= 'a' && c <= 'z' || c >= 'A' && c <= 'Z' || c >= '0' && c <= '9' || c == '_')) return false;
            return true;
        }

        // ---- calls: each one runs by itself and ends in Status, Page and Busy ----

        public static async void Login(string login, string password)
        {
            if (Busy) return;
            if (string.IsNullOrWhiteSpace(login) || string.IsNullOrEmpty(password)) { Say("Enter your username and password", true); return; }
            Busy = true; Say("Logging in ...", false);
            try
            {
                ApiResult<AuthResponse> r = await api.LoginAsync(login.Trim(), password);
                if (r.Ok) { Say("", false); Page = AccountPage.Characters; await RefreshNow(); }
                else Say(Friendly(r), true);
            }
            catch (Exception e) { Fail(e); }
            finally { Busy = false; }
        }

        public static async void Register(string username, string email, string password, string again)
        {
            if (Busy) return;
            string problem = RegisterProblem(username, email, password, again);
            if (problem != null) { Say(problem, true); return; }
            Busy = true; Say("Creating your account ...", false);
            try
            {
                ApiResult<AuthResponse> r = await api.RegisterAsync(username.Trim(), email.Trim(), password);
                if (r.Ok) { Say("", false); Page = AccountPage.Characters; await RefreshNow(); }
                else Say(Friendly(r), true);
            }
            catch (Exception e) { Fail(e); }
            finally { Busy = false; }
        }

        /// <summary>Asks for the reset mail. Every answer from the server that is not "slow down" reads the same, so this page can never tell whether an account exists.</summary>
        public static async void ForgotPassword(string email)
        {
            if (Busy) return;
            string problem = EmailProblem(email);
            if (problem != null) { Say(problem, true); return; }
            Busy = true; Say("Sending ...", false);
            try
            {
                ApiResult<ApiOk> r = await api.RequestPasswordResetAsync(email.Trim());
                if (r.Ok) Say(ForgotSentMessage, false);
                else Say(Friendly(r), true);
            }
            catch (Exception e) { Fail(e); }
            finally { Busy = false; }
        }

        static async void Resume()
        {
            if (Busy) return;
            resumed = true;
            Busy = true; Say("Welcome back ...", false);
            try
            {
                ApiResult<AuthResponse> r = await api.ResumeAsync();
                if (r.Ok) { Say("", false); Page = AccountPage.Characters; await RefreshNow(); }
                else Say(r.Code == ApiErrorCodes.Network ? Friendly(r) : "", r.Code == ApiErrorCodes.Network); // an old session is simply over: the login form is the answer
            }
            catch (Exception e) { Fail(e); }
            finally { Busy = false; }
        }

        public static async void Logout()
        {
            if (Busy) return;
            Busy = true;
            try { await api.LogoutAsync(); }
            catch (Exception e) { Debug.LogWarning("[ZenoniaProto] account: logout: " + e.GetType().Name); }
            finally { Busy = false; }
            Characters.Clear();
            Page = AccountPage.Login;
            Say("", false);
        }

        /// <summary>
        /// From inside the world, to the character list or all the way out. Dropping the connection is what makes the zone write the hero and give its lock back (ZonePersistence.OnLeave), so the
        /// list is asked until the hero shows offline: only then is it really saved. If that takes too long the player still gets out, the zone goes on trying by itself.
        /// </summary>
        public static async void LeaveWorld(bool logout)
        {
            if (Busy || api == null) return;
            string id = enteredId;
            enteredId = null;
            Busy = true;
            Page = AccountPage.Characters;
            Say("Saving your hero ...", false);
            try
            {
                GameSession.Instance.LeaveWorld();
                bool released = await WaitUntilOffline(id);
                if (logout)
                {
                    await api.LogoutAsync();
                    Characters.Clear();
                    Page = AccountPage.Login;
                    Say(released ? "" : "Logged out. The server is still saving your hero.", false);
                }
                else
                {
                    await RefreshNow();
                    if (!api.IsLoggedIn) Say("Please log in again", true);
                    else if (!released) Say("The server is still saving your hero: it can be played again in a moment", false);
                    else Say("", false);
                }
            }
            catch (Exception e) { Fail(e); }
            finally { Busy = false; }
        }

        static async Task<bool> WaitUntilOffline(string id)
        {
            if (id == null) return true;
            float until = Time.realtimeSinceStartup + LeaveWaitSeconds;
            while (Time.realtimeSinceStartup < until)
            {
                ApiResult<CharacterListResponse> r = await api.ListCharactersAsync();
                if (r.Ok)
                {
                    bool online = false;
                    if (r.Data.characters != null) foreach (CharacterSummary c in r.Data.characters) if (c.id == id) online = c.online;
                    if (!online) return true;
                }
                else if (!api.IsLoggedIn) return false;
                await Task.Delay(LeavePollMs);
            }
            return false;
        }

        static void Fail(Exception e)
        {
            Say("Something went wrong", true);
            Debug.LogWarning("[ZenoniaProto] account: " + e.GetType().Name); // the type only: a message could quote a request
        }
    }
}
