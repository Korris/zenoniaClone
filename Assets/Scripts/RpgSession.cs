using UnityEngine;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// Glue between the pure RPG state (ZenoniaProto.Rpg) and the running game: loads / creates the PlayerState of the chosen class,
    /// autosaves (every 30 s, on respawn, on boss kill, on class switch and on quit), turns kills into EXP + ground loot, applies pickups,
    /// handles the potion keys (1 / 2) and the inventory key (B), and keeps the hero's held gear in sync with the equipment.
    /// </summary>
    public class RpgSession : MonoBehaviour
    {
        const float AutosaveSeconds = 30f;
        static readonly Color ExpColor = new Color(0.75f, 0.6f, 1f), GoldColor = new Color(1f, 0.85f, 0.3f), LevelColor = new Color(1f, 0.9f, 0.4f);

        public static RpgSession Instance { get; private set; }
        /// <summary>The live RPG state of the current hero, or null on the class select screen.</summary>
        public static PlayerState Player { get { return Instance != null ? Instance.player : null; } }

        Transform root;
        PlayerState player;
        bool bossDefeated;
        float saveTimer;
        int beganFrame = -1; // keys 1 / 2 that picked the class on the select screen must not also drink a potion

        public static RpgSession Create(Transform parent)
        {
            var s = parent.gameObject.AddComponent<RpgSession>();
            s.root = parent;
            Instance = s;
            return s;
        }

        void OnDestroy() { if (Instance == this) Instance = null; }

        /// <summary>RPG class id ("warrior" ...) of a playable class.</summary>
        public static string ClassId(HeroClass id) { return id.ToString().ToLowerInvariant(); }

        /// <summary>Saves the current hero (if any), then loads the save of `id` or starts a new game. Returns the state for HeroController.Init.</summary>
        public PlayerState Begin(HeroClass id, out SaveData save)
        {
            SaveNow();
            string cid = ClassId(id);
            save = SaveStore.Load(cid);
            player = save != null ? SaveSerializer.Restore(save) : PlayerState.NewGame(cid);
            bossDefeated = save != null && save.bossDefeated;
            saveTimer = 0f;
            beganFrame = Time.frameCount;
            return player;
        }

        /// <summary>After the hero exists: restore the saved spot (when still walkable), remove a boss this save already killed, sync gear.</summary>
        public void Attach(HeroController hero, SaveData save)
        {
            if (save != null && save.hasPosition && !FrostHollowMap.RectBlocked(save.x - 5f, save.y, 10f, 8f)) hero.Teleport(new Vector2(save.x, save.y), 0);
            if (bossDefeated && Monster.Boss != null && Application.isPlaying) Destroy(Monster.Boss.gameObject);
            SyncGear();
            if (save == null) SaveNow(); // a new character gets its file right away
        }

        /// <summary>Writes the current hero to its class save (no-op without a hero, while dead, or when saving is disabled).</summary>
        public void SaveNow()
        {
            HeroController hero = GameSession.Hero;
            if (player == null || hero == null || hero.Rpg != player || hero.IsDead) return;
            Vector2 p = hero.Position;
            SaveStore.Write(SaveSerializer.Capture(player, true, p.x, p.y, bossDefeated));
            saveTimer = 0f;
        }

        /// <summary>Pushes the equipped weapon / offhand to the hero's paper doll (tier from item level, enhanced art from +7).</summary>
        public static void SyncGear()
        {
            HeroController hero = GameSession.Hero;
            PlayerState p = Player;
            if (hero == null || p == null) return;
            hero.View.SetEquipmentGear(p.GetEquipped(Slots.Weapon), p.GetEquipped(Slots.Offhand));
            hero.RefreshGearLabel();
        }

        void Update()
        {
            HeroController hero = GameSession.Hero;
            if (hero == null || player == null || hero.Rpg != player)
            {
                if (InventoryPanel.IsOpen) InventoryPanel.Close();
                return;
            }
            if (Time.frameCount == beganFrame) return;
            if (InputAdapter.Pressed(GameKey.Inventory) || (InventoryPanel.IsOpen && InputAdapter.Pressed(GameKey.Close))) InventoryPanel.Toggle(root);
            if (!hero.IsDead)
            {
                if (InputAdapter.Pressed(GameKey.HpPotion)) Drink(hero, true);
                if (InputAdapter.Pressed(GameKey.MpPotion)) Drink(hero, false);
            }
            saveTimer += Time.unscaledDeltaTime;
            if (saveTimer >= AutosaveSeconds) SaveNow();
        }

        void Drink(HeroController hero, bool hp)
        {
            ActionResult r = player.UsePotion(hp);
            if (!r.Ok) { RpgHud.Toast(r.Text, new Color(1f, 0.7f, 0.5f)); return; }
            DamageNumber.Spawn(hero.Position.x, hero.Position.y + 28f, r.Text, hp ? new Color(0.5f, 1f, 0.55f) : new Color(0.5f, 0.75f, 1f), false);
            UltimateFx.Embers(hero.Position + new Vector2(0f, 6f), 10, 6f, 30f, hp ? new Color(1f, 0.45f, 0.45f, 1f) : new Color(0.45f, 0.65f, 1f, 1f));
        }

        void OnApplicationQuit() { SaveNow(); }
        void OnApplicationPause(bool paused) { if (paused) SaveNow(); }

        // ---- hooks called by the game ----

        /// <summary>Monster.TakeHit on a kill: EXP (level-ups heal), then the drops scatter on the ground.</summary>
        public static void NotifyKill(Monster m)
        {
            RpgSession s = Instance;
            HeroController hero = GameSession.Hero;
            if (s == null || s.player == null || hero == null || hero.Rpg != s.player) return;
            MonsterLootDef def = LootTable.For(m.Type);
            if (def == null) return;
            Vector2 feet = m.Feet;
            int levels = s.player.GainExp(def.Exp);
            DamageNumber.Spawn(feet.x + 6f, feet.y + 20f, "+" + def.Exp + " EXP", ExpColor, false);
            if (levels > 0) s.LevelUp(hero);
            LootDrop.SpawnAll(s.root, LootTable.RollAll(def, RpgRandom.Default), feet);
            if (m.IsBoss) { s.bossDefeated = true; s.SaveNow(); }
        }

        void LevelUp(HeroController hero)
        {
            Vector2 feet = hero.Position;
            RpgHud.LevelUp(player.Level);
            ShockRing.Spawn(feet, 26f, 0.6f, new Color(1f, 0.85f, 0.35f, 0.95f), UltimateFx.GroundFxOrder + 6, 0.72f, 0.25f);
            ShockRing.Spawn(feet, 16f, 0.45f, new Color(1f, 1f, 0.7f, 0.85f), UltimateFx.GroundFxOrder + 7, 0.72f, 0.2f, true);
            UltimateFx.Embers(feet + new Vector2(0f, 6f), 36, 12f, 60f, new Color(1f, 0.9f, 0.45f, 1f));
            ScreenFlash.Trigger(new Color(1f, 0.95f, 0.6f, 0.08f), 0.3f);
            SaveNow();
        }

        /// <summary>LootDrop touched by the hero: applies it and shows the pickup text. Fails (stays on the ground) only for equipment with a full bag.</summary>
        public static ActionResult Pickup(LootEntry e)
        {
            PlayerState p = Player;
            HeroController hero = GameSession.Hero;
            if (p == null || hero == null) return ActionResult.Fail("");
            ActionResult r = p.ApplyPickup(e);
            if (!r.Ok) return r;
            Color c = e.Kind == LootKind.Coin ? GoldColor : e.Kind == LootKind.Equipment ? LootSprites.RarityColor(e.Item)
                : e.Kind == LootKind.UpgradeStone ? new Color(0.5f, 0.95f, 1f) : e.Kind == LootKind.HpPotion ? new Color(1f, 0.55f, 0.55f) : new Color(0.55f, 0.75f, 1f);
            DamageNumber.Spawn(hero.Position.x, hero.Position.y + 26f, r.Text, c, e.Kind == LootKind.Equipment);
            if (e.Kind == LootKind.Equipment)
            {
                string note = p.CanEquip(e.Item) ? "" : "  (" + RpgClasses.Get(e.Item.classId).Name + " gear)";
                RpgHud.Toast("Got " + EnhanceStats.DisplayName(e.Item) + note + "  [B] inventory", c);
            }
            return r;
        }

        /// <summary>HeroController.Respawn: shows the death penalty and saves the revived hero.</summary>
        public static void NotifyRespawn(int expLost)
        {
            if (Instance == null) return;
            if (expLost > 0) RpgHud.Toast("You were defeated: -" + expLost + " EXP", new Color(1f, 0.5f, 0.45f));
            Instance.SaveNow();
        }
    }
}
