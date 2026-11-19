using UnityEngine;
using ZenoniaProto.Api;

namespace ZenoniaProto
{
    /// <summary>What the character slot screen shows: the six cards, the big hero with its info panel, and every frame the buttons, corner buttons and status line.</summary>
    public sealed partial class CharacterListScreen
    {
        /// <summary>The English class name (Warrior, Wizard, Mechanic, Assassin, Cleric, Fighter), the raw id for a class this build does not know.</summary>
        static string ClassName(CharacterSummary c) { HeroClass id; return KnownClass(c, out id) ? HeroClassDef.Get(id).Name : c.classId; }

        /// <summary>Which hero or empty card is in each slot, the big hero and the info panel.</summary>
        void Redraw()
        {
            dirty = false;
            builtVersion = ListVersion;
            PlaceBySlot();
            if (Selected() == null) { CharacterSummary first = bySlot.Find(c => c != null); selectedId = first != null ? first.id : null; } // deleted or unknown: the first one
            int pages = (bySlot.Count + Rows - 1) / Rows;
            if (page < 0) page = Mathf.Max(0, bySlot.FindIndex(c => c != null && c.id == selectedId)) / Rows;
            page = Mathf.Clamp(page, 0, pages - 1);
            pageUp.gameObject.SetActive(pages > 1);
            pageDown.gameObject.SetActive(pages > 1);
            preview.Clear();
            for (int r = 0; r < Rows; r++) ShowCard(r, page * Rows + r);
            ShowBig(Selected());
        }

        void ShowCard(int r, int index)
        {
            Row card = rows[r];
            CharacterSummary hero = index < bySlot.Count ? bySlot[index] : null;
            bool empty = hero == null && index < AccountFlow.MaxCharacters;
            card.Root.SetActive(hero != null || empty);
            card.Hero.SetActive(hero != null);
            card.Empty.SetActive(empty);
            if (hero == null) { card.Face.sprite = SlotArt("slot-card-empty"); return; }
            bool picked = hero.id == selectedId;
            card.Face.sprite = SlotArt(picked ? "slot-card-selected" : "slot-card");
            card.Name.text = hero.name;
            string badge = Playing(hero) ? "Playing" : hero.online ? "In game" : "";
            card.Info.text = badge.Length > 0 ? ClassName(hero) + " · " + badge : ClassName(hero);
            card.Info.color = badge.Length > 0 ? RoSoftTheme.MintDark : RoSoftTheme.Ink;
            card.Level.text = "Lv " + hero.level;
            HeroClass id;
            card.Mini.enabled = KnownClass(hero, out id);
            if (!card.Mini.enabled) return;
            preview.Set(r, id, Rpg.CharacterAppearanceData.Sanitize(hero.appearance, hero.classId), Rpg.CharacterAppearanceData.SeedFor(hero.name, 0), 1f);
            preview.SetPose(r, 1, HeroPreviewStage.Motion.Idle); // the 3/4 front view, like the mockup cards
        }

        void ShowBig(CharacterSummary hero)
        {
            HeroClass id;
            bool known = KnownClass(hero, out id);
            bigHero.enabled = known;
            pedestal.gameObject.SetActive(hero != null);
            foreach (GameObject g in heroControls) g.SetActive(known);
            infoHero.SetActive(hero != null);
            infoEmpty.SetActive(hero == null);
            if (hero == null) return;
            infoName.text = hero.name;
            infoClass.text = ClassName(hero);
            infoLevel.text = hero.level.ToString();
            IGameMap map = !string.IsNullOrEmpty(hero.mapId) ? MapRegistry.Get(hero.mapId) : null;
            infoPlace.text = map != null ? map.Name : "Oakhaven Village";
            if (!known) return;
            preview.Set(BigSlot, id, Rpg.CharacterAppearanceData.Sanitize(hero.appearance, hero.classId), Rpg.CharacterAppearanceData.SeedFor(hero.name, 0), 1f);
            PoseBig();
        }

        void PoseBig() { preview.SetPose(BigSlot, dir8, HeroPreviewStage.Motion.Idle); }

        /// <summary>Every frame: buttons that wait for a running call or the join, the corner buttons of the mode and the status line.</summary>
        void RefreshState()
        {
            NetClient client = NetClient.Active;
            bool online = mode == Mode.Online;
            bool connecting = online && client != null && client.State == ClientState.Connecting;
            bool busy = online && (AccountFlow.Busy || connecting);
            CharacterSummary hero = Selected();
            bool playing = hero != null && Playing(hero);
            enterButton.interactable = hero != null && !busy && !playing;
            enterLabel.text = playing ? "Playing" : "Enter";
            createButton.interactable = !busy && Heroes.Count < AccountFlow.MaxCharacters;
            deleteButton.interactable = hero != null && !busy && !playing;
            foreach (Row r in rows) r.Button.interactable = !busy;
            string text = online ? (connecting ? client.Status : AccountFlow.Status) : offlineStatus;
            status.text = text;
            statusPlate.SetActive(!string.IsNullOrEmpty(text));
            status.color = online && AccountFlow.StatusBad && !connecting ? RoSoftTheme.BadText : RoSoftTheme.Ink;
            emptyNote.text = online && AccountFlow.Busy && Heroes.Count == 0 ? "Loading ..." : "No characters yet.\nPick an empty slot below to create one.";
            bool overGame = !online && GameSession.Hero != null;
            ShowCorner(logoutButton, online);
            ShowCorner(offlineButton, online);
            ShowCorner(directButton, online && ConnectScreenGui.DevPageAvailable);
            ShowCorner(onlineButton, !online && !overGame);
            ShowCorner(backButton, overGame);
            LayoutCorner();
            RefreshConfirm(busy);
        }
    }
}
