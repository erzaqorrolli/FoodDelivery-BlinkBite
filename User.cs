using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace FoodDeliveryyy.Models.Identity
{
    public class User : IdentityUser
    {
        public string? Name { get; set; }
        public string? Lastname { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool Active { get; set; } = true;

        
        public virtual ICollection<IdentityUserRole<string>> UserRoles { get; set; } = new List<IdentityUserRole<string>>();
        public virtual ICollection<IdentityUserClaim<string>> UserClaims { get; set; } = new List<IdentityUserClaim<string>>();
        public virtual ICollection<IdentityUserToken<string>> UserTokens { get; set; } = new List<IdentityUserToken<string>>();

        public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    }
}