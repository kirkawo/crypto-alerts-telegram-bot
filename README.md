# Crypto Alerts Telegram Bot

[![CI](https://github.com/kirkawo/crypto-alerts-telegram-bot/actions/workflows/ci.yml/badge.svg)](https://github.com/kirkawo/crypto-alerts-telegram-bot/actions/workflows/ci.yml)

A .NET 8 Telegram bot for cryptocurrency price lookup and automated price alerts via the CoinGecko API.

## Current Features

- **Telegram long polling bot** — `/start`, `/help`, `/price`, and alert management commands
- **Price lookup** — `/price <symbol>` fetches live prices from CoinGecko (supports BTC, ETH, SOL, BNB, XRP, ADA, DOGE)
- **Alert management** — create (`/set_alert`), list (`/list_alerts`), and cancel (`/remove_alert`) price alerts
- **Background alert checking** — polls active alerts every 60 seconds and sends a Telegram notification when the target price is met
- **SQLite persistence** — users and alerts stored via EF Core
- **GitHub Actions CI** — automatic restore, build, and test on push/PR to main and develop

## Tech Stack

- .NET 8
- Telegram.Bot
- Entity Framework Core + SQLite
- CoinGecko API
- xUnit + Moq
- GitHub Actions

## Project Structure

```
src/
├── CryptoAlerts.Bot           — Telegram long polling, command parsing, background worker, DI wiring
├── CryptoAlerts.Application   — Price queries, alert management, alert processing, application interfaces
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

The alert checker runs automatically every 60 seconds. The polling interval is configurable via `AlertCheckWorker:PollingIntervalSeconds` in `appsettings.json`.

## Available Commands

| Command | Description |
|---------|-------------|
| `/start` | Welcome message |
| `/help` | List available commands |
| `/price BTC` | Current price of a symbol |
| `/set_alert BTC 50000` | Create alert when BTC reaches 50000 USD |
| `/list_alerts` | List your active alerts |
| `/remove_alert <alertId>` | Cancel a specific alert |

`/price` supports: BTC, ETH, SOL, BNB, XRP, ADA, DOGE.

Commands addressed to the bot by name (`/price@MyBot BTC`) are accepted; commands for other bots are silently ignored.

## Planned

- Docker support
- Portfolio tracking
- Additional alert conditions (below price, percentage change)
