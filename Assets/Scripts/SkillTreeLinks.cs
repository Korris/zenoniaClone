using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>Prerequisite lines between nodes: thin stretched images, node centre to node centre, grey until the prerequisite rank is reached, then gold.</summary>
    public sealed class SkillTreeLinks
    {
        struct Link { public Image Img; public SkillReq Need; }
        static readonly Color Unmet = new Color(0.45f, 0.5f, 0.6f, 0.75f), Met = new Color(1f, 0.82f, 0.36f, 0.95f);
        readonly List<Link> links = new List<Link>();
        RectTransform holder;

        /// <summary>(Re)builds the lines of one class; `centers` = node centres in the parent's top-left based units. Sits under its siblings.</summary>
        public void Build(Transform parent, string classId, Vector2[] centers)
        {
            if (holder != null) Object.Destroy(holder.gameObject);
            links.Clear();
            holder = UiKit.Rect(parent, "Links", 0f, 0f, 1f, 1f);
            holder.SetAsFirstSibling();
            foreach (SkillDef d in SkillTable.ForClass(classId))
                foreach (SkillReq r in d.Prereqs)
                {
                    Vector2 a = centers[r.Index], b = centers[d.Index], v = b - a;
                    RectTransform rt = UiWinKit.Anchored(holder, "Link" + r.Index + "-" + d.Index, new Vector2(0f, 1f), new Vector2(0f, 0.5f), a.x, -a.y, v.magnitude, 5f);
                    rt.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(-v.y, v.x) * Mathf.Rad2Deg);
                    Image img = rt.gameObject.AddComponent<Image>();
                    img.raycastTarget = false;
                    links.Add(new Link { Img = img, Need = r });
                }
        }

        public void Refresh(SkillRanks ranks)
        {
            foreach (Link l in links) l.Img.color = ranks[l.Need.Index] >= l.Need.Rank ? Met : Unmet;
        }
    }
}
