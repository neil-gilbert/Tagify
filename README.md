# Tagify
A flexible, high-performance OpenTelemetry tag generator for .NET using source generators. Simplify your telemetry with automatic property-to-tag mapping and support for additional custom tags.

## What's it all about?

Tagify is a high-performance, flexible OpenTelemetry tag generator for .NET applications. It leverages source generators to create efficient, compile-time code for mapping your object properties to OpenTelemetry tags. Tagify focuses on explicit tagging, allowing you to precisely control which properties are included in your telemetry, while also providing the flexibility to add custom tags at runtime.

## Getting Started

1. First things first, install the NuGet package:
```
dotnet add package Tagify
```

2. Decorate your properties with the `ActionTag` attribute:

```csharp
public class UserInfo
{
    [ActionTag("user_id")]
    public int? Id { get; set; }

    [ActionTag("name", "user")]
    public string? Name { get; set; }

    public string? Email { get; set; } // This won't be tagged
}
```

3. Use the generated extension method to add tags to your Activity:
```csharp
var user = new UserInfo
{
    Id = 123,
    Name = "John Doe",
    Email = "john@example.com"
};

// Add tags from the UserInfo object
activity.AddActionTagsForUserInfo(user);

// You can also add additional custom tags
var additionalTags = new Dictionary<string, object?>
{
    { "custom.tag1", "value1" },
    { "custom.tag2", 42 }
};

activity.AddActionTagsForUserInfo(user, additionalTags);
```

And you're done! Tagify will generate an extension method that adds the tagged properties as Activity tags, along with any additional tags you provide.

## How it works
Tagify uses source generators to create specific extension methods for each of your classes containing tagged properties. This approach:

- Avoids runtime reflection for better performance
- Provides a clean, type-safe API
- Allows for better IDE support (autocomplete, etc.)
- Supports adding custom tags at runtime

## Configuration
Tagify only tags properties explicitly marked with the ActionTag attribute. This ensures that you have full control over which properties are included in your telemetry, helping to prevent accidental data leakage.

## Additional Tags
Tagify allows you to add custom tags at runtime in addition to the tags generated from your object properties. This is useful for adding context-specific tags or dynamic values that aren't part of your object model.

```csharp
var additionalTags = new Dictionary<string, object?>
{
    { "request.id", Guid.NewGuid() },
    { "environment", "production" }
};

activity.AddActionTagsForUserInfo(user, additionalTags);
```

## Type Handling
Tagify converts all tag values to strings when setting them on the Activity. This ensures consistency in the stored tag types. When retrieving tag values, they will always be returned as strings.

## Why Tagify?

- **Efficient**: Uses source generators for zero runtime reflection cost.
- **Precise**: Only tag the properties you explicitly choose.
- **Flexible**: Add custom tags at runtime for additional context.
- **Simple**: Just add an attribute and you're good to go.
- **Clean Code**: Say goodbye to repetitive tagging code cluttering up your codebase.
- **Type-Safe**: Generate extension methods specific to your classes.

## Contributing

Found a bug? Have a great idea for an improvement? Feel free to open an issue or submit a pull request.

## Testing
Tagify includes a comprehensive test suite to ensure reliability. When writing tests, remember that all tag values are stored and retrieved as strings. Don't forget to test scenarios with additional tags as well.

Happy tagging! 🏷️
