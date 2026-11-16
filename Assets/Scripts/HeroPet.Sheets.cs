using System.Collections.Generic;
using UnityEngine;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// HeroPet: the pictures. A gacha pet (PetDef.MonsterType) draws the monster's own sheets, Sprites/monsters/&lt;type&gt;.png (walk, 4 facing rows x 4 frames of 192) and
    /// &lt;type&gt;-attack.png (4 x 4 of 256, feet 1/8 up the cell), scaled so its body is PetDef.HeightUnits tall; Walk at 8 FPS while moving, frame 0 standing, the Attack row over
    /// the 400 ms swing. A Legacy pet (Spirit, Imp) draws its one picture, Sprites/pets/&lt;ArtId&gt;/art.png, hovering with a 2 px bob and flipped to face its way.
    /// </summary>
    public sealed partial class HeroPet
    {
        const int WalkCell = 192, AttackCell = 256;
        const float WalkFrameMs = 125f; // 8 FPS (ChatGPT Lần 8); attack 4 frames over 400 ms = 10 FPS
        const bool LegacyArtLooksRight = false; // the Spirit / Imp pictures look a little to the left (sprite-rules Sổ lỗi 2026-11-01)
        static readonly Dictionary<string, Sprite> Stills = new Dictionary<string, Sprite>();

        Sprite[][] walk, attack;
        Sprite still;
        float scale = 1f;

        void LoadArt(PetDef d)
        {
            walk = attack = null; still = null;
            sr.sprite = null; sr.flipX = false; sr.color = Color.white;
            if (d == null) return;
            MonsterDef m = d.MonsterType != null ? MonsterDef.ByType(d.MonsterType) : null;
            if (m != null)
            {
                string sheet = Monster.SheetUsable(m.Sheet, WalkCell) ? m.Sheet : m.FallbackSheet;
                walk = SpriteSheet.Grid("Sprites/monsters/" + sheet, WalkCell, WalkCell, GameConfig.FramePpu, new Vector2(0.5f, 0f));
                attack = SpriteSheet.Grid("Sprites/monsters/" + sheet + "-attack", AttackCell, AttackCell, GameConfig.FramePpu, new Vector2(0.5f, 32f / AttackCell));
                scale = d.HeightUnits / Mathf.Max(1f, m.BodyHeightUnits); // the monster's body at scale 1 is BodyHeightUnits tall
                if (walk != null) return;
            }
            still = LoadStill(d.ArtId);
            if (still != null) scale = d.HeightUnits / still.bounds.size.y;
            sr.color = PetArt.TintOf(d);
        }

        static Sprite LoadStill(string artId)
        {
            Sprite s;
            if (Stills.TryGetValue(artId, out s) && s != null) return s;
            var tex = Resources.Load<Texture2D>("Sprites/pets/" + artId + "/art");
            s = tex != null ? Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0f), 100f, 0, SpriteMeshType.FullRect) : null;
            if (s == null) Debug.LogWarning("[ZenoniaProto] no pet art at Sprites/pets/" + artId + "/art");
            Stills[artId] = s;
            return s;
        }

        /// <summary>Picks the frame for this state and puts the pet on the map, y-sorted by its feet like a monster.</summary>
        void Draw(bool moving)
        {
            Sprite s = null;
            bool flies = def.Flies || walk == null;
            if (walk != null)
            {
                Sprite[] row = SpriteSheet.Row(walk, facing), atk = SpriteSheet.Row(attack, facing);
                if (state == State.Attack && atk != null && atk.Length > 0) s = atk[Mathf.Min(atk.Length - 1, Mathf.FloorToInt(attackMs / (PetConfig.AttackAnimMs / 4f)))];
                else if (row != null && row.Length > 0) s = row[moving ? Mathf.FloorToInt(ageMs / WalkFrameMs) % row.Length : 0];
                sr.flipX = false;
            }
            else
            {
                s = still;
                if (facing >= 2) sr.flipX = (facing == 2) == LegacyArtLooksRight;
            }
            sr.sprite = s;
            sr.enabled = s != null;
            shadow.enabled = s != null;
            if (s == null) return;
            float bob = flies ? Mathf.Sin(ageMs / BobPeriodMs * Mathf.PI * 2f) * LegacyBob + 10f : 0f; // Legacy pets hover; walkers stand on their feet
            float pop = state == State.Attack && walk == null ? 1f + 0.25f * Mathf.Sin(Mathf.Clamp01(attackMs / PetConfig.AttackAnimMs) * Mathf.PI) : 1f; // a still picture lunges a little
            transform.position = new Vector3(pos.x, pos.y + bob, 0f);
            transform.localScale = new Vector3(scale * pop, scale * pop, 1f);
            int order = GameConfig.OrderBase(pos.y);
            sr.sortingOrder = order + 3;
            shadow.sortingOrder = order;
            shadow.transform.localPosition = new Vector3(0f, -bob, 0f);
            float sw = Height * 0.9f / Mathf.Max(0.01f, scale);
            shadow.transform.localScale = new Vector3(sw, sw, 1f);
            shadow.color = new Color(1f, 1f, 1f, 0.3f);
        }
    }
}
