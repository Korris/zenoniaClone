using System.Collections.Generic;
using UnityEngine;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// Ground warning of one locked Wanted Boss skill shape (docs/wanted-boss-skills-design.md). Shown from the moment the telegraph event
    /// arrives until the server's locked strike instant -- NetClient.ServerNowMs vs the event's AtMs, never a local timer, so the warning cannot
    /// vanish before the hit the way the design doc requires -- filling in over that time, then handing off to the boss's impact picture
    /// (WantedSkillVfx) and disappearing. The enrage circle also draws its safe lane(s) (reusing the cone piece, tinted teal) plus a small
    /// warning-safe badge. ProxySpawner.ClearForTransfer destroys every live one on a map change.
    /// </summary>
    public sealed class WantedSkillTelegraphFx : MonoBehaviour
    {
        static readonly List<WantedSkillTelegraphFx> Active = new List<WantedSkillTelegraphFx>(16);
        static readonly Color Danger = new Color(1f, 0.32f, 0.14f, 0.3f), Safe = new Color(0.35f, 1f, 0.85f, 0.75f),
            Fill = new Color(1f, 0.18f, 0.08f, 0.3f);
        const float LingerMs = 250f; // keeps the impact picture's start frame visible a beat after the strike instant

        string bossId;
        WantedSkillDef skill;
        WantedShapeInstance shape;
        uint atMs;
        SpriteRenderer area, fill;
        Vector3 fillFull;   // the fill's scale at the strike instant; it grows from the centre (or the line's base) up to this
        bool firedImpact;

        /// <summary>NetEventPlayer calls this for every WantedSkillTelegraph event (one per locked shape instance).</summary>
        public static void Spawn(string bossId, int skillId, Vector2 origin, float dirDeg, uint atMs)
        {
            if (!NetMode.RendersFx || Zone.Main == null) return;
            WantedSkillDef[] table = WantedSkillTable.For(bossId);
            if (table == null || skillId < 0 || skillId >= table.Length) return;
            var go = new GameObject("WantedTelegraph");
            go.transform.SetParent(Zone.Main.Root, false);
            go.transform.position = new Vector3(origin.x, origin.y, 0f);
            var fx = go.AddComponent<WantedSkillTelegraphFx>();
            fx.bossId = bossId;
            fx.skill = table[skillId];
            fx.shape = new WantedShapeInstance { OriginX = origin.x, OriginY = origin.y, DirDeg = dirDeg };
            fx.atMs = atMs;
            fx.Build();
            Active.Add(fx);
            Debug.Log("[WantedTelegraph] spawn " + bossId + " skill " + skillId + " atMs " + atMs + " serverNow " + NetClient.ServerNowMs.ToString("F0"));
        }

        /// <summary>True when `point` stands inside any live danger shape right now (HeroController.WantedOverlay's alert icon, local only).</summary>
        public static bool AnyDangerAt(Vector2 point)
        {
            for (int i = 0; i < Active.Count; i++)
            {
                WantedSkillTelegraphFx a = Active[i];
                if (a != null && !a.firedImpact && WantedSkillArea.Hits(a.skill, a.shape, point.x, point.y)) return true;
            }
            return false;
        }

        /// <summary>Destroys every live telegraph (a map change leaves them behind, like every other per-map FX -- sprite-rules.md #10).</summary>
        public static void ClearAll()
        {
            for (int i = Active.Count - 1; i >= 0; i--) if (Active[i] != null) Destroy(Active[i].gameObject);
            Active.Clear();
        }

        void Build()
        {
            float u = WantedSkillArea.TileUnits;
            switch (skill.Shape)
            {
                case WantedAreaShape.Cone:
                    fill = WantedWarningSprites.Fill(transform, skill.AngleDeg, shape.DirDeg, Fill);
                    fillFull = new Vector3(skill.RadiusTiles * u, skill.RadiusTiles * u, 1f);
                    area = WantedWarningSprites.Cone(transform, skill.RadiusTiles * u, skill.AngleDeg, shape.DirDeg, Danger);
                    break;
                case WantedAreaShape.Line:
                    fill = WantedWarningSprites.FillLine(transform, shape.DirDeg, Fill);
                    fillFull = new Vector3(skill.WidthTiles * u, skill.LengthTiles * u, 1f);
                    area = WantedWarningSprites.Line(transform, skill.LengthTiles * u, skill.WidthTiles * u, shape.DirDeg, Danger);
                    break;
                default:
                    fill = WantedWarningSprites.Fill(transform, 360f, 0f, Fill);
                    fillFull = new Vector3(skill.RadiusTiles * u, skill.RadiusTiles * u, 1f);
                    area = WantedWarningSprites.Circle(transform, skill.RadiusTiles * u, Danger);
                    if (skill.SafeArcCount > 0)
                    {
                        for (int i = 0; i < skill.SafeArcCount; i++)
                            WantedWarningSprites.Cone(transform, skill.RadiusTiles * u, skill.SafeArcDeg, shape.DirDeg + i * (360f / skill.SafeArcCount), Safe);
                        WantedWarningSprites.Badge(transform, shape.DirDeg, skill.RadiusTiles * u * 0.55f, Safe);
                    }
                    break;
            }
        }

        void Update()
        {
            float remainMs = atMs - NetClient.ServerNowMs;
            float progress = Mathf.Clamp01(1f - remainMs / Mathf.Max(1, skill.TelegraphMs));
            if (area != null) { Color c = area.color; area.color = new Color(c.r, c.g, c.b, Mathf.Lerp(0.55f, 1f, progress)); }
            if (fill != null) // the solid part grows from the centre to the rim as the strike nears; the outline already shows the full extent
            {
                float k = Mathf.Lerp(0.1f, 1f, progress);
                fill.transform.localScale = skill.Shape == WantedAreaShape.Line ? new Vector3(fillFull.x, fillFull.y * k, 1f) : fillFull * k;
                fill.color = new Color(Fill.r, Fill.g, Fill.b, Mathf.Lerp(0.25f, 0.5f, progress));
            }
            if (remainMs > 0f) return;
            if (!firedImpact)
            {
                firedImpact = true;
                Debug.Log("[WantedTelegraph] impact skill " + skill.Id + " atMs " + atMs + " serverNow " + NetClient.ServerNowMs.ToString("F0"));
                WantedSkillVfx.Play(bossId, skill, new Vector2(shape.OriginX, shape.OriginY), shape.DirDeg);
                // Self-predicts a Freeze hit right here, at the same server-clock instant the impact picture fires -- the local hero's own
                // OwnerSim.FreezeLeft (one round trip later) only confirms / corrects this guess (HeroController.PredictFreeze).
                HeroController me = LocalPlayer.Hero;
                if (skill.StatusKind == StatusKind.Freeze && me != null && me.IsPredicted
                    && WantedSkillArea.Hits(skill, shape, me.SimPosition.x, me.SimPosition.y))
                    me.PredictFreeze(skill.StatusMs);
            }
            if (remainMs < -LingerMs) { Active.Remove(this); Destroy(gameObject); }
        }

        void OnDestroy() { Active.Remove(this); }
    }
}
