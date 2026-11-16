using System;
using UnityEngine;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// Capture flags of the rebuilt pet gacha, Lumi's counter (LumiCounterWindow; phase 09 of plans/261116-1206-adventure-paper-ui-rebuild).
    /// Use them with --ui-new gacha --open-gacha and the pet flags of DebugCapture.Pets.cs (--pet-tickets N, --pet-crystals N, --pity SSR,UR,
    /// --ur-shards N, --pet ID, --give-pets). On the new look the old flags mean: --open-gacha rates = the rules paper, --open-gacha history =
    /// the History flag, --gacha-pull 1|10 = the Pull button (the result shows; a pull without the coins shows the "not enough coins" paper).
    ///   --gacha-history N           N real pulls made for free before the shot (fills the history, moves the counters); works on the old window too
    ///   --gacha-history-page N      history page N (1 = newest)
    ///   --gacha-confirm exchange    the UR shard exchange paper
    ///   --gacha-press a,b           press in order, after the pull: rates, history, exchange, nocoins1, nocoins10, ok, cancel
    ///   --gacha-reveal              leave a x10 result turning its cards up one by one (default: shown complete, as after Skip)
    /// Keys: --ui-keys "1=PetGacha;2=PetGacha;3=PetGacha;4=Close" (DebugCapture.UiRedesign.cs) presses the real keys.
    /// </summary>
    public partial class DebugCapture
    {
        partial void DebugUiGacha()
        {
            PlayerState p = RpgSession.Player;
            PlayerRpg owner = RpgSession.Owner;
            string fill = Val("--gacha-history");
            if (fill != null && p != null && owner != null && owner.Account != null)
            {
                int n = int.Parse(fill);
                long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                owner.Account.Run("capture", a => { for (int i = 0; i < n; i++) PetGacha.PullOne(a, RpgRandom.Default, now - (n - i) * 60000L); return ActionResult.Success(""); }, r => { });
                p.AttachAccount(owner.Account.Live);
            }
            if (!LumiCounterWindow.IsOpen) return; // the old look: only the data above applies (DebugCapture.Pets.cs drives the old window)

            string tab = Val("--open-gacha"), page = Val("--gacha-history-page"), pull = Val("--gacha-pull"), press = Val("--gacha-press");
            if (tab == "rates" || tab == "history") LumiCounterWindow.TestPress(tab);
            if (page != null) LumiCounterWindow.TestHistoryPage(int.Parse(page) - 1);
            if (pull != null) LumiCounterWindow.TestPull(int.Parse(pull), !Flag("--gacha-reveal"));
            if (Val("--gacha-confirm") == "exchange") LumiCounterWindow.TestPress("exchange");
            if (press != null) foreach (string b in press.Split(',')) LumiCounterWindow.TestPress(b.Trim());
        }
    }
}
