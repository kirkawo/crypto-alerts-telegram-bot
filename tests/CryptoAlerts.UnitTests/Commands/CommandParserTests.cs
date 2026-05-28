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
