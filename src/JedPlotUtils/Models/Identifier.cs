namespace JedPlotUtils.Models;

public readonly record struct Identifier
{
    private readonly string _id;
    private readonly Guid _guid;

    public Identifier(string id)
        : this(id, Guid.CreateVersion7()) { }

    public Identifier(string id, Guid guid)
    {
        _id = id;
        _guid = guid;
    }

    public void Deconstruct(out string id, out Guid guid)
    {
        id = _id;
        guid = _guid;
    }

    public static implicit operator Identifier(string id) => new(id);

    public static explicit operator string(Identifier identifier) => identifier._id;

    public static explicit operator Guid(Identifier identifier) => identifier._guid;

    public static explicit operator (string, Guid)(Identifier identifier) =>
        (identifier._id, identifier._guid);
}
