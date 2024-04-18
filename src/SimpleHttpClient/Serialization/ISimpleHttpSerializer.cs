namespace SimpleHttpClient.Serialization
{
    /// <summary>
    /// The serializer interface for SimpleHttp.
    /// Custom serialization implementations should implement this interface.
    /// </summary>
    public interface ISimpleHttpSerializer
    {
        /// <summary>
        /// Deserialize the given string into an object of type T.
        /// </summary>
        T Deserialize<T>(string data);

        /// <summary>
        /// Serialize the given object into a string.
        /// </summary>
        string Serialize(object obj);
    }
}