using System.Collections.Generic;
using System.Diagnostics;
using LiteNetLib;
using LiteNetLib.Utils;
using UnityEngine;
using ZenoniaProto.Net;
using ZenoniaProto.Rpg;
using Debug = UnityEngine.Debug;

namespace ZenoniaProto
{
    /// <summary>
    /// The authoritative zone host (server and host modes). Update (before the sim) polls the network and feeds hero input; LateUpdate (after the
    /// sim) moves heroes through portals (ZoneTransferService) and replicates each awake zone to its own players: AOI spawns, prioritised snapshots at 15 Hz per client,
    /// AOI filtered events every tick, self state and RPG pushes. Clients are never trusted: bounded reads, press counters, rate limit, name / class validation.
    /// </summary>
    [DefaultExecutionOrder(-900)]
    public sealed class ZoneServer : MonoBehaviour
    {
        const string ConnectionKey = "zenonia-net";
        const int MaxPacketsPerSecond = 120, MaxPendingHellos = 64;
        const float HelloTimeoutSeconds = 5f, SelfStateMs = 250f, RpgPushMs = 1000f, DrainSeconds = 30f;

        public static ZoneServer Instance { get; private set; }
        public int PlayerCount { get { return players.Count; } }

        NetManager net;
        EventBasedNetListener listener;
        ServerReplication repl;
        ZoneHost host;
        ZoneTransferService transfers;
        PartyService party;
        TradeService trade;
        ZonePersistence persistence;
        NetStats stats;
        readonly List<NetPlayer> players = new List<NetPlayer>();
        readonly Dictionary<NetPeer, float> unauthenticated = new Dictionary<NetPeer, float>(); // peer -> connect time (waiting for Hello)
        readonly HashSet<NetPeer> redeeming = new HashSet<NetPeer>(); // said Hello with a ticket, the API has not answered yet
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
        bool finished, drained;
        float drainDeadline = -1f, nextShutdownCheck;

        /// <summary>Starts listening on `port`; returns null (and logs) when the socket cannot be opened.</summary>
        public static ZoneServer Begin(GameObject root)
        {
            var s = root.AddComponent<ZoneServer>();
            // Real accounts only on a dedicated server: a host is a dev toy on somebody's own screen. A broken setup stops here instead of quietly running without saves.
            if (!string.IsNullOrEmpty(NetLaunchArgs.ApiUrl) && NetMode.IsHeadless && !ZonePersistence.TryCreate(out s.persistence)) { Destroy(s); return null; }
            if (!s.Open(NetLaunchArgs.Port)) { Destroy(s); return null; }
            return s;
        }

        bool Open(int port)
        {
            maxPlayers = NetLaunchArgs.MaxPlayers; tickRate = NetLaunchArgs.TickRate; snapDiv = NetLaunchArgs.SnapDiv;
            devKey = NetLaunchArgs.DevKey; runSeconds = NetLaunchArgs.RunSeconds; statsPath = NetLaunchArgs.StatsPath;
            stats = NetStats.Current = new NetStats(NetMode.IsHeadless ? "server" : "host");
            ApplyWantedBossOverride();
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
            host = NetMode.IsHeadless ? ZoneHost.CreateDedicated(transform, NetLaunchArgs.StartMap) : ZoneHost.CreateSingle(Zone.Main);
            transfers = new ZoneTransferService(host, repl, stats);
            party = new PartyService(stats);
            trade = new TradeService(repl, stats, persistence);
            if (persistence != null) { persistence.Start(stats, OnCharacterLost); Application.wantsToQuit += OnWantsToQuit; }
            else Debug.LogWarning("[ZenoniaProto] net: DEV LOGIN: no accounts, nothing is saved, every join is a new character" + (NetLaunchArgs.DevLevel + NetLaunchArgs.DevGold + NetLaunchArgs.DevItems + NetLaunchArgs.DevHurtPct > 0 ? ", dev cheats on" : "") + ". Start with --api-url for real accounts.");
            for (int i = 0; i < host.Zones.Count; i++) host.Zones[i].BossDied += OnBossDied;
            NetEventBus.ResetState();
            NetEventBus.Active = true;
            startTime = Time.realtimeSinceStartup;
            Debug.Log("[ZenoniaProto] net: " + (NetMode.IsHeadless ? "server" : "host") + " listening on UDP " + port + " (max " + maxPlayers + " players, tick " + tickRate + " Hz, snapshots " + (tickRate / snapDiv) + " Hz, " + host.Zones.Count + " zones, heroes begin on " + host.Start.Map.Id + (transfers.PortalsLive ? "" : ", portals off") + ")");
            return true;
        }

