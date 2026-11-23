using System;
using System.Globalization;

namespace ZenoniaProto
{
    /// <summary>
    /// Command line of the network modes (parsed once at startup):
    ///   --mode offline|server|host|client   --port N (default 7777)   --host H | --connect H:P   --name X   --class warrior|wizard|...
    ///   --sim-latency MS --sim-jitter MS --sim-loss PCT --sim-seed N (client lag harness)   --no-predict   --predict-after S   --net-no-delta
    ///   --max-players N (200)   --tick 30   --snap-div 2   --dev-key K   --run-seconds N   --net-bot fight|wander|portal   --net-stats file.json   --dev-gold N --dev-items N (test servers: goods to trade)
    ///   --wanted-boss TYPE (server: that boss is the Wanted Boss every day, for tests; a warning is logged)
    ///   --wanted-respawn-sec S (server: seconds between the Wanted Boss kill and the next one, default 1800; tests)   --wanted-hp-scale F (server AND client, tests only: multiplies the Wanted Boss HP, default 1)   --wanted-render-scale F (client, tests only: draws the Wanted Bosses F times larger, default 1)
    ///   --map NAME (server: the zone new heroes begin in, default Frost Hollow)   --no-portals (server: load tests keep everybody on one zone)
    ///   --api-url URL (server: real accounts, characters load from and save to server-api, the service key comes from the environment variable ZENONIA_SERVICE_KEY; client: where to log in)
    ///   --wanted-god-bots PREFIX[,PREFIX] (server, crowd tests only: heroes whose name starts with a PREFIX take no damage; see WantedGodBotsAllowed)
    ///   --zone-id ID (server, default frost-hollow, the API's ZONE_ID)   --save-interval S (server, default 30)   --save-spool DIR (server: where a save that cannot be written is kept)   --shutdown-file F (server: F appearing = save everybody and stop)
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
        /// <summary>Server test hook (--dev-hurt PCT): every new hero but a Cleric starts at this percent of its HP; 0 = off. Never set on a public server.</summary>
        public static int DevHurtPct;
        /// <summary>Test hook (--dev-no-cooldowns, on the server and the test client): skills have no cooldown, so a recast can overlap. Never set on a public server.</summary>
        public static bool DevNoCooldowns;
        /// <summary>Server test hook (--dev-gold N): every new hero starts with this much gold; 0 = off. Never set on a public server.</summary>
        public static int DevGold;
        /// <summary>--dev-cards (test servers only): every new hero gets 3 copies of 8 cards and them as a saved duel deck, to play Card Duels without grinding drops.</summary>
        public static bool DevCards;
        /// <summary>Server test hook (--dev-duel-seed N): every PvP duel is dealt from this seed, so a test can replay a known result (a draw). 0 = random. Never set on a public server.</summary>
        public static ulong DevDuelSeed;
        /// <summary>Server test hook (--dev-items N, at most a full bag): every new hero starts with this many bag items, the first an enhanced weapon of its class; 0 = off. Never set on a public server.</summary>
        public static int DevItems;
        /// <summary>Server: --map, the zone new heroes begin in (null = the default map, Frost Hollow).</summary>
        public static string StartMap;
        /// <summary>Server test hook (--no-portals): no hero is ever moved between zones.</summary>
        public static bool NoPortals;
        /// <summary>--api-url: the server-api address. On a server this is what turns real accounts on; every --dev-* cheat is then off.</summary>
        public static string ApiUrl;
        /// <summary>--wanted-boss TYPE (server/host, e2e tests and live ops): this boss is the daily Wanted Boss every day (WantedBoss.Override). Unknown type = ignored with an error. Not set in the deploy compose.</summary>
        public static string WantedBossType;
        /// <summary>--wanted-respawn-sec S: the Wanted Boss comes back S seconds after its death (default 30 min). Test hook.</summary>
        public static float WantedRespawnSec = -1f;
        /// <summary>--wanted-hp-scale F: test hook, multiplies the Wanted Boss max HP on whoever is launched with it (both ends must agree). Production leaves it at 1.</summary>
        public static float WantedHpScale = 1f;
        /// <summary>--wanted-render-scale F: test hook, draws every Wanted Boss F times its normal size (picture only: hit box and reach unchanged). Production leaves it at 1.</summary>
        public static float WantedRenderScale = 1f;
        /// <summary>--wanted-god-bots PREFIX[,PREFIX]: crowd test hook (Tools/wanted-boss-crowd-playtest.ps1 -GodBots / -GodHero), heroes named PREFIX... take no damage (HeroController.God).
        /// Only honoured when WantedGodBotsAllowed; null = off.</summary>
        public static string WantedGodBotsPrefix;
        public static string ZoneId ="frost-hollow", SaveSpool, ShutdownFile;
        public static int SaveIntervalSeconds = 30;

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
            DevHurtPct = Math.Max(0, Math.Min(100, Int(a, "--dev-hurt", 0)));
            DevNoCooldowns = HasFlag("--dev-no-cooldowns");
            DevGold = Math.Max(0, Int(a, "--dev-gold", 0));
            DevCards = HasFlag("--dev-cards");
            ulong ds; DevDuelSeed = ulong.TryParse(Arg(a, "--dev-duel-seed"), out ds) ? ds : 0UL;
            DevItems = Math.Max(0, Math.Min(Rpg.PlayerState.InventorySize, Int(a, "--dev-items", 0)));
            float pa;
            PredictAfter = float.TryParse(Arg(a, "--predict-after"), NumberStyles.Float, CultureInfo.InvariantCulture, out pa) ? pa : 0f;
            Predict = Array.IndexOf(a, "--no-predict") < 0 && PredictAfter <= 0f;
            NoDelta = Array.IndexOf(a, "--net-no-delta") >= 0;
            StartMap = Arg(a, "--map");
            NoPortals = Array.IndexOf(a, "--no-portals") >= 0;
            ApiUrl = Arg(a, "--api-url");
            WantedBossType = Arg(a, "--wanted-boss");
            float wr, wh;
            WantedRespawnSec = float.TryParse(Arg(a, "--wanted-respawn-sec"), NumberStyles.Float, CultureInfo.InvariantCulture, out wr) && wr > 0f ? wr : -1f;
            WantedHpScale = float.TryParse(Arg(a, "--wanted-hp-scale"), NumberStyles.Float, CultureInfo.InvariantCulture, out wh) && wh > 0f ? wh : 1f;
            MonsterDef.WantedHpScale = WantedHpScale;
            float wd;
            WantedRenderScale = float.TryParse(Arg(a, "--wanted-render-scale"), NumberStyles.Float, CultureInfo.InvariantCulture, out wd) && wd > 0f ? wd : 1f;
            MonsterDef.ApplyWantedRenderScale(WantedRenderScale);
            if (WantedRespawnSec > 0f) WantedBossSpawner.RespawnMs = WantedRespawnSec * 1000f;
            WantedGodBotsPrefix = Arg(a, "--wanted-god-bots");
            ZoneId = Arg(a, "--zone-id") ?? "frost-hollow";
            SaveSpool = Arg(a, "--save-spool");
            ShutdownFile = Arg(a, "--shutdown-file");
            SaveIntervalSeconds = Math.Max(1, Math.Min(60, Int(a, "--save-interval", 30))); // the session lock needs a heartbeat well inside its 90 s
            SimLatencyMs = Math.Max(0, Math.Min(2000, Int(a, "--sim-latency", 0)));
            SimJitterMs = Math.Max(0, Math.Min(1000, Int(a, "--sim-jitter", 0)));
            SimLossPct = Math.Max(0, Math.Min(90, Int(a, "--sim-loss", 0)));
            SimSeed = Int(a, "--sim-seed", 1);
            float rs;
            RunSeconds = float.TryParse(Arg(a, "--run-seconds"), NumberStyles.Float, CultureInfo.InvariantCulture, out rs) ? rs : 0f;
        }

        /// <summary>
        /// The god-bot hook can never reach a real server: a test build (not ZENONIA_RELEASE), a test Wanted Boss pinned with --wanted-boss, a prefix of 2+ characters,
        /// and either no API (dev logins) or an API on this machine (loopback). A public zone talks to a remote server-api, so it always says no.
        /// </summary>
        public static bool WantedGodBotsAllowed
        {
            get
            {
                if (ReleaseBuild.IsRelease || string.IsNullOrEmpty(WantedBossType) || string.IsNullOrEmpty(WantedGodBotsPrefix)) return false;
                foreach (string p in WantedGodBotsPrefix.Split(',')) if (p.Trim().Length < 2) return false;
                if (string.IsNullOrEmpty(ApiUrl)) return true;
                Uri u;
                return Uri.TryCreate(ApiUrl, UriKind.Absolute, out u) && u.IsLoopback;
            }
        }

        /// <summary>True for a hero the god-bot hook covers (name starts with one of the prefixes, case-insensitive); always false when the hook is not allowed.</summary>
        public static bool IsWantedGodBot(string heroName)
        {
            if (heroName == null || !WantedGodBotsAllowed) return false;
            foreach (string p in WantedGodBotsPrefix.Split(',')) if (heroName.StartsWith(p.Trim(), StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        public static bool HasFlag(string flag) { return Array.IndexOf(Environment.GetCommandLineArgs(), flag) >= 0; }
    }
}
