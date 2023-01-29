using Newtonsoft.Json;

namespace SimpleHttpClient.Serialization
{
    /// <summary>
    /// The default Json serializer - uses Newtonsoft.Json
    /// </summary>
    public class SimpleHttpDefaultJsonSerializer : ISimpleHttpSerializer
    {
        /// <summary>
        /// Serialize the given object into a string
        /// </summary>
        public string Serialize(object obj) => JsonConvert.SerializeObject(obj, Formatting.Indented);

        /// <summary>
        /// Deserialize the given string into an object of type T
        /// </summary>
        public T Deserialize<T>(string data) => JsonConvert.DeserializeObject<T>(data);
    }
}
