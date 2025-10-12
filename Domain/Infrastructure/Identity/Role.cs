using Microsoft.AspNetCore.Identity;

namespace Domain.Infrastructure.Identity;

public class Role : IdentityRole
{
    public ICollection<UserRole> UserRoles { get; set; }
}

public class UserRole : IdentityUserRole<string>

{
    public ApplicationUser User { get; set; }

    public Role Role { get; set; }
}

public class UserLogin : IdentityUserLogin<string>

{
    public ApplicationUser User { get; set; }
}

public class UserToken : IdentityUserToken<string>

{
    public ApplicationUser User { get; set; }
}