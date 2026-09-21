using System.Text.Json;
using System.Text.Json.Serialization;
using LanguageExt;

namespace JedPlotUtils.Models.Json;

public sealed class OptionStringConverter : JsonConverter<Option<string>>
{
    public override Option<string> Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return Option<string>.None;
        }
        return reader.GetString();
    }

    public override void Write(
        Utf8JsonWriter writer,
        Option<string> value,
        JsonSerializerOptions options
    )
    {
        value.Match(writer.WriteStringValue, writer.WriteNullValue);
    }
}