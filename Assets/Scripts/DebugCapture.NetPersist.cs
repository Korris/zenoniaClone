using System;
using System.Collections;
using System.Globalization;
using UnityEngine;
using ZenoniaProto.Api;
using ZenoniaProto.Net;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// Real-account test bots (Tools/net-persist-test.ps1): the same AccountFlow the screen uses, then the real zone.
    ///   --account NAME [--account-new]   log in (or register) as NAME, password from the environment variable ZENONIA_TEST_PASSWORD; --class picks the hero to create when the account has none
    ///   --account-wait S                 stay on the character list S seconds and ask again: the access token has to be refreshed by then
    ///   --net-bot persist --persist-role actions|earn|earn-stay|show|idle|enhance|trade-a|trade-b|hang-a|hang-b|pet-far|pet-buy|pet-buy-stay|pet-wear|pet-toggle|pet-see|pet-drain|pet-wearout|pet-watch, --partner NAME, --give-uid UID, --confirm-file F (confirm once F exists), --expect-refused (the trade must not go through)
    ///   --account-script messages|delete-online   the account screen's own answers, without joining: --limited-api URL (a server-api that allows two tries)   --delete NAME
    ///   --test-token-store               the real refresh token store under test keys (ApiTokenStore), for the "actions" role to check what logout leaves behind
    /// Every expectation is a "persist-e2e:" line; what the script compares afterwards comes from the API.
    /// </summary>
    public partial class DebugCapture
    {
        static void P(string line) { Debug.Log("[ZenoniaProto] persist-e2e: " + line); }

        static string PersistLine()
        {
            PlayerState me = Me;
            HeroController h = GameSession.Hero;
            string weapon = me.GetEquipped(Slots.Weapon) != null ? me.GetEquipped(Slots.Weapon).uid + "+" + me.GetEquipped(Slots.Weapon).enhance : "-";
            return "gold=" + me.Gold + " level=" + me.Level + " exp=" + me.Exp + " stones=" + me.UpgradeStone + " bag=[" + Bag() + "] weapon=" + weapon + " pet=" + (me.GetEquipped(Slots.Pet) != null ? me.GetEquipped(Slots.Pet).uid : "-") + " petdur=" + (me.GetEquipped(Slots.Pet) != null ? me.GetEquipped(Slots.Pet).durability : 0) + " maxhp=" + me.MaxHp + " hp=" + Mathf.CeilToInt(me.Hp) + " atk=" + me.DamageAtk.ToString("F1", CultureInfo.InvariantCulture) + " map=" + MapRegistry.Current.Id + " at=" + h.Position.x.ToString("F0", CultureInfo.InvariantCulture) + "," + h.Position.y.ToString("F0", CultureInfo.InvariantCulture);
        }

        /// <summary>Drives the account flow up to the zone join.</summary>
        IEnumerator AccountBot(string account)
        {
            string[] a = Environment.GetCommandLineArgs();
            string password = Environment.GetEnvironmentVariable("ZENONIA_TEST_PASSWORD") ?? "";
            AccountFlow.UseServer(NetLaunchArgs.ApiUrl);
            if (Array.IndexOf(a, "--account-new") >= 0) AccountFlow.Register(account, account + "@example.com", password, password);
            else AccountFlow.Login(account, password);
            yield return new WaitForSeconds(0.1f);
            while (AccountFlow.Busy) yield return null;
            if (AccountFlow.Page != AccountPage.Characters) { P("FAIL login: " + AccountFlow.Status); yield break; }
            P("PASS logged in as " + AccountFlow.Username);
            float wait;
            if (float.TryParse(NetLaunchArgs.Arg(a, "--account-wait"), NumberStyles.Float, CultureInfo.InvariantCulture, out wait))
            {
                string before = AccountFlow.Api.AccessToken;
                yield return new WaitForSeconds(wait);
                AccountFlow.Refresh();
                yield return new WaitForSeconds(0.1f);
                while (AccountFlow.Busy) yield return null;
                P((AccountFlow.Page == AccountPage.Characters && AccountFlow.Api.AccessToken != before && AccountFlow.Api.RefreshToken != null ? "PASS" : "FAIL") + " the expired access token was refreshed by itself");
            }
            if (AccountFlow.Characters.Count == 0)
            {
                string cls = string.IsNullOrEmpty(NetLaunchArgs.ClassName) ? "warrior" : NetLaunchArgs.ClassName;
                AccountFlow.Create(account, HeroClassDef.All[GameSession.ClassIndex(cls)].Id, 0, -1);
                yield return new WaitForSeconds(0.1f);
                while (AccountFlow.Busy) yield return null;
                P((AccountFlow.Characters.Count == 1 ? "PASS" : "FAIL") + " created a hero: " + AccountFlow.Status);
            }
            if (AccountFlow.Characters.Count == 0) yield break;
            AccountFlow.Enter(AccountFlow.Characters[0]);
            yield return new WaitForSeconds(0.1f);
            while (AccountFlow.Busy) yield return null;
            if (!string.IsNullOrEmpty(AccountFlow.Status)) P("enter said: " + AccountFlow.Status);
        }

        /// <summary>--login-shots DIR: PNGs of each page of the account flow, reached by the same calls the buttons make. Needs --account (of a fresh account) and a window.</summary>
        IEnumerator LoginShots(string dir)
        {
            System.IO.Directory.CreateDirectory(dir);
            string[] a = Environment.GetCommandLineArgs();
            string account = NetLaunchArgs.Arg(a, "--account");
            string password = Environment.GetEnvironmentVariable("ZENONIA_TEST_PASSWORD") ?? "";
            // AccountBot is not wanted here: the pages are visited one by one.
            ConnectScreenGui.Open();
            AccountFlow.UseServer(NetLaunchArgs.ApiUrl);
            yield return new WaitForSeconds(1.5f);
            yield return Shot(dir, "1-login");
            AccountFlow.ShowRegister();
            yield return new WaitForSeconds(0.5f);
            yield return Shot(dir, "2-register");
            AccountFlow.Register(account, account + "@example.com", password, password);
            yield return new WaitForSeconds(0.1f);
            while (AccountFlow.Busy) yield return null;
            yield return new WaitForSeconds(0.5f);
            yield return Shot(dir, "3-characters-empty");
            AccountFlow.Create(account + "_w", HeroClass.Warrior, 0, -1);
            yield return new WaitForSeconds(0.1f);
            while (AccountFlow.Busy) yield return null;
            yield return new WaitForSeconds(0.5f);
            yield return Shot(dir, "4-characters");
            ConnectScreenGui.CreateHero(AccountFlow.Characters);
            yield return new WaitForSeconds(2f);
            P((HeroSelectScreen.IsShown ? "PASS" : "FAIL") + " the hero select opened in its online mode");
            yield return Shot(dir, "5-hero-select-online");
            HeroSelectScreen.TestPickClass((int)HeroClass.Warrior);
            yield return new WaitForSeconds(0.5f);
            yield return Shot(dir, "6-hero-select-taken");
            HeroSelectScreen.TestPickClass((int)HeroClass.Wizard);
            yield return new WaitForSeconds(0.5f);
            HeroSelectScreen.TestPressStart();
            yield return new WaitForSeconds(1.5f);
            P((AccountFlow.Page == AccountPage.Customize && AccountFlow.CustomizeClass == HeroClass.Wizard && !HeroSelectScreen.IsShown ? "PASS" : "FAIL") + " next asks for the look and name of the picked hero");
            yield return Shot(dir, "7-customize");
            CharacterCustomizeScreen.TestSetName("Frostwind");
            AccountFlow.PendingStyle = 2; AccountFlow.PendingDye = 4; // the third hairstyle, the fifth preset colour
            yield return new WaitForSeconds(1f);
            yield return Shot(dir, "7b-customize-look-and-name");
            AccountFlow.ShowForgot();
            yield return new WaitForSeconds(0.3f);
            yield return Shot(dir, "8-forgot");
            AccountFlow.ForgotPassword(account + "@example.com");
            yield return AccountDone();
            yield return new WaitForSeconds(0.3f);
            yield return Shot(dir, "9-forgot-sent");
            CharacterSummary warrior = HeroOf("warrior");
            AccountFlow.ShowDelete(warrior);
            yield return new WaitForSeconds(0.3f);
            yield return Shot(dir, "10-delete-confirm");
            AccountFlow.ShowCharacters();
            AccountFlow.Enter(warrior);
            yield return AccountDone();
            float until = Time.realtimeSinceStartup + 40f;
            while (GameSession.Hero == null && Time.realtimeSinceStartup < until) yield return null;
            yield return new WaitForSeconds(2f);
            P((GameSession.Hero != null && MapRegistry.Current.Id == OakhavenVillageMap.MapId ? "PASS" : "FAIL") + " in the world, in Oakhaven");
            yield return Shot(dir, "11-in-world");
            HeroChangeScreen.Open(); // what the Settings button calls
            yield return new WaitForSeconds(0.5f);
            P((AccountScreenGui.MenuOpen ? "PASS" : "FAIL") + " the Settings button opens the account menu online");
            yield return Shot(dir, "12-account-menu");
            AccountFlow.LeaveWorld(false);
            yield return AccountDone();
            yield return new WaitForSeconds(0.5f);
            P((!AccountScreenGui.MenuOpen && ConnectScreenGui.Visible && AccountFlow.Page == AccountPage.Characters ? "PASS" : "FAIL") + " change character shows the list");
            yield return Shot(dir, "13-after-change-character");
            P("shots done");
            Application.Quit();
        }

        IEnumerator Shot(string dir, string name)
        {
            yield return new WaitForEndOfFrame();
            Save(System.IO.Path.Combine(dir, name + ".png"));
            P("shot " + name);
        }

        void StartPersistBot()
        {
            string[] a = Environment.GetCommandLineArgs();
            string role = NetLaunchArgs.Arg(a, "--persist-role");
            P("joined as " + GameSession.Hero.PlayerName + " role " + role);
            StartCoroutine(PersistBot(role, NetLaunchArgs.Arg(a, "--partner"), NetLaunchArgs.Arg(a, "--give-uid")));
        }

        IEnumerator FirstState()
        {
            float start = Time.realtimeSinceStartup;
            int states = NetStats.Current != null ? (int)NetStats.Current.Get("rpgStates") : 0;
            while (Time.realtimeSinceStartup - start < 5f && NetStats.Current != null && (int)NetStats.Current.Get("rpgStates") == 0 && states == 0) yield return null;
            yield return new WaitForSeconds(0.3f);
        }

        IEnumerator PersistBot(string role, string partner, string giveUid)
        {
            yield return FirstState();
            P("loaded " + PersistLine());
            switch (role)
            {
                case "earn":
                case "earn-stay":
                {
                    int gold0 = Me.Gold;
                    yield return WalkOutOfTown(); // a new hero begins in the safe village
                    EnableNavBot(false, BotOffset(GameSession.Hero.PlayerName));
                    float start = Time.realtimeSinceStartup;
                    while (Me.Gold <= gold0 && Time.realtimeSinceStartup - start < 300f) yield return null;
                    P((Me.Gold > gold0 ? "PASS" : "FAIL") + " earned gold by fighting in " + (Time.realtimeSinceStartup - start).ToString("F0", CultureInfo.InvariantCulture) + " s");
                    P("earned " + PersistLine());
                    yield return new WaitForSeconds(1f);
                    if (role == "earn-stay") { while (true) yield return new WaitForSeconds(2f); }
                    P("quitting");
                    Application.Quit();
                    break;
                }
                case "actions": yield return AccountActions(); break;
                case "wanted-kill": yield return WantedKill(); break;
                case "wanted-board": yield return WantedBoard(); break;
                case "show":
                    yield return new WaitForSeconds(1f);
                    P("final " + PersistLine());
                    Application.Quit();
                    break;
                case "idle":
                    P("idle " + PersistLine());
                    while (true) { yield return new WaitForSeconds(2f); P("idle " + PersistLine()); }
                case "enhance":
                {
                    Item weapon = Me.GetEquipped(Slots.Weapon);
                    int stones = Me.UpgradeStone, level = weapon.enhance;
                    NetClient.SendInvAction(InvOp.Enhance, weapon.uid, 0);
                    float start = Time.realtimeSinceStartup;
                    while (Me.UpgradeStone == stones && Time.realtimeSinceStartup - start < 15f) yield return null;
                    weapon = Me.GetEquipped(Slots.Weapon);
                    P((Me.UpgradeStone == stones - 1 ? "PASS" : "FAIL") + " one stone spent, level " + level + " -> " + weapon.enhance);
                    P("enhanced " + PersistLine());
                    yield return new WaitForSeconds(1f);
                    Application.Quit();
                    break;
                }
                case "pet-far": yield return PetFar(); break;
                case "pet-buy": yield return PetBuy(); break;
                case "pet-buy-stay": yield return PetBuyStay(); break;
                case "pet-wear": yield return PetWear(); break;
                case "pet-toggle": yield return PetToggle(); break;
                case "pet-see": yield return PetSee(partner); break;
                case "pet-drain": yield return PetDrain(); break;
                case "pet-wearout": yield return PetWearout(); break;
                case "pet-watch": yield return PetWatch(partner); break;
                case "trade-a": yield return PersistTrade(partner, giveUid, 300, 100, true, true); break;
                case "trade-b": yield return PersistTrade(partner, null, 100, 300, false, true); break;
                case "hang-a": yield return PersistTrade(partner, giveUid, 300, 100, true, false); break;
                case "hang-b": yield return PersistTrade(partner, null, 100, 300, false, false); break;
                default: P("FAIL unknown role " + role); break;
            }
        }

        /// <summary>From a safe map through its portal to a hostile one: the real input stream and the real zone transfer. Nothing happens on a hostile map.</summary>
        IEnumerator WalkOutOfTown()
        {
            if (MapRegistry.Current.Hostile) yield break;
            string from = MapRegistry.Current.Id;
            EnablePortalBot(FrostHollowLayout.MapId); // Oakhaven also has the east gate to Thatchwick: the save check below expects Frost Hollow
            float start = Time.realtimeSinceStartup;
            while (!MapRegistry.Current.Hostile && Time.realtimeSinceStartup - start < 60f) yield return null;
            InputAdapter.BotHeld = null;
            InputAdapter.BotPressed = null;
            P((MapRegistry.Current.Hostile ? "PASS" : "FAIL") + " walked from " + from + " through its portal to " + MapRegistry.Current.Id);
            yield return new WaitForSeconds(1f); // the server's save of the new map is on its way
        }

        static IEnumerator AccountDone()
        {
            yield return new WaitForSeconds(0.1f);
            while (AccountFlow.Busy) yield return null;
        }

        static CharacterSummary HeroOf(string classId)
        {
            foreach (CharacterSummary c in AccountFlow.Characters) if (c.classId == classId) return c;
            return null;
        }

        /// <summary>The menu's own calls in order: into the world, change character, into the world as another hero, log out, log in, delete.</summary>
        IEnumerator AccountActions()
        {
            string[] a = Environment.GetCommandLineArgs();
            string account = NetLaunchArgs.Arg(a, "--account");
            string password = Environment.GetEnvironmentVariable("ZENONIA_TEST_PASSWORD") ?? "";
            P((MapRegistry.Current.Id == OakhavenVillageMap.MapId ? "PASS" : "FAIL") + " a new hero stands in Oakhaven");
            P((Vector2.Distance(GameSession.Hero.Position, MapRegistry.Current.PlayerStart) < 40f ? "PASS" : "FAIL") + " at the start of the map, like offline");
            yield return WalkOutOfTown();

            // ---- change character ----
            float t = Time.realtimeSinceStartup;
            AccountFlow.LeaveWorld(false);
            yield return AccountDone();
            P("change character took " + (Time.realtimeSinceStartup - t).ToString("F1", CultureInfo.InvariantCulture) + " s: " + AccountFlow.Status);
            CharacterSummary warrior = HeroOf("warrior");
            P((AccountFlow.Page == AccountPage.Characters && NetClient.Active.State != ClientState.Joined && GameSession.Hero == null ? "PASS" : "FAIL") + " change character ends on the character list, out of the world");
            P((warrior != null && !warrior.online ? "PASS" : "FAIL") + " the lock was released before the list came back");
            P((warrior != null && warrior.mapId == FrostHollowLayout.MapId ? "PASS" : "FAIL") + " the last save was flushed: the API says " + (warrior != null ? warrior.mapId : "?"));

            AccountFlow.Create(account + "w", HeroClass.Wizard, 0, -1);
            yield return AccountDone();
            CharacterSummary wizard = HeroOf("wizard");
            if (wizard == null) { P("FAIL no second hero: " + AccountFlow.Status); yield break; }
            AccountFlow.Enter(wizard);
            yield return AccountDone();
            float until = Time.realtimeSinceStartup + 40f;
            while ((NetClient.Active.State != ClientState.Joined || GameSession.Hero == null) && Time.realtimeSinceStartup < until) yield return null;
            if (GameSession.Hero == null) { P("FAIL the second hero never joined: " + NetClient.Active.Status); yield break; }
            yield return FirstState();
            P((GameSession.Hero.PlayerName == wizard.name && Me.ClassId == "wizard" ? "PASS" : "FAIL") + " in the world again as " + GameSession.Hero.PlayerName + " " + Me.ClassId);
            P((MapRegistry.Current.Id == OakhavenVillageMap.MapId ? "PASS" : "FAIL") + " the second new hero starts in Oakhaven too");
            yield return WalkOutOfTown(); // moving after a rejoin in the same process: the input stream and the epochs were reset properly

            // ---- log out ----
            string oldToken = AccountFlow.Api.RefreshToken;
            P((!string.IsNullOrEmpty(ApiTokenStore.LoadRefreshToken()) ? "PASS" : "FAIL") + " the device remembered the session until now");
            AccountFlow.LeaveWorld(true);
            yield return AccountDone();
            P((AccountFlow.Page == AccountPage.Login && !AccountFlow.LoggedIn && GameSession.Hero == null ? "PASS" : "FAIL") + " log out ends on the login page: " + AccountFlow.Status);
            P((string.IsNullOrEmpty(ApiTokenStore.LoadRefreshToken()) && string.IsNullOrEmpty(ApiTokenStore.LoadUsername()) ? "PASS" : "FAIL") + " the token is gone from the device");
            using (var req = new UnityEngine.Networking.UnityWebRequest(AccountFlow.ApiUrl + "/auth/refresh", "POST"))
            {
                req.uploadHandler = new UnityEngine.Networking.UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(JsonUtility.ToJson(new RefreshRequest { refreshToken = oldToken })));
                req.downloadHandler = new UnityEngine.Networking.DownloadHandlerBuffer();
                req.SetRequestHeader("Content-Type", "application/json");
                yield return req.SendWebRequest();
                P((req.responseCode == 401 ? "PASS" : "FAIL") + " the server refuses the old refresh token: " + req.responseCode);
            }

            // ---- delete ----
            AccountFlow.Login(account, password);
            yield return AccountDone();
            wizard = HeroOf("wizard");
            P((AccountFlow.Characters.Count == 2 && wizard != null && !wizard.online && !HeroOf("warrior").online ? "PASS" : "FAIL") + " logged in again: both heroes there, both offline");
            P((wizard != null && wizard.mapId == FrostHollowLayout.MapId ? "PASS" : "FAIL") + " log out flushed the save too: the API says " + (wizard != null ? wizard.mapId : "?"));
            AccountFlow.ShowDelete(wizard);
            P((AccountFlow.Page == AccountPage.ConfirmDelete ? "PASS" : "FAIL") + " delete asks first");
            AccountFlow.Delete(wizard.name + "x");
            yield return AccountDone();
            AccountFlow.Delete("");
            yield return AccountDone();
            AccountFlow.Delete(wizard.name.ToUpperInvariant() == wizard.name ? wizard.name.ToLowerInvariant() : wizard.name.ToUpperInvariant());
            yield return AccountDone();
            P((AccountFlow.Page == AccountPage.ConfirmDelete && AccountFlow.StatusBad && AccountFlow.Characters.Count == 2 ? "PASS" : "FAIL") + " a wrong name deletes nothing: " + AccountFlow.Status);
            AccountFlow.Delete(wizard.name);
            yield return AccountDone();
            P((AccountFlow.Page == AccountPage.Characters && AccountFlow.Characters.Count == 1 && HeroOf("wizard") == null ? "PASS" : "FAIL") + " the exact name deletes the hero: " + AccountFlow.Status);
            AccountFlow.Logout();
            yield return AccountDone();
            P("actions done");
            Application.Quit();
        }

        static IEnumerator Says(string what, Action call, string want)
        {
            call();
            yield return AccountDone();
            P((AccountFlow.Status == want ? "PASS " : "FAIL ") + what + ": " + AccountFlow.Status);
        }

        /// <summary>Scripts on the account screen's calls that do not join the world. Each line reports what the player would read.</summary>
        IEnumerator AccountScript(string script, string account)
        {
            string[] a = Environment.GetCommandLineArgs();
            string password = Environment.GetEnvironmentVariable("ZENONIA_TEST_PASSWORD") ?? "";
            AccountFlow.UseServer(NetLaunchArgs.ApiUrl);
            if (script == "delete-online")
            {
                AccountFlow.Login(account, password);
                yield return AccountDone();
                CharacterSummary c = AccountFlow.Characters.Find(x => x.name == NetLaunchArgs.Arg(a, "--delete"));
                if (c == null) { P("FAIL no such hero"); yield break; }
                AccountFlow.ShowDelete(c);
                AccountFlow.Delete(c.name);
                yield return AccountDone();
                P((AccountFlow.Page == AccountPage.ConfirmDelete && AccountFlow.StatusBad && AccountFlow.Status.StartsWith("This hero is still in the world", StringComparison.Ordinal) ? "PASS" : "FAIL") + " deleting a hero in the world is refused in words: " + AccountFlow.Status);
                P("script done");
                yield break;
            }

            string email = account + "@example.com", other = account + "x", sent = AccountFlow.ForgotSentMessage;
            string goodPassword = "a-good-password";
            yield return Says("wrong password", () => AccountFlow.Login(account, "wrong-password"), "Wrong username or password");
            yield return Says("unknown user", () => AccountFlow.Login(other, "wrong-password"), "Wrong username or password");
            yield return Says("username taken", () => AccountFlow.Register(account, other + "@example.com", goodPassword, goodPassword), "That username is taken");
            yield return Says("email taken", () => AccountFlow.Register(other, email, goodPassword, goodPassword), "That email already has an account");
            yield return Says("email only the server refuses (validation_error)", () => AccountFlow.Register(other, "x@y.z", goodPassword, goodPassword), "That email does not look right");
            for (int i = 0; i < 6; i++)
            {
                AccountFlow.Login(account, "wrong-password");
                yield return AccountDone();
            }
            P((AccountFlow.StatusBad && AccountFlow.Status == "Too many tries: wait 15 minutes and try again" ? "PASS" : "FAIL") + " locked out: " + AccountFlow.Status);

            yield return Says("reset for a real account", () => AccountFlow.ForgotPassword(email), sent);
            P((!AccountFlow.StatusBad ? "PASS" : "FAIL") + " that reads as good news, not an error");
            yield return Says("reset for no account at all reads the same", () => AccountFlow.ForgotPassword("nobody-" + email), sent);
            yield return Says("reset with no email", () => AccountFlow.ForgotPassword("nope"), "That email does not look right");
            yield return Says("reset with an email only the server refuses", () => AccountFlow.ForgotPassword("x@y.z"), "That email does not look right");

            AccountFlow.UseServer(NetLaunchArgs.Arg(a, "--limited-api"));
            string last = "";
            for (int i = 0; i < 4; i++)
            {
                AccountFlow.ForgotPassword(email);
                yield return AccountDone();
                last = AccountFlow.Status;
            }
            P((last.StartsWith("Too many tries: wait ", StringComparison.Ordinal) && AccountFlow.StatusBad ? "PASS" : "FAIL") + " reset, 429: " + last);

            AccountFlow.UseServer("http://127.0.0.1:1");
            yield return Says("reset, nothing there", () => AccountFlow.ForgotPassword(email), "Cannot reach the login server");
            yield return Says("login, nothing there", () => AccountFlow.Login(account, "x"), "Cannot reach the login server");
            P("player http setting, loopback: " + NetLaunchArgs.ApiUrl + " answered every call above (development build: " + Debug.isDebugBuild + ")");

            // The same setting against an address that is not this machine: does Unity refuse the http call before it leaves, or try it?
            var far = new ApiClient("http://192.0.2.1:3000");
            System.Threading.Tasks.Task<ApiResult<ApiOk>> task = far.RequestPasswordResetAsync(email);
            while (!task.IsCompleted) yield return null;
            P("player http setting, not loopback: " + task.Result.Code + " / " + task.Result.Message);
            P("script done");
        }

        // ---- pets ----

        const string PetSeller = "merchantDale";

        static Item FirstPetInBag() { return Me.Inventory.Find(i => i.slot == Slots.Pet); }

        /// <summary>Waits for the answer to the request just sent: the server answers every inventory request with a state.</summary>
        static IEnumerator AnsweredState()
        {
            int states = (int)NetStats.Current.Get("rpgStates");
            float start = Time.realtimeSinceStartup;
            while ((int)NetStats.Current.Get("rpgStates") == states && Time.realtimeSinceStartup - start < 8f) yield return null;
            yield return new WaitForSeconds(0.2f);
        }

        /// <summary>A hero at the start of Oakhaven with the gold for a pet: the seller is out of reach. Then says where the script should put it: a free spot beside the seller.</summary>
        IEnumerator PetFar()
        {
            int gold = Me.Gold;
            NetClient.SendInvAction(InvOp.PetBuy, PetSeller, 0);
            yield return AnsweredState();
            P((Me.Gold == gold && FirstPetInBag() == null ? "PASS" : "FAIL") + " out of reach of the seller nothing is sold");
            NetClient.SendInvAction(InvOp.PetBuy, "elderRowan", 0);
            yield return AnsweredState();
            P((Me.Gold == gold && FirstPetInBag() == null ? "PASS" : "FAIL") + " a villager who sells nothing sells nothing");
            foreach (MapNpc n in MapRegistry.Current.Npcs)
            {
                if (n.Id != PetSeller) continue;
                Vector2 feet = FrostHollowMap.TileFeet(n.Tx, n.Ty, 2f);
                foreach (Vector2 d in new[] { new Vector2(0f, -22f), new Vector2(22f, 0f), new Vector2(-22f, 0f), new Vector2(0f, 22f), new Vector2(26f, -26f), new Vector2(-26f, -26f) })
                {
                    Vector2 c = feet + d;
                    if (FrostHollowMap.RectBlocked(c.x - 5f, c.y, 10f, 8f)) continue;
                    P("spot x=" + c.x.ToString("F1", CultureInfo.InvariantCulture) + " y=" + c.y.ToString("F1", CultureInfo.InvariantCulture) + " map=" + MapRegistry.Current.Id);
                    break;
                }
            }
            yield return new WaitForSeconds(0.5f);
            Application.Quit();
        }

        /// <summary>Beside the seller: buys the spirit, wears it, checks the numbers, then what must be refused.</summary>
        IEnumerator PetBuy()
        {
            int gold = Me.Gold, hp = Me.MaxHp, stones = Me.UpgradeStone;
            PetDef spirit = PetConfig.Get("spirit");
            NetClient.SendInvAction(InvOp.PetBuy, "healerNina", 0);
            yield return AnsweredState();
            P((Me.Gold == gold && FirstPetInBag() == null ? "PASS" : "FAIL") + " the healer does not sell pets");
            NetClient.SendInvAction(InvOp.PetBuy, PetSeller, 99);
            yield return AnsweredState();
            P((Me.Gold == gold && FirstPetInBag() == null ? "PASS" : "FAIL") + " a pet index nobody sells is refused");
            NetClient.SendInvAction(InvOp.PetBuy, PetSeller, 0);
            yield return AnsweredState();
            Item pet = FirstPetInBag();
            P((pet != null && pet.templateId == spirit.TemplateId && Me.Gold == gold - spirit.Price ? "PASS" : "FAIL") + " bought the spirit: gold " + gold + " -> " + Me.Gold);
            if (pet == null) yield break;
            NetClient.SendInvAction(InvOp.Equip, pet.uid, 0);
            yield return AnsweredState();
            int want = RpgMath.Round(hp * (1 + spirit.MaxHpPct));
            P((Me.GetEquipped(Slots.Pet) != null && Me.MaxHp == want ? "PASS" : "FAIL") + " worn: max HP " + hp + " -> " + Me.MaxHp + ", wanted " + want);
            NetClient.SendInvAction(InvOp.Enhance, pet.uid, 0);
            yield return AnsweredState();
            P((Me.UpgradeStone == stones && Me.GetEquipped(Slots.Pet).enhance == 0 ? "PASS" : "FAIL") + " enhance refuses the worn pet: stones " + stones + " -> " + Me.UpgradeStone);
            P("bought " + PersistLine());
            yield return new WaitForSeconds(1f);
            Application.Quit();
        }

        /// <summary>Buys the imp into the bag and stays: the script kills the zone right after, before the periodic save.</summary>
        IEnumerator PetBuyStay()
        {
            int gold = Me.Gold;
            NetClient.SendInvAction(InvOp.PetBuy, PetSeller, 1);
            yield return AnsweredState();
            P((Me.Gold < gold ? "PASS" : "FAIL") + " bought the imp");
            P("imp-bought " + PersistLine());
            while (true) yield return new WaitForSeconds(2f);
        }

        /// <summary>Wears whatever pet is in the bag.</summary>
        IEnumerator PetWear()
        {
            Item pet = FirstPetInBag();
            float atk = Me.DamageAtk;
            if (pet == null) { P("FAIL no pet in the bag"); yield break; }
            NetClient.SendInvAction(InvOp.Equip, pet.uid, 0);
            yield return AnsweredState();
            PetDef def = PetConfig.Of(Me.GetEquipped(Slots.Pet));
            P((def != null && Mathf.Approximately(Me.DamageAtk, (float)(Me.Atk * (1 + def.DamagePct))) ? "PASS" : "FAIL") + " " + Me.ClassId + " wears a " + (def != null ? def.Id : "?") + " it got in a trade: damage attack " + atk + " -> " + Me.DamageAtk);
            P("wearing " + PersistLine());
            yield return new WaitForSeconds(1f);
            Application.Quit();
        }

        /// <summary>Takes the worn pet off and puts it back on, slowly, for whoever is watching (pet-see).</summary>
        IEnumerator PetToggle()
        {
            yield return new WaitForSeconds(9f);
            string uid = Me.GetEquipped(Slots.Pet).uid;
            NetClient.SendInvAction(InvOp.Unequip, "", (byte)Slots.Index(Slots.Pet));
            yield return AnsweredState();
            P((Me.GetEquipped(Slots.Pet) == null ? "PASS" : "FAIL") + " took the pet off");
            yield return new WaitForSeconds(6f);
            NetClient.SendInvAction(InvOp.Equip, uid, 0);
            yield return AnsweredState();
            P((Me.GetEquipped(Slots.Pet) != null ? "PASS" : "FAIL") + " put the pet on again");
            while (true) yield return new WaitForSeconds(2f);
        }

        /// <summary>In a window: what this client shows for another player's pet, on arrival and through a change. Saves pictures to --shot-path.</summary>
        IEnumerator PetSee(string partner)
        {
            yield return TUntil(() => HeroNamed(partner) != null, 40f, "see " + partner);
            HeroController other = HeroNamed(partner);
            if (other == null || other.View == null) { P("FAIL no view of " + partner); yield break; }
            System.IO.Directory.CreateDirectory(shotPath);
            Camera.main.orthographicSize = 42f;
            yield return new WaitForSeconds(2f);
            string first = other.View.PetId;
            P((first != null ? "PASS" : "FAIL") + " sees " + partner + "'s pet on arrival: " + first);
            P("my own pet here: " + GameSession.Hero.View.PetId);
            yield return Shot(shotPath, "pet-both");
            float start = Time.realtimeSinceStartup;
            while (other.View.PetId != null && Time.realtimeSinceStartup - start < 25f) yield return null;
            P((other.View.PetId == null ? "PASS" : "FAIL") + " sees the pet go when it is taken off");
            yield return new WaitForSeconds(0.5f);
            yield return Shot(shotPath, "pet-off");
            start = Time.realtimeSinceStartup;
            while (other.View.PetId == null && Time.realtimeSinceStartup - start < 25f) yield return null;
            P((other.View.PetId == first ? "PASS" : "FAIL") + " sees it come back");
            yield return new WaitForSeconds(0.5f);
            yield return Shot(shotPath, "pet-back");
            P("see done");
            yield return new WaitForSeconds(0.5f);
            Application.Quit();
        }

        /// <summary>Fights real monsters for --fight-seconds with the worn pet and reports what it cost: the numbers behind the lifetime estimate.</summary>
        IEnumerator PetDrain()
        {
            yield return WalkOutOfTown();
            Item pet = Me.GetEquipped(Slots.Pet);
            if (pet == null) { P("FAIL no pet worn"); yield break; }
            string kind = pet.templateId;
            int before = pet.durability, kills0 = Me.Exp;
            float seconds = float.Parse(NetLaunchArgs.Arg(Environment.GetCommandLineArgs(), "--fight-seconds"), CultureInfo.InvariantCulture);
            EnableNavBot(false, BotOffset(GameSession.Hero.PlayerName));
            yield return new WaitForSeconds(seconds);
            pet = Me.GetEquipped(Slots.Pet);
            int after = pet != null ? pet.durability : 0;
            P((after < before ? "PASS" : "FAIL") + " " + kind + " lost durability in real fighting: " + before + " -> " + after);
            P("rate kind=" + kind + " drop=" + (before - after) + " seconds=" + seconds.ToString("F0", CultureInfo.InvariantCulture));
            P("drained " + PersistLine());
            yield return new WaitForSeconds(0.6f);
            Application.Quit();
        }

        /// <summary>Fights until the worn pet is gone, then stays, for the script to check and kill. Saves pictures when it has --shot-path.</summary>
        IEnumerator PetWearout()
        {
            yield return WalkOutOfTown();
            Item pet = Me.GetEquipped(Slots.Pet);
            if (pet == null) { P("FAIL no pet worn"); yield break; }
            string uid = pet.uid;
            bool window = GameSession.Hero.View != null;
            if (window) { System.IO.Directory.CreateDirectory(shotPath); Camera.main.orthographicSize = 42f; }
            EnableNavBot(false, BotOffset(GameSession.Hero.PlayerName));
            float start = Time.realtimeSinceStartup;
            int hpMax = Me.MaxHp;
            if (window) yield return Shot(shotPath, "wear-before");
            while (Me.GetEquipped(Slots.Pet) != null && Time.realtimeSinceStartup - start < 240f) yield return null;
            bool gone = Me.GetEquipped(Slots.Pet) == null;
            if (window) yield return Shot(shotPath, "wear-out-self");
            P((gone && !Me.Inventory.Exists(i => i.uid == uid) ? "PASS" : "FAIL") + " the pet wore out and is in neither the slot nor the bag, after " + (Time.realtimeSinceStartup - start).ToString("F0", CultureInfo.InvariantCulture) + " s");
            P((Me.Hp <= Me.MaxHp ? "PASS" : "FAIL") + " HP is within the new maximum: " + Me.Hp + " / " + Me.MaxHp + ", was " + hpMax);
            P((!window || GameSession.Hero.View.PetId == null ? "PASS" : "FAIL") + " it is no longer drawn on the owner's screen");
            if (window) { yield return new WaitForSeconds(0.5f); yield return Shot(shotPath, "wear-out-self-2"); }
            P("worn-out " + PersistLine());
            while (true) yield return new WaitForSeconds(2f);
        }

        /// <summary>In a window, fighting beside --partner: sees its pet, then sees it vanish without either hero leaving.</summary>
        IEnumerator PetWatch(string partner)
        {
            yield return WalkOutOfTown();
            System.IO.Directory.CreateDirectory(shotPath);
            Camera.main.orthographicSize = 42f;
            EnableNavBot(false, BotOffset(GameSession.Hero.PlayerName));
            float start = Time.realtimeSinceStartup;
            yield return TUntil(() => HeroNamed(partner) != null && HeroNamed(partner).View.PetId != null, 60f, "see the pet of " + partner);
            P((HeroNamed(partner) != null && HeroNamed(partner).View.PetId != null ? "PASS" : "FAIL") + " sees " + partner + " with a pet");
            yield return Shot(shotPath, "watch-before");
            start = Time.realtimeSinceStartup;
            while ((HeroNamed(partner) == null || HeroNamed(partner).View.PetId != null) && Time.realtimeSinceStartup - start < 240f) yield return null;
            HeroController other = HeroNamed(partner);
            P((other != null && other.View.PetId == null ? "PASS" : "FAIL") + " sees the pet vanish on " + partner + ", who is still here");
            yield return Shot(shotPath, "watch-after");
            P("watch done");
            yield return new WaitForSeconds(0.5f);
            Application.Quit();
        }

        /// <summary>One whole trade by the buttons' requests: the initiator asks; each adds its goods; both lock; if `finish`, both confirm. The side that does not finish stays with the window open, for the script to kill.</summary>
        IEnumerator PersistTrade(string partner, string giveUid, int giveGold, int getGold, bool asks, bool finish)
        {
            yield return TUntil(() => HeroNamed(partner) != null, 40f, "see " + partner);
            if (HeroNamed(partner) == null) { P("FAIL partner never seen"); yield break; }
            ushort other = HeroNamed(partner).PlayerId;
            int gold0 = Me.Gold;
            if (asks)
            {
                yield return new WaitForSeconds(1f);
                Send(TradeOp.Request, target: other);
            }
            else
            {
                yield return TUntil(() => Tr.Incoming.HasValue, 20f, "request arrived");
                Send(TradeOp.Accept, target: other);
            }
            yield return TUntil(() => Tr.Session.HasValue, 10f, "window open");
            if (!Tr.Session.HasValue) { P("FAIL no window " + OfferLine()); yield break; }
            yield return new WaitForSeconds(0.5f);
            if (giveUid != null) { Send(TradeOp.AddItem, uid: giveUid); yield return new WaitForSeconds(0.5f); }
            Send(TradeOp.SetGold, gold: giveGold);
            yield return TUntil(() => Tr.Session.HasValue && Tr.Session.Value.Mine.Gold == giveGold && Tr.Session.Value.Theirs.Gold == getGold, 10f, "both offers up");
            yield return new WaitForSeconds(0.5f);
            string tradeShots = NetLaunchArgs.Arg(Environment.GetCommandLineArgs(), "--trade-shots");
            if (tradeShots != null)
            {
                System.IO.Directory.CreateDirectory(tradeShots);
                yield return Shot(tradeShots, "trade-" + (asks ? "seller" : "buyer"));
                TradeWindow.TestInspectFirst();
                yield return new WaitForSeconds(0.3f);
                yield return Shot(tradeShots, "trade-inspect-" + (asks ? "seller" : "buyer"));
            }
            Send(TradeOp.Lock);
            yield return TUntil(() => Tr.Session.HasValue && Tr.Session.Value.Mine.Locked && Tr.Session.Value.Theirs.Locked, 10f, "both locked");
            P("locked " + PersistLine());
            if (!finish)
            {
                P("window stays open");
                while (true) yield return new WaitForSeconds(2f);
            }
            string[] args = Environment.GetCommandLineArgs();
            string confirmFile = NetLaunchArgs.Arg(args, "--confirm-file");
            if (confirmFile != null)
            {
                P("waiting to confirm");
                while (!System.IO.File.Exists(confirmFile)) yield return null;
            }
            Send(TradeOp.Confirm);
            if (Array.IndexOf(args, "--expect-refused") >= 0)
            {
                int bag = Me.Inventory.Count;
                yield return TUntil(() => Tr.Session.HasValue && !Tr.Session.Value.Mine.Locked, 20f, "the locks came off");
                yield return new WaitForSeconds(1f);
                P((Tr.Session.HasValue && Me.Gold == gold0 && Me.Inventory.Count == bag ? "PASS" : "FAIL") + " refused, window open, nothing moved");
                P("REFUSED " + PersistLine());
                while (true) { yield return new WaitForSeconds(2f); P("idle " + PersistLine()); }
            }
            yield return TUntil(() => !Tr.Session.HasValue, 15f, "trade over");
            yield return new WaitForSeconds(1f);
            P((Me.Gold == gold0 - giveGold + getGold ? "PASS" : "FAIL") + " gold after the trade " + gold0 + " -" + giveGold + " +" + getGold + " = " + Me.Gold);
            P("TRADE-DONE " + PersistLine());
            while (true) { yield return new WaitForSeconds(2f); P("idle " + PersistLine()); }
        }
    }
}
