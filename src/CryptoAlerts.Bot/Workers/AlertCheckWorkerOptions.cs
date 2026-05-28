namespace CryptoAlerts.Bot.Workers;

public class AlertCheckWorkerOptions
{
    public const string SectionName = "AlertCheckWorker";

    public int PollingIntervalSeconds { get; set; } = 60;
}
