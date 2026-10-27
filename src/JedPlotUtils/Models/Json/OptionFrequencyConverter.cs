using System.Text.Json;
using System.Text.Json.Serialization;
using JDPlus.WS.Models;
using LanguageExt;

namespace JedPlotUtils.Models.Json;

public sealed class OptionFrequencyConverter : JsonConverter<Option<Frequency>>
{
    public override Option<Frequency> Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return Option<Frequency>.None;
        }
        return (Frequency)reader.GetInt32();
    }

    public override void Write(
        Utf8JsonWriter writer,
        Option<Frequency> value,
        JsonSerializerOptions options
    )
    {
        value.Match(f => writer.WriteNumberValue((int)f), writer.WriteNullValue);
    }
}
