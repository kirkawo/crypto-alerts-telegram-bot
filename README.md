# Crypto Alerts Telegram Bot

[![CI](https://github.com/kirkawo/crypto-alerts-telegram-bot/actions/workflows/ci.yml/badge.svg)](https://github.com/kirkawo/crypto-alerts-telegram-bot/actions/workflows/ci.yml)

A .NET 8 Telegram bot for cryptocurrency price lookup and automated price alerts via the CoinGecko API.
Designed to run locally or in Docker; cloud deployment is optional and not included in this repository.

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
},
"CoinGecko": {
    "ApiKey": "<your-api-token>"
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

## Docker

Run the bot in a container with SQLite data persisted via a named volume.

### Required environment variables

| Variable | Description |
|----------|-------------|
| `TELEGRAM_BOT_TOKEN` | Telegram bot token from [@BotFather](https://t.me/BotFather) |
| `TELEGRAM_BOT_USERNAME` | Your bot's username (e.g. `MyBot`) |
| `COINGECKO_API_KEY` | CoinGecko API key |

### Quick start

```bash
# 1. Copy and fill in your secrets
cp .env.example .env

# 2. Start the bot (builds image on first run)
docker compose up --build

# 3. Stop
docker compose down
```

The SQLite database is stored in a named Docker volume (`cryptoalerts_data`) at `/data/app.db` inside the container. The data persists across container restarts and recreations. To wipe it:

```bash
docker compose down -v
```

**Secrets note:** Never commit `.env` or `appsettings.Local.json` to version control. The `.gitignore` already excludes them. For production, use a secrets manager or CI/CD secrets instead of `.env`.

### Non-Docker local run

```bash
dotnet build
dotnet run --project src/CryptoAlerts.Bot
```

Set secrets via user secrets, environment variables, or `appsettings.Local.json` (untracked).

## Project status

This project is feature-complete for local and Docker-based use.

- ✅ Telegram bot logic and commands
- ✅ Price alerts and background worker
- ✅ SQLite persistence
- ✅ Docker + Docker Compose setup
- ✅ Local run instructions in this README
- ⏸ Cloud hosting is currently not configured. 
  The project is optimized for local/Docker environments and can be deployed to a paid container hosting platform if needed.
