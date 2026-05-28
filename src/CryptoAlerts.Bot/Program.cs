using CryptoAlerts.Application;
using CryptoAlerts.Application.Interfaces;
using CryptoAlerts.Bot.Telegram;
using CryptoAlerts.Infrastructure;
using CryptoAlerts.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.Configure<TelegramBotOptions>(
    builder.Configuration.GetSection(TelegramBotOptions.SectionName));

builder.Services.AddSingleton<ITelegramBotClient>(sp =>
{
    var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<TelegramBotOptions>>();
    return new TelegramBotClient(options.Value.BotToken);
});

builder.Services.AddScoped<TelegramUpdateHandler>();
builder.Services.AddSingleton<ITelegramMessageSender, TelegramMessageSender>();
builder.Services.AddHostedService<TelegramPollingService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}

app.MapGet("/", () => "Hello World!");

app.Run();
