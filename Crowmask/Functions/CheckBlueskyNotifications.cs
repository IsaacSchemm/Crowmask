using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
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

namespace Crowmask.Functions
{
    public class CheckBlueskyNotifications(
        ActivityPubTranslator translator,
        CrowmaskDbContext context,
        IApplicationInformation appInfo,
        IHttpClientFactory httpClientFactory,
        RemoteInboxLocator locator)
    {
        /// <summary>
        /// Checks the atproto PDS for any notifications since the last check,
        /// and (if any) sends a message to the admin actor summarizing them.
        /// Runs every 6 hours.
        /// </summary>
        /// <param name="myTimer"></param>
        /// <returns></returns>
        [Function("CheckBlueskyNotifications")]
        public async Task Run([TimerTrigger("0 0 */6 * * *")] TimerInfo myTimer)
        {
            var client = httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd(appInfo.UserAgent);

            foreach (var account in appInfo.BlueskyBotAccounts)
            {
                try
                {
                    var session = await context.BlueskySessions
                        .Where(s => s.DID == account.DID)
                        .SingleOrDefaultAsync();

                    if (account.Identifier != null && account.Password != null)
                    {
                        var tokens = await Auth.CreateSessionAsync(
                            client,
                            account,
                            account.Identifier,
                            account.Password);

                        if (tokens.did != account.DID)
                            continue;

                        if (session == null)
                        {
                            session = new BlueskySession
                            {
                                DID = account.DID
                            };
                            context.BlueskySessions.Add(session);
                        }

                        session.AccessToken = tokens.accessJwt;
                        session.RefreshToken = tokens.refreshJwt;
                    }

                    if (session == null)
                        continue;

                    if (session.PDS != account.PDS)
                    {
                        session.PDS = account.PDS;
                    }

                    var wrapper = new TokenWrapper(context, session);

                    var mostRecent = await Notifications.ListNotificationsAsync(
                        client,
                        wrapper,
                        limit: Notifications.Limit.DefaultLimit,
                        cursor: Notifications.Cursor.FromStart);

                    var mostRecentNotifications = mostRecent.notifications.TakeWhile(n => n.cid != session.LastSeenCid);

                    if (!mostRecentNotifications.Any())
                        continue;

                    string countStr = mostRecent.cursor == null
                        ? $"{mostRecentNotifications.Count()}"
                        : $"{mostRecentNotifications.Count()}+";

                    IEnumerable<string> buildContent()
                    {
                        yield return $"{countStr} Bluesky notification(s) for [`{WebUtility.HtmlEncode(account.DID)}`](https://bsky.app/profile/{WebUtility.HtmlEncode(account.DID)})";
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
                catch (Exception) { }
            }
        }
    }
}
