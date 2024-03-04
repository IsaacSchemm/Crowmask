//using Crowmask.ATProto;
//using Crowmask.Data;
//using Crowmask.HighLevel;
//using Crowmask.Interfaces;
//using Crowmask.LowLevel;
//using Microsoft.Azure.Functions.Worker;
//using Microsoft.Azure.Functions.Worker.Http;
//using Microsoft.EntityFrameworkCore;
//using Microsoft.FSharp.Control;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Net;
//using System.Net.Http;
//using System.Threading.Tasks;

//namespace Crowmask.Functions
//{
//    public class ATProtoSetup(
//        IApplicationInformation appInfo,
//        CrowmaskDbContext context,
//        IHttpClientFactory httpClientFactory)
//    {
//        [Function("ATProtoSetup")]
//        public async Task<HttpResponseData> Run(
//            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "api/bots/atproto/setup")] HttpRequestData req)
//        {
//            var httpClient = httpClientFactory.CreateClient();
//            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(appInfo.UserAgent);

//            var sessions = await context.ATProtoSessions.ToListAsync();

//            var yesterday = DateTimeOffset.UtcNow.AddHours(-12);
//            var activeSessions = sessions.Where(s => s.LastSuccessfulCheck > yesterday);
//            var activeSessionDIDs = activeSessions.Select(s => s.DID);

//            var candidateAccounts = appInfo.ATProtoBotAccounts
//                .Where(a => !activeSessionDIDs.Contains(a.DID));

//            // TODO
//            // * quit early if all configured DIDs are accounted for and have successfully checked for notifications in the past 12 hours
//            // * get post form parameters: hostname, identifier, password
//            // * login
//            // * if DID matches, then store it in the DB

//            return req.CreateResponse(HttpStatusCode.NotImplemented);
//        }
//    }
//}
