using System;
using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>Shared context of one running choreography (aim, caster, art, projectile source, sound hook).</summary>
    public sealed class ChoreoEnv
    {
        public Vector2 Origin, Dir;
        public Transform Follow;
        public Func<Vector2> Head;      // live projectile position (optional; otherwise the Travel anchor interpolates Origin -> end)
        public Action<string> Sfx;      // cue names: cast / loop / impact / break (optional)
        public SkillVfxSet Art;
    }

    /// <summary>One running phase: its sprite + additive glow + halo, ground telegraph / decal, and its one-shot events (particles, rings, flash, shake, hit-stop, sound).</summary>
    public sealed class ChoreoLayer
    {
        const float GroundDrop = 5f, Fps = 14f;
        public readonly ChoreoPhase P;
        public readonly float Start, End;
        public bool Done;
        readonly Vector2 pos;           // aim / end point (sprite centre height)
        readonly ChoreoEnv env;
        readonly Transform parent;
        GameObject holder;
        SpriteRenderer sr, glow, halo, tele, decal;
        float[] carry;
        bool started;

        public ChoreoLayer(ChoreoPhase p, float start, float end, Vector2 at, ChoreoEnv env, Transform parent) { P = p; Start = start; End = end; pos = at; this.env = env; this.parent = parent; }

        public void Tick(float ageMs, float dtMs)
        {
            if (Done || ageMs < Start) return;
            if (!started) Begin();
            if (ageMs >= End) { Release(); return; }
            Apply((ageMs - Start) / Mathf.Max(1f, End - Start), ageMs - Start, dtMs);
        }

        public void Release() { Done = true; if (holder != null) UnityEngine.Object.Destroy(holder); }

        Vector2 Centre(float t)
        {
            var off = new Vector2(P.OffX, P.OffY) * SkillChoreoPrimitives.Tile;
            switch (P.At)
            {
                case ChoreoAnchor.Caster: return (env.Follow != null ? (Vector2)env.Follow.position + new Vector2(0f, 6f) : env.Origin) + off;
                case ChoreoAnchor.Travel: return env.Head != null ? env.Head() : Vector2.Lerp(env.Origin, pos, t);
                case ChoreoAnchor.Sky:
                    Vector2 land = pos + off + new Vector2(0f, P.Width * SkillChoreoPrimitives.Tile * 0.8f);
                    return Vector2.Lerp(land + new Vector2(0f, P.SkyTiles * SkillChoreoPrimitives.Tile), land, t * t);
                default: return pos + off;
            }
        }

        float RadiusTiles { get { return Mathf.Max(0.6f, P.Width * 0.5f, P.TeleR1, P.DecalR1); } }

        void Begin()
        {
            started = true;
            holder = new GameObject("Choreo_" + P.From);
            holder.transform.SetParent(parent, false);
            SkillVfxSet art = env.Art;
            int row = (int)P.Row;
            if (row >= 0 && art != null && art.Rows[row] != null)
            {
                sr = UltimateFx.Layer(holder.transform, "Sprite", out glow);
                sr.sortingOrder = row == 0 ? FeelConfig.FxOrder - 7 : FeelConfig.FxOrder + 2; glow.sortingOrder = sr.sortingOrder + 1;
                if (P.GlowAlpha > 0f)
                {
                    halo = ProceduralSprites.Child(holder.transform, "Halo", FxSprites.Dot());
                    halo.sharedMaterial = FeelMaterials.Additive; halo.sortingOrder = sr.sortingOrder - 1;
                }
            }
            int g = UltimateFx.GroundFxOrder;
            if (P.TeleColor != 0) tele = SkillChoreoPrimitives.GroundLayer(holder.transform, "Telegraph", P.TeleLine ? ProceduralSprites.White(true) : FxSprites.Ring(), g);
            if (P.DecalColor != 0) decal = SkillChoreoPrimitives.GroundLayer(holder.transform, "Decal", FxSprites.Dot(), g + 1);
            carry = new float[P.Bursts.Count];
            Vector2 c = Centre(0f), ground = c - new Vector2(0f, GroundDrop);
            if (!P.Stream) foreach (ChoreoBurst b in P.Bursts) SkillChoreoPrimitives.Burst(b, b.Kind == ChoreoParticle.Embers ? c : ground, RadiusTiles, Converges);
            foreach (ChoreoRing r in P.Rings) SkillChoreoPrimitives.Ring(r, ground);
            SkillChoreoPrimitives.Impact(P);
            if (P.Sfx != null && env.Sfx != null) env.Sfx(P.Sfx);
        }

        bool Converges { get { return P.Row == ChoreoRow.Cast && P.At != ChoreoAnchor.Travel; } }

        void Apply(float t, float ms, float dtMs)
        {
            Vector2 c = Centre(t), ground = c - new Vector2(0f, GroundDrop);
            float fade = Mathf.Clamp01((1f - t) / (P.Loop ? 0.12f : 0.22f));
            if (sr != null) ApplySprite(c, t, ms, fade);
            if (tele != null) ApplyTelegraph(ground, t);
            if (decal != null)
            {
                float d = Mathf.Lerp(P.DecalR0, P.DecalR1, t) * SkillChoreoPrimitives.Tile * 2f;
                decal.transform.position = ground; decal.transform.localScale = new Vector3(d, d * 0.72f, 1f);
                decal.color = SkillChoreoPrimitives.Rgb(P.DecalColor, P.DecalAlpha * Mathf.Min(1f, t * 8f) * Mathf.Clamp01((1f - t) / 0.3f));
            }
            if (P.Stream && dtMs > 0f)
                for (int i = 0; i < carry.Length; i++)
                {
                    carry[i] += P.Bursts[i].Count * dtMs / Mathf.Max(1f, End - Start);
                    int n = (int)carry[i]; carry[i] -= n;
                    if (n > 0) SkillChoreoPrimitives.Burst(new ChoreoBurst { Kind = P.Bursts[i].Kind, Count = n, Color = P.Bursts[i].Color }, c, RadiusTiles * 0.6f, Converges);
                }
        }

        void ApplySprite(Vector2 c, float t, float ms, float fade)
        {
            int row = (int)P.Row, count = P.F1 - P.F0 + 1;
            Sprite[] frames = env.Art.Rows[row];
            int f = P.F0 + (P.Loop ? (int)(ms / 1000f * Fps) % count : Mathf.Min(count - 1, (int)(t * count)));
            float s = P.Width * SkillChoreoPrimitives.Tile / (env.Art.WidestPx[row] * 0.01f);
            float ang = P.At == ChoreoAnchor.Travel ? Mathf.Atan2(env.Dir.y, env.Dir.x) * Mathf.Rad2Deg : 0f;
            sr.sprite = frames[Mathf.Min(f, frames.Length - 1)]; glow.sprite = sr.sprite;
            sr.transform.SetPositionAndRotation(c, Quaternion.Euler(0f, 0f, ang)); sr.transform.localScale = new Vector3(s, s, 1f);
            sr.color = new Color(1f, 1f, 1f, fade * P.SpriteAlpha);
            glow.color = SkillChoreoPrimitives.Rgb(P.GlowColor, P.GlowAlpha * fade);
            if (halo != null)
            {
                float h = P.Width * SkillChoreoPrimitives.Tile * 1.1f;
                halo.transform.position = c; halo.transform.localScale = new Vector3(h, h * 0.8f, 1f);
                halo.color = SkillChoreoPrimitives.Rgb(P.GlowColor, P.GlowAlpha * 0.18f * fade);
            }
        }

        void ApplyTelegraph(Vector2 ground, float t)
        {
            float a = (0.3f + 0.4f * t) * (P.TeleLine ? 0.45f : 1f) * Mathf.Clamp01((1f - t) / 0.2f);
            tele.color = SkillChoreoPrimitives.Rgb(P.TeleColor, a);
            if (P.TeleLine)
            {
                float len = P.TeleR1 * SkillChoreoPrimitives.Tile, w = SkillChoreoPrimitives.Tile * 0.7f;
                Vector2 from = env.Follow != null ? (Vector2)env.Follow.position : env.Origin - new Vector2(0f, 6f);
                tele.transform.SetPositionAndRotation(from, Quaternion.Euler(0f, 0f, Mathf.Atan2(env.Dir.y, env.Dir.x) * Mathf.Rad2Deg));
                tele.transform.localScale = new Vector3(len * Mathf.Min(1f, 0.4f + t * 3f), w, 1f);
                return;
            }
            float d = Mathf.Lerp(P.TeleR0, P.TeleR1, t) * SkillChoreoPrimitives.Tile * 2f;
            tele.transform.position = ground; tele.transform.localScale = new Vector3(d, d * 0.72f, 1f);
        }
    }
}
