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
        /// Execute a request
        /// </summary>
        Task<IResponse> MakeRequest(IRequest request);

        /// <summary>
        /// Execute a request
        /// </summary>
        Task<IResponse<T>> MakeRequest<T>(IRequest request);
    }
}