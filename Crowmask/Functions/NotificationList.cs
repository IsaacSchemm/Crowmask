using Crowmask.HighLevel;
using Crowmask.HighLevel.Notifications;
using Crowmask.LowLevel;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Crowmask.Functions
{
    public class NotificationList(
        NotificationCollector notificationCollector,
        WeasylAuthorizationProvider weasylAuthorizationProvider)
    {
        /// <summary>
        /// Returns a list of unread notifications.
        /// </summary>
        /// <param name="req"></param>
        /// <returns></returns>
        [Function("NotificationList")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "api/notification-list")] HttpRequestData req)
        {
            if (!req.Headers.TryGetValues("X-Weasyl-API-Key", out IEnumerable<string> keys))
                return req.CreateResponse(HttpStatusCode.Forbidden);
            if (!keys.Contains(weasylAuthorizationProvider.WeasylApiKey))
                return req.CreateResponse(HttpStatusCode.Forbidden);

            int offset = int.TryParse(req.Query["offset"] ?? "", out int o)
                ? o
                : 0;
            int count = int.TryParse(req.Query["count"] ?? "", out int c)
                ? c
                : 20;

            var notifications = await notificationCollector.GetAllNotificationsAsync()
                .Skip(offset)
                .Take(count)
                .ToListAsync();

            var resp = req.CreateResponse(HttpStatusCode.OK);
            resp.Headers.Add("Content-Type", $"application/json; charset=utf-8");
            await resp.WriteStringAsync(
                JsonSerializer.Serialize(notifications),
                Encoding.UTF8);
            return resp;
        }
    }
}
