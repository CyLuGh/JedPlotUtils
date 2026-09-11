using System.Text.Json;
using System.Text.Json.Serialization;
using LanguageExt;

namespace JedPlotUtils.Models.Json;

public sealed class DateOnlyDoubleHashMapConverter : JsonConverter<HashMap<DateOnly, double>>
{
    private const string DateFormat = "yyyy-MM-dd";

    public override HashMap<DateOnly, double> Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException("Expected start of object.");
        }

        var result = new HashMap<DateOnly, double>();

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                return result;
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException("Expected property name.");
            }

            var dateString = reader.GetString();

            if (!DateOnly.TryParseExact(dateString, DateFormat, out var date))
            {
                throw new JsonException(
                    $"Invalid date key '{dateString}'. Expected format '{DateFormat}'."
                );
            }

            reader.Read();

            var value = reader.GetDouble();

            result = result.Add(date, value);
        }

        throw new JsonException("Unexpected end of JSON.");
    }

    public override void Write(
        Utf8JsonWriter writer,
        HashMap<DateOnly, double> value,
        JsonSerializerOptions options
    )
    {
        writer.WriteStartObject();

        foreach (var (date, number) in value)
        {
            writer.WriteNumber(date.ToString(DateFormat), number);
        }

        writer.WriteEndObject();
    }
}
