using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using Xunit;
using ZenoniaProto.Rpg;

// server-api validates saved card ids against its own copy (server-api/src/rpg/card-ids.json). This test keeps that copy equal to CardTable.
// A mismatch fails with the command that rewrites the file from the C# table.
public class CardIdsExportTests
{
    const string WriteVariable = "ZENONIA_WRITE_CARD_IDS";

    static string JsonPath()
    {
        string dir = AppContext.BaseDirectory;
        while (dir != null && !Directory.Exists(Path.Combine(dir, "server-api"))) dir = Path.GetDirectoryName(dir);
        Assert.NotNull(dir);
        return Path.Combine(dir, "server-api", "src", "rpg", "card-ids.json");
    }

    static string Expected()
    {
        var arr = new JsonArray();
        foreach (CardDef c in CardTable.All) arr.Add(new JsonObject { ["id"] = c.Id, ["rarity"] = c.Rarity.ToString(), ["isBoss"] = c.IsBoss });
        return arr.ToJsonString(new JsonSerializerOptions { WriteIndented = true }).Replace("\r\n", "\n") + "\n";
    }

    [Fact]
    public void ServerApiCardIdsMatchTheCardTable()
    {
        string path = JsonPath();
        string expected = Expected();
        if (Environment.GetEnvironmentVariable(WriteVariable) == "1") File.WriteAllText(path, expected);
        Assert.True(File.Exists(path), "missing " + path + ": run  " + WriteVariable + "=1 dotnet test Tests/dotnet/ZenoniaSkills.Tests --filter CardIdsExportTests");
        string actual = File.ReadAllText(path).Replace("\r\n", "\n");
        Assert.True(actual == expected, "server-api/src/rpg/card-ids.json differs from CardTable. Rewrite it with:  " + WriteVariable + "=1 dotnet test Tests/dotnet/ZenoniaSkills.Tests --filter CardIdsExportTests");
    }
}
