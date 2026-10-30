using ZenoniaProto.Net;

namespace ZenoniaProto
{
    /// <summary>
    /// Turns the local controls (keyboard, touch buttons or a scripted bot, all through InputAdapter) into input frame bits. Presses are edge events that
    /// can happen in a render frame without a sim step, so Poll() latches them every frame and Take() hands them to the next step exactly once.
    /// With the bag open or no hero the frame is empty (the hero stands still).
    /// </summary>
    public sealed class InputSampler
    {
        ushort latched;

        public static bool Live { get { return LocalPlayer.Hero != null && !InventoryPanel.IsOpen; } }

        static void Latch(ref ushort bits, GameKey key, ushort bit) { if (InputAdapter.Pressed(key)) bits |= bit; }

        /// <summary>Every render frame.</summary>
        public void Poll()
        {
            if (!Live) { latched = 0; return; }
            Latch(ref latched, GameKey.Attack, InputBits.Attack);
            Latch(ref latched, GameKey.Dash, InputBits.Dash);
            Latch(ref latched, GameKey.Skill, InputBits.Skill);
            Latch(ref latched, GameKey.HpPotion, InputBits.HpPotion);
            Latch(ref latched, GameKey.MpPotion, InputBits.MpPotion);
        }

        /// <summary>Once per sim step: held directions now + the presses since the last step.</summary>
        public ushort Take()
        {
            ushort bits = 0;
            if (Live)
            {
                if (InputAdapter.Held(GameKey.Up)) bits |= InputBits.Up;
                if (InputAdapter.Held(GameKey.Down)) bits |= InputBits.Down;
                if (InputAdapter.Held(GameKey.Left)) bits |= InputBits.Left;
                if (InputAdapter.Held(GameKey.Right)) bits |= InputBits.Right;
                bits |= latched;
            }
            latched = 0;
            return bits;
        }
    }
}
