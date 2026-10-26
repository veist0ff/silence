using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace HashCore.Laser.Logic.Util;

public sealed class CompatIntConverter : JsonConverter<int>
{
    public override int ReadJson(JsonReader reader, Type objectType, int existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Null) return 0;

        var token = JToken.Load(reader);

        switch (token.Type)
        {
            case JTokenType.Integer:
            case JTokenType.Float:
                return token.Value<int>();

            case JTokenType.Boolean:
                return token.Value<bool>() ? 1 : 0;

            case JTokenType.Date:
                return ElapsedSeconds(token.Value<DateTime>());

            case JTokenType.String:
                string text = token.Value<string>() ?? string.Empty;

                if (int.TryParse(text, out int number)) return number;

                return DateTime.TryParse(text, null, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out DateTime moment)
                    ? ElapsedSeconds(moment)
                    : 0;

            default:
                return 0;
        }
    }

    private static int ElapsedSeconds(DateTime moment)
    {
        double seconds = (DateTime.UtcNow - moment.ToUniversalTime()).TotalSeconds;

        if (seconds <= 0) return 0;
        return seconds >= int.MaxValue ? int.MaxValue : (int)seconds;
    }

    public override void WriteJson(JsonWriter writer, int value, JsonSerializer serializer) => writer.WriteValue(value);
}
