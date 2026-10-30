using System.Collections.Generic;
using UnityEngine;

namespace ZenoniaProto
{
    /// <summary>
    /// Online client: the local hero's own shots are launched the moment the button is pressed (prediction). Such a bolt has no server id yet; the
    /// server's BoltFired event for the same shot later gives it one (so BoltEnd finds it) instead of spawning a duplicate. It only draws: when it
    /// touches a monster proxy it plays the impact picture locally and stops; the damage and the numbers still come from the server.
    /// </summary>
    public partial class ComboProjectile
    {
        HeroController predictedOwner;
        int predictedSerial;
        static int serialCounter;

        /// <summary>Tags a bolt just returned by Fire as a local prediction of `owner`'s shot.</summary>
        public void MarkPredicted(HeroController owner)
        {
            predictedOwner = owner;
            predictedSerial = ++serialCounter;
        }

        /// <summary>Gives the oldest still unnamed predicted bolt of `owner` the server id. False when there is none (the shot was not predicted).</summary>
        public static bool AdoptPredicted(HeroController owner, ushort serverId)
        {
            ComboProjectile best = null;
            for (int i = 0; i < Pool.Count; i++)
            {
                ComboProjectile p = Pool[i];
                if (!p.gameObject.activeSelf || p.predictedOwner != owner || p.netId != 0) continue;
                if (best == null || p.predictedSerial < best.predictedSerial) best = p;
            }
            if (best == null) return false;
            best.netId = serverId;
            return true;
        }

        /// <summary>Cosmetic hit test against the monster proxies; plays the impact picture where the bolt stopped.</summary>
        bool PredictedImpact()
        {
            var box = new Rect(pos.x - def.W * 0.5f, pos.y - def.H * 0.5f, def.W, def.H);
            IReadOnlyList<Monster> monsters = Monster.All;
            for (int i = 0; i < monsters.Count; i++)
            {
                Monster m = monsters[i];
                if (m.IsDying || !box.Overlaps(m.Box)) continue;
                if (def.ExplodeRadius > 0f) ExplodeVisuals(); else HitFx(false);
                return true;
            }
            return false;
        }
    }
}
