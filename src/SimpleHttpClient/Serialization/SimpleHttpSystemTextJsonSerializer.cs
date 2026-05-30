using System.Text.Json;
using System.Text.Json.Serialization;

namespace SimpleHttpClient.Serialization
{
    /// <summary>
    /// A JSON serializer backed by System.Text.Json. Opt in by setting it on the
    /// client (or per-request) instead of the Newtonsoft-based default. Its settings
    /// mirror the default serializer's behavior (camelCase names, null values omitted,
    /// indented output, case-insensitive deserialization) so it's a drop-in for most
    /// payloads.
    /// </summary>
    /// <remarks>
    /// System.Text.Json is stricter than Newtonsoft.Json. Notably, it cannot use a
    /// non-public parameterless constructor when deserializing; such types need a public
    /// constructor or a <see cref="JsonConstructorAttribute"/>. This serializer is slated
    /// to become the default in a future major version.
    /// </remarks>
    public class SimpleHttpSystemTextJsonSerializer : ISimpleHttpSerializer
    {
        // Reused across calls: System.Text.Json caches type metadata per options
        // instance, so sharing one avoids repeating that work on every (de)serialize.
        private static readonly JsonSerializerOptions Options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNameCaseInsensitive = true,
            WriteIndented = true,
        };

        /// <summary>
        /// Serialize the given object into a string.
        /// </summary>
        public string Serialize(object obj) => JsonSerializer.Serialize(obj, Options);

        /// <summary>
        /// Deserialize the given string into an object of type T.
        /// </summary>
        public T Deserialize<T>(string data) => JsonSerializer.Deserialize<T>(data, Options);
    }
}
