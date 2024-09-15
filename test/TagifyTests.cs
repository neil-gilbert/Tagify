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
            Email = "john@example.com"
        };
        var activity = new Activity("TestActivity");
        activity.Start();

        activity.AddActionTagsForUserInfo(user);

        Assert.Equal("123", activity.GetTagItem("user_id")?.ToString());
        Assert.Equal("John Doe", activity.GetTagItem("user.name")?.ToString());
        Assert.Null(activity.GetTagItem("Email")); // Email should not be tagged
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

        Assert.Equal("PROD-001", activity.GetTagItem("product_id")?.ToString());
        Assert.Equal("29.99", activity.GetTagItem("price")?.ToString());
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

        Assert.Null(activity.GetTagItem("user_id"));
        Assert.Null(activity.GetTagItem("user.name"));
    }
}
