using UnityEngine;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// Start-up check that every card id is a real monster (CardTable lives in the engine-free Rpg assembly and cannot see MonsterDef): the id must be a MonsterDef type,
    /// a boss card must be a boss, a normal card must not be. A mismatch is logged as an error (the card could never drop).
    /// </summary>
    public static class CardIdsCheck
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Run() { foreach (string problem in Problems()) Debug.LogError("[ZenoniaProto] card table: " + problem); }

        public static System.Collections.Generic.List<string> Problems()
        {
            var list = new System.Collections.Generic.List<string>();
            foreach (CardDef c in CardTable.All)
            {
                MonsterDef m = MonsterDef.ByType(c.MonsterId);
                if (m == null) list.Add(c.Id + ": no MonsterDef");
                else if (m.IsBoss != c.IsBoss) list.Add(c.Id + ": boss flag differs from MonsterDef (" + m.IsBoss + ")");
            }
            return list;
        }
    }
}
