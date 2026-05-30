using SimpleHttpClient.Serialization;

namespace SimpleHttpClient.Tests.Serialization
{
    public class SimpleHttpSystemTextJsonSerializerTests
    {
        // Same shape and formatting the Newtonsoft-based default produces, so the
        // System.Text.Json serializer is verified to be a drop-in for typical payloads.
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

            var testObject = new SimpleHttpSystemTextJsonSerializer();

            var serialized = testObject.Serialize(objectToSerialize);

            var deserializedObject = testObject.Deserialize<TestSerializationObject>(serialized);

            Assert.NotNull(deserializedObject);
            Assert.Equal(objectToSerialize.Property1, deserializedObject.Property1);
            Assert.Equal(objectToSerialize.Property2, deserializedObject.Property2);
            Assert.Equal(objectToSerialize.Property3, deserializedObject.Property3);
        }

        [Fact]
        public void Serialization_ProducesCamelCaseIndentedJson()
        {
            var objectToSerialize = new TestSerializationObject();

            var testObject = new SimpleHttpSystemTextJsonSerializer();

            var serialized = testObject.Serialize(objectToSerialize);

            // Normalize line endings so the comparison doesn't depend on the
            // platform's newline or the checkout's git autocrlf setting.
            Assert.Equal(Normalize(TestSerializationString), Normalize(serialized));
        }

        [Fact]
        public void Deserialization_IsCaseInsensitive()
        {
            // PascalCase input must still bind even though we serialize camelCase,
            // matching Newtonsoft's default leniency.
            const string pascalCaseJson =
@"{
  ""Property1"": ""property1 value"",
  ""Property2"": 12,
  ""Property3"": true
}";

            var testObject = new SimpleHttpSystemTextJsonSerializer();

            var deserializedObject = testObject.Deserialize<TestSerializationObject>(pascalCaseJson);

            Assert.NotNull(deserializedObject);
            Assert.Equal("property1 value", deserializedObject.Property1);
            Assert.Equal(12, deserializedObject.Property2);
            Assert.True(deserializedObject.Property3);
        }

        [Fact]
        public void Serialization_OmitsNullValues()
        {
            var testObject = new SimpleHttpSystemTextJsonSerializer();

            var serialized = testObject.Serialize(new NullableSerializationObject());

            // The null property is dropped entirely; the non-null one stays.
            Assert.DoesNotContain("absent", serialized);
            Assert.Contains("\"value\": 1", serialized);
        }

        private static string Normalize(string value) => value.Replace("\r\n", "\n");

        private class NullableSerializationObject
        {
            public string? Absent { get; set; } = null;

            public int Value { get; set; } = 1;
        }
    }
}
