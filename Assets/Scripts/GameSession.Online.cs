using System;
using UnityEngine;
using ZenoniaProto.Rpg;

namespace ZenoniaProto
{
    /// <summary>Switching this process between offline play, hosting a zone for others (dev) and joining a server.</summary>
    public partial class GameSession
    {
        public bool IsOnline { get { return !NetMode.IsOffline; } }

        /// <summary>Command-line start: `--mode host` opens the zone server, `--mode client` creates the client (and joins at once when --host / --connect is given).</summary>
        public void StartFromCommandLine()
        {
            if (NetMode.Current == RunMode.Host) StartHostServer(NetLaunchArgs.Name);
            else if (NetMode.Current == RunMode.Client)
            {
                NetClient.Begin(root.gameObject);
                if (NetLaunchArgs.Host != null) JoinServer(NetLaunchArgs.Host, NetLaunchArgs.Port, NetLaunchArgs.Name, ClassIndex(NetLaunchArgs.ClassName), NetLaunchArgs.DevKey);
                else ConnectScreenGui.Open();
            }
        }

        /// <summary>Connect screen / command line: turn this offline process into a client and join `host:port`.</summary>
        public void JoinServer(string host, int port, string name, int heroClass, string devKey)
        {
            EnterOnlineWorld(RunMode.Client, name);
            NetClient client = NetClient.Active != null ? NetClient.Active : NetClient.Begin(root.gameObject);
            client.Connect(host, port, DevLoginStub.SanitizeName(name), heroClass, devKey);
        }

        /// <summary>Connect screen: keep the local zone and let others join it (dev mode). The local hero is picked on the class select afterwards.</summary>
        public void HostServer(string name)
        {
            EnterOnlineWorld(RunMode.Host, name);
            StartHostServer(name);
        }

        void StartHostServer(string name)
        {
            localName = DevLoginStub.SanitizeName(string.IsNullOrEmpty(name) ? "Host" : name);
            SaveStore.Enabled = false;
            if (ZoneServer.Begin(root.gameObject) == null) { Debug.LogWarning("[ZenoniaProto] host: could not open the server port, playing alone"); }
        }

        /// <summary>The server went away: clear the mirrored world and show the connect screen with the reason.</summary>
        public void OnConnectionLost()
        {
            WipeWorld();
            ConnectScreenGui.Open();
        }

        /// <summary>Leaves online play: stops the server / client, clears the shared world and restores the normal offline game.</summary>
        public void ExitOnline()
        {
            if (NetMode.IsOffline) return;
            ZoneServer server = root.GetComponent<ZoneServer>();
            if (server != null) Destroy(server);
            NetClient client = root.GetComponent<NetClient>();
            if (client != null) { client.Disconnect(); Destroy(client); }
            WipeWorld();
            NetMode.Current = RunMode.Offline;
            SaveStore.Enabled = true;
            ClassSelectGui.ResetState();
            MonsterSpawner.SpawnAll(root);
        }

        /// <summary>Switches an offline process into host / client: no saves, and the offline monsters / loot / hero are cleared (host respawns the zone, a client gets the server's).</summary>
        void EnterOnlineWorld(RunMode mode, string name)
        {
            if (!NetMode.IsOffline && NetMode.Current != mode) ExitOnline();
            bool wasOffline = NetMode.IsOffline;
            NetMode.Current = mode;
            SaveStore.Enabled = false;
            localName = DevLoginStub.SanitizeName(name);
            if (!wasOffline) return;
            WipeWorld();
            if (mode == RunMode.Host) MonsterSpawner.SpawnAll(root);
        }

        /// <summary>Destroys the hero, every monster, drop and the spawner (the Zone object itself stays).</summary>
        void WipeWorld()
        {
            if (hero != null) { hero.Retire(); Destroy(heroGo); hero = null; heroGo = null; }
            if (follow != null) follow.Target = null;
            Zone z = Zone.Main;
            if (z == null) return;
            for (int i = z.Monsters.Count - 1; i >= 0; i--) if (z.Monsters[i] != null) Destroy(z.Monsters[i].gameObject);
            for (int i = z.Loot.Count - 1; i >= 0; i--) if (z.Loot[i] != null) Destroy(z.Loot[i].gameObject);
            for (int i = z.Heroes.All.Count - 1; i >= 0; i--)
            {
                HeroController h = z.Heroes.All[i];
                if (h == null) continue;
                h.Retire();
                Destroy(h.gameObject);
            }
            if (z.Spawner != null) Destroy(z.Spawner.gameObject);
            z.Spawner = null; z.Boss = null; z.Kills = 0;
            z.Monsters.Clear(); z.Loot.Clear();
            z.BossTimer.Cancel();
        }

        public static int ClassIndex(string name)
        {
            if (!string.IsNullOrEmpty(name))
                for (int i = 0; i < HeroClassDef.All.Length; i++)
                    if (string.Equals(HeroClassDef.All[i].Name, name, StringComparison.OrdinalIgnoreCase)) return i;
            return 0;
        }
    }
}
