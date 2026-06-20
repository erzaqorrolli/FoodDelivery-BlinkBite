using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;

namespace FoodDeliveryyy.Models.Identity
{
    public class Role : IdentityRole
    {
        public string? Description { get; set; }

        
        public virtual ICollection<IdentityUserRole<string>> UserRoles { get; set; } = new List<IdentityUserRole<string>>();
    }
}