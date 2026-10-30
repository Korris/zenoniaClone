using System.Collections.Generic;
using UnityEngine;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// Skill icon lookup: Resources/Sprites/skills/&lt;class&gt;/&lt;slug&gt;/icon.png (slug = skill id without the class prefix; built by
    /// Tools/build-skill-tree-icons.py), falling back to the old per-class Sprites/skill-icons/&lt;class&gt;-N art, then null.
    /// </summary>
    public static class SkillIcons
    {
        static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

        public static Sprite Get(SkillDef d)
        {
            if (d == null) return null;
            Sprite s;
            if (Cache.TryGetValue(d.Id, out s) && s != null) return s;
            string slug = d.Id.Substring(d.Id.IndexOf('.') + 1);
            Texture2D tex = Resources.Load<Texture2D>("Sprites/skills/" + d.Class + "/" + slug + "/icon")
                ?? Resources.Load<Texture2D>("Sprites/skill-icons/" + d.Class + "-" + d.Index);
            if (tex == null) return null;
            s = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
            Cache[d.Id] = s;
            return s;
        }
    }
}
