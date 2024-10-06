using System.Diagnostics;
using Xunit;

namespace Tagify.Tests;

[ActionTag(prefix: "user")]
public record UserRecord
{
    [ActionTag("id")]
    public int Id { get; init; }

    [ActionTag("name")]
    public string? Name { get; init; }
    
    [ActionTag("address")]
    public AddressRecord? Address { get; init; }
}

[ActionTag(prefix: "address")]
public record AddressRecord
{
    [ActionTag("id")]
    public int Id { get; init; }

    [ActionTag("street")]
    public string? Street { get; init; }
}

public class NestedObjectTagifyTests
{
    [Fact]
    public void AddTagsForUserRecord_ShouldAddCorrectTags()
    {
        var user = new UserRecord
        {
            Id = 123,
            Name = "John Doe",
            Address = new AddressRecord
            {
                Id = 456,
                Street = "123 Main St"
            }
        };
        var activity = new Activity("TestActivity");
        activity.Start();

        activity.AddActionTagsForUserRecord(user);

        Assert.Equal("123", activity.GetTagItem("user.id"));
        Assert.Equal("John Doe", activity.GetTagItem("user.name"));
        Assert.Equal("456", activity.GetTagItem("user.address.id"));
        Assert.Equal("123 Main St", activity.GetTagItem("user.address.street"));
    }

    [Fact]
    public void AddTagsForUserRecord_WithNullNestedObject_ShouldNotAddNestedTags()
    {
        var user = new UserRecord
        {
            Id = 123,
            Name = "John Doe",
            Address = null
        };
        var activity = new Activity("TestActivity");
        activity.Start();

        activity.AddActionTagsForUserRecord(user);

        Assert.Equal("123", activity.GetTagItem("user.id"));
        Assert.Equal("John Doe", activity.GetTagItem("user.name"));
        Assert.Null(activity.GetTagItem("user.address.id"));
        Assert.Null(activity.GetTagItem("user.address.street"));
    }

    [Fact]
    public void AddTagsForAddressRecord_ShouldAddCorrectTags()
    {
        var address = new AddressRecord
        {
            Id = 789,
            Street = "456 Elm St"
        };
        var activity = new Activity("TestActivity");
        activity.Start();

        activity.AddActionTagsForAddressRecord(address);

        Assert.Equal("789", activity.GetTagItem("address.id"));
        Assert.Equal("456 Elm St", activity.GetTagItem("address.street"));
    }

    [Fact]
    public void AddTagsForUserRecord_WithAdditionalTags_ShouldAddAllTags()
    {
        var user = new UserRecord
        {
            Id = 123,
            Name = "John Doe",
            Address = new AddressRecord
            {
                Id = 456,
                Street = "123 Main St"
            }
        };
        var activity = new Activity("TestActivity");
        activity.Start();

        var additionalTags = new Dictionary<string, object?>
        {
            ["custom.tag"] = "custom value",
            ["another.tag"] = 42
        };

        activity.AddActionTagsForUserRecord(user, additionalTags: additionalTags);

        Assert.Equal("123", activity.GetTagItem("user.id"));
        Assert.Equal("John Doe", activity.GetTagItem("user.name"));
        Assert.Equal("456", activity.GetTagItem("user.address.id"));
        Assert.Equal("123 Main St", activity.GetTagItem("user.address.street"));
        Assert.Equal("custom value", activity.GetTagItem("custom.tag"));
        Assert.Equal("42", activity.GetTagItem("another.tag"));
    }
}
