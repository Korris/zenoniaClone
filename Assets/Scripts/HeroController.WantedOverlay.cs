using UnityEngine;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// Local-only overhead icons for a Wanted Boss fight (design doc "Cảnh báo"): a "!" while standing in an active telegraphed danger zone, and
    /// a small slow / poison icon while that status is active. Never shown for other players -- with many players fighting the same boss this
    /// would turn into noise; only the player standing in it needs to see its own warning.
    /// </summary>
    public partial class HeroController
    {
        SpriteRenderer wantedAlertIcon, wantedStatusIcon;
        string wantedStatusIconKind; // which texture wantedStatusIcon currently holds, rebuilt only when the status kind changes

        void UpdateWantedOverlay()
        {
            if (view == null || this != LocalPlayer.Hero || !NetMode.RendersFx) return;
            bool danger = !dead && WantedSkillTelegraphFx.AnyDangerAt(Position);
            ShowOverheadIcon(ref wantedAlertIcon, "WantedAlert", "Sprites/fx/wanted/warning-alert", danger, 7f);
            string kind = dead ? null : wantedStatus.Has(StatusKind.Poison) ? "poison" : wantedStatus.Has(StatusKind.Slow) ? "slow" : null;
            if (kind != wantedStatusIconKind && wantedStatusIcon != null) { Destroy(wantedStatusIcon.gameObject); wantedStatusIcon = null; }
            wantedStatusIconKind = kind;
            ShowOverheadIcon(ref wantedStatusIcon, "WantedStatus", kind != null ? "Sprites/fx/wanted/warning-" + kind : null, kind != null, -7f);
        }

        void ShowOverheadIcon(ref SpriteRenderer sr, string name, string path, bool show, float xOffset)
        {
            if (!show) { if (sr != null) sr.enabled = false; return; }
            if (sr == null)
            {
                Texture2D tex = SpriteSheet.TryLoad(path);
                if (tex == null) return;
                sr = ProceduralSprites.Child(transform, name, SpriteSheet.Whole(path, tex.width, new Vector2(0.5f, 0.5f)));
                sr.transform.localScale = new Vector3(4f, 4f * tex.height / tex.width, 1f);
            }
            sr.enabled = true;
            sr.transform.localPosition = new Vector3(xOffset, 22f, 0f);
            sr.sortingOrder = GameConfig.OrderBase(y) + 20;
        }
    }
}
