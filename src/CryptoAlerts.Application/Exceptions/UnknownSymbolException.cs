namespace CryptoAlerts.Application.Exceptions;

public class UnknownSymbolException : Exception
{
    public UnknownSymbolException(string symbol)
        : base($"Unknown asset symbol: '{symbol}'.")
    {
        Symbol = symbol;
    }

    public string Symbol { get; }
}
