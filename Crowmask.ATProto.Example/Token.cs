using Crowmask.ATProto;

public class Token : IAutomaticRefreshCredentials
{
    public string Hostname => "bsky.social";

    public string RefreshToken { get; set; } = "";

    public string AccessToken { get; set; } = "";

    public async Task UpdateTokensAsync(IRefreshTokenCredentials newCredentials)
    {
        RefreshToken = newCredentials.RefreshToken;
        AccessToken = newCredentials.AccessToken;
    }
}