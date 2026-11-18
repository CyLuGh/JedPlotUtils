namespace JedPlotUtils.Models;

public readonly record struct Message(string Title, string Content)
{
    public Message(string content)
        : this(string.Empty, content) { }

    public Message(Exception exception)
        : this(exception.Message) { }

    public Message(string title, Exception exception)
        : this(title, exception.Message) { }

    public static implicit operator Message(string content) => new(content);

    public static implicit operator Message(Exception exception) => new(exception);
}
