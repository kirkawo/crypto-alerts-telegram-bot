# AGENTS.md — crypto-alerts-telegram-bot

## Identity

.NET 8 Telegram bot for crypto price tracking and alerts. (See `README.md`.)

## State

Scaffolding only — no `.csproj`, `Program.cs`, or any source files yet. The `.gitignore` is the standard Visual Studio template.

## Expected toolchain (standard .NET 8)

```powershell
dotnet restore
dotnet build
dotnet test
dotnet run --project src/<ProjectName>.csproj
```

No custom build/test/lint scripts exist yet. Once projects are added, prefer `dotnet` CLI over MSBuild directly.

## Conventions

- `src/` for application code, `tests/` for test projects (xUnit is typical for .NET).
- Likely NuGet packages: `Telegram.Bot` for bot API, `DotNetEnv` or `Microsoft.Extensions.Configuration.EnvironmentVariables` for config/env loading.
- `.env` files are gitignored by the VS `.gitignore` template — any secrets/config should use environment variables or user secrets.
- Target framework: `net8.0`.

## Development notes

- No CI/CD, Docker, or infra config exists yet.
- No styling/formatting config yet. Consider adding `.editorconfig` and enabling Roslyn analyzers before writing significant code.
