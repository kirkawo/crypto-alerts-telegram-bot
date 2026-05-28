namespace CryptoAlerts.Bot.Commands;

public static class CommandParser
{
    public static ParsedCommand Parse(string? text, string? botUsername = null)
    {
        if (string.IsNullOrWhiteSpace(text) || text[0] != '/')
            return new ParsedCommand(CommandType.Unknown);

        var parts = text.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var rawCommand = parts[0].ToLowerInvariant();

        var atIndex = rawCommand.IndexOf('@');
        if (atIndex >= 0)
        {
            var mentionedBot = rawCommand[(atIndex + 1)..];
            if (botUsername is not null &&
                !string.Equals(mentionedBot, botUsername, StringComparison.OrdinalIgnoreCase))
            {
                return new ParsedCommand(CommandType.Unknown);
            }

            rawCommand = rawCommand[..atIndex];
        }

        return rawCommand switch
        {
            "/start" => new ParsedCommand(CommandType.Start),
            "/help" => new ParsedCommand(CommandType.Help),
            "/price" => new ParsedCommand(CommandType.Price, parts[1..]),
            _ => new ParsedCommand(CommandType.Unknown),
        };
    }
}
