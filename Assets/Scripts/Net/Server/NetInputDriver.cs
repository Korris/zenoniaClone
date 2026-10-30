using ZenoniaProto.Net;

namespace ZenoniaProto
{
    /// <summary>
    /// Feeds a server hero from the network instead of the keyboard: each fixed hero step consumes one input frame (Advance); its direction bits are
    /// Held(key), its press bits are Pressed(key) for that step only. A client that went silent for 250 ms makes the hero idle.
    /// </summary>
    public sealed class NetInputDriver
    {
        public readonly ServerInputState State = new ServerInputState();

        /// <summary>Per fixed hero step: takes the next input frame. False = the frame is late, the hero waits (freezes) for it.</summary>
        public bool Advance() { return State.Advance((uint)Zone.Main.TimeMs); }

        /// <summary>After a step: a second step in the same tick that repays an earlier frozen one (jitter burst).</summary>
        public bool AdvanceExtra() { return State.TakeExtra(); }

        public bool Held(GameKey k)
        {
            switch (k)
            {
                case GameKey.Up: return State.Has(InputBits.Up);
                case GameKey.Down: return State.Has(InputBits.Down);
                case GameKey.Left: return State.Has(InputBits.Left);
                case GameKey.Right: return State.Has(InputBits.Right);
                default: return false;
            }
        }

        /// <summary>Presses live for exactly the step of their frame (the predicting client drops an unusable press the same way).</summary>
        public bool Pressed(GameKey k)
        {
            switch (k)
            {
                case GameKey.Attack: return State.Has(InputBits.Attack);
                case GameKey.Dash: return State.Has(InputBits.Dash);
                default: return k >= GameKey.Skill1 && k <= GameKey.Skill8 && InputBits.PressesSkill(State.Current, k - GameKey.Skill1);
            }
        }
    }
}
