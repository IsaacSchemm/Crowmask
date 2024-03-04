using Crowmask.ATProto;
using Crowmask.Data;
using Microsoft.EntityFrameworkCore;

namespace Crowmask.HighLevel.ATProto
{
    public class TokenWrapper(CrowmaskDbContext context, ATProtoSession session) : IAutomaticRefreshCredentials
    {
        public string AccessToken { get; private set; } = session.AccessToken;

        public string RefreshToken { get; private set; } = session.RefreshToken;

        public async Task UpdateTokensAsync(IRefreshTokenCredentials newCredentials)
        {
            AccessToken = newCredentials.AccessToken;
            RefreshToken = newCredentials.RefreshToken;

            var dbRecord = await context.ATProtoSessions
                .Where(a => a.Handle == session.Handle)
                .SingleOrDefaultAsync();

            if (dbRecord != null)
            {
                dbRecord.AccessToken = AccessToken;
                dbRecord.RefreshToken = RefreshToken;
                await context.SaveChangesAsync();
            }
        }
    }
}
