using Crowmask.ATProto;
using Crowmask.Data;
using Microsoft.EntityFrameworkCore;

namespace Crowmask.HighLevel.ATProto
{
    public class TokenWrapper(CrowmaskDbContext context, BlueskySession session) : IAutomaticRefreshCredentials
    {
        public string DID { get; private set; } = session.DID;

        public string AccessToken { get; private set; } = session.AccessToken;

        public string RefreshToken { get; private set; } = session.RefreshToken;

        public async Task UpdateTokensAsync(IRefreshTokenCredentials newCredentials)
        {
            DID = newCredentials.DID;
            AccessToken = newCredentials.AccessToken;
            RefreshToken = newCredentials.RefreshToken;

            var dbRecord = await context.BlueskySessions
                .Where(a => a.DID == session.DID)
                .SingleOrDefaultAsync();

            if (dbRecord != null)
            {
                dbRecord.DID = DID;
                dbRecord.AccessToken = AccessToken;
                dbRecord.RefreshToken = RefreshToken;
                await context.SaveChangesAsync();
            }
        }
    }
}
