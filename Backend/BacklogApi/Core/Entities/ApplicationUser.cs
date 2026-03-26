using Microsoft.AspNetCore.Identity;

namespace BacklogApi.Core.Entities;

public class ApplicationUser : IdentityUser
{
    public string? Tags { get; set; }
    public string? ProfilePicture { get; set; }
}
