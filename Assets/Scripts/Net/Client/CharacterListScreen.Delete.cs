using UnityEngine;
using UnityEngine.UI;
using ZenoniaProto.Api;

namespace ZenoniaProto
{
    /// <summary>
    /// Deleting a hero from the character select screen takes two steps: DELETE opens this card (who, what is lost), then the red button must be HELD for two
    /// seconds; KEEP or a short tap does nothing. Online the server deletes (AccountFlow.DeleteHeld, refusals show in the status line), offline the class's save goes.
    /// </summary>
    public sealed partial class CharacterListScreen
    {
        public const float DeleteHoldSeconds = 2f;
        const float ConfirmW = 500f, ConfirmH = 300f;

        RectTransform confirmRoot;
        HoldToConfirmButton holdButton;
        Text confirmTitle, confirmBody, holdLabel;
        string deletingId;

        void BuildConfirm()
        {
            confirmRoot = UiKit.Fill(root, "DeleteConfirm");
            Image veilImage = CharacterFlowKit.RoPiece(confirmRoot, "Veil", RoSoftSprites.Shape.Glass, 0f, 0f, 10f, 10f);
            veilImage.color = new Color(1f, 1f, 1f, 0.6f); // the list stays faintly visible behind the card
            RectTransform veil = veilImage.rectTransform;
            veil.anchorMin = Vector2.zero; veil.anchorMax = Vector2.one; veil.offsetMin = veil.offsetMax = Vector2.zero; // the kit glass over everything: taps stop here
            RectTransform card = CharacterFlowKit.RoPiece(confirmRoot, "DeleteCard", RoSoftSprites.Shape.Card, 0f, 0f, ConfirmW, ConfirmH).rectTransform;
            card.anchorMin = card.anchorMax = card.pivot = new Vector2(0.5f, 0.5f);
            card.anchoredPosition = Vector2.zero;
            confirmTitle = CharacterFlowKit.RoText(card, "Title", 20f, 20f, ConfirmW - 40f, 36f, "", 26, RoSoftTheme.CoralDark);
            confirmBody = CharacterFlowKit.RoText(card, "Body", 30f, 62f, ConfirmW - 60f, 110f, "", 18, RoSoftTheme.Ink);

            Image face = CharacterFlowKit.RoPiece(card, "HoldDelete", RoSoftSprites.Shape.Danger, 36f, 196f, 250f, 70f);
            holdLabel = UiKit.Label(UiKit.Fill(face.rectTransform, "Label"), "", 20, Color.white, TextAnchor.MiddleCenter, true);
            if (RoSoftTheme.Bold != null) holdLabel.font = RoSoftTheme.Bold;
            holdButton = face.gameObject.AddComponent<HoldToConfirmButton>();
            holdButton.Seconds = DeleteHoldSeconds;
            holdButton.Face = face;
            holdButton.OnProgress = p => holdLabel.text = p <= 0f ? "HOLD TO DELETE" : "KEEP HOLDING  " + Mathf.Max(0f, DeleteHoldSeconds * (1f - p)).ToString("0.0") + " s";
            holdButton.OnConfirmed = OnConfirmDelete;
            holdLabel.text = "HOLD TO DELETE";
            Text unused;
            CharacterFlowKit.RoButton(card, "KeepHero", RoSoftSprites.Shape.Secondary, 306f, 196f, 158f, 70f, "KEEP", 22, CloseConfirm, out unused);
            confirmRoot.gameObject.SetActive(false);
        }

        void OnAskDelete()
        {
            CharacterSummary hero = Selected();
            if (hero == null || Playing(hero.classId)) return;
            deletingId = hero.id;
            confirmTitle.text = "DELETE " + hero.name.ToUpperInvariant() + "?";
            confirmBody.text = "Level " + hero.level + " " + ClassName(hero) + (mode == Mode.Online ? "" : " (saved on this device)")
                + ".\nThe hero, its items and its gold are gone for good.\nHold the red button for " + DeleteHoldSeconds.ToString("0") + " seconds to delete.";
            confirmRoot.SetAsLastSibling();
            confirmRoot.gameObject.SetActive(true);
        }

        void CloseConfirm() { confirmRoot.gameObject.SetActive(false); deletingId = null; }

        void RefreshConfirm(bool busy)
        {
            if (!confirmRoot.gameObject.activeSelf) return;
            CharacterSummary hero = Selected();
            if (hero == null || hero.id != deletingId) { CloseConfirm(); return; } // the list changed underneath
            holdButton.Interactable = !busy;
        }

        void OnConfirmDelete()
        {
            CharacterSummary hero = Selected();
            if (hero == null || hero.id != deletingId) { CloseConfirm(); return; }
            CloseConfirm();
            if (mode == Mode.Online) { AccountFlow.DeleteHeld(hero); return; }
            OfflineCharacterRoster.Delete(hero.classId);
            offlineStatus = hero.name + " was deleted.";
            selectedId = null;
            ReloadOffline();
            if (offlineHeroes.Exists(c => c.id == hero.id)) offlineHeroes.RemoveAll(c => c.id == hero.id); // saves off (a capture run): gone from this list at least
        }
    }
}
