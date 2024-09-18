namespace Tagify.Tests;

[ActionTag(prefix: "user")]
public class UserInfo
{
    [ActionTag("id")]
    public int? Id { get; set; }

    [ActionTag("name")]
    public string? Name { get; set; }

    [ActionTag("email", prefix: "contact")]
    public string? Email { get; set; }

    public string? Address { get; set; }
}
