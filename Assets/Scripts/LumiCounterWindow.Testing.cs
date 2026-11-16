namespace ZenoniaProto
{
    /// <summary>LumiCounterWindow: what capture runs press by code (DebugCapture.UiGacha.cs). Each does what the tap does.</summary>
    public sealed partial class LumiCounterWindow
    {
        /// <summary>Pull x1 / x10 as the buttons do; a x10 result is shown complete (as after Skip) unless `skip` is false.</summary>
        public static void TestPull(int count, bool skip = true)
        {
            if (!IsOpen) return;
            instance.Pull(count);
            if (skip) instance.RevealAll();
            instance.Refresh();
        }

        /// <summary>"rates" = the Rates flag (rules paper), "history" = the History flag, "exchange" = the shard plaque, "nocoins1" / "nocoins10" = the paper of a refused pull, "ok" = the result's OK.</summary>
        public static void TestPress(string what)
        {
            if (!IsOpen) return;
            switch (what)
            {
                case "rates": instance.historyFlag = false; instance.OpenDialog(Dialog.Rules, 0); break;
                case "history": instance.historyFlag = true; instance.historyPage = 0; break;
                case "exchange": instance.OpenDialog(Dialog.Exchange, 0); break;
                case "nocoins1": instance.OpenDialog(Dialog.NoCoins, 1); break;
                case "nocoins10": instance.OpenDialog(Dialog.NoCoins, Rpg.PetRules.MaxPullsAtOnce); break;
                case "ok": instance.CloseResult(); break;
                case "cancel": instance.CloseDialog(); break;
            }
            instance.Refresh();
        }

        public static void TestHistoryPage(int page)
        {
            if (!IsOpen) return;
            instance.historyPage = page;
            instance.Refresh();
        }
    }
}
