using System.Collections.Generic;
using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// The pet of the Pet slot, for every class: flies at the hero's side with a gentle bob, on the side HeroCompanion leaves free (Mechanic's turret keeps its own), y-sorted by its own ground point.
    /// One static image (Resources/Sprites/pets/&lt;id&gt;/art.png) of any size, drawn PetHeight units tall and flipped to look the way the hero faces; up and down keep the last side. No image yet = nothing is drawn.
    /// View only: HeroRenderer.PetId says what to show, from the equipment or the replicated look. A headless server never creates one.
    /// </summary>
    public sealed class HeroPet : MonoBehaviour
    {
        // Index == facing: down, up, left, right. HeroCompanion stands at +x, -x, +x, -x of the same list.
        static readonly Vector2[] Offsets = { new Vector2(-15f, 3f), new Vector2(15f, -3f), new Vector2(-16f, 1f), new Vector2(16f, 1f) };
        const float Hover = 12f, PetHeight = 11f, FollowMs = 200f, SnapDistance = 90f, MaxStepMs = 100f, BobPeriodMs = 1700f, BobUnits = 1.4f;
        const bool ArtLooksRight = false; // the art faces a little to the left, tail on the right

        static readonly Dictionary<string, Sprite> Art = new Dictionary<string, Sprite>();

        HeroController hero;
        SpriteRenderer sr;
        string shownId;
        Vector2 pos;
        float ageMs;

        public static HeroPet Create(Transform parent, HeroController owner)
        {
            if (!NetMode.RendersFx) return null;
            var go = new GameObject("Pet");
            go.transform.SetParent(parent, false);
            var r = go.AddComponent<HeroPet>();
            r.hero = owner;
            r.sr = go.AddComponent<SpriteRenderer>();
            r.pos = owner.Position + Offsets[owner.Facing];
            return r;
        }

        static Sprite Load(string id)
        {
            Sprite s;
            if (Art.TryGetValue(id, out s) && s != null) return s;
            var tex = Resources.Load<Texture2D>("Sprites/pets/" + id + "/art");
            s = tex != null ? Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect) : null;
            if (s == null) Debug.LogWarning("[ZenoniaProto] no pet art at Sprites/pets/" + id + "/art");
            Art[id] = s;
            return s;
        }

        void LateUpdate()
        {
            if (hero == null) { Destroy(gameObject); return; }
            HeroRenderer view = hero.View;
            string id = view != null && !hero.IsDead ? view.PetId : null;
            if (id != shownId)
            {
                shownId = id;
                sr.sprite = id != null ? Load(id) : null;
                pos = hero.Position + Offsets[hero.Facing]; // a pet just put on appears at the hero, it does not fly in from where the last one was
            }
            if (sr.sprite == null) return;
            Vector2 target = hero.Position + Offsets[hero.Facing];
            float dtMs = Mathf.Min(MaxStepMs, GameClock.Dt * 1000f);
            ageMs += dtMs;
            if (Vector2.Distance(target, pos) > SnapDistance) pos = target;
            else pos += (target - pos) * (1f - Mathf.Exp(-dtMs / FollowMs));
            float bob = Mathf.Sin(ageMs / BobPeriodMs * Mathf.PI * 2f) * BobUnits;
            transform.position = new Vector3(pos.x, pos.y + Hover + bob, 0f);
            if (hero.Facing >= 2) sr.flipX = (hero.Facing == 2) == ArtLooksRight;
            float k = PetHeight / sr.sprite.bounds.size.y;
            transform.localScale = new Vector3(k, k, 1f);
            sr.sortingOrder = GameConfig.OrderBase(pos.y) + 3;
        }
    }
}
