namespace Crowmask.HighLevel.Notifications
{
    public record Notification(
        string Category,
        string Action,
        string User,
        string? Context,
        DateTimeOffset Timestamp);
}
