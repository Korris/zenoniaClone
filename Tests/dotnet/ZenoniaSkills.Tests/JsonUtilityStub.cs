using System.Text.Json;

// Lets the real SaveSerializer run in these tests: the same public-field JSON as Unity's JsonUtility for the save types.
namespace UnityEngine
{
    public static class JsonUtility
    {
        static readonly JsonSerializerOptions Options = new JsonSerializerOptions { IncludeFields = true };
        public static string ToJson(object o, bool pretty = false) { return JsonSerializer.Serialize(o, o.GetType(), Options); }
        public static T FromJson<T>(string json) { return JsonSerializer.Deserialize<T>(json, Options); }
    }
}
