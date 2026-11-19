using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ZenoniaProto.Net;
using ZenoniaProto.Rpg;
using B = ZenoniaProto.InventoryRoBoxes;
using PW = ZenoniaProto.PetRoWidgets;
using W = ZenoniaProto.InventoryRoWidgets;

namespace ZenoniaProto
{
    /// <summary>
    /// PetRoWindow, Team box: the Combat (100 %) and two Support (50 %) slots on pedestals (ribbon, glow behind the combat pet, swap button, rarity +
    /// name, stars, Lv), the two Stat slots (25 % passive) as chips in the title row, the total level of the team, and the Team Bonus row (PetTeam.Bonus).
    /// Tap a pet = select it. Swap = the selected pet goes into that slot; a pet it replaces takes the slot the selected one left (PetCampWindow's rule).
    /// </summary>
    public sealed partial class PetRoWindow
    {
        const float SlotW = 160f;
        static readonly float[] SlotCx = { 110f, 277f, 444f };

        sealed class SlotView
        {
            public RectTransform Root;
            public Image Glow, Pet, Lock;
            public B.PillBox Rarity;
            public Text Name, Lv, Empty;
            public PW.Stars Stars;
            public GameObject Swap, Info;
        }

        readonly SlotView[] slots = new SlotView[3];
        readonly Image[] statIcons = new Image[2], statBacks = new Image[2];
        Text teamLevelText, noBonusText;
        RectTransform bonusRow;

        void BuildTeam(RectTransform box)
        {
            W.Img(box, "TitleIcon", 14f, 9f, 26f, 26f, "pet-title-paw");
            Text title = W.Txt(box, "Title", 46f, 4f, 160f, 36f, "Pet Team", 21, W.Ink);
            W.Clickable(W.Img(box, "Help", 46f + title.preferredWidth + 8f, 10f, 24f, 24f, "pet-btn-help", true), () => Flash(PetViewModel.RulesText()));
            W.Txt(box, "StatLabel", 196f, 4f, 60f, 36f, "Stat", 13, W.InkSoft, TextAnchor.MiddleRight);
            for (int i = 0; i < 2; i++)
            {
                int slot = (int)PetSlot.Stat1 + i;
                statBacks[i] = W.Img(box, "Stat" + i, 262f + i * 38f, 6f, 32f, 32f, "bag-slot", true);
                statIcons[i] = UiKit.Rect(statBacks[i].transform, "Icon", 3f, 3f, 26f, 26f).gameObject.AddComponent<Image>();
                statIcons[i].preserveAspect = true; statIcons[i].raycastTarget = false;
                W.Clickable(statBacks[i], () => OnSlot(slot));
            }
            Image power = W.Img(box, "LevelPill", 342f, 7f, 190f, 30f, "pet-pill-cream");
            power.pixelsPerUnitMultiplier = 104f / 60f;
            W.Img(power.transform, "Icon", 10f, 4f, 22f, 22f, "pet-ico-power");
            teamLevelText = W.Txt(power.transform, "Text", 36f, 0f, 146f, 29f, "", 14, W.Ink, TextAnchor.MiddleLeft);

            for (int i = 0; i < slots.Length; i++) slots[i] = BuildSlot(box, i);

            Image row = W.Img(box, "Bonus", 8f, 254f, CentreW - 16f, 34f, "bag-pill-white");
            row.pixelsPerUnitMultiplier = 104f / 40f;
            row.color = SoftBox;
            W.Img(row.transform, "Star", 12f, 7f, 20f, 20f, "pet-star");
            Text bt = W.Txt(row.transform, "Title", 38f, 0f, 110f, 33f, "Team Bonus", 16, W.Ink);
            W.Clickable(W.Img(row.transform, "Info", 38f + bt.preferredWidth + 6f, 7f, 20f, 20f, "pet-btn-info", true), ShowBonusDetails);
            bonusRow = UiKit.Rect(row.transform, "Parts", 38f + bt.preferredWidth + 36f, 0f, CentreW - 16f - (38f + bt.preferredWidth + 46f), 33f);
            var h = bonusRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 14f; h.childAlignment = TextAnchor.MiddleLeft;
            h.childControlWidth = h.childControlHeight = true; h.childForceExpandWidth = h.childForceExpandHeight = false;
            noBonusText = W.Txt(row.transform, "None", 38f + bt.preferredWidth + 36f, 0f, 320f, 33f, "Put pets in Combat or Stat slots", 13, W.InkSoft);
        }

