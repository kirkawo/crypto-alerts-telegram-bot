using CryptoAlerts.Domain.Entities;
using CryptoAlerts.Domain.Enums;
using CryptoAlerts.Infrastructure.Persistence;
using CryptoAlerts.Infrastructure.Persistence.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace CryptoAlerts.UnitTests;

public class PersistenceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<AppDbContext> _options;

    public PersistenceTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        _options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        using var context = new AppDbContext(_options);
        context.Database.EnsureCreated();
    }

    public void Dispose()
    {
        _connection.Close();
        _connection.Dispose();
    }

    private AppDbContext CreateContext()
    {
        return new AppDbContext(_options);
    }

    [Fact]
    public async Task UserRepository_AddsAndRetrievesUser()
    {
        using var context = CreateContext();
        var repo = new UserRepository(context);

        var user = new TrackedUser(100, 200, "testuser");
        await repo.AddAsync(user);

        var retrieved = await repo.GetByTelegramAsync(100, 200);

        Assert.NotNull(retrieved);
        Assert.Equal(user.Id, retrieved!.Id);
        Assert.Equal(100, retrieved.TelegramChatId);
        Assert.Equal(200, retrieved.TelegramUserId);
        Assert.Equal("testuser", retrieved.Username);
    }

    [Fact]
    public async Task UserRepository_ReturnsNull_ForUnknownUser()
    {
        using var context = CreateContext();
        var repo = new UserRepository(context);

        var result = await repo.GetByTelegramAsync(999, 999);

        Assert.Null(result);
    }

    [Fact]
    public async Task AlertRepository_AddsAndRetrievesAlert()
    {
        using var context = CreateContext();
        var userRepo = new UserRepository(context);
        var alertRepo = new AlertRepository(context);

        var user = new TrackedUser(100, 200, "testuser");
        await userRepo.AddAsync(user);

        var alert = new PriceAlert(user.Id, "BTC", "bitcoin", 50000m, AlertCondition.GreaterOrEqual);
        await alertRepo.AddAsync(alert);

        var retrieved = await alertRepo.GetByIdAsync(alert.Id);

        Assert.NotNull(retrieved);
        Assert.Equal(alert.Id, retrieved!.Id);
        Assert.Equal(user.Id, retrieved.UserId);
        Assert.Equal("BTC", retrieved.AssetSymbol);
        Assert.Equal("bitcoin", retrieved.AssetId);
        Assert.Equal(50000m, retrieved.TargetPrice);
        Assert.Equal(AlertCondition.GreaterOrEqual, retrieved.Condition);
        Assert.Equal(AlertStatus.Active, retrieved.Status);
    }

    [Fact]
    public async Task AlertRepository_ReturnsAlerts_ByUserId()
    {
        using var context = CreateContext();
        var userRepo = new UserRepository(context);
        var alertRepo = new AlertRepository(context);

        var user = new TrackedUser(100, 200, "testuser");
        await userRepo.AddAsync(user);

        var alert1 = new PriceAlert(user.Id, "BTC", "bitcoin", 50000m, AlertCondition.GreaterOrEqual);
        var alert2 = new PriceAlert(user.Id, "ETH", "ethereum", 3000m, AlertCondition.GreaterOrEqual);
        await alertRepo.AddAsync(alert1);
        await alertRepo.AddAsync(alert2);

        var alerts = await alertRepo.GetByUserIdAsync(user.Id);

        Assert.Equal(2, alerts.Count);
        Assert.Contains(alerts, a => a.AssetSymbol == "BTC");
        Assert.Contains(alerts, a => a.AssetSymbol == "ETH");
    }

    [Fact]
    public async Task AlertRepository_Update_PersistsChanges()
    {
        using var context = CreateContext();
        var userRepo = new UserRepository(context);
        var alertRepo = new AlertRepository(context);

        var user = new TrackedUser(100, 200, "testuser");
        await userRepo.AddAsync(user);

        var alert = new PriceAlert(user.Id, "BTC", "bitcoin", 50000m, AlertCondition.GreaterOrEqual);
        await alertRepo.AddAsync(alert);

        // Reload and modify
        var loaded = await alertRepo.GetByIdAsync(alert.Id);
        loaded!.Trigger();
        await alertRepo.UpdateAsync(loaded);

        // Verify with a fresh context
        using var freshContext = CreateContext();
        var verifyRepo = new AlertRepository(freshContext);
        var retrieved = await verifyRepo.GetByIdAsync(alert.Id);

        Assert.NotNull(retrieved);
        Assert.Equal(AlertStatus.Triggered, retrieved!.Status);
        Assert.NotNull(retrieved.TriggeredAtUtc);
    }
}
