# Tagify
A flexible, high-performance OpenTelemetry tag generator for .NET using source generators. Simplify your telemetry with automatic property-to-tag mapping.

## What's it all about?

Tagify is a high-performance, flexible OpenTelemetry tag generator for .NET applications. It leverages source generators to create efficient, compile-time code for mapping your object properties to OpenTelemetry tags. Whether you want to tag all properties or just a select few, OtelTagify has got you covered.

## Getting Started

1. First things first, install the NuGet package:
```
dotnet add package Tagify
```

2. Decorate your classes or properties with the `OtelTag` attribute:

```csharp
[ActionTag]
public class UserInfo
{
    [ActionTag("user_id")]
    public int Id { get; set; }

    [ActionTag("name", "user")]
    public string Name { get; set; }

    public string Email { get; set; } // This won't be tagged
}
```

3. Use the generated extension method to add tags to your span:
```
var user = new UserInfo
{
    Id = 123,
    Name = "John Doe",
    Email = "john@example.com"
};

activity.AddActionTagsForUserInfo(user);
```

And you're done! Tagify will generate an extension method that adds the tagged properties as span tags.

## How it works
Tagify uses source generators to create specific extension methods for each of your tagged classes. This approach:

Avoids runtime reflection for better performance
Provides a clean, type-safe API
Allows for better IDE support (autocomplete, etc.)

## Configuration
By default, Tagify only tags properties marked with the ActionTag attribute. If you want to tag all public properties of a class, you can add the ActionTag attribute to the class itself (This approach is not recommended due to leaking information):

## Why Tagify?

- **Efficient**: Uses source generators for zero runtime reflection cost.
- **Flexible**: Tag all properties or just the ones you choose.
- **Simple**: Just add an attribute and you're good to go.
- **Clean Code**: Say goodbye to repetitive tagging code cluttering up your codebase.

## Contributing

Found a bug? Have a great idea for an improvement? Feel free to open an issue or submit a pull request.

Happy tagging! 🏷️
