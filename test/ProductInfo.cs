namespace Tagify.Tests;

[ActionTag(prefix: "product")]
public class ProductInfo
{
    [ActionTag("id")]
    public string Id { get; set; }

    [ActionTag("price", prefix: "")]
    public decimal Price { get; set; }
}
