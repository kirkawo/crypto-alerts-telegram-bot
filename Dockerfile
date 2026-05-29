FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore "src/CryptoAlerts.Bot/CryptoAlerts.Bot.csproj"
RUN dotnet publish "src/CryptoAlerts.Bot/CryptoAlerts.Bot.csproj" -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "CryptoAlerts.Bot.dll"]
