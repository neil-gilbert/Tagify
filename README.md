# Tagify
A flexible, high-performance OpenTelemetry tag generator for .NET using source generators. Simplify your telemetry with automatic property-to-tag mapping.

## What's it all about?

Tagify is a high-performance, flexible OpenTelemetry tag generator for .NET applications. It leverages source generators to create efficient, compile-time code for mapping your object properties to OpenTelemetry tags. Whether you want to tag all properties or just a select few, OtelTagify has got you covered.

## Getting Started

First things first, install the NuGet package:
```
dotnet add package Tagify
```

Next, decorate the properties you want to tag with the OtelTag attribute:
```
public class PaymentResponse
{
    [OtelTag("transaction_id")]
    public string TransactionId { get; set; }

    [OtelTag("amount", "payment")]
    public decimal Amount { get; set; }

    public string UntaggedProperty { get; set; }
}
```

Use the generated extension method to add tags to your span:
```
var response = new PaymentResponse
{
    TransactionId = "txn_123456",
    Amount = 100.50m,
    UntaggedProperty = "This won't be tagged"
};

activity?.SetTagsFromObject(response);
```

And you're done! OtelTagify will generate an extension method that adds the tagged properties as span tags.


## Why OtelTagify?

 - Efficient: Uses source generators for zero runtime reflection cost.
 - Simple: Just add an attribute and you're good to go. 
 - Clean Code: Say goodbye to repetitive tagging code cluttering up your codebase.

## Contributing

Found a bug? Have a great idea for an improvement? Feel free to open an issue or submit a pull request.

Happy tagging! 🏷️