        /// <summary>--wanted-boss: pins the daily Wanted Boss for tests / live ops. An unknown type is an error and changes nothing; a valid one warns on every start.</summary>
        static void ApplyWantedBossOverride()
        {
            string type = NetLaunchArgs.WantedBossType;
            if (string.IsNullOrEmpty(type)) return;
            if (WantedBoss.Find(type) == null) { Debug.LogError("[ZenoniaProto] net: --wanted-boss '" + type + "' is not a wanted boss (rimeclawTyrant, glacialColossus, mireHydraQueen): ignored"); return; }
            WantedBoss.Override = type;
            Debug.LogWarning("[ZenoniaProto] net: WANTED BOSS PINNED to " + type + " (--wanted-boss): the daily rotation is off. Never set this on a public server.");
        }

        uint ServerMs { get { return (uint)host.ClockMs; } }

        void OnBossDied(Monster boss)
        {
            stats.Add("bossDeaths", 1);
            stats.Mark("bossDiedUtcMs", NetStats.UtcMs.ToString());
        }

        // ---- network callbacks (run inside PollEvents on the main thread) ----

        void OnConnectionRequest(ConnectionRequest request)
        {
            if (players.Count >= maxPlayers || unauthenticated.Count + redeeming.Count >= MaxPendingHellos || drainDeadline >= 0f)
            {
                NetMessageWriter.Reject(scratch, new RejectMsg { Reason = RejectReason.Full, Text = drainDeadline >= 0f ? "Server is shutting down" : "Server full" });
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
            redeeming.Remove(peer);
            NetPlayer p = peer.Tag as NetPlayer;
            if (p == null) return;
            RecordClientStats(p);
            party.OnLeave(p);
            trade.OnLeave(p);
            if (persistence != null) persistence.OnLeave(p); // while the hero is still here to say where it stood; a trade being written goes first
            players.Remove(p);
            if (p.Hero != null && p.Hero.HomeZone != null) p.Hero.HomeZone.Players.Remove(p);
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
                case NetOp.Hello: if (p != null || redeeming.Contains(peer)) Kick(peer, "second hello"); else OnHello(peer, reader); break;
                case NetOp.Input:
                    byte epoch;
                    ushort ack;
                    int n;
                    if (p == null) { Kick(peer, "input before hello"); break; }
                    if (!NetMessageReader.TryInput(reader, out epoch, out ack, frameBuf, out n)) { Kick(peer, "bad input"); break; }
                    if (epoch != p.Epoch) { stats.Add("inputStaleEpoch", 1); break; } // pressed on the map the hero just left, or before the client heard: its ack is void too
                    p.AcceptAck(ack);
                    p.Input.State.Accept(frameBuf, n, ServerMs);
                    break;
                case NetOp.InvAction:
                    InvActionMsg inv;
                    if (p == null || !NetMessageReader.TryInvAction(reader, out inv)) { Kick(peer, "bad inventory action"); break; }
                    ServerInventoryActions.Apply(repl, p, inv);
                    break;
                case NetOp.Revive: break; // phase 1 respawns automatically
                case NetOp.PartyRequest:
                    PartyRequestMsg pr;
                    if (p == null || !PartyRequestMsg.TryRead(reader, out pr)) { Kick(peer, "bad party request"); break; }
                    party.OnRequest(p, pr, host.ClockMs);
                    break;
                case NetOp.TradeRequest:
                    TradeRequestMsg tr;
                    if (p == null || !TradeRequestMsg.TryRead(reader, out tr)) { Kick(peer, "bad trade request"); break; }
                    trade.OnRequest(p, tr, host.ClockMs);
                    break;
                default: Kick(peer, "unknown opcode " + op); break;
            }
        }

