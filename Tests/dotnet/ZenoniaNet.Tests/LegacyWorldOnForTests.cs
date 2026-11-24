using System.Runtime.CompilerServices;
using ZenoniaProto.Rpg;

/// <summary>Taurus V4: the pre-Taurus maps / quests / Wanted Boss are off in the live game, but these tests still cover their rules, so the whole test assembly runs with them on.</summary>
static class LegacyWorldOnForTests
{
    [ModuleInitializer]
    internal static void Init() { WorldFlags.LegacyEnabled = true; }
}
