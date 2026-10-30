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

        Transform root;
        PlayerState player;
        PlayerRpg owner;
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
            DevStartConfig.Apply(player); // Resources/Data/dev-start.json: test stones / gold / potions (editor + dev builds)
            bossDefeated = save != null && save.bossDefeated;
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
            if (save != null && save.hasPosition && !FrostHollowMap.RectBlocked(save.x - 5f, save.y, 10f, 8f)) hero.Teleport(new Vector2(save.x, save.y), 0);
            if (bossDefeated && NetMode.IsOffline && Monster.Boss != null && Application.isPlaying) Destroy(Monster.Boss.gameObject);
            SyncGear();
            if (save == null) SaveNow(); // a new character gets its file right away
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

        /// <summary>Offline the king never returns for this save: remember the kill right away.</summary>
        void OnBossKilled()
        {
            bossDefeated = true;
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
