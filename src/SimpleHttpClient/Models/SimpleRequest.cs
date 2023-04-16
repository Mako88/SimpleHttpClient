using SimpleHttpClient.Serialization;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;

namespace SimpleHttpClient.Models
{
    /// <summary>
    /// The base untyped HTTP request
    /// </summary>
    public class SimpleRequest : SimpleRestObject, ISimpleRequest
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public SimpleRequest(string path, HttpMethod method = null, object body = null)
        {
            Path = path;
            Method = method ?? HttpMethod.Get;
            StringBody = body as string;
            Body = body;
        }

        /// <summary>
        /// The HTTP Request Method
        /// </summary>
        public HttpMethod Method { get; set; }

        /// <summary>
        /// Query String Parameters on the request
        /// </summary>
        public Dictionary<string, string> QueryStringParameters { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// Form Url Encoded parameters for POST/PUT requests
        /// </summary>
        public Dictionary<string, string> FormUrlEncodedParameters { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// The request path that is appended to the client's host
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// A complete URL to override the url set in the client
        /// </summary>
        public string UrlOverride { get; set; }

        /// <summary>
        /// The serializer to use instead of the serializer set in the client
        /// </summary>
        public ISimpleHttpSerializer SerializerOverride { get; set; }

        /// <summary>
        /// Any status codes to be considered successful when setting IsSuccessful in addition to the 200-299 status codes
        /// </summary>
        public IEnumerable<HttpStatusCode> AdditionalSuccessfulStatusCodes { get; set; } = new List<HttpStatusCode>();

        /// <summary>
        /// Timeout in seconds for this request. This overrides the timeout set on the client
        /// To disable the timeout, set to -1
        /// </summary>
        public int? TimeoutOverride { get; set; } = null;

        /// <summary>
        /// The encoding for the request content
        /// </summary>
        public Encoding ContentEncoding { get; set; } = Constants.DefaultEncoding;

        /// <summary>
        /// The content type of the request
        /// </summary>
        public string ContentType { get; set; } = Constants.DefaultContentType;

        /// <summary>
        /// The body of the request
        /// </summary>
        public object Body { get; set; }
    }
}
