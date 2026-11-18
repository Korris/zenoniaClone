using System;
using System.Collections.Generic;
using UnityEngine;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// Capture flags of the cosmetic hats (HeadgearCatalog), offline after spawning (capture runs never save):
    ///   --give-hats 1,6,10                put these hat ids in the bag
    ///   --wear-hat N                      wear hat N (the equipment cell, the look's headgear id)
    ///   --drop-hat-from TYPE              one kill's loot of monster TYPE through the real roll (LootTable.RollAll / Wanted hats) with every roll forced to hit,
    ///                                     spilled in front of the hero, like KillRewards does
    ///   --newbie-quest offer|ready|done   the Elder's First Hunt: the offer window / the hand-in talk after 5 kills / handed in (the gift in the bag)
    ///   --shop-tab hats                   with --open-shop SMITH: the Hats tab of the blacksmith
    /// </summary>
    public partial class DebugCapture
    {
        /// <summary>Before the bag opens (ApplyRpgFlags): bag contents, worn hat, ground loot, quest state.</summary>
        void DebugHeadgear(HeroController hero)
        {
            string[] a = Environment.GetCommandLineArgs();
            PlayerState p = hero.Rpg;
            string give = Arg(a, "--give-hats");
            if (give != null)
                foreach (string s in give.Split(','))
                    p.AddToInventory(HeadgearCatalog.CreateItem(HeadgearCatalog.Get(int.Parse(s)), RpgRandom.Default));
            string wear = Arg(a, "--wear-hat");
            if (wear != null)
            {
                Item hat = HeadgearCatalog.CreateItem(HeadgearCatalog.Get(int.Parse(wear)), RpgRandom.Default);
                p.AddToInventory(hat);
                Debug.Log("[ZenoniaProto] wear hat " + wear + ": " + p.Equip(hat).Ok + ", look headgear " + p.Appearance.equippedHeadgearId);
                RpgSession.SyncGear();
            }
            string from = Arg(a, "--drop-hat-from");
            if (from != null) DropHatLoot(hero, from);
            string quest = Arg(a, "--newbie-quest");
            if (quest != null) NewbieQuest(hero, quest);
        }

        /// <summary>After the windows opened: the blacksmith's Hats tab.</summary>
        static void DebugHeadgearWindows()
        {
            if (Arg(Environment.GetCommandLineArgs(), "--shop-tab") == "hats") GearShopWindow.TestShowHats();
        }

        static void DropHatLoot(HeroController hero, string type)
        {
            Rand always = () => 0.0; // every roll hits: shows the drop, not its odds
            var drops = new List<LootEntry>();
            MonsterLootDef def = LootTable.For(type);
            if (def != null && !def.IsBoss) drops.AddRange(LootTable.RollAll(def, always));
            foreach (Item hat in HeadgearCatalog.RollWantedDrops(type, always)) drops.Add(new LootEntry { Kind = LootKind.Equipment, Item = hat });
            Debug.Log("[ZenoniaProto] --drop-hat-from " + type + ": " + drops.Count + " drops, hats " + drops.FindAll(e => e.Item != null && e.Item.slot == Slots.Headgear).Count);
            LootDrop.SpawnAll(GameObject.Find("ZenoniaProto").transform, drops, hero.Position + new Vector2(34f, 4f)); // beside the hero, on screen whatever he faces
        }

        static void NewbieQuest(HeroController hero, string step)
        {
            QuestDef q = QuestCatalog.Get("first-hunt");
            PlayerRpg rpg = LocalPlayer.Rpg;
            if (step == "offer") { QuestOfferWindow.Open(q, null); return; }
            hero.Rpg.AcceptQuest(q.Id);
            for (int i = 0; i < q.Count; i++) rpg.QuestKill("hornRabbit");
            if (step == "ready") { ElderNpc.Talk(q.GiverNpcId); return; }
            ActionResult r = rpg.ApplyInvAction(Net.InvOp.QuestTurnIn, q.Id, 0); // what QuestTracker.TurnIn does offline
            Debug.Log("[ZenoniaProto] newbie quest turn-in: " + r.Ok + " " + r.Text + ", hat in bag: " + hero.Rpg.Inventory.Exists(i => i.slot == Slots.Headgear));
        }
    }
}
