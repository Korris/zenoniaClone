using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Elder Thomas' quest side: the bobbing marker above his TownNpc body ("!" = quest available, "?" = ready to turn in) and Talk(), which picks
    /// the dialogue for the quest state and chains into the quest offer / reward. Offline only (the quest counts local kills); the body, shadow and
    /// talk range belong to TownNpc.
    /// </summary>
    public sealed class ElderNpc : MonoBehaviour
    {
        const float BodyHeight = 24f;

        Transform markerAvailable, markerReady;

        /// <summary>Adds the quest markers to the villager that runs the quest.</summary>
        public static void Attach(TownNpc npc) { npc.gameObject.AddComponent<ElderNpc>().Build(); }

        void Build()
        {
            int order = GameConfig.OrderBase(transform.position.y);
            markerAvailable = QuestMarker("MarkerAvailable", false, order + 5);
            markerReady = QuestMarker("MarkerReady", true, order + 5);
        }

        /// <summary>"!" = the win-quest-icon ring; "?" = a navy disc with a gold rim + a pixel "?" glyph, since the art only has the "!".</summary>
        Transform QuestMarker(string name, bool question, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            if (!question)
            {
                var tex = Resources.Load<Texture2D>("Sprites/ui/win-quest-icon");
                if (tex == null) return go.transform;
                Sprite s = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), tex.width / 10f, 0, SpriteMeshType.FullRect);
                ProceduralSprites.Child(go.transform, "Icon", s).sortingOrder = order;
                return go.transform;
            }
            SpriteRenderer rim = ProceduralSprites.Child(go.transform, "Rim", UiSprites.Circle());
            rim.color = new Color(1f, 0.8f, 0.3f); rim.sortingOrder = order; rim.transform.localScale = Vector3.one * 8.4f;
            SpriteRenderer disc = ProceduralSprites.Child(go.transform, "Disc", UiSprites.Circle());
            disc.color = new Color(0.04f, 0.08f, 0.17f); disc.sortingOrder = order + 1; disc.transform.localScale = Vector3.one * 7.2f;
            SpriteRenderer glyph = ProceduralSprites.Child(go.transform, "Glyph", QuestionMarkGlyph());
            glyph.color = new Color(1f, 0.85f, 0.35f); glyph.sortingOrder = order + 2;
            return go.transform;
        }

        static Sprite questionMark;

        /// <summary>7 x 9 pixel "?" (the quest-icon art only has the "!"), point filtered, about 5.5 world units tall.</summary>
        static Sprite QuestionMarkGlyph()
        {
            if (questionMark != null) return questionMark;
            string[] rows = { ".#####.", "##...##", "##...##", "....##.", "...##..", "...##..", ".......", "...##..", "...##.." };
            var tex = new Texture2D(7, 9, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < 9; y++)
                for (int x = 0; x < 7; x++) tex.SetPixel(x, 8 - y, rows[y][x] == '#' ? Color.white : Color.clear);
            tex.Apply(false, true);
            questionMark = Sprite.Create(tex, new Rect(0, 0, 7, 9), new Vector2(0.5f, 0.5f), 9f / 5.5f, 0, SpriteMeshType.FullRect);
            return questionMark;
        }

        /// <summary>Starts the conversation that fits the quest state.</summary>
        public static void Talk()
        {
            QuestDef q = QuestTracker.Def;
            switch (QuestTracker.State)
            {
                case QuestState.Available:
                    NpcDialogueBox.Open(q.GiverName, Portrait(), q.OfferLines, () => QuestOfferWindow.Open(q, QuestTracker.Accept));
                    break;
                case QuestState.Active:
                    var lines = (string[])q.ActiveLines.Clone();
                    lines[lines.Length - 1] += "\n(" + q.MonsterName + "  " + QuestTracker.Progress + " / " + q.Count + ")";
                    NpcDialogueBox.Open(q.GiverName, Portrait(), lines, null);
                    break;
                case QuestState.ReadyToTurnIn:
                    NpcDialogueBox.Open(q.GiverName, Portrait(), q.ReadyLines, QuestTracker.TurnIn);
                    break;
                default:
                    NpcDialogueBox.Open(q.GiverName, Portrait(), q.DoneLines, null);
                    break;
            }
        }

        static Sprite Portrait() { return UiArt.Win("portrait-elder"); }

        void Update()
        {
            float bob = Mathf.Sin(Time.time * 3f) * 1.2f;
            markerAvailable.localPosition = new Vector3(0f, BodyHeight + 9f + bob, 0f);
            markerReady.localPosition = markerAvailable.localPosition;
            markerAvailable.gameObject.SetActive(QuestTracker.State == QuestState.Available);
            markerReady.gameObject.SetActive(QuestTracker.State == QuestState.ReadyToTurnIn);
        }
    }
}
