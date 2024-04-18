using SimpleHttpClient.Models;

namespace SimpleHttpClient.Logging
{
    /// <summary>
    /// An interface that can be implemented for logging requests/responses.
    /// </summary>
    public interface ISimpleHttpLogger
    {
        /// <summary>
        /// Log a request.
        /// </summary>
        /// <param name="url">The URL the request is pointing to.</param>
        /// <param name="request">The request object to log.</param>
        void LogRequest(string url, ISimpleRequest request);

        /// <summary>
        /// Log a response.
        /// </summary>
        /// <param name="response">The response object to log.</param>
        void LogResponse(ISimpleResponse response);
    }
}
