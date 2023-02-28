using System.Reflection;
using System.Text;

namespace SimpleHttpClient
{
    /// <summary>
    /// Constant values used by SimpleHttpClient
    /// </summary>
    public static class Constants
    {
        /// <summary>
        /// The default content type used on requests
        /// </summary>
        public static string DefaultContentType = "application/json";

        /// <summary>
        /// The default user agent string used on requests
        /// </summary>
        public static string DefaultUserAgent = $"SimpleHttpClient/{new AssemblyName(typeof(SimpleClient).Assembly.FullName).Version}";

        /// <summary>
        /// The default encoding used on requests
        /// </summary>
        public static Encoding DefaultEncoding = Encoding.UTF8;

        /// <summary>
        /// The Content-Type string for Form Url Encoded parameters
        /// </summary>
        public static string FormUrlEncodedContentType = "application/x-www-form-urlencoded";
    }
}
