namespace Tagify;

/// <summary>
/// Attribute to specify OpenTelemetry tag details for a property or class.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public class ActionTagAttribute : Attribute
{
    /// <summary>
    /// Gets the name of the tag.
    /// </summary>
    public string? Name { get; }

    /// <summary>
    /// Gets the prefix for the tag.
    /// </summary>
    public string? Prefix { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ActionTagAttribute"/> class.
    /// </summary>
    /// <param name="name">The name of the tag. If not provided for a class, all properties will be tagged.</param>
    /// <param name="prefix">The prefix for the tag. If set on a class, it applies to all tagged properties unless overridden.</param>
    public ActionTagAttribute(string? name = null, string? prefix = null)
    {
        Name = name;
        Prefix = prefix;
    }
}
