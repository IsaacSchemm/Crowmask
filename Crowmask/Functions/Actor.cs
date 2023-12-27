using Crowmask.ActivityPub;
using Crowmask.Cache;
using Crowmask.Markdown;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Crowmask.Functions
{
    public class Actor(CrowmaskCache crowmaskCache, IPublicKeyProvider keyProvider, MarkdownTranslator markdownTranslator, Translator translator)
    {
        [FunctionName("Actor")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "api/actor")] HttpRequest req,
            ILogger log)
        {
            var person = await crowmaskCache.GetUser();

            foreach (var format in ContentNegotiation.FindAppropriateFormats(req.GetTypedHeaders().Accept))
            {
                if (format.IsActivityJson)
                {
                    var key = await keyProvider.GetPublicKeyAsync();

                    string json = AP.SerializeWithContext(translator.PersonToObject(person, key));

                    return new ContentResult
                    {
                        Content = json,
                        ContentType = format.ContentType
                    };
                }
                else if (format.IsHTML)
                {
                    return new ContentResult
                    {
                        Content = markdownTranslator.ToHtml(person),
                        ContentType = format.ContentType
                    };
                }
                else if (format.IsMarkdown)
                {
                    return new ContentResult
                    {
                        Content = markdownTranslator.ToMarkdown(person),
                        ContentType = format.ContentType
                    };
                }
            }

            return new StatusCodeResult(406);
        }
    }
}
