using System.Collections.Generic;

namespace ZenoniaProto
{
    /// <summary>
    /// One step of a combo chain (web COMBO_CONFIG*). Times in ms, distances in world units.
    /// Melee steps use Reach / Width / FxScale; ranged steps fire Bolt instead (Reach 0).
    /// </summary>
    public sealed class ComboHit
    {
        public readonly float WindupMs, ActiveMs, RecoverMs, Mult, Reach, Width, Knockback, StepPx, FxScale;
        public readonly BoltDef Bolt; // null = melee swing
        public readonly int Stabs;    // melee hits per step: 2 = double stab (second hit 90 ms after the first, assassin finisher)

        public ComboHit(float windup, float active, float recover, float mult, float reach, float width, float knockback, float stepPx, float fxScale, BoltDef bolt = null, int stabs = 1)
        {
            WindupMs = windup; ActiveMs = active; RecoverMs = recover; Mult = mult; Reach = reach;
            Width = width; Knockback = knockback; StepPx = stepPx; FxScale = fxScale; Bolt = bolt; Stabs = stabs;
        }

        public float TotalMs { get { return WindupMs + ActiveMs + RecoverMs; } }
    }

    /// <summary>Projectile parameters of a ranged combo step (web `bolt`): speed u/s, range u, FX sprite name, scale, hit box, optional AoE burst.</summary>
    public sealed class BoltDef
    {
        public readonly float Speed, Range, Scale, W, H, ExplodeRadius;
        public readonly string Sprite, ExplodeFx, FlashFx;
        public readonly BoltStyle Style; // Arcane = wizard orb + ribbons, HolyArrow = cleric light arrow, Plasma = mechanic plasma bolt, Sprite = painted art

        public BoltDef(float speed, float range, string sprite, float scale, float w, float h, float explodeRadius = 0f, string explodeFx = null, string flashFx = null, BoltStyle style = BoltStyle.Sprite)
        {
            Speed = speed; Range = range; Sprite = sprite; Scale = scale; W = w; H = h;
            ExplodeRadius = explodeRadius; ExplodeFx = explodeFx; FlashFx = flashFx; Style = style;
        }
    }

    public enum ComboEventType { Hit, End }

    public struct ComboEvent
    {
        public ComboEventType Type;
        public int Step;
    }

    public enum ComboPhase { Idle, Windup, Active, Recover }

    /// <summary>
    /// Four-hit combo state machine ported from the web ComboController: idle -> windup -> active -> recover -> (next | idle).
    /// A press during a swing is buffered; after a swing ends there is a chain window to continue the chain.
    /// Pure logic (no Unity types) so it can be unit-tested.
    /// </summary>
    public sealed class ComboSystem
    {
        public const float FinisherMultPerLevel = 0.05f;

        readonly ComboHit[] hits;
        readonly float chainWindowMs;
        ComboPhase phase = ComboPhase.Idle;
        int step;
        int lastStep;
        float timer;
        float chainTimer;
        bool queued;

        public ComboSystem(ComboHit[] config, float chainWindow) { hits = config; chainWindowMs = chainWindow; }

        public ComboPhase Phase { get { return phase; } }
        public bool IsAttacking { get { return phase != ComboPhase.Idle; } }
        public int Step { get { return step; } }
        public int HitCount { get { return hits.Length; } }
        public bool IsFinisher { get { return step == hits.Length; } }
        public ComboHit CurrentHit { get { return step > 0 ? hits[step - 1] : null; } }

        public void Reset()
        {
            phase = ComboPhase.Idle;
            step = 0;
            lastStep = 0;
            timer = 0f;
            chainTimer = 0f;
            queued = false;
        }

        /// <summary>0..1 progress through the current swing (windup + active + recover), for animation frames.</summary>
        public float PhaseProgress
        {
            get
            {
                ComboHit hit = CurrentHit;
                if (hit == null) return 0f;
                float elapsed = phase == ComboPhase.Windup ? timer
                    : phase == ComboPhase.Active ? hit.WindupMs + timer
                    : hit.WindupMs + hit.ActiveMs + timer;
                return elapsed / hit.TotalMs > 1f ? 1f : elapsed / hit.TotalMs;
            }
        }

        /// <summary>Attack pressed. Returns true when a new swing started immediately.</summary>
        public bool PressAttack()
        {
            if (phase == ComboPhase.Idle)
            {
                bool canChain = chainTimer > 0f && lastStep > 0 && lastStep < hits.Length;
                StartHit(canChain ? lastStep + 1 : 1);
                return true;
            }
            if (step < hits.Length) queued = true;
            return false;
        }

        void StartHit(int s)
        {
            step = s;
            phase = ComboPhase.Windup;
            timer = 0f;
            queued = false;
            chainTimer = 0f;
        }

        /// <summary>Advances time (ms). Appends Hit / End events to the (caller-cleared) list.</summary>
        public void Update(float dtMs, List<ComboEvent> events)
        {
            if (phase == ComboPhase.Idle)
            {
                if (chainTimer > 0f)
                {
                    chainTimer -= dtMs;
                    if (chainTimer <= 0f) lastStep = 0;
                }
                return;
            }

            ComboHit hit = CurrentHit;
            timer += dtMs;
            if (phase == ComboPhase.Windup && timer >= hit.WindupMs)
            {
                phase = ComboPhase.Active;
                timer -= hit.WindupMs;
                events.Add(new ComboEvent { Type = ComboEventType.Hit, Step = step });
            }
            if (phase == ComboPhase.Active && timer >= hit.ActiveMs)
            {
                phase = ComboPhase.Recover;
                timer -= hit.ActiveMs;
            }
            if (phase == ComboPhase.Recover && timer >= hit.RecoverMs)
            {
                events.Add(new ComboEvent { Type = ComboEventType.End, Step = step });
                FinishHit();
            }
        }

        void FinishHit()
        {
            int finished = step;
            bool isLast = finished >= hits.Length;
            if (queued && !isLast)
            {
                lastStep = finished;
                StartHit(finished + 1);
                return;
            }
            phase = ComboPhase.Idle;
            step = 0;
            queued = false;
            lastStep = isLast ? 0 : finished;     // the finisher resets the chain completely
            chainTimer = isLast ? 0f : chainWindowMs;
        }

        /// <summary>Damage multiplier for a step; the finisher scales with hero level.</summary>
        public float MultiplierFor(int s, int heroLevel)
        {
            if (s < 1 || s > hits.Length) return 1f;
            ComboHit hit = hits[s - 1];
            return s == hits.Length ? hit.Mult + FinisherMultPerLevel * (heroLevel - 1) : hit.Mult;
        }
    }
}
