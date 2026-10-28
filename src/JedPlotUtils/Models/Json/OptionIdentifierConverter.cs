using System.Text.Json;
using System.Text.Json.Serialization;
using LanguageExt;

namespace JedPlotUtils.Models.Json;

public sealed class OptionIdentifierConverter : JsonConverter<Option<Identifier>>
{
    private static IdentifierConverter _identifierConverter = new();

    public override Option<Identifier> Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return Option<Identifier>.None;
        }

        return _identifierConverter.Read(ref reader, typeToConvert, options);
    }

    public override void Write(
        Utf8JsonWriter writer,
        Option<Identifier> value,
        JsonSerializerOptions options
    )
    {
        value.Match(
            identifier => _identifierConverter.Write(writer, identifier, options),
            writer.WriteNullValue
        );
    }
}
