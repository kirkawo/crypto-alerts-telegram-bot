using CryptoAlerts.Bot.Commands;

namespace CryptoAlerts.UnitTests.Commands;

public class CommandParserTests
{
    [Fact]
    public void Parse_StartCommand_ReturnsStartType()
    {
        var result = CommandParser.Parse("/start");

        Assert.Equal(CommandType.Start, result.Type);
        Assert.Empty(result.Arguments);
    }

    [Fact]
    public void Parse_HelpCommand_ReturnsHelpType()
    {
        var result = CommandParser.Parse("/help");

        Assert.Equal(CommandType.Help, result.Type);
        Assert.Empty(result.Arguments);
    }

    [Theory]
    [InlineData("/price")]
    [InlineData("/PRICE")]
    public void Parse_PriceCommand_ReturnsPriceType(string text)
    {
        var result = CommandParser.Parse(text);

        Assert.Equal(CommandType.Price, result.Type);
    }

    [Fact]
    public void Parse_PriceCommandWithSymbol_SetsArguments()
    {
        var result = CommandParser.Parse("/price BTC");

        Assert.Equal(CommandType.Price, result.Type);
        Assert.Single(result.Arguments);
        Assert.Equal("BTC", result.Arguments[0]);
    }

    [Fact]
    public void Parse_PriceCommandWithSymbolLowercase_SetsArguments()
    {
        var result = CommandParser.Parse("/price eth");

        Assert.Equal(CommandType.Price, result.Type);
        Assert.Equal("eth", result.Arguments[0]);
    }

    [Fact]
    public void Parse_PriceCommandWithExtraSpaces_TrimsArguments()
    {
        var result = CommandParser.Parse("/price   SOL   ");

        Assert.Equal(CommandType.Price, result.Type);
        Assert.Single(result.Arguments);
        Assert.Equal("SOL", result.Arguments[0]);
    }

    [Fact]
    public void Parse_PriceCommand_MultipleSpacesBetweenCommandAndSymbol()
    {
        var result = CommandParser.Parse("/price    BTC");

        Assert.Equal(CommandType.Price, result.Type);
        Assert.Single(result.Arguments);
        Assert.Equal("BTC", result.Arguments[0]);
    }

    [Fact]
    public void Parse_PriceCommand_UppercaseCommandLowercaseSymbol()
    {
        var result = CommandParser.Parse("/PRICE btc");

        Assert.Equal(CommandType.Price, result.Type);
        Assert.Single(result.Arguments);
        Assert.Equal("btc", result.Arguments[0]);
    }

    [Fact]
    public void Parse_UnknownCommand_ReturnsUnknownType()
    {
        var result = CommandParser.Parse("/unknown");

        Assert.Equal(CommandType.Unknown, result.Type);
        Assert.Empty(result.Arguments);
    }

    [Fact]
    public void Parse_PlainText_ReturnsUnknownType()
    {
        var result = CommandParser.Parse("hello world");

        Assert.Equal(CommandType.Unknown, result.Type);
    }

    [Fact]
    public void Parse_StartCommand_WithBotName_ReturnsStartType()
    {
        var result = CommandParser.Parse("/start@MyBot");

        Assert.Equal(CommandType.Start, result.Type);
        Assert.Empty(result.Arguments);
    }

    [Fact]
    public void Parse_HelpCommand_WithBotName_ReturnsHelpType()
    {
        var result = CommandParser.Parse("/help@MyBot");

        Assert.Equal(CommandType.Help, result.Type);
        Assert.Empty(result.Arguments);
    }

    [Fact]
    public void Parse_PriceCommand_WithBotName_ReturnsPriceType()
    {
        var result = CommandParser.Parse("/price@MyBot BTC");

        Assert.Equal(CommandType.Price, result.Type);
        Assert.Single(result.Arguments);
        Assert.Equal("BTC", result.Arguments[0]);
    }

    [Fact]
    public void Parse_PriceCommand_WithBotNameAndExtraSpaces_TrimsArguments()
    {
        var result = CommandParser.Parse("/price@MyBot    BTC   ");

        Assert.Equal(CommandType.Price, result.Type);
        Assert.Single(result.Arguments);
        Assert.Equal("BTC", result.Arguments[0]);
    }

    [Fact]
    public void Parse_SetAlertCommand_WithSymbolAndPrice_ReturnsSetAlertType()
    {
        var result = CommandParser.Parse("/set_alert BTC 70000");

        Assert.Equal(CommandType.SetAlert, result.Type);
        Assert.Equal(2, result.Arguments.Count);
        Assert.Equal("BTC", result.Arguments[0]);
        Assert.Equal("70000", result.Arguments[1]);
    }

    [Fact]
    public void Parse_SetAlertCommand_WithoutArguments_ReturnsSetAlertType()
    {
        var result = CommandParser.Parse("/set_alert");

        Assert.Equal(CommandType.SetAlert, result.Type);
        Assert.Empty(result.Arguments);
    }

    [Fact]
    public void Parse_ListAlertsCommand_ReturnsListAlertsType()
    {
        var result = CommandParser.Parse("/list_alerts");

        Assert.Equal(CommandType.ListAlerts, result.Type);
        Assert.Empty(result.Arguments);
    }

    [Fact]
    public void Parse_RemoveAlertCommand_WithId_ReturnsRemoveAlertType()
    {
        var result = CommandParser.Parse("/remove_alert a1b2c3d4-e5f6-7890-abcd-ef1234567890");

        Assert.Equal(CommandType.RemoveAlert, result.Type);
        Assert.Single(result.Arguments);
    }

    [Fact]
    public void Parse_RemoveAlertCommand_WithoutId_ReturnsRemoveAlertType()
    {
        var result = CommandParser.Parse("/remove_alert");

        Assert.Equal(CommandType.RemoveAlert, result.Type);
        Assert.Empty(result.Arguments);
    }

    [Fact]
    public void Parse_WithBotName_MatchingBot_AcceptsCommand()
    {
        var result = CommandParser.Parse("/start@MyBot", "MyBot");

        Assert.Equal(CommandType.Start, result.Type);
    }

    [Fact]
    public void Parse_WithBotName_DifferentBot_ReturnsUnknown()
    {
        var result = CommandParser.Parse("/start@OtherBot", "MyBot");

        Assert.Equal(CommandType.Unknown, result.Type);
    }

    [Fact]
    public void Parse_PriceWithBotName_MatchingBot_AcceptsCommand()
    {
        var result = CommandParser.Parse("/price@MyBot BTC", "MyBot");

        Assert.Equal(CommandType.Price, result.Type);
        Assert.Equal("BTC", result.Arguments[0]);
    }

    [Fact]
    public void Parse_PriceWithBotName_DifferentBot_ReturnsUnknown()
    {
        var result = CommandParser.Parse("/price@OtherBot BTC", "MyBot");

        Assert.Equal(CommandType.Unknown, result.Type);
        Assert.Empty(result.Arguments);
    }

    [Fact]
    public void Parse_WithBotName_CaseInsensitiveMatch_AcceptsCommand()
    {
        var result = CommandParser.Parse("/start@mybot", "MyBot");

        Assert.Equal(CommandType.Start, result.Type);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Parse_InvalidInput_ReturnsUnknownType(string? text)
    {
        var result = CommandParser.Parse(text);

        Assert.Equal(CommandType.Unknown, result.Type);
    }
}
