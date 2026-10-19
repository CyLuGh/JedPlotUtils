using System.Text.Json;
using System.Text.Json.Serialization;
using JedPlotUtils.Palette;
using LanguageExt;

namespace JedPlotUtils.Models.Json;

public sealed class OptionColorConverter : JsonConverter<Option<Color>>
{
    public override Option<Color> Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return Option<Color>.None;
        }
        return Color.Parse(reader.GetString());
    }

    public override void Write(
        Utf8JsonWriter writer,
        Option<Color> value,
        JsonSerializerOptions options
    )
    {
        value.Match(c => writer.WriteStringValue(c.ToString()), writer.WriteNullValue);
    }
}
