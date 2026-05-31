using System.Text.Json;
using System.Text.Json.Serialization;

namespace SimpleHttpClient.Serialization
{
    /// <summary>
    /// A JSON serializer backed by System.Text.Json. This is the default serializer
    /// (<see cref="SimpleHttpDefaultJsonSerializer"/> derives from it); the type is kept
    /// for callers who reference it explicitly. It serializes with camelCase names, omits
    /// null values, writes indented output, and deserializes case-insensitively. To ease
    /// interop with real-world APIs it also reads numbers from JSON strings (e.g. "123")
    /// and tolerates trailing commas and comments while reading.
    /// </summary>
    /// <remarks>
    /// Deserialization is strict in two ways these options don't soften. It cannot use a
    /// non-public parameterless constructor (such types need a public constructor or a
    /// <see cref="JsonConstructorAttribute"/>), and it won't coerce a JSON value of the wrong
    /// shape (e.g. a string where an object is expected). For fields whose shape varies, attach
    /// a custom <see cref="JsonConverter"/> to the property.
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
            // Read leniencies for real-world API payloads, without masking genuine type mismatches.
            NumberHandling = JsonNumberHandling.AllowReadingFromString,
            AllowTrailingCommas = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
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
