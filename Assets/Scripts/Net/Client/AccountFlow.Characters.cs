using System;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using ZenoniaProto.Api;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>The character calls of AccountFlow: list, create (with the locally kept look), delete, enter, and the random-name dice.</summary>
    public static partial class AccountFlow
    {
        static readonly string[] NameStarts = { "Ka", "Lo", "Mi", "Ra", "Ze", "Th", "Vel", "Dar", "Eli", "Fen", "Gar", "Hol", "Jor", "Kir", "Lun", "Mor", "Nyx", "Or", "Pyr", "Sol", "Tor", "Ul", "Wyn", "Yor" };
        static readonly string[] NameEnds = { "a", "en", "ia", "or", "us", "ix", "ara", "eth", "ion", "dor", "wyn", "mir", "ran", "tis", "vok", "lin", "gar", "sel", "dus", "ria" };

        /// <summary>A pronounceable name for the dice button: letters only, 4 to 10 long, so it always passes NameProblem. No server call: a name that is taken still comes back as name_taken.</summary>
        public static string RandomName()
        {
            for (int attempt = 0; attempt < 20; attempt++)
            {
                var sb = new StringBuilder(NameStarts[UnityEngine.Random.Range(0, NameStarts.Length)]);
                if (UnityEngine.Random.value < 0.4f) sb.Append(NameStarts[UnityEngine.Random.Range(0, NameStarts.Length)].ToLowerInvariant());
                sb.Append(NameEnds[UnityEngine.Random.Range(0, NameEnds.Length)]);
                string name = sb.ToString();
                if (name.Length >= 4 && name.Length <= 10 && NameProblem(name) == null) return name;
            }
            return "Kalen"; // unreachable in practice: every start + end pair is already 3 to 9 letters
        }

        /// <summary>Bumped each time Characters was reloaded from the server: the list screen redraws when it changes.</summary>
        public static int ListVersion { get; private set; }

        public static async void Refresh()
        {
            if (Busy) return;
            Busy = true;
            try { await RefreshNow(); }
            catch (Exception e) { Fail(e); }
            finally { Busy = false; }
        }

        static async Task RefreshNow()
        {
            ApiResult<CharacterListResponse> r = await api.ListCharactersAsync();
            if (r.Ok)
            {
                Characters.Clear();
                if (r.Data.characters != null) Characters.AddRange(r.Data.characters);
                ListVersion++;
                return;
            }
            Say(Friendly(r), true);
            BackToLoginIfLoggedOut();
        }

        /// <summary>
        /// Creates the hero with the look picked on the Customize page: server-api stores it as state.appearance, so the zone shows it on every map. null look = the name-hash default.
        /// Nothing is kept on the device any more (CharacterLookPrefs only migrates heroes made before 19/11/2026).
        /// </summary>
        public static async void Create(string name, HeroClass id, CharacterAppearanceData look)
        {
            if (Busy) return;
            string problem = NameProblem(name);
            if (problem != null) { Say(problem, true); return; }
            Busy = true; Say("Creating your hero ...", false);
            try
            {
                int slot = PendingSlot >= 0 ? PendingSlot : FirstFreeSlot();
                ApiResult<CharacterDetailResponse> r = await api.CreateCharacterAsync(name.Trim(), RpgSession.ClassId(id), look, slot);
                if (!r.Ok) { Say(Friendly(r), true); BackToLoginIfLoggedOut(); return; }
                Say("", false);
                PendingSlot = -1;
                Page = AccountPage.Characters;
                await RefreshNow();
            }
            catch (Exception e) { Fail(e); }
            finally { Busy = false; }
        }

        /// <summary>Deletes the hero on the ConfirmDelete page, once its name has been typed. A refusal stays on that page with the reason.</summary>
        public static void Delete(string typedName)
        {
            if (Busy) return;
            string problem = DeleteProblem(typedName);
            if (problem != null) { Say(problem, true); return; }
            DeleteNow();
        }

        /// <summary>The character select screen: the player tapped DELETE and then held the confirm button (two steps, no name to type). A refusal shows on the list.</summary>
        public static void DeleteHeld(CharacterSummary c)
        {
            if (Busy || c == null) return;
            Deleting = c;
            DeleteNow();
        }

        static async void DeleteNow()
        {
            Busy = true; Say("Deleting ...", false);
            try
            {
                string deletedId = Deleting.id;
                ApiResult<ApiOk> r = await api.DeleteCharacterAsync(deletedId);
                if (!r.Ok && r.Code != ApiErrorCodes.NotFound) { Say(Friendly(r), true); BackToLoginIfLoggedOut(); return; }
                CharacterLookPrefs.Remove(deletedId); // gone on the server (or already gone): its look is no use any more
                Say("", false);
                Deleting = null;
                Page = AccountPage.Characters;
                await RefreshNow();
            }
            catch (Exception e) { Fail(e); }
            finally { Busy = false; }
        }

        /// <summary>Ticket, then the zone. Right after leaving, the zone is still writing the character out, so already_online is waited out for a moment. The result shows as NetClient's state.</summary>
        public static async void Enter(CharacterSummary c)
        {
            if (Busy) return;
            Busy = true; Say("Entering the world ...", false);
            try
            {
                for (int attempt = 1; ; attempt++)
                {
                    ApiResult<EnterResponse> r = await api.EnterAsync(c.id);
                    if (r.Ok)
                    {
                        Say("", false);
                        enteredId = c.id; // before the join: the local hero reads it when it spawns
                        GameSession.Instance.JoinServer(r.Data.zoneHost, r.Data.zonePort, c.name, GameSession.ClassIndex(c.classId), r.Data.ticket);
                        return;
                    }
                    if (r.Code == ApiErrorCodes.AlreadyOnline && attempt < EnterAttempts) { Say("Your last session is still being saved ...", false); await Task.Delay(EnterRetryMs); continue; }
                    Say(Friendly(r), true);
                    BackToLoginIfLoggedOut();
                    return;
                }
            }
            catch (Exception e) { Fail(e); }
            finally { Busy = false; }
        }
    }
}
