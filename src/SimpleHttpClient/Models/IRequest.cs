using System.Collections.Generic;
using System.Net.Http;
using System.Text;

namespace SimpleHttpClient.Models
{
    /// <summary>
    /// An untyped HTTP request
    /// </summary>
    public interface IRequest : IRestObject
    {
        /// <summary>
        /// The HTTP Request Method
        /// </summary>
        HttpMethod Method { get; set; }

        /// <summary>
        /// Query String Parameters on the request
        /// </summary>
        Dictionary<string, string> QueryStringParameters { get; set; }

        /// <summary>
        /// The request path that is appended to the client's host
        /// </summary>
        string Path { get; set; }

        /// <summary>
        /// A request URL to override the url set in the client
        /// </summary>
        string OverrideUrl { get; set; }

        /// <summary>
        /// The encoding for the content
        /// </summary>
        Encoding ContentEncoding { get; set; }

        /// <summary>
        /// The content type of the request
        /// </summary>
        string ContentType { get; set; }
    }

    /// <summary>
    /// A typed HTTP request
    /// </summary>
    public interface IRequest<T> : IRequest, IRestObject<T>
    {

    }
}
