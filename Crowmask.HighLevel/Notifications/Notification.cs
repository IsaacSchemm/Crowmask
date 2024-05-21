using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Crowmask.HighLevel.Notifications
{
    public record Notification(
        NotificationCategory Category,
        string Action,
        string User,
        string? Context,
        DateTimeOffset Timestamp);
}
