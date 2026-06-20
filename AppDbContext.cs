using FoodDeliveryyy.Models.Entities;
using FoodDeliveryyy.Models.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Reflection.Emit;

namespace FoodDeliveryyy.Data
{
    public class AppDbContext : IdentityDbContext<User, Role, string>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Restaurant> Restaurants { get; set; } = null!;
        public DbSet<Category> Categories { get; set; } = null!;
        public DbSet<RestaurantAddress> RestaurantAddresses { get; set; } = null!;
        public DbSet<MenuCategory> MenuCategories { get; set; } = null!;
        public DbSet<MenuItems> MenuItems { get; set; } = null!;
        public DbSet<Orders> Orders { get; set; } = null!;
        public DbSet<OrderItems> OrderItems { get; set; } = null!;
        public DbSet<DeliveryDrivers> DeliveryDrivers { get; set; } = null!;
        public DbSet<Deliveries> Deliveries { get; set; } = null!;
        public DbSet<Reviews> Reviews { get; set; } = null!;
        public DbSet<Addresses> Addresses { get; set; } = null!;
        public DbSet<MenuItemBranch> MenuItemBranch { get; set; } = null!;
        public DbSet<Promotions> Promotions { get; set; } = null!;
        public DbSet<UserFavorite> UserFavorites { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; } = null!;
        public DbSet<OrderStatusHistory> OrderStatusHistories { get; set; }
        public DbSet<Invoice> Invoices { get; set; }
        public DbSet<RestaurantApplication> RestaurantApplications { get; set; } = null!;
        public DbSet<CourierApplication> CourierApplications { get; set; } = null!;
        public DbSet<BranchModificationRequest> BranchModificationRequests { get; set; }

        public DbSet<BranchApplication> BranchApplications { get; set; }

        public DbSet<CustomerAddress> CustomerAddresses { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Restaurant>()
                .Property(r => r.OrariHapjes)
                .HasConversion(v => v.ToString(), v => TimeOnly.Parse(v));

            builder.Entity<Restaurant>()
                .Property(r => r.OrariMbylljes)
                .HasConversion(v => v.ToString(), v => TimeOnly.Parse(v));

            builder.Entity<Orders>()
                .Property(o => o.Statusi)
                .HasConversion<string>();

            builder.Entity<Deliveries>()
                .Property(d => d.Statusi)
                .HasConversion<string>();

            builder.Entity<DeliveryDrivers>()
                .Property(d => d.Statusi)
                .HasConversion<string>();

            builder.Entity<Promotions>()
                .Property(p => p.Statusi)
                .HasConversion<string>();

            builder.Entity<Restaurant>()
                .Property(r => r.Statusi)
                .HasConversion<string>();

            builder.Entity<Category>()
                .HasIndex(c => c.Name)
                .IsUnique();

            builder.Entity<Restaurant>()
                .HasOne(r => r.Category)
                .WithMany(c => c.Restaurants)
                .HasForeignKey(r => r.CategoryId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Entity<RestaurantAddress>()
                .HasIndex(r => r.Qyteti);

            builder.Entity<RestaurantAddress>()
                .HasIndex(r => r.MerchantUserId);

            builder.Entity<RestaurantAddress>()
                .HasOne(r => r.MerchantUser)
                .WithMany()
                .HasForeignKey(r => r.MerchantUserId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Entity<MenuItems>()
                .HasIndex(mi => mi.RestaurantAddressId);

            builder.Entity<MenuItems>()
                .HasOne(mi => mi.RestaurantAddress)
                .WithMany()
                .HasForeignKey(mi => mi.RestaurantAddressId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Entity<Orders>()
                .HasIndex(o => o.RestaurantAddressId);

            builder.Entity<Orders>()
                .HasOne(o => o.RestaurantAddress)
                .WithMany()
                .HasForeignKey(o => o.RestaurantAddressId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Entity<Promotions>()
                .HasIndex(p => p.RestaurantAddressId);

            builder.Entity<Promotions>()
                .HasOne(p => p.RestaurantAddress)
                .WithMany()
                .HasForeignKey(p => p.RestaurantAddressId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Entity<Orders>()
                .HasIndex(o => o.Statusi);

            builder.Entity<Orders>()
                .HasIndex(o => o.UserId);

            builder.Entity<Reviews>()
                .HasIndex(r => r.RestaurantId);

            builder.Entity<IdentityUserRole<string>>()
                   .HasKey(ur => new { ur.UserId, ur.RoleId });

            builder.Entity<IdentityUserClaim<string>>()
                   .HasOne<User>()
                   .WithMany(u => u.UserClaims)
                   .HasForeignKey(uc => uc.UserId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<IdentityUserToken<string>>()
                   .HasOne<User>()
                   .WithMany(u => u.UserTokens)
                   .HasForeignKey(ut => ut.UserId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<IdentityUserRole<string>>()
                   .HasOne<User>()
                   .WithMany(u => u.UserRoles)
                   .HasForeignKey(ur => ur.UserId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<IdentityUserRole<string>>()
                   .HasOne<Role>()
                   .WithMany(r => r.UserRoles)
                   .HasForeignKey(ur => ur.RoleId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<RefreshToken>()
                   .HasOne(r => r.User)
                   .WithMany(u => u.RefreshTokens)
                   .HasForeignKey(r => r.UserId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<UserFavorite>()
                .HasIndex(uf => new { uf.UserId, uf.MenuItemId })
                .IsUnique()
                .HasDatabaseName("IX_UserFavorites_User_MenuItem");

            builder.Entity<UserFavorite>()
                .HasIndex(uf => new { uf.UserId, uf.RestaurantId })
                .IsUnique()
                .HasDatabaseName("IX_UserFavorites_User_Restaurant");

            // Konfigurimi i saktë për MenuItemBranch
            builder.Entity<MenuItemBranch>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.HasOne(e => e.MenuItem)
                    .WithMany(m => m.BranchDetails)  // Lidhja e saktë me BranchDetails
                    .HasForeignKey(e => e.MenuItemId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.RestaurantAddress)
                    .WithMany()
                    .HasForeignKey(e => e.RestaurantAddressId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}