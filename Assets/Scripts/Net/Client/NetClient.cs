using System.Collections.Generic;
using LiteNetLib;
using LiteNetLib.Utils;
using UnityEngine;
using ZenoniaProto.Net;
using NetEvent = ZenoniaProto.Net.NetEvent;

namespace ZenoniaProto
{
    public enum ClientState { Idle, Connecting, Joined, Failed }

    /// <summary>
    /// The online client: connects, says Hello, and mirrors the server -- Spawn / Despawn build the proxies, snapshots feed the interpolation buffer,
    /// events are replayed at render time, the input sender streams the controls. Runs just before the other scripts (order -800) so proxies are
    /// up to date when the frame's hero / monster / camera updates read them.
    /// </summary>
    [DefaultExecutionOrder(-800)]
    public sealed partial class NetClient : MonoBehaviour
    {
        const string ConnectionKey = "zenonia-net";
        public static NetClient Active { get; private set; }
        public static double ClientMs { get { return Time.realtimeSinceStartupAsDouble * 1000.0; } }
        /// <summary>Estimated server clock in ms (0 offline / before the first snapshot).</summary>
        public static float ServerNowMs { get { return Active != null && Active.buffer.HasClock ? (float)Active.buffer.ServerNow(ClientMs) : 0f; } }

        public ClientState State { get; private set; }
        public string Status { get; private set; } = "";

        NetManager net;
        NetPeer server;
        EventBasedNetListener listener;
        SnapshotBuffer buffer = new SnapshotBuffer();
        readonly SnapshotMsg snapMsg = new SnapshotMsg();
        readonly SnapshotRing ring = new SnapshotRing();
        readonly SnapshotLookup lookup = new SnapshotLookup();
        uint ackSeq;                       // newest snapshot decoded (the delta baseline the server may use)
        bool ownerPending;                 // a snapshot with the owner block arrived: reconcile at the next frame
        ushort ownerLastInput;
        HeroSnap ownerSnap;
        OwnerSim ownerSim;
        ushort myHeroId;
        float predictAt = -1f;             // --predict-after: when to switch the local hero to prediction
        readonly List<NetEvent> eventBuf = new List<NetEvent>(64);
        readonly NetDataWriter w = new NetDataWriter();
        NetStats stats;
        ProxySpawner spawner;
        NetEventPlayer events;
        NetInputSender sender;
        ClientRpgMirror mirror;
        HelloMsg hello;
        float startTime, runSeconds;
        string statsPath;
        uint invReq;
        bool finished;

        public static NetClient Begin(GameObject root)
        {
            var c = root.AddComponent<NetClient>();
            c.stats = NetStats.Current = new NetStats("client");
            c.runSeconds = NetLaunchArgs.RunSeconds;
            c.statsPath = NetLaunchArgs.StatsPath;
            c.startTime = Time.realtimeSinceStartup;
            c.sender = new NetInputSender(c.stats);
            c.mirror = new ClientRpgMirror(c.stats);
            Active = c;
            return c;
        }

        /// <summary>Opens the connection; the result arrives as State = Joined (Welcome) or Failed (Status = reason).</summary>
        public void Connect(string host, int port, string name, int heroClass, string devKey)
        {
            Disconnect();
            hello = new HelloMsg { Version = NetLaunchArgs.TestVersion != 0 ? (ushort)NetLaunchArgs.TestVersion : NetProtocol.Version, Name = name, HeroClass = (byte)heroClass, Token = devKey ?? "" };
            listener = new EventBasedNetListener();
            listener.PeerConnectedEvent += OnConnected;
            listener.PeerDisconnectedEvent += OnDisconnected;
            listener.NetworkReceiveEvent += OnReceive;
            net = new NetManager(listener) { UpdateTime = 15, DisconnectTimeout = 8000, PingInterval = 1000, ChannelsCount = 3, AutoRecycle = true, EnableStatistics = true, IPv6Enabled = false, MaxConnectAttempts = 8 };
            NetLinkSimulator.ConfigureReceive(net);
            buffer.Clear();
            ring.Clear(); ackSeq = 0; ownerPending = false;
            if (!net.Start()) { Fail("Cannot open a network socket"); return; }
            try { server = net.Connect(host, port, ConnectionKey); }
            catch (System.Exception e) { Fail("Cannot connect: " + e.Message); return; }
            State = ClientState.Connecting;
            Status = "Connecting to " + host + ":" + port + " ...";
            Debug.Log("[ZenoniaProto] net: connecting to " + host + ":" + port + " as " + name);
        }

