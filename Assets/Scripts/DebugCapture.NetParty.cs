using System;
using System.Collections;
using System.IO;
using UnityEngine;
using ZenoniaProto.Net;

namespace ZenoniaProto
{
    /// <summary>
    /// --net-bot party --party-role leader|ally|stranger (the ally runs as "Allie": EnablePortalBot picks its portal by the name, and this one takes the near one): the end-to-end party script behind Tools/net-party-test.ps1. It sends the same requests the buttons send, and what comes back
    /// goes into the log as "party-e2e:" lines. Server flags: --dev-level (points to learn Heal), --dev-hurt (somebody to heal). --party-shots also saves PNGs of the party UI to --shot-path
    /// (needs a visible client). The last leg either walks the ally through a portal or, with --party-fight, has both fight side by side. Roles: the ally ignores the first invite and accepts every later one; the stranger declines the first and ignores the rest.
    /// </summary>
    public partial class DebugCapture
    {
        const int HealSkill = 0;
        float botSkillUntil;
        bool partyShots;

        static void E2e(string line) { Debug.Log("[ZenoniaProto] party-e2e: " + line); }

        void StartPartyBot()
        {
            string[] a = Environment.GetCommandLineArgs();
            partyShots = Array.IndexOf(a, "--party-shots") >= 0 && NetMode.RendersFx;
            StartCoroutine(LogHpEvery(4f));
            string role = NetLaunchArgs.Arg(a, "--party-role");
            if (role != null && (role.StartsWith("ui-") || role.StartsWith("joiner"))) StartPartyUiBot(role);
            else if (role == "leader") StartCoroutine(PartyLeader());
            else if (role == "ally") StartCoroutine(PartyAlly());
            else StartCoroutine(PartyStranger());
        }

        IEnumerator LogHpEvery(float seconds)
        {
            while (true)
            {
                HeroController h = GameSession.Hero;
                string members = "";
                foreach (PartyMemberInfo m in NetClient.Party.Members) members += " " + m.Name + "=" + m.Hp + "/" + m.MaxHp + "@" + m.MapId;
                if (h != null) E2e("hp " + Mathf.CeilToInt(h.Hp) + "/" + Mathf.CeilToInt(h.MaxHp) + " exp " + h.Rpg.Exp + " map " + MapRegistry.Current.Id + " party[" + members + " ]");
                yield return new WaitForSeconds(seconds);
            }
        }

        IEnumerator Until(Func<bool> done, float timeout, string what)
        {
            float start = Time.realtimeSinceStartup;
            while (!done() && Time.realtimeSinceStartup - start < timeout) yield return null;
            E2e((done() ? "OK " : "TIMEOUT ") + what + " after " + (Time.realtimeSinceStartup - start).ToString("F1") + " s");
        }

        IEnumerator PartyShot(string name)
        {
            if (!partyShots) yield break;
            yield return new WaitForEndOfFrame();
            Directory.CreateDirectory(shotPath);
            Save(Path.Combine(shotPath, name + ".png"));
        }

        static HeroController HeroNamed(string name)
        {
            foreach (HeroController h in Zone.Main.Heroes.All) if (h != null && h.PlayerName == name) return h;
            return null;
        }

        IEnumerator CastHeal(string why)
        {
            E2e("cast Heal: " + why);
            InputAdapter.BotPressed = k => k == GameKey.Skill1 + HealSkill && Time.time < botSkillUntil;
            botSkillUntil = Time.time + 0.3f;
            yield return new WaitForSeconds(6f); // the 5 s cooldown, and time for the HP lines
        }

