using System.Collections.Generic;
using System.Diagnostics;
using LiteNetLib;
using LiteNetLib.Utils;
using UnityEngine;
using ZenoniaProto.Net;
using Debug = UnityEngine.Debug;

namespace ZenoniaProto
{
    /// <summary>
    /// The authoritative zone host (server and host modes). Update (before the sim) polls the network and feeds hero input; LateUpdate (after the
    /// sim) replicates: AOI spawns, prioritised snapshots at 15 Hz per client, AOI filtered events every tick, self state and RPG pushes.
    /// Clients are never trusted: bounded reads, press counters, rate limit, name / class validation.
    /// </summary>
    [DefaultExecutionOrder(-900)]
    public sealed class ZoneServer : MonoBehaviour
    {
        const string ConnectionKey = "zenonia-net";
        const int MaxPacketsPerSecond = 120, MaxPendingHellos = 64;
        const float HelloTimeoutSeconds = 5f, SelfStateMs = 250f, RpgPushMs = 1000f;

        public static ZoneServer Instance { get; private set; }
        public int PlayerCount { get { return players.Count; } }

        NetManager net;
        EventBasedNetListener listener;
        ServerReplication repl;
        NetStats stats;
        readonly List<NetPlayer> players = new List<NetPlayer>();
        readonly Dictionary<NetPeer, float> unauthenticated = new Dictionary<NetPeer, float>(); // peer -> connect time (waiting for Hello)
        readonly Dictionary<NetPlayer, float> nextSnapMs = new Dictionary<NetPlayer, float>();
        readonly InputFrame[] frameBuf = new InputFrame[NetProtocol.MaxInputFrames];
        readonly NetDataWriter scratch = new NetDataWriter();
        readonly Stopwatch tickWatch = new Stopwatch(), phaseWatch = new Stopwatch();
        long simStart;
        double snapshotTicks, flushTicks;   // Stopwatch ticks spent this tick building / sending snapshots and flushing events
        ushort nextPlayerId = 1;
        int maxPlayers, tickRate, snapDiv;
        string devKey;
        float startTime, runSeconds, lastDtMs = 33f;
        string statsPath;
        bool finished;

        /// <summary>Starts listening on `port`; returns null (and logs) when the socket cannot be opened.</summary>
        public static ZoneServer Begin(GameObject root)
        {
            var s = root.AddComponent<ZoneServer>();
            if (!s.Open(NetLaunchArgs.Port)) { Destroy(s); return null; }
            return s;
        }

        bool Open(int port)
        {
            maxPlayers = NetLaunchArgs.MaxPlayers; tickRate = NetLaunchArgs.TickRate; snapDiv = NetLaunchArgs.SnapDiv;
            devKey = NetLaunchArgs.DevKey; runSeconds = NetLaunchArgs.RunSeconds; statsPath = NetLaunchArgs.StatsPath;
            stats = NetStats.Current = new NetStats(NetMode.IsHeadless ? "server" : "host");
            repl = new ServerReplication(stats);
            listener = new EventBasedNetListener();
            listener.ConnectionRequestEvent += OnConnectionRequest;
            listener.PeerDisconnectedEvent += OnPeerDisconnected;
            listener.NetworkReceiveEvent += OnReceive;
            net = new NetManager(listener)
            {
                UpdateTime = 15, DisconnectTimeout = 8000, PingInterval = 1000, ChannelsCount = 3, AutoRecycle = true, EnableStatistics = true, IPv6Enabled = false,
            };
            if (!net.Start(port)) { Debug.LogError("[ZenoniaProto] net: cannot listen on UDP port " + port); return false; }
            Instance = this;
            Zone.Main.BossDied += OnBossDied;
            NetEventBus.ResetState();
            NetEventBus.Active = true;
            startTime = Time.realtimeSinceStartup;
            Debug.Log("[ZenoniaProto] net: " + (NetMode.IsHeadless ? "server" : "host") + " listening on UDP " + port + " (max " + maxPlayers + " players, tick " + tickRate + " Hz, snapshots " + (tickRate / snapDiv) + " Hz)");
            return true;
        }

