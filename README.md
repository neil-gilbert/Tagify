# Tagify
A flexible, high-performance OpenTelemetry tag generator for .NET using source generators. Simplify your telemetry with automatic property-to-tag mapping.

## What's it all about?

Tagify is a high-performance, flexible OpenTelemetry tag generator for .NET applications. It leverages source generators to create efficient, compile-time code for mapping your object properties to OpenTelemetry tags. Whether you want to tag all properties or just a select few, Tagify has got you covered.

## Getting Started

1. First things first, install the NuGet package:
```
dotnet add package Tagify
```

2. Decorate your classes, records, or properties with the `ActionTag` attribute:

```csharp
[ActionTag(prefix: "user")]
public class UserInfo
{
    [ActionTag("id")]
    public int Id { get; set; }

    [ActionTag("name")]
    public string Name { get; set; }

    [ActionTag("email", prefix: "contact")]
    public string Email { get; set; }

    public string Address { get; set; } // This will be tagged as "user.address"
}

[ActionTag(prefix: "product")]
public record ProductInfo
{
    [ActionTag("id")]
    public string Id { get; init; }

    [ActionTag("price", prefix: "")]
    public decimal Price { get; init; }
}
```

3. Use the generated extension method to add tags to your span:
```csharp
var user = new UserInfo
{
    Id = 123,
    Name = "John Doe",
    Email = "john@example.com",
    Address = "123 Main St"
};

activity.AddActionTagsForUserInfo(user);

var product = new ProductInfo
{
    Id = "PROD-001",
    Price = 29.99m
};

activity.AddActionTagsForProductInfo(product);
```

And you're done! Tagify will generate an extension method that adds the tagged properties as span tags.

## How it works
Tagify uses source generators to create specific extension methods for each of your tagged classes or records. This approach:

- Avoids runtime reflection for better performance
- Provides a clean, type-safe API
- Allows for better IDE support (autocomplete, etc.)

## Configuration
By default, Tagify tags all public properties of a class or record marked with the ActionTag attribute. You can customize the tagging behaviour:

- Class/Record-level prefix: Apply a prefix to all properties in a class or record.
- Property-level customization: Override the tag name or prefix for individual properties.
- Exclude properties: Properties without the ActionTag attribute are not tagged unless the class/record has an ActionTag attribute.

## Why Tagify?

- **Efficient**: Uses source generators for zero runtime reflection cost.
- **Flexible**: Tag all properties or just the ones you choose. Works with both classes and records.
- **Simple**: Just add an attribute and you're good to go.
- **Clean Code**: Say goodbye to repetitive tagging code cluttering up your codebase.
- **Customizable**: Use prefixes at the class/record level or override them for specific properties.
- **Consistent**: Tags are the same in your code base making it easier to filter/find data in your observability tooling

## Contributing

Found a bug? Have a great idea for an improvement? Feel free to open an issue or submit a pull request.

Happy tagging! 🏷️
