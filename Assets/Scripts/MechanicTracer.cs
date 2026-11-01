using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Hit 3 of the mechanic fires both pistols but rolls damage once: the real bolt is the right pistol's, this is the left one's picture. No hit box, no event, never on the server.
    /// It flies a short way and ends on its own, so it needs no cleanup on a map change.
    /// </summary>
    public sealed class MechanicTracer : MonoBehaviour
    {
        const float LifeSeconds = 0.22f;
        Vector2 pos, velocity;
        float age;
        Sprite[] frames;
        SpriteRenderer sr;

        public static void Fire(Vector2 from, Vector2 dir, float angleDeg, float speed)
        {
            if (!NetMode.RendersFx) return;
            VfxSet set = VfxSprites.For(BoltStyle.Plasma);
            if (set == null) return;
            var go = new GameObject("MechanicTracer");
            var t = go.AddComponent<MechanicTracer>();
            t.pos = from;
            t.velocity = dir * speed;
            t.frames = set.Rows[VfxSet.Proj];
            t.sr = go.AddComponent<SpriteRenderer>();
            t.sr.sortingOrder = FeelConfig.FxOrder - 10;
            t.sr.sprite = t.frames[0];
            go.transform.rotation = Quaternion.Euler(0f, 0f, angleDeg);
            go.transform.position = new Vector3(from.x, from.y, 0f);
        }

        void Update()
        {
            age += Time.deltaTime;
            if (age >= LifeSeconds) { Destroy(gameObject); return; }
            pos += velocity * GameClock.Dt;
            transform.position = new Vector3(pos.x, pos.y, 0f);
            sr.sprite = frames[(int)(age * 16f) & 3];
        }
    }
}
