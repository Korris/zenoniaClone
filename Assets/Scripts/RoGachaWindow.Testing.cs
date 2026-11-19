using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ZenoniaProto
{
    /// <summary>
    /// RoGachaWindow: what capture runs press (DebugCapture.UiGacha.cs). TestClick sends a real pointer click through the EventSystem to the
    /// object at a path under the window ("Pull1", "RightPanel/TabHistory" ...), so the buttons are tested as a tap would hit them.
    /// </summary>
    public sealed partial class RoGachaWindow
    {
        /// <summary>Pull x1 / x10 as the buttons do; a x10 result is shown complete (as after Skip) unless `skip` is false.</summary>
        public static void TestPull(int count, bool skip = true)
        {
            if (!IsOpen) return;
            instance.Pull(count);
            if (skip) instance.RevealAll();
            instance.Refresh();
        }

        /// <summary>"rates", "history", "rules", "buy", "ok", "close" (popup), "next" / "prev" (carousel).</summary>
        public static void TestPress(string what)
        {
            if (!IsOpen) return;
            switch (what)
            {
                case "rates": instance.tab = Tab.Rates; break;
                case "history": instance.tab = Tab.History; instance.historyPage = 0; break;
                case "rules": instance.popup = Popup.Rules; break;
                case "buy": instance.popup = Popup.Buy; break;
                case "ok": instance.resultCount = 0; break;
                case "close": instance.popup = Popup.None; break;
                case "next": instance.TurnCarousel(1); break;
                case "prev": instance.TurnCarousel(-1); break;
            }
            instance.Refresh();
        }

        /// <summary>A pointer click on the object at `path` under the window, through the EventSystem (false = not found or not clickable).</summary>
        public static bool TestClick(string path)
        {
            if (!IsOpen) return false;
            Transform t = instance.window.Find(path);
            if (t == null || EventSystem.current == null) { Debug.LogWarning("[ZenoniaProto] gacha click: no " + path); return false; }
            var data = new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left };
            GameObject hit = ExecuteEvents.GetEventHandler<IPointerClickHandler>(t.gameObject);
            if (hit == null) { Button b = t.GetComponentInChildren<Button>(); if (b != null) hit = b.gameObject; } // a cell: its back image takes the tap
            if (hit != null) // what a real tap at the button's centre would hit first (catches a button covered by another graphic)
            {
                var rt = (RectTransform)hit.transform;
                data.position = RectTransformUtility.WorldToScreenPoint(null, rt.TransformPoint(rt.rect.center));
                var hits = new System.Collections.Generic.List<RaycastResult>();
                EventSystem.current.RaycastAll(data, hits);
                GameObject top = hits.Count > 0 ? ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject) : null;
                if (top != hit) Debug.LogWarning("[ZenoniaProto] gacha click " + path + ": a tap would hit " + (hits.Count > 0 ? hits[0].gameObject.name : "nothing"));
            }
            bool ok = hit != null && ExecuteEvents.Execute(hit, data, ExecuteEvents.pointerClickHandler);
            Debug.Log("[ZenoniaProto] gacha click " + path + " -> " + (ok ? hit.name : "nothing"));
            return ok;
        }

        /// <summary>Clicks every ClickPaths button from the plain counter, a second after opening (layout and raycasters settled), then logs the count.</summary>
        public static void TestClickAll() { if (IsOpen) instance.StartCoroutine(instance.ClickAll()); }

        System.Collections.IEnumerator ClickAll()
        {
            yield return new WaitForSecondsRealtime(1f);
            int ok = 0;
            foreach (string b in ClickPaths)
            {
                if (!IsOpen) Open();
                popup = Popup.None; resultCount = 0; ToastsOnTop(); Refresh();
                yield return null;
                if (TestClick(b)) ok++;
                yield return null;
            }
            Debug.Log("[ZenoniaProto] gacha click test: " + ok + " / " + ClickPaths.Length + " buttons answered");
        }

        /// <summary>Every button of the window, for the click test (paths under the window).</summary>
        public static readonly string[] ClickPaths =
        {
            "Help", "CoinsPill/Plus", "CrystalsPill/Plus", "GuaranteePanel/PityInfo", "GuaranteePanel/Exchange", "BuyCoins",
            "ArrowLeft", "ArrowRight", "Card2", "RightPanel/TabHistory", "RightPanel/TabRates", "RightPanel/Pet3", "Pull1", "Pull10", "Close",
        };
    }
}
