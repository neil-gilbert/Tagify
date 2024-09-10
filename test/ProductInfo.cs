namespace Tagify.Tests;

public class ProductInfo
{
    [ActionTag("product_id")]
    public string Id { get; set; }

    [ActionTag("price")]
    public decimal Price { get; set; }
}
