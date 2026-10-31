using System;
using System.Globalization;

namespace ZenoniaProto
{
    /// <summary>
    /// Command line of the network modes (parsed once at startup):
    ///   --mode offline|server|host|client   --port N (default 7777)   --host H | --connect H:P   --name X   --class warrior|wizard|...
    ///   --sim-latency MS --sim-jitter MS --sim-loss PCT --sim-seed N (client lag harness)   --no-predict   --predict-after S   --net-no-delta
    ///   --max-players N (200)   --tick 30   --snap-div 2   --dev-key K   --run-seconds N   --net-bot fight|wander|portal   --net-stats file.json
    ///   --map NAME (server: the zone new heroes begin in, default Frost Hollow)   --no-portals (server: load tests keep everybody on one zone)
    /// </summary>
    public static class NetLaunchArgs
    {
        public static int Port = 7777, MaxPlayers = 200, TickRate = 30, SnapDiv = 2;
        public static string Host, Name, ClassName, DevKey = "", NetBot, StatsPath;
        public static float RunSeconds;
        /// <summary>Client prediction of the local hero (off with --no-predict, for before / after comparisons).</summary>
        public static bool Predict = true;
        /// <summary>--predict-after S: join without prediction and switch it on S seconds later (A / B demo in one recording).</summary>
        public static float PredictAfter;
        /// <summary>Server sends full snapshots only (--net-no-delta, for bandwidth comparisons).</summary>
        public static bool NoDelta;
        /// <summary>Lag harness (clients): one-way latency, +- jitter, packet loss in percent, each direction; --sim-seed picks the random stream.</summary>
        public static int SimLatencyMs, SimJitterMs, SimLossPct, SimSeed = 1;
        /// <summary>Test hook (--net-test-version N): the client announces a wrong protocol version to check the server rejects it.</summary>
        public static int TestVersion;
        /// <summary>Server test hook (--dev-level N): new heroes start at this level (skill points to spend); 0 = off. Never set on a public server.</summary>
        public static int DevLevel;
        /// <summary>Server: --map, the zone new heroes begin in (null = the default map, Frost Hollow).</summary>
        public static string StartMap;
        /// <summary>Server test hook (--no-portals): no hero is ever moved between zones.</summary>
        public static bool NoPortals;

        public static string Arg(string[] a, string key)
        {
            int i = Array.IndexOf(a, key);
            return i >= 0 && i + 1 < a.Length ? a[i + 1] : null;
        }

        static int Int(string[] a, string key, int fallback)
        {
            int v;
            return int.TryParse(Arg(a, key), NumberStyles.Integer, CultureInfo.InvariantCulture, out v) ? v : fallback;
        }

        public static void Parse(string[] a)
        {
            Port = Math.Max(1, Math.Min(65535, Int(a, "--port", 7777)));
            MaxPlayers = Math.Max(1, Int(a, "--max-players", 200));
            TickRate = Math.Max(10, Math.Min(60, Int(a, "--tick", 30)));
            SnapDiv = Math.Max(1, Math.Min(6, Int(a, "--snap-div", 2)));
            Host = Arg(a, "--host");
            string connect = Arg(a, "--connect");
            if (connect != null)
            {
                int c = connect.LastIndexOf(':');
                if (c > 0) { Host = connect.Substring(0, c); int p; if (int.TryParse(connect.Substring(c + 1), out p)) Port = p; }
                else Host = connect;
            }
            Name = Arg(a, "--name");
            ClassName = Arg(a, "--class");
            DevKey = Arg(a, "--dev-key") ?? "";
            NetBot = Arg(a, "--net-bot");
            StatsPath = Arg(a, "--net-stats");
            TestVersion = Int(a, "--net-test-version", 0);
            DevLevel = Math.Max(0, Math.Min(Rpg.Leveling.MaxLevel, Int(a, "--dev-level", 0)));
            float pa;
            PredictAfter = float.TryParse(Arg(a, "--predict-after"), NumberStyles.Float, CultureInfo.InvariantCulture, out pa) ? pa : 0f;
            Predict = Array.IndexOf(a, "--no-predict") < 0 && PredictAfter <= 0f;
            NoDelta = Array.IndexOf(a, "--net-no-delta") >= 0;
            StartMap = Arg(a, "--map");
            NoPortals = Array.IndexOf(a, "--no-portals") >= 0;
            SimLatencyMs = Math.Max(0, Math.Min(2000, Int(a, "--sim-latency", 0)));
            SimJitterMs = Math.Max(0, Math.Min(1000, Int(a, "--sim-jitter", 0)));
            SimLossPct = Math.Max(0, Math.Min(90, Int(a, "--sim-loss", 0)));
            SimSeed = Int(a, "--sim-seed", 1);
            float rs;
            RunSeconds = float.TryParse(Arg(a, "--run-seconds"), NumberStyles.Float, CultureInfo.InvariantCulture, out rs) ? rs : 0f;
        }

        public static bool HasFlag(string flag) { return Array.IndexOf(Environment.GetCommandLineArgs(), flag) >= 0; }
    }
}
