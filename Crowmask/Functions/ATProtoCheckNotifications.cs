using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
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
        public async Task Run([TimerTrigger("0 0 */6 * * *")] TimerInfo myTimer)
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
                {
                    break;
                }

                var wrapper = new TokenWrapper(context, session);

                async IAsyncEnumerable<NN.Notification> listAll()
                {
                    var paging = new NN.NotificationParameters(
                        limit: 1,
                        cursor: null);

                    while (true)
                    {
                        var results = await NN.listNotificationsAsync(
                            client,
                            account.Hostname,
                            wrapper,
                            paging);

                        foreach (var item in results.notifications)
                            yield return item;

                        if (results.cursor is string newCursor)
                            paging = new NN.NotificationParameters(
                                limit: paging.limit,
                                cursor: newCursor);
                        else
                            yield break;
                    }
                }

                StringBuilder sb = new();

                try
                {
                    var notifications = await listAll()
                        .TakeWhile(n => n.cid != session.LastSeenCid)
                        .Take(101)
                        .ToListAsync();

                    if (notifications.Count > 100)
                    {
                        sb.Append($"<a href='https://bsky.app/notifications'>Bluesky notifications:</a> 100+");
                    }
                    else
                    {
                        sb.AppendLine("<a href='https://bsky.app/notifications'>Bluesky notifications:</a>");
                        sb.AppendLine("");
                        foreach (var group in notifications.Take(100).GroupBy(n => n.reason))
                        {
                            var allAuthors = group
                                .Select(n => n.author.handle)
                                .ToList();
                            var authorsDisplay = allAuthors.Count > 2
                                ? allAuthors.Take(1).Concat(["and others"])
                                : allAuthors;
                            sb.AppendLine($"* {group.Key}: {group.Count()} notification(s) from {string.Join(", ", authorsDisplay)}");
                        }
                    }

                    session.LastSeenCid = notifications.Take(100).Last().cid;
                }
                catch (Exception ex)
                {
                    sb.AppendLine(WebUtility.HtmlEncode(ex.ToString()));

                    context.ATProtoSessions.Remove(session);
                }

                await foreach (string inbox in locator.GetAdminActorInboxesAsync())
                {
                    context.OutboundActivities.Add(new OutboundActivity
                    {
                        Id = Guid.NewGuid(),
                        Inbox = inbox,
                        JsonBody = ActivityPubSerializer.SerializeWithContext(
                            translator.TransientPrivateNoteToCreate(
                                sb.ToString())),
                        StoredAt = DateTimeOffset.UtcNow
                    });
                }

                await context.SaveChangesAsync();
            }
        }
    }
}
