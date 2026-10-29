using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>Places the prototype's monster pack in a ring around the hero start (ice goblins + frost slimes).</summary>
    public static class MonsterSpawner
    {
        const int Count = 8;

        public static void SpawnAll(Transform parent)
        {
            Vector2 centre = GameConfig.HeroStart;
            for (int i = 0; i < Count; i++)
            {
                float angle = (i / (float)Count) * Mathf.PI * 2f + 0.4f;
                float radius = 95f + (i % 3) * 28f;
                var pos = new Vector2(
                    Mathf.Clamp(centre.x + Mathf.Cos(angle) * radius * 1.4f, 30f, GameConfig.MapW - 30f),
                    Mathf.Clamp(centre.y + Mathf.Sin(angle) * radius, 30f, GameConfig.MapH - 30f));
                MonsterDef def = i % 3 == 2 ? MonsterDef.FrostSlime : MonsterDef.IceGoblin;
                Monster.Create(parent, def, pos);
            }
        }
    }
}