        public void Disconnect()
        {
            if (net != null) { net.Stop(); net = null; }
            server = null;
            if (spawner != null) { spawner.DestroyAll(); spawner = null; }
            events = null;
            transferring = false; heldSpawns.Clear();
            if (State != ClientState.Failed) State = ClientState.Idle;
        }

        void Fail(string why)
        {
            State = ClientState.Failed;
            Status = why;
            Debug.LogWarning("[ZenoniaProto] net: " + why);
            if (net != null) { net.Stop(); net = null; }
            server = null;
        }

        public static void SendInvAction(InvOp op, string uid, byte slot)
        {
            NetClient c = Active;
            if (c == null || c.server == null || c.State != ClientState.Joined) return;
            NetMessageWriter.InvAction(c.w, new InvActionMsg { ReqId = ++c.invReq, Op = op, ItemUid = uid, Slot = slot });
            c.server.Send(c.w, NetProtocol.ChannelReliable, DeliveryMethod.ReliableOrdered);
        }

        // ---- callbacks ----

        void OnConnected(NetPeer peer)
        {
            NetMessageWriter.Hello(w, hello);
            peer.Send(w, NetProtocol.ChannelReliable, DeliveryMethod.ReliableOrdered);
        }

        void OnDisconnected(NetPeer peer, DisconnectInfo info)
        {
            string why = "Disconnected (" + info.Reason + ")";
            NetPacketReader extra = info.AdditionalData;
            byte op;
            RejectMsg rej;
            if (extra != null && extra.AvailableBytes > 0 && NetMessageReader.ReadOpcode(extra, out op) && op == NetOp.Reject && NetMessageReader.TryReject(extra, out rej)) why = "Rejected: " + rej.Text;
            else if (info.Reason == DisconnectReason.ConnectionFailed) why = "Could not reach the server";
            else if (info.Reason == DisconnectReason.Timeout) why = "Connection timed out";
            bool wasJoined = State == ClientState.Joined;
            State = ClientState.Failed;
            Status = why;
            server = null;
            if (wasJoined && stats != null) stats.Add("disconnects", 1);
            Debug.Log("[ZenoniaProto] net: " + why);
            transferring = false; heldSpawns.Clear();
            if (wasJoined) { spawner = null; events = null; if (GameSession.Instance != null) GameSession.Instance.OnConnectionLost(); }
        }

