using SimpleHttpClient.Models;
using System.Threading.Tasks;

namespace SimpleHttpClient
{
    /// <summary>
    /// The primary Simple Http Client
    /// </summary>
    public interface ISimpleClient
    {
        /// <summary>
        /// Make an untyped request
        /// </summary>
        /// <param name="request">The request that will be sent</param>
        /// <returns>A response object without a strongly-typed body property</returns>
        Task<IResponse> MakeRequest(IRequest request);

        /// <summary>
        /// Make a typed request
        /// </summary>
        /// <typeparam name="T">The type the response body will be serialized into</typeparam>
        /// <param name="request">The request that will be sen</param>
        /// <returns>A response object with a strongly-typed body property</returns>
        Task<IResponse<T>> MakeRequest<T>(IRequest request);
    }
}