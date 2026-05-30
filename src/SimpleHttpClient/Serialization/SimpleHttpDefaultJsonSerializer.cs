namespace SimpleHttpClient.Serialization
{
    /// <summary>
    /// The default JSON serializer. As of v5.0.0 it is backed by System.Text.Json
    /// (it used Newtonsoft.Json in earlier versions) and is equivalent to
    /// <see cref="SimpleHttpSystemTextJsonSerializer"/>.
    /// </summary>
    public class SimpleHttpDefaultJsonSerializer : SimpleHttpSystemTextJsonSerializer
    {
    }
}
