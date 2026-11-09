using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// Player-build verification helper, active only when a command-line flag is present (normal play is unaffected):
    ///   --class warrior|wizard|mechanic|assassin|cleric|fighter   skip the class select
    ///   --tp tx,ty                        put the hero on a tile (and --face down|up|left|right)
    ///   --tier N / --enhanced             gear tier / enhanced art
    ///   --hairstyle N                     hairstyle of the head layer (Lv1 body, idle facing down, any class with heads/ art; 0 = the painted head)
    ///   --bot fight|attack|walk           fight: walk to the nearest monster and attack it; attack: hold attack; walk: walk east
    ///   --zoom N                          camera orthographic half-height (default 80) for close-ups
    ///   --shots 2,5,8 --shot-path P       ScreenCapture to P_1.png, P_2.png ... at those seconds, then quit
    ///   --offscreen-capture              render camera/canvas into a texture (for hidden player windows)
    ///   --solids                          start with the collision tint on
    ///   --skill-at 1.5,9                  press the class skill (L) at those seconds after start
    ///   --dummies N                       spawn N frost slimes in front of the hero (skill / AoE targets)
    ///   --nobloom                         disable the bloom post effect (A/B for blown-out FX)
    ///   --run-seconds N                   quit after N seconds (no screenshots); with --perf logs every frame slower than 16 ms
    ///   --mech-blue                       mechanic FX start in blue (default orange)
    ///   --dummy-dist N                    distance of the first dummy in front of the hero (default 30)
    ///   --god                             hero ignores damage
    ///   --plus N                          worn weapon + offhand at +N (enhance glow bands)
    ///   --worn-tier N                     wear art tier N of the weapon + offhand from the dev-start bag (allWeapons)
    ///   --wear-tiers A,G,B                wear armour / gloves / boots of these art tiers (armour look, ArmorLooks)
    ///   --level N / --give-stones N       RPG state for screenshots: hero level, upgrade stones (saving is always off in capture runs)
    ///   --drop-loot                       scatter a boss-sized pile of loot (gold, potions, stones, 3 items) in front of the hero
    ///   --open-inventory                  open the inventory window (B) after spawning
    ///   --pet spirit|imp / --give-pets    wear that pet / one of each pet in the bag
    ///   --fill-bag N                      top the bag up to N items with generated gear (bag-size captures)
    ///   --bag-item N / --open-shop NPC    Inventory tab on bag item N (with --open-inventory) / that seller's window
    ///   --pet-durability N / --select-pet every pet at N / Equipment tab on the worn pet (with --open-inventory)
    ///   --open-settings / --open-quests   the Settings window (gear button) / the quest list, after spawning
    ///   --hud web|mobile                  HUD layout (HudLayout): web = hotbar + bottom-left chat, mobile = touch controls (default on this exe)
    ///   --chat-demo / --open-chat / --close-chat / --chat-focus  four sample chat lines / chat panel open / collapsed / typing (as Enter), after spawning
    ///   --settings-show on,off            draw the music / sound switches in these states (display only, PlayerPrefs untouched)
    ///   --settings-press ChangeHero       click that button of the open Settings window (not the switches: they save to PlayerPrefs)
    ///   --skill-ui "1=open;2=learn:0;3=slot:2"  skill tree UI script, see DebugCapture.Skills.cs
    ///   --record-combo wizard|mechanic|cleric  scripted basic-attack video: hero holds attack at dummies (3 s lead-in of idle, then ~6 s of combos), same frame dump as below
    ///                                     (--mech-blue for the blue mechanic, --zoom for a close-up)
    ///   --record-ultimate warrior|wizard  scripted video scene (monsters + King nearby, basic combo, then the skill, tail), 60 fps fixed-step,
    ///                                     writes frame_00000.png... to --shot-path (a folder), then quits; encode with ffmpeg
    ///   --record-boss warrior|wizard|mechanic|assassin|cleric  12 s at 60 fps of the hero fighting the Frostfang King (god mode, skill every 4 s),
    ///                                     frames + audio.wav (game sound) to --shot-path
    ///   --record-party                    same, with all five classes fighting the King together (warrior is the player, the rest are AI)
    /// </summary>
    public partial class DebugCapture : MonoBehaviour
    {
        GameSession session;
        string recordArg, classArg, faceArg, shotPath, botArg, petArg;
        float[] shotTimes, skillTimes;
        int dummies;
        float dummyDist = 30f;
        float runSeconds, lastCastTime = -100f;
        bool perf;
        Vector2Int tpTile = new Vector2Int(-1, -1);
        int tier = -1, hairStyle = -1;
        float zoom;
        bool enhanced, solids, god, dropLoot, openInventory;
        int level, giveStones, plus = -1;

        /// <summary>True when the command line asks for a capture / bot run (these default to Frost Hollow: their tiles and scripts are written for it).</summary>
        public static bool Requested(string[] args)
        {
            if (ReleaseBuild.IsRelease) return false; // bots and captures are test tools: a player must not be able to start them from the command line
            foreach (string flag in new[] { "--class", "--shots", "--record-ultimate", "--record-combo", "--record-boss", "--record-party", "--record-choreo", "--net-bot", "--record-net", "--hero-select-test", "--record-compare", "--login-shots" })
                if (Array.IndexOf(args, flag) >= 0) return true;
            return false;
        }

        public static void AttachIfRequested(GameObject root, GameSession gameSession)
        {
            string[] args = Environment.GetCommandLineArgs();
            if (!Requested(args)) return;
            SaveStore.Enabled = false; // capture runs never touch the player's saves
            var d = root.AddComponent<DebugCapture>();
            d.session = gameSession;
            d.Parse(args);
            d.StartCoroutine(d.Run());
        }

        void Update()
        {
            if (!perf || Time.frameCount < 30) return;
            float ms = Time.unscaledDeltaTime * 1000f;
            if (ms > 16f) Debug.Log("[ZenoniaProto] HITCH " + ms.ToString("F0") + " ms at t=" + Time.realtimeSinceStartup.ToString("F2") + " (" + (Time.realtimeSinceStartup - lastCastTime).ToString("F2") + " s after the last skill cast)");
        }

        static HeroClass ParseClass(string name)
        {
            foreach (HeroClassDef d in HeroClassDef.All) if (string.Equals(d.Name, name, StringComparison.OrdinalIgnoreCase)) return d.Id;
            return HeroClass.Warrior;
        }

        static string Arg(string[] a, string key)
        {
            int i = Array.IndexOf(a, key);
            return i >= 0 && i + 1 < a.Length ? a[i + 1] : null;
        }

        void Parse(string[] a)
        {
            classArg = Arg(a, "--class");
            recordArg = Arg(a, "--record-ultimate");
            comboArg = Arg(a, "--record-combo");
            compareArg = Arg(a, "--record-compare");
            bossArg = Arg(a, "--record-boss");
            choreoArg = Arg(a, "--record-choreo");
            party = Array.IndexOf(a, "--record-party") >= 0;
            faceArg = Arg(a, "--face");
            botArg = Arg(a, "--bot");
            shotPath = Arg(a, "--shot-path") ?? Path.Combine(Application.persistentDataPath, "shot");
            string tp = Arg(a, "--tp");
            if (tp != null) { string[] p = tp.Split(','); tpTile = new Vector2Int(int.Parse(p[0]), int.Parse(p[1])); }
            string z = Arg(a, "--zoom");
            if (z != null) zoom = float.Parse(z, System.Globalization.CultureInfo.InvariantCulture);
            string t = Arg(a, "--tier");
            if (t != null) tier = int.Parse(t);
            string hs = Arg(a, "--hairstyle");
            if (hs != null) hairStyle = int.Parse(hs);
            enhanced = Array.IndexOf(a, "--enhanced") >= 0;
            solids = Array.IndexOf(a, "--solids") >= 0;
            god = Array.IndexOf(a, "--god") >= 0;
            ClassPalette.MechanicBlue = Array.IndexOf(a, "--mech-blue") >= 0;
            string dd = Arg(a, "--dummy-dist");
            if (dd != null) dummyDist = float.Parse(dd, System.Globalization.CultureInfo.InvariantCulture);
            perf = Array.IndexOf(a, "--perf") >= 0;
            string rs = Arg(a, "--run-seconds");
            if (rs != null) runSeconds = float.Parse(rs, System.Globalization.CultureInfo.InvariantCulture);
            string lv = Arg(a, "--level");
            if (lv != null) level = int.Parse(lv);
            string pl = Arg(a, "--plus");
            if (pl != null) plus = int.Parse(pl);
            string gs = Arg(a, "--give-stones");
            if (gs != null) giveStones = int.Parse(gs);
            dropLoot = Array.IndexOf(a, "--drop-loot") >= 0;
            openInventory = Array.IndexOf(a, "--open-inventory") >= 0;
            petArg = Arg(a, "--pet");
            string dm = Arg(a, "--dummies");
            if (dm != null) dummies = int.Parse(dm);
            shotTimes = Times(Arg(a, "--shots"));
            skillTimes = Times(Arg(a, "--skill-at"));
            skillUi = Arg(a, "--skill-ui");
        }

        static float[] Times(string list)
        {
            if (list == null) return null;
            string[] parts = list.Split(',');
            var t = new float[parts.Length];
            for (int i = 0; i < parts.Length; i++) t[i] = float.Parse(parts[i], System.Globalization.CultureInfo.InvariantCulture);
            return t;
        }

        void ApplyRpgFlags(HeroController hero)
        {
            PlayerState p = hero.Rpg;
            if (level > 1) { p.Level = Mathf.Min(level, Leveling.MaxLevel); p.RecomputeStats(); p.Hp = p.MaxHp; p.Mp = p.MaxMp; }
            if (giveStones > 0) p.UpgradeStone = Mathf.Min(PlayerState.MaxStones, giveStones);
            string wornTier = Arg(Environment.GetCommandLineArgs(), "--worn-tier");
            if (wornTier != null) // needs allWeapons in dev-start.json: the granted tiers are in the bag
                foreach (string slot in new[] { Slots.Weapon, Slots.Offhand })
                {
                    Item pick = p.Inventory.Find(i => i.uid == "dev-" + slot + "-" + wornTier);
                    if (pick != null) p.Equip(pick);
                }
            string wear = Arg(Environment.GetCommandLineArgs(), "--wear-tiers"); // "A,G,B[,N]": wear armour / gloves / boots [/ necklace] of these art tiers (armour look captures)
            if (wear != null)
            {
                string[] t = wear.Split(',');
                string[] wearSlots = { Slots.Armor, Slots.Gloves, Slots.Boots, Slots.Helmet };
                p.Inventory.Clear(); // the dev-start bag is full: room for the pieces taken off (capture runs never save)
                for (int i = 0; i < wearSlots.Length && i < t.Length; i++)
                {
                    ActionResult r = p.Equip(ItemFactory.Generate(RpgRandom.Default, Mathf.Max(1, int.Parse(t[i]) * 10), wearSlots[i], "common", p.ClassId));
                    if (!r.Ok) Debug.LogWarning("[ZenoniaProto] --wear-tiers: " + wearSlots[i] + " " + r.Text);
                }
                p.RecomputeStats();
                RpgSession.SyncGear();
            }
            if (plus >= 0) // enhance level of the worn weapon + offhand (glow band screenshots)
            {
                foreach (string slot in new[] { Slots.Weapon, Slots.Offhand }) { Item it = p.GetEquipped(slot); if (it != null) it.enhance = plus; }
                p.RecomputeStats();
                RpgSession.SyncGear();
            }
            if (petArg != null)
            {
                PetDef def = PetConfig.Get(petArg);
                if (def != null) { p.Equip(ItemFactory.CreatePet(def, RpgRandom.Default)); p.Hp = p.MaxHp; RpgSession.SyncGear(); }
            }
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "--give-pets") >= 0) foreach (PetDef def in PetConfig.All) p.AddToInventory(ItemFactory.CreatePet(def, RpgRandom.Default));
            string fillBag = Arg(Environment.GetCommandLineArgs(), "--fill-bag"); // capture runs never save, so the extra gear is gone on the next start
            if (fillBag != null)
                for (int i = 0, n = int.Parse(fillBag); p.Inventory.Count < n; i++)
                    if (!p.AddToInventory(ItemFactory.Generate(RpgRandom.Default, 1 + i * 3 % 90, Slots.Generated[i % Slots.Generated.Length], ItemDefs.RarityOrder[i % ItemDefs.RarityOrder.Length], p.ClassId))) break;
            string petDurability = Arg(Environment.GetCommandLineArgs(), "--pet-durability");
            if (petDurability != null)
            {
                if (p.GetEquipped(Slots.Pet) != null) p.GetEquipped(Slots.Pet).durability = int.Parse(petDurability);
                foreach (Item it in p.Inventory) if (it.slot == Slots.Pet) it.durability = int.Parse(petDurability);
            }
            if (dropLoot)
            {
                Rand rand = RpgRandom.Default;
                List<LootEntry> drops = LootTable.RollDrops(LootTable.For("frostfangKing"), rand);
                string[] slots = { Slots.Weapon, Slots.Armor, Slots.Offhand };
                string[] rarities = { "rare", "epic", "uncommon" };
                for (int i = 0; i < slots.Length; i++)
                    drops.Add(new LootEntry { Kind = LootKind.Equipment, Item = ItemFactory.Generate(rand, 3, slots[i], rarities[i], p.ClassId) });
                Vector2[] dirs = { new Vector2(0f, -1f), new Vector2(0f, 1f), new Vector2(-1f, 0f), new Vector2(1f, 0f) };
                LootDrop.SpawnAll(GameObject.Find("ZenoniaProto").transform, drops, hero.Position + dirs[hero.Facing] * 34f);
            }
            if (openInventory) InventoryPanel.Open(GameObject.Find("ZenoniaProto").transform);
            string bagItem = Arg(Environment.GetCommandLineArgs(), "--bag-item");
            if (bagItem != null) InventoryPanel.TestSelectBagItem(int.Parse(bagItem));
            string shop = Arg(Environment.GetCommandLineArgs(), "--open-shop");
            if (shop != null) { NpcDef seller = NpcDefs.Get(shop); PotionShopWindow.Open(shop, seller.Name, seller.Lines[0]); } // what TownNpc.Talk passes
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "--select-pet") >= 0) InventoryPanel.TestSelectWorn(Slots.Pet);
            string selectWorn = Arg(Environment.GetCommandLineArgs(), "--select-worn"); // a slot id, e.g. helmet (the Necklace cell): its detail panel
            if (selectWorn != null) InventoryPanel.TestSelectWorn(selectWorn);
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "--open-settings") >= 0) SettingsWindow.Open(); // what the gear button calls
            string show = Arg(Environment.GetCommandLineArgs(), "--settings-show"); // e.g. on,off: switch art only, PlayerPrefs untouched
            if (show != null) { string[] s = show.Split(','); SettingsWindow.TestShow(s[0] == "on", s.Length > 1 && s[1] == "on"); }
            string press = Arg(Environment.GetCommandLineArgs(), "--settings-press"); // e.g. ChangeHero: clicks that button of the open Settings window
            if (press != null) { GameObject b = GameObject.Find("SettingsWindow/Window/" + press); if (b != null) b.GetComponent<UnityEngine.UI.Button>().onClick.Invoke(); }
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "--open-quests") >= 0) QuestLogWindow.Open();  // a window on the same frame, to compare with
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "--chat-demo") >= 0) SeedChat();
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "--open-chat") >= 0) ChatWindow.SetExpandedState(true);
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "--close-chat") >= 0) ChatWindow.SetExpandedState(false);
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "--chat-focus") >= 0) ChatWindow.FocusFromKey(); // what Enter does: open + caret in the field
        }

        /// <summary>--chat-demo: one line per channel, like the approved chat designs (screenshots of the chat layouts).</summary>
        static void SeedChat()
        {
            ChatHub.Receive(ChatChannel.World, "Rook", "Anyone near Greenvale?");
            ChatHub.Receive(ChatChannel.Party, "Mira", "Meet at the south gate.");
            ChatHub.Receive(ChatChannel.Whisper, "Kai", "Ready for the dungeon?");
            ChatHub.Receive(ChatChannel.Map, "Lina", "The shop is by the well.");
        }

        IEnumerator CastSkills(float start)
        {
            foreach (float at in skillTimes)
            {
                while (Time.realtimeSinceStartup - start < at) yield return null;
                HeroController h = GameSession.Hero;
                Debug.Log("[ZenoniaProto] skill cast at " + at + " => " + (h != null && h.TryCastSkill()));
                lastCastTime = Time.realtimeSinceStartup;
            }
        }

        void SpawnDummies(HeroController hero)
        {
            Transform root = GameObject.Find("ZenoniaProto").transform;
            Vector2[] dirs = { new Vector2(0f, -1f), new Vector2(0f, 1f), new Vector2(-1f, 0f), new Vector2(1f, 0f) };
            Vector2 fwd = dirs[hero.Facing], side = new Vector2(-fwd.y, fwd.x);
            for (int i = 0; i < dummies; i++)
            {
                Vector2 p = hero.Position + fwd * (dummyDist + (i % 3) * 9f) + side * ((i - dummies * 0.5f) * 9f);
                Monster.Create(root, i % 3 == 2 ? MonsterDef.IceGoblin : MonsterDef.FrostSlime, p, -1);
            }
        }

        /// <summary>Fixed 60 fps scripted scene: idle, basic combo, skill, then hold until the effect has played out. One PNG per frame.</summary>
        IEnumerator RecordUltimate(string who)
        {
            Time.captureFramerate = 60;
            Directory.CreateDirectory(shotPath);
            session.Spawn(ParseClass(who));
            yield return null;
            HeroController hero = GameSession.Hero;
            hero.God = true;
            hero.Teleport(FrostHollowMap.TileFeet(11, 6, 2f), 2); // facing west: group ahead, Frostfang King beyond
            dummies = 5;
            SpawnDummies(hero);
            int frame = 0, attackFrom = 45, attackTo = 135, castAt = 150, endAt = castAt + 160 + 100;
            InputAdapter.BotPressed = k => k == GameKey.Attack && frame >= attackFrom && frame < attackTo;
            for (; frame < endAt; frame++)
            {
                if (frame == castAt) Debug.Log("[ZenoniaProto] record cast => " + hero.TryCastSkill());
                yield return new WaitForEndOfFrame();
                Save(Path.Combine(shotPath, "frame_" + frame.ToString("D5") + ".png"));
            }
            Debug.Log("[ZenoniaProto] recorded " + frame + " frames");
            Application.Quit();
        }

        IEnumerator Run()
        {
            yield return null;
            if (NetMode.IsClient) { yield return RunNetClient(); yield break; } // online: the server owns the world, the bot only drives the controls
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "--hero-select-test") >= 0) { yield return RunHeroSelectTest(); yield break; }
            if (compareArg != null) { yield return RecordCompare(compareArg); yield break; }
            if (comboArg != null) { yield return RecordCombo(comboArg); yield break; }
            if (bossArg != null) { yield return RecordBoss(bossArg); yield break; }
            if (choreoArg != null) { yield return RecordChoreo(choreoArg); yield break; }
            if (party) { yield return RecordParty(); yield break; }
            string rec = recordArg;
            if (rec != null) { yield return RecordUltimate(rec); yield break; }
            if (classArg != null)
            {
                HeroClass c = ParseClass(classArg);
                session.Spawn(c);
                yield return null;
                HeroController hero = GameSession.Hero;
                if (tpTile.x >= 0) hero.Teleport(FrostHollowMap.TileFeet(tpTile.x, tpTile.y, 2f), faceArg == "up" ? 1 : faceArg == "left" ? 2 : faceArg == "right" ? 3 : 0);
                if (tier >= 0 || enhanced) hero.View.SetGear(Mathf.Max(tier, 0), enhanced);
                if (hairStyle >= 0) hero.View.HairStyle = hairStyle;
                if (botArg == "fight") EnableFightBot();
                else if (botArg == "attack") InputAdapter.BotPressed = k => k == GameKey.Attack;   // hold attack in place (art / FX close-ups)
                else if (botArg == "walk") InputAdapter.BotHeld = k => k == GameKey.Right;          // walk east
                if (zoom > 0f) Camera.main.orthographicSize = zoom;
                hero.God = god;
                if (Array.IndexOf(Environment.GetCommandLineArgs(), "--nobloom") >= 0) { FeelConfig.BloomOn = false; var b = Camera.main.GetComponent<BloomEffect>(); if (b != null) b.enabled = false; }
                if (dummies > 0) SpawnDummies(hero);
                ApplyRpgFlags(hero);
                if (Array.IndexOf(Environment.GetCommandLineArgs(), "--power-slash-check") >= 0)
                { yield return RunPowerSlashCheck(hero); yield break; }
                if (Array.IndexOf(Environment.GetCommandLineArgs(), "--farm-hold-check") >= 0)
                { yield return RunFarmHoldCheck(hero); yield break; }
            }
            if (solids) WorldBuilder.ToggleSolidOverlay();
            float start = Time.realtimeSinceStartup;
            if (skillTimes != null) StartCoroutine(CastSkills(start));
            if (skillUi != null) StartCoroutine(RunSkillUi(start));
            if (runSeconds > 0f) { while (Time.realtimeSinceStartup - start < runSeconds) yield return null; Application.Quit(); yield break; }
            if (shotTimes == null) yield break;
            for (int i = 0; i < shotTimes.Length; i++)
            {
                while (Time.realtimeSinceStartup - start < shotTimes[i]) yield return null;
                yield return new WaitForEndOfFrame();
                Save(shotPath + "_" + (i + 1) + ".png");
                HeroController h = GameSession.Hero;
                Debug.Log("[ZenoniaProto] shot " + (i + 1) + " t=" + Time.realtimeSinceStartup.ToString("F1") + " dt=" + GameClock.Dt + " hero=" + (h != null ? h.Position + " hp " + h.Hp + " dead " + h.IsDead : "none") + " monsters=" + Monster.All.Count);
                yield return null;
            }
            for (int i = 0; i < 10; i++) yield return null;
            Application.Quit();
        }

        /// <summary>End-of-frame backbuffer grab (includes the IMGUI HUD) written as PNG.</summary>
        static void Save(string path)
        {
            if (InventoryPanel.IsOpen || Array.IndexOf(Environment.GetCommandLineArgs(), "--offscreen-capture") >= 0)
            { SaveInventory(path); return; }
            var tex = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Destroy(tex);
        }

        // Hidden player windows have no readable swapchain. Render the camera/canvas explicitly for art reviews.
        static void SaveInventory(string path)
        {
            Camera camera = Camera.main;
            Canvas canvas = UiKit.Canvas(null);
            RenderMode mode = canvas.renderMode;
            Camera oldCamera = canvas.worldCamera;
            float distance = canvas.planeDistance;
            RenderTexture oldTarget = camera.targetTexture, oldActive = RenderTexture.active;
            var target = RenderTexture.GetTemporary(Screen.width, Screen.height, 24);
            var texture = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
            try
            {
                var portrait = UnityEngine.Object.FindFirstObjectByType<HeroPortraitView>();
                if (portrait != null) portrait.RenderForCapture();
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera;
                canvas.planeDistance = camera.nearClipPlane + 1f;
                camera.targetTexture = target;
                Canvas.ForceUpdateCanvases();
                camera.Render();
                RenderTexture.active = target;
                texture.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
                texture.Apply();
                File.WriteAllBytes(path, texture.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = oldTarget;
                canvas.renderMode = mode;
                canvas.worldCamera = oldCamera;
                canvas.planeDistance = distance;
                RenderTexture.active = oldActive;
                RenderTexture.ReleaseTemporary(target);
                Destroy(texture);
            }
        }

        static bool HasLineOfSight(Vector2 a, Vector2 b)
        {
            int n = Mathf.CeilToInt(Vector2.Distance(a, b) / 6f);
            for (int i = 1; i < n; i++)
            {
                Vector2 p = Vector2.Lerp(a, b, i / (float)n);
                if (FrostHollowMap.RectBlocked(p.x - 3f, p.y, 6f, 6f)) return false;
            }
            return true;
        }

        /// <summary>Walks toward the nearest living monster (aligning on one axis first so the facing is right) and attacks in range.</summary>
        void EnableFightBot(bool boss = false, Vector2 offset = default(Vector2), bool extras = false)
        {
            Vector2 move = Vector2.zero;
            bool attack = false;
            float nextSkill = Time.time + 3f + UnityEngine.Random.value * 3f, nextPotion = 0f;
            InputAdapter.BotHeld = k =>
            {
                Think(GameSession.Hero, offset, ref move, ref attack, boss);
                return (k == GameKey.Left && move.x < 0f) || (k == GameKey.Right && move.x > 0f) || (k == GameKey.Down && move.y < 0f) || (k == GameKey.Up && move.y > 0f);
            };
            InputAdapter.BotPressed = k =>
            {
                if (k == GameKey.Attack) return attack;
                if (!extras) return false;
                HeroController h = GameSession.Hero;
                if (k == GameKey.Skill && attack && Time.time >= nextSkill) { nextSkill = Time.time + 4.5f + UnityEngine.Random.value; return true; }   // class skill while fighting
                if (k == GameKey.HpPotion && h != null && h.Hp < h.MaxHp * 0.4f && Time.time >= nextPotion) { nextPotion = Time.time + 2f; return true; } // drink when low
                return false;
            };
        }

        /// <summary>boss = target the Frostfang King while it lives (otherwise the King is skipped); offset = where around the target to stand.</summary>
        static void Think(HeroController hero, Vector2 offset, ref Vector2 move, ref bool attack, bool boss = false)
        {
            move = Vector2.zero; attack = false;
            if (hero == null || hero.IsDead) return;
            Monster best = boss && Monster.Boss != null && !Monster.Boss.IsDying ? Monster.Boss : null; float bestD = best != null ? 0f : float.MaxValue;
            if (best == null) foreach (Monster m in Monster.All)
            {
                if (m.IsDying || m.MaxHp > 300f || !HasLineOfSight(hero.Position, m.Center)) continue; // skip the boss and anything behind a cliff
                float d = Vector2.Distance(m.Center, hero.Position);
                if (d < bestD) { bestD = d; best = m; }
            }
            if (best == null) return;
            Vector2 diff = best.Center + offset - hero.Position - new Vector2(0f, 4f);
            float range = hero.Class.Ranged ? 80f : 24f;
            bool horizontal = Mathf.Abs(diff.x) >= Mathf.Abs(diff.y);
            float along = horizontal ? diff.x : diff.y, across = horizontal ? diff.y : diff.x;
            if (Mathf.Abs(across) > 5f) { if (horizontal) move.y = Mathf.Sign(across); else move.x = Mathf.Sign(across); }
            else if (Mathf.Abs(along) > range) { if (horizontal) move.x = Mathf.Sign(along); else move.y = Mathf.Sign(along); }
            else
            {
                attack = true; // keep the direction toward the target held so the hero faces it when the swing starts
                if (horizontal) move.x = Mathf.Sign(along); else move.y = Mathf.Sign(along);
            }
        }
    }
}
