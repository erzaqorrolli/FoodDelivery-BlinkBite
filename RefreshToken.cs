using System;

namespace FoodDeliveryyy.Models.Identity
{
    public class RefreshToken
    {
        public int Id { get; set; }
        public string Token { get; set; } = null!;
        public DateTime Expires { get; set; }
        public DateTime Created { get; set; } = DateTime.UtcNow;
        public DateTime? Revoked { get; set; }

        
        public string UserId { get; set; } = null!;
        public virtual User User { get; set; } = null!;
    }
}