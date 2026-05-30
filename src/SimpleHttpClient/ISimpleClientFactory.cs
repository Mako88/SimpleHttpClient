namespace SimpleHttpClient
{
    /// <summary>
    /// A factory for creating <see cref="ISimpleClient"/> instances, each with their own host.
    /// This is the preferred way to obtain a client when using dependency injection, as it avoids
    /// sharing a single mutable client (and its Host) across consumers.
    /// </summary>
    public interface ISimpleClientFactory
    {
        /// <summary>
        /// Create a new <see cref="ISimpleClient"/>.
        /// </summary>
        /// <param name="host">The base url all requests sent through the client will use. If not
        /// provided, it is assumed that the path property on requests will be full URLs.</param>
        /// <returns>A new client instance.</returns>
        ISimpleClient CreateClient(string host = null);
    }
}