        void OnReceive(NetPeer peer, NetPacketReader r, byte channel, DeliveryMethod method)
        {
            byte op;
            if (!NetMessageReader.ReadOpcode(r, out op)) return;
            bool ok = true;
            switch (op)
            {
                case NetOp.Welcome:
                    WelcomeMsg wm;
                    if (!(ok = NetMessageReader.TryWelcome(r, out wm))) break;
                    OnWelcome(wm);
                    break;
                case NetOp.Spawn:
                    SpawnMsg sm;
                    if (!(ok = SpawnMsg.TryRead(r, out sm))) break;
                    if (transferring) heldSpawns.Add(sm); // the new zone's, while the old map is still fading out
                    else if (spawner != null) spawner.OnSpawn(sm);
                    break;
                case NetOp.Despawn:
                    DespawnMsg dm;
                    if (!(ok = NetMessageReader.TryDespawn(r, out dm))) break;
                    if (transferring) HoldDespawn(dm.Id);
                    else if (spawner != null) spawner.OnDespawn(dm);
                    break;
                case NetOp.ZoneChanged:
                    ZoneChangedMsg zc;
                    if ((ok = NetMessageReader.TryZoneChanged(r, out zc)) && spawner != null) OnZoneChanged(zc);
                    break;
                case NetOp.Snapshot:
                    stats.Max("snapshotMaxBytes", r.RawDataSize);
                    SnapshotReadResult read = SnapshotPacker.TryRead(r, snapMsg, ring, lookup, epoch);
                    if (read == SnapshotReadResult.StaleEpoch) { stats.Add("snapshotsStaleEpoch", 1); break; } // another zone stay: unreliable packets do not wait for ZoneChanged
                    if (read == SnapshotReadResult.MissingBaseline) { stats.Add("snapshotBaselineMisses", 1); break; } // not acked: the server falls back to older / full
                    if (!(ok = read == SnapshotReadResult.Ok)) break;
                    SnapshotPacker.Remember(ring, snapMsg);
                    if (ackSeq == 0 || NetQuantize.IsNewer(snapMsg.Seq, ackSeq)) ackSeq = snapMsg.Seq;
                    stats.Add("snapshotBytesTotal", r.RawDataSize);
                    if (buffer.Push(snapMsg, ClientMs))
                    {
                        stats.Add("snapshotsReceived", 1); stats.Max("heroesInSnapshotMax", snapMsg.Heroes.Count); stats.Max("monstersInSnapshotMax", snapMsg.Monsters.Count);
                        if (snapMsg.HasOwner && !transferring) CaptureOwner();
                    }
                    break;
                case NetOp.SelfState:
                    SelfStateMsg ss;
                    if (ok = NetMessageReader.TrySelfState(r, out ss)) mirror.OnSelfState(ss);
                    break;
                case NetOp.RpgState:
                    RpgStateMsg rs;
                    if (ok = NetMessageReader.TryRpgState(r, out rs)) mirror.OnRpgState(rs);
                    break;
                case NetOp.EventBatch:
                    uint t;
                    eventBuf.Clear();
                    if ((ok = NetMessageReader.TryEventBatch(r, out t, eventBuf)) && events != null) events.Enqueue(t, eventBuf);
                    break;
            }
            if (!ok) { stats.Add("malformedPackets", 1); Debug.LogWarning("[ZenoniaProto] net: malformed packet, opcode " + op); }
        }

        void CaptureOwner()
        {
            for (int i = 0; i < snapMsg.Heroes.Count; i++)
            {
                if (snapMsg.Heroes[i].Id != myHeroId) continue;
                ownerSnap = snapMsg.Heroes[i]; ownerSim = snapMsg.Owner; ownerLastInput = snapMsg.LastInputSeq; ownerPending = true;
                return;
            }
        }

        void OnWelcome(WelcomeMsg m)
        {
            ApplyWelcomeMap(m);
            Zone zone = Zone.Main;
            myHeroId = m.HeroNetId;
            if (NetLaunchArgs.PredictAfter > 0f) predictAt = Time.realtimeSinceStartup + NetLaunchArgs.PredictAfter;
            buffer = new SnapshotBuffer(1000.0 * Mathf.Max(1, (int)m.SnapDiv) / Mathf.Max(1, (int)m.TickRate));
            spawner = new ProxySpawner(buffer, zone.Root, m.HeroNetId, stats, sender);
            events = new NetEventPlayer(m.HeroNetId, stats);
            State = ClientState.Joined;
            Status = "Joined";
            stats.Set("joinedAtSeconds", Time.realtimeSinceStartup - startTime);
            Debug.Log("[ZenoniaProto] net: joined as player " + m.PlayerId + ", hero id " + m.HeroNetId);
        }

        // ---- frame loop ----

