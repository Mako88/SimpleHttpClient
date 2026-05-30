using SimpleHttpClient.Serialization;

namespace SimpleHttpClient.Tests.Serialization
{
    public class SimpleHttpSystemTextJsonSerializerTests
    {
        // The camelCase, indented shape the serializer is expected to produce.
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

        [Fact]
        public void RoundTrip_WithNestedObjectCollectionAndEnum_PreservesValues()
        {
            var serializer = new SimpleHttpSystemTextJsonSerializer();

            var original = new ComplexObject
            {
                Name = "outer",
                Status = SampleStatus.Active,
                Tags = new List<string> { "a", "b" },
                Child = new TestSerializationObject(),
            };

            var roundTripped = serializer.Deserialize<ComplexObject>(serializer.Serialize(original));

            Assert.NotNull(roundTripped);
            Assert.Equal("outer", roundTripped.Name);
            Assert.Equal(SampleStatus.Active, roundTripped.Status);
            Assert.Equal(new[] { "a", "b" }, roundTripped.Tags);
            Assert.Equal("property1 value", roundTripped.Child?.Property1);
        }

        [Fact]
        public void Serialization_WritesEnumsAsNumbers()
        {
            // Matches Newtonsoft's default (numeric enums), so migrating to System.Text.Json
            // doesn't silently change the enum wire format.
            var serializer = new SimpleHttpSystemTextJsonSerializer();

            var serialized = serializer.Serialize(new ComplexObject { Status = SampleStatus.Active });

            Assert.Contains("\"status\": 1", serialized);
            Assert.DoesNotContain("Active", serialized);
        }

        [Fact]
        public void Deserialization_IntoTypeWithOnlyNonPublicConstructor_Throws()
        {
            // The headline strictness difference from Newtonsoft: System.Text.Json will not
            // invoke a non-public parameterless constructor. Documented in the README migration
            // notes; pinned here so the behavior (and the docs) stay honest.
            var serializer = new SimpleHttpSystemTextJsonSerializer();

            Assert.ThrowsAny<NotSupportedException>(
                () => serializer.Deserialize<NonPublicCtorObject>("{\"value\":1}"));
        }

        [Fact]
        public void Deserialization_ReadsNumbersFromStrings()
        {
            // A quoted number must bind to a numeric property (common API interop case,
            // and Newtonsoft's default behavior).
            var serializer = new SimpleHttpSystemTextJsonSerializer();

            var result = serializer.Deserialize<TestSerializationObject>("{\"property2\": \"42\"}");

            Assert.NotNull(result);
            Assert.Equal(42, result.Property2);
        }

        [Fact]
        public void Deserialization_ToleratesTrailingCommasAndComments()
        {
            var serializer = new SimpleHttpSystemTextJsonSerializer();

            const string json =
@"{
  // leading comment
  ""property1"": ""value"",
  ""property2"": 7,
}";

            var result = serializer.Deserialize<TestSerializationObject>(json);

            Assert.NotNull(result);
            Assert.Equal("value", result.Property1);
            Assert.Equal(7, result.Property2);
        }

        private static string Normalize(string value) => value.Replace("\r\n", "\n");

        private class NullableSerializationObject
        {
            public string? Absent { get; set; } = null;

            public int Value { get; set; } = 1;
        }

        private enum SampleStatus
        {
            None = 0,
            Active = 1,
        }

        private class ComplexObject
        {
            public string? Name { get; set; }

            public SampleStatus Status { get; set; }

            public List<string>? Tags { get; set; }

            public TestSerializationObject? Child { get; set; }
        }

        private class NonPublicCtorObject
        {
            private NonPublicCtorObject()
            {
            }

            public int Value { get; set; }
        }
    }
}
