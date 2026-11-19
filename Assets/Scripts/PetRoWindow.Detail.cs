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
    /// PetRoWindow, right card: picture on its pedestal, rarity + name (rename: not in the game yet, shown dimmed), Lv / 60 bar, stars, element and
    /// role tags, lock (not in the game yet), the Info / Skills / Bond tabs (.DetailTabs) and Level up (Pet Food) / Star up (shards + gold) / Set to team.
    /// A species the account does not own shows at 1 star with "Not owned" and how to get it; its buttons are dimmed.
    /// </summary>
    public sealed partial class PetRoWindow
    {
        Image detailPet;
        B.PillBox detailRarity;
        Text detailName, detailLv, barMax, elementText, roleText, levelCost, starCost, teamCost;
        Image elementIcon, roleIcon, barFill;
        RectTransform barTrack;
        PW.Stars detailStars;
        B.IconButton levelBtn, starBtn, teamBtn;
        GameObject renameBtn, lockBtn;

        void BuildDetail(RectTransform box)
        {
            Image halo = W.Img(box, "Halo", -6f, -4f, 192f, 180f, "pet-glow");
            halo.color = RoSoftTheme.Hex("#BFDDFF"); // the mockup's pale blue ring behind the selected pet
            W.Img(box, "Pedestal", 6f, 108f, 168f, 64f, "pet-pedestal");
            detailPet = UiKit.Rect(box, "Pet", 0f, 0f, 170f, 130f).gameObject.AddComponent<Image>();
            detailPet.preserveAspect = true; detailPet.raycastTarget = false;
            detailPet.rectTransform.pivot = new Vector2(0.5f, 0f);
            detailPet.rectTransform.anchoredPosition = new Vector2(90f, -134f);

            RectTransform nameRow = UiKit.Rect(box, "NameRow", 172f, 14f, 184f, 30f);
            var row = nameRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.spacing = 10f; row.childAlignment = TextAnchor.MiddleLeft;
            row.childControlWidth = row.childControlHeight = true; row.childForceExpandWidth = row.childForceExpandHeight = false;
            detailRarity = PW.Rarity(nameRow, "Rarity", 0f, 0f, 28f, 18, null, 40f);
            detailName = W.Txt(nameRow, "Name", 0f, 0f, 10f, 30f, "", 21, W.Ink);
            detailName.gameObject.AddComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            Image rename = W.Img(box, "Rename", 362f, 14f, 30f, 30f, "pet-btn-rename", true);
            rename.color = new Color(1f, 1f, 1f, 0.55f);
            W.Clickable(rename, () => Flash("Pet names cannot be changed yet."));
            renameBtn = rename.gameObject;

            detailLv = W.Txt(box, "Lv", 176f, 48f, 200f, 24f, "", 17, W.Ink);
            barTrack = W.Img(box, "BarTrack", 176f, 73f, 210f, 19f, "pet-bar-track").rectTransform;
            barTrack.GetComponent<Image>().pixelsPerUnitMultiplier = 40f / 19f;
            barFill = W.Img(barTrack, "Fill", 0f, 0f, 210f, 19f, "pet-bar-green");
            barFill.pixelsPerUnitMultiplier = 40f / 19f;
            barMax = W.Txt(barTrack, "Max", 0f, -1f, 210f, 19f, "", 13, Color.white, TextAnchor.MiddleCenter);
            W.Rim(barMax);
            detailStars = new PW.Stars(box, "Stars", 176f, 96f, 23f, 2f, false);

            Image el = W.Img(box, "Element", 176f, 126f, 88f, 32f, "pet-pill-cream");
            el.pixelsPerUnitMultiplier = 104f / 60f;
            elementIcon = W.Img(el.transform, "Icon", 4f, 3f, 26f, 26f, "pet-el-ice");
            elementText = W.Txt(el.transform, "Text", 34f, 0f, 52f, 31f, "", 15, W.Ink);
            Image role = W.Img(box, "Role", 268f, 126f, 88f, 32f, "pet-pill-cream");
            role.pixelsPerUnitMultiplier = 104f / 60f;
            roleIcon = W.Img(role.transform, "Icon", 4f, 3f, 26f, 26f, "pet-el-strike");
            roleText = W.Txt(role.transform, "Text", 34f, 0f, 52f, 31f, "", 15, W.Ink);
            Image lockImg = W.Img(box, "Lock", 360f, 125f, 32f, 32f, "pet-btn-lock", true);
            lockImg.color = new Color(1f, 1f, 1f, 0.55f);
            W.Clickable(lockImg, () => Flash("Pets cannot be locked yet: nothing in the game can release or trade a pet."));
            lockBtn = lockImg.gameObject;

            BuildDetailTabs(box);

            float by = box.sizeDelta.y - 78f;
            levelBtn = new B.IconButton(box, "LevelUp", 8f, by, 124f, 54f, "btn-mint", "pet-nav-growth", "Level up", 18, LevelUp);
            starBtn = new B.IconButton(box, "StarUp", 138f, by, 118f, 54f, "btn-gold", "pet-star", "Star up", 18, StarUp);
            teamBtn = new B.IconButton(box, "SetTeam", 262f, by, 131f, 54f, "btn-sky", "pet-nav-team", "Set to team", 18, SetToTeam);
            levelCost = W.Txt(box, "LevelCost", 8f, by + 55f, 124f, 18f, "", 13, W.InkSoft, TextAnchor.MiddleCenter);
            starCost = W.Txt(box, "StarCost", 138f, by + 55f, 118f, 18f, "", 13, W.InkSoft, TextAnchor.MiddleCenter);
            teamCost = W.Txt(box, "TeamCost", 262f, by + 55f, 131f, 18f, "", 13, W.InkSoft, TextAnchor.MiddleCenter);
        }

        void RefreshDetail(PlayerState p, AccountState a)
        {
            PetDef d = Selected;
            if (d == null) d = PetConfig.All[PetConfig.All.Length - 1]; // nothing owned yet: show the top species as a goal
            OwnedPet o = a != null ? a.Pet(d.Id) : null;
            int level = o != null ? o.level : 1, stars = o != null ? o.stars : 1;
            detailPet.sprite = PW.PetArt(d);
            detailPet.color = o != null ? ZenoniaProto.PetArt.TintOf(d) : new Color(0.55f, 0.6f, 0.68f, 0.8f);
            PW.SetRarity(detailRarity, d);
            detailName.text = d.Name;
            detailName.fontSize = 21;
            while (detailName.fontSize > 14 && detailName.preferredWidth > 184f - 8f - detailRarity.Root.sizeDelta.x) detailName.fontSize--;
            renameBtn.SetActive(o != null);
            lockBtn.SetActive(o != null);
            detailLv.text = o != null ? "Lv. <b>" + level + "</b> <color=#5F7488>/ " + PetRules.MaxLevel + "</color>" : "<color=#5F7488>Not owned</color>";
            float share = (float)level / PetRules.MaxLevel;
            barFill.rectTransform.sizeDelta = new Vector2(Mathf.Max(19f, 210f * share), 19f);
            barFill.enabled = o != null;
            barMax.text = o != null && level >= PetRules.MaxLevel ? "MAX" : o != null ? Mathf.RoundToInt(share * 100f) + "%" : "";
            detailStars.Set(o != null ? stars : 0);
            elementText.text = ElementName(d.Skill);
            FitText(elementText, 52f, 15);
            W.SetArt(elementIcon, ElementPiece(d.Skill));
            bool guard = d.Skill == PetSkillKind.Shield || d.Skill == PetSkillKind.Heal;
            roleText.text = guard ? "Guard" : d.Melee ? "Melee" : "Ranged";
            FitText(roleText, 52f, 15);
            W.SetArt(roleIcon, guard ? "pet-el-shield" : "pet-el-strike");
            RefreshDetailTabs(p, a, d, o);

            bool owned = o != null;
            int shards = 0, gold = 0;
            bool starOpen = owned && PetGrowth.NextStarCost(a, d.Id, out shards, out gold);
            if (!starOpen) { shards = gold = 0; }
            levelBtn.Set(true, null, !owned || level >= PetRules.MaxLevel);
            starBtn.Set(true, null, !starOpen);
            bool inTeam = PetTeam.Has(p.Team, d.Id);
            teamBtn.Set(true, inTeam ? "Remove" : "Set to team", !owned);
            levelCost.text = !owned ? "" : level >= PetRules.MaxLevel ? "Top level" : PetRules.FoodForLevel(level) + " Pet Food (" + a.food + ")";
            starCost.text = !owned ? "" : !starOpen ? "Most stars" : (o.shards + "/" + shards + " shards, " + N(gold) + " G");
            teamCost.text = !owned ? "Not owned" : inTeam ? PetViewModel.SlotNames[System.Array.IndexOf(p.Team, d.Id)] : "";
        }

        /// <summary>Steps the font down until the text fits `room` ("Physical" in a tag).</summary>
        static void FitText(Text t, float room, int size)
        {
            t.fontSize = size;
            while (t.fontSize > 10 && t.preferredWidth > room) t.fontSize--;
        }

        static string ElementName(PetSkillKind k)
        {
            switch (k) { case PetSkillKind.Ice: return "Ice"; case PetSkillKind.Fire: return "Fire"; case PetSkillKind.Poison: return "Poison"; case PetSkillKind.Lightning: return "Bolt"; case PetSkillKind.Heal: return "Heal"; case PetSkillKind.Shield: return "Shield"; default: return "Physical"; }
        }

        static string ElementPiece(PetSkillKind k)
        {
            switch (k) { case PetSkillKind.Ice: return "pet-el-ice"; case PetSkillKind.Fire: return "pet-el-fire"; case PetSkillKind.Poison: return "pet-el-poison"; case PetSkillKind.Lightning: return "pet-el-lightning"; case PetSkillKind.Heal: return "pet-el-heal"; case PetSkillKind.Shield: return "pet-el-shield"; default: return "pet-el-strike"; }
        }

        // ---- buttons ----

        bool OwnedSelected(out PetDef d)
        {
            d = Selected;
            AccountState a = PetActions.Account;
            if (d != null && a != null && a.OwnsPet(d.Id)) return true;
            Flash(d == null ? "Select a pet first" : "You do not own " + d.Name + " yet");
            return false;
        }

        void LevelUp()
        {
            PetDef d;
            if (!OwnedSelected(out d)) return;
            Send(InvOp.PetLevelUp, (byte)selected);
            Refresh();
        }

        void StarUp()
        {
            PetDef d;
            if (!OwnedSelected(out d)) return;
            Send(InvOp.PetStarUp, (byte)selected);
            Refresh();
        }

        /// <summary>Not in the team: the first open empty slot (Combat, Support, then Stat). In the team: leaves its slot.</summary>
        void SetToTeam()
        {
            PetDef d;
            PlayerState p = PetActions.Me;
            if (p == null || !OwnedSelected(out d)) return;
            int at = System.Array.IndexOf(p.Team, d.Id);
            if (at >= 0) { Send(InvOp.PetEquip, PetRules.EncodeSlot(at, -1)); Flash(d.Name + " left the team"); Refresh(); return; }
            for (int slot = 0; slot < PetRules.TeamSize; slot++)
            {
                if (p.Team[slot] != "" || p.Level < PetRules.SlotUnlockLevel[slot]) continue;
                Place(p, slot, selected);
                return;
            }
            int next = -1;
            for (int slot = 0; slot < PetRules.TeamSize; slot++) if (p.Level < PetRules.SlotUnlockLevel[slot]) { next = slot; break; }
            Flash(next >= 0 ? "Team full: use a slot's swap button (" + PetViewModel.SlotNames[next] + " opens at Lv. " + PetRules.SlotUnlockLevel[next] + ")" : "Team full: use a slot's swap button to replace a pet");
        }
    }
}
