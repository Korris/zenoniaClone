using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Warrior tree skill BLADE CYCLONE (concept sheet in ArtLibrary/custom/blade-cyclone-frames). Follows the caster, who can keep walking after the short cast lock.
    /// Timeline (ms): cast ring and light column 0-500, spin ring 450-1300, peak vortex 1250-2250, finish burst 2250-2750. Six waves hit around the hero at 500, 850, 1200, 1550, 1900, 2250;
    /// the last one also knocks enemies back (bosses are too heavy, Monster.TakeHit). Every frame is cut at the ring's centre line: the upper half draws behind the hero and the lower half in front.
    /// Unity extras: additive bloom layers, shock rings, debris + dust + ember particles, and for the local caster only a shake per wave, then hit-stop, punch-zoom and a soft flash on the last.
    /// </summary>
    public class BladeCycloneUltimate : MonoBehaviour
    {
        public const float Radius = Rpg.LegacySkillNumbers.BladeCycloneRadius, Mult = Rpg.LegacySkillNumbers.BladeCycloneMult, Knockback = Rpg.LegacySkillNumbers.BladeCycloneKnockback;
        const float CastMs = 500f, SpinFromMs = 450f, SpinToMs = 1300f, PeakFromMs = 1250f, PeakToMs = 2250f, FinishMs = 500f, TotalMs = PeakToMs + FinishMs;
        const float SpinFrameMs = 60f, PeakFrameMs = 50f, CrossMs = 60f;
        const float UnitsPerPx = 0.237f;  // the painted ring spans 371 of the 384 px cell: outer edge just past the real radius
        const float RingLift = 5f;        // ring centre above the feet: around the hero's legs
        const float CastAnchorY = 340f, RingAnchorY = 192f, AnchorX = 192f;
        const int Frames = 6;
        const string Folder = "Sprites/fx/bladecyclone/";
        const string LoopClip = "Audio/sfx/skills/warrior/blade-cyclone-loop";
        static readonly float[] WaveAt = { 500f, 850f, 1200f, 1550f, 1900f, 2250f };
        static readonly Color Red = new Color(1f, 0.28f, 0.1f), Hot = new Color(1f, 0.62f, 0.2f);

        /// <summary>One frame strip cut in two at the ring's centre line.</summary>
        sealed class Strip { public Sprite[] Back = new Sprite[Frames], Front = new Sprite[Frames]; }
        sealed class Pair { public SpriteRenderer Back, BackGlow, Front, FrontGlow; }

        static Strip castArt, spinArt, peakArt, finishArt;

        HeroController hero;
        ISkillBody body; // the zone pump finds a skill object's caster through this field: a portal transfer ends the cyclone with its hero
        float age, atk;
        int waves;
        bool local;
        Pair cast, spin, peak, finish;
        SpriteRenderer groundGlow;
        AudioSource loop;

        public static void Cast(HeroController owner) { Cast(owner, 1f); }

        /// <summary>Skill-tree cast: `damageScale` is the rank multiplier (SkillDef.DamageMul) applied to the caster ATK of this cast.</summary>
        public static void Cast(HeroController owner, float damageScale)
        {
            var go = new GameObject("BladeCyclone");
            go.transform.SetParent(owner.transform.parent, false);
            go.AddComponent<BladeCycloneUltimate>().Begin(owner, damageScale);
        }

        static Strip Load(string name, float anchorY)
        {
            var s = new Strip();
            for (int i = 0; i < Frames; i++)
            {
                Texture2D tex = SpriteSheet.Load(Folder + name + "-" + (i + 1));
                if (tex == null) return null;
                float below = tex.height - anchorY, ppu = 1f / UnitsPerPx, px = AnchorX / tex.width;
                s.Back[i] = Sprite.Create(tex, new Rect(0f, below, tex.width, anchorY), new Vector2(px, 0f), ppu, 0, SpriteMeshType.FullRect);
                s.Front[i] = Sprite.Create(tex, new Rect(0f, 0f, tex.width, below), new Vector2(px, 1f), ppu, 0, SpriteMeshType.FullRect);
            }
            return s;
        }

        /// <summary>Loads and slices the 24 painted frames ahead of the first cast (class spawn) so the cast itself never hitches.</summary>
        public static void Preload()
        {
            if (!NetMode.RendersFx) return;
            if (castArt == null) castArt = Load("cast", CastAnchorY);
            if (spinArt == null) spinArt = Load("spin", RingAnchorY);
            if (peakArt == null) peakArt = Load("peak", RingAnchorY);
            if (finishArt == null) finishArt = Load("finish", RingAnchorY);
        }

        void Begin(HeroController owner, float damageScale)
        {
            hero = owner;
            body = new HeroSkillBody(owner);
            atk = owner.AtkStat * damageScale;
            local = body.IsLocal;
            if (!NetMode.RendersFx) return; // headless server: damage timeline only, no art / layers
            Preload();
            owner.PlaySpinPose(PeakToMs); // the cast lock's pose shows first, then the hero turns inside the rings until the finish
            groundGlow = ProceduralSprites.Child(transform, "GroundGlow", FxSprites.Dot());
            groundGlow.sortingOrder = UltimateFx.GroundFxOrder + 1;
            groundGlow.transform.localScale = new Vector3(Radius * 2.3f, Radius * 1.35f, 1f);
            cast = MakePair("Cast"); spin = MakePair("Spin"); peak = MakePair("Peak"); finish = MakePair("Finish");
            AudioClip clip = Resources.Load<AudioClip>(LoopClip);
            if (clip != null)
            {
                loop = gameObject.AddComponent<AudioSource>();
                loop.clip = clip; loop.loop = true; loop.volume = local ? 0.55f : 0.25f; loop.Play();
            }
            Apply();
        }

        Pair MakePair(string name)
        {
            var p = new Pair();
            p.Back = UltimateFx.Layer(transform, name + "Back", out p.BackGlow);
            p.Front = UltimateFx.Layer(transform, name + "Front", out p.FrontGlow);
            return p;
        }

        void Update()
        {
            if (hero == null || hero.IsDead || age >= TotalMs) { Destroy(gameObject); return; } // a fallen caster ends the cyclone
            float dt = GameClock.Dt * 1000f;
            age += dt;
            if (dt > 0f && age < CastMs && NetMode.RendersFx) UltimateFx.Embers(hero.Position, 2, 18f, 30f, new Color(1f, 0.5f, 0.15f, 0.9f), 34f);
            while (waves < WaveAt.Length && age >= WaveAt[waves]) Wave(waves++);
            Apply();
        }

        void Wave(int i)
        {
            bool last = i == WaveAt.Length - 1;
            Vector2 center = hero.Position;
            SkillDamage.Radial(center, Radius, atk, Mult, last ? Knockback : 0f, last, ClassPalette.Orange, 0f, hero);
            if (!NetMode.RendersFx) return;
            Vector2 ring = center + new Vector2(0f, RingLift);
            UltimateFx.Embers(ring, last ? 44 : 14, Radius * 0.7f, last ? 90f : 55f, new Color(1f, 0.5f, 0.12f, 1f));
            UltimateFx.Debris(ring, last ? 22 : 7, Radius * 0.6f, last ? 110f : 70f);
            UltimateFx.Dust(center, last ? 14 : 4, Radius * 0.7f);
            ShockRing.Spawn(ring, Radius * (last ? 1.35f : 1f), last ? 0.42f : 0.24f, new Color(1f, 0.42f, 0.14f, 0.8f), UltimateFx.GroundFxOrder + 6);
            if (last)
            {
                GameSfx.Play("skills/warrior/earth-break-impact", 0.7f);
                ShockRing.Spawn(ring, Radius * 0.9f, 0.3f, new Color(1f, 0.8f, 0.4f, 0.6f), UltimateFx.GroundFxOrder + 7, 0.72f, 0.1f, true);
            }
            if (!local) return; // another player's cyclone never moves this player's camera
            CameraFollow.AddTrauma(last ? 0.5f : i >= 2 ? 0.14f : 0.07f);
            if (i == WaveAt.Length - 2) GameClock.HitStop(18f);
            if (!last) return;
            GameClock.HitStop(55f);
            CameraFollow.Punch(0.5f);
            ScreenFlash.Trigger(new Color(1f, 0.4f, 0.12f, 0.08f), 0.18f);
        }

        static void Show(Pair p, Strip art, int frame, float alpha, float glowAlpha, Color glow, Vector2 at, float scale, int backOrder)
        {
            bool on = art != null && alpha > 0.01f;
            p.Back.enabled = p.Front.enabled = on;
            p.BackGlow.enabled = p.FrontGlow.enabled = on && FeelConfig.FxOn;
            if (!on) return;
            Sprite b = art.Back[frame], f = art.Front[frame];
            p.Back.sprite = p.BackGlow.sprite = b;
            p.Front.sprite = p.FrontGlow.sprite = f;
            Color c = new Color(1f, 1f, 1f, alpha), g = new Color(glow.r, glow.g, glow.b, glowAlpha * alpha);
            p.Back.color = p.Front.color = c;
            p.BackGlow.color = p.FrontGlow.color = g;
            p.Back.transform.position = p.Front.transform.position = at;
            p.Back.transform.localScale = p.Front.transform.localScale = new Vector3(scale, scale, 1f);
            p.Back.sortingOrder = backOrder; p.BackGlow.sortingOrder = backOrder + 1;
            p.Front.sortingOrder = FeelConfig.FxOrder - 3; p.FrontGlow.sortingOrder = FeelConfig.FxOrder - 2;
        }

        /// <summary>1 inside [from, to], easing over `ms` at both ends.</summary>
        static float Fade(float age, float from, float to, float ms) { return Mathf.Clamp01(Mathf.Min((age - from) / ms, (to - age) / ms)); }

        static int Looped(float age, float frameMs) { return Mathf.FloorToInt(age / frameMs) % Frames; }
        static int ByProgress(float t) { return Mathf.Clamp(Mathf.FloorToInt(t * Frames), 0, Frames - 1); }

        void Apply()
        {
            if (!NetMode.RendersFx || hero == null) return;
            Vector2 feet = hero.Position, at = feet + new Vector2(0f, RingLift);
            int behind = GameConfig.OrderBase(feet.y) - 2; // just under the hero, wherever the hero walks
            groundGlow.transform.position = feet;
            groundGlow.color = new Color(Red.r, Red.g, Red.b, 0.28f * Fade(age, 0f, TotalMs, 300f));
            Show(cast, castArt, ByProgress(age / CastMs), Fade(age, -CrossMs, CastMs, CrossMs), 0.3f, Hot, at, 1f, behind);
            Show(spin, spinArt, Looped(age, SpinFrameMs), Fade(age, SpinFromMs, SpinToMs, CrossMs), 0.35f, Hot, at, 1f, behind);
            float pulse = 1.06f + 0.03f * Mathf.Sin(age * 0.02f);
            Show(peak, peakArt, Looped(age, PeakFrameMs), Fade(age, PeakFromMs, PeakToMs, CrossMs), 0.5f, Hot, at, pulse, behind);
            float t = (age - PeakToMs) / FinishMs;
            Show(finish, finishArt, ByProgress(t), t < 0f || t >= 1f ? 0f : Mathf.Min(1f, (1f - t) * 3f), 0.45f, Hot, at, 1.1f + 0.08f * t, behind);
        }
    }
}
