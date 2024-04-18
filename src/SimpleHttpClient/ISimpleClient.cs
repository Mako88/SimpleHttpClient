using SimpleHttpClient.Logging;
using SimpleHttpClient.Models;
using SimpleHttpClient.Serialization;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace SimpleHttpClient
{
    /// <summary>
    /// The primary Simple Http Client.
    /// </summary>
    public interface ISimpleClient
    {
        /// <summary>
        /// The base url all requests sent through this client will use.
        /// If not set, it is assumed that the path property on requests passed to this client will be full URLs.
        /// </summary>
        string Host { get; set; }

        /// <summary>
        /// The serializer to convert request/response bodies to types. If not provided, SimpleHttpDefaultJsonSerializer will be used.
        /// </summary>
        ISimpleHttpSerializer Serializer { get; set; }

        /// <summary>
        /// The Logger for logging requests and responses.
        /// </summary>
        ISimpleHttpLogger Logger { get; set; }

        /// <summary>
        /// Any status codes to be considered successful when setting IsSuccessful in addition to the 200-299 status codes.
        /// This applies to all requests sent with this client.
        /// </summary>
        List<HttpStatusCode> AdditionalSuccessfulStatusCodes { get; }

        /// <summary>
        /// Headers that will be included with all requests sent with this client.
        /// </summary>
        Dictionary<string, string> DefaultHeaders { get; }

        /// <summary>
        /// Timeout in seconds of all requests sent with this client.
        /// To disable the timeout, set to -1.
        /// </summary>
        int Timeout { get; set; }

        /// <summary>
        /// An optional action for logging a request.
        /// This is called right before the request is sent.
        /// </summary>
        LogRequest LogRequest { get; set; }

        /// <summary>
        /// An optional action for logging a response.
        /// This is called right after a response is received.
        /// </summary>
        LogResponse LogResponse { get; set; }

        /// <summary>
        /// Make an untyped request.
        /// </summary>
        /// <param name="request">The request that will be sent.</param>
        /// <returns>A response object without a strongly-typed body property.</returns>
        Task<ISimpleResponse> MakeRequest(ISimpleRequest request);

        /// <summary>
        /// Make a typed request.
        /// </summary>
        /// <typeparam name="T">The type the response body will be serialized into.</typeparam>
        /// <param name="request">The request that will be sent.</param>
        /// <returns>A response object with a strongly-typed body property.</returns>
        Task<ISimpleResponse<T>> MakeRequest<T>(ISimpleRequest request);

        /// <summary>
        /// Get the URL the given request will be sent to by this client.
        /// </summary>
        /// <param name="request">The request to determine the URL for.</param>
        /// <returns>The URL the given request will be made to.</returns>
        string GetUrl(ISimpleRequest request);
    }
}