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

## Nested properties
Tagify supports tagging of nested objects (classes/records/structs) as long as the nested type is also discoverable by the generator (i.e., it has an `ActionTag` at the class level or on at least one of its public properties).

Example:

```csharp
[ActionTag(prefix: "user")]
public record UserRecord
{
    [ActionTag("id")] public int Id { get; init; }
    [ActionTag("name")] public string? Name { get; init; }

    // Property name for the nested hierarchy segment
    [ActionTag("address")] public AddressRecord? Address { get; init; }
}

[ActionTag(prefix: "address")]
public record AddressRecord
{
    [ActionTag("id")] public int Id { get; init; }
    [ActionTag("street")] public string? Street { get; init; }
}

var user = new UserRecord
{
    Id = 123,
    Name = "John Doe",
    Address = new AddressRecord { Id = 456, Street = "123 Main St" }
};

activity.AddActionTagsForUserRecord(user);
// Tags set:
// user.id = 123
// user.name = "John Doe"
// user.address.id = 456
// user.address.street = "123 Main St"
```

Notes and rules:
- Null handling:
  - Reference types are only tagged when not null.
  - Nullable value types (e.g., `int?`) are tagged only when `HasValue`.
  - Non-nullable value types are always tagged.
- Class prefix composition:
  - A class/record-level `prefix` composes with the parent prefix when nested. Example above: `user` (parent) + `address` (child) → `user.address`.
- Property-level `prefix` overrides the class prefix for that property:
  - Not specified (null): use the current class prefix. Example: `user.id`.
  - Empty string (`""`): ignore the class prefix and emit just the tag name for primitives (e.g., `price`). For nested properties, this resets to the nested class prefix (i.e., ignores the parent).
  - Non-empty (e.g., `"metadata"`): replace the class prefix with that value. For nested properties, this becomes the parent of the nested class prefix (e.g., `metadata.address.id`).
- Additional tags: every generated method accepts `additionalTags` so you can mix in arbitrary tags alongside generated ones.

### Nested overrides examples

1) Reset to nested class prefix (ignore parent):

```csharp
[ActionTag(prefix: "user")]
public record UserRecord
{
    [ActionTag("id")] public int Id { get; init; }

    // Reset parent prefix for nested tags: becomes "address.id", "address.street"
    [ActionTag("address", prefix: "")] public AddressRecord? Address { get; init; }
}

[ActionTag(prefix: "address")]
public record AddressRecord
{
    [ActionTag("id")] public int Id { get; init; }
    [ActionTag("street")] public string? Street { get; init; }
}

activity.AddActionTagsForUserRecord(new UserRecord
{
    Id = 1,
    Address = new AddressRecord { Id = 10, Street = "Main" }
});
// Tags: address.id=10, address.street="Main" (no "user." prefix)
```

2) Route nested tags under a custom root:

```csharp
[ActionTag(prefix: "user")]
public record UserRecord
{
    [ActionTag("id")] public int Id { get; init; }

    // Place nested under a custom root: becomes "location.address.id"
    [ActionTag("address", prefix: "location")] public AddressRecord? Address { get; init; }
}

[ActionTag(prefix: "address")]
public record AddressRecord
{
    [ActionTag("id")] public int Id { get; init; }
    [ActionTag("street")] public string? Street { get; init; }
}

activity.AddActionTagsForUserRecord(new UserRecord
{
    Id = 1,
    Address = new AddressRecord { Id = 10, Street = "Main" }
});
// Tags: location.address.id=10, location.address.street="Main"
```

## Troubleshooting
- No tags generated or methods missing:
  - Ensure your types are `public` and have `ActionTag` on the class or at least one `public` property.
  - Nested types must also be discoverable (class-level `ActionTag` or at least one annotated property).
- Unexpected prefixes:
  - Property `prefix` overrides the class prefix. `prefix: ""` removes the class prefix; a non-empty value replaces it.
  - For nested properties, `prefix: ""` resets to the nested class’s own prefix; a non-empty value becomes the parent of the nested class prefix.
- Null handling surprises:
  - Reference types tag only when not null; `Nullable<T>` only when `HasValue`; non-nullable value types always tag.

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
