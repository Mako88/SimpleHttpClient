using SimpleHttpClient.Logging;
using SimpleHttpClient.Models;
using SimpleHttpClient.Serialization;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace SimpleHttpClient
{
    /// <summary>
    /// The primary Simple Http Client
    /// </summary>
    public interface ISimpleClient
    {
        /// <summary>
        /// The base url all requests sent through this client will use.
        /// If not set, it is assumed that the path property on requests passed to this client will be full URLs</param>
        /// </summary>
        string Host { get; set; }

        /// <summary>
        /// The HttpClient instance to use. If not set, a new one is created.
        /// See https://github.com/dotnet/aspnetcore/issues/28385#issuecomment-853766480 for a discussion on the proper way to create an HttpClient
        /// </summary>
        HttpClient HttpClient { get; set; }

        /// <summary>
        /// The serializer to convert request/response bodies to types. If not provided, SimpleHttpDefaultJsonSerializer will be used
        /// </summary>
        ISimpleHttpSerializer Serializer { get; set; }

        /// <summary>
        /// The Logger for logging requests and responses
        /// </summary>
        ISimpleHttpLogger Logger { get; set; }

        /// <summary>
        /// Any status codes to be considered successful when setting IsSuccessful in addition to the 200-299 status codes.
        /// This applies to all requests sent with this client
        /// </summary>
        IEnumerable<HttpStatusCode> AdditionalSuccessfulStatusCodes { get; set; }

        /// <summary>
        /// Headers that will be included with all requests sent with this client
        /// </summary>
        Dictionary<string, string> DefaultHeaders { get; set; }

        /// <summary>
        /// Make an untyped request
        /// </summary>
        /// <param name="request">The request that will be sent</param>
        /// <returns>A response object without a strongly-typed body property</returns>
        Task<ISimpleResponse> MakeRequest(ISimpleRequest request);

        /// <summary>
        /// Make a typed request
        /// </summary>
        /// <typeparam name="T">The type the response body will be serialized into</typeparam>
        /// <param name="request">The request that will be sen</param>
        /// <returns>A response object with a strongly-typed body property</returns>
        Task<IResponse<T>> MakeRequest<T>(ISimpleRequest request);
    }
}