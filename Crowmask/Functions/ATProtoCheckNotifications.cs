using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using Crowmask.ATProto;
using Crowmask.Data;
using Crowmask.HighLevel;
using Crowmask.HighLevel.ATProto;
using Crowmask.Interfaces;
using Crowmask.LowLevel;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using NN = Crowmask.ATProto.Notifications;

namespace Crowmask.Functions
{
    public class ATProtoCheckNotifications(ActivityPubTranslator translator, CrowmaskDbContext context, IApplicationInformation appInfo, IHttpClientFactory httpClientFactory, RemoteInboxLocator locator)
    {
        /// <summary>
        /// Checks the atproto PDS for any notifications since the last check,
        /// and (if any) sends a message to the admin actor summarizing them.
        /// Runs every 6 hours.
        /// </summary>
        /// <param name="myTimer"></param>
        /// <returns></returns>
        [Function("ATProtoCheckNotifications")]
        //public async Task Run([TimerTrigger("0 0 */6 * * *")] TimerInfo myTimer)
        public async Task Run([TimerTrigger("59 */1 * * * *")] TimerInfo myTimer)
        {
            var client = httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd(appInfo.UserAgent);

            foreach (var account in appInfo.ATProtoBotAccounts)
            {
                var session = await context.ATProtoSessions
                    .Where(s => s.DID == account.DID)
                    .SingleOrDefaultAsync();

                if (account.Identifier != null && account.Password != null)
                {
                    if (session == null)
                    {
                        session = new ATProtoSession
                        {
                            DID = account.DID
                        };
                        context.ATProtoSessions.Add(session);
                    }

                    var tokens = await Auth.createSessionAsync(
                        client,
                        account.Hostname,
                        account.Identifier,
                        account.Password);

                    session.AccessToken = tokens.accessJwt;
                    session.RefreshToken = tokens.refreshJwt;
                }

                if (session == null)
                    continue;

                var wrapper = new TokenWrapper(context, session);

                var mostRecent = await NN.listNotificationsAsync(
                    client,
                    account.Hostname,
                    wrapper,
                    new NN.NotificationParameters(
                        limit: 100,
                        cursor: null));

                var mostRecentNotifications = mostRecent.notifications.TakeWhile(n => n.cid != session.LastSeenCid);

                //if (!mostRecentNotifications.Any())
                //    continue;

                string countStr = mostRecent.cursor == null
                    ? $"{mostRecentNotifications.Count()}"
                    : $"{mostRecentNotifications.Count()}+";

                string title = $"{account.DID}: {countStr} Bluesky notification(s)";

                string description = string.Join(
                    "\n",
                    mostRecentNotifications
                        .GroupBy(n => n.reason)
                        .Select(group =>
                        {
                            var authors = group
                                .Select(n => n.author.handle)
                                .Distinct()
                                .Take(3);
                            return $"* **{group.Key}**: {group.Count()} notifications, from users including {string.Join(", ", authors)}";
                        }));

                await foreach (string inbox in locator.GetAdminActorInboxesAsync())
                {
                    context.OutboundActivities.Add(new OutboundActivity
                    {
                        Id = Guid.NewGuid(),
                        Inbox = inbox,
                        JsonBody = ActivityPubSerializer.SerializeWithContext(
                            translator.TransientPrivateArticleToCreate(
                                name: title,
                                description: description)),
                        StoredAt = DateTimeOffset.UtcNow
                    });
                }

                if (mostRecentNotifications.Any())
                    session.LastSeenCid = mostRecentNotifications.First().cid;

                await context.SaveChangesAsync();
            }
        }
    }
}
