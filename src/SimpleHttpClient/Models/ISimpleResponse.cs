using System;
using System.Net;

namespace SimpleHttpClient.Models
{
    /// <summary>
    /// An untyped HTTP response
    /// </summary>
    public interface ISimpleResponse : ISimpleRestObject
    {
        /// <summary>
        /// The response status code
        /// </summary>
        HttpStatusCode StatusCode { get; set; }

        /// <summary>
        /// Whether or not the request was successful
        /// </summary>
        bool IsSuccessful { get; set; }

        /// <summary>
        /// The body as a byte array
        /// </summary>
        byte[] ByteBody { get; set; }
    }

    /// <summary>
    /// A typed HTTP response
    /// </summary>
    public interface ISimpleResponse<T> : ISimpleResponse
    {
        /// <summary>
        /// The serialized response body
        /// </summary>
        T Body { get; set; }

        /// <summary>
        /// The exception thrown (if any) when attempting to serialize the body
        /// </summary>
        Exception SerializationException { get; set; }
    }
}
