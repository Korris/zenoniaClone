using System.Collections.Generic;
using UnityEngine;
using ZenoniaProto.Net;

namespace ZenoniaProto
{
    /// <summary>
    /// The client side of a portal. The server has already moved the hero when ZoneChanged arrives, so from that moment on nothing of the old zone may count: older snapshots fail the
    /// epoch check, the baselines and the interpolation tracks are cleared, and the input stream stops (the frames still in flight carry the old epoch, the server drops them).
    /// The old map stays frozen on screen during the fade; what the new zone sends meanwhile waits (Spawn messages here, snapshots in the buffer, events in the player).
    /// At full black the world is swapped, the same hero gets its new id on the arrival tile and the world comes back. A client without views does all of it at once.
    /// </summary>
    public sealed partial class NetClient
    {
        byte epoch;
        bool transferring, portalsLive;
        ZoneChangedMsg pendingZone;
        readonly List<SpawnMsg> heldSpawns = new List<SpawnMsg>(32);

        /// <summary>The server moves heroes between maps, so the portals on the map are real (false offline, on a dev host and with --no-portals).</summary>
        public static bool PortalsLive { get { return Active != null && Active.portalsLive; } }

        /// <summary>From Welcome: the map the server placed this hero on, plus whether its portals work.</summary>
        void ApplyWelcomeMap(WelcomeMsg m)
        {
            bool wasLive = portalsLive;
            portalsLive = m.PortalsLive;
            epoch = m.Epoch;
            transferring = false;
            heldSpawns.Clear();
            sender.BeginEpoch(epoch);
            sender.Resume();
            if (MapRegistry.Get(m.MapId) == null) { Debug.LogError("[ZenoniaProto] net: the server placed this hero on unknown map '" + m.MapId + "': this build is older than the server"); return; }
            if (MapRegistry.Current.Id != m.MapId) MapTransfer.Instant(m.MapId);
            else if (wasLive != portalsLive) WorldBuilder.Rebuild(); // the map is right, but its portal lights were built for another answer
        }

        void OnZoneChanged(ZoneChangedMsg m)
        {
            epoch = m.Epoch;
            myHeroId = m.HeroNetId;
            pendingZone = m;
            transferring = true;
            heldSpawns.Clear();
            ring.Clear(); ackSeq = 0; ownerPending = false;
            buffer.ClearEntities();
            if (events != null) events.Clear();
            sender.BeginEpoch(epoch);
            stats.Add("zoneChanges", 1);
            Debug.Log("[ZenoniaProto] net: moving to " + m.MapId + ", hero id " + m.HeroNetId + " (epoch " + m.Epoch + ")");
            if (!MapTransfer.GoOnline(FinishZoneChange)) FinishZoneChange();
        }

        /// <summary>At full black (or right away without views).</summary>
        void FinishZoneChange()
        {
            if (!transferring || spawner == null) return; // the connection went away during the fade
            transferring = false;
            ZoneChangedMsg m = pendingZone;
            if (NetMode.RendersFx) { NpcDialogueBox.Close(); QuestOfferWindow.Close(); } // the villager on the other end of a dialogue stays behind
            spawner.ClearForTransfer();
            MapTransfer.Instant(m.MapId);
            spawner = new ProxySpawner(buffer, Zone.Main.Root, m.HeroNetId, stats, sender);
            HeroController me = LocalPlayer.Hero;
            if (me != null)
            {
                me.JoinZone(Zone.Main, m.HeroNetId);
                me.ArriveInZone(new Vector2(NetQuantize.Pos(m.X), NetQuantize.Pos(m.Y)), m.Facing & 3);
                if (me.Predictor != null) me.Predictor.OnZoneChanged();
            }
            hasLastPos = false;
            for (int i = 0; i < heldSpawns.Count; i++) spawner.OnSpawn(heldSpawns[i]);
            heldSpawns.Clear();
            sender.Resume();
        }

        void HoldDespawn(ushort id)
        {
            for (int i = heldSpawns.Count - 1; i >= 0; i--) if (heldSpawns[i].Id == id) heldSpawns.RemoveAt(i);
        }
    }
}