        void Update()
        {
            if (net != null) net.PollEvents();
            if (State == ClientState.Joined && server != null)
            {
                if (buffer.HasClock && !transferring)
                {
                    double rt = buffer.RenderTime(ClientMs);
                    spawner.Apply(rt);
                    events.Play(rt);
                }
                sender.Peer = server; sender.Ack = ackSeq;
                if (predictAt > 0f && Time.realtimeSinceStartup >= predictAt && LocalPlayer.Hero != null && !LocalPlayer.Hero.IsPredicted)
                {
                    LocalPlayer.Hero.AttachPredictor(new LocalHeroPredictor(LocalPlayer.Hero, sender, stats));
                    predictAt = -1f;
                    Debug.Log("[ZenoniaProto] net: client prediction switched on");
                }
                LocalHeroPredictor predictor = LocalPlayer.Hero != null ? LocalPlayer.Hero.Predictor : null;
                if (ownerPending && predictor != null) predictor.OnServerState(ownerLastInput, ownerSnap, ownerSim);
                ownerPending = false;
                sender.Tick(Time.unscaledDeltaTime, predictor != null);
                if (!transferring) TrackLocalHero();
            }
            if (stats != null && Time.frameCount % 60 == 0)
            {
                stats.Sample("clientFrameMs", Time.unscaledDeltaTime * 1000f);
                if (State == ClientState.Joined) { stats.Sample("interpDelayMs", (float)buffer.RenderDelayMs); stats.Sample("arrivalJitterMs", (float)buffer.JitterMs); }
            }
            if (!finished && runSeconds > 0f && Time.realtimeSinceStartup - startTime >= runSeconds) Finish();
        }

        Vector2 lastPos;
        bool hasLastPos;

        /// <summary>Stats only: distance the local hero covered (proves the input stream moves the server hero).</summary>
        bool moveWatch;
        float moveWatchStart;
        Vector2 moveWatchFrom;

        /// <summary>Stats: time from a direction key going down to the local hero visibly moving (0.5 units): the felt input latency.</summary>
        void TrackInputToMotion(HeroController me)
        {
            bool held = InputSampler.Live && (InputAdapter.Held(GameKey.Up) || InputAdapter.Held(GameKey.Down) || InputAdapter.Held(GameKey.Left) || InputAdapter.Held(GameKey.Right));
            if (!held) { moveWatch = false; return; }
            if (!moveWatch && !hasLastPos) return;
            if (!moveWatch) { moveWatch = true; moveWatchStart = Time.realtimeSinceStartup; moveWatchFrom = me.Position; return; }
            if (moveWatchStart < 0f) return; // already measured for this press
            if (Vector2.Distance(me.Position, moveWatchFrom) < 0.5f) return;
            stats.Sample("inputToMotionMs", (Time.realtimeSinceStartup - moveWatchStart) * 1000f);
            moveWatchStart = -1f;
        }

        void TrackLocalHero()
        {
            HeroController me = LocalPlayer.Hero;
            if (me == null) return;
            TrackInputToMotion(me);
            Vector2 p = me.Position;
            if (hasLastPos) stats.Add("localDistance", Vector2.Distance(p, lastPos));
            lastPos = p; hasLastPos = true;
            stats.Set("finalX", p.x); stats.Set("finalY", p.y);
        }

        void Finish()
        {
            finished = true;
            if (server != null)
            {
                float secs = Mathf.Max(1f, Time.realtimeSinceStartup - startTime - (float)stats.Get("joinedAtSeconds"));
                stats.Set("downKBps", server.Statistics.BytesReceived / 1024.0 / secs);
                stats.Set("upKBps", server.Statistics.BytesSent / 1024.0 / secs);
                stats.Set("rttMs", server.RoundTripTime);
            }
            HeroController me = LocalPlayer.Hero;
            if (me != null) { stats.Set("finalLevel", me.Rpg.Level); stats.Set("finalExp", me.Rpg.Exp); stats.Set("finalGold", me.Rpg.Gold); stats.Set("finalInventory", me.Rpg.Inventory.Count); }
            stats.SetText("state", State.ToString());
            stats.SetText("status", Status);
            stats.Write(statsPath);
            Debug.Log("[ZenoniaProto] net: client run finished, stats written to " + statsPath);
            Application.Quit();
        }

        void OnDestroy()
        {
            if (Active == this) Active = null;
            if (net != null) net.Stop();
            if (stats != null) stats.Dispose();
        }
    }
}
