using SimpleHttpClient.Serialization;

namespace SimpleHttpClient.Tests.Serialization
{
    public class SimpleHttpDefaultXmlSerializerTests
    {
        private const string TestSerializationString =
@"<?xml version=""1.0"" encoding=""utf-16""?>
<TestSerializationObject xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"">
  <Property1>property1 value</Property1>
  <Property2>12</Property2>
  <Property3>true</Property3>
</TestSerializationObject>";

        [Fact]
        public void RoundTrip_Succeeds()
        {
            var objectToSerialize = new TestSerializationObject();

            var testObject = new SimpleHttpDefaultXmlSerializer();

            var serialized = testObject.Serialize(objectToSerialize);

            var deserializedObject = testObject.Deserialize<TestSerializationObject>(serialized);

            Assert.NotNull(deserializedObject);
            Assert.Equal(objectToSerialize.Property1, deserializedObject.Property1);
            Assert.Equal(objectToSerialize.Property2, deserializedObject.Property2);
            Assert.Equal(objectToSerialize.Property3, deserializedObject.Property3);
        }

        [Fact]
        public void Serialization_Succeeds()
        {
            var objectToSerialize = new TestSerializationObject();

            var testObject = new SimpleHttpDefaultXmlSerializer();

            var serialized = testObject.Serialize(objectToSerialize);

            Assert.Equal(TestSerializationString, serialized);
        }

        [Fact]
        public void Deserialization_Succeds()
        {
            var testObject = new SimpleHttpDefaultXmlSerializer();

            var deserializedObject = testObject.Deserialize<TestSerializationObject>(TestSerializationString);

            Assert.NotNull(deserializedObject);
            Assert.Equal("property1 value", deserializedObject.Property1);
            Assert.Equal(12, deserializedObject.Property2);
            Assert.Equal(deserializedObject.Property3, deserializedObject.Property3);
        }
    }
}