        SlotView BuildSlot(RectTransform box, int i)
        {
            var v = new SlotView();
            float cx = SlotCx[i];
            v.Root = UiKit.Rect(box, "Slot" + i, cx - SlotW * 0.5f, 42f, SlotW, 206f);
            Image hit = v.Root.gameObject.AddComponent<Image>();
            hit.color = new Color(1f, 1f, 1f, 0f);
            int slot = i;
            W.Clickable(hit, () => OnSlotPet(slot));
            bool combat = i == 0;
            B.PillBox ribbon = B.Pill(v.Root, "Ribbon", SlotW * 0.5f, 0f, 28f, combat ? "pet-pill-red" : "pet-pill-blue", "", 13, Color.white, 130f, true);
            ribbon.Root.SetAsFirstSibling();
            Image ico = W.Img(ribbon.Root, "Icon", 0f, 0f, 22f, 20f, combat ? "pet-ico-swords" : "pet-ico-wings");
            ico.transform.SetAsFirstSibling();
            var le = ico.gameObject.AddComponent<LayoutElement>(); le.minWidth = le.preferredWidth = 22f; le.minHeight = le.preferredHeight = 20f;
            ribbon.Root.GetComponent<HorizontalLayoutGroup>().spacing = 4f;
            ribbon.Label.text = combat ? "Combat (100%)" : "Support (50%)";
            ribbon.Label.fontSize = 14;
            W.Rim(ribbon.Label);
            v.Glow = W.Img(v.Root, "Glow", SlotW * 0.5f - 90f, 14f, 180f, 156f, "pet-glow");
            v.Glow.enabled = combat;
            W.Img(v.Root, "Pedestal", SlotW * 0.5f - 71f, 102f, 142f, 56f, "pet-pedestal");
            v.Pet = UiKit.Rect(v.Root, "Pet", SlotW * 0.5f - 64f, 36f, 128f, 92f).gameObject.AddComponent<Image>();
            v.Pet.preserveAspect = true; v.Pet.raycastTarget = false;
            v.Pet.rectTransform.pivot = new Vector2(0.5f, 0f);
            v.Lock = W.Img(v.Root, "Lock", SlotW * 0.5f - 24f, 62f, 48f, 48f, "pet-skill-locked");
            v.Empty = W.Txt(v.Root, "Empty", 0f, 162f, SlotW, 40f, "", 13, W.InkSoft, TextAnchor.UpperCenter);
            v.Empty.horizontalOverflow = HorizontalWrapMode.Wrap;
            Image swap = W.Img(v.Root, "Swap", SlotW * 0.5f + 42f, 100f, 32f, 32f, "pet-btn-swap", true);
            W.Clickable(swap, () => OnSlot(slot));
            v.Swap = swap.gameObject;

            RectTransform nameRow = UiKit.Rect(v.Root, "NameRow", 0f, 159f, SlotW, 20f);
            var row = nameRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.spacing = 7f; row.childAlignment = TextAnchor.MiddleCenter;
            row.childControlWidth = row.childControlHeight = true; row.childForceExpandWidth = row.childForceExpandHeight = false;
            v.Rarity = PW.Rarity(nameRow, "Rarity", 0f, 0f, 20f, 13, null, 30f);
            v.Name = W.Txt(nameRow, "Name", 0f, 0f, 10f, 20f, "", 15, W.Ink, TextAnchor.MiddleLeft);
            v.Name.gameObject.AddComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            v.Info = nameRow.gameObject;
            v.Stars = new PW.Stars(v.Root, "Stars", SlotW * 0.5f, 180f, 14f, 1f, true);
            v.Lv = W.Txt(v.Root, "Lv", 0f, 194f, SlotW, 17f, "", 15, W.Ink, TextAnchor.MiddleCenter);
            return v;
        }

