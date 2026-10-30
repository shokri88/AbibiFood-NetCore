using Microsoft.AspNetCore.Identity;
namespace AbibiFood.Infrastructure.Identity;

public sealed class AppUser : IdentityUser
{
    public string DisplayName { get; set; } = "";
    public bool IsActive { get; set; } = true;
}
