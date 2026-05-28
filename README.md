# Crypto Alerts Telegram Bot

A .NET 8 Telegram bot for cryptocurrency price tracking and alerts. Currently supports live price lookups via the CoinGecko API, with a layered architecture ready for alert management.

## Features

- **Telegram long polling bot** — `/start`, `/help`, and `/price <symbol>` commands
- **CoinGecko integration** — resolves common symbols (BTC, ETH, SOL, BNB, XRP, ADA, DOGE) and fetches current prices
- **SQLite persistence** — users and alerts stored via EF Core
- **Alert foundation** — domain entities, application services, and persistence for price alerts already implemented

## Tech Stack

- .NET 8
- Telegram.Bot
- Entity Framework Core + SQLite
- CoinGecko API
- xUnit + Moq

## Project Structure

```
src/
├── CryptoAlerts.Bot           — Telegram long polling, command parsing, DI wiring
├── CryptoAlerts.Application   — Price queries, alert management, application interfaces
├── CryptoAlerts.Domain        — Entities (PriceAlert, TrackedUser), enums
└── CryptoAlerts.Infrastructure— CoinGecko HTTP client, EF Core persistence, DI registration
tests/
└── CryptoAlerts.UnitTests     — Unit tests for all layers
```

## How to Run

```bash
git clone <repo-url>
cd crypto-alerts-telegram-bot
```

Set your bot credentials in `src/CryptoAlerts.Bot/appsettings.json`:

```json
"Telegram": {
  "BotToken": "<your-bot-token>",
  "BotUsername": "<your-bot-username>"
}
```

Or use environment variables / user secrets (recommended for tokens).

```bash
dotnet build
dotnet run --project src/CryptoAlerts.Bot
```

## Available Commands

| Command | Description |
|---------|-------------|
| `/start` | Welcome message |
| `/help` | List available commands |
| `/price BTC` | Current price of a symbol |

`/price` supports: BTC, ETH, SOL, BNB, XRP, ADA, DOGE.

Commands addressed to the bot by name (`/price@MyBot BTC`) are accepted; commands for other bots are silently ignored.

## Planned

- Alert creation and management commands (`/set_alert`, `/list_alerts`, `/remove_alert`)
- Background worker for checking alert conditions
- CI pipeline
