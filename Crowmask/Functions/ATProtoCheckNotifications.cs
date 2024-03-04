using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
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

            var exceptions = new List<Exception>(0);

            foreach (var account in appInfo.ATProtoBotAccounts)
            {
                try
                {
                    var session = await context.ATProtoSessions
                        .Where(s => s.Handle == account.Handle)
                        .SingleOrDefaultAsync();

                    if (account.Identifier != null && account.Password != null)
                    {
                        var tokens = await Auth.createSessionAsync(
                            client,
                            account.Hostname,
                            account.Identifier,
                            account.Password);

                        if (tokens.handle != account.Handle)
                            continue;

                        if (session == null)
                        {
                            session = new ATProtoSession
                            {
                                Handle = account.Handle
                            };
                            context.ATProtoSessions.Add(session);
                        }

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
                        limit: Limit.DefaultLimit,
                        cursor: Cursor.FromStart);

                    var mostRecentNotifications = mostRecent.notifications.TakeWhile(n => n.cid != session.LastSeenCid);

                    if (!mostRecentNotifications.Any())
                        continue;

                    string countStr = mostRecent.cursor == null
                        ? $"{mostRecentNotifications.Count()}"
                        : $"{mostRecentNotifications.Count()}+";

                    IEnumerable<string> buildContent()
                    {
                        yield return $"{countStr} Bluesky notification(s) for @{account.Handle}";
                        yield return $"";

                        foreach (var group in mostRecentNotifications.GroupBy(n => n.reason))
                        {
                            var authors = group
                                    .Select(n => n.author.handle)
                                    .Distinct()
                                    .Take(3);
                            yield return $"* **{group.Key}**: {group.Count()} notifications, from users including {string.Join(", ", authors)}";
                        }
                    }

                    await foreach (string inbox in locator.GetAdminActorInboxesAsync())
                    {
                        context.OutboundActivities.Add(new OutboundActivity
                        {
                            Id = Guid.NewGuid(),
                            Inbox = inbox,
                            JsonBody = ActivityPubSerializer.SerializeWithContext(
                                translator.CreateTransientPrivateNote(
                                    string.Join("\n", buildContent()))),
                            StoredAt = DateTimeOffset.UtcNow
                        });
                    }

                    if (mostRecentNotifications.Any())
                        session.LastSeenCid = mostRecentNotifications.First().cid;

                    await context.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            }

            if (exceptions.Count > 0)
            {
                throw new AggregateException("Could not check Bluesky notifications", exceptions);
            }
        }
    }
}
