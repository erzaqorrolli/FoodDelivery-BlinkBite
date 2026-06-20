namespace FoodDeliveryyy.Models.Identity;

public static class AppRoles
{
    public const string Admin = "Admin";
    public const string Merchant = "Merchant";
    public const string BranchManager = "BranchManager";
    public const string Courier = "Courier";
    public const string Customer = "Customer";

    public const string LegacyRestaurantOwner = "RestaurantOwner";
    public const string LegacyDriver = "Driver";

    public static string Normalize(string? role)
    {
        if (string.IsNullOrWhiteSpace(role)) return Customer;

        var trimmed = role.Trim();
        if (trimmed.Equals(LegacyRestaurantOwner, System.StringComparison.OrdinalIgnoreCase)) return Merchant;
        if (trimmed.Equals(LegacyDriver, System.StringComparison.OrdinalIgnoreCase)) return Courier;

        if (trimmed.Equals(Admin, System.StringComparison.OrdinalIgnoreCase)) return Admin;
        if (trimmed.Equals(Merchant, System.StringComparison.OrdinalIgnoreCase)) return Merchant;
        if (trimmed.Equals(BranchManager, System.StringComparison.OrdinalIgnoreCase)) return BranchManager;
        if (trimmed.Equals(Courier, System.StringComparison.OrdinalIgnoreCase)) return Courier;
        if (trimmed.Equals(Customer, System.StringComparison.OrdinalIgnoreCase)) return Customer;

        return trimmed;
    }
}
