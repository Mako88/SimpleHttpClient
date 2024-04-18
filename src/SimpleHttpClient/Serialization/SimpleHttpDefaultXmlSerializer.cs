using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace SimpleHttpClient.Serialization
{
    /// <summary>
    /// The default XML serializer - uses System.Xml.Serialization.
    /// </summary>
    public class SimpleHttpDefaultXmlSerializer : ISimpleHttpSerializer
    {
        /// <summary>
        /// Serialize the given object into a string.
        /// </summary>
        public string Serialize(object obj)
        {
            var serializer = new XmlSerializer(obj.GetType());
            var stringBuilder = new StringBuilder();

            using (var writer = new StringWriter(stringBuilder))
            {
                serializer.Serialize(writer, obj);
                return stringBuilder.ToString();
            }
        }

        /// <summary>
        /// Deserialize the given string into an object of type T.
        /// </summary>
        public T Deserialize<T>(string data)
        {
            var doc = new XmlDocument();
            doc.LoadXml(data);

            var reader = new XmlNodeReader(doc.DocumentElement);
            var serializer = new XmlSerializer(typeof(T));

            return (T) serializer.Deserialize(reader);
        }
    }
}