        void OnHello(NetPeer peer, NetPacketReader reader)
        {
            HelloMsg m;
            if (!NetMessageReader.TryHello(reader, out m)) { Kick(peer, "bad hello"); return; }
            if (m.Version != NetProtocol.Version) { Reject(peer, RejectReason.Version, "Version mismatch (server " + NetProtocol.Version + ")"); return; }
            if (players.Count >= maxPlayers) { Reject(peer, RejectReason.Full, "Server full"); return; }
            if (persistence != null) { BeginTicketJoin(peer, m.Token); return; } // the name and class in the Hello mean nothing here: the character comes from the API
            if (!DevLoginStub.KeyOk(devKey, m.Token)) { Reject(peer, RejectReason.BadToken, "Bad dev key"); return; }
            HeroClassDef def;
            if (!DevLoginStub.TryClass(m.HeroClass, out def)) { Kick(peer, "bad class"); return; }
            string name = DevLoginStub.SanitizeName(m.Name);
            foreach (NetPlayer o in players) if (string.Equals(o.Name, name, System.StringComparison.OrdinalIgnoreCase)) { Reject(peer, RejectReason.AlreadyOnline, "Name already online"); return; }

            unauthenticated.Remove(peer);
            Zone zone = host.Start;
            host.Wake(zone); // also makes it the current zone: the factory builds the hero there
            NetPlayer p = NewPlayer(peer);
            ServerHeroFactory.CreateDev(p, def, name);
            FinishJoin(p, zone, def);
        }

        /// <summary>Real account: the ticket goes to the API, which answers with the character and the one-session lock. The game keeps running; the peer just waits, and may leave meanwhile.</summary>
        void BeginTicketJoin(NetPeer peer, string ticket)
        {
            if (!ZoneTicket.LooksValid(ticket)) { stats.Add("ticketsMalformed", 1); Reject(peer, RejectReason.BadToken, "Bad ticket"); return; }
            if (!persistence.Ready) { Reject(peer, RejectReason.Unavailable, "The world server is starting, try again"); return; }
            unauthenticated.Remove(peer);
            redeeming.Add(peer);
            persistence.Redeem(ticket, r =>
            {
                bool waiting = redeeming.Remove(peer);
                if (!r.Ok)
                {
                    stats.Add("tickets" + r.Refusal, 1);
                    if (waiting) Reject(peer, r.Refusal, r.Text);
                    return;
                }
                string refusal = !waiting || peer.ConnectionState != ConnectionState.Connected ? "gone" : players.Count >= maxPlayers ? "Server full" : drainDeadline >= 0f ? "Server is shutting down" : persistence.IsHere(r.CharacterId) ? "This character is already online" : null;
                if (refusal != null)
                {
                    persistence.ReleaseUnused(r); // nothing was changed: only the lock goes back
                    if (waiting && refusal != "gone") Reject(peer, RejectReason.AlreadyOnline, refusal);
                    return;
                }
                JoinLoaded(peer, r);
            });
        }

        void JoinLoaded(NetPeer peer, ZonePersistence.LoadedCharacter c)
        {
            Zone zone = host.ZoneOf(c.Save.mapId) ?? host.Start;
            host.Wake(zone);
            NetPlayer p = NewPlayer(peer);
            ServerHeroFactory.CreateLoaded(p, c.Def, c.Name, c.State, c.Save);
            persistence.Attach(p, c);
            FinishJoin(p, zone, c.Def);
        }

        NetPlayer NewPlayer(NetPeer peer)
        {
            var p = new NetPlayer { Peer = peer, PlayerId = nextPlayerId++, ConnectedAtRealtime = Time.realtimeSinceStartup };
            if (nextPlayerId == 0) nextPlayerId = 1;
            return p;
        }

        void FinishJoin(NetPlayer p, Zone zone, HeroClassDef def)
        {
            NetPeer peer = p.Peer;
            peer.Tag = p;
            players.Add(p);
            zone.Players.Add(p);
            party.OnJoin(p);
            trade.OnJoin(p);
            nextSnapMs[p] = host.ClockMs + (p.PlayerId % snapDiv) * (1000f / tickRate);
            stats.Max("playersPeak", players.Count);
            repl.SendWelcome(p, zone.Map.Id, transfers.PortalsLive, ServerMs, tickRate, snapDiv);
            repl.RebuildGrid(zone);
            repl.UpdateInterest(zone, p);
            repl.SendSelfState(p);
            repl.SendRpgState(p, 0, true, "");
            p.RpgDirty = false;
            Debug.Log("[ZenoniaProto] net: " + p.Name + " (" + def.Name + ") joined, " + players.Count + " online");
        }

