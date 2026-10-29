using System.Collections.Generic;
using UnityEngine;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// One drop on the ground (coin, potion, upgrade stone or equipment). Pops out of the dead monster in a short arc, bobs with a rarity glow,
    /// and is picked up automatically when the hero touches it. Equipment stays on the ground while the bag is full. Despawns after a minute
    /// (blinking for the last 5 s). Runs on GameClock.Dt, so it freezes with hit-stop and the inventory pause.
    /// </summary>
    public class LootDrop : MonoBehaviour
    {
        const float PopMs = 380f, PopHeight = 9f, PickupDelayMs = 350f, PickupRadius = 11f, LifeMs = 60000f, BlinkMs = 5000f;
        const float ScatterMin = 6f, ScatterMax = 16f, FullBagToastMs = 2500f;
        static readonly List<LootDrop> AllList = new List<LootDrop>();
        static float lastFullToast = -99999f;

        public static IReadOnlyList<LootDrop> All { get { return AllList; } }
        public LootEntry Entry { get { return entry; } }

        LootEntry entry;
        Vector2 from, to;
        float age, phase;
        SpriteRenderer icon, glow, shadow;
        bool glowOn;

        public static void ResetStatics() { AllList.Clear(); lastFullToast = -99999f; }

        /// <summary>Scatters `drops` around a point (index i of n spreads them on a ring so they never stack).</summary>
        public static void SpawnAll(Transform parent, List<LootEntry> drops, Vector2 at)
        {
            float start = Random.value * 360f;
            for (int i = 0; i < drops.Count; i++)
            {
                float angle = (start + i * 360f / Mathf.Max(1, drops.Count)) * Mathf.Deg2Rad;
                float dist = Random.Range(ScatterMin, ScatterMax);
                Vector2 target = at + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle) * 0.7f) * dist;
                if (FrostHollowMap.RectBlocked(target.x - 3f, target.y, 6f, 4f)) target = at; // never into a wall / the river
                Spawn(parent, drops[i], at, target);
            }
        }

        public static LootDrop Spawn(Transform parent, LootEntry e, Vector2 from, Vector2 to)
        {
            var go = new GameObject("Loot_" + e.Kind);
            go.transform.SetParent(parent, false);
            var d = go.AddComponent<LootDrop>();
            d.entry = e; d.from = from; d.to = to; d.phase = Random.value * 10f;
            d.Build();
            AllList.Add(d);
            return d;
        }

        void Build()
        {
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

        void Update()
        {
            if (icon == null) { Destroy(gameObject); return; } // recompile during Play mode
            age += GameClock.Dt * 1000f;
            if (age >= LifeMs) { Destroy(gameObject); return; }
            Place();
            if (age >= PickupDelayMs) TryPickup();
        }

        void Place()
        {
            float t = Mathf.Clamp01(age / PopMs);
            Vector2 p = Vector2.Lerp(from, to, 1f - (1f - t) * (1f - t)); // ease-out slide
            float lift = Mathf.Sin(t * Mathf.PI) * PopHeight;
            float bob = t >= 1f ? Mathf.Sin(age / 1000f * 3.2f + phase) * 1.1f + 1.2f : 0f;
            transform.position = new Vector3(p.x, p.y, 0f);
            int order = GameConfig.OrderBase(p.y);
            shadow.sortingOrder = order;
            glow.sortingOrder = order + 1;
            icon.sortingOrder = order + 3;
            icon.transform.localPosition = new Vector3(0f, lift + bob, 0f);
            glow.transform.localPosition = new Vector3(0f, 2f, 0f);
            bool visible = LifeMs - age > BlinkMs || Mathf.FloorToInt(age / 120f) % 2 == 0;
            icon.enabled = visible;
            glow.enabled = glowOn && visible;
            if (glowOn) { Color c = glow.color; c.a = (0.45f + 0.2f * Mathf.Sin(age / 1000f * 4f + phase)) * (entry.Kind == LootKind.Equipment && entry.Item.rarity == "common" ? 0.6f : 1f); glow.color = c; }
        }

        void TryPickup()
        {
            HeroController hero = HeroController.Instance;
            if (hero == null || hero.IsDead || GameClock.Paused) return;
            if (Vector2.Distance(hero.Position, transform.position) > PickupRadius) return;
            ActionResult r = RpgSession.Pickup(entry);
            if (r.Ok) { Destroy(gameObject); return; }
            if (entry.Kind == LootKind.Equipment && Time.unscaledTime * 1000f - lastFullToast > FullBagToastMs)
            {
                lastFullToast = Time.unscaledTime * 1000f;
                RpgHud.Toast("Inventory full", new Color(1f, 0.55f, 0.45f));
            }
        }

        void OnDestroy() { AllList.Remove(this); }
    }
}
