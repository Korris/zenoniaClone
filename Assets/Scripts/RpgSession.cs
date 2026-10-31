using System.Collections.Generic;
using UnityEngine;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// Offline adapter around one PlayerRpg: loads / creates the PlayerState of the chosen class, autosaves (every 30 s, on respawn, on level-up,
    /// on boss kill, on class switch and on quit), turns the PlayerRpg events (EXP, pickups, death penalty) into toasts / floating text / FX,
    /// handles the potion keys (1 / 2) and the inventory key (B), and keeps the hero's held gear in sync with the equipment.
    /// Kill rewards themselves are computed by the Zone (KillRewards); online there is no RpgSession, the server owns the PlayerRpg.
    /// </summary>
    public class RpgSession : MonoBehaviour
    {
        const float AutosaveSeconds = 30f;
        static readonly Color ExpColor = new Color(0.75f, 0.6f, 1f), GoldColor = new Color(1f, 0.85f, 0.3f);

        public static RpgSession Instance { get; private set; }
        /// <summary>The live RPG state of the current hero, or null on the class select screen.</summary>
        public static PlayerState Player { get { return Instance != null ? Instance.player : null; } }

        /// <summary>
        /// This save already killed the boss of `mapId` (offline a boss does not return): MapTransfer removes it when the map is rebuilt. A boss gate map opens its forward portal on this
        /// too. Maps with a boss of their own are listed by id, every other map shares the Frostfang King's flag.
        /// </summary>
        public static bool BossDefeatedOn(string mapId)
        {
            if (Instance == null) return false;
            return MapSpawnTable.HasOwnBoss(mapId) ? Instance.defeatedBossMaps.Contains(mapId) : Instance.bossDefeated;
        }

        Transform root;
        PlayerState player;
        PlayerRpg owner;
        bool bossDefeated;
        List<string> defeatedBossMaps = new List<string>();
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
            DevStartConfig.ApplySkillBonus();
            player = save != null ? SaveSerializer.Restore(save) : PlayerState.NewGame(cid);
            DevStartConfig.Apply(player); // Resources/Data/dev-start.json: test stones / gold / potions (editor + dev builds)
            bossDefeated = save != null && save.bossDefeated;
            defeatedBossMaps = save != null ? new List<string>(save.defeatedBossMaps) : new List<string>();
            // A save already past a gate (older saves walked to Sunspire before the Den existed) counts as having opened it, so it can never be shut out going back.
            string passed = save != null ? MapSpawnTable.GatePassedOn(save.mapId) : null;
            if (passed != null && !defeatedBossMaps.Contains(passed)) defeatedBossMaps.Add(passed);
            saveTimer = 0f;
            beganFrame = Time.frameCount;
            owner = new PlayerRpg(player);
            owner.ExpGained += OnExpGained;
            owner.LootPicked += OnLootPicked;
            owner.Revived += OnRevived;
            owner.BossKilled += OnBossKilled;
            return player;
        }

        /// <summary>After the hero exists: bind its PlayerRpg, restore the saved spot (when still walkable), remove a boss this save already killed, sync gear.</summary>
        public void Attach(HeroController hero, SaveData save)
        {
            hero.Owner = owner;
            RestoreMapAndPosition(hero, save);
            string here = MapRegistry.Current.Id;
            if (BossDefeatedOn(here) && NetMode.IsOffline && Monster.Boss != null && Application.isPlaying) Destroy(Monster.Boss.gameObject);
            // F1..F5 on the same map: another class's save may still owe this boss, and its gate stays shut until the boss can be killed.
            else if (!BossDefeatedOn(here) && MapSpawnTable.HasOwnBoss(here) && NetMode.IsOffline && Application.isPlaying && Zone.Main != null && Zone.Main.Spawner != null) Zone.Main.Spawner.SpawnBoss();
            SyncGear();
            if (save == null) SaveNow(); // a new character gets its file right away
        }

        /// <summary>
        /// Offline: a saved hero resumes on the map it was saved on (older saves have no village and were in Frost Hollow), a new character starts in the
        /// village; `--map` and online play keep the current map. The saved spot is used only on its own map and only while still walkable.
        /// </summary>
        void RestoreMapAndPosition(HeroController hero, SaveData save)
        {
            string savedMap = save != null && save.hasPosition ? (string.IsNullOrEmpty(save.mapId) ? FrostHollowLayout.MapId : save.mapId) : null;
            string want = NetMode.IsOffline && !MapRegistry.Forced ? (savedMap ?? OakhavenVillageMap.MapId) : MapRegistry.Current.Id;
            bool switched = want != MapRegistry.Current.Id;
            if (switched) MapTransfer.Instant(want);
            else if (NetMode.IsOffline && MapSpawnTable.HasBossGate(want)) WorldBuilder.Rebuild(); // same map, another save: its gate may differ, so the portal lights do too
            if (savedMap == MapRegistry.Current.Id && !FrostHollowMap.RectBlocked(save.x - 5f, save.y, 10f, 8f)) hero.Teleport(new Vector2(save.x, save.y), 0);
            else if (switched) hero.Teleport(GameConfig.HeroStart, 0);
        }

        /// <summary>Online client: adopts the mirrored state the server pushed (the bag, equipment and HUD all read Player), re-binds the hero's owner and refreshes its gear.</summary>
        public void AttachRemote(PlayerState state, HeroController hero)
        {
            player = state;
            owner = new PlayerRpg(state);
            hero.ProxyReplaceState(state);
            hero.Owner = owner;
            beganFrame = Time.frameCount;
            SyncGear();
            InventoryPanel.RefreshIfOpen();
        }

        /// <summary>Online client: this hero's boss flags from the server's RpgState (what BossDefeatedOn answers, so the gated portal). The server decides; a gate that just opened shows at once.</summary>
        public void ApplyRemoteBossFlags(SaveData save)
        {
            string map = MapRegistry.Current.Id;
            bool wasOpen = BossDefeatedOn(map);
            bossDefeated = save.bossDefeated;
            defeatedBossMaps = new List<string>(save.defeatedBossMaps);
            if (!wasOpen && BossDefeatedOn(map) && MapSpawnTable.HasBossGate(map)) WorldBuilder.Rebuild();
        }

        /// <summary>Writes the current hero to its class save (no-op without a hero, while dead, or when saving is disabled).</summary>
        public void SaveNow()
        {
            HeroController hero = GameSession.Hero;
            if (player == null || hero == null || hero.Rpg != player || hero.IsDead) return;
            Vector2 p = hero.Position;
            SaveStore.Write(SaveSerializer.Capture(player, true, p.x, p.y, bossDefeated, 0, MapRegistry.Current.Id, defeatedBossMaps));
            saveTimer = 0f;
        }

        /// <summary>Pushes the equipped weapon / offhand to the hero's paper doll (tier from item level, enhanced art from +7).</summary>
        public static void SyncGear()
        {
            HeroController hero = GameSession.Hero;
            PlayerState p = Player;
            if (hero == null || p == null || hero.View == null) return;
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
            if (!hero.IsDead && NetMode.IsAuthority) // online clients send the potion keys to the server instead (NetInputSender)
            {
                if (InputAdapter.Pressed(GameKey.HpPotion)) Drink(hero, true);
                if (InputAdapter.Pressed(GameKey.MpPotion)) Drink(hero, false);
            }
            saveTimer += Time.unscaledDeltaTime;
            if (saveTimer >= AutosaveSeconds) SaveNow();
        }

        void Drink(HeroController hero, bool hp)
        {
            ActionResult r = owner.UsePotion(hp);
            if (!r.Ok) { RpgHud.Toast(r.Text, new Color(1f, 0.7f, 0.5f)); return; }
            HeroFx.Potion(hero.Position, hp, r.Text);
            NetEventBus.Potion(hero, hp); // host: the other players see the drink too
        }

        void OnApplicationQuit() { SaveNow(); }
        void OnApplicationPause(bool paused) { if (paused) SaveNow(); }

        // ---- PlayerRpg events -> presentation ----

        /// <summary>Kill EXP: floating "+N EXP", level-up celebration (which also saves).</summary>
        void OnExpGained(int exp, int levels, Vector2 at)
        {
            DamageNumber.Spawn(at.x + 6f, at.y + 20f, "+" + exp + " EXP", ExpColor, false);
            if (levels <= 0) return;
            HeroController hero = GameSession.Hero;
            if (hero != null) { HeroFx.LevelUp(hero.Position, player.Level); NetEventBus.LevelUp(hero, player.Level); }
            SaveNow();
        }

        /// <summary>LootDrop touched by the hero: shows the pickup text. A failed pickup (full bag) is silent here; LootDrop toasts it.</summary>
        void OnLootPicked(LootEntry e, ActionResult r)
        {
            HeroController hero = GameSession.Hero;
            if (!r.Ok || hero == null) return;
            Color c = e.Kind == LootKind.Coin ? GoldColor : e.Kind == LootKind.Equipment ? LootSprites.RarityColor(e.Item)
                : e.Kind == LootKind.UpgradeStone ? new Color(0.5f, 0.95f, 1f) : e.Kind == LootKind.HpPotion ? new Color(1f, 0.55f, 0.55f) : new Color(0.55f, 0.75f, 1f);
            DamageNumber.Spawn(hero.Position.x, hero.Position.y + 26f, r.Text, c, e.Kind == LootKind.Equipment);
            if (e.Kind != LootKind.Equipment) return;
            string note = player.CanEquip(e.Item) ? "" : "  (" + RpgClasses.Get(e.Item.classId).Name + " gear)";
            RpgHud.Toast("Got " + EnhanceStats.DisplayName(e.Item) + note + "  [B] inventory", c);
        }

        /// <summary>Respawn after death: shows the EXP penalty and saves the revived hero.</summary>
        void OnRevived(int expLost)
        {
            if (expLost > 0) RpgHud.Toast("You were defeated: -" + expLost + " EXP", new Color(1f, 0.5f, 0.45f));
            SaveNow();
        }

        /// <summary>Offline a boss never returns for this save: remember the kill right away. On a gate map the forward portal opens at once: its light appears and the toast says so.</summary>
        void OnBossKilled()
        {
            string map = MapRegistry.Current.Id;
            if (!MapSpawnTable.HasOwnBoss(map)) bossDefeated = true;
            else if (!defeatedBossMaps.Contains(map))
            {
                defeatedBossMaps.Add(map);
                if (MapSpawnTable.HasBossGate(map) && NetMode.IsOffline)
                {
                    WorldBuilder.Rebuild();
                    RpgHud.Toast("The way forward is open", new Color(0.5f, 0.95f, 1f));
                }
            }
            SaveNow();
        }

        // ---- compat for existing call sites ----

        /// <summary>Applies a pickup to the current hero's RPG state (used by tools / tests; LootDrop goes through the hero's PlayerRpg).</summary>
        public static ActionResult Pickup(LootEntry e)
        {
            RpgSession s = Instance;
            return s != null && s.owner != null ? s.owner.Pickup(e) : ActionResult.Fail("");
        }
    }
}
