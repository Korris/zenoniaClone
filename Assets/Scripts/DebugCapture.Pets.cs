using System;
using UnityEngine;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// Capture flags for Pet Companion + pet gacha (account pets; capture runs never save, so the account is a throwaway one):
    ///   --pet &lt;id&gt;            unlock that pet for the account and take it along (spirit, imp, frostSlime ... frostfangKing)
    ///   --give-pets            unlock all 8 pets;  --pet-stars N   every owned pet at N stars
    ///   --pet-tickets N        N Gacha Coins;  --pet-crystals N;  --pet-food N;  --pet-balls N;  --rare-balls N;  --shared-shards N;  --ur-shards N   N UR shards
    ///   --pet-level N          every owned pet at level N;  --pet-team a,b,c,d,e   the five team slots by pet id ("" = empty);  --hp-pct N   hero at N % HP (guards trigger under 70 %)
    ///   --pity SSR,UR          pity counters (e.g. 0,49: the next pull is the UR guarantee);  --pet-cd MS   the pet skill starts on this cooldown
    ///   --talk                 talk to the nearest villager (Lumi: the gacha);  --open-pets [team|collection|growth|capture]  the Pet window on that tab;  --pet-select id  the species it shows
    ///   --open-gacha [rates|history]  the gacha window on that tab;  --gacha-pull 1|10  pull before the shot
    /// </summary>
    public partial class DebugCapture
    {
        static bool Flag(string key) { return Array.IndexOf(Environment.GetCommandLineArgs(), key) >= 0; }
        static string Val(string key) { return Arg(Environment.GetCommandLineArgs(), key); }

        static void DebugPets(PlayerState p, string petArg)
        {
            PetConfig.LogCycles = Flag("--pet-log");
            PlayerRpg owner = RpgSession.Owner;
            if (owner == null || owner.Account == null) return;
            string stars = Val("--pet-stars"), tickets = Val("--pet-tickets"), urShards = Val("--ur-shards"), pity = Val("--pity"), level = Val("--pet-level"), team = Val("--pet-team");
            string crystals = Val("--pet-crystals"), food = Val("--pet-food"), balls = Val("--pet-balls"), rareBalls = Val("--rare-balls"), shared = Val("--shared-shards");
            bool all = Flag("--give-pets");
            if (petArg == null && !all && stars == null && tickets == null && urShards == null && pity == null && level == null && team == null && crystals == null && food == null && balls == null && rareBalls == null && shared == null) return;
            owner.Account.Run("capture", a =>
            {
                foreach (PetDef d in PetConfig.All)
                    if ((all || d.Id == petArg) && !a.OwnsPet(d.Id)) a.pets.Add(new OwnedPet { id = d.Id, stars = 1 });
                if (stars != null) foreach (OwnedPet o in a.pets) o.stars = Mathf.Clamp(int.Parse(stars), 1, PetConfig.MaxStars);
                if (tickets != null) a.tickets = int.Parse(tickets);
                if (urShards != null) a.urShards = int.Parse(urShards);
                if (level != null) foreach (OwnedPet o in a.pets) o.level = Mathf.Clamp(int.Parse(level), 1, PetRules.MaxLevel);
                if (crystals != null) a.crystals = int.Parse(crystals);
                if (food != null) a.food = int.Parse(food);
                if (balls != null) a.balls = int.Parse(balls);
                if (rareBalls != null) a.rareBalls = int.Parse(rareBalls);
                if (shared != null) a.sharedShards = int.Parse(shared);
                if (pity != null) { string[] v = pity.Split(','); a.pitySSR = int.Parse(v[0]); a.pityUR = int.Parse(v[v.Length - 1]); }
                return ActionResult.Success("");
            }, r => { });
            p.AttachAccount(owner.Account.Live);
            if (petArg != null) p.TakePet(petArg);
            if (team != null) { string[] ids = team.Split(','); for (int i = 0; i < ids.Length && i < PetRules.TeamSize; i++) if (ids[i] != "" && PetConfig.Get(ids[i]) != null) p.SetTeamSlot(i, ids[i]); }
            p.Hp = p.MaxHp;
            string hpPct = Val("--hp-pct");
            if (hpPct != null) p.Hp = Mathf.Max(1f, p.MaxHp * int.Parse(hpPct) / 100f);
            RpgSession.SyncGear();
            string cd = Val("--pet-cd");
            if (cd != null && GameSession.Hero != null) GameSession.Hero.DebugPetCooldown(float.Parse(cd, System.Globalization.CultureInfo.InvariantCulture));
        }

        /// <summary>After spawning: the pet / gacha windows, and a pull so the result panel has something to show.</summary>
        static void DebugPetWindows()
        {
            if (Flag("--talk")) TownNpc.Talk(); // what E does beside a villager (Lumi in Thatchwick opens the pet gacha)
            if (Flag("--open-pets"))
            {
                PetWindow.Open();
                string sel = Val("--pet-select");
                if (sel != null && PetConfig.Get(sel) != null) PetWindow.Select(PetConfig.IndexOf(PetConfig.Get(sel))); // after Open, which starts on the combat pet
                string petTab = Val("--open-pets");
                if (petTab == "collection") PetWindow.ShowTab(PetWindow.Tab.Collection);
                else if (petTab == "growth") PetWindow.ShowTab(PetWindow.Tab.Growth);
                else if (petTab == "capture") PetWindow.ShowTab(PetWindow.Tab.Capture);
                else PetWindow.ShowTab(PetWindow.Tab.Team);
            }
            if (!Flag("--open-gacha")) return;
            PetGachaWindow.Open();
            string tab = Val("--open-gacha");
            if (tab == "rates") PetGachaWindow.ShowTab(PetGachaWindow.Tab.Rates);
            else if (tab == "history") PetGachaWindow.ShowTab(PetGachaWindow.Tab.History);
            string pull = Val("--gacha-pull");
            if (pull != null) PetGachaWindow.TestPull(int.Parse(pull));
        }
    }
}
