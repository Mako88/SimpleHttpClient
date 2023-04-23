using SimpleHttpClient.Serialization;

namespace SimpleHttpClient.Tests.Serialization
{
    public class SimpleHttpDefaultJsonSerializerTests
    {
        private const string TestSerializationString =
@"{
  ""property1"": ""property1 value"",
  ""property2"": 12,
  ""property3"": true
}";

        [Fact]
        public void RoundTrip_Succeeds()
        {
            var objectToSerialize = new TestSerializationObject();

            var testObject = new SimpleHttpDefaultJsonSerializer();

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

            var testObject = new SimpleHttpDefaultJsonSerializer();

            var serialized = testObject.Serialize(objectToSerialize);

            Assert.Equal(TestSerializationString, serialized);
        }

        [Fact]
        public void Deserialization_Succeds()
        {
            var testObject = new SimpleHttpDefaultJsonSerializer();

            var deserializedObject = testObject.Deserialize<TestSerializationObject>(TestSerializationString);

            Assert.NotNull(deserializedObject);
            Assert.Equal("property1 value", deserializedObject.Property1);
            Assert.Equal(12, deserializedObject.Property2);
            Assert.Equal(deserializedObject.Property3, deserializedObject.Property3);
        }
    }

    public class TestSerializationObject
    {
        public string Property1 { get; set; } = "property1 value";

        public int Property2 { get; set; } = 12;

        public bool Property3 { get; set; } = true;
    }
}
