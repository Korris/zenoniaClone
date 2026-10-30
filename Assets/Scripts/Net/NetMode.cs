namespace ZenoniaProto
{
    /// <summary>How this process runs: offline (today's single player), authoritative zone server, host (server that also renders + plays), client (proxies only).</summary>
    public enum RunMode { Offline, Server, Host, Client }

    /// <summary>
    /// Process-wide run mode and the three questions the sim asks about it. Cosmetic code (FX, audio, camera, HUD) is guarded with RendersFx so a
    /// headless server never builds views; time-bending feel (hit-stop, inventory pause) only exists offline because a shared zone cannot freeze.
    /// </summary>
    public static class NetMode
    {
        public static RunMode Current = RunMode.Offline;

        public static bool IsOffline { get { return Current == RunMode.Offline; } }
        /// <summary>Runs the real gameplay rules (offline, server, host). A pure client only mirrors the server.</summary>
        public static bool IsAuthority { get { return Current != RunMode.Client; } }
        /// <summary>Dedicated server: no cameras, sprites, UI or audio are created at all.</summary>
        public static bool IsHeadless { get { return Current == RunMode.Server; } }
        public static bool RendersFx { get { return !NoViews; } }
        /// <summary>No sprites / renderers / FX are created: a dedicated server, or any process started with -nographics (unattended bot clients).</summary>
        public static bool NoViews { get { return Current == RunMode.Server || NoGraphics; } }
        static int graphicsNull = -1;
        /// <summary>True when Unity runs without a graphics device (-nographics): shaders and materials do not exist.</summary>
        public static bool NoGraphics
        {
            get
            {
                if (graphicsNull < 0) graphicsNull = UnityEngine.SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null ? 1 : 0;
                return graphicsNull == 1;
            }
        }
        /// <summary>Runs a ZoneServer in this process (server, host): remote players connect to it.</summary>
        public static bool IsServerSide { get { return Current == RunMode.Server || Current == RunMode.Host; } }
        public static bool IsClient { get { return Current == RunMode.Client; } }
        /// <summary>Hit-stop and the inventory pause freeze the whole sim: offline only.</summary>
        public static bool UsesLocalTimeBending { get { return Current == RunMode.Offline; } }

        /// <summary>Reads `--mode offline|server|host|client` from the command line (unknown / missing = offline).</summary>
        public static RunMode ParseMode(string[] args)
        {
            for (int i = 0; i + 1 < args.Length; i++)
            {
                if (args[i] != "--mode") continue;
                switch (args[i + 1].ToLowerInvariant())
                {
                    case "server": return RunMode.Server;
                    case "host": return RunMode.Host;
                    case "client": return RunMode.Client;
                }
            }
            return RunMode.Offline;
        }
    }
}
