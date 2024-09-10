using System.Diagnostics;

namespace Tagify.Tests;

public class TagifyTests
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
    [ActionTag("tagged_property")]
    public string? TaggedProperty { get; set; }

    [ActionTag("prefixed_property", "prefix")]
    public int PrefixedProperty { get; set; }

    public string? UntaggedProperty { get; set; }
}