        IEnumerator PartyLeader()
        {
            PartyClientState party = NetClient.Party;
            yield return Until(() => HeroNamed("Allie") != null && HeroNamed("Stranger") != null, 40f, "see Allie and Stranger");
            ushort ally = HeroNamed("Allie").PlayerId, stranger = HeroNamed("Stranger").PlayerId;
            NetClient.SendInvAction(InvOp.SkillLearn, "", HealSkill);
            yield return Until(() => GameSession.Hero.Rpg.Skills[HealSkill] > 0, 10f, "learned Heal");

            yield return CastHeal("no party yet, the leader is at full HP, so nobody may be healed");

            NetClient.SendParty(PartyOp.Invite, ally);
            yield return Until(() => party.Outgoing.HasValue, 5f, "invite to Ally pending");
            yield return PartyShot("leader-pending");
            yield return new WaitForSeconds(3f);
            NetClient.SendParty(PartyOp.CancelInvite);
            yield return Until(() => !party.Outgoing.HasValue, 5f, "invite cancelled");
            yield return new WaitForSeconds(2f);

            NetClient.SendParty(PartyOp.Invite, ally);
            yield return Until(() => party.InParty, 8f, "Ally accepted, in a party");
            yield return new WaitForSeconds(1f);
            yield return PartyShot("leader-party");

            yield return CastHeal("Ally is in the party, Stranger is not");

            NetClient.SendParty(PartyOp.Invite, ally); // refused: already a member
            yield return new WaitForSeconds(1f);
            if (partyShots) { PartyPlayerMenu.OpenFor(HeroNamed("Stranger"), new Vector2(Screen.width * 0.5f, Screen.height * 0.5f)); yield return new WaitForSeconds(0.3f); yield return PartyShot("leader-menu"); }
            NetClient.SendParty(PartyOp.Invite, stranger); // declined
            yield return new WaitForSeconds(3f);
            NetClient.SendParty(PartyOp.Invite, stranger); // left to run out
            yield return new WaitForSeconds(0.5f);
            NetClient.SendParty(PartyOp.Invite, stranger); // refused: already out
            yield return Until(() => !party.Outgoing.HasValue, 34f, "invite to Stranger ran out");

            NetClient.SendParty(PartyOp.Kick, ally);
            yield return Until(() => !party.InParty, 5f, "kicked Ally, party ended with one member");
            yield return CastHeal("Ally is out of the party again");

            NetClient.SendParty(PartyOp.Invite, ally);
            yield return Until(() => party.InParty, 8f, "Ally back in the party");
            if (NetLaunchArgs.HasFlag("--party-fight"))
            {
                E2e("fighting together: the server log has the EXP splits");
                EnableNavBot(false, BotOffset(NetLaunchArgs.Name));
                yield return Until(() => !party.InParty, 90f, "Ally went offline, party ended");
                Application.Quit();
                yield break;
            }
            yield return Until(() => party.Members.Count == 2 && party.Members[1].MapId != party.Members[0].MapId, 150f, "Ally shows on another map, still in the party");
            EnableNavBot(false, BotOffset(NetLaunchArgs.Name)); // kills here pay the leader alone
            yield return Until(() => !party.InParty, 110f, "Ally went offline, party ended");
            E2e("leader done");
            Application.Quit();
        }

        IEnumerator PartyAlly()
        {
            PartyClientState party = NetClient.Party;
            yield return Until(() => party.Incoming.HasValue, 60f, "first invite shows");
            yield return new WaitForSeconds(1f);
            yield return PartyShot("ally-invite");
            yield return Until(() => !party.Incoming.HasValue, 8f, "the leader's cancel closed the window");
            yield return Until(() => party.Incoming.HasValue, 15f, "second invite shows");
            NetClient.SendParty(PartyOp.Accept, party.Incoming.Value.Other);
            yield return Until(() => party.InParty, 5f, "accepted, in a party");
            yield return new WaitForSeconds(1f);
            yield return PartyShot("ally-party");
            yield return Until(() => !party.InParty, 80f, "kicked");
            yield return Until(() => party.Incoming.HasValue, 15f, "invited back");
            NetClient.SendParty(PartyOp.Accept, party.Incoming.Value.Other);
            yield return Until(() => party.InParty, 5f, "back in the party");
            if (NetLaunchArgs.HasFlag("--party-fight"))
            {
                EnableNavBot(false, BotOffset(NetLaunchArgs.Name));
                yield return new WaitForSeconds(60f);
            }
            else
            {
                string startMap = MapRegistry.Current.Id;
                E2e("walking to a portal");
                EnablePortalBot();
                yield return Until(() => MapRegistry.Current.Id != startMap, 100f, "through a portal, on " + startMap + " before");
                InputAdapter.BotHeld = null; InputAdapter.BotPressed = null;
                yield return new WaitForSeconds(90f);
            }
            E2e("ally goes offline, party size " + party.Members.Count);
            Application.Quit();
        }

        IEnumerator PartyStranger()
        {
            PartyClientState party = NetClient.Party;
            yield return Until(() => party.Incoming.HasValue, 90f, "invite shows");
            yield return new WaitForSeconds(1f);
            NetClient.SendParty(PartyOp.Decline, party.Incoming.Value.Other);
            yield return Until(() => !party.Incoming.HasValue, 5f, "declined");
            yield return Until(() => party.Incoming.HasValue, 15f, "second invite shows, not answering");
            yield return Until(() => !party.Incoming.HasValue, 34f, "second invite ran out");
            E2e("stranger stays out of it");
        }
    }
}
