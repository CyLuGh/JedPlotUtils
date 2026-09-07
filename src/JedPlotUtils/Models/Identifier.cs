namespace JedPlotUtils.Models;

public readonly record struct Identifier
{
    private readonly string _id;
    private readonly Guid _guid;
    public bool IsDerived { get; }

    public Identifier(string id, bool isDerived = false)
        : this(id, Guid.CreateVersion7(), isDerived) { }

    public Identifier(string id, Guid guid, bool isDerived = false)
    {
        _id = id;
        _guid = guid;
        IsDerived = isDerived;
    }

    public void Deconstruct(out string id, out Guid guid, out bool isDerived)
    {
        id = _id;
        guid = _guid;
        isDerived = IsDerived;
    }

    public static Identifier CreateDerivedIdentifier(string id = "") =>
        new(id, Guid.CreateVersion7(), true);

    public override string ToString() => $"{_id} ({_guid})";

    public static implicit operator Identifier(string id) => new(id);

    public static explicit operator string(Identifier identifier) => identifier._id;

    public static explicit operator Guid(Identifier identifier) => identifier._guid;

    public static explicit operator (string, Guid)(Identifier identifier) =>
        (identifier._id, identifier._guid);
}