        void RefreshTeam(PlayerState p, AccountState a)
        {
            int total = 0;
            for (int i = 0; i < PetRules.TeamSize; i++) { PetDef d = PetConfig.Get(p.Team[i]); if (d != null && a != null) total += a.LevelOf(d.Id); }
            teamLevelText.text = "Team Level: <color=#B9811C>" + total + "</color>";
            for (int i = 0; i < slots.Length; i++) RefreshSlot(slots[i], i, p, a);
            for (int i = 0; i < 2; i++)
            {
                int slot = (int)PetSlot.Stat1 + i;
                PetDef d = PetConfig.Get(p.Team[slot]);
                bool open = p.Level >= PetRules.SlotUnlockLevel[slot];
                statIcons[i].sprite = d != null ? ZenoniaProto.PetArt.Icon(d) : W.Art(open ? null : "pet-skill-locked");
                statIcons[i].enabled = statIcons[i].sprite != null;
                statIcons[i].color = d != null ? ZenoniaProto.PetArt.TintOf(d) : Color.white;
                statBacks[i].color = d != null ? PW.CardTint(d) : Color.white;
            }
            RefreshBonus(p, a);
        }

        void RefreshSlot(SlotView v, int slot, PlayerState p, AccountState a)
        {
            PetDef d = PetConfig.Get(p.Team[slot]);
            bool open = p.Level >= PetRules.SlotUnlockLevel[slot];
            v.Lock.enabled = !open;
            v.Pet.enabled = d != null;
            v.Glow.enabled = slot == 0 && d != null;
            v.Info.SetActive(d != null);
            v.Stars.Root.gameObject.SetActive(d != null);
            v.Swap.SetActive(open);
            v.Lv.text = d != null && a != null ? "Lv. " + a.LevelOf(d.Id) : "";
            v.Empty.text = !open ? "Opens at Lv. " + PetRules.SlotUnlockLevel[slot] : d == null ? "Empty\nselect a pet, then swap" : "";
            if (d == null) return;
            Sprite art = PW.PetArt(d);
            v.Pet.sprite = art;
            v.Pet.color = ZenoniaProto.PetArt.TintOf(d);
            float scale = Mathf.Clamp(d.HeightUnits / 14f, 0.92f, 1f); // King and Golem stand taller than a slime
            v.Pet.rectTransform.sizeDelta = new Vector2(144f * scale, 104f * scale);
            v.Pet.rectTransform.anchoredPosition = new Vector2(SlotW * 0.5f, -131f); // pivot bottom-centre: feet on the pedestal's top
            PW.SetRarity(v.Rarity, d);
            v.Name.text = d.Name;
            v.Stars.Set(a != null ? a.StarsOf(d.Id) : 1);
        }

        void RefreshBonus(PlayerState p, AccountState a)
        {
            for (int i = bonusRow.childCount - 1; i >= 0; i--) Destroy(bonusRow.GetChild(i).gameObject);
            PetBonus b = PetTeam.Bonus(p.Team, a);
            int shown = 0;
            shown += BonusPart("bag-stat-atk", "DMG", b.Damage, true, shown);
            shown += BonusPart("bag-stat-def", "Taken", b.DamageTaken, false, shown);
            shown += BonusPart("bag-stat-hp", "HP", b.MaxHp, true, shown);
            shown += BonusPart("bag-stat-speed", "Speed", b.MoveSpeed, true, shown);
            shown += BonusPart("pet-ico-attack-speed", "Atk Spd", b.AttackSpeed, true, shown);
            shown += BonusPart("pet-ico-potion", "Potion", b.PotionHeal, true, shown);
            shown += BonusPart("pet-ico-boss", "Boss", b.BossDamage, true, shown);
            noBonusText.enabled = shown == 0;
        }

