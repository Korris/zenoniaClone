using System;
using System.Globalization;

namespace ZenoniaProto
{
    /// <summary>
    /// Command line of the network modes (parsed once at startup):
    ///   --mode offline|server|host|client   --port N (default 7777)   --host H | --connect H:P   --name X   --class warrior|wizard|...
    ///   --max-players N (200)   --tick 30   --snap-div 2   --dev-key K   --run-seconds N   --net-bot fight|wander   --net-stats file.json
    /// </summary>
    public static class NetLaunchArgs
    {
        public static int Port = 7777, MaxPlayers = 200, TickRate = 30, SnapDiv = 2;
        public static string Host, Name, ClassName, DevKey = "", NetBot, StatsPath;
        public static float RunSeconds;
        /// <summary>Test hook (--net-test-version N): the client announces a wrong protocol version to check the server rejects it.</summary>
        public static int TestVersion;

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
            float rs;
            RunSeconds = float.TryParse(Arg(a, "--run-seconds"), NumberStyles.Float, CultureInfo.InvariantCulture, out rs) ? rs : 0f;
        }

        public static bool HasFlag(string flag) { return Array.IndexOf(Environment.GetCommandLineArgs(), flag) >= 0; }
    }
}