        uint ServerMs { get { return (uint)Zone.Main.TimeMs; } }

        void OnBossDied(Monster boss)
        {
            stats.Add("bossDeaths", 1);
            stats.Mark("bossDiedUtcMs", NetStats.UtcMs.ToString());
        }

        // ---- network callbacks (run inside PollEvents on the main thread) ----

        void OnConnectionRequest(ConnectionRequest request)
        {
            if (players.Count >= maxPlayers || unauthenticated.Count >= MaxPendingHellos)
            {
                NetMessageWriter.Reject(scratch, new RejectMsg { Reason = RejectReason.Full, Text = "Server full" });
                request.Reject(scratch.Data, 0, scratch.Length);
                stats.Add("rejectedFull", 1);
                return;
            }
            NetPeer peer = request.AcceptIfKey(ConnectionKey);
            if (peer != null) unauthenticated[peer] = Time.realtimeSinceStartup;
        }

        void OnPeerDisconnected(NetPeer peer, DisconnectInfo info)
        {
            unauthenticated.Remove(peer);
            NetPlayer p = peer.Tag as NetPlayer;
            if (p == null) return;
            RecordClientStats(p);
            players.Remove(p);
            nextSnapMs.Remove(p);
            ServerHeroFactory.Destroy(p);
            Debug.Log("[ZenoniaProto] net: " + p.Name + " left (" + info.Reason + "), " + players.Count + " online");
        }

        void OnReceive(NetPeer peer, NetPacketReader reader, byte channel, DeliveryMethod method)
        {
            NetPlayer p = peer.Tag as NetPlayer;
            byte op;
            if (!NetMessageReader.ReadOpcode(reader, out op)) { Kick(peer, "empty packet"); return; }
            if (p != null && !RateOk(p)) { Kick(peer, "packet flood"); return; }
            switch (op)
            {
                case NetOp.Hello: if (p != null) Kick(peer, "second hello"); else OnHello(peer, reader); break;
                case NetOp.Input:
                    ushort ack;
                    int n;
                    if (p == null) { Kick(peer, "input before hello"); break; }
                    if (!NetMessageReader.TryInput(reader, out ack, frameBuf, out n)) { Kick(peer, "bad input"); break; }
                    p.AcceptAck(ack);
                    p.Input.State.Accept(frameBuf, n, ServerMs);
                    break;
                case NetOp.InvAction:
                    InvActionMsg inv;
                    if (p == null || !NetMessageReader.TryInvAction(reader, out inv)) { Kick(peer, "bad inventory action"); break; }
                    ServerInventoryActions.Apply(repl, p, inv);
                    break;
                case NetOp.Revive: break; // phase 1 respawns automatically
                default: Kick(peer, "unknown opcode " + op); break;
            }
        }

        void OnHello(NetPeer peer, NetPacketReader reader)
        {
            HelloMsg m;
            if (!NetMessageReader.TryHello(reader, out m)) { Kick(peer, "bad hello"); return; }
            if (m.Version != NetProtocol.Version) { Reject(peer, RejectReason.Version, "Version mismatch (server " + NetProtocol.Version + ")"); return; }
            if (!DevLoginStub.KeyOk(devKey, m.Token)) { Reject(peer, RejectReason.BadToken, "Bad dev key"); return; }
            if (players.Count >= maxPlayers) { Reject(peer, RejectReason.Full, "Server full"); return; }
            HeroClassDef def;
            if (!DevLoginStub.TryClass(m.HeroClass, out def)) { Kick(peer, "bad class"); return; }
            string name = DevLoginStub.SanitizeName(m.Name);
            foreach (NetPlayer o in players) if (string.Equals(o.Name, name, System.StringComparison.OrdinalIgnoreCase)) { Reject(peer, RejectReason.AlreadyOnline, "Name already online"); return; }

            unauthenticated.Remove(peer);
            var p = new NetPlayer { Peer = peer, PlayerId = nextPlayerId++, ConnectedAtRealtime = Time.realtimeSinceStartup };
            if (nextPlayerId == 0) nextPlayerId = 1;
            ServerHeroFactory.Create(p, def, name);
            peer.Tag = p;
            players.Add(p);
            nextSnapMs[p] = Zone.Main.TimeMs + (p.PlayerId % snapDiv) * (1000f / tickRate);
            stats.Max("playersPeak", players.Count);
            Zone zone = Zone.Main;
            repl.SendWelcome(p, ServerMs, tickRate, snapDiv);
            repl.RebuildGrid(zone);
            repl.UpdateInterest(zone, p);
            repl.SendSelfState(p);
            repl.SendRpgState(p, 0, true, "");
            p.RpgDirty = false;
            Debug.Log("[ZenoniaProto] net: " + name + " (" + def.Name + ") joined, " + players.Count + " online");
        }