        /// <summary>Somebody else owns this character now (its session is gone): this copy must leave the world at once, nothing it does can count any more.</summary>
        void OnCharacterLost(NetPlayer p)
        {
            stats.Add("playersKickedLostCharacter", 1);
            if (p.Peer != null && p.Peer.ConnectionState == ConnectionState.Connected) Reject(p.Peer, RejectReason.AlreadyOnline, "This character was opened elsewhere");
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
            if (persistence != null) { persistence.Tick((long)(Time.realtimeSinceStartupAsDouble * 1000.0)); TickShutdown(); }
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
                Zone.Enter(p.Hero.HomeZone);
                ServerInputState st = p.Input.State;
                int hpSize = p.Rpg.State.QuickPotionSize(true), mpSize = p.Rpg.State.QuickPotionSize(false);
                bool drinks = !TradeService.IsTrading(p); // the keys are spent either way: a press during a trade must not fire when it ends
                if (st.TakePotion(true) && drinks && p.Rpg.UsePotion(true).Ok) NetEventBus.Potion(p.Hero, true, hpSize);
                if (st.TakePotion(false) && drinks && p.Rpg.UsePotion(false).Ok) NetEventBus.Potion(p.Hero, false, mpSize);
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
            host.TickPumps();
            if (simStart != 0) stats.Sample("tickSimMs", (float)((Stopwatch.GetTimestamp() - simStart) * 1000.0 / Stopwatch.Frequency)); // heroes, monsters, projectiles, skills (everything between Update and here)
            lastDtMs = Mathf.Max(1f, GameClock.Dt * 1000f);
            uint serverMs = ServerMs;
            transfers.CheckPortals(lastDtMs);
            phaseWatch.Restart();
            IReadOnlyList<Zone> zones = host.Zones;
            for (int z = 0; z < zones.Count; z++)
            {
                Zone zone = zones[z];
                if (zone.Paused) continue;
                if (zone.Players.Count == 0) { zone.Events.Clear(); continue; } // nobody here to tell
                repl.RebuildGrid(zone);
                repl.RouteEvents(zone);
            }
            stats.Sample("tickGridEventsMs", (float)phaseWatch.Elapsed.TotalMilliseconds);
            snapshotTicks = flushTicks = 0.0;
            for (int z = 0; z < zones.Count; z++)
            {
                Zone zone = zones[z];
                if (zone.Paused || zone.Players.Count == 0) continue;
                Zone.Enter(zone);
                ReplicateZone(zone, serverMs);
            }
            stats.Sample("tickSnapshotMs", (float)(snapshotTicks * 1000.0 / Stopwatch.Frequency));
            stats.Sample("tickFlushMs", (float)(flushTicks * 1000.0 / Stopwatch.Frequency));
            int paused = host.PauseEmptyZones(lastDtMs);
            if (paused > 0) stats.Add("zonePauses", paused);
            stats.Sample("zonesAwake", host.ZonesAwake);
            Zone.Enter(host.Start);
            party.Tick(host.ClockMs);
            trade.Tick(host.ClockMs);
            tickWatch.Stop();
            stats.Sample("tickMs", (float)tickWatch.Elapsed.TotalMilliseconds);
            if (!finished && runSeconds > 0f && Time.realtimeSinceStartup - startTime >= runSeconds) Finish();
        }

