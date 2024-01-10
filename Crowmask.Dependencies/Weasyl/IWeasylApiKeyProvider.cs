namespace Crowmask.Dependencies.Weasyl
{
    /// <summary>
    /// Provides an API key to access the Weasyl API and web interface.
    /// </summary>
    public interface IWeasylApiKeyProvider
    {
        /// <summary>
        /// The Weasyl API key.
        /// </summary>
        string ApiKey { get; }
    }
}
