using System.Threading.Tasks;
using Crowmask.HighLevel;
using Microsoft.Azure.Functions.Worker;

namespace Crowmask.Functions
{
    public class RefreshProfile(UserCache userCache)
    {
        /// <summary>
        /// Refreshes user profile data (name, avatar, etc.) Runs every hour.
        /// </summary>
        /// <param name="myTimer"></param>
        /// <returns></returns>
        [Function("RefreshProfile")]
        public async Task Run([TimerTrigger("0 5 * * * *")] TimerInfo myTimer)
        {
            await userCache.UpdateUserAsync();
        }
    }
}
