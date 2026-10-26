using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace HashCore.Laser.Logic.Util;

public sealed class CompatIntListConverter : JsonConverter<List<int>>
{
    private const int MinGlobalId = 1000000;

    public override List<int> ReadJson(JsonReader reader, Type objectType, List<int>? existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Null) return new List<int>();

        var token = JToken.Load(reader);

        if (token.Type == JTokenType.Array)
            return token.ToObject<List<int>>(serializer) ?? new List<int>();

        if (token.Type == JTokenType.Object)
            return ReadLegacyObject((JObject)token);

        if (token.Type == JTokenType.Integer)
            return new List<int> { token.Value<int>() };

        return new List<int>();
    }

    private static List<int> ReadLegacyObject(JObject json)
    {
        var values = new List<int>();
        var keys = new List<int>();

        foreach (JProperty property in json.Properties())
        {
            if (property.Value.Type == JTokenType.Integer) values.Add(property.Value.Value<int>());
            if (int.TryParse(property.Name, out int key)) keys.Add(key);
        }

        List<int> result = values.Any(value => value >= MinGlobalId)
            ? values.Where(value => value >= MinGlobalId).ToList()
            : keys.Any(key => key >= MinGlobalId)
                ? keys.Where(key => key >= MinGlobalId).ToList()
                : values.Count > 0 ? values : keys;

        return result.Distinct().ToList();
    }

    public override void WriteJson(JsonWriter writer, List<int>? value, JsonSerializer serializer) => serializer.Serialize(writer, value ?? new List<int>());
}
