using System.Diagnostics;
using Xunit;

namespace Tagify.Tests;

public class TagifyTests
{
    [Fact]
    public void AddTagsForUserInfo_ShouldAddCorrectTags()
    {
        var user = new UserInfo
        {
            Id = 123,
            Name = "John Doe",
            Email = "john@example.com",
            Address = "123 Main St"
        };
        var activity = new Activity("TestActivity");
        activity.Start();

        activity.AddActionTagsForUserInfo(user);

        Assert.Equal("123", activity.GetTagItem("user.id"));
        Assert.Equal("John Doe", activity.GetTagItem("user.name"));
        Assert.Equal("john@example.com", activity.GetTagItem("contact.email"));
        Assert.Equal("123 Main St", activity.GetTagItem("user.address"));
    }

    [Fact]
    public void AddTagsForProductInfo_ShouldAddCorrectTags()
    {
        var product = new ProductInfo
        {
            Id = "PROD-001",
            Price = 29.99m
        };
        var activity = new Activity("TestActivity");
        activity.Start();

        activity.AddActionTagsForProductInfo(product);

        Assert.Equal("PROD-001", activity.GetTagItem("product_id"));
        Assert.Equal("29.99", activity.GetTagItem("price"));
    }

    [Fact]
    public void AddTags_WithNullObject_ShouldNotThrowException()
    {
        UserInfo user = null;
        var activity = new Activity("TestActivity");
        activity.Start();
        
        var exception = Record.Exception(() => activity.AddActionTagsForUserInfo(user));
        Assert.Null(exception);
    }

    [Fact]
    public void AddTags_WithNullProperties_ShouldNotAddTags()
    {
        var user = new UserInfo(); // All properties will be null or default
        var activity = new Activity("TestActivity");
        activity.Start();
        
        activity.AddActionTagsForUserInfo(user);

        Assert.Null(activity.GetTagItem("user.id"));
        Assert.Null(activity.GetTagItem("user.name"));
        Assert.Null(activity.GetTagItem("contact.email"));
        Assert.Null(activity.GetTagItem("user.address"));
    }

    [Fact]
    public void AddTagsForClassWithPrefix_ShouldRespectPrefixOverrides()
    {
        var item = new ItemWithPrefixOverrides
        {
            Id = "ITEM-001",
            Name = "Test Item",
            Category = "Electronics",
            Price = 99.99m
        };
        var activity = new Activity("TestActivity");
        activity.Start();

        activity.AddActionTagsForItemWithPrefixOverrides(item);

        Assert.Equal("ITEM-001", activity.GetTagItem("item.id"));
        Assert.Equal("Test Item", activity.GetTagItem("item.name"));
        Assert.Equal("Electronics", activity.GetTagItem("metadata.category"));
        Assert.Equal("99.99", activity.GetTagItem("price"));
    }
}

[ActionTag(prefix: "item")]
public class ItemWithPrefixOverrides
{
    [ActionTag("id")]
    public string Id { get; set; }

    [ActionTag("name")]
    public string Name { get; set; }

    [ActionTag("category", prefix: "metadata")]
    public string Category { get; set; }

    [ActionTag("price", prefix: "")]
    public decimal Price { get; set; }
}
