using System.Diagnostics;
using System.Globalization;
using Xunit;

namespace Tagify.Tests;

public class TagifyTests
{
    [Fact]
    public void AddTagsForUserInfoShouldAddCorrectTags()
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

        Assert.Equal("123", activity.GetTagItem("user.id")?.ToString());
        Assert.Equal("John Doe", activity.GetTagItem("user.name"));
        Assert.Equal("john@example.com", activity.GetTagItem("contact.email"));
        Assert.Equal("123 Main St", activity.GetTagItem("user.address"));
    }

    [Fact]
    public void AddTagsForProductInfoShouldAddCorrectTags()
    {
        var product = new ProductInfo
        {
            Id = "PROD-001",
            Price = 29.99m
        };
        var activity = new Activity("TestActivity");
        activity.Start();

        activity.AddActionTagsForProductInfo(product);

        Assert.Equal("PROD-001", activity.GetTagItem("product.id"));
        AssertDecimalEqual(29.99m, activity.GetTagItem("price")?.ToString());
    }

    [Fact]
    public void AddTagsWithNullObjectShouldNotThrowException()
    {
        UserInfo? user = null;
        var activity = new Activity("TestActivity");
        activity.Start();
        
        var exception = Record.Exception(() => activity.AddActionTagsForUserInfo(user));
        Assert.Null(exception);
    }

    [Fact]
    public void AddTagsWithNullPropertiesShouldNotAddTags()
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
    public void AddTagsForClassWithPrefixShouldRespectPrefixOverrides()
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
        AssertDecimalEqual(99.99m, activity.GetTagItem("price")?.ToString());
    }

    [Fact]
    public void AddTagsForRecordTypeShouldAddCorrectTags()
    {
        var person = new PersonRecord
        {
            Id = 456,
            Name = "Jane Doe",
            Email = "jane@example.com"
        };
        var activity = new Activity("TestActivity");
        activity.Start();

        activity.AddActionTagsForPersonRecord(person);

        Assert.Equal("456", activity.GetTagItem("person.id")?.ToString());
        Assert.Equal("Jane Doe", activity.GetTagItem("person.name")?.ToString());
        Assert.Equal("jane@example.com", activity.GetTagItem("contact.email")?.ToString());
    }

    private static void AssertDecimalEqual(decimal expected, string? actual)
    {
        Assert.True(decimal.TryParse(actual, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal actualDecimal),
            $"Failed to parse '{actual}' as decimal");
        Assert.Equal(expected, actualDecimal);
    }
}

[ActionTag(prefix: "item")]
public class ItemWithPrefixOverrides
{
    [ActionTag("id")]
    public string? Id { get; set; }

    [ActionTag("name")]
    public string? Name { get; set; }

    [ActionTag("category", prefix: "metadata")]
    public string? Category { get; set; }

    [ActionTag("price", prefix: "")]
    public decimal Price { get; set; }
}

[ActionTag(prefix: "person")]
public record PersonRecord
{
    [ActionTag("id")]
    public int Id { get; init; }

    [ActionTag("name")]
    public string? Name { get; init; }

    [ActionTag("email", prefix: "contact")]
    public string? Email { get; init; }
}

[ActionTag(prefix: "user")]
public record UserRecord
{
    [ActionTag("id")]
    public int Id { get; init; }

    [ActionTag("name")]
    public string? Name { get; init; }
    
    [ActionTag("address")]
    public AddressRecord? Address { get; set; }
}

[ActionTag(prefix: "address")]
public record AddressRecord
{
    [ActionTag("id")]
    public int Id { get; init; }
}
