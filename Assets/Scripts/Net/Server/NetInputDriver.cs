using ZenoniaProto.Net;

namespace ZenoniaProto
{
    /// <summary>
    /// Feeds a server hero from the network instead of the keyboard: held direction bits become Held(key), the press counters become one Pressed(key)
    /// per tick (HeroController asks once per key per tick), stale input (client silent for 250 ms) releases everything.
    /// </summary>
    public sealed class NetInputDriver
    {
        public readonly ServerInputState State = new ServerInputState();

        public bool Held(GameKey k)
        {
            switch (k)
            {
                case GameKey.Up: return State.HeldBit(InputBits.Up);
                case GameKey.Down: return State.HeldBit(InputBits.Down);
                case GameKey.Left: return State.HeldBit(InputBits.Left);
                case GameKey.Right: return State.HeldBit(InputBits.Right);
                case GameKey.Attack: return State.HeldBit(InputBits.Attack);
                case GameKey.Dash: return State.HeldBit(InputBits.Dash);
                case GameKey.Skill: return State.HeldBit(InputBits.Skill);
                default: return false;
            }
        }

        public bool Pressed(GameKey k)
        {
            switch (k)
            {
                case GameKey.Attack: return State.TakePress(PressKind.Attack);
                case GameKey.Dash: return State.TakePress(PressKind.Dash);
                case GameKey.Skill: return State.TakePress(PressKind.Skill);
                default: return false;
            }
        }
    }
}
