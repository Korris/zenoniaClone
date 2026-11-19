using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// PetRoWindow hooks for capture runs (DebugCapture.Pets.cs): --open-pets team|collection|growth|capture + --pet-select id pick the page and pet;
    /// --pet-ro "a,b,..." presses in order: levelup, starup, team, tab:0-2, slot:0-4 (swap / chip), card:N, page:next, own:N, rarity:N, sort, help,
    /// bonus, rename, lock, skill:0-3, dropdown:own|rarity, nav:0-3; --pet-ro-clicktest clicks every button of the window through the EventSystem
    /// (raycast at its centre: the topmost hit must be the button itself, then a pointer click) and logs "[PetRoClick] name: ok|BLOCKED by X -> message".
    /// </summary>
    public sealed partial class PetRoWindow
    {
        public static void TestShow(string pageName, string petId)
        {
            if (!IsOpen) return;
            PetDef d = PetConfig.Get(petId);
            if (d != null) instance.selected = PetConfig.IndexOf(d);
            Page pg = pageName == "collection" ? Page.Collection : pageName == "growth" ? Page.Growth : pageName == "capture" ? Page.Capture : Page.Team;
            instance.SetPage(pg);
        }

        public static void TestPress(string what)
        {
            if (!IsOpen) return;
            PetRoWindow w = instance;
            string[] kv = what.Split(':');
            int n = kv.Length > 1 ? int.Parse(kv[1]) : 0;
            switch (kv[0])
            {
                case "levelup": w.LevelUp(); break;
                case "starup": w.StarUp(); break;
                case "team": w.SetToTeam(); break;
                case "tab": w.detailTab = n; w.Refresh(); break;
                case "slot": w.OnSlot(n); break;
                case "card": w.OnCard(n); break;
                case "page": w.TurnList(1); break;
                case "own": w.ownFilter = n; w.Refresh(); break;
                case "rarity": w.rarityFilter = n; w.Refresh(); break;
                case "sort": w.sortAscending = !w.sortAscending; w.Refresh(); break;
                case "help": w.Flash(PetViewModel.RulesText()); break;
                case "bonus": w.ShowBonusDetails(); break;
                case "skill": w.Flash(w.skillFull[n]); break;
                case "dropdown": w.OpenDropdown(w.listPage, kv[1] == "own" ? 286f : 398f, 104f, kv[1] == "own" ? OwnFilters : RarityFilters, i => { }); break;
                case "nav": w.SetPage((Page)n); break;
                default: Debug.LogWarning("[ZenoniaProto] --pet-ro: unknown press '" + what + "'"); break;
            }
        }

        public static void TestClickAll() { if (IsOpen) instance.StartCoroutine(instance.ClickAll()); }

        IEnumerator ClickAll()
        {
            var seen = new HashSet<Button>();
            var later = new List<Button>(); // the ones that leave the window (close, coins -> gacha)
            for (int pass = 0; pass < 4; pass++) // every menu page in turn: their buttons only exist while shown
            {
                SetPage((Page)pass);
                yield return null;
                foreach (Button b in overlay.GetComponentsInChildren<Button>(false))
                {
                    if (!seen.Add(b)) continue;
                    if (b.name == "Close" || b.transform.parent != null && b.transform.parent.name == "CoinsPill") { later.Add(b); continue; }
                    if (b.name.StartsWith("Nav")) continue; // pressed as the pass itself
                    yield return Click(b);
                    if (popup.gameObject.activeSelf) { CloseDropdown(); yield return null; }
                    if (page != (Page)pass) { SetPage((Page)pass); yield return null; }
                }
            }
            SetPage(Page.Team);
            foreach (Button b in later) { yield return Click(b); break; } // Close last: the window goes away
            Debug.Log("[PetRoClick] done, " + seen.Count + " buttons");
        }

        IEnumerator Click(Button b)
        {
            if (b == null || !b.gameObject.activeInHierarchy) yield break;
            var rt = (RectTransform)b.transform;
            Vector3 c = rt.TransformPoint(rt.rect.center);
            Vector2 screen = RectTransformUtility.WorldToScreenPoint(null, c);
            var data = new PointerEventData(EventSystem.current) { position = screen, button = PointerEventData.InputButton.Left };
            var hits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(data, hits);
            GameObject top = hits.Count > 0 ? hits[0].gameObject : null;
            bool ok = top != null && (top == b.gameObject || top.transform.IsChildOf(b.transform));
            messageText.text = "";
            string before = messageText.text;
            ExecuteEvents.Execute(b.gameObject, data, ExecuteEvents.pointerClickHandler);
            yield return null;
            yield return null;
            Debug.Log("[PetRoClick] " + Path(b.transform) + ": " + (ok ? "ok" : "BLOCKED by " + (top != null ? Path(top.transform) : "nothing")) + " -> " + (messageText.text != before ? messageText.text : "(no message)") + " | page " + page + ", tab " + detailTab + ", open " + IsOpen);
        }

        static string Path(Transform t) { return t.parent != null ? t.parent.name + "/" + t.name : t.name; }
    }
}