        /// <summary>One "icon label +x%" entry; at most three fit the row (all of them are in the ⓘ details).</summary>
        int BonusPart(string icon, string label, double v, bool plus, int shown)
        {
            if (v <= 0 || shown >= 3) return 0;
            RectTransform part = UiKit.Rect(bonusRow, label, 0f, 0f, 10f, 33f);
            var h = part.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 3f; h.childAlignment = TextAnchor.MiddleLeft;
            h.childControlWidth = h.childControlHeight = true; h.childForceExpandWidth = h.childForceExpandHeight = false;
            Image ico = W.Img(part, "Icon", 0f, 0f, 18f, 18f, icon);
            var le = ico.gameObject.AddComponent<LayoutElement>(); le.preferredWidth = le.minWidth = 18f; le.preferredHeight = le.minHeight = 18f;
            Text t = W.Txt(part, "Text", 0f, 0f, 10f, 30f, label + " <color=#2E9E57>" + (plus ? "+" : "-") + PetViewModel.Pct(v) + "</color>", 14, W.Ink);
            t.gameObject.AddComponent<LayoutElement>().minHeight = 30f;
            return 1;
        }

        void ShowBonusDetails()
        {
            PlayerState p = PetActions.Me;
            if (p == null) return;
            List<string> parts = PetViewModel.BonusParts(PetTeam.Bonus(p.Team, p.Account));
            List<PetBond> bonds = PetTeam.ActiveBonds(p.Team);
            string s = parts.Count == 0 ? "No team bonus yet." : "Team bonus: " + string.Join(", ", parts) + ".";
            if (bonds.Count > 0) { s += " Bonds:"; foreach (PetBond b in bonds) s += " " + b.Name; }
            Flash(s);
        }

        // ---- taps ----

        /// <summary>Tap on a slot's pet: select it (empty slot: the swap rule).</summary>
        void OnSlotPet(int slot)
        {
            PlayerState p = PetActions.Me;
            if (p == null) return;
            PetDef d = PetConfig.Get(p.Team[slot]);
            if (d != null) Select(PetConfig.IndexOf(d)); else OnSlot(slot);
        }

        /// <summary>Swap button / stat chip: the selected pet goes into `slot`.</summary>
        void OnSlot(int slot)
        {
            PlayerState p = PetActions.Me;
            if (p == null) return;
            if (p.Level < PetRules.SlotUnlockLevel[slot]) { Flash(PetViewModel.SlotNames[slot] + " opens at level " + PetRules.SlotUnlockLevel[slot]); return; }
            PetDef d = Selected;
            if (d == null || p.Account == null || !p.Account.OwnsPet(d.Id)) { Flash("Select a pet you own in the list first"); return; }
            if (p.Team[slot] == d.Id) { Flash(d.Name + " is already in " + PetViewModel.SlotNames[slot]); return; }
            Place(p, slot, selected);
        }

        /// <summary>Species `pet` into `slot`. In another slot already: it moves, and a pet it replaces takes the slot it left.</summary>
        void Place(PlayerState p, int slot, int pet)
        {
            PetDef d = PetConfig.All[pet];
            int from = System.Array.IndexOf(p.Team, d.Id);
            PetDef replaced = PetConfig.Get(p.Team[slot]);
            var after = (string[])p.Team.Clone(); // the team once the pet left its old slot: what the server will check
            if (from >= 0) after[from] = "";
            string why = PetTeam.Refusal(after, slot, d.Id, p.Level, p.Account);
            if (why != null) { Flash(why); return; }
            if (from >= 0) Send(InvOp.PetEquip, PetRules.EncodeSlot(from, -1));
            Send(InvOp.PetEquip, PetRules.EncodeSlot(slot, pet));
            if (from >= 0 && replaced != null) Send(InvOp.PetEquip, PetRules.EncodeSlot(from, PetConfig.IndexOf(replaced)));
            Flash(d.Name + " joined the team: " + PetViewModel.SlotNames[slot]);
            Refresh();
        }
    }
}
