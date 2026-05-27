namespace CryptoAlerts.Application.Exceptions;

public class AlertAccessDeniedException : Exception
{
    public AlertAccessDeniedException()
        : base("Alert not found or access denied.")
    {
    }
}
