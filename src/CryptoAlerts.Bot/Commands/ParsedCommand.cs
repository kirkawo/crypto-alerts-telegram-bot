namespace CryptoAlerts.Bot.Commands;

public class ParsedCommand
{
    public CommandType Type { get; }
    public IReadOnlyList<string> Arguments { get; }

    public ParsedCommand(CommandType type, IReadOnlyList<string>? arguments = null)
    {
        Type = type;
        Arguments = arguments ?? Array.Empty<string>();
    }
}
