using UnityEngine;
using ZenoniaProto.Net;

namespace ZenoniaProto
{
    /// <summary>
    /// Client-side prediction + server reconciliation of the online local hero.
    /// The hero simulates itself in fixed 1/30 s steps from the player's own controls (same code, same FrostHollowMap.Move as the server), sends each
    /// step's input frame, and keeps the frames + the position it reached. Every snapshot says which input frame the server had simulated (LastInputSeq)
    /// and its sim state after it: when that differs from what was predicted for the same frame (position beyond a small tolerance, or a different
    /// dash / cast / combo state) the hero is put back to the server state and the newer, not yet acknowledged frames are replayed on top.
    /// The visible jump is turned into an offset that fades out over ~100 ms. The hero is drawn interpolated between its last two steps.
    /// </summary>
    public sealed class LocalHeroPredictor
    {
        const int Ring = 256;
        public const float PositionTolerance = 0.4f, SnapDistance = 24f, SmoothTauSeconds = 0.05f;

        struct Rec { public uint Seq; public float X, Y; public int State; }

        readonly HeroController hero;
        readonly NetInputSender sender;
        readonly NetStats stats;
        readonly InputSampler sampler = new InputSampler();
        readonly InputFrame[] frames = new InputFrame[Ring];
        readonly Rec[] history = new Rec[Ring];
        uint seq, firstSeq;       // last frame stepped / first frame this predictor ever stepped (older acks cannot be replayed)
        ushort bits;              // bits of the frame being simulated (Held / Pressed read them)
        float acc, sinceStep;
        Vector2 prevSim, sim, error;
        bool started;

        public LocalHeroPredictor(HeroController owner, NetInputSender inputSender, NetStats netStats)
        {
            hero = owner; sender = inputSender; stats = netStats;
        }

        public uint Seq { get { return seq; } }
        /// <summary>Where the hero is drawn: interpolated between its last two steps plus the fading correction offset.</summary>
        public Vector2 VisualPos { get { return started ? Vector2.Lerp(prevSim, sim, Mathf.Clamp01(sinceStep / HeroController.FixedDt)) + error : hero.SimPosition; } }

        // ---- input as seen by the hero's rules ----

        public bool Held(GameKey k)
        {
            switch (k)
            {
                case GameKey.Up: return (bits & InputBits.Up) != 0;
                case GameKey.Down: return (bits & InputBits.Down) != 0;
                case GameKey.Left: return (bits & InputBits.Left) != 0;
                case GameKey.Right: return (bits & InputBits.Right) != 0;
                default: return false;
            }
        }

        public bool Pressed(GameKey k)
        {
            switch (k)
            {
                case GameKey.Attack: return (bits & InputBits.Attack) != 0;
                case GameKey.Dash: return (bits & InputBits.Dash) != 0;
                case GameKey.Skill: return (bits & InputBits.Skill) != 0;
                default: return false;
            }
        }

        // ---- stepping ----

        /// <summary>Every render frame: latch presses, run the due fixed steps, fade the correction.</summary>
        public void Tick(float dt)
        {
            if (!started) { sim = prevSim = hero.SimPosition; started = true; }
            sampler.Poll();
            acc += dt;
            int n = 0;
            while (acc + 1e-4f >= HeroController.FixedDt && n < 4) { acc -= HeroController.FixedDt; n++; Step(); }
            if (n == 4) acc = 0f; // long hitch: do not run a burst of steps
            sinceStep += dt;
            error *= Mathf.Exp(-dt / SmoothTauSeconds);
            if (error.sqrMagnitude < 0.0004f) error = Vector2.zero;
        }

        void Step()
        {
            bits = sampler.Take();
            InputFrame f = sender.Push(bits);
            seq = f.Seq;
            if (firstSeq == 0) firstSeq = seq;
            frames[seq % Ring] = f;
            prevSim = hero.SimPosition;
            hero.PredictStep();
            sim = hero.SimPosition;
            sinceStep = 0f;
            Record();
            stats.Add("predictedSteps", 1);
        }