        void Reject(NetPeer peer, RejectReason reason, string text)
        {
            unauthenticated.Remove(peer);
            stats.Add("rejected" + reason, 1);
            repl.SendReject(peer, net, reason, text);
        }

        void Kick(NetPeer peer, string why)
        {
            stats.Add("kicked", 1);
            Debug.LogWarning("[ZenoniaProto] net: kicked " + peer + ": " + why);
            net.DisconnectPeer(peer);
        }

        bool RateOk(NetPlayer p)
        {
            float now = Time.realtimeSinceStartup * 1000f;
            if (now - p.RateWindowStartMs > 1000f) { p.RateWindowStartMs = now; p.PacketsInWindow = 0; }
            return ++p.PacketsInWindow <= MaxPacketsPerSecond;
        }

        // ---- frame loop ----

        void Update()
        {
            tickWatch.Restart();
            net.PollEvents();
            stats.Sample("tickPollMs", (float)tickWatch.Elapsed.TotalMilliseconds);
            float now = Time.realtimeSinceStartup;
            // A frame hitch (GC, disk, a busy machine) delays the hero steps while the clients keep sending: the lost ticks become catch-up credit.
            int lostTicks = Mathf.FloorToInt(Time.unscaledDeltaTime * tickRate + 0.3f) - 1;
            if (lostTicks > 0) for (int i = 0; i < players.Count; i++) players[i].Input.State.AddCredit(lostTicks);
            if (unauthenticated.Count > 0) DropSilentPeers(now);
            for (int i = 0; i < players.Count; i++)
            {
                NetPlayer p = players[i];
                if (p.Hero == null) continue;
                ServerInputState st = p.Input.State;
                if (st.TakePotion(true) && p.Rpg.UsePotion(true).Ok) NetEventBus.Potion(p.Hero, true);
                if (st.TakePotion(false) && p.Rpg.UsePotion(false).Ok) NetEventBus.Potion(p.Hero, false);
                st.ClampPotions(3);
            }
            simStart = Stopwatch.GetTimestamp();
        }

        /// <summary>A connection that never says Hello within 5 s is cut (it would otherwise hold a slot).</summary>
        void DropSilentPeers(float now)
        {
            List<NetPeer> stale = null;
            foreach (KeyValuePair<NetPeer, float> kv in unauthenticated)
                if (now - kv.Value > HelloTimeoutSeconds) (stale ?? (stale = new List<NetPeer>())).Add(kv.Key);
            if (stale == null) return;
            foreach (NetPeer peer in stale) { unauthenticated.Remove(peer); net.DisconnectPeer(peer); stats.Add("helloTimeouts", 1); }
        }

