using System;
using UnityEngine;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>
    /// Test resources on start, read from Assets/Resources/Data/dev-start.json (edit the numbers, save, press Play).
    /// Player builds read dev-start.json placed next to Zenonia.exe instead (see ReadJson).
    /// Every time a class is loaded or created, its upgrade stones / gold / potions are topped UP to these values (never lowered),
    /// so enhancing can be tested right away. The bundled Resources copy only works in the editor and development builds.
    /// </summary>
    [Serializable]
    public class DevStartConfig
    {
        public bool enabled = true;
        public int upgradeStones, gold, hpPotions, mpPotions;

        /// <summary>
        /// A dev-start.json next to the player executable (testers drop it there; a shipped build has none) wins; otherwise the bundled
        /// Resources copy, but only in the editor and development builds.
        /// </summary>
        static string ReadJson()
        {
            try
            {
                string beside = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Application.dataPath) ?? ".", "dev-start.json");
                if (!Application.isEditor && System.IO.File.Exists(beside)) return System.IO.File.ReadAllText(beside);
            }
            catch (Exception e) { Debug.LogWarning("[ZenoniaProto] dev-start.json next to the game unreadable: " + e.Message); }
            if (!Application.isEditor && !Debug.isDebugBuild) return null;
            var asset = Resources.Load<TextAsset>("Data/dev-start");
            return asset != null ? asset.text : null;
        }

        public static void Apply(PlayerState p)
        {
            if (p == null) return;
            string json = ReadJson();
            if (json == null) return;
            DevStartConfig c;
            try { c = JsonUtility.FromJson<DevStartConfig>(json); }
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
