using System.Collections.Generic;
using UnityEngine;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// One drop on the ground (coin, potion, upgrade stone or equipment). Pops out of the dead monster in a short arc, bobs with a rarity glow,
    /// and is picked up automatically when a hero touches it. Equipment stays on the ground while the bag is full. Despawns after a minute
    /// (blinking for the last 5 s). Runs on GameClock.Dt, so it freezes with hit-stop and the inventory pause. Online, the top damage dealer
    /// owns the drop until freeAt (LootOwnership); on a headless server there are no sprites, only the pickup rules.
    /// </summary>
    public class LootDrop : MonoBehaviour
    {
        const float PopMs = 380f, PopHeight = 9f, PickupDelayMs = 350f, PickupRadius = 11f, LifeMs = 60000f, BlinkMs = 5000f;
        const float ScatterMin = 6f, ScatterMax = 16f, FullBagToastMs = 2500f;
        static readonly List<LootDrop> NoLoot = new List<LootDrop>();
        static float lastFullToast = -99999f;

        public static IReadOnlyList<LootDrop> All { get { return Zone.Main != null ? Zone.Main.Loot : NoLoot; } }
        public LootEntry Entry { get { return entry; } }
        /// <summary>Hero NetId that has pickup priority (0 = nobody) and the zone time (ms) at which the drop turns free.</summary>
        public ushort OwnerId { get { return ownerId; } }
        public float FreeAtMs { get { return freeAtMs; } }
        /// <summary>Per-zone entity id (server assigned on a client).</summary>
        public ushort NetId;
        public Vector2 Position { get { return transform.position; } }
        /// <summary>Where the drop comes to rest (the pop animation ends here).</summary>
        public Vector2 Target { get { return to; } }
        bool proxy;  // client view of a server drop: no pickup, no despawn timer (the server decides)

        LootEntry entry;
        Vector2 from, to;
        float age, phase, freeAtMs;
        ushort ownerId;
        SpriteRenderer icon, glow, shadow;
        bool glowOn, headless;
        Zone homeZone;

        public static void ResetStatics() { lastFullToast = -99999f; }

        /// <summary>Scatters `drops` around a point (index i of n spreads them on a ring so they never stack).</summary>
        public static void SpawnAll(Transform parent, List<LootEntry> drops, Vector2 at, ushort ownerId = 0, float freeAtMs = 0f)
        {
            float start = Random.value * 360f;
            for (int i = 0; i < drops.Count; i++)
            {
                float angle = (start + i * 360f / Mathf.Max(1, drops.Count)) * Mathf.Deg2Rad;
                float dist = Random.Range(ScatterMin, ScatterMax);
                Vector2 target = at + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle) * 0.7f) * dist;
                if (FrostHollowMap.RectBlocked(target.x - 3f, target.y, 6f, 4f)) target = at; // never into a wall / the river
                Spawn(parent, drops[i], at, target, ownerId, freeAtMs);
            }
        }

        public static LootDrop Spawn(Transform parent, LootEntry e, Vector2 from, Vector2 to, ushort ownerId = 0, float freeAtMs = 0f)
        {
            var go = new GameObject("Loot_" + e.Kind);
            go.transform.SetParent(parent, false);
            var d = go.AddComponent<LootDrop>();
            d.entry = e; d.from = from; d.to = to; d.phase = Random.value * 10f;
            d.ownerId = ownerId; d.freeAtMs = freeAtMs;
            d.headless = NetMode.NoViews;
            d.Build();
            d.homeZone = Zone.Main;
            if (Zone.Main != null) { Zone.Main.Loot.Add(d); d.NetId = Zone.Main.Ids.Alloc(d); }
            return d;
        }

        /// <summary>Client: shows a drop the server announced (fresh = just dropped, so it pops out of the corpse instead of lying there).</summary>
        public static LootDrop SpawnProxy(Transform parent, LootEntry e, Vector2 at, ushort id, ushort ownerId, float freeAtMs, bool fresh)
        {
            var go = new GameObject("Loot_" + e.Kind);
            go.transform.SetParent(parent, false);
            var d = go.AddComponent<LootDrop>();
            d.entry = e; d.from = d.to = at; d.phase = Random.value * 10f;
            d.ownerId = ownerId; d.freeAtMs = freeAtMs;
            d.proxy = true;
            d.headless = NetMode.NoViews;
            d.age = fresh ? 0f : PopMs;
            d.Build();
            d.homeZone = Zone.Main;
            if (Zone.Main != null) { Zone.Main.Loot.Add(d); d.NetId = id; Zone.Main.Ids.Register(id, d); }
            return d;
        }

        void Build()
        {
            if (headless) { Place(); return; }
            shadow = ProceduralSprites.Child(transform, "Shadow", ProceduralSprites.Shadow());
            bool gear = entry.Kind == LootKind.Equipment;
            shadow.transform.localScale = new Vector3(gear ? 9f : 6f, gear ? 9f : 6f, 1f);
            shadow.color = new Color(1f, 1f, 1f, 0.35f);
            glow = ProceduralSprites.Child(transform, "Glow", LootSprites.Glow());
            Color rc = gear ? LootSprites.RarityColor(entry.Item) : entry.Kind == LootKind.UpgradeStone ? new Color(0.45f, 0.95f, 1f) : Color.white;
            glowOn = gear || entry.Kind == LootKind.UpgradeStone;
            glow.color = new Color(rc.r, rc.g, rc.b, gear && entry.Item.rarity == "common" ? 0.35f : 0.6f);
            glow.transform.localScale = new Vector3(gear ? 18f : 12f, gear ? 12f : 8f, 1f);
            glow.enabled = glowOn;
            icon = ProceduralSprites.Child(transform, "Icon", LootSprites.For(entry));
            if (gear && icon.sprite == null) icon.sprite = LootSprites.Stone(); // missing icon art: still something to pick up
            Place();
        }

        /// <summary>The zone slept for `ms`: the drop aged all the same and is gone on the next tick when its minute is over.</summary>
        public void FastForward(float ms) { age += ms; }

        void Update()
        {
            Zone.Enter(homeZone);
            if (!headless && icon == null) { Destroy(gameObject); return; } // recompile during Play mode
            age += GameClock.Dt * 1000f;
            if (proxy) { if (age > LifeMs) age = LifeMs - BlinkMs - 1f; Place(); return; } // the server decides when it vanishes
            if (age >= LifeMs) { Destroy(gameObject); return; }
            Place();
            if (age >= PickupDelayMs) TryPickup();
        }

        void Place()
        {
            float t = Mathf.Clamp01(age / PopMs);
            Vector2 p = Vector2.Lerp(from, to, 1f - (1f - t) * (1f - t)); // ease-out slide
            transform.position = new Vector3(p.x, p.y, 0f);
            if (headless) return;
            float lift = Mathf.Sin(t * Mathf.PI) * PopHeight;
            float bob = t >= 1f ? Mathf.Sin(age / 1000f * 3.2f + phase) * 1.1f + 1.2f : 0f;
            int order = GameConfig.OrderBase(p.y);
            shadow.sortingOrder = order;
            glow.sortingOrder = order + 1;
            icon.sortingOrder = order + 3;
            icon.transform.localPosition = new Vector3(0f, lift + bob, 0f);
            glow.transform.localPosition = new Vector3(0f, 2f, 0f);
            bool visible = LifeMs - age > BlinkMs || Mathf.FloorToInt(age / 120f) % 2 == 0;
            icon.enabled = visible;
            glow.enabled = glowOn && visible;
            if (glowOn) { Color c = glow.color; c.a = (0.45f + 0.2f * Mathf.Sin(age / 1000f * 4f + phase)) * (entry.Kind == LootKind.Equipment && entry.Item.rarity == "common" ? 0.6f : 1f) * (proxy && OwnedByOtherNow() ? 0.25f : 1f); glow.color = c; }
            if (proxy) icon.color = OwnedByOtherNow() ? new Color(1f, 1f, 1f, 0.55f) : Color.white; // another player still has the priority
        }

        /// <summary>First living hero in reach that may take it (owner priority) applies the pickup; heroes without a PlayerRpg (debug party) never do.</summary>
        void TryPickup()
        {
            if (GameClock.Paused) return;
            Zone zone = Zone.Main;
            if (zone == null) return;
            IReadOnlyList<HeroController> heroes = zone.Heroes.All;
            for (int i = 0; i < heroes.Count; i++)
            {
                HeroController hero = heroes[i];
                if (hero.IsDead || hero.Owner == null) continue;
                if (Vector2.Distance(hero.Position, transform.position) > PickupRadius) continue;
                if (!LootOwnership.CanPick(ownerId, freeAtMs, hero.NetId, zone.TimeMs, false)) continue;
                ActionResult r = hero.Owner.Pickup(entry);
                if (r.Ok) { Destroy(gameObject); return; }
                if (hero == LocalPlayer.Hero) ToastFullBag();
            }
        }

        /// <summary>Client: another player still has the loot priority (dims the glow until the server clock passes freeAt).</summary>
        bool OwnedByOtherNow()
        {
            HeroController me = LocalPlayer.Hero;
            return ownerId != 0 && (me == null || me.NetId != ownerId) && NetClient.ServerNowMs < freeAtMs;
        }

        void ToastFullBag()
        {
            if (entry.Kind != LootKind.Equipment || !NetMode.RendersFx || Time.unscaledTime * 1000f - lastFullToast <= FullBagToastMs) return;
            lastFullToast = Time.unscaledTime * 1000f;
            RpgHud.Toast("Inventory full", new Color(1f, 0.55f, 0.45f));
        }

        void OnDestroy()
        {
            Zone z = homeZone;
            if (z == null) return;
            z.Loot.Remove(this);
            z.Ids.Release(NetId, z.TimeMs);
        }
    }
}
