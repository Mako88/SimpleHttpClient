using System;
using System.Net;

namespace SimpleHttpClient.Models
{
    /// <summary>
    /// An untyped HTTP response
    /// </summary>
    public interface IResponse : IRestObject
    {
        /// <summary>
        /// The response status code
        /// </summary>
        HttpStatusCode StatusCode { get; set; }

        /// <summary>
        /// The exception thrown when attempting to serialize the body
        /// </summary>
        Exception SerializationException { get; set; }
    }

    /// <summary>
    /// A typed HTTP response
    /// </summary>
    public interface IResponse<T> : IResponse
    {
        /// <summary>
        /// The body of the RestObject
        /// </summary>
        T Body { get; set; }
    }
}
