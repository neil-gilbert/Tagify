namespace Tagify.Tests;

[ActionTag]
public class UserInfo
{
    [ActionTag("user_id")]
    public int Id { get; set; }

    [ActionTag("name", "user")]
    public string Name { get; set; }

    public string Email { get; set; }
}
