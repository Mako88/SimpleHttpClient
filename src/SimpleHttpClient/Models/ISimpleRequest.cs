using SimpleHttpClient.Serialization;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;

namespace SimpleHttpClient.Models
{
    /// <summary>
    /// The base untyped HTTP request.
    /// </summary>
    public interface ISimpleRequest : ISimpleRestObject
    {
        /// <summary>
        /// The HTTP Request Method.
        /// </summary>
        HttpMethod Method { get; set; }

        /// <summary>
        /// Query String Parameters on the request.
        /// </summary>
        Dictionary<string, string> QueryStringParameters { get; }

        /// <summary>
        /// Form Url Encoded parameters for POST/PUT requests.
        /// </summary>
        Dictionary<string, string> FormUrlEncodedParameters { get; }

        /// <summary>
        /// The request path that is appended to the client's host.
        /// </summary>
        string Path { get; set; }

        /// <summary>
        /// A complete URL to override the url set in the client.
        /// </summary>
        string UrlOverride { get; set; }

        /// <summary>
        /// The serializer to use instead of the serializer set in the client.
        /// </summary>
        ISimpleHttpSerializer SerializerOverride { get; set; }

        /// <summary>
        /// Any status codes to be considered successful when setting IsSuccessful in addition to the 200-299 status codes.
        /// </summary>
        List<HttpStatusCode> AdditionalSuccessfulStatusCodes { get; set; }

        /// <summary>
        /// Timeout in seconds for this request. This overrides the timeout set on the client.
        /// To disable the timeout, set to -1.
        /// </summary>
        int? TimeoutOverride { get; set; }

        /// <summary>
        /// The encoding for the request content.
        /// </summary>
        Encoding ContentEncoding { get; set; }

        /// <summary>
        /// The content type of the request.
        /// </summary>
        string ContentType { get; set; }

        /// <summary>
        /// The body of the request.
        /// </summary>
        object Body { get; set; }
    }
}
