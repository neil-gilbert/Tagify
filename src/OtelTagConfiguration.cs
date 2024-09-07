namespace OtelTagify
{
    /// <summary>
    /// Configuration for OtelTagify behavior.
    /// </summary>
    public static class OtelTagConfiguration
    {
        /// <summary>
        /// Gets or sets whether to tag all properties or only those with OtelTag attribute.
        /// </summary>
        public static bool TagAllProperties { get; set; } = false;
    }
}
