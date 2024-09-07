using System.Diagnostics;

namespace OtelTagify.Tests
{
    public class OtelTagifyTests
    {
        [Fact]
        public void SetTagsFromObject_WithTaggedProperties_ShouldSetCorrectTags()
        {
            // Arrange
            var testObject = new TestClass
            {
                TaggedProperty = "test value",
                PrefixedProperty = 42,
                UntaggedProperty = "should not be tagged"
            };

            var activity = new Activity("TestActivity");
            activity.Start();

            // Act
            activity.SetTagsFromObject(testObject);

            // Assert
            Assert.Equal("test value", activity.GetTagItem("tagged_property"));
            Assert.Equal("42", activity.GetTagItem("prefix.prefixed_property"));
            Assert.Null(activity.GetTagItem("UntaggedProperty"));
        }

        [Fact]
        public void SetTagsFromObject_WithNullValues_ShouldNotSetTags()
        {
            // Arrange
            var testObject = new TestClass
            {
                TaggedProperty = null,
                PrefixedProperty = 42,
                UntaggedProperty = "should not be tagged"
            };

            var activity = new Activity("TestActivity");
            activity.Start();

            // Act
            activity.SetTagsFromObject(testObject);

            // Assert
            Assert.Null(activity.GetTagItem("tagged_property"));
            Assert.Equal("42", activity.GetTagItem("prefix.prefixed_property"));
        }

        [Fact]
        public void SetTagsFromObject_WithTagAllPropertiesTrue_ShouldTagAllProperties()
        {
            // Arrange
            OtelTagConfiguration.TagAllProperties = true;
            var testObject = new TestClass
            {
                TaggedProperty = "test value",
                PrefixedProperty = 42,
                UntaggedProperty = "should be tagged now"
            };

            var activity = new Activity("TestActivity");
            activity.Start();

            // Act
            activity.SetTagsFromObject(testObject);

            // Assert
            Assert.Equal("test value", activity.GetTagItem("taggedproperty"));
            Assert.Equal("42", activity.GetTagItem("prefixedproperty"));
            Assert.Equal("should be tagged now", activity.GetTagItem("untaggedproperty"));

            // Clean up
            OtelTagConfiguration.TagAllProperties = false;
        }

        [Fact]
        public void SetTagsFromObject_WithNullObject_ShouldNotThrowException()
        {
            // Arrange
            TestClass testObject = null;
            var activity = new Activity("TestActivity");
            activity.Start();

            // Act & Assert
            var exception = Record.Exception(() => activity.SetTagsFromObject(testObject));
            Assert.Null(exception);
        }
    }

    public class TestClass
    {
        [OtelTag("tagged_property")]
        public string? TaggedProperty { get; set; }

        [OtelTag("prefixed_property", "prefix")]
        public int PrefixedProperty { get; set; }

        public string? UntaggedProperty { get; set; }
    }
}
