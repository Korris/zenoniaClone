using System.Collections.Generic;
using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// A villager standing on a tile of the current map: the web's HD idle sheet (192 px frames at 8 px per unit = hero height, 4 idle frames per
    /// facing row down / left / right) with a ground shadow. Offline only. InRange tells WindowsHost when "Talk" / E works for the nearest one;
    /// Talk() opens the quest dialogue for the quest giver and the idle chatter (NpcDialogueBox, the villager's face as portrait) for the rest.
    /// </summary>
    public sealed class TownNpc : MonoBehaviour
    {
        const float TalkRange = 30f, IdleFrameSeconds = 0.26f; // web: slow 260 ms breathing loop
        static readonly List<TownNpc> All = new List<TownNpc>();
        static readonly Dictionary<string, Sprite> Portraits = new Dictionary<string, Sprite>();

        public NpcDef Def { get; private set; }
        public Vector2 Feet { get; private set; }
        SpriteRenderer body;
        Sprite[] idle;
        float phase;

        /// <summary>Creates every villager of the map (web: x = (tx + .5) * 16, y = (ty + 1) * 16 - 2).</summary>
        public static void CreateAll(Transform parent, IGameMap map)
        {
            foreach (MapNpc n in map.Npcs)
            {
                NpcDef def = NpcDefs.Get(n.Id);
                if (def == null) { Debug.LogError("[ZenoniaProto] unknown NPC " + n.Id); continue; }
                var go = new GameObject("Npc_" + def.Id);
                go.transform.SetParent(parent, false);
                go.AddComponent<TownNpc>().Build(def, FrostHollowMap.TileFeet(n.Tx, n.Ty, 2f));
            }
        }

        void Build(NpcDef def, Vector2 feet)
        {
            Def = def; Feet = feet;
            phase = feet.x * 0.013f; // villagers do not breathe in lockstep
            transform.position = new Vector3(feet.x, feet.y, 0f);
            int order = GameConfig.OrderBase(feet.y);
            SpriteRenderer shadow = ProceduralSprites.Child(transform, "Shadow", ProceduralSprites.Shadow());
            shadow.transform.localScale = new Vector3(14f, 14f, 1f);
            shadow.color = new Color(0f, 0f, 0f, 0.45f);
            shadow.sortingOrder = order;
            Sprite[][] sheet = SpriteSheet.Grid("Sprites/village/npcs/" + def.Id, GameConfig.FramePx, GameConfig.FramePx, GameConfig.FramePpu, new Vector2(0.5f, 0f));
            if (sheet != null && sheet.Length > 0)
            {
                idle = sheet[Mathf.Min(def.Facing, sheet.Length - 1)];
                body = ProceduralSprites.Child(transform, "Body", idle[0]);
                body.sortingOrder = order + 3;
            }
            if (def.QuestGiver) ElderNpc.Attach(this);
            All.Add(this);
        }

        void OnDestroy() { All.Remove(this); }

        void Update()
        {
            if (body != null) body.sprite = idle[(int)((Time.time + phase) / IdleFrameSeconds) % idle.Length];
        }

        /// <summary>Nearest villager within talk range of the living local hero, or null.</summary>
        static TownNpc Nearest()
        {
            HeroController h = GameSession.Hero;
            if (h == null || h.IsDead) return null;
            TownNpc best = null;
            float bestDist = TalkRange;
            foreach (TownNpc n in All)
            {
                float d = Vector2.Distance(h.Position, n.Feet);
                if (d <= bestDist) { best = n; bestDist = d; }
            }
            return best;
        }

        /// <summary>The local hero stands close enough to talk to somebody.</summary>
        public static bool InRange { get { return Nearest() != null; } }

        /// <summary>Starts the conversation with the nearest villager.</summary>
        public static void Talk()
        {
            TownNpc n = Nearest();
            if (n == null) return;
            if (n.Def.QuestGiver) { ElderNpc.Talk(); return; }
            NpcDialogueBox.Open(n.Def.Name, Portrait(n.Def.Id), n.Def.Lines, null);
        }

        /// <summary>The 152 px face of a villager (cached; null when the file is missing).</summary>
        static Sprite Portrait(string id)
        {
            Sprite s;
            if (Portraits.TryGetValue(id, out s) && s != null) return s;
            var face = Resources.Load<Texture2D>("Sprites/village/npcs/" + id + "_face");
            if (face == null) return null;
            s = Sprite.Create(face, new Rect(0, 0, face.width, face.height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
            Portraits[id] = s;
            return s;
        }
    }
}
