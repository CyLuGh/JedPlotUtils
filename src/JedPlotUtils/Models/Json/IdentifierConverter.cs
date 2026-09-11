using System.Text.Json;
using System.Text.Json.Serialization;

namespace JedPlotUtils.Models.Json;

public sealed class IdentifierConverter : JsonConverter<Identifier>
{
    public override Identifier Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        using var doc = JsonDocument.ParseValue(ref reader);

        var root = doc.RootElement;

        var id = root.GetProperty("id").GetString()!;
        var guid = root.GetProperty("guid").GetGuid();

        var level = root.TryGetProperty("level", out var levelProp)
            ? Enum.Parse<Level>(levelProp.GetString()!)
            : Level.Primary;

        return new Identifier(id, guid, level);
    }

    public override void Write(
        Utf8JsonWriter writer,
        Identifier value,
        JsonSerializerOptions options
    )
    {
        var (id, guid, level) = value;

        writer.WriteStartObject();

        writer.WriteString("id", id);
        writer.WriteString("guid", guid);

        writer.WriteString("level", level.ToString());

        writer.WriteEndObject();
    }
}