        void ReplicateZone(Zone zone, uint serverMs)
        {
            List<NetPlayer> here = zone.Players;
            for (int i = 0; i < here.Count; i++)
            {
                NetPlayer p = here[i];
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
        }

        void PushPlayerState(Zone zone, NetPlayer p)
        {
            float now = zone.TimeMs;
            if (now - p.LastSelfSentMs >= SelfStateMs)
            {
                HeroController h = p.Hero;
                SelfStateMsg l = p.LastSelf;
                bool changed = h.PetDurability != l.PetDurability || Mathf.CeilToInt(h.Hp) != l.Hp || Mathf.CeilToInt(h.Mp) != l.Mp || h.Rpg.Exp != (int)l.Exp || h.Rpg.Level != l.Level || h.AnySkillCooldown || HeroBuffs.AnyOn(h) || (l.Buffs != null && l.Buffs.Length > 0) || now - p.LastSelfSentMs >= 1000f;
                if (changed) { repl.SendSelfState(p); p.LastSelfSentMs = now; }
            }
            if (p.RpgDirty && now - p.LastRpgSentMs >= RpgPushMs) { repl.SendRpgState(p, 0, true, ""); p.RpgDirty = false; p.LastRpgSentMs = now; }
        }

        // ---- shutdown ----

        /// <summary>The ways to stop a server with everybody saved: --shutdown-file (works everywhere, a hidden Windows process has no window to close), --run-seconds and Application.Quit from outside. A killed process is the 30 s case: what the last periodic save had.</summary>
        void TickShutdown()
        {
            float now = Time.realtimeSinceStartup;
            if (drainDeadline < 0f)
            {
                if (now < nextShutdownCheck) return;
                nextShutdownCheck = now + 1f;
                if (NetLaunchArgs.ShutdownFile != null && System.IO.File.Exists(NetLaunchArgs.ShutdownFile))
                {
                    try { System.IO.File.Delete(NetLaunchArgs.ShutdownFile); } catch (System.Exception) { /* it did its job */ }
                    BeginDrain();
                }
                return;
            }
            if (players.Count > 0 && now < drainDeadline) return;
            if (persistence.Idle || now >= drainDeadline) EndDrain();
        }

        void BeginDrain()
        {
            if (drainDeadline >= 0f) return;
            drainDeadline = Time.realtimeSinceStartup + DrainSeconds;
            Debug.Log("[ZenoniaProto] net: shutting down, saving " + players.Count + " players");
            for (int i = 0; i < players.Count; i++)
            {
                NetPeer peer = players[i].Peer;
                if (peer.ConnectionState != ConnectionState.Connected) continue;
                NetMessageWriter.Reject(scratch, new RejectMsg { Reason = RejectReason.Unavailable, Text = "Server is restarting" });
                net.DisconnectPeer(peer, scratch);
            }
            foreach (NetPeer peer in new List<NetPeer>(unauthenticated.Keys)) net.DisconnectPeer(peer);
            foreach (NetPeer peer in new List<NetPeer>(redeeming)) net.DisconnectPeer(peer);
        }

        void EndDrain()
        {
            drained = true;
            persistence.KeepWhatIsLeft("server stopped");
            Debug.Log("[ZenoniaProto] net: shutdown complete, " + stats.Get("savesWritten") + " saves written");
            Application.Quit();
        }

        bool OnWantsToQuit()
        {
            if (drained || persistence == null) return true;
            BeginDrain();
            return false;
        }

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
            stats.Add("skillCastsServer", p.Hero.SkillCasts);      // tree skills cast by the players (all accepted by the server rules)
        }

        void Finish()
        {
            finished = true;
            for (int i = 0; i < players.Count; i++) RecordClientStats(players[i]);
            stats.Set("playersAtEnd", players.Count);
            int kills = 0;
            for (int i = 0; i < host.Zones.Count; i++) kills += host.Zones[i].Kills;
            stats.Set("zoneKills", kills);
            stats.Write(statsPath);
            Debug.Log("[ZenoniaProto] net: run finished, stats written to " + statsPath);
            if (persistence != null) BeginDrain();
            else Application.Quit();
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (persistence != null)
            {
                Application.wantsToQuit -= OnWantsToQuit;
                persistence.KeepWhatIsLeft("server object destroyed");
                persistence.Stop();
            }
            if (party != null) party.Stop();
            if (trade != null) trade.Stop();
            if (host != null)
            {
                for (int i = 0; i < host.Zones.Count; i++) host.Zones[i].BossDied -= OnBossDied;
                host.Stop();
            }
            NetEventBus.Active = false;
            if (net != null) net.Stop();
            if (stats != null) stats.Dispose();
        }
    }
}
