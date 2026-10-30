using System;
using UnityEngine;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// Test resources on start, read from Assets/Resources/Data/dev-start.json (edit the numbers, save, press Play).
    /// Every time a class is loaded or created, its upgrade stones / gold / potions are topped UP to these values (never lowered),
    /// so enhancing can be tested right away. Editor and development builds only; a release build ignores the file.
    /// </summary>
    [Serializable]
    public class DevStartConfig
    {
        public bool enabled = true;
        public int upgradeStones, gold, hpPotions, mpPotions;

        public static void Apply(PlayerState p)
        {
            if (p == null || (!Application.isEditor && !Debug.isDebugBuild)) return;
            var asset = Resources.Load<TextAsset>("Data/dev-start");
            if (asset == null) return;
            DevStartConfig c;
            try { c = JsonUtility.FromJson<DevStartConfig>(asset.text); }
            catch (Exception e) { Debug.LogWarning("[ZenoniaProto] dev-start.json ignored: " + e.Message); return; }
            if (c == null || !c.enabled) return;
            p.UpgradeStone = Mathf.Clamp(Mathf.Max(p.UpgradeStone, c.upgradeStones), 0, PlayerState.MaxStones);
            p.Gold = Mathf.Max(p.Gold, c.gold);
            p.HpPotions = Mathf.Max(p.HpPotions, c.hpPotions);
            p.MpPotions = Mathf.Max(p.MpPotions, c.mpPotions);
            Debug.Log("[ZenoniaProto] dev-start: stones " + p.UpgradeStone + ", gold " + p.Gold + ", potions " + p.HpPotions + "/" + p.MpPotions);
        }
    }
}