        void Record()
        {
            Vector2 p = hero.SimPosition;
            history[seq % Ring] = new Rec { Seq = seq, X = p.x, Y = p.y, State = hero.DiscreteState() | (hero.Facing << 9) };
        }

        /// <summary>The hero was placed by the server (spawn): forget the smoothing and the pose history.</summary>
        public void OnTeleport()
        {
            sim = prevSim = hero.SimPosition;
            error = Vector2.zero;
        }

        // ---- reconciliation ----

        /// <summary>Applies the owner block of a snapshot: `lastInput` = low 16 bits of the last frame the server simulated, `me` = its entry for this hero.</summary>
        public void OnServerState(ushort lastInput, HeroSnap me, OwnerSim s)
        {
            if (!started || seq == 0) return;
            uint last = ServerInputState.Expand(lastInput, seq);
            if (NetQuantize.IsNewer(firstSeq, last)) return; // the server has not reached the frames this predictor produced yet
            if (NetQuantize.IsNewer(last, seq) || seq - last >= Ring - 8) { stats.Add("reconcileSkipped", 1); return; } // server "ahead" (restart) or far too old
            var server = new Vector2(NetQuantize.Pos(me.X), NetQuantize.Pos(me.Y));
            bool serverDead = (NetQuantize.Bits(me.FacingFlags) & HeroFlags.Dead) != 0;
            int serverState = HeroController.Discrete(serverDead, s) | (NetQuantize.Facing(me.FacingFlags) << 9);
            Rec r = history[last % Ring];
            bool known = r.Seq == last;
            float err = known ? Vector2.Distance(server, new Vector2(r.X, r.Y)) : float.MaxValue;
            stats.Add("reconciles", 1);
            if (known && err <= PositionTolerance && serverState == r.State) { stats.Sample("correctionUnits", 0f); return; }

            Vector2 before = hero.SimPosition;
            hero.RestoreSim(server, NetQuantize.Facing(me.FacingFlags), serverDead, NetQuantize.Progress(me.Progress), s);
            hero.SetReplaying(true);
            try
            {
                for (uint f = last + 1; NetQuantize.IsNewer(seq + 1, f); f++)
                {
                    bits = frames[f % Ring].Bits;
                    hero.PredictStep();
                    Vector2 p = hero.SimPosition;
                    history[f % Ring] = new Rec { Seq = f, X = p.x, Y = p.y, State = hero.DiscreteState() | (hero.Facing << 9) };
                }
            }
            finally { hero.SetReplaying(false); }
            Vector2 after = hero.SimPosition;
            Vector2 delta = after - before;
            if (last == seq) history[seq % Ring] = new Rec { Seq = seq, X = after.x, Y = after.y, State = hero.DiscreteState() | (hero.Facing << 9) };
            prevSim += delta; sim = after; error -= delta;
            if (error.magnitude > SnapDistance) error = Vector2.zero; // teleport / respawn: no sliding across the map
            stats.Add("reconcilesCorrected", 1);
            stats.Sample("correctionUnits", delta.magnitude);
            stats.Max("correctionMaxUnits", delta.magnitude);
            // A server-side death / respawn or a blink cast legitimately moves the hero; anything else is a rubber band.
            int diff = known ? r.State ^ serverState : 0;
            bool teleportLike = (diff & 5) != 0 || !known;
            stats.Sample(teleportLike ? "correctionTeleport" : "correctionRubber", delta.magnitude);
            if (!teleportLike && delta.magnitude > 6f)
                Debug.Log("[ZenoniaProto] net: rubber band " + delta.magnitude.ToString("F1") + " units, frames behind " + (seq - last) + ", state diff " + diff + ", err " + err.ToString("F1"));
        }
    }
}
