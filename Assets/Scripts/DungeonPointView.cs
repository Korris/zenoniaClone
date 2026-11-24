using System.Collections.Generic;
using UnityEngine;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// The chests and warp crystals of the map on screen (Rootburrow V5, DungeonPoints), built with the scenery (MapScenery.Build) on every map load, offline and on a client.
    /// Each stands on its solid tile like a prop (billboard on a 2.5D map, lit by DungeonLights) but reads the LOCAL character's state every frame: a chest this
    /// character emptied shows open (other players see their own state), a crystal not attuned yet stands dim. When the state flips while the hero is near,
    /// the opening / attuning FX play (DungeonPointFx); so the server's answer online and the local rule offline look the same.
    /// Art: ChatGPT sheet Assets/ArtLibrary/maps/rootburrow/dark-25d/rootburrow-chest-warp-props-58-chatgpt-1.png (Tools/cut-map-25d-dungeon-props.py rootburrow chest-warp).
    /// </summary>
    public sealed class DungeonPointView : MonoBehaviour
    {
        const float NearFxUnits = 140f; // only a state change seen from close by plays its FX (not a refill timer running out across the map)
        static readonly Color LockedTint = new Color(0.62f, 0.6f, 0.78f, 1f);
        static readonly List<DungeonPointView> All = new List<DungeonPointView>();

        public ChestDef Chest { get; private set; }
        public WaypointDef Crystal { get; private set; }
        /// <summary>Where the hero's distance is measured from: the object's foot, on the bottom edge of its solid tile.</summary>
        public Vector2 Foot { get; private set; }
        SpriteRenderer sr;
        bool shownOn, built;
        float popAge = -1f, attuneAskedAt = -99f;

        public static IReadOnlyList<DungeonPointView> Views { get { return All; } }

        public static void Build(Transform parent, IGameMap map)
        {
            foreach (ChestDef c in DungeonPoints.ChestsOn(map.Id)) Create(parent, c.Id, c.Tx, c.Ty).Chest = c;
            foreach (WaypointDef w in DungeonPoints.WaypointsOn(map.Id)) Create(parent, w.Id, w.Tx, w.Ty).Crystal = w;
        }

        static DungeonPointView Create(Transform parent, string id, int tx, int ty)
        {
            var go = new GameObject("DungeonPoint_" + id);
            go.transform.SetParent(parent, false);
            var v = go.AddComponent<DungeonPointView>();
            float t = GameConfig.TileUnits;
            v.Foot = new Vector2((tx + 0.5f) * t, GameConfig.MapH - (ty + 1) * t);
            All.Add(v);
            return v;
        }

        void OnDestroy() { All.Remove(this); }

        static PlayerState Me { get { HeroController h = GameSession.Hero; return h != null ? h.Rpg : null; } }

        /// <summary>Chest: empty for this character now. Crystal: attuned.</summary>
        public bool StateOn
        {
            get
            {
                PlayerState p = Me;
                if (p == null) return false;
                return Chest != null ? p.ChestEmpty(Chest.Id, PlayerState.UnixNowMs) : p.HasWaypoint(Crystal.Id);
            }
        }

        string SpriteName(bool on)
        {
            if (Crystal != null) return Crystal.QuestOnly ? "tauQuestTouch" : "rbWarpCrystal"; // tauQuestTouch has no art yet (phase 04): the greybox marker is drawn
            return Chest.Rare ? (on ? "rbChestRareOpen" : "rbChestRare") : (on ? "rbChestOpen" : "rbChest");
        }

        void LateBuild()
        {
            built = true;
            shownOn = StateOn;
            sr = ProceduralSprites.Child(transform, "Body", MapScenery.PropSprite(SpriteName(shownOn)) ?? ProceduralSprites.White(false));
            sr.transform.position = new Vector3(Foot.x, Foot.y, 0f);
            if (Crystal != null && Crystal.QuestOnly) { sr.transform.localScale = new Vector3(9f, 15f, 1f); sr.transform.position = new Vector3(Foot.x, Foot.y + 7.5f, 0f); } // TEMP greybox marker of a Taurus quest touch point (no prop art yet, phase 04)
            sr.sortingOrder = GameConfig.OrderBase(Foot.y);
            if (View25D.Active) View25D.StandProp(sr);
            ApplyLook();
        }

        void ApplyLook()
        {
            Sprite s = MapScenery.PropSprite(SpriteName(shownOn));
            if (s != null) sr.sprite = s;
            if (Crystal != null) sr.color = shownOn ? Color.white : LockedTint;
        }

        void Update()
        {
            if (!built) { LateBuild(); return; } // after Start: the hero (and its state) exists once the world is built
            bool on = StateOn;
            HeroController h = GameSession.Hero;
            float dist = h != null ? Vector2.Distance(h.Position, Foot) : float.MaxValue;
            if (on != shownOn)
            {
                shownOn = on;
                ApplyLook();
                if (on && dist < NearFxUnits)
                {
                    popAge = 0f;
                    if (Chest != null) DungeonPointFx.ChestOpened(Foot, Chest.Rare);
                    else { DungeonPointFx.CrystalAttuned(Foot); NotificationToast.Show("Warp Crystal Attuned", Crystal.Name, UiArt.Win("quest-icon")); }
                }
            }
            if (Crystal != null && !on && h != null && !h.IsDead && dist <= DungeonPoints.UnlockUnits && Time.time - attuneAskedAt > 2f)
            {
                attuneAskedAt = Time.time; // walking up to a crystal attunes it (online the server checks the distance and answers with the state)
                DungeonInteract.Attune(Crystal.Id);
            }
            Pop();
        }

        /// <summary>A short squash-and-stretch when the lid flies open / the crystal wakes (0.32 s).</summary>
        void Pop()
        {
            if (popAge < 0f) return;
            popAge += Time.deltaTime;
            float k = popAge / 0.32f;
            if (k >= 1f) { popAge = -1f; sr.transform.localScale = Vector3.one; return; }
            float s = Mathf.Sin(k * Mathf.PI) * (1f - k);
            sr.transform.localScale = new Vector3(1f - 0.10f * s, 1f + 0.16f * s, 1f);
        }
    }
}
