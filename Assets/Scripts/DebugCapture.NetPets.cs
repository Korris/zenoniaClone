using System.Collections;
using System.Globalization;
using UnityEngine;
using ZenoniaProto.Net;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// Online e2e roles for Pet Companion + pet gacha (Tools/net-persist-test.ps1, --persist-role ...): pets are account data since 16/11/2026, so these check the
    /// account answers (unlock, take along, stars, gacha pull) instead of the old item pets. The old durability roles (pet-drain / pet-wearout / pet-watch) are gone with durability.
    /// </summary>
    public partial class DebugCapture
    {
        const string PetSeller = "merchantDale";
        static int ImpIndex { get { return PetConfig.IndexOf(PetConfig.Get("imp")); } }
        static bool OwnsPet(string id) { return Me.Account != null && Me.Account.OwnsPet(id); }

        /// <summary>Waits for the answer to the request just sent: the server answers every inventory request with a state (account requests once stored).</summary>
        static IEnumerator AnsweredState()
        {
            int states = (int)NetStats.Current.Get("rpgStates");
            float start = Time.realtimeSinceStartup;
            while ((int)NetStats.Current.Get("rpgStates") == states && Time.realtimeSinceStartup - start < 8f) yield return null;
            yield return new WaitForSeconds(0.2f);
        }

        /// <summary>At the start of Oakhaven with gold for the Imp: the seller is out of reach. Then prints a free spot beside the seller for the script.</summary>
        IEnumerator PetFar()
        {
            int gold = Me.Gold;
            NetClient.SendInvAction(InvOp.PetBuy, PetSeller, (byte)ImpIndex);
            yield return AnsweredState();
            P((Me.Gold == gold && !OwnsPet("imp") ? "PASS" : "FAIL") + " out of reach of the seller nothing is sold");
            NetClient.SendInvAction(InvOp.PetBuy, "elderRowan", (byte)ImpIndex);
            yield return AnsweredState();
            P((Me.Gold == gold && !OwnsPet("imp") ? "PASS" : "FAIL") + " a villager who sells nothing sells nothing");
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

        /// <summary>Beside the seller: what must be refused, then the Imp unlocked for the account, taken along, its passive checked; a second purchase refused.</summary>
        IEnumerator PetBuy()
        {
            int gold = Me.Gold;
            float atk = Me.Atk;
            NetClient.SendInvAction(InvOp.PetBuy, "healerNina", (byte)ImpIndex);
            yield return AnsweredState();
            P((Me.Gold == gold && !OwnsPet("imp") ? "PASS" : "FAIL") + " the healer does not sell pets");
            NetClient.SendInvAction(InvOp.PetBuy, PetSeller, 0);
            yield return AnsweredState();
            P((Me.Gold == gold ? "PASS" : "FAIL") + " Spirit is not for sale (level 5)");
            NetClient.SendInvAction(InvOp.PetBuy, PetSeller, (byte)ImpIndex);
            yield return AnsweredState();
            PetDef imp = PetConfig.Get("imp");
            P((OwnsPet("imp") && Me.Gold == gold - imp.Price ? "PASS" : "FAIL") + " unlocked the Imp for the account: gold " + gold + " -> " + Me.Gold);
            NetClient.SendInvAction(InvOp.PetEquip, "", PetRules.EncodeSlot(0, ImpIndex));
            yield return AnsweredState();
            P((Me.PetId == "imp" && Mathf.Approximately(Me.DamageAtk, (float)(atk * 1.03)) ? "PASS" : "FAIL") + " the Imp comes along: damage attack " + atk + " -> " + Me.DamageAtk);
            int after = Me.Gold;
            NetClient.SendInvAction(InvOp.PetBuy, PetSeller, (byte)ImpIndex);
            yield return AnsweredState();
            P((Me.Gold == after ? "PASS" : "FAIL") + " a second Imp is refused (one per account)");
            NetClient.SendInvAction(InvOp.BallBuy, PetSeller, 1); // V2: the same merchant sells Pet Balls (500 G, 5 a day, into the account)
            yield return AnsweredState();
            P((Me.Account != null && Me.Account.balls == 1 && Me.Account.ballsBoughtToday == 1 && Me.Gold == after - PetRules.BallPrice ? "PASS" : "FAIL") + " a Pet Ball: gold " + after + " -> " + Me.Gold + ", balls " + (Me.Account != null ? Me.Account.balls : -1));
            P("bought " + PersistLine());
            yield return new WaitForSeconds(1f);
            Application.Quit();
        }

        /// <summary>Unlocks the Imp and stays: the script kills the zone right after, before the periodic save (the account write is already stored).</summary>
        IEnumerator PetBuyStay()
        {
            int gold = Me.Gold;
            NetClient.SendInvAction(InvOp.PetBuy, PetSeller, (byte)ImpIndex);
            yield return AnsweredState();
            P((Me.Gold < gold && OwnsPet("imp") ? "PASS" : "FAIL") + " unlocked the imp");
            P("imp-bought " + PersistLine());
            while (true) yield return new WaitForSeconds(2f);
        }

        /// <summary>Takes along the first owned pet (another hero of the account unlocked it, or the migration did).</summary>
        IEnumerator PetWear()
        {
            yield return TUntil(() => Me.Account != null && Me.Account.pets.Count > 0, 15f, "the account's pets (a migration is written first)");
            OwnedPet first = Me.Account != null && Me.Account.pets.Count > 0 ? Me.Account.pets[0] : null;
            if (first == null) { P("FAIL the account owns no pet"); yield break; }
            NetClient.SendInvAction(InvOp.PetEquip, "", PetRules.EncodeSlot(0, PetConfig.IndexOf(PetConfig.Get(first.id))));
            yield return AnsweredState();
            P((Me.PetId == first.id ? "PASS" : "FAIL") + " " + Me.ClassId + " takes along the account's " + first.id);
            P("wearing " + PersistLine());
            yield return new WaitForSeconds(1f);
            Application.Quit();
        }

        /// <summary>Puts the pet away and takes it out again, slowly, for whoever is watching (pet-see).</summary>
        IEnumerator PetToggle()
        {
            yield return new WaitForSeconds(9f);
            string id = Me.PetId;
            NetClient.SendInvAction(InvOp.PetEquip, "", PetRules.EncodeSlot(0, -1));
            yield return AnsweredState();
            P((Me.PetId == "" ? "PASS" : "FAIL") + " put the pet away");
            yield return new WaitForSeconds(6f);
            NetClient.SendInvAction(InvOp.PetEquip, "", PetRules.EncodeSlot(0, PetConfig.IndexOf(PetConfig.Get(id))));
            yield return AnsweredState();
            P((Me.PetId == id ? "PASS" : "FAIL") + " took it out again");
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
            yield return Shot(shotPath, "pet-both");
            float start = Time.realtimeSinceStartup;
            while (other.View.PetId != null && Time.realtimeSinceStartup - start < 25f) yield return null;
            P((other.View.PetId == null ? "PASS" : "FAIL") + " sees the pet go when it is put away");
            yield return Shot(shotPath, "pet-off");
            start = Time.realtimeSinceStartup;
            while (other.View.PetId == null && Time.realtimeSinceStartup - start < 25f) yield return null;
            P((other.View.PetId == first ? "PASS" : "FAIL") + " sees it come back");
            yield return Shot(shotPath, "pet-back");
            P("see done");
            yield return new WaitForSeconds(0.5f);
            Application.Quit();
        }

        /// <summary>Settings "Other players' pets" off (--hide-other-pets): the partner's pet is known (replicated) but not drawn, the own pet still is. Saves a picture.</summary>
        IEnumerator PetSeeHidden(string partner)
        {
            yield return TUntil(() => HeroNamed(partner) != null && HeroNamed(partner).View != null && HeroNamed(partner).View.PetId != null, 40f, "see " + partner + "'s pet id");
            HeroController other = HeroNamed(partner);
            if (other == null) { P("FAIL no view of " + partner); yield break; }
            System.IO.Directory.CreateDirectory(shotPath);
            Camera.main.orthographicSize = 42f;
            yield return new WaitForSeconds(2f);
            P((other.View.PetId != null && HeroPet.DrawnAt(other) == null ? "PASS" : "FAIL") + " with other players' pets off, " + partner + "'s " + other.View.PetId + " is not drawn");
            P((HeroPet.DrawnAt(GameSession.Hero) != null ? "PASS" : "FAIL") + " my own pet is still drawn");
            yield return Shot(shotPath, "pet-hidden");
            P("see done");
            yield return new WaitForSeconds(0.5f);
            Application.Quit();
        }

        /// <summary>A x10 pull on the server (V2): 10 coins go, 10 records land in the history (pets or 5-shard results), a UR shard per non-UR pull, pity within 20 / 50; then coins cannot be bought without crystals.</summary>
        IEnumerator PetGachaPull()
        {
            yield return TUntil(() => Me.Account != null && Me.Account.tickets >= 10, 15f, "10 tickets (welcome gift)");
            AccountState a = Me.Account;
            if (a == null) { P("FAIL no account"); yield break; }
            int tickets = a.tickets, pulls = a.totalPulls, ur = a.urShards;
            NetClient.SendInvAction(InvOp.GachaPull, "", 10);
            yield return AnsweredState();
            a = Me.Account;
            int urs = 0, shardResults = 0, bad = 0;
            for (int i = System.Math.Max(0, a.history.Count - 10); i < a.history.Count; i++)
            {
                PullRecord r = a.history[i];
                PetDef d = PetConfig.Get(r.id);
                if (d == null) { if (r.shards == PetRules.SharedShardsPerPull) shardResults++; else bad++; }
                else if (d.Legacy) bad++;
                else if (d.Rarity == PetRarity.UR) urs++;
            }
            P((a.tickets == tickets - 10 && a.totalPulls == pulls + 10 && a.history.Count >= 10 ? "PASS" : "FAIL") + " x10: coins " + tickets + " -> " + a.tickets + ", pulls " + pulls + " -> " + a.totalPulls);
            P((bad == 0 && a.urShards == ur + 10 - urs && a.pitySSR < PetRules.SsrPity && a.pityUR < PetRules.UrPity ? "PASS" : "FAIL") + " records are pets or 5-shard results (" + shardResults + " shard results), UR shards " + ur + " -> " + a.urShards + ", pity " + a.pitySSR + "/" + a.pityUR);
            NetClient.SendInvAction(InvOp.GachaBuyCoins, "", 1);
            yield return AnsweredState();
            P((Me.Account.tickets == tickets - 10 && Me.Account.crystals == 0 ? "PASS" : "FAIL") + " a coin is refused without " + PetRules.CrystalsPerPull + " crystals");
            P("pulled " + PersistLine());
            yield return new WaitForSeconds(1f);
            Application.Quit();
        }
    }
}
