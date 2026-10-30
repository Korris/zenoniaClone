using UnityEngine;
using ZenoniaProto.Net;

namespace ZenoniaProto
{
    /// <summary>
    /// Fixed-step plumbing of the hero: 1/30 s steps for the online local hero (client-side prediction, LocalHeroPredictor drives them) and for the
    /// server-side hero of a remote player (one input frame per step, StepGate pulls it). Offline and host-own heroes keep the variable-dt update.
    /// </summary>
    public partial class HeroController
    {
        public const float FixedDt = 1f / 30f;
        const int MaxCatchUpSteps = 4;

        LocalHeroPredictor predictor;
        bool predicted;
        /// <summary>True while the predictor re-simulates already shown steps after a correction: no FX, sounds or spent resources.</summary>
        bool replaying;
        float stepAcc;

        /// <summary>Server: asked once per fixed step; true = simulate this step (the player's next input frame is in), false = freeze (frame late).</summary>
        public System.Func<bool> StepGate;
        /// <summary>Server: asked after each step; true = take one more step this tick (repays a frozen tick when a backlog of frames exists).</summary>
        public System.Func<bool> ExtraStepGate;

        public bool IsPredicted { get { return predicted; } }
        public LocalHeroPredictor Predictor { get { return predictor; } }
        /// <summary>The warrior / wizard skills are pictures + damage the server decides, so they start at once; blink and heal wait for the server.</summary>
        public bool PredictsSkillFx { get { return PredictsSkill; } }
        bool PredictsSkill { get { return cls.Id == HeroClass.Warrior || cls.Id == HeroClass.Wizard; } }

        /// <summary>Online local hero: from now on it simulates itself in fixed steps from the player's own controls and is corrected by the server.</summary>
        public void AttachPredictor(LocalHeroPredictor p)
        {
            predictor = p;
            predicted = true;
            proxy = false;
            AiHeld = p.Held;
            AiPressed = p.Pressed;
        }

        void GatedSteps(float dt)
        {
            stepAcc += dt;
            int n = 0;
            while (stepAcc + 1e-4f >= FixedDt && n < MaxCatchUpSteps)
            {
                stepAcc -= FixedDt; n++;
                if (!StepGate()) continue;
                StepOnce(FixedDt);
                if (ExtraStepGate != null && ExtraStepGate()) StepOnce(FixedDt);
            }
            if (n == MaxCatchUpSteps) stepAcc = 0f; // far behind (editor pause, long hitch): do not burst
        }

        // ---- called by LocalHeroPredictor ----

        /// <summary>Runs one predicted step with the predictor's current input frame.</summary>
        public void PredictStep() { StepOnce(FixedDt); }

        public void SetReplaying(bool on) { replaying = on; }

        public Vector2 SimPosition { get { return new Vector2(x, y); } }

        public void RememberPredictedBolt(ComboProjectile shot)
        {
            if (shot == null) return;
            shot.MarkPredicted(this);
        }

        /// <summary>Server event BoltFired of the local hero: names the matching predicted bolt. False = nothing predicted, draw it from the event.</summary>
        public bool AdoptPredictedBolt(ushort serverId) { return predicted && ComboProjectile.AdoptPredicted(this, serverId); }
    }
}
