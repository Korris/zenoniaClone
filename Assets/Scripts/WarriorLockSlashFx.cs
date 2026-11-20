using System.Collections.Generic;
using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Slash trail of the Warrior's lock-target basic attack (2026-11-20): a 4-frame painted crescent (ChatGPT, Tan Pham chat
    /// https://chatgpt.com/c/6affde79-fbb0-83ec-9f29-83403e41d605; cut by Tools/build-warrior-lock-slash-vfx.py into
    /// Sprites/classes/warrior/fx/attack/lock-slash.png, 1 x 4 cells, pivot = cell centre). The art bulges toward +x and sweeps top to bottom;
    /// it is turned toward the locked monster, mirrored on the left half so it still sweeps downward, and placed so the crescent's front reaches the
    /// monster's near side. Plays at the combo Hit event (the blow), so its timing is the swing's by construction. Normal alpha + a faint additive
    /// copy for the bloom, like the painted ranged FX. Pooled; cosmetic only (nothing on a headless server).
    /// </summary>
    public class WarriorLockSlashFx : MonoBehaviour
    {
        const string SheetPath = "Sprites/classes/warrior/fx/attack/lock-slash";
        const int Frames = 4;
        const float CellUnits = 20f;        // drawn size of one cell in world units (x figure scale): the full crescent (0.77 cell) is ~15 u tall, a sword's sweep at contact range, not a wall over the hero (40 u covered him; 24 u still lay over his shield once he stood at contact, 2026-11-20)
        const float FrontOfCell = 0.41f;    // frame 2's front edge from the cell centre, fraction of the cell (printed by the tool)
        const float Life = 0.2f;            // 4 frames x 50 ms: the sword's forward frames f1-f2 of the 400 ms swing
        const float Lift = 4f;              // above the chest point the lock is measured from (y + 6): the sword's height
        const float FrontPast = 3f, MaxFront = 30f; // the crescent's front lands this far past the contact point (the monster's body edge), at most MaxFront
        const float MinAhead = 9f;          // the crescent's centre stays at least this far ahead of the chest: at sword contact (13 u) its back sits at the hilt, not over the hero (2026-11-20, contact reach)

        static readonly List<WarriorLockSlashFx> Pool = new List<WarriorLockSlashFx>();
        static Transform root;
        static Sprite[] frames;

        SpriteRenderer sr, glow;
        float age;

        public static void Init(Transform parent)
        {
            root = parent;
            Pool.Clear();
            Texture2D tex = Resources.Load<Texture2D>(SheetPath);
            frames = tex != null ? SpriteTint.Slice(tex, Frames, 1, tex.width / Frames, new Vector2(0.5f, 0.5f))[0] : null;
        }

        /// <summary>True when the art loaded (else the caller falls back to the old slash + trail).</summary>
        public static bool Ready { get { return frames != null && root != null; } }

        /// <summary>A slash from the chest of the hero standing at `feet` toward `angleDeg` (0 = right, 90 = up; toward the contact point on the monster); `dist` = chest to the contact point on the monster's body edge (0 = no target).</summary>
        public static void Spawn(Vector2 feet, float angleDeg, float dist, float figureScale)
        {
            if (!NetMode.RendersFx || !Ready) return;
            WarriorLockSlashFx fx = null;
            for (int i = 0; i < Pool.Count; i++) if (!Pool[i].gameObject.activeSelf) { fx = Pool[i]; break; }
            if (fx == null)
            {
                var go = new GameObject("LockSlash");
                go.transform.SetParent(root, false);
                fx = go.AddComponent<WarriorLockSlashFx>();
                fx.sr = go.AddComponent<SpriteRenderer>();
                fx.glow = ProceduralSprites.Child(go.transform, "Glow", null);
                fx.glow.sharedMaterial = FeelMaterials.Additive;
                Pool.Add(fx);
            }
            float k = Mathf.Max(0.5f, figureScale);
            float size = CellUnits * k;
            float rad = angleDeg * Mathf.Deg2Rad;
            var dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
            float ahead = Mathf.Max(MinAhead * k, Mathf.Min(dist > 0f ? dist + FrontPast : 18f, MaxFront) - FrontOfCell * size);
            Vector2 chest = feet + new Vector2(0f, (6f + Lift) * k);
            Vector2 c = chest + dir * ahead;
            bool left = Mathf.Cos(rad) < -0.01f; // left half: mirror so the sweep still runs from high to low on screen
            fx.transform.position = new Vector3(c.x, c.y, 0f);
            fx.transform.rotation = Quaternion.Euler(0f, 0f, angleDeg);
            fx.transform.localScale = new Vector3(size, left ? -size : size, 1f);
            // A slash up the screen goes behind the hero (y-sorted just under him), as his sword does: drawn over him it covered his back.
            int order = dir.y > 0.35f ? GameConfig.OrderBase(feet.y) - 3 : FeelConfig.FxOrder;
            fx.sr.sortingOrder = order;
            fx.glow.sortingOrder = order + 1;
            fx.age = 0f;
            fx.gameObject.SetActive(true);
            fx.Apply();
        }

        void Update()
        {
            age += Time.deltaTime;
            if (age >= Life) { gameObject.SetActive(false); return; }
            Apply();
        }

        void Apply()
        {
            int i = Mathf.Min(Frames - 1, Mathf.FloorToInt(age / Life * Frames));
            sr.sprite = frames[i];
            float fade = Mathf.Clamp01((Life - age) / (Life * 0.3f));
            sr.color = new Color(1f, 1f, 1f, fade);
            glow.enabled = FeelConfig.FxOn;
            glow.sprite = sr.sprite;
            glow.color = new Color(1f, 0.55f, 0.4f, fade * FeelConfig.PaintedGlowAlpha);
        }
    }
}
