namespace CryptoAlerts.Bot.Commands;

public static class CommandParser
{
    public static ParsedCommand Parse(string? text)
    {
        if (string.IsNullOrWhiteSpace(text) || text[0] != '/')
            return new ParsedCommand(CommandType.Unknown);

        var parts = text.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var command = parts[0].ToLowerInvariant();

        return command switch
        {
            "/start" => new ParsedCommand(CommandType.Start),
            "/help" => new ParsedCommand(CommandType.Help),
            "/price" => new ParsedCommand(CommandType.Price, parts[1..]),
            _ => new ParsedCommand(CommandType.Unknown),
        };
    }
}
