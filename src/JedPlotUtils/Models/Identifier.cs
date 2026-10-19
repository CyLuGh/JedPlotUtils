namespace JedPlotUtils.Models;

public readonly record struct Identifier : IComparable<Identifier>
{
    private readonly string _id;
    private readonly Guid _guid;
    public Level Level { get; }

    public Identifier(string id, Level level = Level.Primary)
        : this(id, Guid.CreateVersion7(), level) { }

    public Identifier(string id, Guid guid, Level level = Level.Primary)
    {
        _id = id;
        _guid = guid;
        Level = level;
    }

    public void Deconstruct(out string id, out Guid guid, out Level level)
    {
        id = _id;
        guid = _guid;
        level = Level;
    }

    public override string ToString() => $"{_id} ({_guid})";

    public static implicit operator Identifier(string id) => new(id);

    public static explicit operator string(Identifier identifier) => identifier._id;

    public static explicit operator Guid(Identifier identifier) => identifier._guid;

    public static explicit operator (string, Guid)(Identifier identifier) =>
        (identifier._id, identifier._guid);

    public int CompareTo(Identifier other)
    {
        var guidComparison = _guid.CompareTo(other._guid);
        if (guidComparison != 0)
            return guidComparison;
        return Level.CompareTo(other.Level);
    }
}
