namespace Tagify;

/// <summary>
/// Attribute to specify OpenTelemetry tag details for a property.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property)]
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
    /// <param name="name">The name of the tag.</param>
    /// <param name="prefix">The prefix for the tag.</param>
    public ActionTagAttribute(string? name = null, string? prefix = null)
    {
        Name = name;
        Prefix = prefix;
    }
}