        void LateUpdate()
        {
            Zone zone = Zone.Main;
            if (simStart != 0) stats.Sample("tickSimMs", (float)((Stopwatch.GetTimestamp() - simStart) * 1000.0 / Stopwatch.Frequency)); // heroes, monsters, projectiles (everything between Update and LateUpdate)
            lastDtMs = Mathf.Max(1f, GameClock.Dt * 1000f);
            uint serverMs = ServerMs;
            phaseWatch.Restart();
            repl.RebuildGrid(zone);
            repl.RouteEvents(players);
            stats.Sample("tickGridEventsMs", (float)phaseWatch.Elapsed.TotalMilliseconds);
            snapshotTicks = flushTicks = 0.0;
            for (int i = 0; i < players.Count; i++)
            {
                NetPlayer p = players[i];
                if (p.Hero == null || p.Peer.ConnectionState != ConnectionState.Connected) continue;
                float next = nextSnapMs[p];
                if (zone.TimeMs + lastDtMs * 0.5f >= next)
                {
                    long t0 = Stopwatch.GetTimestamp();
                    repl.UpdateInterest(zone, p);
                    repl.SendSnapshot(zone, p, serverMs);
                    snapshotTicks += Stopwatch.GetTimestamp() - t0;
                    float interval = snapDiv * 1000f / tickRate;
                    next += interval;
                    if (next < zone.TimeMs) next = zone.TimeMs + interval;
                    nextSnapMs[p] = next;
                }
                long t1 = Stopwatch.GetTimestamp();
                repl.FlushEvents(p, serverMs);
                PushPlayerState(zone, p);
                flushTicks += Stopwatch.GetTimestamp() - t1;
            }
            stats.Sample("tickSnapshotMs", (float)(snapshotTicks * 1000.0 / Stopwatch.Frequency));
            stats.Sample("tickFlushMs", (float)(flushTicks * 1000.0 / Stopwatch.Frequency));
            tickWatch.Stop();
            stats.Sample("tickMs", (float)tickWatch.Elapsed.TotalMilliseconds);
            if (!finished && runSeconds > 0f && Time.realtimeSinceStartup - startTime >= runSeconds) Finish();
        }

        void PushPlayerState(Zone zone, NetPlayer p)
        {
            float now = zone.TimeMs;
            if (now - p.LastSelfSentMs >= SelfStateMs)
            {
                HeroController h = p.Hero;
                SelfStateMsg l = p.LastSelf;
                bool changed = Mathf.CeilToInt(h.Hp) != l.Hp || Mathf.CeilToInt(h.Mp) != l.Mp || h.Rpg.Exp != (int)l.Exp || h.Rpg.Level != l.Level || h.SkillCooldownLeftMs > 0f || now - p.LastSelfSentMs >= 1000f;
                if (changed) { repl.SendSelfState(p); p.LastSelfSentMs = now; }
            }
            if (p.RpgDirty && now - p.LastRpgSentMs >= RpgPushMs) { repl.SendRpgState(p, 0, true, ""); p.RpgDirty = false; p.LastRpgSentMs = now; }
        }

        // ---- shutdown ----

        void RecordClientStats(NetPlayer p)
        {
            float secs = Mathf.Max(1f, Time.realtimeSinceStartup - p.ConnectedAtRealtime);
            stats.Sample("clientDownKBps", p.Peer.Statistics.BytesSent / 1024f / secs); // what the server sent = what the client downloaded
            stats.Sample("clientUpKBps", p.Peer.Statistics.BytesReceived / 1024f / secs);
            stats.Sample("clientRttMs", p.Peer.RoundTripTime);
            stats.Add("inputFramesDropped", p.Input.State.Dropped); // frames the jitter buffer cut (client ahead / bursts)
            stats.Add("inputStepsStarved", p.Input.State.Starved);  // steps the hero froze because the next frame was late
            stats.Add("inputCatchUps", p.Input.State.CatchUps);     // frozen steps repaid with a second step in one tick
            stats.Add("inputSteps", p.Input.State.LastSeq);
        }

        void Finish()
        {
            finished = true;
            for (int i = 0; i < players.Count; i++) RecordClientStats(players[i]);
            stats.Set("playersAtEnd", players.Count);
            stats.Set("zoneKills", Zone.Main.Kills);
            stats.Write(statsPath);
            Debug.Log("[ZenoniaProto] net: run finished, stats written to " + statsPath);
            Application.Quit();
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (Zone.Main != null) Zone.Main.BossDied -= OnBossDied;
            NetEventBus.Active = false;
            if (net != null) net.Stop();
            if (stats != null) stats.Dispose();
        }
    }
}
