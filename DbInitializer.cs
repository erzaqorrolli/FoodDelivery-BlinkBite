using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using FoodDeliveryyy.Data;
using FoodDeliveryyy.Models.Entities;
using FoodDeliveryyy.Models.Identity;
using FoodDeliveryyy.Models.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;


public static class DbInitializer
{
    public static async Task InitializeAsync(AppDbContext context,
                                             UserManager<User> userManager,
                                             RoleManager<Role> roleManager)
    {
        Console.WriteLine("INITIALIZER RUNNING");

        await context.Database.MigrateAsync();

        var roles = new[] { AppRoles.Admin, AppRoles.Merchant, AppRoles.BranchManager, AppRoles.Courier, AppRoles.Customer };
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new Role { Name = role, Description = $"{role} role" });
                Console.WriteLine($"Role {role} created ✅");
            }
        }

        await MigrateLegacyRoleAsync(roleManager, userManager, AppRoles.LegacyRestaurantOwner, AppRoles.Merchant);
        await MigrateLegacyRoleAsync(roleManager, userManager, AppRoles.LegacyDriver, AppRoles.Courier);

        var adminUser = await userManager.FindByNameAsync("admin");
        if (adminUser == null)
        {
            var admin = new User
            {
                UserName = "admin",
                Email = "admin@example.com",
                EmailConfirmed = true
            };
            var result = await userManager.CreateAsync(admin, "Admin@1234");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(admin, AppRoles.Admin);
                adminUser = admin;
                Console.WriteLine("Admin user created ");
            }
        }

        var merchantUsers = new List<User>();
        var merchantSeeds = new[]
        {
            new { Legacy = "merchant1", Username = "sushico", Email = "sushico@example.com" },
            new { Legacy = "merchant2", Username = "burgerking", Email = "burgerking@example.com" },
            new { Legacy = "merchant3", Username = "pastafasta", Email = "pastafasta@example.com" },
            new { Legacy = "merchant4", Username = "properpizza", Email = "properpizza@example.com" },
            new { Legacy = "merchant5", Username = "kfc", Email = "kfc@example.com" },
            new { Legacy = "merchant6", Username = "greenandprotein", Email = "greenandprotein@example.com" },
            new { Legacy = "merchant7", Username = "myshawarma", Email = "myshawarma@example.com" },
            new { Legacy = "merchant8", Username = "heavyhit", Email = "heavyhit@example.com" },
            new { Legacy = "merchant9", Username = "popeyes", Email = "popeyes@example.com" },
            new { Legacy = "merchant10", Username = "merchant10", Email = "merchant10@example.com" },
            new { Legacy = "merchant11", Username = "agusholli", Email = "agusholli@example.com" },
            new { Legacy = "merchant12", Username = "saraysweets", Email = "saraysweets@example.com" },
            new { Legacy = "merchant13", Username = "capvin13", Email = "capvin13@example.com" },
            new { Legacy = "merchant14", Username = "fikaeatery", Email = "fikaeatery@example.com" },
            new { Legacy = "merchant15", Username = "mulliri", Email = "mulliri@example.com" },
            new { Legacy = "merchant16", Username = "gjikschiks", Email = "gjikschiks@example.com" },
            new { Legacy = "merchant17", Username = "smashburgerco", Email = "smashburgerco@example.com" },
            new { Legacy = "merchant18", Username = "buffaloburgers", Email = "buffaloburgers@example.com" },
            new { Legacy = "merchant19", Username = "hookfishchips", Email = "hookfishchips@example.com" },
            new { Legacy = "merchant20", Username = "frix", Email = "frix@example.com" }
        };

        foreach (var seed in merchantSeeds)
        {
            var user = await userManager.FindByNameAsync(seed.Username);

            if (user == null)
            {
                user = await userManager.FindByNameAsync(seed.Legacy);
            }

            if (user == null)
            {
                user = new User
                {
                    UserName = seed.Username,
                    Email = seed.Email,
                    EmailConfirmed = true
                };
                var createResult = await userManager.CreateAsync(user, "Merchant@1234");
                if (!createResult.Succeeded)
                {
                    throw new InvalidOperationException($"Failed to create merchant user {seed.Username}");
                }
                Console.WriteLine($"Merchant user {seed.Username} created");
            }
            else
            {
                var changed = false;

                if (!string.Equals(user.UserName, seed.Username, StringComparison.OrdinalIgnoreCase))
                {
                    user.UserName = seed.Username;
                    changed = true;
                }

                if (!string.Equals(user.Email, seed.Email, StringComparison.OrdinalIgnoreCase))
                {
                    user.Email = seed.Email;
                    changed = true;
                }

                if (!user.EmailConfirmed)
                {
                    user.EmailConfirmed = true;
                    changed = true;
                }

                if (changed)
                {
                    var updateResult = await userManager.UpdateAsync(user);
                    if (!updateResult.Succeeded)
                    {
                        throw new InvalidOperationException($"Failed to update merchant user {seed.Username}");
                    }
                }
            }

            if (!await userManager.IsInRoleAsync(user, AppRoles.Merchant))
            {
                await userManager.AddToRoleAsync(user, AppRoles.Merchant);
            }

            merchantUsers.Add(user);
        }

        var courierUser = await userManager.FindByNameAsync("courier");
        if (courierUser == null)
        {
            var courier = new User
            {
                UserName = "courier",
                Email = "courier@example.com",
                EmailConfirmed = true
            };
            var result = await userManager.CreateAsync(courier, "Courier@1234");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(courier, AppRoles.Courier);
                courierUser = courier;
                Console.WriteLine("Courier user created ");
            }
        }

        var customerUser = await userManager.FindByNameAsync("customer");
        if (customerUser == null)
        {
            var customer = new User
            {
                UserName = "customer",
                Email = "customer@example.com",
                EmailConfirmed = true
            };
            var result = await userManager.CreateAsync(customer, "Customer@1234");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(customer, AppRoles.Customer);
                customerUser = customer;
                Console.WriteLine("Customer user created ");
            }
        }

        var seedCategories = new Category[]
        {
            new Category { Name = "Sushi", ImageUrl = "/uploads/categories/sushiIcon.png" },
            new Category { Name = "Fast Food", ImageUrl = "/uploads/categories/fastFoodIcon.png" },
            new Category { Name = "Italian", ImageUrl = "/uploads/categories/ItalianIcon.png" },
            new Category { Name = "Pizza", ImageUrl = "/uploads/categories/pizzaIcon.png" },
            new Category { Name = "Healthy", ImageUrl = "/uploads/categories/healthyIcon.png" },
            new Category { Name = "Shawarma", ImageUrl = "/uploads/categories/shawarmaIcon.png" },
            new Category { Name = "Burgers", ImageUrl = "/uploads/categories/burgersIcon.png" },
            new Category { Name = "Dessert", ImageUrl = "/uploads/categories/dessertIcon.png" },
            new Category { Name = "Traditional", ImageUrl = "/uploads/categories/TraditionalIcon.png" },

            new Category { Name = "Seafood", ImageUrl = "/uploads/categories/seafoodIcon.png" },
            new Category { Name = "Korean", ImageUrl = "/uploads/categories/koreanIcon.png" }
        };

        var categoriesToAdd = seedCategories
            .Where(c => !context.Categories.Any(db => db.Name == c.Name))
            .ToArray();

        if (categoriesToAdd.Any())
        {
            context.Categories.AddRange(categoriesToAdd);
            await context.SaveChangesAsync();
            Console.WriteLine($"Inserted {categoriesToAdd.Length} categories");
        }

        var seedRestaurants = new Restaurant[]
        {
            new Restaurant { Emertimi = "SushiCo", Pershkrimi = "SuchiCo – Fresh sushi...", Telefoni = "+383 49 000 000", Email = "info@sushicokosova.com", Logo = "/uploads/logos/sushico1.png", OrariHapjes = new TimeOnly(10, 0), OrariMbylljes = new TimeOnly(0, 0), Rating = 4.5m, Statusi = RestaurantStatus.Active, UserId =merchantUsers[0].Id, Kategori = "Sushi" },
            new Restaurant { Emertimi = "Burger King", Pershkrimi = "Flame-grilled burgers...", Telefoni = "+383 49 000 000", Email = "info@burgerking.com", Logo = "/uploads/logos/BurgerKingLogo.png", OrariHapjes = new TimeOnly(10, 0), OrariMbylljes = new TimeOnly(0, 0), Rating = 4.5m, Statusi = RestaurantStatus.Active, UserId =merchantUsers[1].Id, Kategori = "Fast Food" },

            new Restaurant { Emertimi = "Pasta Fasta", Pershkrimi = "Delicious pasta", Telefoni = "+383 49 111 000", Email = "info@pastafasta.com", Logo = "/uploads/logos/PastaFastaLogo.png", OrariHapjes = new TimeOnly(10, 0), OrariMbylljes = new TimeOnly(0, 0), Rating = 4.5m, Statusi = RestaurantStatus.Active, UserId =merchantUsers[2].Id, Kategori = "Italian" },
            new Restaurant { Emertimi = "Proper Pizza", Pershkrimi = "Fresh pizza", Telefoni = "+383 49 000 100", Email = "info@properpizaaks.com", Logo = "/uploads/logos/PropperPizzaLogo.png", OrariHapjes = new TimeOnly(10, 0), OrariMbylljes = new TimeOnly(0, 0), Rating = 4.5m, Statusi = RestaurantStatus.Active, UserId =merchantUsers[3].Id, Kategori = "Pizza" },
            new Restaurant { Emertimi = "KFC", Pershkrimi = "Fameous fastfood", Telefoni = "+383 49 222 000", Email = "info@kfc-ks.com", Logo = "/uploads/logos/KfcLogo.png", OrariHapjes = new TimeOnly(10, 0), OrariMbylljes = new TimeOnly(0, 0), Rating = 4.5m, Statusi = RestaurantStatus.Active, UserId =  merchantUsers[4].Id, Kategori = "Fast Food" },
            new Restaurant { Emertimi = "Green and Protein", Pershkrimi = "Delicous healthy meals", Telefoni = "+383 49 000 000", Email = "info@greenandproteinks.com", Logo = "/uploads/logos/GreenProteinLogo.png", OrariHapjes = new TimeOnly(10, 0), OrariMbylljes = new TimeOnly(0, 0), Rating = 4.5m, Statusi = RestaurantStatus.Active, UserId =merchantUsers[5].Id, Kategori = "Healthy" },
            new Restaurant { Emertimi = "My Shawarma", Pershkrimi = "Your authentic Shawarma", Telefoni = "+383 49 000 000", Email = "info@myshawarma.com", Logo = "/uploads/logos/MyShawarmaLogo.png", OrariHapjes = new TimeOnly(10, 0), OrariMbylljes = new TimeOnly(0, 0), Rating = 4.5m, Statusi = RestaurantStatus.Active, UserId =merchantUsers[6].Id, Kategori = "Shawarma" },
            new Restaurant { Emertimi = "Heavy Hit", Pershkrimi = "Flame-grilled burgers...", Telefoni = "+383 49 000 000", Email = "info@heavyhit-ks.com", Logo = "/uploads/logos/HeavyHitLogo.png", OrariHapjes = new TimeOnly(10, 0), OrariMbylljes = new TimeOnly(0, 0), Rating = 4.5m, Statusi = RestaurantStatus.Active, UserId =merchantUsers[7].Id, Kategori = "Burgers" },
            new Restaurant { Emertimi = "Popeyes", Pershkrimi = "Flame-grilled burgers...", Telefoni = "+383 49 000 000", Email = "info@popeyes.com", Logo = "/uploads/logos/PopeyesLogo.png", OrariHapjes = new TimeOnly(10, 0), OrariMbylljes = new TimeOnly(0, 0), Rating = 4.5m, Statusi = RestaurantStatus.Active, UserId =merchantUsers[8].Id, Kategori = "Burgers" },
            new Restaurant { Emertimi = "Agusholli", Pershkrimi = "Sweet sweets!", Telefoni = "+383 49 000 000", Email = "info@agushollisweets.com", Logo = "/uploads/logos/AgusholliLogo.png", OrariHapjes = new TimeOnly(10, 0), OrariMbylljes = new TimeOnly(0, 0), Rating = 4.5m, Statusi = RestaurantStatus.Active, UserId =merchantUsers[10].Id, Kategori = "Dessert" },
            new Restaurant { Emertimi = "Saray Sweets", Pershkrimi = "Baklava and much more!", Telefoni = "+383 49 000 000", Email = "info@saraysweets.com", Logo = "/uploads/logos/SaraysweetsLogo.webp", OrariHapjes = new TimeOnly(10, 0), OrariMbylljes = new TimeOnly(0, 0), Rating = 4.5m, Statusi = RestaurantStatus.Active, UserId =merchantUsers[11].Id, Kategori = "Dessert" },
            new Restaurant { Emertimi = "Capvin 13", Pershkrimi = "Delicious burgers and more", Telefoni = "+383 49 000 000", Email = "info@capvin13.com", Logo = "/uploads/logos/Capvin13Logo.png", OrariHapjes = new TimeOnly(10, 0), OrariMbylljes = new TimeOnly(0, 0), Rating = 4.5m, Statusi = RestaurantStatus.Active, UserId =merchantUsers[12].Id, Kategori = "Burgers" },
            new Restaurant { Emertimi = "Fika Eatery", Pershkrimi = "Healthy and delicious meals", Telefoni = "+383 49 000 000", Email = "info@fikaeatery.com", Logo = "/uploads/logos/FikaLogo.png", OrariHapjes = new TimeOnly(10, 0), OrariMbylljes = new TimeOnly(0, 0), Rating = 4.5m, Statusi = RestaurantStatus.Active, UserId =merchantUsers[13].Id, Kategori = "Healthy" },
            new Restaurant { Emertimi = "Mulliri", Pershkrimi = "Traditional food with a modern twist", Telefoni = "+383 49 000 000", Email = "info@mullirivjeter.com", Logo = "/uploads/logos/MulliriLogo.jpg", OrariHapjes = new TimeOnly(10, 0), OrariMbylljes = new TimeOnly(0, 0), Rating = 4.5m, Statusi = RestaurantStatus.Active, UserId =merchantUsers[14].Id, Kategori = "Traditional" },
            new Restaurant { Emertimi = "Gjiks&Chiks", Pershkrimi = "Delicious chicken dishes!", Telefoni = "+383 49 000 000", Email = "info@gjiksandchiks.com", Logo = "/uploads/logos/GjiksLogo.png", OrariHapjes = new TimeOnly(10, 0), OrariMbylljes = new TimeOnly(0, 0), Rating = 4.5m, Statusi = RestaurantStatus.Active, UserId =merchantUsers[15].Id, Kategori = "Korean" },
            new Restaurant { Emertimi = "Smash Burger CO", Pershkrimi = "Delicious burgers and more", Telefoni = "+383 49 000 000", Email = "info@smashburgerco.com", Logo = "/uploads/logos/SmashLogo.png", OrariHapjes = new TimeOnly(10, 0), OrariMbylljes = new TimeOnly(0, 0), Rating = 4.5m, Statusi = RestaurantStatus.Active, UserId =merchantUsers[16].Id, Kategori = "Burgers" },
            new Restaurant { Emertimi = "Buffalo Burgers", Pershkrimi = "Delicious burgers and more", Telefoni = "+383 49 000 000", Email = "info@buffaloburgers.com", Logo = "/uploads/logos/BuffaloLogo.jpg", OrariHapjes = new TimeOnly(10, 0), OrariMbylljes = new TimeOnly(0, 0), Rating = 4.5m, Statusi = RestaurantStatus.Active, UserId =merchantUsers[17].Id, Kategori = "Burgers" },
            new Restaurant { Emertimi = "Hook Fish&Chips", Pershkrimi = "Delicious fish and chips!", Telefoni = "+383 49 000 000", Email = "info@hookfishandchips.com", Logo = "/uploads/logos/HookLogo.webp", OrariHapjes = new TimeOnly(10, 0), OrariMbylljes = new TimeOnly(0, 0), Rating = 4.5m, Statusi = RestaurantStatus.Active, UserId =merchantUsers[18].Id, Kategori = "Seafood" },
            new Restaurant { Emertimi = "Frix", Pershkrimi = "Delicious fries and more!", Telefoni = "+383 49 000 000", Email = "info@frixs.com", Logo = "/uploads/logos/FrixLogo.png", OrariHapjes = new TimeOnly(10, 0), OrariMbylljes = new TimeOnly(0, 0), Rating = 4.5m, Statusi = RestaurantStatus.Active, UserId =merchantUsers[19].Id, Kategori = "Fast Food" },
        };

        var toAdd = seedRestaurants
            .Where(r => !context.Restaurants.Any(db => db.Emertimi == r.Emertimi || db.Email == r.Email))
            .ToArray();

        if (toAdd.Any())
        {
            context.Restaurants.AddRange(toAdd);
            await context.SaveChangesAsync();
            Console.WriteLine($"Inserted {toAdd.Length} restaurants");

            if (!context.RestaurantAddresses.Any())
            {
                var allRestaurants = await context.Restaurants.ToListAsync();
                var addresses = new List<RestaurantAddress>();

                foreach (var restaurant in allRestaurants)
                {
                    if (restaurant.Emertimi == "SushiCo")
                    {
                        addresses.Add(new RestaurantAddress
                        {
                            RestaurantId = restaurant.Id,
                            Adresa = "Rr.Luan Hardinaj, Pallati i Rinisë",
                            Qyteti = "Prishtinë",
                            Zona = "Qendër",
                            IsMain = true,
                            IsActive = true,
                            Latitude = 42.661746791222185,
                            Longitude = 21.158287154546105
                        });
                    }
                    else if (restaurant.Emertimi == "Burger King")
                    {
                        addresses.Add(new RestaurantAddress
                        {
                            RestaurantId = restaurant.Id,
                            Adresa = "10000 Xhorxh Bush",
                            Qyteti = "Prishtinë",
                            Zona = "Qendër",
                            IsMain = true,
                            IsActive = true,
                            Latitude = 42.659109100741,
                            Longitude = 21.16076295045968
                        });

                        addresses.Add(new RestaurantAddress
                        {
                            RestaurantId = restaurant.Id,
                            Adresa = "Rr. Ahmet Krasniqi",
                            Qyteti = "Prishtinë",
                            Zona = "Arbëri",
                            IsMain = false,
                            IsActive = true,
                            Latitude = 42.65789276428699,
                            Longitude = 21.137545805949017
                        });

                        addresses.Add(new RestaurantAddress
                        {
                            RestaurantId = restaurant.Id,
                            Adresa = "Royal Mall, Rruga B",
                            Qyteti = "Prishtinë",
                            Zona = "Bregu i Diellit",
                            IsMain = false,
                            IsActive = true,
                            Latitude = 42.65398360322043,
                            Longitude = 21.17741720185093

                        });

                        addresses.Add(new RestaurantAddress
                        {
                            RestaurantId = restaurant.Id,
                            Adresa = "Rr Ukshin Hoti",
                            Qyteti = "Prishtinë",
                            Zona = "Pejton",
                            IsMain = false,
                            IsActive = true,
                            Latitude = 42.658959200309425,
                            Longitude = 21.153938746030168
                        });

                        addresses.Add(new RestaurantAddress
                        {
                            RestaurantId = restaurant.Id,
                            Adresa = "Veternik",
                            Qyteti = "Prishtinë",
                            Zona = "Veternik",
                            IsMain = false,
                            IsActive = true,
                            Latitude = 42.63158479775115,
                            Longitude = 21.147336142328303
                        });

                        addresses.Add(new RestaurantAddress
                        {
                            RestaurantId = restaurant.Id,
                            Adresa = " Albi Mall,Zona e Re Industriale",
                            Qyteti = "Prishtinë",
                            Zona = "Veternik",
                            IsMain = false,
                            IsActive = true,
                            Latitude = 42.49437779850004,
                            Longitude = 21.49905185590765
                        });

                    }
                    else if (restaurant.Emertimi == "Pasta Fasta")
                    {

                        addresses.Add(new RestaurantAddress
                        {
                            RestaurantId = restaurant.Id,
                            Adresa = "Rr Hajdar Dushi, nr 12",
                            Qyteti = "Prishtinë",
                            Zona = "Qendër",
                            IsMain = true,
                            IsActive = true,
                            Latitude = 42.66296333180196,
                            Longitude = 21.16216657486602
                        });

                    }

                    else if (restaurant.Emertimi == "Proper Pizza")
                    {
                        addresses.Add(new RestaurantAddress
                        {
                            RestaurantId = restaurant.Id,
                            Adresa = "Rrethi i Çagllavicës",
                            Qyteti = "Prishtinë",
                            Zona = "Çagllavicë",
                            IsMain = false,
                            IsActive = true,
                            Latitude = 42.61653150647903,
                            Longitude = 21.143237890140693
                        });

                        addresses.Add(new RestaurantAddress
                        {
                            RestaurantId = restaurant.Id,
                            Adresa = "Rr Dervish Hima",
                            Qyteti = "Prishtinë",
                            Zona = "Emshir",
                            IsMain = false,
                            IsActive = false,
                            Latitude = 42.64307810037658,
                            Longitude = 21.1537448423283
                        });

                        addresses.Add(new RestaurantAddress
                        {
                            RestaurantId = restaurant.Id,
                            Adresa = "Rr Robert Doll",
                            Qyteti = "Prishtinë",
                            Zona = "Pejton",
                            IsMain = false,
                            IsActive = true,
                            Latitude = 42.658414864338965,
                            Longitude = 21.15422995843568
                        });

                        addresses.Add(new RestaurantAddress
                        {
                            RestaurantId = restaurant.Id,
                            Adresa = "Rr Rexhep Luci",
                            Qyteti = "Prishtinë",
                            Zona = "Qendër",
                            IsMain = true,
                            IsActive = true,
                            Latitude = 42.66564959695687,
                            Longitude = 21.16275784811355
                        });

                        addresses.Add(new RestaurantAddress
                        {
                            RestaurantId = restaurant.Id,
                            Adresa = "Rr Hyzri Talla",
                            Qyteti = "Prishtinë",
                            Zona = "Bregu i Diellit",
                            IsMain = false,
                            IsActive = true,
                            Latitude = 42.65083595491357,
                            Longitude = 21.17363691301479
                        });

                        addresses.Add(new RestaurantAddress
                        {
                            RestaurantId = restaurant.Id,
                            Adresa = "Rr Haxhi Zeka",
                            Qyteti = "Prishtinë",
                            Zona = "Kolovicë",
                            IsMain = false,
                            IsActive = true,
                            Latitude = 42.68119298858435,
                            Longitude = 21.1730895752945
                        });

                        addresses.Add(new RestaurantAddress
                        {
                            RestaurantId = restaurant.Id,
                            Adresa = "Rr Ferid Curri",
                            Qyteti = "Prishtinë",
                            Zona = "Arbëri",
                            IsMain = false,
                            IsActive = true,
                            Latitude = 42.66095587439803,
                            Longitude = 21.141339771164148
                        });
                    }

                    else if (restaurant.Emertimi == "KFC")
                    {
                        addresses.Add(new RestaurantAddress
                        {
                            RestaurantId = restaurant.Id,
                            Adresa = "Kompleksi ish-Ramiz Sadiku",
                            Qyteti = "Prishtinë",
                            Zona = "Pejton",
                            IsMain = true,
                            IsActive = true,
                            Latitude = 42.65896126992145,
                            Longitude = 21.15304541164153
                        });

                        addresses.Add(new RestaurantAddress
                        {
                            RestaurantId = restaurant.Id,
                            Adresa = "Arbëri",
                            Qyteti = "Prishtinë",
                            Zona = "Arbëri",
                            IsMain = false,
                            IsActive = true,
                            Latitude = 42.65798360243033,
                            Longitude = 21.137315769313222
                        });
                        addresses.Add(new RestaurantAddress
                        {
                            RestaurantId = restaurant.Id,
                            Adresa = "Rruga B",
                            Qyteti = "Prishtinë",
                            Zona = "Bregu i Diellit",
                            IsMain = false,
                            IsActive = true,
                            Latitude = 42.65473091732552,
                            Longitude = 21.176711840433374
                        });
                        addresses.Add(new RestaurantAddress
                        {
                            RestaurantId = restaurant.Id,
                            Adresa = "Qendra Tregtare Albi Mall",
                            Qyteti = "Prishtinë",
                            Zona = "Veternik",
                            IsMain = false,
                            IsActive = true,
                            Latitude = 42.49437779850004,
                            Longitude = 21.49905185590765
                        });
                        addresses.Add(new RestaurantAddress
                        {
                            RestaurantId = restaurant.Id,
                            Adresa = "Veternik",
                            Qyteti = "Prishtinë",
                            Zona = "Veternik",
                            IsMain = false,
                            IsActive = true,
                            Latitude = 42.63143479299327,
                            Longitude = 21.147303952369974
                        });
                        addresses.Add(new RestaurantAddress
                        {
                            RestaurantId = restaurant.Id,
                            Adresa = "Lagja Marigona",
                            Qyteti = "Prishtinë",
                            Zona = "Çagllavicë",
                            IsMain = false,
                            IsActive = true,
                            Latitude = 42.62574929545231,
                            Longitude = 21.08312669701782

                        });
                        addresses.Add(new RestaurantAddress
                        {
                            RestaurantId = restaurant.Id,
                            Adresa = "Prishtina Mall, M2 (Prishtinë-Ferizaj)",
                            Qyteti = "Prishtinë",
                            Zona = "Çagllavicë",
                            IsMain = false,
                            IsActive = true,
                            Latitude = 42.564671220016166,
                            Longitude = 21.133386684904043

                        });
                    }

                    else if (restaurant.Emertimi == "Green and Protein")
                    {
                        addresses.Add(new RestaurantAddress
                        {
                            RestaurantId = restaurant.Id,
                            Adresa = "Prime Residence, Tirana",
                            Qyteti = "Prishtinë",
                            Zona = "Lakrishtë",
                            IsMain = true,
                            IsActive = true,
                            Latitude = 42.65607750726741,
                            Longitude = 21.151074513492457
                        });
                    }

                    else if (restaurant.Emertimi == "My Shawarma")
                    {
                        addresses.Add(new RestaurantAddress
                        {
                            RestaurantId = restaurant.Id,
                            Adresa = "Sheshi Xhorxh Bush",
                            Qyteti = "Prishtinë",
                            Zona = "Qendër",
                            IsMain = true,
                            IsActive = true,
                            Latitude = 42.657348936064004,
                            Longitude = 21.160636950520498
                        });
                    }

                    else if (restaurant.Emertimi == "Heavy Hit")
                    {
                        addresses.Add(new RestaurantAddress
                        {
                            RestaurantId = restaurant.Id,
                            Adresa = "Albi Mall, Zona e re Industriale",
                            Qyteti = "Prishtinë",
                            Zona = "Veternik",
                            IsMain = true,
                            IsActive = true,
                            Latitude = 42.49437779850004,
                            Longitude = 21.49905185590765
                        });

                    }
                    else if (restaurant.Emertimi == "Popeyes")
                    {
                        addresses.Add(new RestaurantAddress
                        {
                            RestaurantId = restaurant.Id,
                            Adresa = "Prishtina Mall, M2 (Prishtinë-Ferizaj)",
                            Qyteti = "Prishtinë",
                            Zona = "Çagllavicë",
                            IsMain = true,
                            IsActive = true,
                            Latitude = 42.564671220016166,
                            Longitude = 21.133386684904043
                        });
                    }

                    else if (restaurant.Emertimi == "Agusholli")
                    {
                        addresses.Add(new RestaurantAddress
                        {
                            RestaurantId = restaurant.Id,
                            Adresa = "Santea",
                            Qyteti = "Prishtinë",
                            Zona = "Bill Clinton",
                            IsMain = true,
                            IsActive = true,
                            Latitude = 42.65873016747044,
                            Longitude = 21.16484176685335
                        });

                        addresses.Add(new RestaurantAddress
                        {
                            RestaurantId = restaurant.Id,
                            Adresa = "m9 Nënë Tereza",
                            Qyteti = "Prishtinë",
                            Zona = "M9 Fushë Kosovë",
                            IsMain = false,
                            IsActive = false,
                            Latitude = 42.641767568275036,
                            Longitude = 21.104080228199194
                        });
                    }
                    else if (restaurant.Emertimi == "Saray Sweets")
                    {
                        addresses.Add(new RestaurantAddress
                        {
                            RestaurantId = restaurant.Id,
                            Adresa = "Rr Agim Ramadani",
                            Qyteti = "Prishtinë",
                            Zona = "Bulevardi Nënë Tereza",
                            IsMain = true,
                            IsActive = true,
                            Latitude = 42.664535982621516,
                            Longitude = 21.165257275050763
                        });
                    }
                    else if (restaurant.Emertimi == "Capvin 13")
                    {
                        addresses.Add(new RestaurantAddress
                        {
                            RestaurantId = restaurant.Id,
                            Adresa = "Rr Hyzri Talla",
                            Qyteti = "Prishtinë",
                            Zona = "Bregu i Diellit",
                            IsMain = true,
                            IsActive = true,
                            Latitude = 42.65118040887382,
                            Longitude = 21.175045729087945
                        });

                    }

                    else if (restaurant.Emertimi == "Fika Eatery")
                    {
                        addresses.Add(new RestaurantAddress
                        {
                            RestaurantId = restaurant.Id,
                            Adresa = "Bashkim Fehmiu 47",
                            Qyteti = "Prishtinë",
                            Zona = "Lakrishtë",
                            IsMain = true,
                            IsActive = true,
                            Latitude = 42.657902432970126,
                            Longitude = 21.148112923535553
                        });
                    }
                    else if (restaurant.Emertimi == "Mulliri")
                    {
                        addresses.Add(new RestaurantAddress
                        {
                            RestaurantId = restaurant.Id,
                            Adresa = "Rr Luan Haradinaj",
                            Qyteti = "Prishtinë",
                            Zona = "Qendër",
                            IsMain = true,
                            IsActive = true,
                            Latitude = 42.702782669836346,
                            Longitude = 21.17973761
                        });
                    }


                    else if (restaurant.Emertimi == "Gjiks&Chiks")
                    {
                        addresses.Add(new RestaurantAddress

                        {
                            RestaurantId = restaurant.Id,
                            Adresa = "Rruga Vicianum",
                            Qyteti = "Prishtinë",
                            Zona = "Arbëri",
                            IsMain = true,
                            IsActive = true,
                            Latitude = 42.66188248266638,
                            Longitude = 21.15364637875169
                        });

                    }

                    else if (restaurant.Emertimi == "Smash Burger CO")
                    {
                        addresses.Add(new RestaurantAddress

                        {
                            RestaurantId = restaurant.Id,
                            Adresa = "Ramiz Sadiku",
                            Qyteti = "Prishtinë",
                            Zona = "Pejton",
                            IsMain = true,
                            IsActive = true,
                            Latitude = 42.65771040379635,
                            Longitude = 21.151608596550705
                        });

                    }
                    else if (restaurant.Emertimi == "Buffalo Burgers")
                    {
                        addresses.Add(new RestaurantAddress

                        {
                            RestaurantId = restaurant.Id,
                            Adresa = "Rruga Muharrem Fejza",
                            Qyteti = "Prishtinë",
                            Zona = "Bregu i Diellit",
                            IsMain = false,
                            IsActive = false,
                            Latitude = 42.64612983216337,
                            Longitude = 21.17528867687603
                        });

                        addresses.Add(new RestaurantAddress

                        {
                            RestaurantId = restaurant.Id,
                            Adresa = "Rr Nurije Zeka",
                            Qyteti = "Prishtinë",
                            Zona = "Qendër",
                            IsMain = true,
                            IsActive = true,
                            Latitude = 42.66145834096824,
                            Longitude = 21.16089913030993
                        });

                    }

                    else if (restaurant.Emertimi == "Hook Fish&Chips")
                    {
                        addresses.Add(new RestaurantAddress
                        {
                            RestaurantId = restaurant.Id,
                            Adresa = "Rruga Hajdar Dushi",
                            Qyteti = "Prishtinë",
                            Zona = "Qafa",
                            IsMain = true,
                            IsActive = true,
                            Latitude = 42.663282501508405,
                            Longitude = 21.161554834572957
                        });
                    }


                    else if (restaurant.Emertimi == "Frix")
                    {
                        addresses.Add(new RestaurantAddress
                        {
                            RestaurantId = restaurant.Id,
                            Adresa = "Albi Mall, Veternik",
                            Qyteti = "Prishtinë",
                            Zona = "Veternik",
                            IsMain = true,
                            IsActive = true,
                            Latitude = 42.49437779850004,
                            Longitude = 21.49905185590765
                        });

                        addresses.Add(new RestaurantAddress
                        {
                            RestaurantId = restaurant.Id,
                            Adresa = "Prishtina Mall, Zona Industriale",
                            Qyteti = "Prishtinë",
                            Zona = "Çagllavicë",
                            IsMain = true,
                            IsActive = true,
                            Latitude = 42.564671220016166,
                            Longitude = 21.133386684904043

                        });
                    }


                }


                context.RestaurantAddresses.AddRange(addresses);
                await context.SaveChangesAsync();

                Console.WriteLine("Addresses inserted");
            }
        }
        else
        {
            Console.WriteLine("No new restaurants to insert.");
        }

        var addressesNeedingOwner = await context.RestaurantAddresses
            .Include(a => a.Restaurant)
            .Where(a => a.MerchantUserId == null && a.Restaurant != null && a.Restaurant.UserId != null)
            .ToListAsync();

        if (addressesNeedingOwner.Any())
        {
            foreach (var address in addressesNeedingOwner)
            {
                address.MerchantUserId = address.Restaurant!.UserId;
            }

            await context.SaveChangesAsync();
            Console.WriteLine($"Backfilled MerchantUserId for {addressesNeedingOwner.Count} restaurant addresses");
        }

        var branchManagerSeeds = new[]
        {
            // SushiCo branches
            new { Username = "bm_sushico", Email = "bm_sushico@example.com", RestaurantName = "SushiCo", AddressContains = "Rr. Luan Hardinaj, Pallati i Rinisë, Qendër" },
            // Burger King branches
            new { Username = "bm_burger1", Email = "bm_burger1@example.com", RestaurantName = "Burger King", AddressContains = "Xhorxh Bush" },
            new { Username = "bm_burger2", Email = "bm_burger2@example.com", RestaurantName = "Burger King", AddressContains = "Ahmet Krasniqi" },
            new { Username = "bm_burger3", Email = "bm_burger3@example.com", RestaurantName = "Burger King", AddressContains = "Albi Mall" },
            new { Username = "bm_burger4", Email = "bm_burger4@example.com", RestaurantName = "Burger King", AddressContains = "Royal Mall" },
            new { Username = "bm_burger5", Email = "bm_burger5@example.com", RestaurantName = "Burger King", AddressContains = "Ukshin Hoti" },
            new { Username = "bm_burger6", Email = "bm_burger6@example.com", RestaurantName = "Burger King", AddressContains = "Veternik" }, 
            // KFC branch manager seed (leave as is)
            new { Username = "bm_kfc1", Email = "bm_kfc1@example.com", RestaurantName = "KFC", AddressContains = "Kompleksi ish-Ramiz Sadiku, Pejton" },
            new { Username = "bm_kfc2", Email = "bm_kfc2@example.com", RestaurantName = "KFC", AddressContains = "Arbëri" },
            new { Username = "bm_kfc3", Email = "bm_kfc3@example.com", RestaurantName = "KFC", AddressContains = "Lagja Marigona, Çagllavicë" },
            new { Username = "bm_kfc4", Email = "bm_kfc4@example.com", RestaurantName = "KFC", AddressContains = "Prishtina Mall, Çagllavicë" },
            new { Username = "bm_kfc5", Email = "bm_kfc5@example.com", RestaurantName = "KFC", AddressContains = "Qendra Tregtare Albi Mall, Veternik" },
            new { Username = "bm_kfc6", Email = "bm_kfc6@example.com", RestaurantName = "KFC", AddressContains = "Rruga B, Bregu i Diellit" },
            new { Username = "bm_kfc7", Email = "bm_kfc7@example.com", RestaurantName = "KFC", AddressContains = "Veternik" },

            //Frix branches
            new { Username = "bm_frix1", Email = "bm_frix1@example.com", RestaurantName = "Frix", AddressContains = "Albi Mall, Veternik" },
            new { Username = "bm_frix2", Email = "bm_frix2@example.com", RestaurantName = "Frix", AddressContains = "Prishtina Mall, Zona Industriale, Çagllavicë" },

            //HeavyHit branches
            new { Username = "bm_heavyhit", Email = "bm_heavyhit@example.com", RestaurantName = "Heavy Hit", AddressContains = "Veternik" },
            //Popeyes branches
            new { Username = "bm_popeyes", Email = "bm_popeyes@example.com", RestaurantName = "Popeyes", AddressContains = "Prishtina Mall, Çagllavicë" },

            //Capvin 13 branches
            new { Username = "bm_capvin13", Email = "bm_capvin13@example.com", RestaurantName = "Capvin 13", AddressContains = "Rr Hyzri Talla, Bregu i Diellit" },
            //Smash Burger CO branches
            new { Username = "bm_smashburger", Email = "bm_smashburger@example.com", RestaurantName = "Smash Burger CO", AddressContains = "Ramiz Sadiku, Pejton" },
            //Buffalo Burgers branches
            new { Username = "bm_buffalo1", Email = "bm_buffalo1@example.com", RestaurantName = "Buffalo Burgers", AddressContains = "Rr Nurije Zeka, Qendër" },
            new { Username = "bm_buffalo2", Email = "bm_buffalo2@example.com", RestaurantName = "Buffalo Burgers", AddressContains = "Rruga Muharrem Fejza, Bregu i Diellit" },
            //Agusholli branches    
            new { Username = "bm_agusholli1", Email = "bm_agusholli1@example.com", RestaurantName = "Agusholli", AddressContains = "Bill Clinton" },
            new { Username = "bm_agusholli2", Email = "bm_agusholli2@example.com", RestaurantName = "Agusholli", AddressContains = "M9 Fushë Kosovë" },
            //Saray Sweets branches
            new { Username = "bm_saray", Email = "bm_saray@example.com", RestaurantName = "Saray Sweets", AddressContains = "Bulevardi Nënë Tereza" },

            //Green and Protein branches
            new { Username = "bm_greenprotein", Email = "bm_greenprotein@example.com", RestaurantName = "Green and Protein", AddressContains = "Prime Residence, Lakrishte" },
            //Fika Eatery branches
            new { Username = "bm_fikaeatery", Email = "bm_fikaeatery@example.com", RestaurantName = "Fika Eatery", AddressContains = "Bashkim Fehmiu 47, Lakrishte" },
            //Pasta Fasta branches
            new { Username = "bm_pastafasta", Email = "bm_pastafasta@example.com", RestaurantName = "Pasta Fasta", AddressContains = "Rr Hajdar Dushi nr 12, Qendër" },
            //Gjiks&Chiks branches  
            new { Username = "bm_gjiks", Email = "bm_gjiks@example.com", RestaurantName = "Gjiks&Chiks", AddressContains = "Rruga Vicianum, Arbëri" },
            //Propper Pizza branches
            new { Username = "bm_proper1", Email = "bm_proper1@example.com", RestaurantName = "Proper Pizza", AddressContains = "Rr Rexhep Luci, Qendër" },
            new { Username = "bm_proper2", Email = "bm_proper2@example.com", RestaurantName = "Proper Pizza", AddressContains = "Rr Ferid Curri, Arbëri" },
            new { Username = "bm_proper3", Email = "bm_proper3@example.com", RestaurantName = "Proper Pizza", AddressContains = "Rr Haxhi Zeka, Kolovcë" },
            new { Username = "bm_proper4", Email = "bm_proper4@example.com", RestaurantName = "Proper Pizza", AddressContains = "Rr Hyzri Talla, Bregu i Diellit" },
            new { Username = "bm_proper5", Email = "bm_proper5@example.com", RestaurantName = "Proper Pizza", AddressContains = "Rr Robert Doll, Pejton" },
            new { Username = "bm_proper6", Email = "bm_proper6@example.com", RestaurantName = "Proper Pizza", AddressContains = "Rrethi i Çagllavicës, Çagllavicë" },
            new { Username = "bm_proper7", Email = "bm_proper7@example.com", RestaurantName = "Proper Pizza", AddressContains = "Rr Dervish Hima, Emshir" },
            //Hook Fish&Chips branches
            new { Username = "bm_hookfish", Email = "bm_hookfish@example.com", RestaurantName = "Hook Fish&Chips", AddressContains = "Rruga Hajdar Dushi, Qafa" },
            //My Shawarma branches
            new { Username = "bm_myshawarma", Email = "bm_myshawarma@example.com", RestaurantName = "My Shawarma", AddressContains = "Sheshi Xhorxh Bush, Qendër" },
            //mulliri branches
            new { Username = "bm_mulliri", Email = "bm_mulliri@example.com", RestaurantName = "Mulliri", AddressContains = "Rr Luan Haradinaj, Qendër" }


                    }; 

        var branchAssignments = 0;
        foreach (var seed in branchManagerSeeds)
        {
            var user = await userManager.FindByNameAsync(seed.Username)
                       ?? await userManager.FindByEmailAsync(seed.Email);

            if (user == null)
            {
                user = new User
                {
                    UserName = seed.Username,
                    Email = seed.Email,
                    EmailConfirmed = true
                };

                var createResult = await userManager.CreateAsync(user, "Branch@1234");
                if (!createResult.Succeeded)
                {
                    throw new InvalidOperationException($"Failed to create branch manager user {seed.Username}");
                }
            }
            else
            {
                var changed = false;

                if (!string.Equals(user.UserName, seed.Username, StringComparison.OrdinalIgnoreCase))
                {
                    user.UserName = seed.Username;
                    changed = true;
                }

                if (!string.Equals(user.Email, seed.Email, StringComparison.OrdinalIgnoreCase))
                {
                    user.Email = seed.Email;
                    changed = true;
                }

                if (!user.EmailConfirmed)
                {
                    user.EmailConfirmed = true;
                    changed = true;
                }

                if (changed)
                {
                    var updateResult = await userManager.UpdateAsync(user);
                    if (!updateResult.Succeeded)
                    {
                        throw new InvalidOperationException($"Failed to update branch manager user {seed.Username}");
                    }
                }
            }

            if (!await userManager.IsInRoleAsync(user, AppRoles.BranchManager))
            {
                await userManager.AddToRoleAsync(user, AppRoles.BranchManager);
            }

            var restaurant = await context.Restaurants.FirstOrDefaultAsync(r => r.Emertimi == seed.RestaurantName);
            if (restaurant == null)
            {
                Console.WriteLine($"Restaurant '{seed.RestaurantName}' not found for branch manager seed {seed.Username}");
                continue;
            }

            var targetAddress = await context.RestaurantAddresses
                .Where(a => a.RestaurantId == restaurant.Id && (a.IsActive || a.IsMain))
                .OrderByDescending(a => a.Adresa.Contains(seed.AddressContains))
                .ThenByDescending(a => a.IsMain)
                .ThenBy(a => a.Id)
                .FirstOrDefaultAsync();

            if (targetAddress == null)
            {
                Console.WriteLine($"No address found for restaurant '{seed.RestaurantName}' while seeding {seed.Username}");
                continue;
            }

            if (targetAddress.MerchantUserId != user.Id)
            {
                targetAddress.MerchantUserId = user.Id;
                branchAssignments++;
            }
        }

        if (branchAssignments > 0)
        {
            await context.SaveChangesAsync();
            Console.WriteLine($"Assigned {branchAssignments} branch manager ownership mappings");
        }


        var seedDrivers = new DeliveryDrivers[]
        {
            new DeliveryDrivers { Automjeti = "Scooter", Targa = "AA 000 AA", Zona = "Qendër", Statusi = DriverStatus.Available, Vlersimi = 4.5m, UserId = courierUser.Id },
            new DeliveryDrivers { Automjeti = "Scooter", Targa = "AA 111 AA", Zona = "Qendër", Statusi = DriverStatus.Available, Vlersimi = 4.5m, UserId = courierUser.Id },
            new DeliveryDrivers { Automjeti = "Motorcycle", Targa = "BB 000 BB", Zona = "Qendër", Statusi = DriverStatus.Available, Vlersimi = 4.5m, UserId = courierUser.Id },
            new DeliveryDrivers { Automjeti = "Bicycle", Targa = "CC 000 CC", Zona = "Qendër", Statusi = DriverStatus.Available, Vlersimi = 4.5m, UserId = courierUser.Id },
            new DeliveryDrivers { Automjeti = "Scooter", Targa = "AA 222 AA", Zona = "Arbëri", Statusi = DriverStatus.Available, Vlersimi = 4.5m, UserId = courierUser.Id },
            new DeliveryDrivers { Automjeti = "Scooter", Targa = "AA 333 AA", Zona = "Arbëri", Statusi = DriverStatus.Available, Vlersimi = 4.5m, UserId = courierUser.Id },
            new DeliveryDrivers { Automjeti = "Motorcycle", Targa = "BB 111 BB", Zona = "Arbëri", Statusi = DriverStatus.Available, Vlersimi = 4.5m, UserId = courierUser.Id },
            new DeliveryDrivers { Automjeti = "Bicycle", Targa = "CC 111 CC", Zona = "Arbëri", Statusi = DriverStatus.Available, Vlersimi = 4.5m, UserId = courierUser.Id },
            new DeliveryDrivers { Automjeti = "Scooter", Targa = "AA 444 AA", Zona = "Bregu i Diellit", Statusi = DriverStatus.Available, Vlersimi = 4.5m, UserId = courierUser.Id },
            new DeliveryDrivers { Automjeti = "Bicycle", Targa = "CC 222 CC", Zona = "Bregu i Diellit", Statusi = DriverStatus.Available, Vlersimi = 4.5m, UserId = courierUser.Id },
            new DeliveryDrivers { Automjeti = "Motorcycle", Targa = "BB 222 BB", Zona = "Bregu i Diellit", Statusi = DriverStatus.Available, Vlersimi = 4.5m, UserId = courierUser.Id },
            new DeliveryDrivers { Automjeti = "Scooter", Targa = "AA 666 AA", Zona = "Bregu i Diellit", Statusi = DriverStatus.Available, Vlersimi = 4.5m, UserId = courierUser.Id },
            new DeliveryDrivers { Automjeti = "Scooter", Targa = "AA 555 AA", Zona = "Veternik", Statusi = DriverStatus.Available, Vlersimi = 4.5m, UserId = courierUser.Id },
            new DeliveryDrivers { Automjeti = "Motorcycle", Targa = "BB 333 BB", Zona = "Veternik", Statusi = DriverStatus.Available, Vlersimi = 4.5m, UserId = courierUser.Id },
            new DeliveryDrivers { Automjeti = "Bicycle", Targa = "CC 333 CC", Zona = "Veternik", Statusi = DriverStatus.Available, Vlersimi = 4.5m, UserId = courierUser.Id },
            new DeliveryDrivers { Automjeti = "Scooter", Targa = "AA 777 AA", Zona = "Veternik", Statusi = DriverStatus.Available, Vlersimi = 4.5m, UserId = courierUser.Id },
            new DeliveryDrivers { Automjeti = "Scooter", Targa = "AA 888 AA", Zona = "Çagllavicë", Statusi = DriverStatus.Available, Vlersimi = 4.5m, UserId = courierUser.Id },
            new DeliveryDrivers { Automjeti = "Motorcycle", Targa = "BB 444 BB", Zona = "Çagllavicë", Statusi = DriverStatus.Available, Vlersimi = 4.5m, UserId = courierUser.Id },
            new DeliveryDrivers { Automjeti = "Bicycle", Targa = "CC 444 CC", Zona = "Çagllavicë", Statusi = DriverStatus.Available, Vlersimi = 4.5m, UserId = courierUser.Id },
            new DeliveryDrivers { Automjeti = "Scooter", Targa = "AA 999 AA", Zona = "Çagllavicë", Statusi = DriverStatus.Available, Vlersimi = 4.5m, UserId = courierUser.Id },
            new DeliveryDrivers { Automjeti = "Scooter", Targa = "AA 000 BB", Zona = "Pejton", Statusi = DriverStatus.Available, Vlersimi = 4.5m, UserId = courierUser.Id },
            new DeliveryDrivers { Automjeti = "Motorcycle", Targa = "BB 555 BB", Zona = "Pejton", Statusi = DriverStatus.Available, Vlersimi = 4.5m, UserId = courierUser.Id },
            new DeliveryDrivers { Automjeti = "Bicycle", Targa = "CC 555 CC", Zona = "Pejton", Statusi = DriverStatus.Available, Vlersimi = 4.5m, UserId = courierUser.Id },
            new DeliveryDrivers { Automjeti = "Scooter", Targa = "AA 111 BB", Zona = "Pejton", Statusi = DriverStatus.Available, Vlersimi = 4.5m, UserId = courierUser.Id },
            new DeliveryDrivers { Automjeti = "Scooter", Targa = "AA 222 BB", Zona = "Lakrishtë", Statusi = DriverStatus.Available, Vlersimi = 4.5m, UserId = courierUser.Id },
            new DeliveryDrivers { Automjeti = "Motorcycle", Targa = "BB 666 BB", Zona = "Lakrishtë", Statusi = DriverStatus.Available, Vlersimi = 4.5m, UserId = courierUser.Id },
            new DeliveryDrivers { Automjeti = "Bicycle", Targa = "CC 666 CC", Zona = "Lakrishtë", Statusi = DriverStatus.Available, Vlersimi = 4.5m, UserId = courierUser.Id },
            new DeliveryDrivers { Automjeti = "Scooter", Targa = "AA 333 BB", Zona = "Lakrishtë", Statusi = DriverStatus.Available, Vlersimi = 4.5m, UserId = courierUser.Id },
            new DeliveryDrivers { Automjeti = "Scooter", Targa = "AA 444 BB", Zona = "Qafa", Statusi = DriverStatus.Available, Vlersimi = 4.5m, UserId = courierUser.Id },
            new DeliveryDrivers { Automjeti = "Motorcycle", Targa = "BB 777 BB", Zona = "Qafa", Statusi = DriverStatus.Available, Vlersimi = 4.5m, UserId = courierUser.Id },
            new DeliveryDrivers { Automjeti = "Bicycle", Targa = "CC 777 CC", Zona = "Qafa", Statusi = DriverStatus.Available, Vlersimi = 4.5m, UserId = courierUser.Id },
            new DeliveryDrivers { Automjeti = "Scooter", Targa = "AA 555 BB", Zona = "Emshir", Statusi = DriverStatus.Available, Vlersimi = 4.5m, UserId = courierUser.Id },
            new DeliveryDrivers { Automjeti = "Motorcycle", Targa = "BB 888 BB", Zona = "Emshir", Statusi = DriverStatus.Available, Vlersimi = 4.5m, UserId = courierUser.Id },
            new DeliveryDrivers { Automjeti = "Bicycle", Targa = "CC 888 CC", Zona = "Emshir", Statusi = DriverStatus.Available, Vlersimi = 4.5m, UserId = courierUser.Id },
            new DeliveryDrivers { Automjeti = "Scooter", Targa = "AA 666 BB", Zona = "Emshir", Statusi = DriverStatus.Available, Vlersimi = 4.5m, UserId = courierUser.Id },
            new DeliveryDrivers { Automjeti = "Scooter", Targa = "AA 777 BB", Zona = "Bill Clinton", Statusi = DriverStatus.Available, Vlersimi = 4.5m, UserId = courierUser.Id },
            new DeliveryDrivers { Automjeti = "Motorcycle", Targa = "BB 999 BB", Zona = "Bill Clinton", Statusi = DriverStatus.Available, Vlersimi = 4.5m, UserId = courierUser.Id },
            new DeliveryDrivers { Automjeti = "Bicycle", Targa = "CC 999 CC", Zona = "Bill Clinton", Statusi = DriverStatus.Available, Vlersimi = 4.5m, UserId = courierUser.Id },
            new DeliveryDrivers { Automjeti = "Scooter", Targa = "AA 888 BB", Zona = "Bill Clinton", Statusi = DriverStatus.Available, Vlersimi = 4.5m, UserId = courierUser.Id },
            new DeliveryDrivers { Automjeti = "Scooter", Targa = "AA 999 BB", Zona = "M9 Fushë Kosovë", Statusi = DriverStatus.Available, Vlersimi = 4.5m, UserId = courierUser.Id },
            new DeliveryDrivers { Automjeti = "Motorcycle", Targa = "BB 000 CC", Zona = "M9 Fushë Kosovë", Statusi = DriverStatus.Available, Vlersimi = 4.5m, UserId = courierUser.Id },
            new DeliveryDrivers { Automjeti = "Bicycle", Targa = "CC 000 CC", Zona = "M9 Fushë Kosovë", Statusi = DriverStatus.Available, Vlersimi = 4.5m, UserId = courierUser.Id },
            new DeliveryDrivers { Automjeti = "Scooter", Targa = "AA 000 CC", Zona = "M9 Fushë Kosovë", Statusi = DriverStatus.Available, Vlersimi = 4.5m, UserId = courierUser.Id },
            new DeliveryDrivers { Automjeti = "Scooter", Targa = "AA 111 CC", Zona = "Bulevardi Nënë Tereza", Statusi = DriverStatus.Available, Vlersimi = 4.5m, UserId = courierUser.Id },
            new DeliveryDrivers { Automjeti = "Motorcycle", Targa = "BB 111 CC", Zona = "Bulevardi Nënë Tereza", Statusi = DriverStatus.Available, Vlersimi = 4.5m, UserId = courierUser.Id },
            new DeliveryDrivers { Automjeti = "Bicycle", Targa = "CC 111 CC", Zona = "Bulevardi Nënë Tereza", Statusi = DriverStatus.Available, Vlersimi = 4.5m, UserId = courierUser.Id },
            new DeliveryDrivers { Automjeti = "Scooter", Targa = "AA 222 CC", Zona = "Bulevardi Nënë Tereza", Statusi = DriverStatus.Available, Vlersimi = 4.5m, UserId = courierUser.Id },
            new DeliveryDrivers { Automjeti = "Scooter", Targa = "AA 333 CC", Zona = "Kolovicë", Statusi = DriverStatus.Available, Vlersimi = 4.5m, UserId = courierUser.Id },
            new DeliveryDrivers { Automjeti = "Motorcycle", Targa = "BB 222 CC", Zona = "Kolovicë", Statusi = DriverStatus.Available, Vlersimi = 4.5m, UserId = courierUser.Id },
            new DeliveryDrivers { Automjeti = "Bicycle", Targa = "CC 222 CC", Zona = "Kolovicë", Statusi = DriverStatus.Available, Vlersimi = 4.5m, UserId = courierUser.Id }
        };

        if (!context.DeliveryDrivers.Any())
        {
            context.DeliveryDrivers.AddRange(seedDrivers);
            await context.SaveChangesAsync();
            Console.WriteLine("Delivery drivers inserted");

        }
        if (!context.MenuCategories.Any())
        {
            var categories = new List<MenuCategory>();

            var burgerKing = context.Restaurants.FirstOrDefault(r => r.Emertimi == "Burger King");

            if (burgerKing != null)
            {
                var beef = new MenuCategory
                {
                    Emertimi = "Beef",
                    Pershkrimi = "Juicy flame-grilled beef burgers",
                    Renditja = 1,
                    RestaurantId = burgerKing.Id
                };

                var chicken = new MenuCategory
                {
                    Emertimi = "Chicken",
                    Pershkrimi = "Crispy, juicy chicken",
                    Renditja = 2,
                    RestaurantId = burgerKing.Id
                };
                var fish = new MenuCategory
                {
                    Emertimi = "Fish",
                    Pershkrimi = "Golden fried fish, tender and delicious ",
                    Renditja = 3,
                    RestaurantId = burgerKing.Id
                };
                var royal = new MenuCategory
                {
                    Emertimi = "Royal",
                    Pershkrimi = "Premium burgers with special sauces",
                    Renditja = 4,
                    RestaurantId = burgerKing.Id
                };
                var whopper = new MenuCategory
                {
                    Emertimi = "Whopper",
                    Pershkrimi = "The iconic flame-grilled Whopper burger",
                    Renditja = 5,
                    RestaurantId = burgerKing.Id
                };
                var salads = new MenuCategory
                {
                    Emertimi = "Salads",
                    Pershkrimi = "Fresh and crispy salads",
                    Renditja = 6,
                    RestaurantId = burgerKing.Id
                };
                var sides = new MenuCategory
                {
                    Emertimi = "Sides",
                    Pershkrimi = "Crispy fries, onion rings, and more",
                    Renditja = 7,
                    RestaurantId = burgerKing.Id
                };
                var desserts = new MenuCategory
                {
                    Emertimi = "Desserts",
                    Pershkrimi = "Sweet treats to finish your meal",
                    Renditja = 8,
                    RestaurantId = burgerKing.Id
                };
                var beverages = new MenuCategory
                {
                    Emertimi = "Beverages",
                    Pershkrimi = "Refreshing soft drinks and shakes",
                    Renditja = 9,
                    RestaurantId = burgerKing.Id
                };

                categories.AddRange(new[] { beef, chicken, fish, royal, whopper, salads, sides, desserts, beverages });
                context.MenuCategories.AddRange(categories);
                context.SaveChanges();


                var beefItems = new List<MenuItems>
                {
                    new MenuItems
                    {
                        Emertimi = "Single BBQ Beefacon",
                        Pershkrimi = "Grilled beef layered with beefacon and tangy BBQ sauce.",
                        Cmimi = 3.90m,
                        Foto = "menuitems/beefacon.jpg",
                        Disponueshme = true,
                        Alergjene = "Gluten, Dairy, Eggs, Soy, Mustard",
                        Kalori = 750,
                        CategoryId = beef.Id
                    },
                    new MenuItems
                    {
                        Emertimi = "Double BBQ Beefacon",
                        Pershkrimi = "Double beef, bacon, and smoky BBQ in every bite",
                        Cmimi = 6.50m,
                        Foto = "menuitems/doublebeefacon.jpg",
                        Disponueshme = true,
                        Alergjene = "Gluten, Dairy, Eggs, Soy, Mustard",
                        Kalori = 950,
                        CategoryId = beef.Id
                    },
                    new MenuItems
                    {
                        Emertimi = "Single Mushroom Swiss",
                        Pershkrimi = "Beef, mushrooms, and Swiss cheese.",
                        Cmimi = 5.90m,
                        Foto = "menuitems/mushroom.jpg",
                        Disponueshme = true,
                        Alergjene = "Gluten, Dairy, Eggs, Soy",
                        Kalori = 700,
                        CategoryId = beef.Id
                    },
                    new MenuItems
                    {
                        Emertimi = "Cheeseburger",
                        Pershkrimi = "Beef burger with cheese.",
                        Cmimi = 3.90m,
                        Foto = "menuitems/cheeseburger.jpg",
                        Disponueshme = true,
                        Alergjene = "Gluten, Dairy, Eggs, Soy",
                        Kalori = 650,
                        CategoryId = beef.Id
                    },

                    new MenuItems
                    {
                        Emertimi = "Double Cheeseburger",
                        Pershkrimi = " Double beef burger with cheese.",
                        Cmimi = 5.90m,
                        Foto = "menuitems/doublecheese.jpg",
                        Disponueshme = true,
                        Alergjene = "Gluten, Dairy, Eggs, Soy",
                        Kalori = 970,
                        CategoryId = beef.Id
                    },
                    new MenuItems
                    {
                        Emertimi = "Long Cheesy Onion Beef",
                        Pershkrimi = " Beef, cheese, and caramelized onions.",
                        Cmimi = 6.99m,
                        Foto = "menuitems/onionlong.jpg",
                        Disponueshme = true,
                        Alergjene = "Gluten, Dairy, Eggs, Soy",
                        Kalori = 750,
                        CategoryId = beef.Id
                    },

                };
                context.MenuItems.AddRange(beefItems);
                context.SaveChanges();

                var chickenItems = new List<MenuItems>
                {

                    new MenuItems
                    {
                        Emertimi = "Spicy Tendercrisp",
                        Pershkrimi = "Crispy chicken with a spicy kick.",
                        Cmimi = 4.50m,
                        Foto = "menuitems/spicychicken.jpg",
                        Disponueshme = true,
                        Alergjene = "Gluten, Dairy, Eggs, Soy",
                        Kalori = 575,
                        CategoryId = chicken.Id
                    },
                    new MenuItems
                    {
                        Emertimi = "French Chicken",
                        Pershkrimi = "Crispy chicken with fresh toopings.",
                        Cmimi = 5.50m,
                        Foto = "menuitems/frenchchicken.jpg",
                        Disponueshme = true,
                        Alergjene = "Gluten, Dairy, Eggs, Soy",
                        Kalori = 565,
                        CategoryId = chicken.Id
                    },
                    new MenuItems
                    {
                        Emertimi = "BK Chick'N Crisp",
                        Pershkrimi = "Crispy chicken fillet with fresh lettuce and savory sauce.",
                        Cmimi = 6.00m,
                        Foto = "menuitems/chickcrisp.jpg",
                        Disponueshme = true,
                        Alergjene = "Gluten, Dairy, Eggs, Soy",
                        Kalori = 570,
                        CategoryId = chicken.Id
                    },
                    new MenuItems
                    {
                        Emertimi = "Long Chicken",
                        Pershkrimi = "Crispy chicken with lettuce and mayo",
                        Cmimi = 6.00m,
                        Foto = "menuitems/longchicken.jpg",
                        Disponueshme = true,
                        Alergjene = "Gluten, Dairy, Eggs, Soy",
                        Kalori = 600,
                        CategoryId = chicken.Id
                    },
                    new MenuItems
                    {
                        Emertimi = "Tendergrill",
                        Pershkrimi = "Tender grilled chicken with fresh toppings.",
                        Cmimi = 6.00m,
                        Foto = "menuitems/tendergrill.jpg",
                        Disponueshme = true,
                        Alergjene = "Gluten, Dairy, Eggs, Soy",
                        Kalori = 450,
                        CategoryId = chicken.Id
                    },
                    new MenuItems
                    {
                        Emertimi = "Crispy Fried Chicken",
                        Pershkrimi = "Crispy fried chicken with fresh toppings",
                        Cmimi = 6.00m,
                        Foto = "menuitems/friedchicken.jpg",
                        Disponueshme = true,
                        Alergjene = "Gluten, Dairy, Eggs, Soy",
                        Kalori = 580,
                        CategoryId = chicken.Id
                    }

                };
                context.MenuItems.AddRange(chickenItems);
                context.SaveChanges();

                var fishItems = new List<MenuItems>
                {
                    new MenuItems
                    {
                        Emertimi = "Fish'N Crisp",
                        Pershkrimi = "Crispy fish with fresh toppings",
                        Cmimi = 4.00m,
                        Foto = "menuitems/fishh.jpg",
                        Disponueshme = true,
                        Alergjene = "Gluten, Dairy, Eggs, Soy,Fish",
                        Kalori = 500,
                        CategoryId = fish.Id
                    }
                };
                context.MenuItems.AddRange(fishItems);
                context.SaveChanges();

                var royalItems = new List<MenuItems>
                {
                    new MenuItems
                    {
                        Emertimi = "Supreme Nachos Deluxe Tendercrisp",
                        Pershkrimi = "Tendercrisp chicken with nachos, cheese, and sauce",
                        Cmimi = 6.50m,
                        Foto = "menuitems/supremenacho.jpg",
                        Disponueshme = true,
                        Alergjene = "Gluten, Dairy, Eggs, Soy,Chicken,Mustard",
                        Kalori = 750,
                        CategoryId = royal.Id

                    },
                    new MenuItems
                    {
                        Emertimi = "Triple Whopper Jr with Cheese",
                        Pershkrimi = "Triple beef patties with cheese and fresh toppings",
                        Cmimi = 6.50m,
                        Foto = "menuitems/triple.jpg",
                        Disponueshme = true,
                        Alergjene = "Gluten, Dairy, Eggs, Soy",
                        Kalori = 1100,
                        CategoryId = royal.Id

                    },
                    new MenuItems
                    {
                        Emertimi = "Signature Steakhouse Whopper®",
                        Pershkrimi = "Beef, bacon, cheese, and Steakhouse sauce.",
                        Cmimi = 6.50m,
                        Foto = "menuitems/signature.jpg",
                        Disponueshme = true,
                        Alergjene = "Gluten, Dairy, Eggs, Soy,Mustard,Pork",
                        Kalori = 900,
                        CategoryId = royal.Id

                    },


                };
                context.MenuItems.AddRange(royalItems);
                context.SaveChanges();

                var whopperItems = new List<MenuItems>
                {
                    new MenuItems
                    {
                        Emertimi = "Whopper",
                        Pershkrimi = "Flame-grilled beef with fresh toppings.",
                        Cmimi = 5.00m,
                        Foto = "menuitems/whopper.jpg",
                        Disponueshme = true,
                        Alergjene = "Gluten, Dairy, Eggs, Soy",
                        Kalori = 650,
                        CategoryId = whopper.Id
                    },
                    new MenuItems
                    {
                        Emertimi = "Whopper with Cheese",
                        Pershkrimi = "Beef patty with cheese and fresh toppings",
                        Cmimi = 5.90m,
                        Foto = "menuitems/whoppercheese.jpg",
                        Disponueshme = true,
                        Alergjene = "Gluten, Dairy, Eggs, Soy",
                        Kalori = 750,
                        CategoryId = whopper.Id
                    },

                    new MenuItems
                    {
                        Emertimi = "Whopper Jr",
                        Pershkrimi = "Beef patty with cheese and fresh toppings",
                        Cmimi = 5.00m,
                        Foto = "menuitems/whopperjr.jpg",
                        Disponueshme = true,
                        Alergjene = "Gluten, Dairy, Eggs, Soy",
                        Kalori = 750,
                        CategoryId = whopper.Id
                    }
                };
                context.MenuItems.AddRange(whopperItems);
                context.SaveChanges();


                var saladsItems = new List<MenuItems>
                {
                    new MenuItems
                    {
                        Emertimi = "Salad",
                        Pershkrimi = "Fresh greens with veggies and dressing",
                        Cmimi = 4.00m,
                        Foto = "menuitems/bkcsalad.jpg",
                        Disponueshme = true,
                        Alergjene = "Dairy",
                        Kalori = 150,
                        CategoryId = salads.Id
                    },

                    new MenuItems
                    {
                        Emertimi = "Mushroom Veggie Burger",
                        Pershkrimi = "Veggie patty with mushrooms and fresh toppings.",
                        Cmimi = 4.00m,
                        Foto = "menuitems/veggie.jpg",
                        Disponueshme = true,
                        Alergjene = "Dairy",
                        Kalori = 250,
                        CategoryId = salads.Id
                    }
                };

                context.MenuItems.AddRange(saladsItems);
                context.SaveChanges();

                var sidesItems = new List<MenuItems>
                {
                    new MenuItems
                    {
                        Emertimi = "Nuggets",
                        Pershkrimi = "Crispy golden nuggets.",
                        Cmimi = 3.00m,
                        Foto = "menuitems/nuggets.jpg",
                        Disponueshme = true,
                        Alergjene = "Gluten",
                        Kalori = 45,
                        CategoryId = sides.Id
                    },
                    new MenuItems
                    {
                        Emertimi = "Onion Rings",
                        Pershkrimi = "Crispy battered onion rings.",
                        Cmimi = 3.50m,
                        Foto = "menuitems/onionrings.jpg",
                        Disponueshme = true,
                        Alergjene = "Gluten, Dairy, Eggs",
                        Kalori = 350,
                        CategoryId = sides.Id
                    },
                    new MenuItems
                    {
                        Emertimi = "French Fries",
                        Pershkrimi = "Crispy golden fries.",
                        Cmimi = 2.50m,
                        Foto = "menuitems/frenchfries.jpg",
                        Disponueshme = true,
                        Alergjene = "Gluten",
                        Kalori = 300,
                        CategoryId = sides.Id
                    },
                    new MenuItems
                    {
                        Emertimi = "Mozzarella Sticks",
                        Pershkrimi = "Crispy fried mozzarella sticks.",
                        Cmimi = 4.00m,
                        Foto = "menuitems/mozzarella.jpg",
                        Disponueshme = true,
                        Alergjene = "Gluten, Dairy, Eggs",
                        Kalori = 400,
                        CategoryId = sides.Id
                    },
                    new MenuItems
                    {
                        Emertimi = "Cheese Fries",
                        Pershkrimi = "Fries topped with melted cheese.",
                        Cmimi = 3.50m,
                        Foto = "menuitems/cheesefries.jpg",
                        Disponueshme = true,
                        Alergjene = "Gluten, Dairy, Eggs",
                        Kalori = 450,
                        CategoryId = sides.Id
                    }
                };
                context.MenuItems.AddRange(sidesItems);
                context.SaveChanges();

                var dessertsItems = new List<MenuItems>
                {
                    new MenuItems
                    {
                        Emertimi = "HERSHEY’S Sundae Pie",
                        Pershkrimi = "Chocolate sundae pie with HERSHEY’S® topping",
                        Cmimi = 3.00m,
                        Foto = "menuitems/sundae.jpg",
                        Disponueshme = true,
                        Alergjene = "Dairy, Gluten,Eggs,Nuts",
                        Kalori = 355,
                        CategoryId = desserts.Id
                    },
                    new MenuItems
                    {
                        Emertimi = "Vanilla Soft Serve",
                        Pershkrimi = "Vanilla soft-serve ice cream.",
                        Cmimi = 2.50m,
                        Foto = "menuitems/vanillaicecream.jpg",
                        Disponueshme = true,
                        Alergjene = "Dairy, Gluten,Eggs",
                        Kalori = 250,
                        CategoryId = desserts.Id
                    },
                    new MenuItems
                    {
                        Emertimi = "Affogato Sundae",
                        Pershkrimi = "Soft-serve ice cream with espresso and chocolate topping",
                        Cmimi = 3.00m,
                        Foto = "menuitems/affogato.png",
                        Disponueshme = true,
                        Alergjene = "Dairy, Gluten,Eggs",
                        Kalori = 300,
                        CategoryId = desserts.Id
                    }

                };
                context.MenuItems.AddRange(dessertsItems);
                context.SaveChanges();

                var beveragesItems = new List<MenuItems>
                {
                    new MenuItems
                    {
                        Emertimi = "Cold Soft Drink",
                        Pershkrimi = "Classic Coca-Cola,Sprite,Fanta....",
                        Cmimi = 1.50m,
                        Foto = "menuitems/coke.jpg",
                        Disponueshme = true,
                        Alergjene = "None",
                        Kalori = 150,
                        CategoryId = beverages.Id
                    },

                    new MenuItems
                    {
                        Emertimi = "Water",
                        Pershkrimi = "Water",
                        Cmimi = 1.00m,
                        Foto = "menuitems/water.jpg",
                        Disponueshme = true,
                        Alergjene = "None",
                        Kalori = 00,
                        CategoryId = beverages.Id
                    }
                };
                context.MenuItems.AddRange(beveragesItems);
                context.SaveChanges();
            }





            var categoriesSushi = new List<MenuCategory>();

            var sushico = context.Restaurants.FirstOrDefault(r => r.Emertimi == "SushiCo");

            if (sushico != null)
            {
                var appetizer = new MenuCategory
                {
                    Emertimi = "Appetizer",
                    Pershkrimi = "A variety of small dishes to awaken your appetite",
                    Renditja = 1,
                    RestaurantId = sushico.Id
                };

                var soup = new MenuCategory
                {
                    Emertimi = "Soup",
                    Pershkrimi = "Warm and comforting soups to start your meal",
                    Renditja = 2,
                    RestaurantId = sushico.Id
                };
                var dimsum = new MenuCategory
                {
                    Emertimi = "Dim Sum",
                    Pershkrimi = "Steamed or fried dumplings filled with delicious ingredients",
                    Renditja = 3,
                    RestaurantId = sushico.Id
                };
                var ramen = new MenuCategory
                {
                    Emertimi = "Ramen",
                    Pershkrimi = "Savory noodle soups with rich broths and toppings",
                    Renditja = 4,
                    RestaurantId = sushico.Id
                };
                var saladS = new MenuCategory
                {
                    Emertimi = "Salads",
                    Pershkrimi = "Fresh and vibrant salads with Asian-inspired flavors",
                    Renditja = 5,
                    RestaurantId = sushico.Id
                };

                var chickenS = new MenuCategory
                {
                    Emertimi = "Chicken",
                    Pershkrimi = "Tender chicken dishes with Asian flavors",
                    Renditja = 6,
                    RestaurantId = sushico.Id
                };
                var beefS = new MenuCategory
                {
                    Emertimi = "Beef",
                    Pershkrimi = "Savory beef dishes with Asian flavors",
                    Renditja = 7,
                    RestaurantId = sushico.Id
                };
                var seafoodS = new MenuCategory
                {
                    Emertimi = "Seafood",
                    Pershkrimi = "Fresh seafood dishes with Asian flavors",
                    Renditja = 8,
                    RestaurantId = sushico.Id
                };
                var noodlesS = new MenuCategory
                {
                    Emertimi = "Noodles",
                    Pershkrimi = "Delicious noodle dishes with Asian flavors",
                    Renditja = 9,
                    RestaurantId = sushico.Id
                };

                var rice = new MenuCategory
                {
                    Emertimi = "Rice",
                    Pershkrimi = "Flavorful rice dishes with Asian flavors",
                    Renditja = 10,
                    RestaurantId = sushico.Id
                };
                var donburi = new MenuCategory
                {
                    Emertimi = "Donburi",
                    Pershkrimi = "Hearty rice bowls topped with savory ingredients",
                    Renditja = 11,
                    RestaurantId = sushico.Id
                };
                var sushirolls = new MenuCategory
                {
                    Emertimi = "Sushi Rolls",
                    Pershkrimi = "Fresh and creative sushi rolls with various fillings",
                    Renditja = 12,
                    RestaurantId = sushico.Id
                };
                var nigiri = new MenuCategory
                {
                    Emertimi = "Nigiri",
                    Pershkrimi = "Sliced raw fish atop small mounds of rice",
                    Renditja = 13,
                    RestaurantId = sushico.Id
                };
                var setmenu = new MenuCategory
                {
                    Emertimi = "Set Menu",
                    Pershkrimi = "Curated combinations of dishes for a complete meal",
                    Renditja = 14,
                    RestaurantId = sushico.Id
                };
                var sashimi = new MenuCategory
                {
                    Emertimi = "Sashimi",
                    Pershkrimi = "Thinly sliced raw fish served without rice",
                    Renditja = 15,
                    RestaurantId = sushico.Id
                };
                var specialrolls = new MenuCategory
                {
                    Emertimi = "Special Rolls",
                    Pershkrimi = "Unique and creative sushi rolls with special ingredients",
                    Renditja = 16,
                    RestaurantId = sushico.Id
                };
                var cookedrolls = new MenuCategory
                {
                    Emertimi = "Cooked Rolls",
                    Pershkrimi = "Warm and flavorful rolls for a rich sushi experience",
                    Renditja = 17,
                    RestaurantId = sushico.Id
                };
                var beveragesS = new MenuCategory
                {
                    Emertimi = "Cold Drinks",
                    Pershkrimi = "Fresh dinks",
                    Renditja = 18,
                    RestaurantId = sushico.Id
                };
                var extra = new MenuCategory
                {
                    Emertimi = "Extra",
                    Pershkrimi = "Extra sauces",
                    Renditja = 19,
                    RestaurantId = sushico.Id
                };
                var alcohol = new MenuCategory
                {
                    Emertimi = "Alcohol",
                    Pershkrimi = "Alcohol drinks",
                    Renditja = 20,
                    RestaurantId = sushico.Id
                };
                categoriesSushi.AddRange(new[] { appetizer, soup, dimsum, ramen, saladS, chickenS, beefS, seafoodS, noodlesS, rice, donburi, sushirolls, nigiri, setmenu, sashimi, specialrolls, cookedrolls, beveragesS, extra, alcohol });
                context.MenuCategories.AddRange(categoriesSushi);
                context.SaveChanges();

                var appetizerItems = new List<MenuItems>
                {
                    new MenuItems {
                        Emertimi = "Edamame with Parmesan",
                        Pershkrimi = "Steamed edamame tossed with Parmesan cheese and a hint of garlic.",
                        Cmimi = 5.30m,
                        Foto = "sushico/edamame.jpg",
                        Disponueshme = true,
                        Alergjene = "Soy, Dairy",
                        Kalori = 200,
                        CategoryId = appetizer.Id
                    },
                    new MenuItems {
                        Emertimi = "Chicken Katsu Finger",
                        Pershkrimi = "Crispy Japanese-style chicken fingers.",
                        Cmimi = 6.80m,
                        Foto = "sushico/katsu.jpg",
                        Disponueshme = true,
                        Alergjene = "Gluten,Eggs,Soy, Dairy",
                        Kalori = 420,
                        CategoryId = appetizer.Id
                    },
                    new MenuItems {
                        Emertimi = " Veal Tataki with Truffle",
                        Pershkrimi = "Lightly seared veal slices served with aromatic truffle sauce.",
                        Cmimi = 11.80m,
                        Foto = "sushico/tataki.jpg",
                        Disponueshme = true,
                        Alergjene = "Gluten,Eggs,Soy, Dairy",
                        Kalori = 350,
                        CategoryId = appetizer.Id
                    },
                    new MenuItems {
                        Emertimi = " Japanese Potato",
                        Pershkrimi = "Crunchy potatoes with a delicate umami flavor.",
                        Cmimi = 5.70m,
                        Foto = "sushico/japanesepotato.jpg",
                        Disponueshme = true,
                        Alergjene = "Gluten,Soy, Dairy",
                        Kalori = 300,
                        CategoryId = appetizer.Id
                    },
                    new MenuItems {
                        Emertimi = "Shrimp Bomb",
                        Pershkrimi = "Crispy shrimp tossed in a creamy spicy sauce",
                        Cmimi = 8.20m,
                        Foto = "sushico/shrimpbomb.jpg",
                        Disponueshme = true,
                        Alergjene = "Shellfish,Eggs,Gluten,Soy",
                        Kalori = 400,
                        CategoryId = appetizer.Id
                    },
                    new MenuItems {
                        Emertimi = "Salmon Carpaccio",
                        Pershkrimi = "Crispy shrimp tossed in a creamy spicy sauce",
                        Cmimi = 8.20m,
                        Foto = "sushico/carpaccio.jpg",
                        Disponueshme = true,
                        Alergjene = "Gluten,Soy,Dairy",
                        Kalori = 250,
                        CategoryId = appetizer.Id
                    },
                    new MenuItems {
                        Emertimi = "Tempura Mix",
                        Pershkrimi = "Crispy shrimp and seasonal vegetables in light tempura batter, served with dipping sauce.",
                        Cmimi = 14.50m,
                        Foto = "sushico/tempura.jpg",
                        Disponueshme = true,
                        Alergjene = "Soy,Fish",
                        Kalori = 400,
                        CategoryId = appetizer.Id
                    },
                    new MenuItems {
                        Emertimi = "Salmon Tataki",
                        Pershkrimi = "Crispy shrimp and seasonal vegetables in light tempura batter, served with dipping sauce.",
                        Cmimi = 9.10m,
                        Foto = "sushico/salmontataki.jpg",
                        Disponueshme = true,
                        Alergjene = "Soy,Fish,Sesame,Gluten",
                        Kalori = 300,
                        CategoryId = appetizer.Id
                    }
                };
                context.MenuItems.AddRange(appetizerItems);
                context.SaveChanges();

                var soupItems = new List<MenuItems>
                {
                    new MenuItems
                    {
                        Emertimi = "Wonton Soup",
                        Pershkrimi = "Delicate wontons in a savory broth with vegetables and a hint of ginger.",
                        Cmimi = 4.50m,
                        Foto = "sushico/wonton.jpg",
                        Disponueshme = true,
                        Alergjene = "Soy, Gluten,Eggs,Shellfish",
                        Kalori = 250,
                        CategoryId = soup.Id
                    },
                    new MenuItems
                    {
                        Emertimi = "Ramen Soup",
                        Pershkrimi = "Savory noodle soup with rich broth, tender noodles, and various toppings.",
                        Cmimi = 6.00m,
                        Foto = "sushico/ramen.jpg",
                        Disponueshme = true,
                        Alergjene = "Gluten, Soy, Eggs",
                        Kalori = 550,
                        CategoryId = soup.Id
                    },
                    new MenuItems
                    {
                        Emertimi = "Miso Soup",
                        Pershkrimi = "Traditional Japanese miso broth with tofu, seaweed, and spring onions.",
                        Cmimi = 4.50m,
                        Foto = "sushico/miso.jpg",
                        Disponueshme = true,
                        Alergjene = "Fish, Soy",
                        Kalori = 80,
                        CategoryId = soup.Id
                    },
                    new MenuItems
                    {
                        Emertimi = "Sea Food Soup",
                        Pershkrimi = "A hearty broth with a mix of fresh seafood, vegetables, and aromatic herbs.",
                        Cmimi = 7.90m,
                        Foto = "sushico/seafoods.jpg",
                        Disponueshme = true,
                        Alergjene = "Crustaceans, Soy,Mollusks",
                        Kalori = 400,
                        CategoryId = soup.Id
                    }
                };
                context.MenuItems.AddRange(soupItems);
                context.SaveChanges();

                var dimsumItems = new List<MenuItems>
                {
                    new MenuItems
                    {
                        Emertimi = "Spring Roll",
                        Pershkrimi = "Crispy golden spring rolls filled with fresh vegetables and served with a tangy dipping sauce.",
                        Cmimi = 5.50m,
                        Foto = "sushico/springroll.jpg",
                        Disponueshme = true,
                        Alergjene = "Gluten, Soy, Eggs",
                        Kalori = 150,
                        CategoryId = dimsum.Id
                    },
                    new MenuItems
                    {
                        Emertimi = "Korean Wonton",
                        Pershkrimi = "Crispy Korean-style wontons filled with seasoned meat and vegetables, served with a savory dipping sauce.",
                        Cmimi = 6.60m,
                        Foto = "sushico/koreanwonton.jpg",
                        Disponueshme = true,
                        Alergjene = "Gluten, Soy, Eggs, Shellfish",
                        Kalori = 200,
                        CategoryId = dimsum.Id
                    },
                    new MenuItems
                    {
                        Emertimi = "Shrimp gyoza",
                        Pershkrimi = "Delicate dumplings filled with fresh shrimp and vegetables, pan-seared and served with a savory dipping sauce.",
                        Cmimi = 6.60m,
                        Foto = "sushico/gyoza.jpg",
                        Disponueshme = true,
                        Alergjene = "Crustaceans,Gluten, Soy, Eggs",
                        Kalori = 190,
                        CategoryId = dimsum.Id
                    },
                    new MenuItems
                    {
                        Emertimi = "Duck Meat Bun",
                        Pershkrimi = "Soft steamed bun filled with flavorful, tender duck meat.",
                        Cmimi = 6.60m,
                        Foto = "sushico/duckbun.jpg",
                        Disponueshme = true,
                        Alergjene = "Crustaceans,Gluten, Soy, Eggs",
                        Kalori = 190,
                        CategoryId = dimsum.Id
                    },
                    new MenuItems
                    {
                        Emertimi = "Steamed Beef Dumplings",
                        Pershkrimi = "Tender beef dumplings steamed to perfection, served with a savory dipping sauce.",
                        Cmimi = 6.10m,
                        Foto = "sushico/dumpling.jpg",
                        Disponueshme = true,
                        Alergjene = "Gluten, Soy, Eggs",
                        Kalori = 250,
                        CategoryId = dimsum.Id
                    },
                    new MenuItems
                    {
                        Emertimi = "Chicken Bun",
                        Pershkrimi = "Soft steamed bun filled with tender, seasoned chicken.",
                        Cmimi = 5.00m,
                        Foto = "sushico/chickenbun.jpg",
                        Disponueshme = true,
                        Alergjene = "Gluten, Soy, Eggs",
                        Kalori = 250,
                        CategoryId = dimsum.Id
                    },

                };
                context.MenuItems.AddRange(dimsumItems);
                context.SaveChanges();

                var ramenItems = new List<MenuItems>
                {
                    new MenuItems
                    {
                        Emertimi = "Wonton Ramen",
                        Pershkrimi = "Savory ramen noodles in a rich broth, served with delicate wontons and vegetables.",
                        Cmimi = 10.90m,
                        Foto = "sushico/wontonramen.jpg",
                        Disponueshme = true,
                        Alergjene = "Gluten, Soy, Eggs,Fish",
                        Kalori = 400,
                        CategoryId = dimsum.Id
                    },
                    new MenuItems
                    {
                        Emertimi = "Chicken Ramen",
                        Pershkrimi = "Noodles in a flavorful chicken broth with tender chicken, vegetables, and a soft-boiled egg.",
                        Cmimi = 9.70m,
                        Foto = "sushico/chickenramen.jpg",
                        Disponueshme = true,
                        Alergjene = "Gluten, Soy, Eggs,Fish",
                        Kalori = 400,
                        CategoryId = dimsum.Id
                    },
                    new MenuItems
                    {
                        Emertimi = "Shrimp Ramen",
                        Pershkrimi = "Noodles in a savory broth with succulent shrimp, vegetables, and a soft-boiled egg.",
                        Cmimi = 12.00m,
                        Foto = "sushico/shrimpramen.jpg",
                        Disponueshme = true,
                        Alergjene = "Crustaceans,Gluten, Soy, Eggs",
                        Kalori = 450,
                        CategoryId = dimsum.Id
                    },
                    new MenuItems
                    {
                        Emertimi = "Beef Ramen",
                        Pershkrimi = "Ramen noodles served in a rich beef broth with tender slices of beef, vegetables, and a soft-boiled egg.",
                        Cmimi = 10.90m,
                        Foto = "sushico/beeframen.jpg",
                        Disponueshme = true,
                        Alergjene = "Gluten, Soy, Eggs",
                        Kalori = 490,
                        CategoryId = dimsum.Id
                    }
                };
                context.MenuItems.AddRange(ramenItems);
                context.SaveChanges();

                var saladItems = new List<MenuItems>
                {
                    new MenuItems
                    {
                        Emertimi = "Quinoa Salad with Salmon",
                        Pershkrimi = "A healthy quinoa salad topped with fresh salmon, mixed greens, and a light citrus dressing.",
                        Cmimi = 10.90m,
                        Foto = "sushico/quinoa.jpg",
                        Disponueshme = true,
                        Alergjene = "Soy,Fish,Nuts",
                        Kalori = 300,
                        CategoryId = saladS.Id
                    },
                    new MenuItems
                    {
                        Emertimi = "Spicy Cabbage Salad",
                        Pershkrimi = "Crunchy cabbage tossed in a zesty, spicy dressing for a bold and refreshing flavor.",
                        Cmimi = 3.30m,
                        Foto = "sushico/cabagge.jpg",
                        Disponueshme = true,
                        Alergjene = "Soy, Sesame",
                        Kalori = 120,
                        CategoryId = saladS.Id
                    },
                    new MenuItems
                    {
                        Emertimi = "Seafood Salad",
                        Pershkrimi = "Fresh mixed seafood with crisp greens, tossed in a light and tangy dressing.",
                        Cmimi = 6.00m,
                        Foto = "sushico/saefoodsalad.jpg",
                        Disponueshme = true,
                        Alergjene = "Soy, Gluten,Mollusks,Fish,Crustaceans",
                        Kalori = 250,
                        CategoryId = saladS.Id
                    },
                    new MenuItems
                    {
                        Emertimi = "Sashimi Salad",
                        Pershkrimi = "Fresh slices of raw fish served over mixed greens with a light citrus-soy dressing.",
                        Cmimi = 9.70m,
                        Foto = "sushico/sashimisalad.jpg",
                        Disponueshme = true,
                        Alergjene = "Soy, Gluten,Fish,Sesame",
                        Kalori = 250,
                        CategoryId = saladS.Id
                    },
                    new MenuItems
                    {
                        Emertimi = "Japanese-Style Kani Salad",
                        Pershkrimi = "Imitation crab mixed with fresh vegetables and a creamy, tangy Japanese-style dressing.",
                        Cmimi = 9.70m,
                        Foto = "sushico/kanisalad.jpg",
                        Disponueshme = true,
                        Alergjene = "Eggs,Soy,Crustaceans",
                        Kalori = 320,
                        CategoryId = saladS.Id
                    },
                    new MenuItems
                    {
                        Emertimi = "Kani & Cucumber Salad",
                        Pershkrimi = "Crisp cucumber and imitation crab tossed in a light, creamy dressing.",
                        Cmimi = 7.30m,
                        Foto = "sushico/kanicsalad.jpg",
                        Disponueshme = true,
                        Alergjene = "Sesame,Soy,Eggs,Crustaceans",
                        Kalori = 150,
                        CategoryId = saladS.Id
                    }
                };
                context.MenuItems.AddRange(saladItems);
                context.SaveChanges();

                var chickenItems = new List<MenuItems>
                {
                    new MenuItems
                    {
                        Emertimi = "Chicken Katsu Bowl",
                        Pershkrimi = "Crispy breaded chicken served over steamed rice with vegetables and savory sauce.",
                        Cmimi = 7.20m,
                        Foto = "sushico/chickenkatsu.jpg",
                        Disponueshme = true,
                        Alergjene = "Soy, Gluten, Eggs,Sesame",
                        Kalori = 600,
                        CategoryId = chickenS.Id
                    },
                    new MenuItems
                    {
                        Emertimi = "Korean-Style Chicken Bulgogi",
                        Pershkrimi = "Tender chicken marinated in a sweet and savory Korean sauce, grilled to perfection and served with vegetables.",
                        Cmimi = 9.70m,
                        Foto = "sushico/bulgogi.jpg",
                        Disponueshme = true,
                        Alergjene = "Soy, Gluten, Eggs,Sesame",
                        Kalori = 450,
                        CategoryId = chickenS.Id
                    },
                    new MenuItems
                    {
                        Emertimi = "Chicken with Vegetables",
                        Pershkrimi = "Tender chicken stir-fried with fresh seasonal vegetables in a light savory sauce.",
                        Cmimi = 8.50m,
                        Foto = "sushico/vegetable.jpg",
                        Disponueshme = true,
                        Alergjene = "Gluten, Soy, Eggs",
                        Kalori = 400,
                        CategoryId = chickenS.Id
                    },
                    new MenuItems
                    {
                        Emertimi = "Chicken with Red Curry",
                        Pershkrimi = "Tender chicken cooked in a rich and aromatic red curry sauce with vegetables.",
                        Cmimi = 9.70m,
                        Foto = "sushico/currychicken.jpg",
                        Disponueshme = true,
                        Alergjene = "Gluten,Fish,Nuts",
                        Kalori = 450,
                        CategoryId = chickenS.Id
                    },
                    new MenuItems
                    {
                        Emertimi = "Sweet & Sour Chicken",
                        Pershkrimi = "Crispy chicken pieces tossed in a tangy and sweet sauce with bell peppers and pineapple.",
                        Cmimi = 9.10m,
                        Foto = "sushico/sweetsour.jpg",
                        Disponueshme = true,
                        Alergjene = "Gluten,Eggs,Soy,Sesame",
                        Kalori = 600,
                        CategoryId = chickenS.Id
                    },
                    new MenuItems
                    {
                        Emertimi = "Chicken Manchurian",
                        Pershkrimi = "Crispy chicken cooked in a savory and slightly spicy Indo-Chinese sauce with vegetables.",
                        Cmimi = 9.10m,
                        Foto = "sushico/manchurian.jpg",
                        Disponueshme = true,
                        Alergjene = "Gluten,Eggs,Soy,Sesame",
                        Kalori = 600,
                        CategoryId = chickenS.Id
                    }
                };
                context.MenuItems.AddRange(chickenItems);
                context.SaveChanges();

                var beefItems = new List<MenuItems>
                {
                    new MenuItems {
                        Emertimi = "Beef with Crispy Eggplant",
                        Pershkrimi = "Tender beef stir-fried with crispy eggplant in a savory, slightly sweet sauce.",
                        Cmimi = 12.60m,
                        Foto = "sushico/eggplantbeef.jpg",
                        Disponueshme = true,
                        Alergjene = "Gluten,Soy,Sesame",
                        Kalori = 600,
                        CategoryId = beefS.Id
                    },
                    new MenuItems {
                        Emertimi = "Korean-Style Beef Bulgogi",
                        Pershkrimi = "Tender beef marinated in a sweet and savory Korean sauce, grilled and served with vegetables",
                        Cmimi = 11.50m,
                        Foto = "sushico/beefbulgogi.jpg",
                        Disponueshme = true,
                        Alergjene = "Gluten,Soy,Sesame,Eggs",
                        Kalori = 600,
                        CategoryId = beefS.Id
                    },
                    new MenuItems {
                        Emertimi = "Beef with Green Peppers",
                        Pershkrimi = "Tender beef stir-fried with fresh green peppers in a savory sauce.",
                        Cmimi = 11.60m,
                        Foto = "sushico/beefgreen.jpg",
                        Disponueshme = true,
                        Alergjene = "Gluten,Soy,Sesame",
                        Kalori = 500,
                        CategoryId = beefS.Id
                    },
                    new MenuItems {
                        Emertimi = "Spicy Beef with Mushrooms",
                        Pershkrimi = "Tender beef stir-fried with mushrooms and spicy seasonings for a bold, flavorful dish.",
                        Cmimi = 12.10m,
                        Foto = "sushico/mushroombeeg.jpg",
                        Disponueshme = true,
                        Alergjene = "Gluten,Soy,Sesame",
                        Kalori = 500,
                        CategoryId = beefS.Id
                    },
                    new MenuItems {
                        Emertimi = "Thai-Style Beef with Chili & Basil",
                        Pershkrimi = "Tender beef stir-fried with fresh chili and aromatic basil in a savory Thai sauce.",
                        Cmimi = 12.60m,
                        Foto = "sushico/thaibeef.jpg",
                        Disponueshme = true,
                        Alergjene = "Gluten,Soy,Sesame",
                        Kalori = 500,
                        CategoryId = beefS.Id
                    },
                    new MenuItems {
                        Emertimi = "Beef with Black Garlic",
                        Pershkrimi = "Tender beef cooked with rich, aromatic black garlic in a savory sauce.",
                        Cmimi = 12.10m,
                        Foto = "sushico/beefgarlic.jpg",
                        Disponueshme = true,
                        Alergjene = "Gluten,Soy,Sesame",
                        Kalori = 550,
                        CategoryId = beefS.Id
                    }
                };
                context.MenuItems.AddRange(beefItems);
                context.SaveChanges();

                var seafoodItems = new List<MenuItems>
                {
                    new MenuItems
                    {
                        Emertimi = "Beef with Black Garlic",
                        Pershkrimi = "Grilled salmon glazed with a sweet and savory teriyaki sauce, served with vegetables.",
                        Cmimi = 14.50m,
                        Foto = "sushico/salmonteryaki.jpg",
                        Disponueshme = true,
                        Alergjene = "Gluten,Soy,Sesame,Fish",
                        Kalori = 470,
                        CategoryId = seafoodS.Id
                    },
                    new MenuItems
                    {
                        Emertimi = "Beef with Black Garlic",
                        Pershkrimi = "Delicately steamed salmon served with a light, savory sauce.",
                        Cmimi = 13.40m,
                        Foto = "sushico/steamedsalmon.jpg",
                        Disponueshme = true,
                        Alergjene = "Gluten,Soy,Fish",
                        Kalori = 360,
                        CategoryId = seafoodS.Id
                    },
                    new MenuItems
                    {
                        Emertimi = "Spicy Calamari with Cashews",
                        Pershkrimi = "Crispy calamari stir-fried with spicy seasonings and crunchy cashews.",
                        Cmimi = 13.40m,
                        Foto = "sushico/spicycalamari.jpg",
                        Disponueshme = true,
                        Alergjene = "Mollusks,Nuts,Soy,Gluten",
                        Kalori = 450,
                        CategoryId = seafoodS.Id
                    },
                    new MenuItems
                    {
                        Emertimi = "Sweet & Sour Shrimp",
                        Pershkrimi = "Crispy shrimp tossed in a tangy and sweet sauce with vegetables",
                        Cmimi = 14.50m,
                        Foto = "sushico/ssshrimp.jpg",
                        Disponueshme = true,
                        Alergjene = "Crustaceans,Sesame,Soy,Gluten",
                        Kalori = 450,
                        CategoryId = seafoodS.Id
                    },
                    new MenuItems
                    {
                        Emertimi = "Shrimp with Cashews",
                        Pershkrimi = "Succulent shrimp stir-fried with crunchy cashews in a savory sauce.",
                        Cmimi = 14.50m,
                        Foto = "sushico/cashewsshrimp.jpg",
                        Disponueshme = true,
                        Alergjene = "Crustaceans,Sesame,Soy,Gluten,Nuts",
                        Kalori = 570,
                        CategoryId = seafoodS.Id
                    }

                };
                context.MenuItems.AddRange(seafoodItems);
                context.SaveChanges();

                var noodleItems = new List<MenuItems>
                {
                    new MenuItems
                    {
                        Emertimi = "Udon with Shrimp & Black Garlic",
                        Pershkrimi = "Thick udon noodles stir-fried with shrimp and rich black garlic in a savory sauce",
                        Cmimi = 11.50m,
                        Foto = "sushico/shrimpgarlic.jpg",
                        Disponueshme = true,
                        Alergjene = "Crustaceans,Sesame,Soy,Gluten",
                        Kalori = 570,
                        CategoryId = noodlesS.Id
                    },
                    new MenuItems
                    {
                        Emertimi = "Udon with Chicken & Black Garlic",
                        Pershkrimi = "Thick udon noodles stir-fried with tender chicken and rich black garlic in a savory sauce.",
                        Cmimi = 11.50m,
                        Foto = "sushico/shrimpgarlic.jpg",
                        Disponueshme = true,
                        Alergjene = "Sesame,Soy,Gluten",
                        Kalori = 570,
                        CategoryId = noodlesS.Id
                    },
                    new MenuItems
                    {
                        Emertimi = "Plain Noodles",
                        Pershkrimi = "Simple stir-fried noodles with a light savory flavor.",
                        Cmimi = 5.50m,
                        Foto = "sushico/plainnoodles.jpg",
                        Disponueshme = true,
                        Alergjene = "Sesame,Soy,Gluten",
                        Kalori = 350,
                        CategoryId = noodlesS.Id
                    },
                    new MenuItems
                    {
                        Emertimi = "Korean-Style Chicken Bulgogi Udon",
                        Pershkrimi = "Thick udon noodles stir-fried with tender chicken in a sweet and savory Korean bulgogi sauce",
                        Cmimi = 9.10m,
                        Foto = "sushico/chickenudon.jpg",
                        Disponueshme = true,
                        Alergjene = "Sesame,Soy,Gluten",
                        Kalori = 560,
                        CategoryId = noodlesS.Id
                    },
                    new MenuItems
                    {
                        Emertimi = "Korean-Style Beef Bulgogi Udon",
                        Pershkrimi = "Thick udon noodles stir-fried with tender beef in a sweet and savory Korean bulgogi sauce.",
                        Cmimi = 10.40m,
                        Foto = "sushico/beefudon.jpg",
                        Disponueshme = true,
                        Alergjene = "Sesame,Soy,Gluten",
                        Kalori = 560,
                        CategoryId = noodlesS.Id
                    },
                    new MenuItems
                    {
                        Emertimi = "Vegetable Noodles",
                        Pershkrimi = "Stir-fried noodles with fresh seasonal vegetables in a light savory sauce.",
                        Cmimi = 10.40m,
                        Foto = "sushico/vegetablenoodle.jpg",
                        Disponueshme = true,
                        Alergjene = "Sesame,Soy,Gluten",
                        Kalori = 400,
                        CategoryId = noodlesS.Id
                    },
                    new MenuItems
                    {
                        Emertimi = "Chicken Noodles",
                        Pershkrimi = "Stir-fried noodles with tender chicken and fresh vegetables in a savory sauce.",
                        Cmimi = 10.40m,
                        Foto = "sushico/chickennoodle.jpg",
                        Disponueshme = true,
                        Alergjene = "Sesame,Soy,Gluten",
                        Kalori = 500,
                        CategoryId = noodlesS.Id
                    },
                    new MenuItems
                    {
                        Emertimi = "Beef Noodles",
                        Pershkrimi = "Stir-fried noodles with tender beef and fresh vegetables in a savory sauce.",
                        Cmimi = 9.20m,
                        Foto = "sushico/beefnoodle.jpg",
                        Disponueshme = true,
                        Alergjene = "Sesame,Soy,Gluten",
                        Kalori = 500,
                        CategoryId = noodlesS.Id
                    },
                    new MenuItems
                    {
                        Emertimi = "Thai-Style Chicken Noodles",
                        Pershkrimi = "Stir-fried noodles with tender chicken, vegetables, and aromatic Thai spices.",
                        Cmimi = 10.30m,
                        Foto = "sushico/thaichicken.jpg",
                        Disponueshme = true,
                        Alergjene = "Sesame,Soy,Gluten",
                        Kalori = 500,
                        CategoryId = noodlesS.Id
                    },
                    new MenuItems
                    {
                        Emertimi = "Thai-Style Beef Noodles",
                        Pershkrimi = "Stir-fried noodles with tender beef, vegetables, and aromatic Thai spices",
                        Cmimi = 11.50m,
                        Foto = "sushico/beefthai.jpg",
                        Disponueshme = true,
                        Alergjene = "Sesame,Soy,Gluten",
                        Kalori = 500,
                        CategoryId = noodlesS.Id
                    },
                    new MenuItems
                    {
                        Emertimi = "Thai-Style Shrimp Noodles",
                        Pershkrimi = "Stir-fried noodles with succulent shrimp, vegetables, and aromatic Thai spices.",
                        Cmimi = 12.60m,
                        Foto = "sushico/shrimpthai.jpg",
                        Disponueshme = true,
                        Alergjene = "Sesame,Soy,Gluten,Fish",
                        Kalori = 560,
                        CategoryId = noodlesS.Id
                    }
                };
                context.MenuItems.AddRange(noodleItems);
                context.SaveChanges();

                var riceItems = new List<MenuItems>
                {
                    new MenuItems
                    {
                        Emertimi = "Steamed Rice",
                        Pershkrimi = "Fluffy, perfectly steamed white rice – a simple and versatile side.",
                        Cmimi = 2.50m,
                        Foto = "sushico/rice.jpg",
                        Disponueshme = true,
                        Alergjene = "None",
                        Kalori = 200,
                        CategoryId = rice.Id
                    },
                    new MenuItems
                    {
                        Emertimi = "Vegetable Rice",
                        Pershkrimi = "Fluffy steamed rice stir-fried with fresh seasonal vegetables.",
                        Cmimi = 5.50m,
                        Foto = "sushico/vegetablerice.jpg",
                        Disponueshme = true,
                        Alergjene = "Soy,Gluten",
                        Kalori = 300,
                        CategoryId = rice.Id
                    },
                    new MenuItems
                    {
                        Emertimi = "Curry Beef Rice",
                        Pershkrimi = "Steamed rice served with tender beef cooked in a flavorful curry sauce.",
                        Cmimi = 7.50m,
                        Foto = "sushico/curryrice.jpg",
                        Disponueshme = true,
                        Alergjene = "Soy,Gluten,Sesame",
                        Kalori = 500,
                        CategoryId = rice.Id
                    },
                    new MenuItems
                    {
                        Emertimi = "Thai-Style Pineapple Rice",
                        Pershkrimi = "Fragrant Thai-style fried rice with pineapple, vegetables, and a touch of spices.",
                        Cmimi = 7.20m,
                        Foto = "sushico/thaiananas.jpg",
                        Disponueshme = true,
                        Alergjene = "Soy,Gluten,Eggs",
                        Kalori = 550,
                        CategoryId = rice.Id
                    },
                    new MenuItems
                    {
                        Emertimi = "Shrimp Rice",
                        Pershkrimi = "Steamed or fried rice served with succulent shrimp and fresh vegetables.",
                        Cmimi = 8.50m,
                        Foto = "sushico/shrimprice.jpg",
                        Disponueshme = true,
                        Alergjene = "Crustaceans,Soy,Gluten,Eggs",
                        Kalori = 550,
                        CategoryId = rice.Id
                    }
                };
                context.MenuItems.AddRange(riceItems);
                context.SaveChanges();

                var donburiItems = new List<MenuItems>
                {

                    new MenuItems
                    {
                        Emertimi = "Unagi Donburi",
                        Pershkrimi = "Grilled eel glazed in sweet soy sauce, served over steamed rice.",
                        Cmimi = 12.70m,
                        Foto = "sushico/unagidonburi.jpg",
                        Disponueshme = true,
                        Alergjene = "Soy,Gluten,Fish",
                        Kalori = 650,
                        CategoryId = donburi.Id
                    },
                    new MenuItems
                    {
                        Emertimi = "Duck Donburi",
                        Pershkrimi = "Sliced duck served over steamed rice with savory sauce.",
                        Cmimi = 12.70m,
                        Foto = "sushico/duckdonburi.jpg",
                        Disponueshme = true,
                        Alergjene = "Soy,Gluten",
                        Kalori = 700,
                        CategoryId = donburi.Id
                    },
                    new MenuItems
                    {
                        Emertimi = "Salmon Donburi",
                        Pershkrimi = "Sliced duck served over steamed rice with savory sauce.",
                        Cmimi = 12.10m,
                        Foto = "sushico/salmondonburi.jpg",
                        Disponueshme = true,
                        Alergjene = "Soy,Gluten.Fish",
                        Kalori = 700,
                        CategoryId = donburi.Id
                    }

                };
                context.MenuItems.AddRange(donburiItems);
                context.SaveChanges();

                var sushirollItems = new List<MenuItems>
                {
                    new MenuItems {
                        Emertimi = "Quinoa California Roll",
                        Pershkrimi = "Sushi roll with quinoa, crab, avocado, and cucumber.",
                        Cmimi = 8.50m,
                        Foto = "sushico/quinoaroll.jpg",
                        Disponueshme = true,
                        Alergjene = "Crustaceans,Soy,Gluten,Eggs",
                        Kalori = 250,
                        CategoryId = sushirolls.Id
                    },
                    new MenuItems {
                        Emertimi = "Maguro Roll",
                        Pershkrimi = "Sushi roll with quinoa, crab, avocado, and cucumber.",
                        Cmimi = 6.60m,
                        Foto = "sushico/maguroroll.jpg",
                        Disponueshme = true,
                        Alergjene = "Soy,Gluten,Fish",
                        Kalori = 250,
                        CategoryId = sushirolls.Id
                    },
                    new MenuItems {
                        Emertimi = "Tofu Takuan Roll",
                        Pershkrimi = "Tofu and pickled radish wrapped with rice and seaweed.",
                        Cmimi = 6.60m,
                        Foto = "sushico/tofuroll.jpg",
                        Disponueshme = true,
                        Alergjene = "Soy,Gluten",
                        Kalori = 250,
                        CategoryId = sushirolls.Id
                    },
                    new MenuItems {
                        Emertimi = "Ebi Roll",
                        Pershkrimi = "Shrimp with rice and seaweed, often with avocado or cucumber.",
                        Cmimi = 6.60m,
                        Foto = "sushico/ebiroll.jpg",
                        Disponueshme = true,
                        Alergjene = "Soy,Gluten,Shellfish,Eggs",
                        Kalori = 250,
                        CategoryId = sushirolls.Id
                    },
                    new MenuItems {
                        Emertimi = "Kani Roll",
                        Pershkrimi = "Crab stick with rice and seaweed, often with avocado or cucumber.",
                        Cmimi = 6.10m,
                        Foto = "sushico/kaniroll.jpg",
                        Disponueshme = true,
                        Alergjene = "Soy,Gluten,Shellfish,Eggs",
                        Kalori = 250,
                        CategoryId = sushirolls.Id
                    },
                    new MenuItems {
                        Emertimi = "Sake Roll",
                        Pershkrimi = "Fresh salmon rolled with rice and seaweed.",
                        Cmimi = 6.60m,
                        Foto = "sushico/sakeroll.jpg",
                        Disponueshme = true,
                        Alergjene = "Soy,Gluten,Shellfish,Eggs",
                        Kalori = 250,
                        CategoryId = sushirolls.Id
                    },
                    new MenuItems {
                        Emertimi = "Unagi Roll",
                        Pershkrimi = "Grilled eel with rice and seaweed, glazed in sweet soy sauce.",
                        Cmimi = 6.60m,
                        Foto = "sushico/unagiroll.jpg",
                        Disponueshme = true,
                        Alergjene = "Soy,Gluten,Fish",
                        Kalori = 350,
                        CategoryId = sushirolls.Id
                    },
                    new MenuItems {
                        Emertimi = "Saba Roll",
                        Pershkrimi = "Marinated mackerel rolled with rice and seaweed.",
                        Cmimi = 5.50m,
                        Foto = "sushico/unagiroll.jpg",
                        Disponueshme = true,
                        Alergjene = "Soy,Gluten,Fish",
                        Kalori = 350,
                        CategoryId = sushirolls.Id
                    },
                    new MenuItems {
                        Emertimi = "Avocado Sake Roll",
                        Pershkrimi = "Fresh salmon and avocado rolled with rice and seaweed.",
                        Cmimi = 7.90m,
                        Foto = "sushico/avocadoroll.jpg",
                        Disponueshme = true,
                        Alergjene = "Soy,Gluten,Fish",
                        Kalori = 350,
                        CategoryId = sushirolls.Id
                    }
                };
                context.MenuItems.AddRange(sushirollItems);
                context.SaveChanges();

                var nigiriItems = new List<MenuItems>
                {
                    new MenuItems
                    {
                        Emertimi = "Salmon poke bowl",
                        Pershkrimi = "Cubed fresh salmon over rice with vegetables and sesame dressing.",
                        Cmimi = 12.70m,
                        Foto = "sushico/salmonbowl.jpg",
                        Disponueshme = true,
                        Alergjene = "Soy,Sesame,Fish",
                        Kalori = 350,
                        CategoryId = nigiri.Id
                    },
                    new MenuItems
                    {
                        Emertimi = "Tamago (N)",
                        Pershkrimi = "Sweet Japanese omelette rolled with rice and seaweed.",
                        Cmimi = 12.70m,
                        Foto = "sushico/tamago.jpg",
                        Disponueshme = true,
                        Alergjene = "Soy,Gluten,Eggs",
                        Kalori = 250,
                        CategoryId = nigiri.Id
                    },
                    new MenuItems
                    {
                        Emertimi = "Ikura (N)",
                        Pershkrimi = "Salmon roe served over rice or as sushi topping.",
                        Cmimi = 5.50m,
                        Foto = "sushico/ikura.jpg",
                        Disponueshme = true,
                        Alergjene = "Soy,Gluten,Fish",
                        Kalori = 250,
                        CategoryId = nigiri.Id
                    },
                    new MenuItems
                    {
                        Emertimi = "Saba (N)",
                        Pershkrimi = "Marinated mackerel served over sushi rice.",
                        Cmimi = 1.70m,
                        Foto = "sushico/ikura.jpg",
                        Disponueshme = true,
                        Alergjene = "Soy,Gluten,Fish",
                        Kalori = 200,
                        CategoryId = nigiri.Id
                    },
                    new MenuItems
                    {
                        Emertimi = "Salmon & Maguro Poke Bowl",
                        Pershkrimi = "Fresh salmon and tuna cubes over rice with vegetables and sesame dressing.",
                        Cmimi = 12.70m,
                        Foto = "sushico/salmonmaguro.jpg",
                        Disponueshme = true,
                        Alergjene = "Soy,Sesame,Fish",
                        Kalori = 550,
                        CategoryId = nigiri.Id
                    },
                    new MenuItems
                    {
                        Emertimi = "Maguro (N)",
                        Pershkrimi = "Fresh salmon and tuna cubes over rice with vegetables and sesame dressing.",
                        Cmimi = 2.80m,
                        Foto = "sushico/maguro.jpg",
                        Disponueshme = true,
                        Alergjene = "Soy,Sesame,Fish",
                        Kalori = 550,
                        CategoryId = nigiri.Id
                    },
                    new MenuItems
                    {
                        Emertimi = "Ebi Tempura (N)",
                        Pershkrimi = "Deep-fried battered shrimp served with dipping sauce.",
                        Cmimi = 3.30m,
                        Foto = "sushico/ebitempura.jpg",
                        Disponueshme = true,
                        Alergjene = "Shellfish,Wheat,Soy,Gluten,Fish",
                        Kalori = 170,
                        CategoryId = nigiri.Id
                    },
                    new MenuItems
                    {
                        Emertimi = "Nigiri Mix 9 pcs",
                        Pershkrimi = "Assorted nigiri sushi with a variety of fresh fish over rice.",
                        Cmimi = 19.50m,
                        Foto = "sushico/nigirimix.jpg",
                        Disponueshme = true,
                        Alergjene = "Shellfish,Fish,Soy,Gluten",
                        Kalori = 400,
                        CategoryId = nigiri.Id
                    }

                };

                context.MenuItems.AddRange(nigiriItems);
                context.SaveChanges();


                var setmenuItems = new List<MenuItems>
                {

                    new MenuItems
                    {
                        Emertimi = "Ebi Love (15pcs)",
                        Pershkrimi = "Shrimp sushi and rolls assortment, 15 pieces.",
                        Cmimi = 19.90m,
                        Foto = "sushico/ebilove.jpg",
                        Disponueshme = true,
                        Alergjene = "Shellfish,Eggs,Soy,Gluten",
                        Kalori = 800,
                        CategoryId = setmenu.Id
                    },
                    new MenuItems
                    {
                        Emertimi = "Warm Nigiri Plate",
                        Pershkrimi = "Assorted nigiri sushi served slightly warm.",
                        Cmimi = 9.90m,
                        Foto = "sushico/warmnigiri.jpg",
                        Disponueshme = true,
                        Alergjene = "Shellfish,Eggs,Soy,Gluten,Fish",
                        Kalori = 450,
                        CategoryId = setmenu.Id
                    },
                    new MenuItems
                    {
                        Emertimi = "Sushi Queen",
                        Pershkrimi = "Assorted premium sushi rolls and nigiri selection..",
                        Cmimi = 22.20m,
                        Foto = "sushico/sushiqueen.jpg",
                        Disponueshme = true,
                        Alergjene = "Shellfish,Eggs,Soy,Gluten,Fish",
                        Kalori = 750,
                        CategoryId = setmenu.Id
                    },
                    new MenuItems
                    {
                        Emertimi = "California Dream",
                        Pershkrimi = "California-style sushi rolls with crab, avocado, and cucumber.",
                        Cmimi = 9.90m,
                        Foto = "sushico/californiadream.jpg",
                        Disponueshme = true,
                        Alergjene = "Shellfish,Eggs,Soy,Gluten",
                        Kalori = 350,
                        CategoryId = setmenu.Id
                    }
                };
                context.MenuItems.AddRange(setmenuItems);
                context.SaveChanges();
                var sashimiItems = new List<MenuItems>
                {

                new MenuItems
                {
                     Emertimi = "Sake Nigiri 12 pcs",
                        Pershkrimi = "Fresh salmon served over sushi rice.",
                        Cmimi = 12.10m,
                        Foto = "sushico/sake.jpg",
                        Disponueshme = true,
                        Alergjene = "Fish,Soy,Gluten",
                        Kalori = 240,
                        CategoryId = sashimi.Id
                },
                new MenuItems
                {
                     Emertimi = "Maguro 12 pcs",
                        Pershkrimi = "Fresh tuna and salmon served over sushi rice.",
                        Cmimi = 12.10m,
                        Foto = "sushico/maguros.jpg",
                        Disponueshme = true,
                        Alergjene = "Fish,Soy,Gluten",
                        Kalori = 280,
                        CategoryId = sashimi.Id
                },
                new MenuItems
                {
                     Emertimi = "Sashimi Mix 12 pcs",
                        Pershkrimi = "Assorted fresh raw fish slices, 12 pieces.",
                        Cmimi = 12.10m,
                        Foto = "sushico/sashimimix.jpg",
                        Disponueshme = true,
                        Alergjene = "Fish,Soy,Gluten,Shellfish",
                        Kalori = 360,
                        CategoryId = sashimi.Id
                },
                new MenuItems
                {
                     Emertimi = "Deluxe Set - 13 pcs",
                        Pershkrimi = "Premium assortment of sushi rolls and nigiri, 13 pieces.",
                        Cmimi = 12.10m,
                        Foto = "sushico/sashimimix.jpg",
                        Disponueshme = true,
                        Alergjene = "Fish,Soy,Gluten,Shellfish,Eggs",
                        Kalori = 550,
                        CategoryId = sashimi.Id
                }

                };
                context.MenuItems.AddRange(sashimiItems);
                context.SaveChanges();


                var specialrollsItems = new List<MenuItems>
                {
                  new MenuItems
                  {
                    Emertimi = "Zen Roll",
                        Pershkrimi = "Fresh sushi roll with a balanced mix of fish and vegetables.",
                        Cmimi = 13.80m,
                        Foto = "sushico/zenroll.jpg",
                        Disponueshme = true,
                        Alergjene = "Fish,Soy,Gluten,Shellfish,Eggs,Sesame,Mustard,Nuts,Milk",
                        Kalori = 550,
                        CategoryId = specialrolls.Id
                  },
                  new MenuItems
                  {
                    Emertimi = "Super Salmon Roll",
                        Pershkrimi = "Salmon sushi roll with rich flavor and fresh ingredients.",
                        Cmimi = 12.60m,
                        Foto = "sushico/superroll.jpg",
                        Disponueshme = true,
                        Alergjene = "Fish,Soy,Gluten,Shellfish,Eggs,Sesame,Mustard,Nuts,Milk",
                        Kalori = 550,
                        CategoryId = specialrolls.Id
                  },
                  new MenuItems
                  {
                    Emertimi = "Ebi Salmon Roll",
                        Pershkrimi = "Shrimp and salmon rolled with rice and seaweed.",
                        Cmimi = 9.70m,
                        Foto = "sushico/ebispecial.jpg",
                        Disponueshme = true,
                        Alergjene = "Soy,Eggs,Gluten",
                        Kalori = 400,
                        CategoryId = specialrolls.Id
                  },
                  new MenuItems
                  {
                    Emertimi = "Veggie Roll",
                        Pershkrimi = "Fresh vegetables rolled with rice and seaweed.",
                        Cmimi = 8.50m,
                        Foto = "sushico/ebispecial.jpg",
                        Disponueshme = true,
                        Alergjene = "Soy",
                        Kalori = 200,
                        CategoryId = specialrolls.Id
                  },
                  new MenuItems
                  {
                    Emertimi = "Zen Dragon Roll",
                        Pershkrimi = "Sushi roll with eel, avocado, and cucumber, topped with sweet sauce.",
                        Cmimi = 12.60m,
                        Foto = "sushico/dragon.jpg",
                        Disponueshme = true,
                        Alergjene = "Soy",
                        Kalori = 200,
                        CategoryId = specialrolls.Id
                  },
                  new MenuItems
                  {
                    Emertimi = "Quinoa Avocado Roll",
                        Pershkrimi = "Sushi roll with quinoa and fresh avocado.",
                        Cmimi = 6.60m,
                        Foto = "sushico/dragon.jpg",
                        Disponueshme = true,
                        Alergjene = "None",
                        Kalori = 250,
                        CategoryId = specialrolls.Id
                  },
                  new MenuItems
                  {
                    Emertimi = "Fire Salmon Roll",
                        Pershkrimi = "Spicy salmon roll with rice and seaweed.",
                        Cmimi = 12.60m,
                        Foto = "sushico/firesalmon.jpg",
                        Disponueshme = true,
                        Alergjene = "Soy,Eggs",
                        Kalori = 300,
                        CategoryId = specialrolls.Id
                  }
                };
                context.MenuItems.AddRange(specialrollsItems);
                context.SaveChanges();

                var cookedrollsItems = new List<MenuItems>
                {
                    new MenuItems
                    {
                        Emertimi = "Salmon Aburi Roll",
                        Pershkrimi = "Partially seared salmon roll with rice and seaweed.",
                        Cmimi = 11.50m,
                        Foto = "sushico/aburi.jpg",
                        Disponueshme = true,
                        Alergjene = "Soy,Eggs",
                        Kalori = 300,
                        CategoryId = cookedrolls.Id
                    },
                    new MenuItems
                    {
                        Emertimi = "Chicken Teriyaki Roll",
                        Pershkrimi = "Chicken teriyaki with rice and seaweed.",
                        Cmimi = 10.90m,
                        Foto = "sushico/teriyaki.jpg",
                        Disponueshme = true,
                        Alergjene = "Shellfish,Wheat,Soy,Gluten,Fish",
                        Kalori = 400,
                        CategoryId = cookedrolls.Id
                    },
                    new MenuItems
                    {
                        Emertimi = "Tempura Sake Maki Roll",
                        Pershkrimi = "Salmon tempura rolled with rice and seaweed, crispy on the outside.",
                        Cmimi = 11.60m,
                        Foto = "sushico/makiroll.jpg",
                        Disponueshme = true,
                        Alergjene = "Soy,Eggs",
                        Kalori = 350,
                        CategoryId = cookedrolls.Id
                    },
                    new MenuItems
                    {
                        Emertimi = "Crazy Philly Roll",
                        Pershkrimi = "Philadelphia-style roll with salmon, cream cheese, and cucumber.",
                        Cmimi = 12.60m,
                        Foto = "sushico/phillyroll.jpg",
                        Disponueshme = true,
                        Alergjene = "Soy",
                        Kalori = 350,
                        CategoryId = cookedrolls.Id
                    },
                    new MenuItems
                    {
                        Emertimi = "Yuzu Special Roll",
                        Pershkrimi = "Sushi roll with a citrusy yuzu sauce, fish, and fresh vegetables.",
                        Cmimi = 11.50m,
                        Foto = "sushico/yuzu.jpg",
                        Disponueshme = true,
                        Alergjene = "Soy",
                        Kalori = 350,
                        CategoryId = cookedrolls.Id
                    },
                    new MenuItems
                    {
                        Emertimi = "Fish & Chips Roll",
                        Pershkrimi = "Crispy fried fish with rice and seaweed, inspired by classic fish & chips.",
                        Cmimi = 11.50m,
                        Foto = "sushico/fishroll.jpg",
                        Disponueshme = true,
                        Alergjene = "Soy,Eggs",
                        Kalori = 350,
                        CategoryId = cookedrolls.Id
                    }
                };
                context.MenuItems.AddRange(cookedrollsItems);
                context.SaveChanges();

                var beveragessItems = new List<MenuItems>
                {
                    new MenuItems
                    {
                        Emertimi = "Pepsi",
                        Pershkrimi = "Cold Drink",
                        Cmimi = 1.50m,
                        Foto = "sushico/pepsi.jpg",
                        Disponueshme = true,
                        Alergjene = "None",
                        Kalori = 0,
                        CategoryId = beveragesS.Id },
                    new MenuItems
                    {
                        Emertimi = "Red Bull",
                        Pershkrimi = "Cold Drink",
                        Cmimi = 4.00m,
                        Foto = "sushico/redbull.jpg",
                        Disponueshme = true,
                        Alergjene = "None",
                        Kalori = 0,
                        CategoryId = beveragesS.Id },
                    new MenuItems
                    {
                        Emertimi = "Rugove Water",
                        Pershkrimi = "Cold Drink",
                        Cmimi = 1.30m,
                        Foto = "sushico/uje.jpg",
                        Disponueshme = true,
                        Alergjene = "None",
                        Kalori = 0,
                        CategoryId = beveragesS.Id },
                    new MenuItems
                    {
                        Emertimi = "Santal Apple Juice",
                        Pershkrimi = "Cold Drink",
                        Cmimi = 2.00m,
                        Foto = "sushico/apple.jpg",
                        Disponueshme = true,
                        Alergjene = "None",
                        Kalori = 0,
                        CategoryId = beveragesS.Id }

                };
                context.MenuItems.AddRange(beveragessItems);
                context.SaveChanges();

                var extraItems = new List<MenuItems> {
                new MenuItems{
                        Emertimi = "Spicy Mayo",
                        Pershkrimi = "Extra Spicy Mayo Sauce",
                        Cmimi = 0.60m,
                        Foto = "sushico/spicy.jpg",
                        Disponueshme = true,
                        Alergjene = "Soy,Gluten",
                        Kalori = 150,
                        CategoryId = extra.Id
                     },
                new MenuItems{
                        Emertimi = "Sriracha",
                        Pershkrimi = "Extra Sriracha Sauce",
                        Cmimi = 0.60m,
                        Foto = "sushico/sriracha.jpg",
                        Disponueshme = true,
                        Alergjene = "Soy,Gluten",
                        Kalori = 150,
                        CategoryId = extra.Id
                     },
                new MenuItems{
                        Emertimi = "Ginger",
                        Pershkrimi = "Ginger",
                        Cmimi = 0.60m,
                        Foto = "sushico/ginger.jpg",
                        Disponueshme = true,
                        Alergjene = "Soy,Gluten",
                        Kalori = 150,
                        CategoryId = extra.Id
                     }

                };
                context.MenuItems.AddRange(extraItems);
                context.SaveChanges();

                var alcoholItems = new List<MenuItems>
                {
                    new MenuItems
                    {
                        Emertimi = "Peja",
                        Pershkrimi = "Peja",
                        Cmimi = 2.50m,
                        Foto = "sushico/peja.jpg",
                        Disponueshme = true,
                        Alergjene = "None",
                        Kalori = 150,
                        CategoryId = alcohol.Id
                     },
                    new MenuItems
                    {
                        Emertimi = "Asahi Beer",
                        Pershkrimi = "Crisp and refreshing Japanese beer.",
                        Cmimi = 4.50m,
                        Foto = "sushico/asahi.jpg",
                        Disponueshme = true,
                        Alergjene = "None",
                        Kalori = 150,
                        CategoryId = alcohol.Id
                     },
                    new MenuItems
                    {
                        Emertimi = "Corona",
                        Pershkrimi = "Corona Beer",
                        Cmimi = 6.00m,
                        Foto = "sushico/corona.jpg",
                        Disponueshme = true,
                        Alergjene = "None",
                        Kalori = 150,
                        CategoryId = alcohol.Id
                     }
                 };

                context.MenuItems.AddRange(alcoholItems);
                context.SaveChanges();


            }
            var categoriesPastaFasta = new List<MenuCategory>();
            var pastafasta = context.Restaurants.FirstOrDefault(r => r.Emertimi == "Pasta Fasta");

            if (pastafasta != null)
            {
                var coldpasta = new MenuCategory
                {
                    Emertimi = "Cold Pasta",
                    Pershkrimi = "Delicious cold pasta dishes perfect for a refreshing meal.",
                    Renditja = 1,
                    RestaurantId = pastafasta.Id
                };
                var warmpasta = new MenuCategory
                {
                    Emertimi = "Warm Pasta",
                    Pershkrimi = "Warm pasta tossed in a delicious sauce with fresh ingredients.",
                    Renditja = 2,
                    RestaurantId = pastafasta.Id
                };
                var chipspasta = new MenuCategory
                {
                    Emertimi = "Pasta Chips",
                    Pershkrimi = "Crispy baked pasta chips seasoned to perfection.",
                    Renditja = 3,
                    RestaurantId = pastafasta.Id
                };
                var extras = new MenuCategory
                {
                    Emertimi = "Extras",
                    Pershkrimi = "Add extra ingredients and sides to enhance your dish.",
                    Renditja = 4,
                    RestaurantId = pastafasta.Id
                };
                var box = new MenuCategory
                {
                    Emertimi = "Surprise box",
                    Pershkrimi = "You pick the box, we pick the flavor!",
                    Renditja = 5,
                    RestaurantId = pastafasta.Id
                };

                var drinkss = new MenuCategory
                {
                    Emertimi = "Drinks",
                    Pershkrimi = "A variety of refreshing drinks to keep you cool and satisfied.",
                    Renditja = 6,
                    RestaurantId = pastafasta.Id
                };

                categoriesPastaFasta.AddRange(new[] { coldpasta, warmpasta, chipspasta, extras, box, drinkss });
                context.MenuCategories.AddRange(categoriesPastaFasta);
                context.SaveChanges();

                var coldpastaItems = new List<MenuItems>
                {
                    new MenuItems
                    {
                        Emertimi = "Arugula",
                        Pershkrimi = "Penne pasta tossed with fresh arugula, mozzarella, cherry tomatoes, and a light balsamic dressing.",
                        Cmimi = 9.50m,
                        Foto = "pastafasta/Arugula.png",
                        Disponueshme = true,
                        Alergjene = "Gluten,Dairy",
                        Kalori = 450,
                        CategoryId = coldpasta.Id

                    },
                     new MenuItems
                    {
                        Emertimi = "Cucudill",
                        Pershkrimi = "Fresh penne pasta tossed with crisp cucumbers, red onions, and a light house salad dressing for a refreshing taste.",
                        Cmimi = 8.90m,
                        Foto = "pastafasta/Cucudill.png",
                        Disponueshme = true,
                        Alergjene = "Gluten,Mustard",
                        Kalori = 360,
                        CategoryId = coldpasta.Id

                    },
                     new MenuItems
                    {
                        Emertimi = "Butterflies",
                        Pershkrimi = "Penne pasta mixed with pesto, fresh spinach, red beans, cucumbers, red onions, and a light salad dressing.",
                        Cmimi = 9.70m,
                        Foto = "pastafasta/Butterflies.png",
                        Disponueshme = true,
                        Alergjene = "Gluten,Nuts",
                        Kalori = 480,
                        CategoryId = coldpasta.Id

                    },
                     new MenuItems
                    {
                        Emertimi = "Sprinkled Tuna",
                        Pershkrimi = "Penne pasta mixed with tuna, boiled eggs, fresh vegetables, and a light salad dressing.",
                        Cmimi =  10.50m,
                        Foto = "pastafasta/SprinkledTuna.png",
                        Disponueshme = true,
                        Alergjene = "Gluten,Egg,Fish",
                        Kalori = 520,
                        CategoryId = coldpasta.Id

                    },
                      new MenuItems
                    {
                        Emertimi = "Sweety",
                        Pershkrimi = "Penne pasta mixed with pineapple, carrots, turkey, and a light salad dressing.",
                        Cmimi =  10.20m,
                        Foto = "pastafasta/Sweety.png",
                        Disponueshme = true,
                        Alergjene = "Gluten",
                        Kalori = 490,
                        CategoryId = coldpasta.Id

                    },
                        new MenuItems
                    {
                        Emertimi = "Bleta",
                        Pershkrimi = "Penne pasta mixed with honey, cinnamon, cheese, arugula, and walnuts for a sweet and savory flavor.",
                        Cmimi =  10.80m,
                        Foto = "pastafasta/Bleta.png",
                        Disponueshme = true,
                        Alergjene = "Gluten,Dairy,Nuts",
                        Kalori = 540,
                        CategoryId = coldpasta.Id

                    },
                        new MenuItems
                    {
                        Emertimi = "Iron Mac",
                        Pershkrimi = "Penne pasta mixed with asparagus, green beans, arugula, and a light salad dressing.",
                        Cmimi =  9.90m,
                        Foto = "pastafasta/IronMac.png",
                        Disponueshme = true,
                        Alergjene = "Gluten",
                        Kalori = 400,
                        CategoryId = coldpasta.Id

                    },
                          new MenuItems
                    {
                        Emertimi = "Kësulkuqja",
                        Pershkrimi = "Penne pasta mixed with strawberries, radish, red onions, and a light salad dressing.",
                        Cmimi =  9.60m,
                        Foto = "pastafasta/Kesulkuqja.png",
                        Disponueshme = true,
                        Alergjene = "Gluten",
                        Kalori = 370,
                        CategoryId = coldpasta.Id

                    },

                };
                context.MenuItems.AddRange(coldpastaItems);
                context.SaveChanges();


                var warmpastaItems = new List<MenuItems>
                {
                    new MenuItems
                    {
                        Emertimi = "Bolognese",
                        Pershkrimi = "Classic Italian pasta dish with a rich and hearty meat sauce made from ground beef, tomatoes, onions, garlic, and herbs.",
                        Cmimi = 11.50m,
                        Foto = "pastafasta/Bolognese.png",
                        Disponueshme = true,
                        Alergjene = "Gluten",
                        Kalori = 600,
                        CategoryId = warmpasta.Id

                    },
                     new MenuItems
                    {
                        Emertimi = "Quattro Formaggi",
                        Pershkrimi = "A rich and creamy blend of four cheeses melted over perfectly cooked pasta, creating a deliciously cheesy experience.",
                        Cmimi = 9.50m,
                        Foto = "pastafasta/QuattroFormaggi.png",
                        Disponueshme = true,
                        Alergjene = "Gluten",
                        Kalori = 550,
                        CategoryId = warmpasta.Id

                    },
                     new MenuItems
                    {
                        Emertimi = "Chicken Pesto & Broccoli",
                        Pershkrimi = "Tender chicken and fresh broccoli tossed with penne in a creamy pesto sauce.",
                        Cmimi = 7.50m,
                        Foto = "pastafasta/Pesto.png",
                        Disponueshme = true,
                        Alergjene = "Gluten",
                        Kalori = 400,
                        CategoryId = warmpasta.Id

                    },
                      new MenuItems
                    {
                        Emertimi = "Arrabiata",
                        Pershkrimi = "Penne pasta tossed in a spicy tomato and garlic sauce, delivering a bold and flavorful kick.",
                        Cmimi = 5.50m,
                        Foto = "pastafasta/Arrabiata.png",
                        Disponueshme = true,
                        Alergjene = "Gluten,Dairy,Mustard",
                        Kalori = 490,
                        CategoryId = warmpasta.Id

                    },
                      new MenuItems
                    {
                        Emertimi = "Beef & Mushrooms",
                        Pershkrimi = "Pasta tossed with tender beef slices and sautéed mushrooms in a savory sauce for a hearty and satisfying meal.",
                        Cmimi = 4.50m,
                        Foto = "pastafasta/Mushrooms.png",
                        Disponueshme = true,
                        Alergjene = "Gluten,Dairy",
                        Kalori = 400,
                        CategoryId = warmpasta.Id

                    },
                      new MenuItems
                    {
                        Emertimi = "Pizza Sauce",
                        Pershkrimi = "A rich and tangy tomato-based sauce, seasoned with herbs and spices, perfect for spreading over your pizza.",
                        Cmimi = 4.20m,
                        Foto = "pastafasta/PizzaSauce.png",
                        Disponueshme = true,
                        Alergjene = "None",
                        Kalori = 450,
                        CategoryId = warmpasta.Id

                    },
                       new MenuItems
                    {
                        Emertimi = "Chicken Curry",
                        Pershkrimi = "Tender chicken pieces cooked in a flavorful curry sauce with aromatic spices, served over your choice of pasta or rice.",
                        Cmimi = 3.20m,
                        Foto = "pastafasta/ChickenCurry.png",
                        Disponueshme = true,
                        Alergjene = "Gluten,Dairy",
                        Kalori = 300,
                        CategoryId = warmpasta.Id

                    },
                      new MenuItems
                    {
                        Emertimi = "Napoli",
                        Pershkrimi = "Classic tomato sauce with garlic and herbs, served over pasta or as a base for your favorite pizza.",
                        Cmimi = 3.80m,
                        Foto = "pastafasta/Napoli.png",
                        Disponueshme = true,
                        Alergjene = "Gluten",
                        Kalori = 600,
                        CategoryId = warmpasta.Id

                    },
                       new MenuItems
                    {
                        Emertimi = "Veggie Tuna",
                        Pershkrimi = "A refreshing mix of tuna, fresh vegetables, and a light salad dressing, perfect for a healthy meal.",
                        Cmimi = 3.80m,
                        Foto = "pastafasta/VeggieTuna.png",
                        Disponueshme = true,
                        Alergjene = "Gluten,Egg,Fish",
                        Kalori = 500,
                        CategoryId = warmpasta.Id

                    },
                        new MenuItems
                    {
                        Emertimi = "Mac'n'cheese",
                        Pershkrimi = "Creamy macaroni pasta baked with a rich blend of cheeses for a comforting and cheesy delight.",
                        Cmimi = 4.80m,
                        Foto = "pastafasta/MacnCheese.png",
                        Disponueshme = true,
                        Alergjene = "Gluten,Dairy",
                        Kalori = 520,
                        CategoryId = warmpasta.Id

                    },
                           new MenuItems
                    {
                        Emertimi = "Lasagna Sauce",
                        Pershkrimi = "A rich and savory tomato-based sauce, blended with herbs and spices, perfect for layering in lasagna.",
                        Cmimi = 8.00m,
                        Foto = "pastafasta/LasagnaSauce.png",
                        Disponueshme = true,
                        Alergjene = "Dairy",
                        Kalori = 800,
                        CategoryId = warmpasta.Id

                    },
                       new MenuItems
                    {
                        Emertimi = "Alfredo",
                        Pershkrimi = "A creamy and rich sauce made with butter, cream, and Parmesan cheese, perfect for tossing with your favorite pasta.",
                        Cmimi = 5.20m,
                        Foto = "pastafasta/Alfredo.png",
                        Disponueshme = true,
                        Alergjene = "Dairy,Gluten",
                        Kalori = 300,
                        CategoryId = warmpasta.Id

                    },
                       new MenuItems
                    {
                        Emertimi = "Spinach Ricotta",
                        Pershkrimi = "A creamy and flavorful combination of fresh spinach and ricotta cheese, perfect for tossing with pasta or layering in lasagna.",
                        Cmimi = 4.20m,
                        Foto = "pastafasta/SpinachRicotta.png",
                        Disponueshme = true,
                        Alergjene = "Dairy,Gluten",
                        Kalori = 350,
                        CategoryId = warmpasta.Id

                    },
                       new MenuItems
                    {
                        Emertimi = "Aglio e Olio",
                        Pershkrimi = "A creamy and flavorful combination of fresh spinach and ricotta cheese, perfect for tossing with pasta or layering in lasagna.",
                        Cmimi = 4.60m,
                        Foto = "pastafasta/AglioeOlio.png",
                        Disponueshme = true,
                        Alergjene = "Dairy,Gluten",
                        Kalori = 900,
                        CategoryId = warmpasta.Id

                    },
                         new MenuItems
                    {
                        Emertimi = "Fantasea",
                        Pershkrimi = "A refreshing mix of fresh vegetables and seafood, tossed in a light salad dressing for a vibrant and flavorful dish.",
                        Cmimi = 7.20m,
                        Foto = "pastafasta/Fantasea.png",
                        Disponueshme = true,
                        Alergjene = "Gluten,Fish",
                        Kalori = 400,
                        CategoryId = warmpasta.Id

                    },
                         new MenuItems
                    {
                        Emertimi = "Carbonara",
                        Pershkrimi = "Creamy pasta made with eggs, Parmesan cheese, pancetta, and black pepper for a rich and classic Italian flavor.",
                        Cmimi = 6.20m,
                        Foto = "pastafasta/Carbonara.png",
                        Disponueshme = true,
                        Alergjene = "Gluten,Dairy,Egg",
                        Kalori = 200,
                        CategoryId = warmpasta.Id

                    },


                 };
                context.MenuItems.AddRange(warmpastaItems);
                context.SaveChanges();


                var chipspastaItems = new List<MenuItems>
                {
                    new MenuItems
                    {
                        Emertimi = "Pasta Chips Garlic Sauce",
                        Pershkrimi = "Crispy baked pasta chips served with a flavorful garlic sauce for dipping or drizzling.",
                        Cmimi = 4.50m,
                        Foto = "pastafasta/ClassicChips.png",
                        Disponueshme = true,
                        Alergjene = "Gluten,Dairy",
                        Kalori = 300,
                        CategoryId = chipspasta.Id
                    },
                     new MenuItems
                    {
                        Emertimi = "Pasta Chips Chilly",
                        Pershkrimi = "Crispy baked pasta chips tossed in a spicy chili seasoning for a bold and flavorful snack.",
                        Cmimi = 5.20m,
                        Foto = "pastafasta/PastaChipsChilly.png",
                        Disponueshme = true,
                        Alergjene = "Gluten",
                        Kalori = 350,
                        CategoryId = chipspasta.Id
                    },
                     new MenuItems
                    {
                        Emertimi = "Pasta Chips Ketchup & Basil",
                        Pershkrimi = "Crispy baked pasta chips served with a tangy ketchup and fresh basil for a flavorful snack.",
                        Cmimi = 5.80m,
                        Foto = "pastafasta/PastaChipsKetchup&Basil.png",
                        Disponueshme = true,
                        Alergjene = "Gluten",
                        Kalori = 400,
                        CategoryId = chipspasta.Id
                    },

                    };
                context.MenuItems.AddRange(chipspastaItems);
                context.SaveChanges();

                var extrasItems = new List<MenuItems>
                {
                       new MenuItems
                    {
                        Emertimi = "Parmesan Cheese",
                        Pershkrimi = "Grated Parmesan cheese, perfect for sprinkling over your pasta dishes to add a rich and savory flavor.",
                        Cmimi = 1.50m,
                        Foto = "pastafasta/ParmesanCheese.png",
                        Disponueshme = true,
                        Alergjene = "Dairy",
                        Kalori = 100,
                        CategoryId = extras.Id
                     },
                       new MenuItems
                       {
                        Emertimi = "Fara Mix",
                        Pershkrimi = "A nutritious mix of fresh vegetables and assorted seeds, tossed in a light salad dressing for a healthy and crunchy dish.",
                        Cmimi = 2.50m,
                        Foto = "pastafasta/FaraMix.png",
                        Disponueshme = true,
                        Alergjene = "Gluten,Seeds",
                        Kalori = 200,
                        CategoryId = extras.Id
                       },
                       new MenuItems
                       {
                        Emertimi = "Olive",
                        Pershkrimi = "A fresh pasta or salad dish featuring flavorful olives, tossed with vegetables and a light dressing.",
                        Cmimi = 3.50m,
                        Foto = "pastafasta/Olive.png",
                        Disponueshme = true,
                        Alergjene = "Gluten",
                        Kalori = 250,
                        CategoryId = extras.Id
                       },
                        new MenuItems
                       {
                        Emertimi = "Parsley",
                        Pershkrimi = "Fresh parsley added for a burst of flavor and a touch of green, enhancing the freshness of your dish.",
                        Cmimi = 1.50m,
                        Foto = "pastafasta/Parsley.png",
                        Disponueshme = true,
                        Alergjene = "None",
                        Kalori = 50,
                        CategoryId = extras.Id
                       },
                         new MenuItems
                       {
                        Emertimi = "Tomato",
                        Pershkrimi = "Fresh, ripe tomatoes adding natural sweetness and juiciness, perfect for salads, pasta, and sauces.",
                        Cmimi = 1.00m,
                        Foto = "pastafasta/Tomato.png",
                        Disponueshme = true,
                        Alergjene = "None",
                        Kalori = 100,
                        CategoryId = extras.Id
                       },
                           new MenuItems
                       {
                        Emertimi = "Broccoli",
                        Pershkrimi = "Fresh broccoli florets, lightly steamed or raw, adding a healthy crunch and vibrant green color to your dish.",
                        Cmimi = 1.00m,
                        Foto = "pastafasta/Broccoli.png",
                        Disponueshme = true,
                        Alergjene = "None",
                        Kalori = 200,
                        CategoryId = extras.Id
                       },
                           new MenuItems
                       {
                        Emertimi = "Garlic Oil",
                        Pershkrimi = "Aromatic garlic infused oil, perfect for drizzling over pasta, bread, or salads to enhance flavor.",
                        Cmimi = 2.00m,
                        Foto = "pastafasta/GarlicOil.png",
                        Disponueshme = true,
                        Alergjene = "None",
                        Kalori = 100,
                        CategoryId = extras.Id
                       },
                 };

                context.MenuItems.AddRange(extrasItems);
                context.SaveChanges();

                var boxItems = new List<MenuItems>
                {
                       new MenuItems
                    {
                        Emertimi = "Surprise Box",
                        Pershkrimi = "You pick the box, we pick the flavor! A delightful surprise of pasta dishes and flavors, curated by our chef for a unique dining experience.",
                        Cmimi = 12.00m,
                        Foto = "pastafasta/SurpriseBox.png",
                        Disponueshme = true,
                        Alergjene = "Varies",
                        Kalori = 600,
                        CategoryId = box.Id
                     },
                 };

                context.MenuItems.AddRange(boxItems);
                context.SaveChanges();


                var drinkssItems = new List<MenuItems>
                {
                       new MenuItems
                    {
                        Emertimi = "Coca Cola",
                        Pershkrimi = "Classic Coca-Cola, a refreshing soft drink with its signature sweet and fizzy taste.",
                        Cmimi = 3.00m,
                        Foto = "pastafasta/CocaCola.png",
                        Disponueshme = true,
                        Alergjene = "None",
                        Kalori = 150,
                        CategoryId = drinkss.Id
                     },
                         new MenuItems
                    {
                        Emertimi = "Coca Cola Zero",
                        Pershkrimi = "Coca-Cola Zero, a refreshing soft drink with classic Coke taste but zero sugar and zero calories.",
                        Cmimi = 3.00m,
                        Foto = "pastafasta/CocaColaZero.png",
                        Disponueshme = true,
                        Alergjene = "None",
                        Kalori = 100,
                        CategoryId = drinkss.Id
                     },
                           new MenuItems
                    {
                        Emertimi = "Fanta Orange",
                        Pershkrimi = "Fanta Orange, a refreshing and fizzy soft drink bursting with sweet and tangy orange flavor.",
                        Cmimi = 2.00m,
                        Foto = "pastafasta/FantaOrange.png",
                        Disponueshme = true,
                        Alergjene = "None",
                        Kalori = 300,
                        CategoryId = drinkss.Id
                     },
                          new MenuItems
                    {
                        Emertimi = "Fanta Tropikal",
                        Pershkrimi = "Fanta Tropical, a refreshing fizzy drink with a sweet and exotic tropical fruit flavor.",
                        Cmimi = 2.00m,
                        Foto = "pastafasta/FantaTropikal.png",
                        Disponueshme = true,
                        Alergjene = "None",
                        Kalori = 300,
                        CategoryId = drinkss.Id
                     },
                         new MenuItems
                    {
                        Emertimi = "Schweppes Bitter Lemon",
                        Pershkrimi = "Schweppes Bitter Lemon, a refreshing fizzy drink with a distinct bitter and zesty lemon flavor.",
                        Cmimi = 2.50m,
                        Foto = "pastafasta/SchweppesBitterLemon.png",
                        Disponueshme = true,
                        Alergjene = "None",
                        Kalori = 200,
                        CategoryId = drinkss.Id
                     },
                          new MenuItems
                    {
                        Emertimi = "Sprite",
                        Pershkrimi = "Sprite, a crisp and refreshing lemon-lime flavored soft drink, perfect to quench your thirst.",
                        Cmimi = 1.50m,
                        Foto = "pastafasta/Sprite.png",
                        Disponueshme = true,
                        Alergjene = "None",
                        Kalori = 250,
                        CategoryId = drinkss.Id
                     },

                          new MenuItems
                    {
                        Emertimi = "Water",
                        Pershkrimi = "Fresh and pure drinking water, perfect for staying hydrated anytime.",
                        Cmimi = 1.00m,
                        Foto = "pastafasta/Water.png",
                        Disponueshme = true,
                        Alergjene = "None",
                        Kalori = 100,
                        CategoryId = drinkss.Id
                     },
                          new MenuItems
                    {
                        Emertimi = "Sparkling Water",
                        Pershkrimi = "Refreshing sparkling water with natural bubbles, perfect for staying hydrated with a fizzy twist.",
                        Cmimi = 1.00m,
                        Foto = "pastafasta/SparklingWater.png",
                        Disponueshme = true,
                        Alergjene = "None",
                        Kalori = 100,
                        CategoryId = drinkss.Id
                     },
                           new MenuItems
                    {
                        Emertimi = "Red Bull",
                        Pershkrimi = "Red Bull Energy Drink, a fizzy beverage packed with caffeine and vitamins to boost energy and alertness.",
                        Cmimi = 3.00m,
                        Foto = "pastafasta/RedBull.png",
                        Disponueshme = true,
                        Alergjene = "None",
                        Kalori = 250,
                        CategoryId = drinkss.Id
                     },
                         new MenuItems
                    {
                        Emertimi = "Pasionada",
                        Pershkrimi = "Pasionada, a refreshing tropical fruit juice with the vibrant taste of passion fruit and a hint of sweetness.",
                        Cmimi = 3.00m,
                        Foto = "pastafasta/Pasionada.png",
                        Disponueshme = true,
                        Alergjene = "None",
                        Kalori = 250,
                        CategoryId = drinkss.Id
                     },
                           new MenuItems
                    {
                        Emertimi = "Malinada",
                        Pershkrimi = "Pasionada, a refreshing tropical fruit juice with the vibrant taste of passion fruit and a hint of sweetness.",
                        Cmimi = 4.00m,
                        Foto = "pastafasta/Malinada.png",
                        Disponueshme = true,
                        Alergjene = "None",
                        Kalori = 250,
                        CategoryId = drinkss.Id
                     },



                       new MenuItems
                       {
                        Emertimi = "Iced Tea",
                        Pershkrimi = "Chilled iced tea brewed to perfection, offering a refreshing and flavorful beverage option to accompany your meal.",
                        Cmimi = 2.50m,
                        Foto = "pastafasta/IcedTea.png",
                        Disponueshme = true,
                        Alergjene = "None",
                        Kalori = 100,
                        CategoryId = drinkss.Id
                       },
                        new MenuItems
                       {
                        Emertimi = "Multivitamin Juice",
                        Pershkrimi = "Multivitamin Juice, a refreshing blend of various fruit juices packed with essential vitamins for a healthy boost.",
                        Cmimi = 2.50m,
                        Foto = "pastafasta/MultivitaminJuice.png",
                        Disponueshme = true,
                        Alergjene = "None",
                        Kalori = 100,
                        CategoryId = drinkss.Id
                       },

                         new MenuItems
                       {
                        Emertimi = "Orange Juice",
                        Pershkrimi = "Freshly squeezed orange juice, sweet and tangy, packed with vitamin C for a refreshing start to your day.",
                        Cmimi = 2.50m,
                        Foto = "pastafasta/OrangeJuice.png",
                        Disponueshme = true,
                        Alergjene = "None",
                        Kalori = 100,
                        CategoryId = drinkss.Id
                       },
                          new MenuItems
                       {
                        Emertimi = "Blueberry Juice",
                        Pershkrimi = "Fresh blueberry juice, naturally sweet and rich in antioxidants, perfect for a refreshing and healthy drink.",
                        Cmimi = 1.50m,
                        Foto = "pastafasta/BlueberryJuice.png",
                        Disponueshme = true,
                        Alergjene = "None",
                        Kalori = 200,
                        CategoryId = drinkss.Id
                       },
                            new MenuItems
                       {
                        Emertimi = "Strawberry Juice",
                        Pershkrimi = "Fresh strawberry juice, sweet and naturally fruity, perfect for a refreshing and healthy drink.",
                        Cmimi = 1.50m,
                        Foto = "pastafasta/StrawberryJuice.png",
                        Disponueshme = true,
                        Alergjene = "None",
                        Kalori = 200,
                        CategoryId = drinkss.Id
                       },
                             new MenuItems
                       {
                        Emertimi = "Cherry Juice",
                        Pershkrimi = "Fresh cherry juice, naturally sweet and slightly tart, packed with rich cherry flavor for a refreshing drink.",
                        Cmimi = 1.50m,
                        Foto = "pastafasta/CherryJuice.png",
                        Disponueshme = true,
                        Alergjene = "None",
                        Kalori = 200,
                        CategoryId = drinkss.Id
                       },
                          new MenuItems
                       {
                        Emertimi = "Peach Juice",
                        Pershkrimi = "Fresh peach juice, sweet and naturally fruity, offering a smooth and refreshing taste.",
                        Cmimi = 1.50m,
                        Foto = "pastafasta/PeachJuice.png",
                        Disponueshme = true,
                        Alergjene = "None",
                        Kalori = 200,
                        CategoryId = drinkss.Id
                       },

                 };

                context.MenuItems.AddRange(drinkssItems);
                context.SaveChanges();


            }

            var categoriesProperPizza = new List<MenuCategory>();
            var properpizza = context.Restaurants.FirstOrDefault(r => r.Emertimi == "Proper Pizza");

            if (properpizza != null)
            {
                var pizza = new MenuCategory
                {
                    Emertimi = "Pizza",
                    Pershkrimi = "A classic Italian dish with baked dough, tomato sauce, cheese, and toppings.",
                    Renditja = 1,
                    RestaurantId = properpizza.Id
                };
                var sweetpizza = new MenuCategory
                {
                    Emertimi = " Sweet Pizza",
                    Pershkrimi = "A dessert-style pizza topped with sweet ingredients like chocolate, fruits, and cream.",
                    Renditja = 2,
                    RestaurantId = properpizza.Id
                };
                var drinks = new MenuCategory
                {
                    Emertimi = "Pizza",
                    Pershkrimi = "Drinks",
                    Renditja = 3,
                    RestaurantId = properpizza.Id
                };

                categoriesProperPizza.AddRange(new[] { pizza, sweetpizza, drinks });
                context.MenuCategories.AddRange(categoriesProperPizza);
                context.SaveChanges();

                var pizzaItems = new List<MenuItems>{
                    new MenuItems {
                         Emertimi = "Vegetarian Pizza",
                        Pershkrimi = "A delicious pizza topped with fresh vegetables, tomato sauce, and melted cheese on a crispy crust.",
                        Cmimi = 3.40m,
                        Foto = "properpizza/vegan1.webp",
                        Disponueshme = true,
                        Alergjene = "Gluten,Milk",
                        Kalori = 1800,
                        CategoryId = pizza.Id

                    },
                    new MenuItems {
                         Emertimi = "Crudo Rucola Pizza",
                        Pershkrimi = "A gourmet pizza topped with tomato sauce, mozzarella, prosciutto crudo, fresh arugula, and shaved parmesan.",
                        Cmimi = 3.90m,
                        Foto = "properpizza/rucola1.webp",
                        Disponueshme = true,
                        Alergjene = "Gluten,Milk",
                        Kalori = 2000,
                        CategoryId = pizza.Id

                    },
                    new MenuItems {
                         Emertimi = "Pizza Margharita",
                        Pershkrimi = "A classic pizza with tomato sauce, fresh mozzarella, and basil.",
                        Cmimi = 3.40m,
                        Foto = "properpizza/margarita.webp",
                        Disponueshme = true,
                        Alergjene = "Gluten,Milk",
                        Kalori = 1600,
                        CategoryId = pizza.Id

                    },
                    new MenuItems {
                         Emertimi = "Pizza Funghi",
                        Pershkrimi = "A savory pizza topped with tomato sauce, mozzarella, and fresh mushrooms.",
                        Cmimi = 3.40m,
                        Foto = "properpizza/fungi.webp",
                        Disponueshme = true,
                        Alergjene = "Gluten,Milk",
                        Kalori = 1900,
                        CategoryId = pizza.Id

                    },
                    new MenuItems {
                         Emertimi = "Proper Pizza",
                        Pershkrimi = "A hearty pizza loaded with tomato sauce, mozzarella, and a rich mix of premium toppings.",
                        Cmimi = 3.40m,
                        Foto = "properpizza/proper.webp",
                        Disponueshme = true,
                        Alergjene = "Gluten,Milk",
                        Kalori = 2400,
                        CategoryId = pizza.Id

                    },
                    new MenuItems {
                         Emertimi = "Pizza Prosciutto",
                        Pershkrimi = "A classic pizza topped with tomato sauce, mozzarella, and savory prosciutto.",
                        Cmimi = 3.40m,
                        Foto = "properpizza/proshute.webp",
                        Disponueshme = true,
                        Alergjene = "Gluten,Milk",
                        Kalori = 2200,
                        CategoryId = pizza.Id

                    },
                    new MenuItems {
                         Emertimi = "Pizza Tuna",
                        Pershkrimi = "A flavorful pizza topped with tomato sauce, mozzarella, and tuna, often finished with onions or herbs.",
                        Cmimi = 4.00m,
                        Foto = "properpizza/tuna.webp",
                        Disponueshme = true,
                        Alergjene = "Gluten,Milk",
                        Kalori = 1850,
                        CategoryId = pizza.Id

                    },

                    new MenuItems {
                         Emertimi = "Pizza Oriental",
                        Pershkrimi = "A zesty pizza topped with tomato sauce, mozzarella, chicken or beef, bell peppers, onions, and a blend of oriental spices.",
                        Cmimi = 3.40m,
                        Foto = "properpizza/oriental.webp",
                        Disponueshme = true,
                        Alergjene = "Gluten,Milk",
                        Kalori = 2500,
                        CategoryId = pizza.Id

                    },

                    new MenuItems {
                         Emertimi = "Pizza 4 Cheeses",
                        Pershkrimi = "A rich and creamy pizza topped with a blend of four cheeses—mozzarella, gorgonzola, parmesan, and fontina—on a golden crust.",
                        Cmimi = 3.90m,
                        Foto = "properpizza/cheese.webp",
                        Disponueshme = true,
                        Alergjene = "Gluten,Milk",
                        Kalori = 2600,
                        CategoryId = pizza.Id

                    }
                };
                context.MenuItems.AddRange(pizzaItems);
                context.SaveChanges();

                var sweetPizzaItems = new List<MenuItems> {
                new MenuItems
                {
                         Emertimi = "Pizza Nutella",
                        Pershkrimi = "A sweet dessert pizza with a crispy crust, generously spread with Nutella and optionally topped with fruits or powdered sugar.",
                        Cmimi = 4.00m,
                        Foto = "properpizza/nutella.webp",
                        Disponueshme = true,
                        Alergjene = "Gluten,Milk",
                        Kalori = 2800,
                        CategoryId = sweetpizza.Id
                }

                };
                context.MenuItems.AddRange(sweetPizzaItems);
                context.SaveChanges();

                var drinkItems = new List<MenuItems> {
                new MenuItems
                {
                        Emertimi = "Coca Cola",
                        Pershkrimi = "Coca Cola",
                        Cmimi = 1.50m,
                        Foto = "pastafasta/CocaCola.png",
                        Disponueshme = true,
                        Alergjene = "None",
                        Kalori = 150,
                        CategoryId = drinks.Id
                },
                new MenuItems
                {
                        Emertimi = "Water",
                        Pershkrimi = "Water",
                        Cmimi = 1.00m,
                        Foto = "pastafasta/Water.png",
                        Disponueshme = true,
                        Alergjene = "None",
                        Kalori = 0,
                        CategoryId = drinks.Id
                },
                new MenuItems
                {
                        Emertimi = "Fanta",
                        Pershkrimi = "Fanta",
                        Cmimi = 1.50m,
                        Foto = "pastafasta/FantaOrange.png",
                        Disponueshme = true,
                        Alergjene = "None",
                        Kalori = 0,
                        CategoryId = drinks.Id
                }

                };
                context.MenuItems.AddRange(drinkItems);
                context.SaveChanges();
            }


            var categoriesKFC = new List<MenuCategory>();
            var kfc = context.Restaurants.FirstOrDefault(r => r.Emertimi == "KFC");

            if (kfc != null)
            {
                var burgerandwrap = new MenuCategory
                {
                    Emertimi = "Burger and Wrap",
                    Pershkrimi = "A delicious selection of burgers and wraps, featuring crispy chicken, fresh vegetables, and flavorful sauces, all served on a soft bun or wrapped in a warm tortilla.",
                    Renditja = 1,
                    RestaurantId = kfc.Id
                };
                var buckets = new MenuCategory
                {
                    Emertimi = "Buckets & Pieces",
                    Pershkrimi = "A shareable meal featuring a bucket of crispy, golden pieces of chicken, perfect for enjoying with friends or family.",
                    Renditja = 2,
                    RestaurantId = kfc.Id
                };
                var sidesdrinks = new MenuCategory
                {
                    Emertimi = "Sides & Beverages",
                    Pershkrimi = "Perfect companions for your meal – from tasty sides to refreshing beverages.",
                    Renditja = 3,
                    RestaurantId = kfc.Id
                };

                categoriesKFC.AddRange(new[] { burgerandwrap, buckets, sidesdrinks });
                context.MenuCategories.AddRange(categoriesKFC);
                context.SaveChanges();

                var burgerandwrapItems = new List<MenuItems>
                {
                new MenuItems{
                        Emertimi = "Cheese Burger",
                        Pershkrimi = "A juicy beef patty topped with melted cheese, fresh lettuce, tomato, onions, and a soft bun.",
                        Cmimi = 2.29m,
                        Foto = "kfc/cheese.jpg",
                        Disponueshme = true,
                        Alergjene = "Gluten,Milk,Eggs",
                        Kalori = 600,
                        CategoryId = burgerandwrap.Id
                },
                new MenuItems{
                        Emertimi = "Original Burger",
                        Pershkrimi = "A classic burger with a savory beef patty, fresh lettuce, tomato, onions, and a soft bun.",
                        Cmimi = 2.29m,
                        Foto = "kfc/original.jpg",
                        Disponueshme = true,
                        Alergjene = "Gluten,Milk,Eggs",
                        Kalori = 450,
                        CategoryId = burgerandwrap.Id
                },
                new MenuItems{
                        Emertimi = "Twister Wrap",
                        Pershkrimi = "A soft tortilla wrap filled with seasoned chicken, fresh vegetables, and a creamy sauce, rolled up for a convenient, flavorful meal.",
                        Cmimi = 3.99m,
                        Foto = "kfc/twister.jpg",
                        Disponueshme = true,
                        Alergjene = "Gluten,Milk,Eggs",
                        Kalori = 400,
                        CategoryId = burgerandwrap.Id
                },
                new MenuItems{
                        Emertimi = "Tower Burger",
                        Pershkrimi = "A towering burger stacked with multiple beef patties, cheese, fresh vegetables, and sauces, served on a soft bun.",
                        Cmimi = 4.69m,
                        Foto = "kfc/tower.jpg",
                        Disponueshme = true,
                        Alergjene = "Gluten,Milk,Eggs",
                        Kalori = 800,
                        CategoryId = burgerandwrap.Id
                },
                new MenuItems{
                        Emertimi = "Boxmaster",
                        Pershkrimi = "A hearty meal featuring a combination of burger, fries, and chicken pieces, all served together in one convenient box.",
                        Cmimi = 4.69m,
                        Foto = "kfc/boxmaster.jpg",
                        Disponueshme = true,
                        Alergjene = "Gluten,Milk,Eggs",
                        Kalori = 1000,
                        CategoryId = burgerandwrap.Id
                }

                };
                context.MenuItems.AddRange(burgerandwrapItems);
                context.SaveChanges();

                var bucketsItems = new List<MenuItems>
                {
                    new MenuItems
                    {
                        Emertimi = "2 Hot Wings",
                        Pershkrimi = "Two crispy and spicy chicken wings, perfect as a snack or appetizer.",
                        Cmimi = 1.99m,
                        Foto = "kfc/2hot.png",
                        Disponueshme = true,
                        Alergjene = "Gluten,Milk,Eggs",
                        Kalori = 160,
                        CategoryId = buckets.Id
                    },
                    new MenuItems
                    {
                        Emertimi = "5 Hot Wings",
                        Pershkrimi = "Five crispy and spicy chicken wings, perfect as a snack or shareable appetizer.",
                        Cmimi = 3.99m,
                        Foto = "kfc/5hot.png",
                        Disponueshme = true,
                        Alergjene = "Gluten,Milk,Eggs",
                        Kalori = 500,
                        CategoryId = buckets.Id
                    },
                    new MenuItems
                    {
                        Emertimi = "8 Hot Wings",
                        Pershkrimi = "Eight crispy and spicy chicken wings, perfect for sharing or enjoying as a hearty snack.",
                        Cmimi = 5.99m,
                        Foto = "kfc/8hot.png",
                        Disponueshme = true,
                        Alergjene = "Gluten,Milk,Eggs",
                        Kalori = 640,
                        CategoryId = buckets.Id
                     },
                    new MenuItems
                    {
                        Emertimi = "12 Hot Wings",
                        Pershkrimi = "Twelve crispy and spicy chicken wings, ideal for sharing or a filling snack.",
                        Cmimi = 8.99m,
                        Foto = "kfc/12hot.png",
                        Disponueshme = true,
                        Alergjene = "Gluten,Milk,Eggs",
                        Kalori = 1000,
                        CategoryId = buckets.Id
                     },
                    new MenuItems
                    {
                        Emertimi = "18 Hot Wings",
                        Pershkrimi = "Eighteen crispy and spicy chicken wings, perfect for sharing with friends or as a hearty snack.",
                        Cmimi = 12.59m,
                        Foto = "kfc/18hot.png",
                        Disponueshme = true,
                        Alergjene = "Gluten,Milk,Eggs",
                        Kalori = 1440,
                        CategoryId = buckets.Id
                     },
                    new MenuItems
                    {
                        Emertimi = "3 Crispy Strips",
                        Pershkrimi = "Three golden, crispy chicken strips, seasoned and fried to perfection, perfect as a snack or side.",
                        Cmimi = 3.69m,
                        Foto = "kfc/3crispy.png",
                        Disponueshme = true,
                        Alergjene = "Gluten,Milk,Eggs",
                        Kalori = 300,
                        CategoryId = buckets.Id
                     },
                    new MenuItems
                    {
                        Emertimi = "5 Crispy Strips",
                        Pershkrimi = "Five golden, crispy chicken strips, seasoned and fried to perfection, perfect as a snack or side.",
                        Cmimi = 5.59m,
                        Foto = "kfc/5crispy.png",
                        Disponueshme = true,
                        Alergjene = "Gluten,Milk,Eggs",
                        Kalori = 500,
                        CategoryId = buckets.Id
                     },
                    new MenuItems
                    {
                        Emertimi = "8 Crispy Strips",
                        Pershkrimi = "Eight golden, crispy chicken strips, seasoned and fried to perfection, ideal for sharing or a hearty snack.",
                        Cmimi = 7.99m,
                        Foto = "kfc/8crispy.png",
                        Disponueshme = true,
                        Alergjene = "Gluten,Milk,Eggs",
                        Kalori = 800,
                        CategoryId = buckets.Id
                     },
                    new MenuItems
                    {
                        Emertimi = "12 Crispy Strips",
                        Pershkrimi = "Twelve golden, crispy chicken strips, perfectly seasoned and fried, ideal for sharing or a filling snack.",
                        Cmimi = 11.49m,
                        Foto = "kfc/12crispy.png",
                        Disponueshme = true,
                        Alergjene = "Gluten,Milk,Eggs",
                        Kalori = 1200,
                        CategoryId = buckets.Id
                     }
                 };
                context.MenuItems.AddRange(bucketsItems);
                context.SaveChanges();

                var sidesdrinksItems = new List<MenuItems>
                {
                    new MenuItems
                    {
                        Emertimi = "Chicken Salad",
                        Pershkrimi = "A fresh salad with tender chicken pieces, mixed greens, tomatoes, cucumbers, and a light dressing.",
                        Cmimi = 4.49m,
                        Foto = "kfc/salad.png",
                        Disponueshme = true,
                        Alergjene = "Eggs,Milk,Soy",
                        Kalori = 250,
                        CategoryId = sidesdrinks.Id
                     },
                    new MenuItems
                    {
                        Emertimi = "Fries",
                        Pershkrimi = "Crispy golden fries, lightly salted and perfect as a side or snack.",
                        Cmimi = 1.39m,
                        Foto = "kfc/fries.png",
                        Disponueshme = true,
                        Alergjene = "None",
                        Kalori = 300,
                        CategoryId = sidesdrinks.Id
                     },
                    new MenuItems
                    {
                        Emertimi = "BBQ Sauce",
                        Pershkrimi = "A rich and tangy tomato-based BBQ sauce, perfect for dipping or adding flavor to your meals.",
                        Cmimi = 1.39m,
                        Foto = "kfc/bbq.png",
                        Disponueshme = true,
                        Alergjene = "None",
                        Kalori = 50,
                        CategoryId = sidesdrinks.Id
                     },
                    new MenuItems
                    {
                        Emertimi = "Don Garlos",
                        Pershkrimi = "A bold and spicy sauce with a unique blend of herbs and spices, perfect for dipping or enhancing your favorite dishes.",
                        Cmimi = 1.39m,
                        Foto = "kfc/dongarlos.png",
                        Disponueshme = true,
                        Alergjene = "None",
                        Kalori = 60,
                        CategoryId = sidesdrinks.Id
                     },
                    new MenuItems
                    {
                        Emertimi = "Corn",
                        Pershkrimi = "Sweet and tender corn, steamed or boiled, perfect as a side or snack.",
                        Cmimi = 0.99m,
                        Foto = "kfc/dongarlos.png",
                        Disponueshme = true,
                        Alergjene = "None",
                        Kalori = 80,
                        CategoryId = sidesdrinks.Id
                     },
                    new MenuItems
                    {
                        Emertimi = "Coca Cola",
                        Pershkrimi = "Cola Cola",
                        Cmimi = 1.50m,
                        Foto = "pastafasta/CocaCola.png",
                        Disponueshme = true,
                        Alergjene = "None",
                        Kalori = 200,
                        CategoryId = sidesdrinks.Id
                     },
                    new MenuItems
                    {
                        Emertimi = "Fanta Orange",
                        Pershkrimi = "Fanta Orange",
                        Cmimi = 1.50m,
                        Foto = "pastafasta/FantaOrange.png",
                        Disponueshme = true,
                        Alergjene = "None",
                        Kalori = 200,
                        CategoryId = sidesdrinks.Id
                     },
                    new MenuItems
                    {
                        Emertimi = "Water",
                        Pershkrimi = "Water",
                        Cmimi = 1.00m,
                        Foto = "pastafasta/Water.png",
                        Disponueshme = true,
                        Alergjene = "None",
                        Kalori = 0,
                        CategoryId = sidesdrinks.Id
                     },


                };
                context.MenuItems.AddRange(sidesdrinksItems);
                context.SaveChanges();
            }

            var categoriesGreenandProtein = new List<MenuCategory>();
            var greenandprotein = context.Restaurants.FirstOrDefault(r => r.Emertimi == "Green and Protein");

            if (greenandprotein != null)
            {
                var burgers = new MenuCategory
                {
                    Emertimi = "Burgers",
                    Pershkrimi = "Tasty burgers with a healthy twist",
                    Renditja = 1,
                    RestaurantId = greenandprotein.Id
                };
                var bowls = new MenuCategory
                {
                    Emertimi = "Bowls",
                    Pershkrimi = "Balanced bowls for energy and taste.",
                    Renditja = 2,
                    RestaurantId = greenandprotein.Id
                };
                var salads = new MenuCategory
                {
                    Emertimi = "Salads",
                    Pershkrimi = "Fresh and vibrant salads for a healthy meal.",
                    Renditja = 3,
                    RestaurantId = greenandprotein.Id
                };
                var wraps = new MenuCategory
                {
                    Emertimi = "Wraps",
                    Pershkrimi = "Delicious wraps packed with flavor and nutrition.",
                    Renditja = 4,
                    RestaurantId = greenandprotein.Id
                };
                var lightmeals = new MenuCategory
                {
                    Emertimi = "Light Meals",
                    Pershkrimi = "Lighter options for a satisfying meal.",
                    Renditja = 5,
                    RestaurantId = greenandprotein.Id
                };
                var juicessmoothies = new MenuCategory
                {
                    Emertimi = "Juices & Smoothies",
                    Pershkrimi = "Refreshing and nutritious beverages to complement your meal.",
                    Renditja = 6,
                    RestaurantId = greenandprotein.Id
                };

                categoriesGreenandProtein.AddRange(new[] { burgers, bowls, salads, wraps, lightmeals, juicessmoothies });
                context.MenuCategories.AddRange(categoriesGreenandProtein);
                context.SaveChanges();


                var burgersItems = new List<MenuItems>
                {
                    new MenuItems
                    {
                        Emertimi = "Queen Premium",
                        Pershkrimi = "Chicken meatballs with eggs, cheese, tomatoes, pickled cucumbers, lettuce, soy bean sauce & Greek yogurt.",
                        Cmimi = 5.99m,
                        Foto = "greenandprotein/queenpremium.png",
                        Disponueshme = true,
                        Alergjene = "Eggs, Milk, Soy, Gluten (bun)",
                        Kalori = 700,
                        CategoryId = burgers.Id
                    },
                    new MenuItems
                    {
                        Emertimi = "Duplex Day",
                        Pershkrimi = "Avocado, eggs, cucumbers, tomatoes, lettuce, soy bean sauce & Greek yogurt.",
                        Cmimi = 6.49m,
                        Foto = "greenandprotein/duplexday.png",
                        Disponueshme = true,
                        Alergjene = "Eggs, Milk, Soy",
                        Kalori = 600,
                        CategoryId = burgers.Id
                    },
                    new MenuItems
                    {
                        Emertimi = "Queen L",
                        Pershkrimi = "Chicken meatballs with tomatoes, pickled cucumbers, lettuce, soy bean sauce & Greek yogurt.",
                        Cmimi = 5.49m,
                        Foto = "greenandprotein/queenL.png",
                        Disponueshme = true,
                        Alergjene = "Milk, Soy, Gluten (bun)",
                        Kalori = 500,
                        CategoryId = burgers.Id
                    },
                    new MenuItems
                    {
                        Emertimi = "Queen Deluxe XL",
                        Pershkrimi = "Chicken meatballs with tomatoes, pickled cucumbers, lettuce, soy bean sauce & Greek yogurt.",
                        Cmimi = 6.99m,
                        Foto = "greenandprotein/queendeluxeXL.png",
                        Disponueshme = true,
                        Alergjene = "Gluten,Milk,Eggs",
                        Kalori = 550,
                        CategoryId = burgers.Id
                    },
                    new MenuItems
                    {
                        Emertimi = "Leading Light",
                        Pershkrimi = "Chicken breast, cheese, eggs, tomatoes, lettuce, soy bean sauce & Greek yogurt.",
                        Cmimi = 5.99m,
                        Foto = "greenandprotein/leadingLight.png",
                        Disponueshme = true,
                        Alergjene = "Eggs, Milk, Soy, Gluten (bun)",
                        Kalori = 450,
                        CategoryId = burgers.Id
                    },
                    new MenuItems
                    {
                        Emertimi = "Gorgeous Gang",
                        Pershkrimi = "Vegan lentil pattie with tomatoes, cucumbers, red onions, lettuce, soy bean sauce & beet & pb sauce.",
                        Cmimi = 6.99m,
                        Foto = "greenandprotein/gorgeousGang.png",
                        Disponueshme = true,
                        Alergjene = "Soy, Gluten (bun), Peanuts",
                        Kalori = 500,
                        CategoryId = burgers.Id
                    },




                };
                context.MenuItems.AddRange(burgersItems);
                context.SaveChanges();

                var bowlsItems = new List<MenuItems>
                {
                    new MenuItems
                    {
                        Emertimi = "Protein Beast",
                        Pershkrimi = "Chicken breast, avocado, corn, pickled radishes, boiled eggs, soybean sauce, sesame seeds, lemon & parsley.",
                        Cmimi = 5.99m,
                        Foto = "greenandprotein/proteinbeast.png",
                        Disponueshme = true,
                        Alergjene = "Eggs, Soy, Sesame",
                        Kalori = 700,
                        CategoryId = bowls.Id
                     },
                    new MenuItems
                    {
                        Emertimi = "Greenies Grains",
                        Pershkrimi = "Spinach, lentils, quinoa, chickpeas, corn, carrots, pumpkin seeds, sesame seeds, pomegranate arils, parsley & strong mustard.",
                        Cmimi = 6.49m,
                        Foto = "greenandprotein/greeniesgrains.png",
                        Disponueshme = true,
                        Alergjene = "Sesame, Mustard",
                        Kalori = 600,
                        CategoryId = bowls.Id
                     },
                    new MenuItems
                    {
                        Emertimi = "Sweet & Tasty",
                        Pershkrimi = "Marinated chicken breast, sweet potato, boiled eggs, soybean sauce, peas, pickled red onions, pomegranate arils, sesame seeds & strong mustard.",
                        Cmimi = 6.99m,
                        Foto = "greenandprotein/sweetTasty.png",
                        Disponueshme = true,
                        Alergjene = "Eggs, Soy, Sesame, Mustard",
                        Kalori = 550,
                        CategoryId = bowls.Id
                     },
                    new MenuItems
                    {
                        Emertimi = "Tofu & Mushroom fix",
                        Pershkrimi = "Marinated tofu, mushrooms, sweet potato, chickpeas, mashed broccoli, spinach & sesame seeds.",
                        Cmimi = 8.99m,
                        Foto = "greenandprotein/tofuMushroomFix.png",
                        Disponueshme = true,
                        Alergjene = "Soy, Sesame",
                        Kalori = 750,
                        CategoryId = bowls.Id
                     },
                    new MenuItems
                    {
                        Emertimi = "BodyBuilder +",
                        Pershkrimi = "Chicken breast with fresh vegetables, grains and protein-rich toppings.",
                        Cmimi = 5.99m,
                        Foto = "greenandprotein/bodybuilder.png",
                        Disponueshme = true,
                        Alergjene = "Soy, Sesame, Mustard",
                        Kalori = 700,
                        CategoryId = bowls.Id
                     },
                    new MenuItems
                    {
                        Emertimi = "Wabi-Sabi",
                        Pershkrimi = "Balanced bowl with fresh veggies, grains and Asian-inspired flavors.",
                        Cmimi = 6.99m,
                        Foto = "greenandprotein/wabisabi.png",
                        Disponueshme = true,
                        Alergjene = "Soy, Sesame",
                        Kalori = 300,
                        CategoryId = bowls.Id
                     },
                    new MenuItems
                    {
                        Emertimi = "Chicken's Joy",
                        Pershkrimi = "Chicken-based bowl with fresh vegetables and flavorful sauces.",
                        Cmimi = 4.99m,
                        Foto = "greenandprotein/chikensjoy.png",
                        Disponueshme = true,
                        Alergjene = "Soy,Gluten",
                        Kalori = 400,
                        CategoryId = bowls.Id
                     }
                 };
                context.MenuItems.AddRange(bowlsItems);
                context.SaveChanges();

                var saladsItems = new List<MenuItems>
                {
                    new MenuItems
                    {
                        Emertimi = "Full-Veg-Protein",
                        Pershkrimi = "Vegan patties served with tricolor quinoa, hummus, boiled eggs, peas, pickled radish, and sesame seeds, finished with a lemon & parsley dressing.",
                        Cmimi = 6.49m,
                        Foto = "greenandprotein/fullvegprotein.png",
                        Disponueshme = true,
                        Alergjene = "Eggs, Sesame",
                        Kalori = 600,
                        CategoryId = salads.Id
                    },
                    new MenuItems
                    {
                        Emertimi = "Healing Power ",
                        Pershkrimi = "Brown rice with avocado, edamame, lentils, hummus, carrots, pickled radish, and sunflower seeds, topped with a strong mustard sauce.",
                        Cmimi = 4.49m,
                        Foto = "greenandprotein/healingpower.png",
                        Disponueshme = true,
                        Alergjene = "Soy, Sesame, Mustard",
                        Kalori = 250,
                        CategoryId = salads.Id
                    },
                    new MenuItems
                    {
                        Emertimi = "Positive Calorie",
                        Pershkrimi = "Brown rice with chicken breast, boiled eggs, pickled red onions, corn, peas, beetroot, and sesame seeds, finished with strong mustard dressing.",
                        Cmimi = 5.99m,
                        Foto = "greenandprotein/positiveCalorie.png",
                        Disponueshme = true,
                        Alergjene = " Eggs, Sesame, Mustard",
                        Kalori = 350,
                        CategoryId = salads.Id
                    },
                    new MenuItems
                    {
                        Emertimi = "Heart & Soul",
                        Pershkrimi = "Tuna mix with carrots, cucumbers, corn, black beans, rice, whole‑wheat pasta, pickled onions, peppers, sesame seeds, finished with spicy tomato and lemon vinaigrette.",
                        Cmimi = 5.49m,
                        Foto = "greenandprotein/heartsoul.png",
                        Disponueshme = true,
                        Alergjene = "Fish, Gluten (pasta), Sesame, Peanuts.",
                        Kalori = 300,
                        CategoryId = salads.Id
                    },
                    new MenuItems
                    {
                        Emertimi = "Ketogen Boost",
                        Pershkrimi = "Chicken breast with mushrooms, cucumbers, keto cauliflower rice, eggs, red peppers, pickled red onions, white cheese, sesame seeds, finished with lemon & parsley dressing.",
                        Cmimi = 5.99m,
                        Foto = "greenandprotein/ketogenBoost.png",
                        Disponueshme = true,
                        Alergjene = "Eggs, Milk, Sesame",
                        Kalori = 400,
                        CategoryId = salads.Id
                     },
                    new MenuItems
                    {
                        Emertimi = "Tunalicious",
                        Pershkrimi = "Tuna mix with carrots, cucumbers, corn, black beans, cabbage slaw, tomatoes, whole‑wheat croutons, finished with sharp vinaigrette.",
                        Cmimi = 6.99m,
                        Foto = "greenandprotein/tunalicious.png",
                        Disponueshme = true,
                        Alergjene = "Fish, Gluten (croutons), Soy (sauce)",
                        Kalori = 500,
                        CategoryId = salads.Id
                     },
                };
                context.MenuItems.AddRange(saladsItems);
                context.SaveChanges();

                var wrapsItems = new List<MenuItems>
                {
                    new MenuItems
                    {
                        Emertimi = "Chicken Delight",
                        Pershkrimi = "Grilled chicken with cheese, corn, tomatoes, lettuce, finished with spicy tomato sauce.",
                        Cmimi = 5.99m,
                        Foto = "greenandprotein/chickendelight.png",
                        Disponueshme = true,
                        Alergjene = "Gluten (tortilla), Milk (parmesan), Eggs (dressing)",
                        Kalori = 450,
                        CategoryId = wraps.Id
                    },
                    new MenuItems
                    {
                        Emertimi = "Cool Egg",
                        Pershkrimi = "Boiled eggs with soybean sauce, cheese, dill, tomatoes, and lettuce.",
                        Cmimi = 4.99m,
                        Foto = "greenandprotein/coolEgg.png",
                        Disponueshme = true,
                        Alergjene = "Eggs, Milk, Soy, Gluten (tortilla)",
                        Kalori = 400,
                        CategoryId = wraps.Id
                    },
                    new MenuItems
                    {
                        Emertimi = "Chicken Charm",
                        Pershkrimi = "Brown rice, chicken breast, black beans, carrots, corn, lettuce, with Greek yogurt & herbs.",
                        Cmimi = 6.49m,
                        Foto = "greenandprotein/chickenCharm.png",
                        Disponueshme = true,
                        Alergjene = "Milk (yogurt), Gluten (tortilla)",
                        Kalori = 500,
                        CategoryId = wraps.Id
                    },
                    new MenuItems
                    {
                        Emertimi = "Avocado & Egg",
                        Pershkrimi = "Avocado, boiled eggs, tomatoes, lettuce, with soybean sauce.",
                        Cmimi = 5.49m,
                        Foto = "greenandprotein/avocadoEgg.png",
                        Disponueshme = true,
                        Alergjene = "Eggs, Soy, Gluten (tortilla)",
                        Kalori = 450,
                        CategoryId = wraps.Id
                    },
                   new MenuItems
                    {
                        Emertimi = "Gut Power",
                        Pershkrimi = "Vegan lentil pattie with carrots, broccoli, red onions, cucumbers, lettuce, and soybean sauce.",
                        Cmimi = 6.99m,
                        Foto = "greenandprotein/gutPower.png",
                        Disponueshme = true,
                        Alergjene = "Soy, Gluten (tortilla), Legumes",
                        Kalori = 500,
                        CategoryId = wraps.Id
                     },
                    new MenuItems
                    {
                        Emertimi = "Tuna Turner",
                        Pershkrimi = "Tuna mix with corn, cucumbers, black beans, peppers, red onions, lettuce, finished with spicy tomato sauce.",
                        Cmimi = 5.99m,
                        Foto = "greenandprotein/tunaTurner.png",
                        Disponueshme = true,
                        Alergjene = "Fish, Gluten (tortilla), Soy",
                        Kalori = 450,
                        CategoryId = wraps.Id
                     }
            };
                context.MenuItems.AddRange(wrapsItems);
                context.SaveChanges();


                var lightmealsItems = new List<MenuItems>
{
    new MenuItems
    {
        Emertimi = "Pink Vibes",
        Pershkrimi = "Strawberries and bananas with house-made granola, dried fruits, seeds, coconut chips, almonds.",
        Cmimi = 5.99m,
        Foto = "greenandprotein/pinkVibes.png",
        Disponueshme = true,
        Alergjene = "Nuts (almonds), Gluten (granola), Honey",
        Kalori = 420,
        CategoryId = lightmeals.Id
    },
    new MenuItems
    {
        Emertimi = "Choco Lover",
        Pershkrimi = "Banana, cacao, granola, coconut chips, strawberries, peanut butter, peanuts, dark chocolate.",
        Cmimi = 6.49m,
        Foto = "greenandprotein/chocoLover.png",
        Disponueshme = true,
        Alergjene = "Peanuts, Nuts, Gluten (granola), Milk (chocolate)",
        Kalori = 480,
        CategoryId = lightmeals.Id
    },
    new MenuItems
    {
        Emertimi = "Berry Good",
        Pershkrimi = "Cashew mylk with date sweetener, chia seeds, bananas, strawberries, coconut chips.",
        Cmimi = 5.99m,
        Foto = "greenandprotein/berryGood.png",
        Disponueshme = true,
        Alergjene = "Nuts (cashew), Coconut",
        Kalori = 400,
        CategoryId = lightmeals.Id
    },
    new MenuItems
    {
        Emertimi = "PBJ Power",
        Pershkrimi = "Cashew mylk with date sweetener, chia seeds, bananas, peanut butter, dark chocolate, peanuts.",
        Cmimi = 6.49m,
        Foto = "greenandprotein/pbjPower.png",
        Disponueshme = true,
        Alergjene = "Peanuts, Nuts (cashew), Soy (chocolate)",
        Kalori = 450,
        CategoryId = lightmeals.Id
    },
    new MenuItems
    {
        Emertimi = "Simple Sunshine",
        Pershkrimi = "Oats with soy milk, date sweetener, banana, goji berries, seedless sultanas.",
        Cmimi = 5.49m,
        Foto = "greenandprotein/simpleSunshine.png",
        Disponueshme = true,
        Alergjene = "Soy, Gluten (oats)",
        Kalori = 430,
        CategoryId = lightmeals.Id
    },
    new MenuItems
    {
        Emertimi = "Chocolate Beauty",
        Pershkrimi = "Oats with soy milk, date sweetener, banana, vegan dark chocolate, coconut chips.",
        Cmimi = 6.49m,
        Foto = "greenandprotein/chocolateBeauty.png",
        Disponueshme = true,
        Alergjene = "Soy, Gluten (oats), Coconut",
        Kalori = 460,
        CategoryId = lightmeals.Id
    }
};

                context.MenuItems.AddRange(lightmealsItems);
                context.SaveChanges();

                var juicessmoothiesItems = new List<MenuItems>
{
    new MenuItems
    {
        Emertimi = "Antioxidant Hero",
        Pershkrimi = "Spinach, pineapple, green apple.",
        Cmimi = 4.99m,
        Foto = "greenandprotein/antioxidantHero.png",
        Disponueshme = true,
        Alergjene = "None",
        Kalori = 120,
        CategoryId = juicessmoothies.Id
    },
    new MenuItems
    {
        Emertimi = "Balanced Chakras",
        Pershkrimi = "Pineapple, beetroot, carrot, green apple, orange, ginger, lemon.",
        Cmimi = 5.49m,
        Foto = "greenandprotein/balancedChakras.png",
        Disponueshme = true,
        Alergjene = "None",
        Kalori = 140,
        CategoryId = juicessmoothies.Id
    },
    new MenuItems
    {
        Emertimi = "Ginger Lover",
        Pershkrimi = "Green apple, ginger, carrot, lemon.",
        Cmimi = 4.99m,
        Foto = "greenandprotein/gingerLover.png",
        Disponueshme = true,
        Alergjene = "None",
        Kalori = 110,
        CategoryId = juicessmoothies.Id
    },
    new MenuItems
    {
        Emertimi = "Happy Oranges",
        Pershkrimi = "Fresh oranges.",
        Cmimi = 3.99m,
        Foto = "greenandprotein/happyOranges.png",
        Disponueshme = true,
        Alergjene = "None",
        Kalori = 100,
        CategoryId = juicessmoothies.Id
    },
    new MenuItems
    {
        Emertimi = "Feel Good",
        Pershkrimi = "Beetroot, carrots, lemon, green apple.",
        Cmimi = 4.99m,
        Foto = "greenandprotein/feelGood.png",
        Disponueshme = true,
        Alergjene = "None",
        Kalori = 130,
        CategoryId = juicessmoothies.Id
    },
    new MenuItems
    {
        Emertimi = "O&C",
        Pershkrimi = "Oranges and carrots.",
        Cmimi = 3.99m,
        Foto = "greenandprotein/oAndC.png",
        Disponueshme = true,
        Alergjene = "None",
        Kalori = 110,
        CategoryId = juicessmoothies.Id
    },
    new MenuItems
    {
        Emertimi = "Pink Panther",
        Pershkrimi = "Orange juice, bananas, strawberries, chia seeds.",
        Cmimi = 5.49m,
        Foto = "greenandprotein/pinkPanther.png",
        Disponueshme = true,
        Alergjene = "Chia seeds",
        Kalori = 180,
        CategoryId = juicessmoothies.Id
    },
    new MenuItems
    {
        Emertimi = "Fiber Attack",
        Pershkrimi = "Orange juice, avocado, bananas, strawberries, spinach, dates.",
        Cmimi = 5.99m,
        Foto = "greenandprotein/fiberAttack.png",
        Disponueshme = true,
        Alergjene = "None",
        Kalori = 200,
        CategoryId = juicessmoothies.Id
    },
    new MenuItems
    {
        Emertimi = "Tasty Peanut Butter",
        Pershkrimi = "Milk, banana, peanut butter, dates.",
        Cmimi = 5.99m,
        Foto = "greenandprotein/tastyPeanutButter.png",
        Disponueshme = true,
        Alergjene = "Milk, Peanuts",
        Kalori = 250,
        CategoryId = juicessmoothies.Id
    },
    new MenuItems
    {
        Emertimi = "Chocolate Cravings",
        Pershkrimi = "Milk, banana, cacao, dates.",
        Cmimi = 5.99m,
        Foto = "greenandprotein/chocolateCravings.png",
        Disponueshme = true,
        Alergjene = "Milk",
        Kalori = 240,
        CategoryId = juicessmoothies.Id
    },
    new MenuItems
    {
        Emertimi = "Vitamin Splash",
        Pershkrimi = "Orange juice, carrot, bananas, pineapple.",
        Cmimi = 5.49m,
        Foto = "greenandprotein/vitaminSplash.png",
        Disponueshme = true,
        Alergjene = "None",
        Kalori = 170,
        CategoryId = juicessmoothies.Id
    },
    new MenuItems
    {
        Emertimi = "Whey Protein & PB",
        Pershkrimi = "Milk, water, bananas, whey protein, peanut butter, dates.",
        Cmimi = 6.49m,
        Foto = "greenandprotein/wheyProteinPB.png",
        Disponueshme = true,
        Alergjene = "Milk, Peanuts",
        Kalori = 300,
        CategoryId = juicessmoothies.Id
    }
};

                context.MenuItems.AddRange(juicessmoothiesItems);
                context.SaveChanges();




            }

            var categoriesMyShawarma = new List<MenuCategory>();
            var myshawarma = context.Restaurants.FirstOrDefault(r => r.Emertimi == "My Shawarma");

            if (myshawarma != null)
            {

                var menuchicken = new MenuCategory
                {
                    Emertimi = "Menu Chicken",
                    Pershkrimi = "Juicy, marinated chicken slow-cooked to perfection, wrapped or served with fresh vegetables and flavorful sauces for an authentic shawarma experience.",
                    Renditja = 1,
                    RestaurantId = myshawarma.Id
                };
                var sauces = new MenuCategory
                {
                    Emertimi = "Sauces",
                    Pershkrimi = "A selection of rich and flavorful sauces, from creamy garlic to spicy and tangy blends, crafted to enhance every bite of your shawarma.",
                    Renditja = 2,
                    RestaurantId = myshawarma.Id
                };
                var bread = new MenuCategory
                {
                    Emertimi = "Bread & Wrap Doner Chicken",
                    Pershkrimi = "Tender doner chicken served in fresh bread or a soft wrap, topped with crisp vegetables and flavorful sauces",
                    Renditja = 3,
                    RestaurantId = myshawarma.Id
                };
                var plates = new MenuCategory
                {
                    Emertimi = "Shawarma Plates Chicken",
                    Pershkrimi = "Juicy chicken shawarma served on a plate with fresh vegetables, rice or fries, and a selection of flavorful sauces.",
                    Renditja = 4,
                    RestaurantId = myshawarma.Id
                };
                var extras = new MenuCategory
                {
                    Emertimi = "Shawarma Plates Chicken",
                    Pershkrimi = "Add-ons to customize your meal, including extra toppings, sauces,and sides for even more flavor.",
                    Renditja = 5,
                    RestaurantId = myshawarma.Id
                };
                var drinks = new MenuCategory
                {
                    Emertimi = "Drinks",
                    Pershkrimi = "Drinks",
                    Renditja = 6,
                    RestaurantId = myshawarma.Id
                };


                categoriesMyShawarma.AddRange(new[] { menuchicken, sauces, bread, plates, extras, drinks });
                context.MenuCategories.AddRange(categoriesMyShawarma);
                context.SaveChanges();

                var menuchickenItems = new List<MenuItems> {
                new MenuItems
                   {
                    Emertimi = "M Döner Menu Chicken",
                    Pershkrimi = "A medium chicken döner menu served with fresh bread or wrap, crispy fries, and a refreshing drink.",
                    Cmimi = 3.89m,
                    Foto = "myshawarma/download1.png",
                    Disponueshme = true,
                    Alergjene = "Gluten,Milk,Eggs",
                    Kalori = 800,
                    CategoryId = menuchicken.Id
                },
                new MenuItems
                   {
                    Emertimi = "L Döner Menu Chicken",
                    Pershkrimi = "A large chicken döner menu served with fresh bread or wrap, crispy fries, and a refreshing drink.",
                    Cmimi = 4.49m,
                    Foto = "myshawarma/download2.png",
                    Disponueshme = true,
                    Alergjene = "Gluten,Milk,Eggs",
                    Kalori = 800,
                    CategoryId = menuchicken.Id
                },

                new MenuItems
                   {
                    Emertimi = "Eggy Döner Menu Chicken",
                    Pershkrimi = "A satisfying chicken döner menu with added egg, served in fresh bread or wrap, with crispy fries and a refreshing drink.",
                    Cmimi = 4.89m,
                    Foto = "myshawarma/download3.png",
                    Disponueshme = true,
                    Alergjene = "Gluten,Milk,Eggs",
                    Kalori = 1100,
                    CategoryId = menuchicken.Id
                },
                new MenuItems
                   {
                    Emertimi = "Pitalkë Döner Menu Chicken",
                    Pershkrimi = "Delicious chicken döner served in traditional pitalkë bread, with fresh vegetables, flavorful sauces, crispy fries, and a refreshing drink",
                    Cmimi = 4.69m,
                    Foto = "myshawarma/download4.png",
                    Disponueshme = true,
                    Alergjene = "Gluten,Milk,Eggs",
                    Kalori = 1100,
                    CategoryId = menuchicken.Id
                },
                new MenuItems
                   {
                    Emertimi = "Gyros Döner Menu Chicken",
                    Pershkrimi = "Tender chicken gyros served with fresh bread or wrap, accompanied by crispy fries, fresh vegetables, and flavorful sauces.",
                    Cmimi = 5.19m,
                    Foto = "myshawarma/gyros1.png",
                    Disponueshme = true,
                    Alergjene = "Gluten,Milk,Eggs",
                    Kalori = 1100,
                    CategoryId = menuchicken.Id
                },
                new MenuItems
                   {
                    Emertimi = "Burger Döner Menu Chicken",
                    Pershkrimi = "A tasty chicken döner served as a burger, paired with crispy fries and a refreshing drink for a complete meal.",
                    Cmimi = 4.19m,
                    Foto = "myshawarma/burger11.png",
                    Disponueshme = true,
                    Alergjene = "Gluten,Milk,Eggs",
                    Kalori = 1150,
                    CategoryId = menuchicken.Id
                }

                };
                context.MenuItems.AddRange(menuchickenItems);
                context.SaveChanges();

                var saucesItems = new List<MenuItems>
                {
                    new MenuItems
                    {
                        Emertimi = "Home Sauce",
                        Pershkrimi = "Our signature house-made sauce, creamy and flavorful, perfect for dipping or adding to any dish.",
                        Cmimi = 0.50m,
                        Foto = "myshawarma/sauce1.png",
                        Disponueshme = true,
                        Alergjene = "Milk, Eggs,Soy",
                        Kalori = 55,
                        CategoryId = sauces.Id
                     },
                    new MenuItems
                    {
                        Emertimi = "Garlic Sauce",
                        Pershkrimi = "A creamy and flavorful garlic sauce, perfect for dipping or drizzling over your meal.",
                        Cmimi = 0.50m,
                        Foto = "myshawarma/garlic.png",
                        Disponueshme = true,
                        Alergjene = "Milk,Eggs",
                        Kalori = 60,
                        CategoryId = sauces.Id
                     },
                    new MenuItems
                    {
                        Emertimi = "Spicy Sauce",
                        Pershkrimi = "A bold and tangy spicy sauce, perfect for adding a kick to your meals.",
                        Cmimi = 0.50m,
                        Foto = "myshawarma/spicy.png",
                        Disponueshme = true,
                        Alergjene = "Milk,Eggs",
                        Kalori = 40,
                        CategoryId = sauces.Id
                     },
                    new MenuItems
                    {
                        Emertimi = "Thunderlight",
                        Pershkrimi = "A zesty and bold sauce with a perfect balance of heat and flavor, ideal for adding an extra kick to your meals.",
                        Cmimi = 0.50m,
                        Foto = "myshawarma/thunder.png",
                        Disponueshme = true,
                        Alergjene = "Milk,Eggs",
                        Kalori = 50,
                        CategoryId = sauces.Id
                     }
                };
                context.MenuItems.AddRange(saucesItems);
                context.SaveChanges();

                var breadItems = new List<MenuItems>() {
                new MenuItems{
                    Emertimi = "Bread Döner Chicken",
                    Pershkrimi = "Tender chicken döner served in fresh bread, topped with crisp vegetables and your choice of flavorful sauces.",
                    Cmimi = 2.80m,
                    Foto = "myshawarma/doner1.png",
                    Disponueshme = true,
                    Alergjene = "Gluten,Milk,Eggs",
                    Kalori = 0,
                    CategoryId = bread.Id
                 },
                new MenuItems{
                    Emertimi = "Beggy Döner Chicken",
                    Pershkrimi = "A hearty chicken döner served with a fried egg, fresh vegetables, and your choice of sauces, all wrapped in soft bread or a wrap.",
                    Cmimi = 3.20m,
                    Foto = "myshawarma/doner2.png",
                    Disponueshme = true,
                    Alergjene = "Gluten,Milk,Eggs",
                    Kalori = 600,
                    CategoryId = bread.Id
                 },
                new MenuItems{
                    Emertimi = "Don'Fries Chicken",
                    Pershkrimi = "Crispy chicken strips served with golden fries and your choice of dipping sauces, a perfect combo for a quick meal.",
                    Cmimi = 3.20m,
                    Foto = "myshawarma/doner3.png",
                    Disponueshme = true,
                    Alergjene = "Gluten,Milk,Eggs",
                    Kalori = 700,
                    CategoryId = bread.Id
                 }

                };

                context.MenuItems.AddRange(breadItems);
                context.SaveChanges();
                var platesItems = new List<MenuItems>()
                {
                    new MenuItems{
                        Emertimi = "Full House Chicken",
                        Pershkrimi = "A generous chicken meal featuring tender chicken pieces, fries, fresh vegetables, and a selection of flavorful sauces",
                        Cmimi = 3.50m,
                        Foto = "myshawarma/plate11.png",
                        Disponueshme = true,
                        Alergjene = "Gluten,Milk,Eggs",
                        Kalori = 900,
                        CategoryId = plates.Id
                     },
                    new MenuItems{
                        Emertimi = "Shawarma Platter Chicken",
                        Pershkrimi = "A hearty chicken shawarma platter served with rice or fries, fresh vegetables, and a choice of flavorful sauces.",
                        Cmimi = 4.00m,
                        Foto = "myshawarma/plate22.png",
                        Disponueshme = true,
                        Alergjene = "Gluten,Milk,Eggs",
                        Kalori = 900,
                        CategoryId = plates.Id
                     },
                    new MenuItems{
                        Emertimi = "Chef Döner Chicken",
                        Pershkrimi = "A premium chicken döner served with fresh vegetables, golden fries, and a choice of signature sauces for a complete meal.",
                        Cmimi = 4.00m,
                        Foto = "myshawarma/plate33.png",
                        Disponueshme = true,
                        Alergjene = "Gluten,Milk,Eggs",
                        Kalori = 900,
                        CategoryId = plates.Id
                     },
                    new MenuItems{
                        Emertimi = "Box Döner Chicken",
                        Pershkrimi = "A convenient and filling chicken döner served in a box with fresh vegetables, golden fries, and your choice of sauces.",
                        Cmimi = 3.20m,
                        Foto = "myshawarma/plate44.png",
                        Disponueshme = true,
                        Alergjene = "Gluten,Milk,Eggs",
                        Kalori = 850,
                        CategoryId = plates.Id
                     }
                };
                context.MenuItems.AddRange(platesItems);
                context.SaveChanges();



                var extrasItems = new List<MenuItems>()
                {
                    new MenuItems{
                        Emertimi = "5 pcs Mozarella Sticks",
                        Pershkrimi = "Five golden, crispy mozzarella sticks, perfectly breaded and served with a side of dipping sauce.",
                        Cmimi = 2.30m,
                        Foto = "myshawarma/55.png",
                        Disponueshme = true,
                        Alergjene = "Gluten,Milk,Eggs",
                        Kalori = 400,
                        CategoryId = extras.Id
                     },
                    new MenuItems{
                        Emertimi = "20 pcs Mozarella Sticks",
                        Pershkrimi = "Twenty golden, crispy mozzarella sticks, perfectly breaded and served with a side of dipping sauce—ideal for sharing.",
                        Cmimi =6.70m,
                        Foto = "myshawarma/55.png",
                        Disponueshme = true,
                        Alergjene = "Gluten,Milk,Eggs",
                        Kalori = 1600,
                        CategoryId = extras.Id
                     },
                    new MenuItems{
                        Emertimi = "Fries",
                        Pershkrimi = "Crispy golden fries, lightly salted, perfect as a side or snack.",
                        Cmimi = 1.00m,
                        Foto = "myshawarma/fries.png",
                        Disponueshme = true,
                        Alergjene = "Gluten",
                        Kalori = 300,
                        CategoryId = extras.Id
                     },
                    new MenuItems{
                        Emertimi = "10 pcs Spicy Cheesy Nuggets",
                        Pershkrimi = "Ten crispy and spicy chicken nuggets stuffed with melted cheese, perfect as a snack or side.",
                        Cmimi = 2.50m,
                        Foto = "myshawarma/nuget1.png",
                        Disponueshme = true,
                        Alergjene = "Gluten,Milk,Eggs",
                        Kalori = 600,
                        CategoryId = extras.Id
                     }
                 };

                context.MenuItems.AddRange(extrasItems);
                context.SaveChanges();

                var drinkItems = new List<MenuItems>
                {
                new MenuItems
                {
                  Emertimi = "Coca Cola",
                        Pershkrimi = "Coca Cola",
                        Cmimi = 1.50m,
                        Foto = "pastafasta/cocacola.png",
                        Disponueshme = true,
                        Alergjene = "None",
                        Kalori = 250,
                        CategoryId = drinks.Id
                },
                new MenuItems
                {
                  Emertimi = "Fanta Orange",
                        Pershkrimi = "Fanta Orange",
                        Cmimi = 1.50m,
                        Foto = "pastafasta/fantaorange.png",
                        Disponueshme = true,
                        Alergjene = "None",
                        Kalori = 250,
                        CategoryId = drinks.Id
                },
                new MenuItems
                {
                  Emertimi = "Water",
                        Pershkrimi = "Water",
                        Cmimi = 1.50m,
                        Foto = "pastafasta/water.png",
                        Disponueshme = true,
                        Alergjene = "None",
                        Kalori = 0,
                        CategoryId = drinks.Id
                }

                };

                context.MenuItems.AddRange(drinkItems);
                context.SaveChanges();



            }

            var categoriesHeavyHit = new List<MenuCategory>();
            var heavyHit = context.Restaurants.FirstOrDefault(r => r.Emertimi == "Heavy Hit");

            if (heavyHit != null)
            {
                var combo = new MenuCategory
                {
                    Emertimi = "Combo",
                    Pershkrimi = "Special meal combinations with burgers, fries, and drinks.",
                    Renditja = 1,
                    RestaurantId = heavyHit.Id
                };

                var burgers = new MenuCategory
                {
                    Emertimi = "Burgers",
                    Pershkrimi = "Juicy beef and chicken burgers with fresh toppings.",
                    Renditja = 2,
                    RestaurantId = heavyHit.Id
                };

                var fries = new MenuCategory
                {
                    Emertimi = "Fries",
                    Pershkrimi = "Crispy golden fries, perfect side for any meal.",
                    Renditja = 3,
                    RestaurantId = heavyHit.Id
                };

                var dips = new MenuCategory
                {
                    Emertimi = "Dips",
                    Pershkrimi = "Flavorful sauces and dips to complement your meal.",
                    Renditja = 4,
                    RestaurantId = heavyHit.Id
                };

                var drinks = new MenuCategory
                {
                    Emertimi = "Drinks",
                    Pershkrimi = "Refreshing beverages to complete your combo.",
                    Renditja = 5,
                    RestaurantId = heavyHit.Id
                };

                categoriesHeavyHit.AddRange(new[] { combo, burgers, fries, dips, drinks });
                context.MenuCategories.AddRange(categoriesHeavyHit);
                context.SaveChanges();


                var comboItems = new List<MenuItems>
{
    new MenuItems{
        Emertimi = "THE HEAVY HIT Combo",
        Pershkrimi = "Double beef patty, double cheese, caramelized onions, tomato, lettuce, and sauce.",
        Cmimi = 8.99m,
        Foto = "heavyhit/theHeavyhitCombo.png",
        Disponueshme = true,
        Alergjene = "Gluten,Milk",
        Kalori = 950,
        CategoryId = combo.Id
    },
    new MenuItems{
        Emertimi = "THE CLASSIC HIT Combo",
        Pershkrimi = "Double beef patty, double cheese, fresh onions, pickles, ketchup, and mustard.",
        Cmimi = 8.49m,
        Foto = "heavyhit/theClassichitCombo.png",
        Disponueshme = true,
        Alergjene = "Gluten,Milk,Mustard",
        Kalori = 900,
        CategoryId = combo.Id
    },
    new MenuItems{
        Emertimi = "THE TRUFFLE HIT Combo",
        Pershkrimi = "Double beef patty, double cheese, grated parmesan, truffle mayo, caramelized onions.",
        Cmimi = 9.49m,
        Foto = "heavyhit/theTrufflehitCombo.png",
        Disponueshme = true,
        Alergjene = "Gluten,Milk",
        Kalori = 980,
        CategoryId = combo.Id
    },
    new MenuItems{
        Emertimi = "THE PROTEIN HIT Combo",
        Pershkrimi = "Lettuce wrap with double beef patty, double cheese, caramelized onions, tomato, and thick sauce.",
        Cmimi = 9.49m,
        Foto = "heavyhit/theProteinhitCombo.png",
        Disponueshme = true,
        Alergjene = "Milk",
        Kalori = 850,
        CategoryId = combo.Id
    }
};
                context.MenuItems.AddRange(comboItems);
                context.SaveChanges();

                var burgerItems = new List<MenuItems>
{
    new MenuItems{
        Emertimi = "THE HEAVY HIT",
        Pershkrimi = "Double beef patty, double cheese, caramelized onions, tomato, lettuce, and sauce.",
        Cmimi = 6.49m,
        Foto = "heavyhit/theHeavyHitBurger.png",
        Disponueshme = true,
        Alergjene = "Gluten,Milk",
        Kalori = 750,
        CategoryId = burgers.Id
    },
    new MenuItems{
        Emertimi = "THE CLASSIC HIT",
        Pershkrimi = "Double beef patty, double cheese, fresh onions, pickles, ketchup, and mustard.",
        Cmimi = 5.99m,
        Foto = "heavyhit/theClassicHitBurger.png",
        Disponueshme = true,
        Alergjene = "Gluten,Milk,Mustard",
        Kalori = 720,
        CategoryId = burgers.Id
    },
    new MenuItems{
        Emertimi = "THE TRUFFLE HIT",
        Pershkrimi = "Double beef patty, double cheese, grated parmesan, truffle mayo, caramelized onions.",
        Cmimi = 6.99m,
        Foto = "heavyhit/theTruffleHitBurger.png",
        Disponueshme = true,
        Alergjene = "Gluten,Milk",
        Kalori = 780,
        CategoryId = burgers.Id
    },
    new MenuItems{
        Emertimi = "THE PROTEIN HIT",
        Pershkrimi = "Lettuce wrap with double beef patty, double cheese, caramelized onions, tomato, and thick sauce.",
        Cmimi = 6.99m,
        Foto = "heavyhit/theProteinHitBurger.png",
        Disponueshme = true,
        Alergjene = "Milk",
        Kalori = 680,
        CategoryId = burgers.Id
    }
};

                context.MenuItems.AddRange(burgerItems);
                context.SaveChanges();

                var friesItems = new List<MenuItems>
{
    new MenuItems{
        Emertimi = "Heavy Fries",
        Pershkrimi = "Double cheese, caramelized onions, and sauce.",
        Cmimi = 3.99m,
        Foto = "heavyhit/HeavyFries.png",
        Disponueshme = true,
        Alergjene = "Milk",
        Kalori = 500,
        CategoryId = fries.Id
    },
    new MenuItems{
        Emertimi = "Truffle Fries",
        Pershkrimi = "Truffle mayo and grated parmesan.",
        Cmimi = 4.49m,
        Foto = "heavyhit/TruffleFries.png",
        Disponueshme = true,
        Alergjene = "Milk",
        Kalori = 520,
        CategoryId = fries.Id
    },
    new MenuItems{
        Emertimi = "Cheese Fries",
        Pershkrimi = "Say cheeseeeeeee.",
        Cmimi = 2.79m,
        Foto = "heavyhit/CheeseFries.png",
        Disponueshme = true,
        Alergjene = "Milk",
        Kalori = 480,
        CategoryId = fries.Id
    },
    new MenuItems{
        Emertimi = "Plain Fries",
        Pershkrimi = "Classic crispy golden fries.",
        Cmimi = 1.99m,
        Foto = "heavyhit/PlainFries.png",
        Disponueshme = true,
        Alergjene = "",
        Kalori = 450,
        CategoryId = fries.Id
    }
};

                context.MenuItems.AddRange(friesItems);
                context.SaveChanges();

                var dipsItems = new List<MenuItems>
{
    new MenuItems{
        Emertimi = "Heavy Hit Dip",
        Pershkrimi = "Signature dip with cheese and caramelized onions.",
        Cmimi = 1.09m,
        Foto = "heavyhit/HeavyHitDip.png",
        Disponueshme = true,
        Alergjene = "Milk",
        Kalori = 150,
        CategoryId = dips.Id
    },
    new MenuItems{
        Emertimi = "Truffle Mayo Dip",
        Pershkrimi = "Rich truffle mayo with parmesan.",
        Cmimi = 1.39m,
        Foto = "heavyhit/TruffleHitDip.png",
        Disponueshme = true,
        Alergjene = "Milk,Eggs",
        Kalori = 160,
        CategoryId = dips.Id
    },
    new MenuItems{
        Emertimi = "Spicy Mayo Dip",
        Pershkrimi = "Creamy mayo with a spicy kick.",
        Cmimi = 1.09m,
        Foto = "heavyhit/SpicyMayoDip.png",
        Disponueshme = true,
        Alergjene = "Eggs",
        Kalori = 140,
        CategoryId = dips.Id
    },
    new MenuItems{
        Emertimi = "Mango Dip",
        Pershkrimi = "Sweet and tangy mango sauce.",
        Cmimi = 0.49m,
        Foto = "heavyhit/Mango.png",
        Disponueshme = true,
        Alergjene = "",
        Kalori = 120,
        CategoryId = dips.Id
    },
    new MenuItems{
        Emertimi = "Asia Sauce Dip",
        Pershkrimi = "Savory Asian-style sauce.",
        Cmimi = 0.49m,
        Foto = "heavyhit/AsiaSauceDip.png",
        Disponueshme = true,
        Alergjene = "Soy",
        Kalori = 110,
        CategoryId = dips.Id
    },
    new MenuItems{
        Emertimi = "Curry Dip",
        Pershkrimi = "Fragrant curry-flavored dip.",
        Cmimi = 0.49m,
        Foto = "heavyhit/CurryDip.png",
        Disponueshme = true,
        Alergjene = "",
        Kalori = 115,
        CategoryId = dips.Id
    },
    new MenuItems{
        Emertimi = "Mayonnaise Satchet",
        Pershkrimi = "Classic mayonnaise portion.",
        Cmimi = 0.19m,
        Foto = "heavyhit/Mayo.png",
        Disponueshme = true,
        Alergjene = "Eggs",
        Kalori = 100,
        CategoryId = dips.Id
    }
};

                context.MenuItems.AddRange(dipsItems);
                context.SaveChanges();

                var drinksItems = new List<MenuItems>
{
    new MenuItems{
        Emertimi = "Sparkling Water",
        Pershkrimi = "Refreshing carbonated water.",
        Cmimi = 0.89m,
        Foto = "heavyhit/SparklingWaterHH.png",
        Disponueshme = true,
        Alergjene = "",
        Kalori = 0,
        CategoryId = drinks.Id
    },
    new MenuItems{
        Emertimi = "Still Water",
        Pershkrimi = "Pure still water.",
        Cmimi = 0.89m,
        Foto = "heavyhit/StillWaterHH.png",
        Disponueshme = true,
        Alergjene = "",
        Kalori = 0,
        CategoryId = drinks.Id
    },
    new MenuItems{
        Emertimi = "Fructal Juice",
        Pershkrimi = "Fruit juice in carton packaging.",
        Cmimi = 1.29m,
        Foto = "heavyhit/FructalJuicesHH.png",
        Disponueshme = true,
        Alergjene = "",
        Kalori = 120,
        CategoryId = drinks.Id
    },
    new MenuItems{
        Emertimi = "Ice Tea",
        Pershkrimi = "Chilled refreshing ice tea.",
        Cmimi = 1.49m,
        Foto = "heavyhit/IceTeaHH.png",
        Disponueshme = true,
        Alergjene = "",
        Kalori = 90,
        CategoryId = drinks.Id
    },
    new MenuItems{
        Emertimi = "Fructal Glass Juice",
        Pershkrimi = "Premium fruit juice in glass bottle.",
        Cmimi = 1.49m,
        Foto = "heavyhit/Fructalglass-bottledHH.png",
        Disponueshme = true,
        Alergjene = "",
        Kalori = 130,
        CategoryId = drinks.Id
    },
    new MenuItems{
        Emertimi = "Coca-Cola Zero 330ml",
        Pershkrimi = "Classic Coke taste with zero sugar.",
        Cmimi = 1.49m,
        Foto = "heavyhit/CocaColaZeroHH.png",
        Disponueshme = true,
        Alergjene = "",
        Kalori = 1,
        CategoryId = drinks.Id
    },
    new MenuItems{
        Emertimi = "Fanta 330ml",
        Pershkrimi = "Refreshing orange soda.",
        Cmimi = 1.49m,
        Foto = "heavyhit/FantaHH.png",
        Disponueshme = true,
        Alergjene = "",
        Kalori = 140,
        CategoryId = drinks.Id
    },
    new MenuItems{
        Emertimi = "Coca-Cola 330ml",
        Pershkrimi = "Classic Coca-Cola taste.",
        Cmimi = 1.49m,
        Foto = "heavyhit/CocaColaHH.png",
        Disponueshme = true,
        Alergjene = "",
        Kalori = 150,
        CategoryId = drinks.Id
    },
    new MenuItems{
        Emertimi = "Pepsi 330ml",
        Pershkrimi = "Refreshing Pepsi cola.",
        Cmimi = 1.49m,
        Foto = "heavyhit/Pepsi.png",
        Disponueshme = true,
        Alergjene = "",
        Kalori = 150,
        CategoryId = drinks.Id
    },
    new MenuItems{
        Emertimi = "Pepsi Max 330ml",
        Pershkrimi = "Zero sugar Pepsi Max.",
        Cmimi = 1.49m,
        Foto = "heavyhit/PepsiMaxZero.png",
        Disponueshme = true,
        Alergjene = "",
        Kalori = 1,
        CategoryId = drinks.Id
    },

    new MenuItems{
        Emertimi = "IVY Lemon 330ml",
        Pershkrimi = "Refreshing lemon soda.",
        Cmimi = 1.49m,
        Foto = "heavyhit/IVYLemon.png",
        Disponueshme = true,
        Alergjene = "",
        Kalori = 135,
        CategoryId = drinks.Id
    },
    new MenuItems{
        Emertimi = "IVY Orange 330ml",
        Pershkrimi = "Citrusy orange soda.",
        Cmimi = 1.49m,
        Foto = "heavyhit/IVYOrange.png",
        Disponueshme = true,
        Alergjene = "",
        Kalori = 145,
        CategoryId = drinks.Id
    },
    new MenuItems{
        Emertimi = "7up 330ml",
        Pershkrimi = "Crisp lemon-lime soda.",
        Cmimi = 1.49m,
        Foto = "heavyhit/7up.png",
        Disponueshme = true,
        Alergjene = "",
        Kalori = 140,
        CategoryId = drinks.Id
    }
};

                context.MenuItems.AddRange(drinksItems);
                context.SaveChanges();

            }


            var categoriesPopeyes = new List<MenuCategory>();
            var popeyes = context.Restaurants.FirstOrDefault(r => r.Emertimi == "Popeyes");

            if (popeyes != null)
            {

                var sandwich = new MenuCategory
                {
                    Emertimi = "Sandwiches",
                    Pershkrimi = "Crispy fried chicken on a toasted brioche bun with pickles and signature sauce.",
                    Renditja = 1,
                    RestaurantId = popeyes.Id

                };
                var tenders = new MenuCategory
                {
                    Emertimi = "Tenders",
                    Pershkrimi = "Crispy, juicy chicken strips seasoned with bold Louisiana spices.",
                    Renditja = 2,
                    RestaurantId = popeyes.Id

                };
                var wings = new MenuCategory
                {
                    Emertimi = "Wings",
                    Pershkrimi = "Crispy, flavorful chicken wings tossed in bold Louisiana spices.",
                    Renditja = 3,
                    RestaurantId = popeyes.Id

                };
                var wraps = new MenuCategory
                {
                    Emertimi = "Wraps",
                    Pershkrimi = "Crispy chicken wrapped in a soft tortilla with fresh toppings and signature sauce.",
                    Renditja = 4,
                    RestaurantId = popeyes.Id

                };
                var kids = new MenuCategory
                {
                    Emertimi = "Kids Menu",
                    Pershkrimi = "Tasty, kid-friendly portions served with sides and a drink.",
                    Renditja = 5,
                    RestaurantId = popeyes.Id

                };
                var bucket = new MenuCategory
                {
                    Emertimi = "Bucket",
                    Pershkrimi = "Generous portions of crispy fried chicken, perfect for sharing.",
                    Renditja = 6,
                    RestaurantId = popeyes.Id

                };
                var sides = new MenuCategory
                {
                    Emertimi = "Sides",
                    Pershkrimi = "Classic, flavorful sides that perfectly complement your meal.",
                    Renditja = 7,
                    RestaurantId = popeyes.Id

                };
                var drink = new MenuCategory
                {
                    Emertimi = "Drinks",
                    Pershkrimi = "Drinks",
                    Renditja = 8,
                    RestaurantId = popeyes.Id

                };
                categoriesPopeyes.AddRange(new[] { sandwich, tenders, wings, wraps, kids, bucket, sides, drink });
                context.MenuCategories.AddRange(categoriesPopeyes);
                context.SaveChanges();


                var sandwichItems = new List<MenuItems> {

                new MenuItems
                   {
                    Emertimi = "Chicken Sandwich",
                    Pershkrimi = "Crispy fried chicken breast on a toasted brioche bun with pickles and signature sauce.",
                    Cmimi = 4.79m,
                    Foto = "popeyes/11.png",
                    Disponueshme = true,
                    Alergjene = "Gluten,Milk,Eggs",
                    Kalori = 700,
                    CategoryId = sandwich.Id
                 },
                new MenuItems
                   {
                    Emertimi = "Spicy Chicken Sandwich",
                    Pershkrimi = "Crispy spicy chicken on a toasted brioche bun with pickles and spicy mayo.",
                    Cmimi = 4.79m,
                    Foto = "popeyes/2.png",
                    Disponueshme = true,
                    Alergjene = "Gluten,Milk,Eggs",
                    Kalori = 720,
                    CategoryId = sandwich.Id
                 },
                new MenuItems
                   {
                    Emertimi = "Deluxe Chicken Sandwich",
                    Pershkrimi = "Crispy chicken with lettuce, tomato, pickles, and mayo on a toasted brioche bun.",
                    Cmimi = 5.29m,
                    Foto = "popeyes/3.png",
                    Disponueshme = true,
                    Alergjene = "Gluten,Milk,Eggs",
                    Kalori = 750,
                    CategoryId = sandwich.Id
                 },
                new MenuItems
                   {
                    Emertimi = "Colaslaw Sandwich",
                    Pershkrimi = "Crispy chicken with a crunchy, creamy coleslaw on a soft toasted bun.",
                    Cmimi = 5.29m,
                    Foto = "popeyes/4.png",
                    Disponueshme = true,
                    Alergjene = "Gluten,Milk,Eggs",
                    Kalori = 730,
                    CategoryId = sandwich.Id
                 }

                };

                context.MenuItems.AddRange(sandwichItems);
                context.SaveChanges();

                var tenderItems = new List<MenuItems> {

                    new MenuItems
                    {
                    Emertimi = "Chicken Tenders 3 Pieces",
                    Pershkrimi = "Three crispy, juicy chicken strips seasoned with Louisiana spices.",
                    Cmimi = 3.99m,
                    Foto = "popeyes/tender1.png",
                    Disponueshme = true,
                    Alergjene = "Gluten,Milk,Eggs,Soy",
                    Kalori = 360,
                    CategoryId = tenders.Id

                    },
                    new MenuItems
                    {
                    Emertimi = "Chicken Tenders 5 Pieces",
                    Pershkrimi = "Five crispy, juicy chicken strips seasoned with Louisiana spices.",
                    Cmimi = 5.99m,
                    Foto = "popeyes/tender2.png",
                    Disponueshme = true,
                    Alergjene = "Gluten,Milk,Eggs,Soy",
                    Kalori = 860,
                    CategoryId = tenders.Id

                    },
                      new MenuItems
                    {
                    Emertimi = "Chicken Tenders Spicy 3 Pieces",
                    Pershkrimi = "Three crispy, spicy, juicy chicken strips seasoned with Louisiana spices.",
                    Cmimi = 3.99m,
                    Foto = "popeyes/tender1.png",
                    Disponueshme = true,
                    Alergjene = "Gluten,Milk,Eggs,Soy",
                    Kalori = 360,
                    CategoryId = tenders.Id

                    },
                    new MenuItems
                    {
                    Emertimi = "Chicken Tenders Spicy 5 Pieces",
                    Pershkrimi = "Five crispy, spicy, juicy chicken strips seasoned with Louisiana spices.",
                    Cmimi = 5.99m,
                    Foto = "popeyes/tender2.png",
                    Disponueshme = true,
                    Alergjene = "Gluten,Milk,Eggs,Soy",
                    Kalori = 860,
                    CategoryId = tenders.Id

                    }

                };
                context.MenuItems.AddRange(tenderItems);
                context.SaveChanges();

                var wingsItems = new List<MenuItems> {


                new MenuItems
                    {
                    Emertimi = "Hot Wings 3 Pieces",
                    Pershkrimi = "Three crispy, flavorful chicken wings tossed in bold Louisiana spices.",
                    Cmimi = 2.99m,
                    Foto = "popeyes/wings.png",
                    Disponueshme = true,
                    Alergjene = "Gluten,Milk,Eggs,Soy",
                    Kalori = 500,
                    CategoryId = wings.Id
                    },
                new MenuItems
                    {
                    Emertimi = "Hot Wings 5 Pieces",
                    Pershkrimi = "Five crispy, flavorful chicken wings tossed in bold Louisiana spices.",
                    Cmimi = 4.79m,
                    Foto = "popeyes/wings2.png",
                    Disponueshme = true,
                    Alergjene = "Gluten,Milk,Eggs,Soy",
                    Kalori = 860,
                    CategoryId = wings.Id
                    },

                new MenuItems
                    {
                    Emertimi = "BBQ Glazed Wings 3 Pieces",
                    Pershkrimi = "Three crispy chicken wings tossed in sweet and smoky BBQ glaze.",
                    Cmimi = 3.49m,
                    Foto = "popeyes/bbq.png",
                    Disponueshme = true,
                    Alergjene = "Gluten,Milk,Eggs",
                    Kalori = 330,
                    CategoryId = wings.Id
                    },
                new MenuItems
                    {
                    Emertimi = "BBQ Glazed Wings 5 Pieces",
                    Pershkrimi = "Five crispy chicken wings tossed in sweet and smoky BBQ glaze.",
                    Cmimi = 5.49m,
                    Foto = "popeyes/bbq1.png",
                    Disponueshme = true,
                    Alergjene = "Gluten,Milk,Eggs",
                    Kalori = 550,
                    CategoryId = wings.Id
                    },
                new MenuItems
                    {
                    Emertimi = "Voodoo Wings 3 pcs",
                    Pershkrimi = "Three crispy chicken wings tossed in bold, spicy Voodoo sauce.",
                    Cmimi = 3.79m,
                    Foto = "popeyes/voodo1.png",
                    Disponueshme = true,
                    Alergjene = "Gluten,Milk,Eggs",
                    Kalori = 340,
                    CategoryId = wings.Id
                    },
                new MenuItems
                    {
                    Emertimi = "Voodoo Wings 5 pcs",
                    Pershkrimi = "Three crispy chicken wings tossed in bold, spicy Voodoo sauce.",
                    Cmimi = 5.49m,
                    Foto = "popeyes/voodo2.png",
                    Disponueshme = true,
                    Alergjene = "Gluten,Milk,Eggs",
                    Kalori = 560,
                    CategoryId = wings.Id
                    }

                };
                context.MenuItems.AddRange(wingsItems);
                context.SaveChanges();

                var wrapsItems = new List<MenuItems>
                {
                    new MenuItems
                    {
                     Emertimi = "Chicken Wrap",
                    Pershkrimi = "Crispy chicken, fresh veggies, and signature sauce wrapped in a soft tortilla.",
                    Cmimi = 4.49m,
                    Foto = "popeyes/wrap1.png",
                    Disponueshme = true,
                    Alergjene = "Gluten,Milk,Eggs,Mustard",
                    Kalori = 550,
                    CategoryId = wraps.Id
                    },
                    new MenuItems
                    {
                     Emertimi = "Spicy Chicken Wrap ",
                    Pershkrimi = "Crispy chicken, fresh veggies, and signature spicy sauce wrapped in a soft tortilla.",
                    Cmimi = 4.49m,
                    Foto = "popeyes/wrap2.png",
                    Disponueshme = true,
                    Alergjene = "Gluten,Milk,Eggs,Mustard",
                    Kalori = 550,
                    CategoryId = wraps.Id
                    }
                };
                context.MenuItems.AddRange(wrapsItems);
                context.SaveChanges();

                var kidsItems = new List<MenuItems>
                {
                    new MenuItems
                    {
                        Emertimi = "Kids Menu 1",
                        Pershkrimi = "Kid-sized crispy chicken with a side and a drink, perfect for little appetites.",
                        Cmimi = 3.99m,
                        Foto = "popeyes/kids.png",
                        Disponueshme = true,
                        Alergjene = "Gluten,Milk,Eggs,Soy",
                        Kalori = 500,
                        CategoryId = kids.Id
                    },
                    new MenuItems
                    {
                        Emertimi = "Kids Menu Mini Wrap",
                        Pershkrimi = "A smaller crispy chicken wrap with fresh veggies and mild sauce, perfect for kids.",
                        Cmimi = 3.99m,
                        Foto = "popeyes/kids2.png",
                        Disponueshme = true,
                        Alergjene = "Gluten,Milk,Eggs,Soy",
                        Kalori = 500,
                        CategoryId = kids.Id
                    }
                };

                context.MenuItems.AddRange(kidsItems);
                context.SaveChanges();

                var bucketItems = new List<MenuItems>
                {
                    new MenuItems
                    {
                        Emertimi = "8 Piece Chicken Bucket",
                        Pershkrimi = "A generous bucket of 8 pieces of crispy fried chicken, perfect for sharing.",
                        Cmimi = 12.99m,
                        Foto = "popeyes/bucket1.png",
                        Disponueshme = true,
                        Alergjene = "Gluten,Milk,Eggs,Soy",
                        Kalori = 1600,
                        CategoryId = bucket.Id
                    },
                    new MenuItems
                    {
                        Emertimi = "12 Piece Chicken Bucket",
                        Pershkrimi = "A large bucket of 12 pieces of crispy fried chicken, ideal for family meals.",
                        Cmimi = 21.99m,
                        Foto = "popeyes/bucket2.png",
                        Disponueshme = true,
                        Alergjene = "Gluten,Milk,Eggs,Soy",
                        Kalori = 2400,
                        CategoryId = bucket.Id
                     },
                    new MenuItems
                    {
                        Emertimi = "Hot Bucket",
                        Pershkrimi = "A generous portion of crispy fried chicken, perfect for sharing.",
                        Cmimi = 12.99m,
                        Foto = "popeyes/bucket3.png",
                        Disponueshme = true,
                        Alergjene = "Gluten,Milk,Eggs,Soy",
                        Kalori = 1200,
                        CategoryId = bucket.Id
                     }
                 };
                context.MenuItems.AddRange(bucketItems);
                context.SaveChanges();

                var sidesItems = new List<MenuItems>
                {
                    new MenuItems
                    {
                        Emertimi = "Fries Small",
                        Pershkrimi = "Crispy, golden fries seasoned to perfection.",
                        Cmimi = 1.29m,
                        Foto = "popeyes/fries.png",
                        Disponueshme = true,
                        Alergjene = "Gluten",
                        Kalori = 150,
                        CategoryId = sides.Id
                    },
                    new MenuItems
                    {
                        Emertimi = "Fries Large",
                        Pershkrimi = "Crispy, golden fries seasoned to perfection.",
                        Cmimi = 1.99m,
                        Foto = "popeyes/fries.png",
                        Disponueshme = true,
                        Alergjene = "None",
                        Kalori =250 ,
                        CategoryId = sides.Id
                     },
                    new MenuItems
                    {
                        Emertimi = "Coleslaw",
                        Pershkrimi = "Crunchy coleslaw made with shredded cabbage and a tangy dressing.",
                        Cmimi = 1.79m,
                        Foto = "popeyes/side.png",
                        Disponueshme = true,
                        Alergjene = "Milk,Eggs",
                        Kalori = 150,
                        CategoryId = sides.Id
                     },
                    new MenuItems
                    {
                        Emertimi = "Onion Rings 5 Pieces",
                        Pershkrimi = "Five golden, crispy onion rings with a crunchy coating.",
                        Cmimi = 1.79m,
                        Foto = "popeyes/side1.png",
                        Disponueshme = true,
                        Alergjene = "Milk,Eggs,Gluten",
                        Kalori = 250,
                        CategoryId = sides.Id
                     }
                 };
                context.MenuItems.AddRange(sidesItems);
                context.SaveChanges();

                var drinkItems = new List<MenuItems>
                {
                new MenuItems
                {
                        Emertimi = "Coca Cola",
                        Pershkrimi = "Coca Cola",
                        Cmimi = 1.50m,
                        Foto = "pastafasta/CocaCola.png",
                        Disponueshme = true,
                        Alergjene = "None",
                        Kalori = 250,
                        CategoryId = drink.Id

                 },
                new MenuItems
                {
                        Emertimi = "Fanta Orange",
                        Pershkrimi = "Fanta Orange",
                        Cmimi = 1.50m,
                        Foto = "pastafasta/fantaorange.png",
                        Disponueshme = true,
                        Alergjene = "None",
                        Kalori = 250,
                        CategoryId = drink.Id

                 },
                new MenuItems
                {
                        Emertimi = "Water",
                        Pershkrimi = "Water",
                        Cmimi = 1.00m,
                        Foto = "pastafasta/water.png",
                        Disponueshme = true,
                        Alergjene = "None",
                        Kalori = 0,
                        CategoryId = drink.Id

                 },
                new MenuItems
                {
                        Emertimi = "Sprite",
                        Pershkrimi = "Sprite",
                        Cmimi = 1.50m,
                        Foto = "pastafasta/Sprite.png",
                        Disponueshme = true,
                        Alergjene = "None",
                        Kalori = 250,
                        CategoryId = drink.Id

                 }

                };
                context.MenuItems.AddRange(drinkItems);
                context.SaveChanges();
            }

            var agusholli = context.Restaurants.FirstOrDefault(r => r.Emertimi == "Agusholli");

            if (agusholli != null)
            {

                var sweet = context.MenuCategories.FirstOrDefault(c => c.Emertimi == "Sweet" && c.RestaurantId == agusholli.Id);

                if (sweet == null)
                {
                    sweet = new MenuCategory
                    {
                        Emertimi = "Sweet",
                        Pershkrimi = "Indulge in our irresistible sweet treats, from decadent cakes to delightful pastries.",
                        Renditja = 1,
                        RestaurantId = agusholli.Id
                    };

                    context.MenuCategories.Add(sweet);
                    context.SaveChanges();
                }



                var sweetItemsExist = context.MenuItems.Any(i => i.CategoryId == sweet.Id);


                if (!sweetItemsExist)
                {
                    var sweetItems = new List<MenuItems>
                    {

                    new MenuItems
                    {
                        Emertimi = "Cremisimo",
                        Pershkrimi = "A delicate soft dessert with layers of puff pastry, crunchy nuts, and creamy vanilla pudding.",
                        Cmimi = 2.49m,
                        Foto = "agusholli/1.png",
                        Disponueshme = true,
                        Alergjene = "Gluten,Milk,Eggs,Nuts",
                        Kalori = 350,
                        CategoryId = sweet.Id
                     },
                    new MenuItems
                    {
                        Emertimi = "Red Velvet",
                        Pershkrimi = "Moist red velvet cake layered with smooth cream cheese frosting.",
                        Cmimi = 2.49m,
                        Foto = "agusholli/2.png",
                        Disponueshme = true,
                        Alergjene = "Gluten,Milk,Eggs",
                        Kalori = 400,
                        CategoryId = sweet.Id
                     },
                    new MenuItems
                    {
                        Emertimi = "Kadaif",
                        Pershkrimi = "Golden, crispy shredded pastry filled with nuts and drizzled with sweet syrup.",
                        Cmimi = 2.00m,
                        Foto = "agusholli/3.png",
                        Disponueshme = true,
                        Alergjene = "Gluten,Milk,Eggs,Nuts",
                        Kalori = 320,
                        CategoryId = sweet.Id
                     },
                    new MenuItems
                    {
                        Emertimi = "Cheese Tropical",
                        Pershkrimi = "Creamy cheese dessert topped with tropical fruits for a sweet and refreshing treat.",
                        Cmimi = 2.50m,
                        Foto = "agusholli/4.png",
                        Disponueshme = true,
                        Alergjene = "Gluten,Milk,Eggs",
                        Kalori = 280,
                        CategoryId = sweet.Id
                     },
                    new MenuItems
                    {
                        Emertimi = "Snikers",
                        Pershkrimi = "Rich chocolate and caramel dessert layered with peanuts and creamy filling.",
                        Cmimi = 2.50m,
                        Foto = "agusholli/5.png",
                        Disponueshme = true,
                        Alergjene = "Gluten,Milk,Eggs",
                        Kalori = 400,
                        CategoryId = sweet.Id
                     },
                    new MenuItems
                    {
                        Emertimi = "Snikers",
                        Pershkrimi = "Classic Italian dessert with layers of coffee-soaked ladyfingers, creamy mascarpone, and cocoa.",
                        Cmimi = 2.50m,
                        Foto = "agusholli/6.png",
                        Disponueshme = true,
                        Alergjene = "Gluten,Milk,Eggs",
                        Kalori = 350,
                        CategoryId = sweet.Id
                     },
                    new MenuItems
                    {
                        Emertimi = "Oreo",
                        Pershkrimi = "Creamy dessert layered with crushed Oreo cookies and sweet filling.",
                        Cmimi = 2.30m,
                        Foto = "agusholli/7.png",
                        Disponueshme = true,
                        Alergjene = "Gluten,Milk,Eggs",
                        Kalori = 350,
                        CategoryId = sweet.Id
                     },
                    new MenuItems
                    {
                        Emertimi = "Lotus",
                        Pershkrimi = "Creamy dessert layered with caramelized Lotus Biscoff cookies for a rich, sweet treat.",
                        Cmimi = 2.50m,
                        Foto = "agusholli/8.png",
                        Disponueshme = true,
                        Alergjene = "Gluten,Milk,Eggs",
                        Kalori = 350,
                        CategoryId = sweet.Id
                    }

                };
                    context.MenuItems.AddRange(sweetItems);
                    context.SaveChanges();

                }
            }

            var categoryMap = await context.Categories
                .ToDictionaryAsync(c => c.Name.ToLower().Trim(), c => c.Id);

            var restaurantsWithoutCategoryId = await context.Restaurants
                .Where(r => r.CategoryId == null && !string.IsNullOrWhiteSpace(r.Kategori))
                .ToListAsync();

            foreach (var restaurant in restaurantsWithoutCategoryId)
            {
                var normalizedCategory = restaurant.Kategori.ToLower().Trim();
                if (categoryMap.TryGetValue(normalizedCategory, out var categoryId))
                {
                    restaurant.CategoryId = categoryId;
                }
            }

            await context.SaveChangesAsync();

            var categoriesSaray = new List<MenuCategory>();
            var saray = context.Restaurants.FirstOrDefault(r => r.Emertimi == "Saray Sweets");

            if (saray != null)
            {
                var bakllavaTradicionale = new MenuCategory
                {
                    Emertimi = "Bakllava Tradicionale",
                    Pershkrimi = "Traditional baklava varieties prepared with nuts and syrup.",
                    Renditja = 1,
                    RestaurantId = saray.Id
                };

                var burma = new MenuCategory
                {
                    Emertimi = "Burma",
                    Pershkrimi = "Rolled pastry sweets with syrup.",
                    Renditja = 2,
                    RestaurantId = saray.Id
                };

                var havuc = new MenuCategory
                {
                    Emertimi = "Havuc Dilim",
                    Pershkrimi = "Carrot-shaped baklava slice specialty.",
                    Renditja = 3,
                    RestaurantId = saray.Id
                };

                var kadaif = new MenuCategory
                {
                    Emertimi = "Kadaif",
                    Pershkrimi = "Shredded pastry dessert with syrup.",
                    Renditja = 4,
                    RestaurantId = saray.Id
                };

                var specialitete = new MenuCategory
                {
                    Emertimi = "Specialitete",
                    Pershkrimi = "House specialties and unique sweets.",
                    Renditja = 5,
                    RestaurantId = saray.Id
                };

                var kuru = new MenuCategory
                {
                    Emertimi = "Kuru",
                    Pershkrimi = "Dry sweets and pastries.",
                    Renditja = 6,
                    RestaurantId = saray.Id
                };

                var midye = new MenuCategory
                {
                    Emertimi = "Midye",
                    Pershkrimi = "Shell-shaped baklava specialty.",
                    Renditja = 7,
                    RestaurantId = saray.Id
                };

                categoriesSaray.AddRange(new[] { bakllavaTradicionale, burma, havuc, kadaif, specialitete, kuru, midye });
                context.MenuCategories.AddRange(categoriesSaray);
                context.SaveChanges();


                var bakllavaTradicionaleItems = new List<MenuItems>
                    {
                        new MenuItems
                        {
                            Emertimi = "Bakllava e Ftohtë me Kokos",
                            Pershkrimi = "Traditional baklava served cold, layered with coconut flakes and sweet syrup.",
                            Cmimi = 1.80m,
                            Foto = "saray/bakllavaKokos.png",
                            Disponueshme = true,
                            Alergjene = "Gluten, Milk, Eggs, Nuts, Coconut",
                            Kalori = 310,
                            CategoryId = bakllavaTradicionale.Id
                         },
                        new MenuItems
                        {
                            Emertimi = "Bakllava e Ftohtë me Fistik",
                            Pershkrimi = "Traditional baklava filled with pistachios, served cold with syrup.",
                            Cmimi = 1.80m,
                            Foto = "saray/bakllavaFistik.png",
                            Disponueshme = true,
                            Alergjene = "Gluten, Milk, Eggs, Nuts (Pistachio)",
                            Kalori = 320,
                            CategoryId = bakllavaTradicionale.Id
                         },
                        new MenuItems
                        {
                             Emertimi = "Bakllava e Ftohtë me Arra",
                             Pershkrimi = "Traditional baklava filled with walnuts, served cold with syrup.",
                             Cmimi = 1.80m,
                             Foto = "saray/bakllavaArra.png",
                             Disponueshme = true,
                             Alergjene = "Gluten, Milk, Eggs, Nuts (Walnuts)",
                             Kalori = 315,
                             CategoryId = bakllavaTradicionale.Id

                        }
                    };
                context.MenuItems.AddRange(bakllavaTradicionaleItems);
                context.SaveChanges();

                var burmaItems = new List<MenuItems>
                    {
                        new MenuItems
                        {
                            Emertimi = "Burma Arra",
                            Pershkrimi = "Rolled pastry filled with walnuts and sweet syrup.",
                            Cmimi = 1.20m,
                            Foto = "saray/burmaArra.png",
                            Disponueshme = true,
                            Alergjene = "Gluten, Milk, Eggs, Nuts (Walnuts)",
                            Kalori = 280,
                            CategoryId = burma.Id

                        },
                        new MenuItems
                        {
                            Emertimi = "Burma Fistik",
                            Pershkrimi = "Rolled pastry filled with pistachios and sweet syrup.",
                            Cmimi = 1.50m,
                            Foto = "saray/burmaFistik.png",
                            Disponueshme = true,
                            Alergjene = "Gluten, Milk, Eggs, Nuts (Pistachio)",
                            Kalori = 290,
                            CategoryId = burma.Id
                         },
                        new MenuItems
                        {
                            Emertimi = "Burma Kokos",
                            Pershkrimi = "Rolled pastry filled with coconut flakes and syrup.",
                            Cmimi = 1.00m,
                            Foto = "saray/burmaKokos.png",
                            Disponueshme = true,
                            Alergjene = "Gluten, Milk, Eggs, Coconut",
                            Kalori = 270,
                            CategoryId = burma.Id
                        },
                        new MenuItems
                        {
                            Emertimi = "Burma Kadaif",
                            Pershkrimi = "Rolled kadaif pastry with nuts and syrup.",
                            Cmimi = 1.70m,
                            Foto = "saray/burmaKadaif.png",
                            Disponueshme = true,
                            Alergjene = "Gluten, Milk, Eggs, Nuts",
                            Kalori = 270,
                            CategoryId = burma.Id
                         }
                    };
                context.MenuItems.AddRange(burmaItems);
                context.SaveChanges();

                var havucDilimItems = new List<MenuItems>
                    {
                        new MenuItems
                        {
                            Emertimi = "Havuc Dilim Arra",
                            Pershkrimi = "Carrot-shaped baklava slice filled with walnuts and sweet syrup.",
                            Cmimi = 3.00m,
                            Foto = "saray/havucArra.png",
                            Disponueshme = true,
                            Alergjene = "Gluten, Milk, Eggs, Nuts (Walnuts)",
                            Kalori = 350,
                            CategoryId = havuc.Id
                        },
                        new MenuItems
                        {
                            Emertimi = "Havuc Dilim Fistik",
                            Pershkrimi = "Carrot-shaped baklava slice filled with pistachios and sweet syrup.",
                            Cmimi = 1.80m,
                            Foto = "saray/havucFistik.png",
                            Disponueshme = true,
                            Alergjene = "Gluten, Milk, Eggs, Nuts (Pistachio)",
                            Kalori = 310,
                            CategoryId = havuc.Id
                         },
                        new MenuItems
                        {
                             Emertimi = "Havuc Dilim Mor",
                             Pershkrimi = "Carrot-shaped baklava slice with purple coloring and syrup.",
                             Cmimi = 3.50m,
                             Foto = "saray/havucMor.png",
                             Disponueshme = true,
                             Alergjene = "Gluten, Milk, Eggs",
                             Kalori = 360,
                             CategoryId = havuc.Id
                         },
                        new MenuItems
                        {
                             Emertimi = "Havuc Dilim Nutella",
                             Pershkrimi = "Carrot-shaped baklava slice filled with Nutella cream and syrup.",
                             Cmimi = 3.50m,
                             Foto = "saray/havucNutella.png",
                             Disponueshme = true,
                             Alergjene = "Gluten, Milk, Eggs, Nuts (Cashews)",
                             Kalori = 380,
                             CategoryId = havuc.Id
                         }
                     };
                context.MenuItems.AddRange(havucDilimItems);
                context.SaveChanges();

                var kadaifItems = new List<MenuItems>
                    {
                        new MenuItems
                        {
                            Emertimi = "Kadaif Krem",
                            Pershkrimi = "Shredded kadaif pastry layered with cream and sweet syrup.",
                            Cmimi = 3.50m,
                            Foto = "saray/kadaifKrem.png",
                            Disponueshme = true,
                            Alergjene = "Gluten, Milk, Eggs",
                            Kalori = 340,
                            CategoryId = kadaif.Id
                        },
                        new MenuItems
                        {
                            Emertimi = "Kadaif Arra",
                            Pershkrimi = "Shredded pastry dessert filled with walnuts and sweet syrup.",
                            Cmimi = 3.50m,
                            Foto = "saray/kadaifArra.png",
                            Disponueshme = true,
                            Alergjene = "Gluten, Milk, Eggs, Nuts (Walnuts)",
                            Kalori = 360,
                            CategoryId = kadaif.Id
                        }

                     };
                context.MenuItems.AddRange(kadaifItems);
                context.SaveChanges();

                var specialiteteItems = new List<MenuItems>
                    {
                        new MenuItems
                        {
                            Emertimi = "Kelebek",
                            Pershkrimi = "Butterfly-shaped specialty pastry with syrup.",
                            Cmimi = 1.00m,
                            Foto = "saray/kelebek.png",
                            Disponueshme = true,
                            Alergjene = "Gluten, Milk, Eggs",
                            Kalori = 250,
                            CategoryId = specialitete.Id
                        },
                        new MenuItems
                        {
                            Emertimi = "Kush me Fistik",
                            Pershkrimi = "Specialty pastry filled with pistachios and syrup.",
                            Cmimi = 2.00m,
                            Foto = "saray/kushFistik.png",
                            Disponueshme = true,
                            Alergjene = "Gluten, Milk, Eggs,Nuts (Pistachios)",
                            Kalori = 320,
                            CategoryId = specialitete.Id
                        }
                     };

                context.MenuItems.AddRange(specialiteteItems);
                context.SaveChanges();

                var kuruItems = new List<MenuItems>
                    {
                        new MenuItems
                        {
                            Emertimi = "Kuru Arra",
                            Pershkrimi = "Dry pastry filled with walnuts and sweet syrup.",
                            Cmimi = 1.20m,
                            Foto = "saray/kuruArra.png",
                            Disponueshme = true,
                            Alergjene = "Gluten, Milk, Eggs, Nuts (Walnuts)",
                            Kalori = 280,
                            CategoryId = kuru.Id
                        },
                        new MenuItems
                        {
                            Emertimi = "Kuru Fistik",
                            Pershkrimi = "Dry pastry filled with pistachios and sweet syrup.",
                            Cmimi = 1.50m,
                            Foto = "saray/kuruFistik.png",
                            Disponueshme = true,
                            Alergjene = "Gluten, Milk, Eggs, Nuts (Pistachios)",
                            Kalori = 290,
                            CategoryId = kuru.Id
                        },
                        new MenuItems
                        {
                            Emertimi = "Kuru Mor",
                            Pershkrimi = "Dry baklava-style sweet with purple coloring and syrup.",
                            Cmimi = 1.70m,
                            Foto = "saray/kuruMor.png",
                            Disponueshme = true,
                            Alergjene = "Gluten, Milk, Eggs",
                            Kalori = 300,
                            CategoryId = kuru.Id
                        },
                        new MenuItems
                        {
                            Emertimi = "Kuru Nutella",
                            Pershkrimi = "Dry baklava-style sweet filled with Nutella cream.",
                            Cmimi = 1.70m,
                            Foto = "saray/kuruNutella.png",
                            Disponueshme = true,
                            Alergjene = "Gluten, Milk, Eggs, Nuts (Hazelnuts)",
                            Kalori = 320,
                            CategoryId = kuru.Id
                        }

                    };
                context.MenuItems.AddRange(kuruItems);
                context.SaveChanges();

                var midyeItems = new List<MenuItems>
                    {
                        new MenuItems
                        {
                            Emertimi = "Midye Fistik",
                            Pershkrimi = "Shell-shaped baklava specialty filled with pistachios and sweet syrup.",
                            Cmimi = 1.80m,
                            Foto = "saray/midyeFistik.png",
                            Disponueshme = true,
                            Alergjene = "Gluten, Milk, Eggs,Nuts (Pistachios)",
                            Kalori = 330,
                            CategoryId = midye.Id
                        }
                     };

                context.MenuItems.AddRange(midyeItems);
                context.SaveChanges();

            }

            var categoriesCapvin13 = new List<MenuCategory>();
            var capvin13 = context.Restaurants.FirstOrDefault(r => r.Emertimi == "Capvin 13");


            if (capvin13 != null)
            {
                var uniqueMaestro = new MenuCategory
                {
                    Emertimi = "Unique by Maestro Capuano",
                    Pershkrimi = "Exclusive dishes crafted by Maestro Capuano, showcasing his culinary artistry and creativity.",
                    Renditja = 1,
                    RestaurantId = capvin13.Id
                };
                var innovativePizza = new MenuCategory
                {
                    Emertimi = "Innovative Pizzas",
                    Pershkrimi = "Creative pizza combinations with unique toppings and flavors, crafted with high-quality ingredients.",
                    Renditja = 2,
                    RestaurantId = capvin13.Id
                };
                var traditionalPizza = new MenuCategory
                {
                    Emertimi = "Traditional Pizzas",
                    Pershkrimi = "Classic pizza varieties made with traditional recipes and high-quality ingredients.",
                    Renditja = 3,
                    RestaurantId = capvin13.Id
                };
                var calzoneNapoletano = new MenuCategory
                {
                    Emertimi = "Calzone Napoletano",
                    Pershkrimi = "Traditional Neapolitan calzone filled with classic ingredients and baked to perfection.",
                    Renditja = 4,
                    RestaurantId = capvin13.Id
                };
                var drinks = new MenuCategory
                {
                    Emertimi = "Drinks",
                    Pershkrimi = "A selection of beverages to complement your meal, including soft drinks, juices, and more.",
                    Renditja = 5,
                    RestaurantId = capvin13.Id
                };
                categoriesCapvin13.AddRange(new[] { uniqueMaestro, innovativePizza, traditionalPizza, calzoneNapoletano, drinks });
                context.MenuCategories.AddRange(categoriesCapvin13);
                context.SaveChanges();

                var uniqueMaestroItems = new List<MenuItems>
                {
                    new MenuItems
                    {
                        Emertimi = "Don Vincenzo",
                        Pershkrimi = "Ricotta, mozzarella di bufala DOP, pecorino romano, crunchy bread, yellow tomatoes, olive oil.",
                        Cmimi = 11.00m,
                        Foto = "capvin13/donVincenzo.png",
                        Disponueshme = true,
                        Alergjene = "Gluten, Milk",
                        Kalori = 950,
                        CategoryId = uniqueMaestro.Id
                     },
                    new MenuItems
                    {
                        Emertimi = "Abbraccio E Mamma",
                        Pershkrimi = "Ricotta, fiordilatte, classic meatballs, grana padano fondue, olive oil.",
                        Cmimi = 10.00m,
                        Foto = "capvin13/abbraccioMamma.png",
                        Disponueshme = true,
                        Alergjene = "Gluten, Milk",
                        Kalori = 900,
                        CategoryId = uniqueMaestro.Id

                    },
                    new MenuItems
                    {
                        Emertimi = "Don Egidio",
                        Pershkrimi = "Pumpkin cream base, pumpkin chips, classic meatballs, stracciatella, olive oil, basil.",
                        Cmimi = 9.50m,
                        Foto = "capvin13/donEgidio.png",
                        Disponueshme = true,
                        Alergjene = "Milk",
                        Kalori = 880,
                        CategoryId = uniqueMaestro.Id
                     },
                    new MenuItems
                    {
                        Emertimi = "Tetti Illuminati",
                        Pershkrimi = "Chicken mortadella, pistachio cream, pecorino romano, olive oil, basil.",
                        Cmimi = 10.00m,
                        Foto = "capvin13/tettiIlluminati.png",
                        Disponueshme = true,
                        Alergjene = "Milk, Nuts (Pistachios)",
                        Kalori = 910,
                        CategoryId = uniqueMaestro.Id

                    },
                    new MenuItems
                    {
                        Emertimi = "Napolitudine",
                        Pershkrimi = "Hand-crushed tomatoes, smoked provola, meatballs, ricotta, basil, olive oil.",
                        Cmimi = 9.00m,
                        Foto = "capvin13/napolitudine.png",
                        Disponueshme = true,
                        Alergjene = "Milk",
                        Kalori = 870,
                        CategoryId = uniqueMaestro.Id
                     },
                    new MenuItems
                    {
                        Emertimi = "Provola e Pepe Campione",
                        Pershkrimi = "Smoked provola cheese and black pepper specialty.",
                        Cmimi = 9.00m,
                        Foto = "capvin13/provolaPepe.png",
                        Disponueshme = true,
                        Alergjene = "Milk",
                        Kalori = 860,
                        CategoryId = uniqueMaestro.Id
                     }
                 };
                context.MenuItems.AddRange(uniqueMaestroItems);
                context.SaveChanges();

                var innovativePizzaItems = new List<MenuItems>
                {
                    new MenuItems
                    {
                        Emertimi = "Centro Calabria",
                        Pershkrimi = "Hand-crushed tomatoes, fiordilatte, spicy salami, olive oil, basil.",
                        Cmimi = 9.00m,
                        Foto = "capvin13/centroCalabria.png",
                        Disponueshme = true,
                        Alergjene = "Gluten, Milk",
                        Kalori = 880,
                        CategoryId = innovativePizza.Id
                     },
                    new MenuItems
                    {
                        Emertimi = "Ritorno A Napoli",
                        Pershkrimi = "Smoked provola, zucchini cream, tomato bruschetta, potato chips.",
                        Cmimi = 10.00m,
                        Foto = "capvin13/ritornoNapoli.png",
                        Disponueshme = true,
                        Alergjene = "Gluten, Milk",
                        Kalori = 900,
                        CategoryId = innovativePizza.Id
                    },
                    new MenuItems
                    {
                        Emertimi = "Marinara Contemporanea",
                        Pershkrimi = "Hand-crushed tomatoes, garlic, olive oil, basil, chili.",
                        Cmimi = 12.00m,
                        Foto = "capvin13/marinaraContemporanea.png",
                        Disponueshme = true,
                        Alergjene = "Gluten",
                        Kalori = 8200,
                        CategoryId = innovativePizza.Id
                     },
                    new MenuItems
                    {
                        Emertimi = "Come Una Capricciosa",
                        Pershkrimi = "Olive oil, mushrooms, fiordilatte, tomatoes, roasted ham, olives, D.O.P.",
                        Cmimi = 12.00m,
                        Foto = "capvin13/comeCapricciosa.png",
                        Disponueshme = true,
                        Alergjene = "Gluten, Milk",
                        Kalori = 950,
                        CategoryId = innovativePizza.Id

                    },
                    new MenuItems
                    {
                        Emertimi = "Melanzanella",
                        Pershkrimi = "Tomatoes, fried aubergines, stracciatella cream, olive oil, basil.",
                        Cmimi = 10.00m,
                        Foto = "capvin13/melanzanella.png",
                        Disponueshme = true,
                        Alergjene = "Gluten, Milk",
                        Kalori = 890,
                        CategoryId = innovativePizza.Id
                     },
                    new MenuItems
                    {
                        Emertimi = "Stella Di Capuano",
                        Pershkrimi = "Star-shaped pizza with ricotta, fiordilatte, mortadella, pistachios, olive oil, basil.",
                        Cmimi = 16.00m,
                        Foto = "capvin13/stellaCapuano.png",
                        Disponueshme = true,
                        Alergjene = "Gluten, Milk, Nuts (Pistachios)",
                        Kalori = 800,
                        CategoryId = innovativePizza.Id
                     },
                    new MenuItems
                    {
                        Emertimi = "Bellelaria",
                        Pershkrimi = "Burrata base, pesto, fiordilatte, tomatoes, olive oil.",
                        Cmimi = 11.00m,
                        Foto = "capvin13/bellelaria.png",
                        Disponueshme = true,
                        Alergjene = "Gluten, Milk, Nuts (Pine nuts in pesto)",
                        Kalori = 970,
                        CategoryId = innovativePizza.Id
                     }
                 };
                context.MenuItems.AddRange(innovativePizzaItems);
                context.SaveChanges();

                var traditionalPizzaItems = new List<MenuItems>
                {
                    new MenuItems
                    {
                        Emertimi = "Marinara",
                        Pershkrimi = "Hand-crushed tomatoes, garlic, olive oil, basil.",
                        Cmimi = 6.00m,
                        Foto = "capvin13/marinara.png",
                        Disponueshme = true,
                        Alergjene = "Gluten",
                        Kalori = 750,
                        CategoryId = traditionalPizza.Id
                    },
                     new MenuItems
                    {
                        Emertimi = "Margherita",
                        Pershkrimi = "Hand-crushed tomatoes, fiordilatte, basil, olive oil.",
                        Cmimi = 7.00m,
                        Foto = "capvin13/margherita.png",
                        Disponueshme = true,
                        Alergjene = "Gluten, Milk",
                        Kalori = 820,
                        CategoryId = traditionalPizza.Id
                     },
                    new MenuItems
                    {
                        Emertimi = "Napoli",
                        Pershkrimi = "Tomatoes, fiordilatte, anchovies, olive oil, basil.",
                        Cmimi = 10.00m,
                        Foto = "capvin13/napoli.png",
                        Disponueshme = true,
                        Alergjene = "Gluten, Milk,Fish",
                        Kalori = 880,
                        CategoryId = traditionalPizza.Id
                     },
                    new MenuItems
                    {
                       Emertimi = "Diovala Alla Nonno Enzo",
                       Pershkrimi = "Provola, fiordilatte, spicy sausage, olive oil, basil.",
                       Cmimi = 8.00m,
                       Foto = "capvin13/diovalaNonnoEnzo.png",
                       Disponueshme = true,
                       Alergjene = "Gluten, Milk",
                       Kalori = 900,
                       CategoryId = traditionalPizza.Id
                    },
                    new MenuItems
                    {
                       Emertimi = "Sguardo Alto",
                       Pershkrimi = "Provola, pesto, bresaola, grana padano, olive oil, basil.",
                       Cmimi = 10.00m,
                       Foto = "capvin13/sguardoAlto.png",
                       Disponueshme = true,
                       Alergjene = "Gluten, Milk, Nuts (Pine nuts in pesto)",
                       Kalori = 950,
                       CategoryId = traditionalPizza.Id
                    },
                    new MenuItems
                    {
                       Emertimi = "Salsiccia & Broccoli",
                       Pershkrimi = "Sausage, broccoli, fiordilatte, olive oil, basil.",
                       Cmimi = 9.00m,
                       Foto = "capvin13/salsicciaBroccoli.png",
                       Disponueshme = true,
                       Alergjene = "Gluten, Milk",
                       Kalori = 870,
                       CategoryId = traditionalPizza.Id

                    }
                 };
                context.MenuItems.AddRange(traditionalPizzaItems);
                context.SaveChanges();

                var calzoneNapoletanoItems = new List<MenuItems>
                {
                    new MenuItems
                    {
                        Emertimi = "Calzone Al Forno",
                        Pershkrimi = "Ricotta fuscella, Napoli salami, turkey, mountain fiordilatte, hand-crushed tomatoes.",
                        Cmimi = 9.00m,
                        Foto = "capvin13/calzoneAlForno.png",
                        Disponueshme = true,
                        Alergjene = "Gluten, Milk, Eggs",
                        Kalori = 950,
                        CategoryId = calzoneNapoletano.Id
                     },
                    new MenuItems
                    {
                        Emertimi = "Pizza Fritta Completa",
                        Pershkrimi = "Ricotta fuscella, hand-crushed tomatoes, Napoli salami, smoked provola, Vincenzo’s meatballs, basil, olive oil.",
                        Cmimi = 8.00m,
                        Foto = "capvin13/pizzaFrittaCompleta.png",
                        Disponueshme = true,
                        Alergjene = "Gluten, Milk, Eggs",
                        Kalori = 980,
                        CategoryId = calzoneNapoletano.Id
                     }

                 };

                context.MenuItems.AddRange(calzoneNapoletanoItems);
                context.SaveChanges();

                var drinksItems = new List<MenuItems>
{
    new MenuItems
    {
        Emertimi = "Ujë Rugove Glass",
        Pershkrimi = "Natural spring water Rugove served in glass bottle.",
        Cmimi = 1.20m,
        Foto = "capvin13/ujeRugove.png",
        Disponueshme = true,
        Alergjene = "None",
        Kalori = 0,
        CategoryId = drinks.Id
    },
    new MenuItems
    {
        Emertimi = "Acqua Tepelena",
        Pershkrimi = "Mineral water Tepelena.",
        Cmimi = 1.50m,
        Foto = "capvin13/acquaTepelena.png",
        Disponueshme = true,
        Alergjene = "None",
        Kalori = 0,
        CategoryId = drinks.Id
    },
    new MenuItems
    {
        Emertimi = "San Pellegrino (Small)",
        Pershkrimi = "Sparkling mineral water San Pellegrino small bottle.",
        Cmimi = 1.20m,
        Foto = "capvin13/sanPellegrinoSmall.png",
        Disponueshme = true,
        Alergjene = "None",
        Kalori = 0,
        CategoryId = drinks.Id
    },
    new MenuItems
    {
        Emertimi = "San Pellegrino (Large)",
        Pershkrimi = "Sparkling mineral water San Pellegrino large bottle.",
        Cmimi = 3.00m,
        Foto = "capvin13/sanPellegrinoLarge.png",
        Disponueshme = true,
        Alergjene = "None",
        Kalori = 0,
        CategoryId = drinks.Id
    },
    new MenuItems
    {
        Emertimi = "Coca Cola",
        Pershkrimi = "Classic Coca Cola soft drink.",
        Cmimi = 1.50m,
        Foto = "capvin13/cocaCola.png",
        Disponueshme = true,
        Alergjene = "None",
        Kalori = 140,
        CategoryId = drinks.Id
    },
    new MenuItems
    {
        Emertimi = "Coca Cola Zero",
        Pershkrimi = "Sugar-free Coca Cola Zero.",
        Cmimi = 1.50m,
        Foto = "capvin13/cocaColaZero.png",
        Disponueshme = true,
        Alergjene = "None",
        Kalori = 0,
        CategoryId = drinks.Id
    },
    new MenuItems
    {
        Emertimi = "Schweppes",
        Pershkrimi = "Refreshing Schweppes tonic drink.",
        Cmimi = 1.50m,
        Foto = "capvin13/schweppes.png",
        Disponueshme = true,
        Alergjene = "None",
        Kalori = 120,
        CategoryId = drinks.Id
    },
    new MenuItems
    {
        Emertimi = "Sprite",
        Pershkrimi = "Lemon-lime flavored Sprite soft drink.",
        Cmimi = 1.50m,
        Foto = "capvin13/sprite.png",
        Disponueshme = true,
        Alergjene = "None",
        Kalori = 130,
        CategoryId = drinks.Id
    },
    new MenuItems
    {
        Emertimi = "Fanta",
        Pershkrimi = "Orange flavored Fanta soft drink.",
        Cmimi = 1.50m,
        Foto = "capvin13/fanta.png",
        Disponueshme = true,
        Alergjene = "None",
        Kalori = 150,
        CategoryId = drinks.Id
    },
    new MenuItems
    {
        Emertimi = "Santal",
        Pershkrimi = "Fruit juice Santal.",
        Cmimi = 1.50m,
        Foto = "capvin13/santal.png",
        Disponueshme = true,
        Alergjene = "None",
        Kalori = 110,
        CategoryId = drinks.Id
    },
    new MenuItems
    {
        Emertimi = "Fructal Fresh Juice",
        Pershkrimi = "Natural fruit juice Fructal.",
        Cmimi = 1.50m,
        Foto = "capvin13/fructal.png",
        Disponueshme = true,
        Alergjene = "None",
        Kalori = 120,
        CategoryId = drinks.Id
    },
    new MenuItems
    {
        Emertimi = "Red Bull",
        Pershkrimi = "Energy drink Red Bull.",
        Cmimi = 3.00m,
        Foto = "capvin13/redBull.png",
        Disponueshme = true,
        Alergjene = "None",
        Kalori = 160,
        CategoryId = drinks.Id
    },
    new MenuItems
    {
        Emertimi = "Rose Lemonade",
        Pershkrimi = "Refreshing rose lemonade.",
        Cmimi = 3.00m,
        Foto = "capvin13/roseLemonade.png",
        Disponueshme = true,
        Alergjene = "None",
        Kalori = 100,
        CategoryId = drinks.Id
    },
    new MenuItems
    {
        Emertimi = "Birra 0%",
        Pershkrimi = "Non-alcoholic beer.",
        Cmimi = 3.00m,
        Foto = "capvin13/birraZero.png",
        Disponueshme = true,
        Alergjene = "Gluten",
        Kalori = 90,
        CategoryId = drinks.Id
    }
};

                context.MenuItems.AddRange(drinksItems);
                context.SaveChanges();

            }

            var categoriesFika = new List<MenuCategory>();
            var fika = context.Restaurants.FirstOrDefault(r => r.Emertimi == "Fika Eatery");

            if (fika != null)
            {

                var burgers = new MenuCategory
                {

                    Emertimi = "Burger",
                    Pershkrimi = "Juicy grilled patty with fresh lettuce, tomato, cheese, and signature sauce in a soft toasted bun",
                    Renditja = 1,
                    RestaurantId = fika.Id

                };

                var sandwich = new MenuCategory
                {

                    Emertimi = "Sandwich",
                    Pershkrimi = "Freshly made with your choice of filling, crisp vegetables, and flavorful sauce in soft, toasted bread.",
                    Renditja = 2,
                    RestaurantId = fika.Id

                };
                var bagel = new MenuCategory
                {

                    Emertimi = " Bagel Sandwich",
                    Pershkrimi = "Freshly made with your choice of filling, crisp vegetables, and flavorful sauce in soft, bagel",
                    Renditja = 3,
                    RestaurantId = fika.Id

                };
                var salad = new MenuCategory
                {

                    Emertimi = " Salad",
                    Pershkrimi = "Fresh mixed greens with crisp vegetables and a light, flavorful dressing.",
                    Renditja = 4,
                    RestaurantId = fika.Id

                };
                var risotto = new MenuCategory
                {

                    Emertimi = " Risotto",
                    Pershkrimi = "Creamy Italian rice dish cooked slowly with rich flavors and finished with parmesan.",
                    Renditja = 5,
                    RestaurantId = fika.Id

                };
                var drinks = new MenuCategory
                {

                    Emertimi = " Drinks",
                    Pershkrimi = "Refreshing beverages to complement your meal.",
                    Renditja = 6,
                    RestaurantId = fika.Id

                };





                categoriesFika.AddRange(new[] { burgers, sandwich, bagel, salad, risotto, drinks });
                context.MenuCategories.AddRange(categoriesFika);
                context.SaveChanges();


                var burgerItems = new List<MenuItems> {
                new MenuItems{
                    Emertimi = "Fika Chicken Burger",
                    Pershkrimi = "Juicy grilled patty with fresh lettuce, tomato, cheese, and signature sauce in a soft toasted bun.",
                    Cmimi = 5.30m,
                    Foto = "fika/burger.png",
                    Disponueshme = true,
                    Alergjene = "Gluten, Milk",
                    Kalori = 550,
                    CategoryId = burgers.Id
                 },
                new MenuItems{
                    Emertimi = "Fika Beef Burger",
                    Pershkrimi = "Juicy grilled beef patty with fresh lettuce, tomato, cheese, and signature Fika sauce in a soft toasted bun.",
                    Cmimi = 5.80m,
                    Foto = "fika/burger1.png",
                    Disponueshme = true,
                    Alergjene = "Gluten, Milk,Mustard",
                    Kalori = 550,
                    CategoryId = burgers.Id
                 },
                new MenuItems{
                    Emertimi = "Fika Cheese Burger",
                    Pershkrimi = "Juicy beef patty topped with melted cheese, fresh lettuce, and creamy sauce in a soft toasted bun.",
                    Cmimi = 5.80m,
                    Foto = "fika/#burger2.png",
                    Disponueshme = true,
                    Alergjene = "Gluten, Milk,Mustard",
                    Kalori = 650,
                    CategoryId = burgers.Id
                 }
                };
                context.MenuItems.AddRange(burgerItems);
                context.SaveChanges();

                var sandwichItems = new List<MenuItems> {

                new MenuItems
                {
                    Emertimi = "Sandwich Spicy Chicken Crunch",
                    Pershkrimi = "Crispy chicken fillet with spicy seasoning, fresh lettuce, and creamy sauce in toasted bread for an extra crunchy bite.",
                    Cmimi = 4.50m,
                    Foto = "fika/sand1.png",
                    Disponueshme = true,
                    Alergjene = "Gluten, Milk,Mustard,Eggs",
                    Kalori = 500,
                    CategoryId = sandwich.Id


                },
                new MenuItems
                {
                    Emertimi = "Grilled Chicken Sandwich",
                    Pershkrimi = "Tender grilled chicken with fresh lettuce, tomato, and light sauce in soft toasted bread.",
                    Cmimi = 4.40m,
                    Foto = "fika/sand2.png",
                    Disponueshme = true,
                    Alergjene = "Gluten, Milk,Mustard,Eggs",
                    Kalori = 500,
                    CategoryId = sandwich.Id


                },
                new MenuItems
                {
                    Emertimi = "Prosciutto Sandwich",
                    Pershkrimi = "Thin slices of prosciutto with fresh lettuce, tomato, and creamy sauce in soft toasted bread.",
                    Cmimi = 5.00m,
                    Foto = "fika/sand3.png",
                    Disponueshme = true,
                    Alergjene = "Gluten, Milk,Mustard,Eggs",
                    Kalori = 450,
                    CategoryId = sandwich.Id


                },
                new MenuItems
                {
                    Emertimi = "Club Sandwich",
                    Pershkrimi = "Layered sandwich with chicken, crispy bacon, fresh lettuce, tomato, and creamy sauce on toasted bread.",
                    Cmimi = 4.00m,
                    Foto = "fika/sand4.png",
                    Disponueshme = true,
                    Alergjene = "Gluten, Milk,Mustard,Eggs",
                    Kalori = 600,
                    CategoryId = sandwich.Id


                },
                new MenuItems
                {
                    Emertimi = "Veggie Sandwich",
                    Pershkrimi = "Fresh vegetables with lettuce, tomato, cucumber, and light sauce in soft toasted bread.",
                    Cmimi = 4.00m,
                    Foto = "fika/sand5.png",
                    Disponueshme = true,
                    Alergjene = "Gluten",
                    Kalori = 300,
                    CategoryId = sandwich.Id


                }

                };
                context.MenuItems.AddRange(sandwichItems);
                context.SaveChanges();


                var bagelItems = new List<MenuItems> {

                new MenuItems
                {
                    Emertimi = "Prosciutto Bagel",
                    Pershkrimi = "Soft toasted bagel filled with prosciutto, fresh lettuce, tomato, and creamy sauce.",
                    Cmimi = 4.50m,
                    Foto = "fika/bagel1.png",
                    Disponueshme = true,
                    Alergjene = "Gluten,Milk,Eggs,Mustard",
                    Kalori = 450,
                    CategoryId = sandwich.Id
                },
                new MenuItems
                {
                    Emertimi = "Turkey Bagel",
                    Pershkrimi = "Soft toasted bagel filled with sliced turkey, fresh lettuce, tomato, and creamy sauce.",
                    Cmimi = 4.30m,
                    Foto = "fika/bagel2.png",
                    Disponueshme = true,
                    Alergjene = "Gluten,Milk,Eggs,Mustard",
                    Kalori = 450,
                    CategoryId = sandwich.Id
                },
                new MenuItems
                {
                    Emertimi = "Egg & Turkey Bagel",
                    Pershkrimi = "Soft toasted bagel filled with egg and turkey ham, fresh lettuce, tomato, and creamy sauce.",
                    Cmimi = 4.30m,
                    Foto = "fika/baglel3.png",
                    Disponueshme = true,
                    Alergjene = "Gluten,Milk,Eggs,Mustard",
                    Kalori = 450,
                    CategoryId = sandwich.Id
                },
                new MenuItems
                {
                    Emertimi = "Avocado & Egg Bagel",
                    Pershkrimi = "Soft toasted bagel filled with creamy avocado, boiled egg, fresh lettuce, and light sauce.",
                    Cmimi = 5.00m,
                    Foto = "fika/bagel4.png",
                    Disponueshme = true,
                    Alergjene = "Gluten,Milk,Eggs,Mustard",
                    Kalori = 400,
                    CategoryId = sandwich.Id
                },
                new MenuItems
                {
                    Emertimi = "Smoked Salmon Bagel",
                    Pershkrimi = "Soft toasted bagel filled with smoked salmon, cream cheese, fresh lettuce, and lemon touch.",
                    Cmimi = 6.50m,
                    Foto = "fika/bagel5.png",
                    Disponueshme = true,
                    Alergjene = "Gluten,Milk,Fish,Mustard",
                    Kalori = 450,
                    CategoryId = sandwich.Id
                }
                };
                context.MenuItems.AddRange(bagelItems);
                context.SaveChanges();

                var saladItems = new List<MenuItems>
                    {
                        new MenuItems
                        {
                            Emertimi = "Caesar Salad",
                            Pershkrimi = "Crisp romaine lettuce, creamy Caesar dressing, croutons, and parmesan cheese.",
                            Cmimi = 6.50m,
                            Foto = "fika/cesar.png",
                            Disponueshme = true,
                            Alergjene = "Milk, Gluten, Eggs, Fish (Anchovies in dressing)",
                            Kalori = 350,
                            CategoryId = salad.Id
                        },
                        new MenuItems
                        {
                            Emertimi = "Crispy Chicken Salad",
                            Pershkrimi = "Fresh mixed greens topped with crispy chicken, cherry tomatoes, cucumber, and light dressing.",
                            Cmimi = 6.50m,
                            Foto = "fika/salad.png",
                            Disponueshme = true,
                            Alergjene = "Milk,Soy,Eggs,Mustard",
                            Kalori = 450,
                            CategoryId = salad.Id
                        },

                        new MenuItems
                        {
                            Emertimi = "Tuna Salad",
                            Pershkrimi = "Fresh mixed greens with tuna, sweet corn, cherry tomatoes, and light dressing.",
                            Cmimi = 6.30m,
                            Foto = "fika/salad2.png",
                            Disponueshme = true,
                            Alergjene = "Gluten,Fish, Eggs ",
                            Kalori = 350,
                            CategoryId = salad.Id
                        }
                };
                context.MenuItems.AddRange(saladItems);
                context.SaveChanges();

                var risottoItems = new List<MenuItems> {
                new MenuItems
                {
                    Emertimi = "Chicken, Spinach & Sun-Dried Tomato Risotto",
                    Pershkrimi = "Creamy risotto with tender chicken, fresh spinach, and sun-dried tomatoes, finished with parmesan.",
                    Cmimi = 6.30m,
                    Foto = "fika/r1.png",
                    Disponueshme = true,
                    Alergjene = "Milk,Soy",
                    Kalori = 650,
                    CategoryId = risotto.Id
                 },
                new MenuItems
                {
                    Emertimi = "Chicken & Soy Sauce Risotto",
                    Pershkrimi = "Creamy risotto with tender chicken, flavored with savory soy sauce and finished with parmesan.",
                    Cmimi = 6.30m,
                    Foto = "fika/r2.png",
                    Disponueshme = true,
                    Alergjene = "Milk,Soy,Eggs,Gluten",
                    Kalori = 650,
                    CategoryId = risotto.Id
                 }

                };
                context.MenuItems.AddRange(risottoItems);
                context.SaveChanges();

                var drinkItems = new List<MenuItems> {
                new MenuItems
                {
                    Emertimi = "Coca Cola",
                    Pershkrimi = "Classic Coca Cola soft drink.",
                    Cmimi = 1.50m,
                    Foto = "pastafasta/CocaCola.png",
                    Disponueshme = true,
                    Alergjene = "None",
                    Kalori = 150,
                    CategoryId = drinks.Id
                },
                new MenuItems
                {
                    Emertimi = "Fanta Orange",
                    Pershkrimi = "Fanta Orange",
                    Cmimi = 1.50m,
                    Foto = "pastafasta/fantaorange.png",
                    Disponueshme = true,
                    Alergjene = "None",
                    Kalori = 150,
                    CategoryId = drinks.Id
                },
                new MenuItems
                {
                    Emertimi = "Sprite",
                    Pershkrimi = "Sprite",
                    Cmimi = 1.50m,
                    Foto = "pastafasta/sprite.png",
                    Disponueshme = true,
                    Alergjene = "None",
                    Kalori = 150,
                    CategoryId = drinks.Id
                },
                new MenuItems
                {
                    Emertimi = "Water",
                    Pershkrimi = "Water",
                    Cmimi = 1.50m,
                    Foto = "pastafasta/water.png",
                    Disponueshme = true,
                    Alergjene = "None",
                    Kalori = 150,
                    CategoryId = drinks.Id
                }

                };
                context.MenuItems.AddRange(drinkItems);
                context.SaveChanges();



            }
            var categoriesMulliri = new List<MenuCategory>();
            var mulliri = context.Restaurants.FirstOrDefault(r => r.Emertimi == "Mulliri");

            if (mulliri != null)
            {
                var espressoCoffee = new MenuCategory
                {
                    Emertimi = "Espresso & Coffee",
                    Pershkrimi = "Espresso and coffee classics.",
                    Renditja = 1,
                    RestaurantId = mulliri.Id
                };

                var singleOrigin = new MenuCategory
                {
                    Emertimi = "Single Origin Coffee",
                    Pershkrimi = "Specialty single origin coffees.",
                    Renditja = 2,
                    RestaurantId = mulliri.Id
                };

                var coldDrinks = new MenuCategory
                {
                    Emertimi = "Cold Drinks",
                    Pershkrimi = "Iced coffee and refreshing cold drinks.",
                    Renditja = 3,
                    RestaurantId = mulliri.Id
                };

                var hotChocolate = new MenuCategory
                {
                    Emertimi = "Hot Chocolate & Creams",
                    Pershkrimi = "Hot chocolate and creamy delights.",
                    Renditja = 4,
                    RestaurantId = mulliri.Id
                };

                var teas = new MenuCategory
                {
                    Emertimi = "Teas",
                    Pershkrimi = "Variety of teas.",
                    Renditja = 5,
                    RestaurantId = mulliri.Id
                };

                var hotBeverages = new MenuCategory
                {
                    Emertimi = "Hot Beverages",
                    Pershkrimi = "Selection of hot drinks.",
                    Renditja = 6,
                    RestaurantId = mulliri.Id
                };

                var juicesSmoothies = new MenuCategory
                {
                    Emertimi = "Juices & Smoothies",
                    Pershkrimi = "Fresh juices and smoothies.",
                    Renditja = 7,
                    RestaurantId = mulliri.Id
                };

                var milkshakes = new MenuCategory
                {
                    Emertimi = "Milkshakes",
                    Pershkrimi = "Creamy milkshakes.",
                    Renditja = 8,
                    RestaurantId = mulliri.Id
                };

                var granitaMocktails = new MenuCategory
                {
                    Emertimi = "Granita & Mocktails",
                    Pershkrimi = "Frozen granitas and mocktails.",
                    Renditja = 9,
                    RestaurantId = mulliri.Id
                };

                var water = new MenuCategory
                {
                    Emertimi = "Water",
                    Pershkrimi = "Still and sparkling water.",
                    Renditja = 10,
                    RestaurantId = mulliri.Id
                };

                var softDrinks = new MenuCategory
                {
                    Emertimi = "Soft Drinks",
                    Pershkrimi = "Refreshing soft drinks.",
                    Renditja = 11,
                    RestaurantId = mulliri.Id
                };

                var sandwiches = new MenuCategory
                {
                    Emertimi = "Sandwiches",
                    Pershkrimi = "Freshly made sandwiches.",
                    Renditja = 12,
                    RestaurantId = mulliri.Id
                };

                var croissants = new MenuCategory
                {
                    Emertimi = "Croissants",
                    Pershkrimi = "Croissants and pastries.",
                    Renditja = 13,
                    RestaurantId = mulliri.Id
                };

                var pancakesMuffins = new MenuCategory
                {
                    Emertimi = "Pancakes & Muffins",
                    Pershkrimi = "Delicious pancakes and muffins.",
                    Renditja = 14,
                    RestaurantId = mulliri.Id
                };

                var desserts = new MenuCategory
                {
                    Emertimi = "Desserts",
                    Pershkrimi = "Sweet dessert options.",
                    Renditja = 15,
                    RestaurantId = mulliri.Id
                };

                var rollsBiscuits = new MenuCategory
                {
                    Emertimi = "Rolls & Biscuits",
                    Pershkrimi = "Rolls and biscuits.",
                    Renditja = 16,
                    RestaurantId = mulliri.Id
                };

                var capsules = new MenuCategory
                {
                    Emertimi = "Capsules",
                    Pershkrimi = "Coffee capsules.",
                    Renditja = 17,
                    RestaurantId = mulliri.Id
                };

                var turkishCoffee = new MenuCategory
                {
                    Emertimi = "Turkish Coffee",
                    Pershkrimi = "Traditional Turkish coffee.",
                    Renditja = 18,
                    RestaurantId = mulliri.Id
                };

                var coffeeBeans = new MenuCategory
                {
                    Emertimi = "Coffee Beans",
                    Pershkrimi = "Whole coffee beans.",
                    Renditja = 19,
                    RestaurantId = mulliri.Id
                };

                var groundCoffee = new MenuCategory
                {
                    Emertimi = "Ground Coffee",
                    Pershkrimi = "Freshly ground coffee.",
                    Renditja = 20,
                    RestaurantId = mulliri.Id
                };

                var packagedTeas = new MenuCategory
                {
                    Emertimi = "Packaged Teas",
                    Pershkrimi = "Packaged tea varieties.",
                    Renditja = 21,
                    RestaurantId = mulliri.Id
                };

                var cocoaSalep = new MenuCategory
                {
                    Emertimi = "Cocoa & Salep",
                    Pershkrimi = "Cocoa and salep drinks.",
                    Renditja = 22,
                    RestaurantId = mulliri.Id
                };

                var sweetSnacks = new MenuCategory
                {
                    Emertimi = "Sweet Snacks",
                    Pershkrimi = "Selection of sweet snacks.",
                    Renditja = 23,
                    RestaurantId = mulliri.Id
                };

                var grandmasRecipes = new MenuCategory
                {
                    Emertimi = "Grandma’s Recipes",
                    Pershkrimi = "Traditional homemade recipes.",
                    Renditja = 24,
                    RestaurantId = mulliri.Id
                };

                categoriesMulliri.AddRange(new[] {
        espressoCoffee, singleOrigin, coldDrinks, hotChocolate, teas, hotBeverages, juicesSmoothies,
        milkshakes, granitaMocktails, water, softDrinks, sandwiches, croissants, pancakesMuffins,
        desserts, rollsBiscuits, capsules, turkishCoffee, coffeeBeans, groundCoffee, packagedTeas,
        cocoaSalep, sweetSnacks, grandmasRecipes
    });

                context.MenuCategories.AddRange(categoriesMulliri);
                context.SaveChanges();

                var espressoCoffeeItems = new List<MenuItems>
{
    new MenuItems
    {
        Emertimi = "Espresso",
        Pershkrimi = "Classic single shot of espresso.",
        Cmimi = 1.20m,
        Foto = "mulliri/Espresso.png",
        Disponueshme = true,
        Alergjene = "None",
        Kalori = 5,
        CategoryId = espressoCoffee.Id
    },
    new MenuItems
    {
        Emertimi = "Decaf Espresso",
        Pershkrimi = "Decaffeinated espresso shot.",
        Cmimi = 1.40m,
        Foto = "mulliri/EspressoDecaffeinato.png",
        Disponueshme = true,
        Alergjene = "None",
        Kalori = 5,
        CategoryId = espressoCoffee.Id
    },
    new MenuItems
    {
        Emertimi = "Double Espresso",
        Pershkrimi = "Strong double shot of espresso.",
        Cmimi = 2.00m,
        Foto = "mulliri/DoppioEspresso.png",
        Disponueshme = true,
        Alergjene = "None",
        Kalori = 10,
        CategoryId = espressoCoffee.Id
    },
    new MenuItems
    {
        Emertimi = "Macchiato",
        Pershkrimi = "Espresso topped with milk foam.",
        Cmimi = 1.20m,
        Foto = "mulliri/Macchiato.png",
        Disponueshme = true,
        Alergjene = "Milk",
        Kalori = 15,
        CategoryId = espressoCoffee.Id
    },
    new MenuItems
    {
        Emertimi = "Double Macchiato",
        Pershkrimi = "Double espresso with milk foam.",
        Cmimi = 2.40m,
        Foto = "mulliri/DoppioMacchiato.png",
        Disponueshme = true,
        Alergjene = "Milk",
        Kalori = 25,
        CategoryId = espressoCoffee.Id
    },
 new MenuItems
    {
        Emertimi = "Macchiato Doppio Latte",
        Pershkrimi = "Double espresso with milk foamss.",
        Cmimi = 2.40m,
        Foto = "mulliri/MacchiatoDoppioLatte.png",
        Disponueshme = true,
        Alergjene = "Milk",
        Kalori = 25,
        CategoryId = espressoCoffee.Id
    },

    new MenuItems
    {
        Emertimi = "Large Macchiato",
        Pershkrimi = "Large macchiato with extra milk.",
        Cmimi = 1.60m,
        Foto = "mulliri/MacchiatoeMadhe.png",
        Disponueshme = true,
        Alergjene = "Milk",
        Kalori = 35,
        CategoryId = espressoCoffee.Id
    },
    new MenuItems
    {
        Emertimi = "Caffe Latte",
        Pershkrimi = "Espresso with steamed milk and foam.",
        Cmimi = 1.90m,
        Foto = "mulliri/CaffeLatte.png",
        Disponueshme = true,
        Alergjene = "Milk",
        Kalori = 120,
        CategoryId = espressoCoffee.Id
    },
    new MenuItems
    {
        Emertimi = "Caramel Latte",
        Pershkrimi = "Latte flavored with caramel syrup.",
        Cmimi = 2.30m,
        Foto = "mulliri/CaramelLatte.png",
        Disponueshme = true,
        Alergjene = "Milk",
        Kalori = 150,
        CategoryId = espressoCoffee.Id
    },
    new MenuItems
    {
        Emertimi = "Vanilla Latte",
        Pershkrimi = "Latte flavored with vanilla syrup.",
        Cmimi = 2.30m,
        Foto = "mulliri/VanillaLatte.png",
        Disponueshme = true,
        Alergjene = "Milk",
        Kalori = 140,
        CategoryId = espressoCoffee.Id
    },
    new MenuItems
    {
        Emertimi = "Spanish Latte",
        Pershkrimi = "Latte with condensed milk for sweetness.",
        Cmimi = 2.60m,
        Foto = "mulliri/SpanishLatte.png",
        Disponueshme = true,
        Alergjene = "Milk",
        Kalori = 180,
        CategoryId = espressoCoffee.Id
    },
    new MenuItems
    {
        Emertimi = "Hot Mocha",
        Pershkrimi = "Espresso with chocolate and steamed milk.",
        Cmimi = 2.80m,
        Foto = "mulliri/HotMocha.png",
        Disponueshme = true,
        Alergjene = "Milk",
        Kalori = 190,
        CategoryId = espressoCoffee.Id
    },
    new MenuItems
    {
        Emertimi = "Americano",
        Pershkrimi = "Espresso diluted with hot water.",
        Cmimi = 1.40m,
        Foto = "mulliri/AmericanCoffee.png",
        Disponueshme = true,
        Alergjene = "None",
        Kalori = 10,
        CategoryId = espressoCoffee.Id
    },
    new MenuItems
    {
        Emertimi = "Americano Latte",
        Pershkrimi = "Americano with added milk.",
        Cmimi = 1.50m,
        Foto = "mulliri/AmericanCoffeeLatte.png",
        Disponueshme = true,
        Alergjene = "Milk",
        Kalori = 60,
        CategoryId = espressoCoffee.Id
    },
    new MenuItems
    {
        Emertimi = "Hot Mochaccino",
        Pershkrimi = "Espresso with chocolate, milk, and foam.",
        Cmimi = 2.80m,
        Foto = "mulliri/HotMochaccino.png",
        Disponueshme = true,
        Alergjene = "Milk",
        Kalori = 200,
        CategoryId = espressoCoffee.Id
    },
    new MenuItems
    {
        Emertimi = "Cafe Bombon",
        Pershkrimi = "Espresso with sweetened condensed milk.",
        Cmimi = 1.80m,
        Foto = "mulliri/CafeBombon.png",
        Disponueshme = true,
        Alergjene = "Milk",
        Kalori = 160,
        CategoryId = espressoCoffee.Id
    },
    new MenuItems
    {
        Emertimi = "Turkish Coffee",
        Pershkrimi = "Traditional Turkish-style coffee.",
        Cmimi = 1.00m,
        Foto = "mulliri/KafeTurke.png",
        Disponueshme = true,
        Alergjene = "None",
        Kalori = 15,
        CategoryId = espressoCoffee.Id
    },
    new MenuItems
    {
        Emertimi = "Turkish Coffee with Milk",
        Pershkrimi = "Traditional Turkish coffee with added milk.",
        Cmimi = 1.00m,
        Foto = "mulliri/KafeTurkeMeQumesht.png",
        Disponueshme = true,
        Alergjene = "Milk",
        Kalori = 40,
        CategoryId = espressoCoffee.Id
    }
};

                context.MenuItems.AddRange(espressoCoffeeItems);
                context.SaveChanges();


                var singleOriginItems = new List<MenuItems>
{
    new MenuItems
    {
        Emertimi = "Espresso Colombia Supremo",
        Pershkrimi = "Single origin espresso made from Colombia Supremo beans.",
        Cmimi = 1.40m,
        Foto = "mulliri/EspressoColombiaSupremo.png",
        Disponueshme = true,
        Alergjene = "None",
        Kalori = 5,
        CategoryId = singleOrigin.Id
    },
    new MenuItems
    {
        Emertimi = "Espresso Ethiopia Sidamo",
        Pershkrimi = "Single origin espresso made from Ethiopia Sidamo beans.",
        Cmimi = 1.40m,
        Foto = "mulliri/EspressoEthiopiaSidamo.png",
        Disponueshme = true,
        Alergjene = "None",
        Kalori = 5,
        CategoryId = singleOrigin.Id
    },
    new MenuItems
    {
        Emertimi = "Espresso Gourmet",
        Pershkrimi = "Premium gourmet espresso blend.",
        Cmimi = 1.40m,
        Foto = "mulliri/EspressoGourmet.png",
        Disponueshme = true,
        Alergjene = "None",
        Kalori = 5,
        CategoryId = singleOrigin.Id
    }
};

                context.MenuItems.AddRange(singleOriginItems);
                context.SaveChanges();


                var coldDrinksItems = new List<MenuItems>
{
    new MenuItems
    {
        Emertimi = "Freddo Espresso",
        Pershkrimi = "Iced espresso served cold.",
        Cmimi = 2.00m,
        Foto = "mulliri/FreddoEspresso.png",
        Disponueshme = true,
        Alergjene = "None",
        Kalori = 10,
        CategoryId = coldDrinks.Id
    },
    new MenuItems
    {
        Emertimi = "Freddo Cappuccino",
        Pershkrimi = "Iced cappuccino with milk foam.",
        Cmimi = 2.00m,
        Foto = "mulliri/FreddoCappuccino.png",
        Disponueshme = true,
        Alergjene = "Milk",
        Kalori = 80,
        CategoryId = coldDrinks.Id
    },
    new MenuItems
    {
        Emertimi = "Frappe",
        Pershkrimi = "Classic Greek-style iced coffee.",
        Cmimi = 1.90m,
        Foto = "mulliri/Frappe.png",
        Disponueshme = true,
        Alergjene = "Milk (optional)",
        Kalori = 60,
        CategoryId = coldDrinks.Id
    },
    new MenuItems
    {
        Emertimi = "Frappe with Milk",
        Pershkrimi = "Iced frappe blended with milk.",
        Cmimi = 1.90m,
        Foto = "mulliri/FrappeMequmesht.png",
        Disponueshme = true,
        Alergjene = "Milk",
        Kalori = 90,
        CategoryId = coldDrinks.Id
    },
    new MenuItems
    {
        Emertimi = "Iced Americano",
        Pershkrimi = "Espresso diluted with cold water and ice.",
        Cmimi = 1.50m,
        Foto = "mulliri/IcedAmericano.png",
        Disponueshme = true,
        Alergjene = "None",
        Kalori = 10,
        CategoryId = coldDrinks.Id
    },
    new MenuItems
    {
        Emertimi = "Iced Latte",
        Pershkrimi = "Espresso with cold milk and ice.",
        Cmimi = 2.00m,
        Foto = "mulliri/IcedLatte.png",
        Disponueshme = true,
        Alergjene = "Milk",
        Kalori = 120,
        CategoryId = coldDrinks.Id
    },
    new MenuItems
    {
        Emertimi = "Iced Caramel Latte",
        Pershkrimi = "Iced latte flavored with caramel syrup.",
        Cmimi = 2.40m,
        Foto = "mulliri/IcedLatteCaramel.png",
        Disponueshme = true,
        Alergjene = "Milk",
        Kalori = 150,
        CategoryId = coldDrinks.Id
    },
    new MenuItems
    {
        Emertimi = "Iced Vanilla Latte",
        Pershkrimi = "Iced latte flavored with vanilla syrup.",
        Cmimi = 2.40m,
        Foto = "mulliri/IcedLatteVanilla.png",
        Disponueshme = true,
        Alergjene = "Milk",
        Kalori = 140,
        CategoryId = coldDrinks.Id
    },
    new MenuItems
    {
        Emertimi = "Iced Spanish Latte",
        Pershkrimi = "Iced latte with condensed milk for sweetness.",
        Cmimi = 2.50m,
        Foto = "mulliri/IcedSpanishLatte.png",
        Disponueshme = true,
        Alergjene = "Milk",
        Kalori = 180,
        CategoryId = coldDrinks.Id
    },
    new MenuItems
    {
        Emertimi = "Iced Mocha",
        Pershkrimi = "Espresso with chocolate, milk, and ice.",
        Cmimi = 2.40m,
        Foto = "mulliri/IcedMocha.png",
        Disponueshme = true,
        Alergjene = "Milk",
        Kalori = 190,
        CategoryId = coldDrinks.Id
    },
    new MenuItems
    {
        Emertimi = "Freppuccino Chocolate",
        Pershkrimi = "Frozen blended chocolate coffee drink.",
        Cmimi = 2.40m,
        Foto = "mulliri/FreppuccinoÇokollatë.png",
        Disponueshme = true,
        Alergjene = "Milk",
        Kalori = 220,
        CategoryId = coldDrinks.Id
    },
    new MenuItems
    {
        Emertimi = "Freppuccino Caramel",
        Pershkrimi = "Frozen blended caramel coffee drink.",
        Cmimi = 2.40m,
        Foto = "mulliri/FreppuccinoCaramel.png",
        Disponueshme = true,
        Alergjene = "Milk",
        Kalori = 230,
        CategoryId = coldDrinks.Id
    },
    new MenuItems
    {
        Emertimi = "Chocolate Mochaccino",
        Pershkrimi = "Iced mochaccino with chocolate flavor.",
        Cmimi = 2.40m,
        Foto = "mulliri/MochaccinoÇokollatë.png",
        Disponueshme = true,
        Alergjene = "Milk",
        Kalori = 210,
        CategoryId = coldDrinks.Id
    },
    new MenuItems
    {
        Emertimi = "Caramel Mochaccino",
        Pershkrimi = "Iced mochaccino with caramel flavor.",
        Cmimi = 2.40m,
        Foto = "mulliri/MochaccinoCaramel.png",
        Disponueshme = true,
        Alergjene = "Milk",
        Kalori = 220,
        CategoryId = coldDrinks.Id
    },
    new MenuItems
    {
        Emertimi = "Caramel Peanut Latte",
        Pershkrimi = "Iced latte with caramel and peanut flavor.",
        Cmimi = 2.50m,
        Foto = "mulliri/CaramelPeanutLatte.png",
        Disponueshme = true,
        Alergjene = "Milk, Nuts (Peanuts)",
        Kalori = 240,
        CategoryId = coldDrinks.Id
    },
    new MenuItems
    {
        Emertimi = "Cold Chocolate",
        Pershkrimi = "Refreshing iced chocolate drink.",
        Cmimi = 1.90m,
        Foto = "mulliri/ÇokollatëeFtohtë.png",
        Disponueshme = true,
        Alergjene = "Milk",
        Kalori = 180,
        CategoryId = coldDrinks.Id
    },
    new MenuItems
    {
        Emertimi = "Cold Chocolate with Caramel",
        Pershkrimi = "Iced chocolate flavored with caramel.",
        Cmimi = 2.30m,
        Foto = "mulliri/ÇokollatëeFtohtëmeKaramel.png",
        Disponueshme = true,
        Alergjene = "Milk",
        Kalori = 200,
        CategoryId = coldDrinks.Id
    },
    new MenuItems
    {
        Emertimi = "Cold Chocolate with Coffee",
        Pershkrimi = "Iced chocolate blended with coffee.",
        Cmimi = 2.40m,
        Foto = "mulliri/ÇokollatëeFtohtëmeKafe.png",
        Disponueshme = true,
        Alergjene = "Milk",
        Kalori = 210,
        CategoryId = coldDrinks.Id
    },
    new MenuItems
    {
        Emertimi = "Iced Chai Latte",
        Pershkrimi = "Spiced tea latte served cold with milk.",
        Cmimi = 2.40m,
        Foto = "mulliri/IcedChaiLatte.png",
        Disponueshme = true,
        Alergjene = "Milk",
        Kalori = 170,
        CategoryId = coldDrinks.Id
    }
};

                context.MenuItems.AddRange(coldDrinksItems);
                context.SaveChanges();


                var hotChocolateCreamsItems = new List<MenuItems>
{
    new MenuItems
    {
        Emertimi = "Hot Chocolate",
        Pershkrimi = "Rich and creamy hot chocolate.",
        Cmimi = 2.10m,
        Foto = "mulliri/ÇokollatëeNgrohtë.png",
        Disponueshme = true,
        Alergjene = "Milk",
        Kalori = 200,
        CategoryId = hotChocolate.Id
    },
    new MenuItems
    {
        Emertimi = "White Hot Chocolate",
        Pershkrimi = "Smooth and sweet white hot chocolate.",
        Cmimi = 2.00m,
        Foto = "mulliri/ÇokollatëeBardhëENgrohtë.png",
        Disponueshme = true,
        Alergjene = "Milk",
        Kalori = 210,
        CategoryId = hotChocolate.Id
    },
    new MenuItems
    {
        Emertimi = "Cocoa",
        Pershkrimi = "Classic hot cocoa drink.",
        Cmimi = 1.90m,
        Foto = "mulliri/Kakao.png",
        Disponueshme = true,
        Alergjene = "Milk",
        Kalori = 180,
        CategoryId = hotChocolate.Id
    },
    new MenuItems
    {
        Emertimi = "Salep",
        Pershkrimi = "Traditional hot salep drink.",
        Cmimi = 1.80m,
        Foto = "mulliri/Salep.png",
        Disponueshme = true,
        Alergjene = "Milk",
        Kalori = 170,
        CategoryId = hotChocolate.Id
    }
};

                context.MenuItems.AddRange(hotChocolateCreamsItems);
                context.SaveChanges();

                var teasItems = new List<MenuItems>
{
    new MenuItems
    {
        Emertimi = "Mountain Tea",
        Pershkrimi = "Traditional herbal mountain tea.",
        Cmimi = 1.60m,
        Foto = "mulliri/QajMali.png",
        Disponueshme = true,
        Alergjene = "None",
        Kalori = 0,
        CategoryId = teas.Id
    },
    new MenuItems
    {
        Emertimi = "Chamomile Tea",
        Pershkrimi = "Relaxing chamomile herbal tea.",
        Cmimi = 1.50m,
        Foto = "mulliri/QajKamomil.png",
        Disponueshme = true,
        Alergjene = "None",
        Kalori = 0,
        CategoryId = teas.Id
    },
    new MenuItems
    {
        Emertimi = "Forest Fruit Tea",
        Pershkrimi = "Fruit tea blend with forest berries.",
        Cmimi = 1.50m,
        Foto = "mulliri/QajFrutaPylli.png",
        Disponueshme = true,
        Alergjene = "None",
        Kalori = 5,
        CategoryId = teas.Id
    },
    new MenuItems
    {
        Emertimi = "Hibiscus Tea",
        Pershkrimi = "Refreshing hibiscus herbal tea.",
        Cmimi = 1.50m,
        Foto = "mulliri/QajHibiskus.png",
        Disponueshme = true,
        Alergjene = "None",
        Kalori = 0,
        CategoryId = teas.Id
    },
    new MenuItems
    {
        Emertimi = "Ginger Tea",
        Pershkrimi = "Spicy and warming ginger tea.",
        Cmimi = 1.50m,
        Foto = "mulliri/QajXhinxher.png",
        Disponueshme = true,
        Alergjene = "None",
        Kalori = 0,
        CategoryId = teas.Id
    },
    new MenuItems
    {
        Emertimi = "Apple & Cinnamon Tea",
        Pershkrimi = "Fruit tea with apple and cinnamon flavor.",
        Cmimi = 1.50m,
        Foto = "mulliri/QajMolleKanell.png",
        Disponueshme = true,
        Alergjene = "None",
        Kalori = 5,
        CategoryId = teas.Id
    },
    new MenuItems
    {
        Emertimi = "Peach & Apricot Tea",
        Pershkrimi = "Fruit tea with peach and apricot flavor.",
        Cmimi = 1.50m,
        Foto = "mulliri/QajPjeshkeKajsi.png",
        Disponueshme = true,
        Alergjene = "None",
        Kalori = 5,
        CategoryId = teas.Id
    },
    new MenuItems
    {
        Emertimi = "Almond & Cinnamon Tea",
        Pershkrimi = "Unique blend of almond and cinnamon flavors.",
        Cmimi = 1.50m,
        Foto = "mulliri/QajBajameKanell.png",
        Disponueshme = true,
        Alergjene = "Nuts (Almonds)",
        Kalori = 10,
        CategoryId = teas.Id
    },
    new MenuItems
    {
        Emertimi = "Earl Grey Tea",
        Pershkrimi = "Classic black tea with bergamot flavor.",
        Cmimi = 1.50m,
        Foto = "mulliri/QajEarlGrey.png",
        Disponueshme = true,
        Alergjene = "None",
        Kalori = 0,
        CategoryId = teas.Id
    },
    new MenuItems
    {
        Emertimi = "Special Green Tea",
        Pershkrimi = "Premium green tea blend.",
        Cmimi = 1.50m,
        Foto = "mulliri/QajJeshilSpecial.png",
        Disponueshme = true,
        Alergjene = "None",
        Kalori = 0,
        CategoryId = teas.Id
    },
    new MenuItems
    {
        Emertimi = "Jasmine Green Tea",
        Pershkrimi = "Green tea infused with jasmine flowers.",
        Cmimi = 1.50m,
        Foto = "mulliri/QajJeshilJasemin.png",
        Disponueshme = true,
        Alergjene = "None",
        Kalori = 0,
        CategoryId = teas.Id
    }
};

                context.MenuItems.AddRange(teasItems);
                context.SaveChanges();


                var hotBeveragesItems = new List<MenuItems>
{
    new MenuItems
    {
        Emertimi = "Chai Latte",
        Pershkrimi = "Spiced tea latte made with black tea, milk, and aromatic spices.",
        Cmimi = 3.20m,
        Foto = "mulliri/chaiLatte.png",
        Disponueshme = true,
        Alergjene = "Milk",
        Kalori = 180,
        CategoryId = hotBeverages.Id
    }
};

                context.MenuItems.AddRange(hotBeveragesItems);
                context.SaveChanges();

                var juicesSmoothiesItems = new List<MenuItems>
{
    new MenuItems
    {
        Emertimi = "Orange Juice",
        Pershkrimi = "Freshly squeezed orange juice.",
        Cmimi = 2.80m,
        Foto = "mulliri/LengPortokalli.png",
        Disponueshme = true,
        Alergjene = "None",
        Kalori = 110,
        CategoryId = juicesSmoothies.Id
    },
    new MenuItems
    {
        Emertimi = "Orange & Carrot Juice",
        Pershkrimi = "Blend of orange and carrot juice.",
        Cmimi = 3.00m,
        Foto = "mulliri/LengPortokalliKarrote.png",
        Disponueshme = true,
        Alergjene = "None",
        Kalori = 120,
        CategoryId = juicesSmoothies.Id
    },
    new MenuItems
    {
        Emertimi = "Beetroot Juice",
        Pershkrimi = "Fresh beetroot juice.",
        Cmimi = 3.00m,
        Foto = "mulliri/LengPanxhari.png",
        Disponueshme = true,
        Alergjene = "None",
        Kalori = 100,
        CategoryId = juicesSmoothies.Id
    },
    new MenuItems
    {
        Emertimi = "Kiwi Juice",
        Pershkrimi = "Fresh kiwi juice.",
        Cmimi = 3.50m,
        Foto = "mulliri/LengKiwi.png",
        Disponueshme = true,
        Alergjene = "None",
        Kalori = 90,
        CategoryId = juicesSmoothies.Id
    },
    new MenuItems
    {
        Emertimi = "Strawberry Smoothie",
        Pershkrimi = "Smoothie made with fresh strawberries.",
        Cmimi = 3.50m,
        Foto = "mulliri/SmoothieStrawberry.png",
        Disponueshme = true,
        Alergjene = "Milk",
        Kalori = 180,
        CategoryId = juicesSmoothies.Id
    },
    new MenuItems
    {
        Emertimi = "Blackberry Smoothie",
        Pershkrimi = "Smoothie made with fresh blackberries.",
        Cmimi = 3.70m,
        Foto = "mulliri/SmoothieManaferre.png",
        Disponueshme = true,
        Alergjene = "Milk",
        Kalori = 190,
        CategoryId = juicesSmoothies.Id
    },
    new MenuItems
    {
        Emertimi = "Raspberry Smoothie",
        Pershkrimi = "Smoothie made with fresh raspberries.",
        Cmimi = 3.70m,
        Foto = "mulliri/SmoothieMjeder.png",
        Disponueshme = true,
        Alergjene = "Milk",
        Kalori = 185,
        CategoryId = juicesSmoothies.Id
    },
    new MenuItems
    {
        Emertimi = "Kiwi Smoothie",
        Pershkrimi = "Smoothie made with fresh kiwi.",
        Cmimi = 3.50m,
        Foto = "mulliri/SmoothieKiwi.png",
        Disponueshme = true,
        Alergjene = "Milk",
        Kalori = 170,
        CategoryId = juicesSmoothies.Id
    }
};

                context.MenuItems.AddRange(juicesSmoothiesItems);
                context.SaveChanges();

                var milkshakesItems = new List<MenuItems>
{
    new MenuItems
    {
        Emertimi = "Chocolate Milkshake",
        Pershkrimi = "Creamy milkshake with chocolate flavor.",
        Cmimi = 2.10m,
        Foto = "mulliri/MilkshakeQokollate.png",
        Disponueshme = true,
        Alergjene = "Milk",
        Kalori = 250,
        CategoryId = milkshakes.Id
    },
    new MenuItems
    {
        Emertimi = "Strawberry Milkshake",
        Pershkrimi = "Milkshake made with fresh strawberries.",
        Cmimi = 2.10m,
        Foto = "mulliri/MilkshakeLuleshtrydhe.png",
        Disponueshme = true,
        Alergjene = "Milk",
        Kalori = 240,
        CategoryId = milkshakes.Id
    },
    new MenuItems
    {
        Emertimi = "Caramel Milkshake",
        Pershkrimi = "Milkshake flavored with caramel syrup.",
        Cmimi = 2.10m,
        Foto = "mulliri/MilkshakeKaramel.png",
        Disponueshme = true,
        Alergjene = "Milk",
        Kalori = 260,
        CategoryId = milkshakes.Id
    },
    new MenuItems
    {
        Emertimi = "Orange Milkshake",
        Pershkrimi = "Refreshing milkshake with orange flavor.",
        Cmimi = 2.10m,
        Foto = "mulliri/MilkshakePortokall.png",
        Disponueshme = true,
        Alergjene = "Milk",
        Kalori = 230,
        CategoryId = milkshakes.Id
    },
    new MenuItems
    {
        Emertimi = "Passion Fruit Milkshake",
        Pershkrimi = "Exotic milkshake with passion fruit flavor.",
        Cmimi = 2.10m,
        Foto = "mulliri/MilkshakeFrutPasioni.png",
        Disponueshme = true,
        Alergjene = "Milk",
        Kalori = 240,
        CategoryId = milkshakes.Id
    },
    new MenuItems
    {
        Emertimi = "Lemon Milkshake",
        Pershkrimi = "Refreshing milkshake with lemon flavor.",
        Cmimi = 2.10m,
        Foto = "mulliri/MilkshakeLimon.png",
        Disponueshme = true,
        Alergjene = "Milk",
        Kalori = 220,
        CategoryId = milkshakes.Id
    }
};

                context.MenuItems.AddRange(milkshakesItems);
                context.SaveChanges();

                var granitaMocktailsItems = new List<MenuItems>
{
    new MenuItems
    {
        Emertimi = "Raspberry Granita",
        Pershkrimi = "Frozen raspberry granita, refreshing and fruity.",
        Cmimi = 2.00m,
        Foto = "mulliri/raspberryGranita.png",
        Disponueshme = true,
        Alergjene = "None",
        Kalori = 120,
        CategoryId = granitaMocktails.Id
    },
    new MenuItems
    {
        Emertimi = "Mango&Lime Granita",
        Pershkrimi = "Frozen mango granita, tropical and sweet.",
        Cmimi = 2.00m,
        Foto = "mulliri/GranitaMangoLime.png",
        Disponueshme = true,
        Alergjene = "None",
        Kalori = 130,
        CategoryId = granitaMocktails.Id
    },
    new MenuItems
    {
        Emertimi = "Pineapple Soda",
        Pershkrimi = "Refreshing soda with pineapple flavor.",
        Cmimi = 2.40m,
        Foto = "mulliri/AnanasSoda.png",
        Disponueshme = true,
        Alergjene = "None",
        Kalori = 100,
        CategoryId = granitaMocktails.Id
    },
    new MenuItems
    {
        Emertimi = "Grenadine Soda",
        Pershkrimi = "Sparkling soda flavored with grenadine syrup.",
        Cmimi = 2.40m,
        Foto = "mulliri/GrenadineSoda.png",
        Disponueshme = true,
        Alergjene = "None",
        Kalori = 110,
        CategoryId = granitaMocktails.Id
    },
    new MenuItems
    {
        Emertimi = "Orange Spritz",
        Pershkrimi = "Sparkling orange spritz mocktail.",
        Cmimi = 2.30m,
        Foto = "mulliri/OrangeSpritz.png",
        Disponueshme = true,
        Alergjene = "None",
        Kalori = 90,
        CategoryId = granitaMocktails.Id
    }
};

                context.MenuItems.AddRange(granitaMocktailsItems);
                context.SaveChanges();

                var waterItems = new List<MenuItems>
{
    new MenuItems
    {
        Emertimi = "Rugove Glass Water 0.25L",
        Pershkrimi = "Premium still water in glass bottle.",
        Cmimi = 1.20m,
        Foto = "mulliri/UjeRugoveQelq.png",
        Disponueshme = true,
        Alergjene = "None",
        Kalori = 0,
        CategoryId = water.Id
    },
    new MenuItems
    {
        Emertimi = "Rugove Glass Water 0.75L",
        Pershkrimi = "Premium still water in large glass bottle.",
        Cmimi = 2.00m,
        Foto = "mulliri/UjeRugoveQelq075l.png",
        Disponueshme = true,
        Alergjene = "None",
        Kalori = 0,
        CategoryId = water.Id
    },
    new MenuItems
    {
        Emertimi = "Rugove Plastic Water 0.5L",
        Pershkrimi = "Still water in plastic bottle.",
        Cmimi = 1.00m,
        Foto = "mulliri/UjeRugovePlastike.png",
        Disponueshme = true,
        Alergjene = "None",
        Kalori = 0,
        CategoryId = water.Id
    },
    new MenuItems
    {
        Emertimi = "Rugove Plastic Water 0.33L",
        Pershkrimi = "Still water in small plastic bottle.",
        Cmimi = 0.80m,
        Foto = "mulliri/UjeRugovePlastike033l.png",
        Disponueshme = true,
        Alergjene = "None",
        Kalori = 0,
        CategoryId = water.Id
    },
    new MenuItems
    {
        Emertimi = "Lajthiza Water 0.5L",
        Pershkrimi = "Still water from Lajthiza source.",
        Cmimi = 1.00m,
        Foto = "mulliri/UjeLajthiza.png",
        Disponueshme = true,
        Alergjene = "None",
        Kalori = 0,
        CategoryId = water.Id
    },
    new MenuItems
    {
        Emertimi = "Lajthiza Sparkling Water 0.33L",
        Pershkrimi = "Carbonated water from Lajthiza source.",
        Cmimi = 1.00m,
        Foto = "mulliri/UjeLajthizaMeGaz.png",
        Disponueshme = true,
        Alergjene = "None",
        Kalori = 0,
        CategoryId = water.Id
    },
    new MenuItems
    {
        Emertimi = "Pana Water 0.25L",
        Pershkrimi = "Premium bottled water from Pana.",
        Cmimi = 1.50m,
        Foto = "mulliri/UjePana.png",
        Disponueshme = true,
        Alergjene = "None",
        Kalori = 0,
        CategoryId = water.Id
    },
    new MenuItems
    {
        Emertimi = "Sanpellegrino Water 0.25L",
        Pershkrimi = "Italian sparkling mineral water.",
        Cmimi = 1.50m,
        Foto = "mulliri/UjeSanpelegrino.png",
        Disponueshme = true,
        Alergjene = "None",
        Kalori = 0,
        CategoryId = water.Id
    },
    new MenuItems
    {
        Emertimi = "Killkoti Water 0.5L",
        Pershkrimi = "Still bottled water from Killkoti.",
        Cmimi = 1.00m,
        Foto = "mulliri/UjeKllokoti.png",
        Disponueshme = true,
        Alergjene = "None",
        Kalori = 0,
        CategoryId = water.Id
    },
    new MenuItems
    {
        Emertimi = "Radenska Water 0.5L",
        Pershkrimi = "Sparkling mineral water Radenska.",
        Cmimi = 1.40m,
        Foto = "mulliri/UjeRadenska.png",
        Disponueshme = true,
        Alergjene = "None",
        Kalori = 0,
        CategoryId = water.Id
    }
};

                context.MenuItems.AddRange(waterItems);
                context.SaveChanges();

                var softDrinksItems = new List<MenuItems>
{
    new MenuItems
    {
        Emertimi = "Coca Cola",
        Pershkrimi = "Classic Coca Cola soft drink.",
        Cmimi = 1.50m,
        Foto = "mulliri/cocaCola.png",
        Disponueshme = true,
        Alergjene = "None",
        Kalori = 140,
        CategoryId = softDrinks.Id
    },
    new MenuItems
    {
        Emertimi = "Coca Cola Zero",
        Pershkrimi = "Sugar-free Coca Cola Zero.",
        Cmimi = 1.50m,
        Foto = "mulliri/cocaColaZero.png",
        Disponueshme = true,
        Alergjene = "None",
        Kalori = 0,
        CategoryId = softDrinks.Id
    },
    new MenuItems
    {
        Emertimi = "Fanta Orange",
        Pershkrimi = "Refreshing orange flavored soda.",
        Cmimi = 1.50m,
        Foto = "mulliri/Fanta.png",
        Disponueshme = true,
        Alergjene = "None",
        Kalori = 150,
        CategoryId = softDrinks.Id
    },
    new MenuItems
    {
        Emertimi = "Fanta Exotic",
        Pershkrimi = "Tropical exotic flavored soda.",
        Cmimi = 1.50m,
        Foto = "mulliri/FantaExotic.png",
        Disponueshme = true,
        Alergjene = "None",
        Kalori = 150,
        CategoryId = softDrinks.Id
    },
    new MenuItems
    {
        Emertimi = "Sprite",
        Pershkrimi = "Lemon-lime flavored soda.",
        Cmimi = 1.50m,
        Foto = "mulliri/Sprite.png",
        Disponueshme = true,
        Alergjene = "None",
        Kalori = 140,
        CategoryId = softDrinks.Id
    },
    new MenuItems
    {
        Emertimi = "Fructal",
        Pershkrimi = "Fruit-based refreshing drink.",
        Cmimi = 1.80m,
        Foto = "mulliri/Fructal.png",
        Disponueshme = true,
        Alergjene = "None",
        Kalori = 120,
        CategoryId = softDrinks.Id
    },
    new MenuItems
    {
        Emertimi = "Lemon Soda",
        Pershkrimi = "Sparkling soda with lemon flavor.",
        Cmimi = 1.80m,
        Foto = "mulliri/LemonSoda.png",
        Disponueshme = true,
        Alergjene = "None",
        Kalori = 100,
        CategoryId = softDrinks.Id
    },
    new MenuItems
    {
        Emertimi = "Orange Soda",
        Pershkrimi = "Sparkling soda with orange flavor.",
        Cmimi = 1.80m,
        Foto = "mulliri/OrangeSoda.png",
        Disponueshme = true,
        Alergjene = "None",
        Kalori = 110,
        CategoryId = softDrinks.Id
    },
    new MenuItems
    {
        Emertimi = "Mohito Soda",
        Pershkrimi = "Refreshing soda with mojito flavor.",
        Cmimi = 1.80m,
        Foto = "mulliri/MohitoSoda.png",
        Disponueshme = true,
        Alergjene = "None",
        Kalori = 90,
        CategoryId = softDrinks.Id
    },
    new MenuItems
    {
        Emertimi = "Schweppes Tonic Water",
        Pershkrimi = "Classic tonic water.",
        Cmimi = 1.80m,
        Foto = "mulliri/SchweppesTonic.png",
        Disponueshme = true,
        Alergjene = "None",
        Kalori = 0,
        CategoryId = softDrinks.Id
    },
    new MenuItems
    {
        Emertimi = "Schweppes Bitter Lemon",
        Pershkrimi = "Sparkling soda with bitter lemon flavor.",
        Cmimi = 1.80m,
        Foto = "mulliri/SchweppesBitterLemon.png",
        Disponueshme = true,
        Alergjene = "None",
        Kalori = 90,
        CategoryId = softDrinks.Id
    },
    new MenuItems
    {
        Emertimi = "Ice Tea",
        Pershkrimi = "Refreshing iced tea drink.",
        Cmimi = 1.80m,
        Foto = "mulliri/IceTea.png",
        Disponueshme = true,
        Alergjene = "None",
        Kalori = 80,
        CategoryId = softDrinks.Id
    },
    new MenuItems
    {
        Emertimi = "Red Bull",
        Pershkrimi = "Energy drink Red Bull.",
        Cmimi = 2.60m,
        Foto = "mulliri/RedBull.png",
        Disponueshme = true,
        Alergjene = "None",
        Kalori = 110,
        CategoryId = softDrinks.Id
    },
    new MenuItems
    {
        Emertimi = "Rose Lemonade",
        Pershkrimi = "Premium rose flavored lemonade.",
        Cmimi = 3.80m,
        Foto = "mulliri/RoseLemonade.png",
        Disponueshme = true,
        Alergjene = "None",
        Kalori = 120,
        CategoryId = softDrinks.Id
    },
    new MenuItems
    {
        Emertimi = "Golden Eagle",
        Pershkrimi = "Local energy drink Golden Eagle.",
        Cmimi = 1.70m,
        Foto = "mulliri/GoldenEagle.png",
        Disponueshme = true,
        Alergjene = "None",
        Kalori = 120,
        CategoryId = softDrinks.Id
    }
};

                context.MenuItems.AddRange(softDrinksItems);
                context.SaveChanges();


                var sandwichesItems = new List<MenuItems>
{
    new MenuItems
    {
        Emertimi = "Savory Croissant with Turkey & Gouda",
        Pershkrimi = "Buttery croissant filled with turkey and Gouda cheese.",
        Cmimi = 2.60m,
        Foto = "mulliri/KruasaniKripur.png",
        Disponueshme = true,
        Alergjene = "Gluten, Milk",
        Kalori = 320,
        CategoryId = sandwiches.Id
    },
    new MenuItems
    {
        Emertimi = "Chicken Ham & Gouda Toast",
        Pershkrimi = "Grilled toast with chicken ham and Gouda cheese.",
        Cmimi = 2.10m,
        Foto = "mulliri/TostProshutë.png",
        Disponueshme = true,
        Alergjene = "Gluten, Milk",
        Kalori = 280,
        CategoryId = sandwiches.Id
    },
    new MenuItems
    {
        Emertimi = "Integrale Milano",
        Pershkrimi = "Whole grain Milano-style sandwich.",
        Cmimi = 2.60m,
        Foto = "mulliri/IntegralMilano.png",
        Disponueshme = true,
        Alergjene = "Gluten",
        Kalori = 300,
        CategoryId = sandwiches.Id
    },
    new MenuItems
    {
        Emertimi = "Pretzel with Beef Salami & Gouda",
        Pershkrimi = "Pretzel sandwich filled with beef salami and Gouda cheese.",
        Cmimi = 2.90m,
        Foto = "mulliri/PretzelSallamViqi.png",
        Disponueshme = true,
        Alergjene = "Gluten, Milk",
        Kalori = 350,
        CategoryId = sandwiches.Id
    },
    new MenuItems
    {
        Emertimi = "Chicken Breast & Kaçkavall Panini",
        Pershkrimi = "Panini filled with grilled chicken breast and Kaçkavall cheese.",
        Cmimi = 2.90m,
        Foto = "mulliri/PanineGjoksPule.png",
        Disponueshme = true,
        Alergjene = "Gluten, Milk",
        Kalori = 360,
        CategoryId = sandwiches.Id
    },
    new MenuItems
    {
        Emertimi = "Chicken Ham & Potato Panini",
        Pershkrimi = "Panini with chicken ham and potato filling.",
        Cmimi = 2.60m,
        Foto = "mulliri/PaninemeProshutePule.png",
        Disponueshme = true,
        Alergjene = "Gluten, Milk",
        Kalori = 340,
        CategoryId = sandwiches.Id
    }
};

                context.MenuItems.AddRange(sandwichesItems);
                context.SaveChanges();

                var croissantsItems = new List<MenuItems>
{
    new MenuItems
    {
        Emertimi = "Pistachio Cream Croissant",
        Pershkrimi = "Flaky croissant filled with rich pistachio cream.",
        Cmimi = 1.80m,
        Foto = "mulliri/KruasanKremFëstëk.png",
        Disponueshme = true,
        Alergjene = "Gluten, Milk, Nuts (Pistachio)",
        Kalori = 320,
        CategoryId = croissants.Id
    },
    new MenuItems
    {
        Emertimi = "Hazelnut Chocolate Cream Croissant",
        Pershkrimi = "Buttery croissant filled with hazelnut chocolate cream.",
        Cmimi = 1.60m,
        Foto = "mulliri/KruasanKremQokollateLajthi.png",
        Disponueshme = true,
        Alergjene = "Gluten, Milk, Nuts (Hazelnut)",
        Kalori = 310,
        CategoryId = croissants.Id
    }
};

                context.MenuItems.AddRange(croissantsItems);
                context.SaveChanges();

                var pancakesMuffinsItems = new List<MenuItems>
{
    new MenuItems
    {
        Emertimi = "Chocolate Filled Donut",
        Pershkrimi = "Soft donut filled with rich chocolate cream.",
        Cmimi = 1.60m,
        Foto = "mulliri/PetulleMbushurmeÇokollat.png",
        Disponueshme = true,
        Alergjene = "Gluten, Milk",
        Kalori = 320,
        CategoryId = pancakesMuffins.Id
    },
    new MenuItems
    {
        Emertimi = "Chocolate Coated Donut",
        Pershkrimi = "Donut coated with chocolate glaze.",
        Cmimi = 1.60m,
        Foto = "mulliri/PetulleVeshurmeÇokollat.png",
        Disponueshme = true,
        Alergjene = "Gluten, Milk",
        Kalori = 310,
        CategoryId = pancakesMuffins.Id
    },
    new MenuItems
    {
        Emertimi = "Pistachio Filled Donut",
        Pershkrimi = "Donut filled with pistachio cream.",
        Cmimi = 2.00m,
        Foto = "mulliri/PetulleMbushurmeFestek.png",
        Disponueshme = true,
        Alergjene = "Gluten, Milk, Nuts (Pistachio)",
        Kalori = 330,
        CategoryId = pancakesMuffins.Id
    },
    new MenuItems
    {
        Emertimi = "Pistachio Coated Donut",
        Pershkrimi = "Donut coated with pistachio cream.",
        Cmimi = 1.90m,
        Foto = "mulliri/PetulleVeshurmeFestek.png",
        Disponueshme = true,
        Alergjene = "Gluten, Milk, Nuts (Pistachio)",
        Kalori = 325,
        CategoryId = pancakesMuffins.Id
    },
    new MenuItems
    {
        Emertimi = "Chocolate Muffin",
        Pershkrimi = "Moist muffin with chocolate flavor.",
        Cmimi = 1.70m,
        Foto = "mulliri/MuffinÇokollat.png",
        Disponueshme = true,
        Alergjene = "Gluten, Milk, Eggs",
        Kalori = 280,
        CategoryId = pancakesMuffins.Id
    },
    new MenuItems
    {
        Emertimi = "Pistachio Muffin",
        Pershkrimi = "Muffin flavored with pistachio.",
        Cmimi = 2.10m,
        Foto = "mulliri/MuffinFestek.png",
        Disponueshme = true,
        Alergjene = "Gluten, Milk, Eggs, Nuts (Pistachio)",
        Kalori = 300,
        CategoryId = pancakesMuffins.Id
    },
    new MenuItems
    {
        Emertimi = "Red Velvet Muffin",
        Pershkrimi = "Classic red velvet muffin with a soft texture.",
        Cmimi = 1.70m,
        Foto = "mulliri/MuffinRedVelvet.png",
        Disponueshme = true,
        Alergjene = "Gluten, Milk, Eggs",
        Kalori = 290,
        CategoryId = pancakesMuffins.Id
    },
    new MenuItems
    {
        Emertimi = "Raisin Muffin",
        Pershkrimi = "Muffin with dried raisins.",
        Cmimi = 1.50m,
        Foto = "mulliri/MuffinRrushiThat.png",
        Disponueshme = true,
        Alergjene = "Gluten, Milk, Eggs",
        Kalori = 270,
        CategoryId = pancakesMuffins.Id
    },
    new MenuItems
    {
        Emertimi = "Blueberry Muffin",
        Pershkrimi = "Muffin with fresh blueberries.",
        Cmimi = 1.70m,
        Foto = "mulliri/MuffinBoronic.png",
        Disponueshme = true,
        Alergjene = "Gluten, Milk, Eggs",
        Kalori = 280,
        CategoryId = pancakesMuffins.Id
    }
};

                context.MenuItems.AddRange(pancakesMuffinsItems);
                context.SaveChanges();

                var dessertsItems = new List<MenuItems>
{
    new MenuItems
    {
        Emertimi = "Capriccio Tart",
        Pershkrimi = "Delicate tart topped with mixed fruits.",
        Cmimi = 2.00m,
        Foto = "mulliri/TarteCapriccio.png",
        Disponueshme = true,
        Alergjene = "Gluten, Milk, Eggs",
        Kalori = 280,
        CategoryId = desserts.Id
    },
    new MenuItems
    {
        Emertimi = "Fig Tart",
        Pershkrimi = "Tart filled with fig cream and decorated icing.",
        Cmimi = 2.00m,
        Foto = "mulliri/TarteFiku.png",
        Disponueshme = true,
        Alergjene = "Gluten, Milk, Eggs",
        Kalori = 290,
        CategoryId = desserts.Id
    },
    new MenuItems
    {
        Emertimi = "Carrot Cake",
        Pershkrimi = "Moist carrot cake slice with cream layers.",
        Cmimi = 2.90m,
        Foto = "mulliri/TorteMeKarrote.png",
        Disponueshme = true,
        Alergjene = "Gluten, Milk, Eggs, Nuts",
        Kalori = 350,
        CategoryId = desserts.Id
    },
    new MenuItems
    {
        Emertimi = "Mango Cheesecake",
        Pershkrimi = "Cheesecake topped with mango sauce.",
        Cmimi = 3.20m,
        Foto = "mulliri/CheesecakeMango.png",
        Disponueshme = true,
        Alergjene = "Gluten, Milk, Eggs",
        Kalori = 370,
        CategoryId = desserts.Id
    },
    new MenuItems
    {
        Emertimi = "Blueberry Cheesecake",
        Pershkrimi = "Cheesecake topped with blueberry sauce.",
        Cmimi = 3.20m,
        Foto = "mulliri/CheesecakeBoronic.png",
        Disponueshme = true,
        Alergjene = "Gluten, Milk, Eggs",
        Kalori = 365,
        CategoryId = desserts.Id
    },
    new MenuItems
    {
        Emertimi = "Brownie Mousse",
        Pershkrimi = "Chocolate mousse with brownie pieces.",
        Cmimi = 2.70m,
        Foto = "mulliri/MousseBrownie.png",
        Disponueshme = true,
        Alergjene = "Milk, Eggs",
        Kalori = 330,
        CategoryId = desserts.Id
    },
    new MenuItems
    {
        Emertimi = "Biscoff Mousse",
        Pershkrimi = "Creamy mousse topped with Biscoff spread.",
        Cmimi = 2.70m,
        Foto = "mulliri/MousseBiscoff.png",
        Disponueshme = true,
        Alergjene = "Milk, Eggs, Gluten",
        Kalori = 340,
        CategoryId = desserts.Id
    },
    new MenuItems
    {
        Emertimi = "Hazelnut Profiterole",
        Pershkrimi = "Profiterole filled with hazelnut cream.",
        Cmimi = 2.20m,
        Foto = "mulliri/ProfiterolLajthi.png",
        Disponueshme = true,
        Alergjene = "Gluten, Milk, Nuts (Hazelnut)",
        Kalori = 300,
        CategoryId = desserts.Id
    },
    new MenuItems
    {
        Emertimi = "Pistachio Profiterole",
        Pershkrimi = "Profiterole filled with pistachio cream.",
        Cmimi = 2.20m,
        Foto = "mulliri/ProfiterolFestek.png",
        Disponueshme = true,
        Alergjene = "Gluten, Milk, Nuts (Pistachio)",
        Kalori = 305,
        CategoryId = desserts.Id
    },
    new MenuItems
    {
        Emertimi = "Banana Cake",
        Pershkrimi = "Moist banana cake slice.",
        Cmimi = 4.20m,
        Foto = "mulliri/TortemeBanane.png",
        Disponueshme = true,
        Alergjene = "Gluten, Milk, Eggs",
        Kalori = 360,
        CategoryId = desserts.Id
    }
};

                context.MenuItems.AddRange(dessertsItems);
                context.SaveChanges();


                var rollsBiscuitsItems = new List<MenuItems>
{
    new MenuItems
    {
        Emertimi = "Fitbar",
        Pershkrimi = "Healthy oat and fruit energy bar.",
        Cmimi = 1.50m,
        Foto = "mulliri/Fitbar.png",
        Disponueshme = true,
        Alergjene = "Gluten",
        Kalori = 180,
        CategoryId = rollsBiscuits.Id
    },
    new MenuItems
    {
        Emertimi = "Crunchy Oats & Date Roll",
        Pershkrimi = "Crunchy roll made with oats and Arabian dates.",
        Cmimi = 1.40m,
        Foto = "mulliri/KrokanteTershere.png",
        Disponueshme = true,
        Alergjene = "Gluten",
        Kalori = 200,
        CategoryId = rollsBiscuits.Id
    },
    new MenuItems
    {
        Emertimi = "Caramel Roll",
        Pershkrimi = "Sweet roll filled with caramel cream.",
        Cmimi = 2.80m,
        Foto = "mulliri/KaramelRolls.png",
        Disponueshme = true,
        Alergjene = "Gluten, Milk",
        Kalori = 320,
        CategoryId = rollsBiscuits.Id
    },
    new MenuItems
    {
        Emertimi = "Cinnamon Apple Roll",
        Pershkrimi = "Roll filled with apple and cinnamon.",
        Cmimi = 2.80m,
        Foto = "mulliri/CinnamonAppleRoll.png",
        Disponueshme = true,
        Alergjene = "Gluten",
        Kalori = 310,
        CategoryId = rollsBiscuits.Id
    },
    new MenuItems
    {
        Emertimi = "Pistachio Roll",
        Pershkrimi = "Sweet roll filled with pistachio cream.",
        Cmimi = 3.20m,
        Foto = "mulliri/PistachioRoll.png",
        Disponueshme = true,
        Alergjene = "Gluten, Nuts (Pistachio), Milk",
        Kalori = 330,
        CategoryId = rollsBiscuits.Id
    },
    new MenuItems
    {
        Emertimi = "Oat & Raisin Biscuit",
        Pershkrimi = "Crunchy biscuit made with oats and raisins.",
        Cmimi = 1.20m,
        Foto = "mulliri/BiskotemeTershere.png",
        Disponueshme = true,
        Alergjene = "Gluten",
        Kalori = 150,
        CategoryId = rollsBiscuits.Id
    }
};

                context.MenuItems.AddRange(rollsBiscuitsItems);
                context.SaveChanges();


                var capsulesItems = new List<MenuItems>
{
    new MenuItems
    {
        Emertimi = "Mulliri Espresso Oro (10 Nespresso Original)",
        Pershkrimi = "Smooth and balanced espresso blend Oro.",
        Cmimi = 3.90m,
        Foto = "mulliri/EspressoOro10.png",
        Disponueshme = true,
        Alergjene = "None",
        Kalori = 0,
        CategoryId = capsules.Id
    },
    new MenuItems
    {
        Emertimi = "Mulliri Espresso Colombia (10 Nespresso Original)",
        Pershkrimi = "Rich espresso with Colombian origin.",
        Cmimi = 4.30m,
        Foto = "mulliri/EspressoColombia10.png",
        Disponueshme = true,
        Alergjene = "None",
        Kalori = 0,
        CategoryId = capsules.Id
    },
    new MenuItems
    {
        Emertimi = "Mulliri Espresso Brazil (10 Nespresso Original)",
        Pershkrimi = "Smooth espresso with Brazilian origin.",
        Cmimi = 3.90m,
        Foto = "mulliri/EspressoBrazil10.png",
        Disponueshme = true,
        Alergjene = "None",
        Kalori = 0,
        CategoryId = capsules.Id
    },
    new MenuItems
    {
        Emertimi = "Mulliri Espresso Ethiopia (10 Nespresso Original)",
        Pershkrimi = "Floral and aromatic Ethiopian espresso.",
        Cmimi = 4.30m,
        Foto = "mulliri/EspressoEthiopia10.png",
        Disponueshme = true,
        Alergjene = "None",
        Kalori = 0,
        CategoryId = capsules.Id
    },
    new MenuItems
    {
        Emertimi = "Mulliri Espresso India (10 Nespresso Original)",
        Pershkrimi = "Strong and spicy Indian espresso.",
        Cmimi = 3.90m,
        Foto = "mulliri/EspressoIndia10.png",
        Disponueshme = true,
        Alergjene = "None",
        Kalori = 0,
        CategoryId = capsules.Id
    },
    new MenuItems
    {
        Emertimi = "Mulliri Espresso Decaf (10 Nespresso Original)",
        Pershkrimi = "Decaffeinated espresso blend.",
        Cmimi = 4.80m,
        Foto = "mulliri/EspressoDekaf10.png",
        Disponueshme = true,
        Alergjene = "None",
        Kalori = 0,
        CategoryId = capsules.Id
    },
    new MenuItems
    {
        Emertimi = "Mulliri Espresso Oro (50 Cialde)",
        Pershkrimi = "Large pack of Oro espresso pods.",
        Cmimi = 15.50m,
        Foto = "mulliri/EspressoOro50.png",
        Disponueshme = true,
        Alergjene = "None",
        Kalori = 0,
        CategoryId = capsules.Id
    },
    new MenuItems
    {
        Emertimi = "Mulliri Espresso Brazil (18 Cialde)",
        Pershkrimi = "Pack of 18 Brazilian espresso pods.",
        Cmimi = 8.00m,
        Foto = "mulliri/EspressoBrazil18.png",
        Disponueshme = true,
        Alergjene = "None",
        Kalori = 0,
        CategoryId = capsules.Id
    },
    new MenuItems
    {
        Emertimi = "Mulliri Espresso India (18 Cialde)",
        Pershkrimi = "Pack of 18 Indian espresso pods.",
        Cmimi = 8.00m,
        Foto = "mulliri/EspressoIndia18.png",
        Disponueshme = true,
        Alergjene = "None",
        Kalori = 0,
        CategoryId = capsules.Id
    }
};

                context.MenuItems.AddRange(capsulesItems);
                context.SaveChanges();



                var turkishCoffeeItems = new List<MenuItems>
{
    new MenuItems
    {
        Emertimi = "Mulliri Turkish Coffee 100g",
        Pershkrimi = "Traditional ground Turkish coffee, rich and aromatic.",
        Cmimi = 1.80m,
        Foto = "mulliri/MulliriKafeTurke.png",
        Disponueshme = true,
        Alergjene = "None",
        Kalori = 0,
        CategoryId = turkishCoffee.Id
    }
};

                context.MenuItems.AddRange(turkishCoffeeItems);
                context.SaveChanges();


                var coffeeBeansItems = new List<MenuItems>
{
    new MenuItems
    {
        Emertimi = "Mulliri Espresso Professional Barista 1000g",
        Pershkrimi = "Premium espresso beans for professional barista use.",
        Cmimi = 24.00m,
        Foto = "mulliri/ProfessionalBarista.png",
        Disponueshme = true,
        Alergjene = "None",
        Kalori = 0,
        CategoryId = coffeeBeans.Id
    },
    new MenuItems
    {
        Emertimi = "Mulliri Espresso Crema Bar 1000g",
        Pershkrimi = "Espresso beans with smooth crema.",
        Cmimi = 17.00m,
        Foto = "mulliri/CremaBar.png",
        Disponueshme = true,
        Alergjene = "None",
        Kalori = 0,
        CategoryId = coffeeBeans.Id
    },
    new MenuItems
    {
        Emertimi = "Mulliri Espresso Crema Bar Supremo 1000g",
        Pershkrimi = "Supreme blend of espresso beans with rich crema.",
        Cmimi = 18.50m,
        Foto = "mulliri/CremaBarSupremo.png",
        Disponueshme = true,
        Alergjene = "None",
        Kalori = 0,
        CategoryId = coffeeBeans.Id
    },
    new MenuItems
    {
        Emertimi = "Mulliri Espresso Miscela Premium 1000g",
        Pershkrimi = "Premium blend of espresso beans for refined taste.",
        Cmimi = 40.00m,
        Foto = "mulliri/MiscelaPremium.png",
        Disponueshme = true,
        Alergjene = "None",
        Kalori = 0,
        CategoryId = coffeeBeans.Id
    }
};
                context.MenuItems.AddRange(coffeeBeansItems);
                context.SaveChanges();

                var groundCoffeeItems = new List<MenuItems>
{
    new MenuItems
    {
        Emertimi = "Mulliri Moka Intenso 250g",
        Pershkrimi = "Intense ground coffee blend for moka pot.",
        Cmimi = 5.00m,
        Foto = "mulliri/MulliriMokaIntenso.png",
        Disponueshme = true,
        Alergjene = "None",
        Kalori = 0,
        CategoryId = groundCoffee.Id
    }
};

                context.MenuItems.AddRange(groundCoffeeItems);
                context.SaveChanges();

                var packagedTeasItems = new List<MenuItems>
{
    new MenuItems
    {
        Emertimi = "Ginger Tea",
        Pershkrimi = "Packaged herbal tea with ginger flavor.",
        Cmimi = 6.00m,
        Foto = "mulliri/Xhinxher.png",
        Disponueshme = true,
        Alergjene = "None",
        Kalori = 0,
        CategoryId = packagedTeas.Id
    },
    new MenuItems
    {
        Emertimi = "Apple & Cinnamon Tea",
        Pershkrimi = "Packaged tea with apple and cinnamon flavor.",
        Cmimi = 6.00m,
        Foto = "mulliri/MolleKanell.png",
        Disponueshme = true,
        Alergjene = "None",
        Kalori = 0,
        CategoryId = packagedTeas.Id
    },
    new MenuItems
    {
        Emertimi = "Almond & Cinnamon Tea",
        Pershkrimi = "Packaged tea with almond and cinnamon flavor.",
        Cmimi = 6.00m,
        Foto = "mulliri/BajameKanell.png",
        Disponueshme = true,
        Alergjene = "Nuts (Almond)",
        Kalori = 0,
        CategoryId = packagedTeas.Id
    },
    new MenuItems
    {
        Emertimi = "Earl Grey Tea",
        Pershkrimi = "Classic Earl Grey packaged tea.",
        Cmimi = 6.00m,
        Foto = "mulliri/EarlGrey.png",
        Disponueshme = true,
        Alergjene = "None",
        Kalori = 0,
        CategoryId = packagedTeas.Id
    },
    new MenuItems
    {
        Emertimi = "Green Gunpowder Tea",
        Pershkrimi = "Traditional green gunpowder packaged tea.",
        Cmimi = 6.00m,
        Foto = "mulliri/JeshilGunpowder.png",
        Disponueshme = true,
        Alergjene = "None",
        Kalori = 0,
        CategoryId = packagedTeas.Id
    },
    new MenuItems
    {
        Emertimi = "Green Jasmine Tea",
        Pershkrimi = "Green tea blended with jasmine flowers.",
        Cmimi = 6.00m,
        Foto = "mulliri/JeshilmeJasemin.png",
        Disponueshme = true,
        Alergjene = "None",
        Kalori = 0,
        CategoryId = packagedTeas.Id
    }
};
                context.MenuItems.AddRange(packagedTeasItems);
                context.SaveChanges();


                var cocoaSalepItems = new List<MenuItems>
{
    new MenuItems
    {
        Emertimi = "Mulliri Cocoa 100g",
        Pershkrimi = "Rich cocoa powder, 100g pack.",
        Cmimi = 2.50m,
        Foto = "mulliri/MulliriKakao.png",
        Disponueshme = true,
        Alergjene = "None",
        Kalori = 0,
        CategoryId = cocoaSalep.Id
    },
    new MenuItems
    {
        Emertimi = "Mulliri Salep",
        Pershkrimi = "Rich cocoa powder",
        Cmimi = 4.50m,
        Foto = "mulliri/MulliriSalep.png",
        Disponueshme = true,
        Alergjene = "None",
        Kalori = 0,
        CategoryId = cocoaSalep.Id
    }
};

                context.MenuItems.AddRange(cocoaSalepItems);
                context.SaveChanges();

                var sweetSnacksItems = new List<MenuItems>
{
    new MenuItems
    {
        Emertimi = "Campbells Shortbread Fingers 100g",
        Pershkrimi = "Classic buttery shortbread fingers, 100g pack.",
        Cmimi = 2.70m,
        Foto = "mulliri/Campbells.png",
        Disponueshme = true,
        Alergjene = "Gluten, Milk",
        Kalori = 480,
        CategoryId = sweetSnacks.Id
    },
    new MenuItems
    {
        Emertimi = "Caramel Stroopwafel Piece",
        Pershkrimi = "Single caramel stroopwafel with syrup filling.",
        Cmimi = 1.00m,
        Foto = "mulliri/CaramelStroopwafel.png",
        Disponueshme = true,
        Alergjene = "Gluten, Milk, Eggs",
        Kalori = 150,
        CategoryId = sweetSnacks.Id
    },
    new MenuItems
    {
        Emertimi = "Mini Caramel Stroopwafels",
        Pershkrimi = "Pack of mini stroopwafels with caramel filling.",
        Cmimi = 4.10m,
        Foto = "mulliri/MiniCaramelStroopwafels.png",
        Disponueshme = true,
        Alergjene = "Gluten, Milk, Eggs",
        Kalori = 600,
        CategoryId = sweetSnacks.Id
    }
};

                context.MenuItems.AddRange(sweetSnacksItems);
                context.SaveChanges();

                var grandmasRecipesItems = new List<MenuItems>
{
    new MenuItems
    {
        Emertimi = "Cherry Juice",
        Pershkrimi = "Refreshing bottled cherry juice.",
        Cmimi = 1.90m,
        Foto = "mulliri/LengVishnje.png",
        Disponueshme = true,
        Alergjene = "None",
        Kalori = 120,
        CategoryId = grandmasRecipes.Id
    },
    new MenuItems
    {
        Emertimi = "Orange Juice",
        Pershkrimi = "Fresh bottled orange juice.",
        Cmimi = 1.90m,
        Foto = "mulliri/LengPortokalli.png",
        Disponueshme = true,
        Alergjene = "None",
        Kalori = 110,
        CategoryId = grandmasRecipes.Id
    },
    new MenuItems
    {
        Emertimi = "Peach Juice",
        Pershkrimi = "Sweet bottled peach juice.",
        Cmimi = 1.90m,
        Foto = "mulliri/LengPjeshke.png",
        Disponueshme = true,
        Alergjene = "None",
        Kalori = 115,
        CategoryId = grandmasRecipes.Id
    },
    new MenuItems
    {
        Emertimi = "Multivitamin Juice",
        Pershkrimi = "Mixed fruit multivitamin juice.",
        Cmimi = 1.90m,
        Foto = "mulliri/LengMultivitamin.png",
        Disponueshme = true,
        Alergjene = "None",
        Kalori = 130,
        CategoryId = grandmasRecipes.Id
    },
    new MenuItems
    {
        Emertimi = "Apple Juice",
        Pershkrimi = "Classic bottled apple juice.",
        Cmimi = 1.90m,
        Foto = "mulliri/LengMolle.png",
        Disponueshme = true,
        Alergjene = "None",
        Kalori = 115,
        CategoryId = grandmasRecipes.Id
    },
    new MenuItems
    {
        Emertimi = "Forest Fruit Juice",
        Pershkrimi = "Juice blend of forest fruits.",
        Cmimi = 1.90m,
        Foto = "mulliri/LengFrutaMali.png",
        Disponueshme = true,
        Alergjene = "None",
        Kalori = 125,
        CategoryId = grandmasRecipes.Id
    },
    new MenuItems
    {
        Emertimi = "Strawberry Juice",
        Pershkrimi = "Sweet bottled strawberry juice.",
        Cmimi = 1.90m,
        Foto = "mulliri/LengDredheze.png",
        Disponueshme = true,
        Alergjene = "None",
        Kalori = 120,
        CategoryId = grandmasRecipes.Id
    },
    new MenuItems
    {
        Emertimi = "Pear Juice",
        Pershkrimi = "Refreshing bottled pear juice.",
        Cmimi = 1.90m,
        Foto = "mulliri/LengDardhe.png",
        Disponueshme = true,
        Alergjene = "None",
        Kalori = 115,
        CategoryId = grandmasRecipes.Id
    },
    new MenuItems
    {
        Emertimi = "Blueberry Juice",
        Pershkrimi = "Juice made from fresh blueberries.",
        Cmimi = 1.90m,
        Foto = "mulliri/LengBoronice.png",
        Disponueshme = true,
        Alergjene = "None",
        Kalori = 125,
        CategoryId = grandmasRecipes.Id
    }
};

                context.MenuItems.AddRange(grandmasRecipesItems);
                context.SaveChanges();



            }


            var categoriesGjicksandChicks = new List<MenuCategory>();
            var gjiks = context.Restaurants.FirstOrDefault(r => r.Emertimi == "Gjiks&Chiks");

            if (gjiks != null)
            {

                var appetizer = new MenuCategory
                {

                    Emertimi = "Appetizer",
                    Pershkrimi = "Tasty starters perfect for sharing, featuring a variety of crispy, savory, and flavorful bites to begin your meal",
                    Renditja = 1,
                    RestaurantId = gjiks.Id
                };

                var salad = new MenuCategory
                {

                    Emertimi = "Salad",
                    Pershkrimi = "Fresh and vibrant salads made with crisp ingredients and light, flavorful dressings.",
                    Renditja = 2,
                    RestaurantId = gjiks.Id
                };

                var rice = new MenuCategory
                {

                    Emertimi = "Rice",
                    Pershkrimi = "Fresh and vibrant salads made with crisp ingredients and light, flavorful dressings.",
                    Renditja = 3,
                    RestaurantId = gjiks.Id
                };
                var nigiri = new MenuCategory
                {

                    Emertimi = "Nigiri Sushi",
                    Pershkrimi = "Hand-pressed sushi rice topped with fresh slices of fish or seafood, simple and delicate in flavor.",
                    Renditja = 4,
                    RestaurantId = gjiks.Id
                };

                var sushi = new MenuCategory
                {

                    Emertimi = "Sushi Roll",
                    Pershkrimi = "Freshly rolled sushi filled with a variety of ingredients, combining flavors and textures in every bite.",
                    Renditja = 5,
                    RestaurantId = gjiks.Id
                };
                var sides = new MenuCategory
                {

                    Emertimi = "Sides",
                    Pershkrimi = "Perfect complements to your meal, featuring a selection of tasty and satisfying additions.",
                    Renditja = 6,
                    RestaurantId = gjiks.Id
                };
                var taco = new MenuCategory
                {

                    Emertimi = "Taco & Wraps",
                    Pershkrimi = "Soft tortillas filled with flavorful ingredients, fresh toppings, and delicious sauces for a satisfying bite.",
                    Renditja = 7,
                    RestaurantId = gjiks.Id
                };
                var linguini = new MenuCategory
                {

                    Emertimi = "Linguini",
                    Pershkrimi = "Delicate long pasta served with rich sauces and flavorful ingredients for a classic Italian taste.",
                    Renditja = 8,
                    RestaurantId = gjiks.Id
                };
                var noodles = new MenuCategory
                {

                    Emertimi = "Noodles",
                    Pershkrimi = "Stir-fried or sauced noodles prepared with flavorful ingredients and savory seasonings.",
                    Renditja = 9,
                    RestaurantId = gjiks.Id
                };
                var drinks = new MenuCategory
                {

                    Emertimi = "Drinks",
                    Pershkrimi = "Drinks",
                    Renditja = 10,
                    RestaurantId = gjiks.Id
                };
                categoriesGjicksandChicks.AddRange(new[] { appetizer, salad, rice, nigiri, sushi, sides, taco, linguini, noodles, drinks });
                context.MenuCategories.AddRange(categoriesGjicksandChicks);
                context.SaveChanges();

                var appetizerItems = new List<MenuItems>
                {

                new MenuItems
                {
                   Emertimi = "Chicken Katsu",
                   Pershkrimi = "Crispy breaded chicken cutlet served with a light savory sauce, offering a crunchy outside and tender inside.",
                   Cmimi = 6.50m,
                   Foto = "gjiks/1.png",
                   Disponueshme = true,
                   Alergjene = "Gluten, Eggs,Soy",
                   Kalori = 680,
                   CategoryId = appetizer.Id
                },
                new MenuItems
                {
                   Emertimi = "Tom Yum Soup",
                   Pershkrimi = "A hot and sour Thai soup with fragrant herbs, mushrooms, and a rich, tangy broth.",
                   Cmimi = 6.20m,
                   Foto = "gjiks/2.png",
                   Disponueshme = true,
                   Alergjene = "Fish,Shellfish,Soy",
                   Kalori = 200,
                   CategoryId = appetizer.Id
                },
                new MenuItems
                {
                   Emertimi = "Shrimp Tempura",
                   Pershkrimi = "Light and crispy battered shrimp, fried to golden perfection and served with dipping sauce.",
                   Cmimi = 8.00m,
                   Foto = "gjiks/3.png",
                   Disponueshme = true,
                   Alergjene = "Fish,Shellfish,Soy",
                   Kalori = 300,
                   CategoryId = appetizer.Id
                },
                new MenuItems
                {
                   Emertimi = "Kadaif Shrimp",
                   Pershkrimi = "Crispy shredded kadaif wrapped shrimp, fried until golden and served with a light dipping sauce.",
                   Cmimi = 7.50m,
                   Foto = "gjiks/4.png",
                   Disponueshme = true,
                   Alergjene = "Fish,Gluten,Shellfish,Soy",
                   Kalori = 350,
                   CategoryId = appetizer.Id
                },
                new MenuItems
                {
                   Emertimi = "Korean Crispy Chicken",
                   Pershkrimi = "Crispy fried chicken tossed in a flavorful Korean-style sauce, sweet, savory, and slightly spicy.",
                   Cmimi = 5.90m,
                   Foto = "gjiks/5.png",
                   Disponueshme = true,
                   Alergjene = "Gluten,Eggs,Soy,Sesame",
                   Kalori = 600,
                   CategoryId = appetizer.Id
                },
                new MenuItems
                {
                   Emertimi = "Gyoza",
                   Pershkrimi = "Pan-fried dumplings filled with seasoned meat and vegetables, served with a light dipping sauce.",
                   Cmimi = 4.00m,
                   Foto = "gjiks/6.png",
                   Disponueshme = true,
                   Alergjene = "Gluten,Soy,Sesame",
                   Kalori = 250,
                   CategoryId = appetizer.Id
                },
                new MenuItems
                {
                   Emertimi = "Spring Rolls",
                   Pershkrimi = "Crispy rolls filled with vegetables and light seasoning, served golden and crunchy.",
                   Cmimi = 5.00m,
                   Foto = "gjiks/7.png",
                   Disponueshme = true,
                   Alergjene = "Gluten,Soy",
                   Kalori = 250,
                   CategoryId = appetizer.Id
                },
                new MenuItems
                {
                   Emertimi = "Tempura Calamari",
                   Pershkrimi = "Lightly battered and crispy fried calamari, served with a savory dipping sauce.",
                   Cmimi = 13.50m,
                   Foto = "gjiks/8.png",
                   Disponueshme = true,
                   Alergjene = "Shellfish,Gluten,Soy,Eggs",
                   Kalori = 300,
                   CategoryId = appetizer.Id
                }
                };
                context.MenuItems.AddRange(appetizerItems);
                context.SaveChanges();

                var saladItems = new List<MenuItems>
                {
                new MenuItems
                {
                   Emertimi = "Caesar Salad",
                   Pershkrimi = "Crisp romaine lettuce with parmesan cheese, croutons, and creamy Caesar dressing.",
                   Cmimi = 6.90m,
                   Foto = "gjiks/s1.png",
                   Disponueshme = true,
                   Alergjene = "Fish,Gluten,Soy,Eggs,Milk",
                   Kalori = 350,
                   CategoryId = salad.Id

                },
                new MenuItems
                {
                   Emertimi = "Crisp Garden Fete",
                   Pershkrimi = "Fresh mixed greens with crisp vegetables, herbs, and a light refreshing dressing",
                   Cmimi = 8.50m,
                   Foto = "gjiks/s2.png",
                   Disponueshme = true,
                   Alergjene = "Soy, Mustard",
                   Kalori = 200,
                   CategoryId = salad.Id

                },
                new MenuItems
                {
                   Emertimi = "Wakame Salad",
                   Pershkrimi = "Fresh seaweed salad seasoned with sesame dressing for a light and refreshing taste.",
                   Cmimi = 7.50m,
                   Foto = "gjiks/s3.png",
                   Disponueshme = true,
                   Alergjene = "Soy, Sesame",
                   Kalori = 200,
                   CategoryId = salad.Id

                }
            };
                context.MenuItems.AddRange(saladItems);
                context.SaveChanges();

                var riceItems = new List<MenuItems>
            {
            new MenuItems
            {
                   Emertimi = "Beef Rice Bowl",
                   Pershkrimi = "Tender sliced beef served over steamed rice with savory sauce and fresh garnishes.",
                   Cmimi = 7.50m,
                   Foto = "gjiks/r1.png",
                   Disponueshme = true,
                   Alergjene = "Soy, Sesame,Gluten",
                   Kalori = 650,
                   CategoryId = rice.Id

            },
            new MenuItems
            {
                   Emertimi = "Chicken Rice Bowl",
                   Pershkrimi = "Juicy chicken served over steamed rice with savory sauce and fresh toppings.",
                   Cmimi = 5.90m,
                   Foto = "gjiks/r2.png",
                   Disponueshme = true,
                   Alergjene = "Soy, Sesame,Gluten",
                   Kalori = 650,
                   CategoryId = rice.Id

            },
            new MenuItems
            {
                   Emertimi = "Shrimp Rice Bowl",
                   Pershkrimi = "Tender shrimp served over steamed rice with savory sauce and fresh garnishes.",
                   Cmimi = 7.90m,
                   Foto = "gjiks/r3.png",
                   Disponueshme = true,
                   Alergjene = "Soy, Sesame,Gluten,Shellfish",
                   Kalori = 550,
                   CategoryId = rice.Id

            },
            new MenuItems
            {
                   Emertimi = "Salmon Rice Bowls",
                   Pershkrimi = "Fresh salmon served over steamed rice with savory sauce and fresh toppings.",
                   Cmimi = 7.90m,
                   Foto = "gjiks/r44.png",
                   Disponueshme = true,
                   Alergjene = "Soy, Sesame,Gluten,Fish",
                   Kalori = 600,
                   CategoryId = rice.Id

            }

            };
                context.MenuItems.AddRange(riceItems);
                context.SaveChanges();


                var nigiriItems = new List<MenuItems>()
                {
                new MenuItems
                {
                   Emertimi = "Nigiri Salmon 4 pcs",
                   Pershkrimi = "Fresh slice of salmon served over seasoned sushi rice for a simple and delicate bite.",
                   Cmimi = 11.90m,
                   Foto = "gjiks/n11.png",
                   Disponueshme = true,
                   Alergjene = "Fish",
                   Kalori = 200,
                   CategoryId = nigiri.Id
                },

                new MenuItems
                {
                   Emertimi = "Sushi Mix Nigiri 16 pcs",
                   Pershkrimi = "A selection of fresh nigiri with assorted fish and seafood over seasoned sushi rice.",
                   Cmimi = 23.50m,
                   Foto = "gjiks/n22.png",
                   Disponueshme = true,
                   Alergjene = "Shellfish,Fish",
                   Kalori = 350,
                   CategoryId = nigiri.Id

                }

                };
                context.MenuItems.AddRange(nigiriItems);
                context.SaveChanges();

                var sushiItems = new List<MenuItems>
                {
                new MenuItems
                {
                  Emertimi = "California Roll",
                   Pershkrimi = "Sushi roll with crab, avocado, cucumber, and rice wrapped in seaweed.",
                   Cmimi = 9.90m,
                   Foto = "gjiks/sushi1.png",
                   Disponueshme = true,
                   Alergjene = "Crustaceans,Gluten,Soy,Sesame",
                   Kalori = 250,
                   CategoryId = sushi.Id
                },
                new MenuItems
                {
                  Emertimi = "Tempura Roll",
                   Pershkrimi = "Crispy tempura-fried shrimp or vegetables rolled with rice, seaweed, and savory sauce.",
                   Cmimi = 8.90m,
                   Foto = "gjiks/sushi2.png",
                   Disponueshme = true,
                   Alergjene = "Shellfish,Gluten,Soy,Sesame,Eggs",
                   Kalori = 400,
                   CategoryId = sushi.Id
                },
                new MenuItems
                {
                  Emertimi = "Philadelphia Roll",
                   Pershkrimi = "Sushi roll with salmon, cream cheese, and cucumber for a creamy and fresh flavor.",
                   Cmimi = 9.00m,
                   Foto = "gjiks/sushi3.png",
                   Disponueshme = true,
                   Alergjene = "Fish,Gluten,Soy,Sesame",
                   Kalori = 300,
                   CategoryId = sushi.Id
                },
                new MenuItems
                {
                  Emertimi = "Fried California Roll",
                   Pershkrimi = "Crispy deep-fried California roll with crab, avocado, and cucumber, served warm with savory sauce.",
                   Cmimi = 10.90m,
                   Foto = "gjiks/sushi4.png",
                   Disponueshme = true,
                   Alergjene = "Crustaceans,Fish,Gluten,Soy,Sesame",
                   Kalori = 450,
                   CategoryId = sushi.Id
                },
                new MenuItems
                {
                  Emertimi = "Cheese Salmon Roll",
                   Pershkrimi = "Fresh salmon and creamy cheese rolled with rice and seaweed for a rich and smooth taste.",
                   Cmimi = 12.50m,
                   Foto = "gjiks/sushi5.png",
                   Disponueshme = true,
                   Alergjene = "Milk,Fish,Gluten,Soy,Sesame",
                   Kalori = 350,
                   CategoryId = sushi.Id
                },
                new MenuItems
                {
                  Emertimi = "Cheese Salmon Roll",
                   Pershkrimi = "Fresh salmon and creamy cheese rolled with rice and seaweed for a rich and smooth taste.",
                   Cmimi = 12.50m,
                   Foto = "gjiks/sushi55.png",
                   Disponueshme = true,
                   Alergjene = "Milk,Fish,Gluten,Soy,Sesame",
                   Kalori = 350,
                   CategoryId = sushi.Id
                },
                new MenuItems
                {
                  Emertimi = "Beef Roll",
                   Pershkrimi = "Sushi roll filled with tender beef, rice, and fresh vegetables wrapped in seaweed.",
                   Cmimi = 14.50m,
                   Foto = "gjiks/sushi6.png",
                   Disponueshme = true,
                   Alergjene = "Gluten,Soy,Sesame",
                   Kalori = 350,
                   CategoryId = sushi.Id
                },
                new MenuItems
                {
                  Emertimi = "Tempura Mango",
                   Pershkrimi = "Crispy tempura-battered mango, lightly fried and served with a sweet sauce for a warm, crunchy-sweet bite.",
                   Cmimi = 13.50m,
                   Foto = "gjiks/sushi7.png",
                   Disponueshme = true,
                   Alergjene = "Gluten,Soy,Eggs",
                   Kalori = 250,
                   CategoryId = sushi.Id
                }
                };
                context.MenuItems.AddRange(sushiItems);
                context.SaveChanges();

                var sidesItems = new List<MenuItems>
                {
                new MenuItems
                {
                Emertimi = "Edamame",
                   Pershkrimi = "Steamed young soybeans lightly salted for a simple and healthy side dish.",
                   Cmimi = 4.00m,
                   Foto = "gjiks/sides1.png",
                   Disponueshme = true,
                   Alergjene = "Soy",
                   Kalori = 150,
                   CategoryId = sides.Id
                },
                new MenuItems
                {
                Emertimi = "Fried Mushrooms",
                   Pershkrimi = "Crispy golden fried mushrooms, lightly breaded and served with a savory dipping sauce.",
                   Cmimi = 4.50m,
                   Foto = "gjiks/sides22.png",
                   Disponueshme = true,
                   Alergjene = "Soy,Gluten,Soy",
                   Kalori = 300,
                   CategoryId = sides.Id
                },
                new MenuItems
                {
                Emertimi = "Mac'n'cheese",
                   Pershkrimi = "Creamy baked macaroni pasta with rich cheese sauce, topped for a golden finish.",
                   Cmimi = 3.90m,
                   Foto = "gjiks/side3.png",
                   Disponueshme = true,
                   Alergjene = "Soy,Gluten,Soy",
                   Kalori = 600,
                   CategoryId = sides.Id
                }
                };
                context.MenuItems.AddRange(sidesItems);
                context.SaveChanges();

                var tacoItems = new List<MenuItems>
                {
                new MenuItems
                {
                    Emertimi = "Chicken Taco",
                    Pershkrimi = "Soft tortilla filled with seasoned chicken, fresh toppings, and savory sauce for a delicious bite.",
                    Cmimi = 6.50m,
                    Foto = "gjiks/taco2.png",
                    Disponueshme = true,
                    Alergjene = "Gluten,Soy.Milk,Mustard",
                    Kalori = 300,
                    CategoryId = taco.Id
                },
                new MenuItems
                {
                    Emertimi = "Chicken Tortilla",
                    Pershkrimi = "Soft tortilla filled with seasoned chicken, fresh vegetables, and creamy sauce..",
                    Cmimi = 4.50m,
                    Foto = "gjiks/taco1.png",
                    Disponueshme = true,
                    Alergjene = "Gluten,Soy,Milk,Mustard",
                    Kalori = 450,
                    CategoryId = taco.Id
                },
                new MenuItems
                {
                    Emertimi = "Chicken Taco x3 Tacos",
                    Pershkrimi = "Three soft tortillas filled with seasoned chicken, fresh vegetables, and flavorful sauce.",
                    Cmimi = 6.50m,
                    Foto = "gjiks/taco3.png",
                    Disponueshme = true,
                    Alergjene = "Gluten,Soy,Milk,Mustard",
                    Kalori = 500,
                    CategoryId = taco.Id
                }
                };
                context.MenuItems.AddRange(tacoItems);
                context.SaveChanges();

                var linguiniItems = new List<MenuItems>

                {
                new MenuItems
                {
                    Emertimi = "Chicken Linguini",
                    Pershkrimi = "Tender chicken served with linguini pasta in a rich, flavorful sauce.",
                    Cmimi = 7.20m,
                    Foto = "gjiks/l1.png",
                    Disponueshme = true,
                    Alergjene = "Gluten, Nuts,Milk,Soy",
                    Kalori = 650,
                    CategoryId = linguini.Id
                },
                new MenuItems
                {
                    Emertimi = "Shrimp Linguini",
                    Pershkrimi = "Tender shrimp served with linguini pasta in a rich, savory sauce.",
                    Cmimi = 9.40m,
                    Foto = "gjiks/l2.png",
                    Disponueshme = true,
                    Alergjene = "Gluten,Shellfish,Milk,Soy",
                    Kalori = 650,
                    CategoryId = linguini.Id
                },
                new MenuItems
                {
                    Emertimi = "Beef Linguini",
                    Pershkrimi = "Tender beef served with linguini pasta in a rich, savory sauce.",
                    Cmimi = 8.90m,
                    Foto = "gjiks/l3.png",
                    Disponueshme = true,
                    Alergjene = "Gluten,Eggs,Milk,Soy",
                    Kalori = 700,
                    CategoryId = linguini.Id
                }
                };
                context.MenuItems.AddRange(linguiniItems);
                context.SaveChanges();

                var noodlesItems = new List<MenuItems>
                {
                new MenuItems{
                    Emertimi = "Fly Noodles Veggie",
                    Pershkrimi = "Stir-fried noodles with fresh mixed vegetables, tossed in a savory light sauce.",
                    Cmimi = 4.90m,
                    Foto = "gjiks/nn1.png",
                    Disponueshme = true,
                    Alergjene = "Gluten,Sesame,Soy",
                    Kalori = 450,
                    CategoryId = noodles.Id
                },
                new MenuItems{
                    Emertimi = "Fly Noodles Chicken",
                    Pershkrimi = "Stir-fried noodles with tender chicken and fresh vegetables in a savory sauce.",
                    Cmimi = 6.90m,
                    Foto = "gjiks/nn2.png",
                    Disponueshme = true,
                    Alergjene = "Gluten,Sesame,Soy",
                    Kalori = 550,
                    CategoryId = noodles.Id
                },
                new MenuItems{
                    Emertimi = "Fly Noodles Beef",
                    Pershkrimi = "Stir-fried noodles with tender beef and fresh vegetables in a savory sauce.",
                    Cmimi = 6.90m,
                    Foto = "gjiks/nn3.png",
                    Disponueshme = true,
                    Alergjene = "Gluten,Sesame,Soy",
                    Kalori = 650,
                    CategoryId = noodles.Id
                },
                new MenuItems{
                    Emertimi = "Fly Noodles Shrimp",
                    Pershkrimi = "Stir-fried noodles with juicy shrimp and fresh vegetables in a savory sauce.",
                    Cmimi = 8.90m,
                    Foto = "gjiks/nn4.png",
                    Disponueshme = true,
                    Alergjene = "Shellfish,Gluten,Sesame,Soy",
                    Kalori = 600,
                    CategoryId = noodles.Id
                }
                };
                context.MenuItems.AddRange(noodlesItems);
                context.SaveChanges();

                var drinksItems = new List<MenuItems>
                {
                new MenuItems{
                Emertimi = " Coca Cola",
                    Pershkrimi = "Refreshing Coca Cola beverage.",
                    Cmimi = 1.50m,
                    Foto = "pastafasta/CocaCola.png",
                    Disponueshme = true,
                    Alergjene = "None",
                    Kalori = 140,
                    CategoryId = drinks.Id
                },
                new MenuItems{
                Emertimi = " Fanta Orange",
                    Pershkrimi = "Refreshing Fanta Orange beverage.",
                    Cmimi = 1.50m,
                    Foto = "pastafasta/FantaOrange.png",
                    Disponueshme = true,
                    Alergjene = "None",
                    Kalori = 140,
                    CategoryId = drinks.Id
                },
                new MenuItems{
                Emertimi = " Sprite",
                    Pershkrimi = "Refreshing Sprite beverage.",
                    Cmimi = 1.50m,
                    Foto = "pastafasta/Sprite.png",
                    Disponueshme = true,
                    Alergjene = "None",
                    Kalori = 140,
                    CategoryId = drinks.Id
                },
                new MenuItems{
                Emertimi = " Water",
                    Pershkrimi = "Refreshing Water beverage.",
                    Cmimi = 1.50m,
                    Foto = "pastafasta/Water.png",
                    Disponueshme = true,
                    Alergjene = "None",
                    Kalori = 0,
                    CategoryId = drinks.Id
                }


                };
                context.MenuItems.AddRange(drinksItems);
                context.SaveChanges();
            }

            var categoriesSmashBurgerCo = new List<MenuCategory>();
            var smashBurger = context.Restaurants.FirstOrDefault(r => r.Emertimi == "Smash Burger CO");

            if (smashBurger != null)
            {
                var burgers = new MenuCategory
                {
                    Emertimi = "Burgers",
                    Pershkrimi = "Juicy flame-grilled burgers.",
                    Renditja = 1,
                    RestaurantId = smashBurger.Id
                };

                var drinks = new MenuCategory
                {
                    Emertimi = "Drinks",
                    Pershkrimi = "Refreshing soft drinks and water.",
                    Renditja = 2,
                    RestaurantId = smashBurger.Id
                };

                categoriesSmashBurgerCo.AddRange(new[] { burgers, drinks });
                context.MenuCategories.AddRange(categoriesSmashBurgerCo);
                context.SaveChanges();


                var burgersItems = new List<MenuItems>
{
    new MenuItems
    {
        Emertimi = "Cheese Burger",
        Pershkrimi = "Beef patty, cheese, pickles, lettuce, tomato, smash sauce",
        Cmimi = 4.25m,
        Foto = "smashburger/cheeseBurger.png",
        Disponueshme = true,
        Alergjene = "Gluten, Milk",
        Kalori = 650,
        CategoryId = burgers.Id
    },
    new MenuItems
    {
        Emertimi = "Double Cheese Burger",
        Pershkrimi = "Two beef patties, cheese, pickles, lettuce, tomato, smash sauce",
        Cmimi = 5.50m,
        Foto = "smashburger/doubleCheeseBurger.png",
        Disponueshme = true,
        Alergjene = "Gluten, Milk",
        Kalori = 950,
        CategoryId = burgers.Id
    },
    new MenuItems
    {
        Emertimi = "Mexican 🌶️",
        Pershkrimi = "Beef patty, cheese, jalapeño, nachos, lettuce, tomato, hot smash sauce",
        Cmimi = 4.50m,
        Foto = "smashburger/mexicanBurger.png",
        Disponueshme = true,
        Alergjene = "Gluten, Milk",
        Kalori = 700,
        CategoryId = burgers.Id
    },
    new MenuItems
    {
        Emertimi = "Double Mexican 🌶️",
        Pershkrimi = "Two beef patties, cheese, jalapeño, nachos, lettuce, tomato, hot smash sauce",
        Cmimi = 5.75m,
        Foto = "smashburger/doubleMexicanBurger.png",
        Disponueshme = true,
        Alergjene = "Gluten, Milk",
        Kalori = 1000,
        CategoryId = burgers.Id
    },
    new MenuItems
    {
        Emertimi = "New Yorker",
        Pershkrimi = "Beef patty, cheese, pickles & beef slices, lettuce, tomato, smash sauce",
        Cmimi = 4.88m,
        Foto = "smashburger/newYorker.png",
        Disponueshme = true,
        Alergjene = "Gluten, Milk",
        Kalori = 720,
        CategoryId = burgers.Id
    },
    new MenuItems
    {
        Emertimi = "Double New Yorker",
        Pershkrimi = "Two beef patties, cheese, pickles & beef slices, lettuce, tomato, smash sauce",
        Cmimi = 6.13m,
        Foto = "smashburger/doubleNewYorker.png",
        Disponueshme = true,
        Alergjene = "Gluten, Milk",
        Kalori = 1050,
        CategoryId = burgers.Id
    },
    new MenuItems
    {
        Emertimi = "Chicken Strip Burger",
        Pershkrimi = "Chicken strips, cheese, pickles, lettuce, smash sauce",
        Cmimi = 4.25m,
        Foto = "smashburger/chickenStripBurger.png",
        Disponueshme = true,
        Alergjene = "Gluten, Milk",
        Kalori = 600,
        CategoryId = burgers.Id
    },
    new MenuItems
    {
        Emertimi = "Nashville Strip Burger",
        Pershkrimi = "Spicy chicken strips, cheese, jalapeño, lettuce, hot smash sauce, barbecue",
        Cmimi = 4.63m,
        Foto = "smashburger/nashvilleStripBurger.png",
        Disponueshme = true,
        Alergjene = "Gluten, Milk",
        Kalori = 650,
        CategoryId = burgers.Id
    }
};

                context.MenuItems.AddRange(burgersItems);
                context.SaveChanges();


                var drinksItems = new List<MenuItems>
{
    new MenuItems
    {
        Emertimi = "Still Water",
        Pershkrimi = "Refreshing still water bottle.",
        Cmimi = 1.00m,
        Foto = "smashburger/waterStill.png",
        Disponueshme = true,
        Alergjene = "None",
        Kalori = 0,
        CategoryId = drinks.Id
    },
    new MenuItems
    {
        Emertimi = "Sparkling Water",
        Pershkrimi = "Sparkling water bottle.",
        Cmimi = 1.00m,
        Foto = "smashburger/waterStill.png",
        Disponueshme = true,
        Alergjene = "None",
        Kalori = 0,
        CategoryId = drinks.Id
    },
    new MenuItems
    {
        Emertimi = "Coca Cola",
        Pershkrimi = "Classic refreshing cola.",
        Cmimi = 1.50m,
        Foto = "smashburger/cocacola.png",
        Disponueshme = true,
        Alergjene = "None",
        Kalori = 140,
        CategoryId = drinks.Id
    },
    new MenuItems
    {
        Emertimi = "Coca Cola Zero",
        Pershkrimi = "Sugar-free cola.",
        Cmimi = 1.50m,
        Foto = "smashburger/cocacolazero.png",
        Disponueshme = true,
        Alergjene = "None",
        Kalori = 0,
        CategoryId = drinks.Id
    },
    new MenuItems
    {
        Emertimi = "Fanta",
        Pershkrimi = "Orange-flavored soft drink.",
        Cmimi = 1.50m,
        Foto = "smashburger/fanta.png",
        Disponueshme = true,
        Alergjene = "None",
        Kalori = 160,
        CategoryId = drinks.Id
    },
    new MenuItems
    {
        Emertimi = "Sprite",
        Pershkrimi = "Lemon-lime soft drink.",
        Cmimi = 1.50m,
        Foto = "smashburger/sprite.png",
        Disponueshme = true,
        Alergjene = "None",
        Kalori = 150,
        CategoryId = drinks.Id
    },
    new MenuItems
    {
        Emertimi = "Sola Ice Tea",
        Pershkrimi = "Refreshing iced tea.",
        Cmimi = 1.50m,
        Foto = "smashburger/solaicetea.png",
        Disponueshme = true,
        Alergjene = "None",
        Kalori = 80,
        CategoryId = drinks.Id
    },
    new MenuItems
    {
        Emertimi = "Beer",
        Pershkrimi = "Cold beer (18+).",
        Cmimi = 2.50m,
        Foto = "smashburger/beer.png",
        Disponueshme = true,
        Alergjene = "Gluten",
        Kalori = 200,
        CategoryId = drinks.Id
    }
};

                context.MenuItems.AddRange(drinksItems);
                context.SaveChanges();



            }

            var categoriesBuffaloBurgers = new List<MenuCategory>();
            var buffaloBurgers = context.Restaurants.FirstOrDefault(r => r.Emertimi == "Buffalo Burgers");

            if (buffaloBurgers != null)
            {
                var burgers = new MenuCategory
                {
                    Emertimi = "Burgers",
                    Pershkrimi = "Juicy flame-grilled burgers.",
                    Renditja = 1,
                    RestaurantId = buffaloBurgers.Id
                };

                var sandwiches = new MenuCategory
                {
                    Emertimi = "Sandwich",
                    Pershkrimi = "Freshly made sandwiches.",
                    Renditja = 2,
                    RestaurantId = buffaloBurgers.Id
                };

                var sides = new MenuCategory
                {
                    Emertimi = "Sides",
                    Pershkrimi = "Tasty side dishes.",
                    Renditja = 3,
                    RestaurantId = buffaloBurgers.Id
                };

                var mexican = new MenuCategory
                {
                    Emertimi = "Mexican",
                    Pershkrimi = "Mexican-inspired dishes.",
                    Renditja = 4,
                    RestaurantId = buffaloBurgers.Id
                };

                categoriesBuffaloBurgers.AddRange(new[] { burgers, sandwiches, sides, mexican });
                context.MenuCategories.AddRange(categoriesBuffaloBurgers);
                context.SaveChanges();

                burgers = context.MenuCategories.FirstOrDefault(c => c.Emertimi == "Burgers" && c.RestaurantId == buffaloBurgers.Id);
                sandwiches = context.MenuCategories.FirstOrDefault(c => c.Emertimi == "Sandwich" && c.RestaurantId == buffaloBurgers.Id);
                sides = context.MenuCategories.FirstOrDefault(c => c.Emertimi == "Sides" && c.RestaurantId == buffaloBurgers.Id);
                mexican = context.MenuCategories.FirstOrDefault(c => c.Emertimi == "Mexican" && c.RestaurantId == buffaloBurgers.Id);

                var burgersItems = new List<MenuItems>
    {
        new MenuItems
        {
            Emertimi = "El Chapo",
            Pershkrimi = "Double beef, soft bun, mayo, bbq prosciutto, triple cheddar cheese",
            Cmimi = 5.99m,
            Foto = "buffaloburgers/elchapo.png",
            Disponueshme = true,
            Alergjene = "Gluten, Milk",
            Kalori = 950,
            CategoryId = burgers.Id
        },
        new MenuItems
        {
            Emertimi = "Gustavo",
            Pershkrimi = "Beef, soft bun, mayo, ketchup, mustard, emmentaler cheese, grilled onions",
            Cmimi = 4.90m,
            Foto = "buffaloburgers/gustavo.png",
            Disponueshme = true,
            Alergjene = "Gluten, Milk",
            Kalori = 850,
            CategoryId = burgers.Id
        },
        new MenuItems
        {
            Emertimi = "Pablo",
            Pershkrimi = "Beef, soft bun, mayo, bbq prosciutto, double cheddar cheese, jalapenos",
            Cmimi = 5.20m,
            Foto = "buffaloburgers/pablo.png",
            Disponueshme = true,
            Alergjene = "Gluten, Milk",
            Kalori = 880,
            CategoryId = burgers.Id
        },
        new MenuItems
        {
            Emertimi = "American Classic",
            Pershkrimi = "Beef, soft bun, secret sauce, double cheddar cheese, lettuce, tomato, red onion",
            Cmimi = 4.90m,
            Foto = "buffaloburgers/americanclassic.png",
            Disponueshme = true,
            Alergjene = "Gluten, Milk",
            Kalori = 820,
            CategoryId = burgers.Id
        },
        new MenuItems
        {
            Emertimi = "Felix",
            Pershkrimi = "Beef, soft bun, mayo, ruccola, emmentaler cheese, grilled mushrooms",
            Cmimi = 4.90m,
            Foto = "buffaloburgers/felix.png",
            Disponueshme = true,
            Alergjene = "Gluten, Milk",
            Kalori = 800,
            CategoryId = burgers.Id
        },
        new MenuItems
        {
            Emertimi = "Chickano",
            Pershkrimi = "Chicken burger",
            Cmimi = 4.49m,
            Foto = "buffaloburgers/chickano.png",
            Disponueshme = true,
            Alergjene = "Gluten, Milk",
            Kalori = 750,
            CategoryId = burgers.Id
        }
    };

                context.MenuItems.AddRange(burgersItems);
                context.SaveChanges();

                var sandwichesItems = new List<MenuItems>
    {
        new MenuItems
        {
            Emertimi = "Sandwich Tony",
            Pershkrimi = "Minced beef, mayo beef, prosciutto, red oil sauce, parsley",
            Cmimi = 3.90m,
            Foto = "buffaloburgers/sandwichTony.png",
            Disponueshme = true,
            Alergjene = "Gluten, Milk",
            Kalori = 600,
            CategoryId = sandwiches.Id
        },
        new MenuItems
        {
            Emertimi = "Hot Dog",
            Pershkrimi = "Classic hot dog",
            Cmimi = 3.99m,
            Foto = "buffaloburgers/hotdog.png",
            Disponueshme = true,
            Alergjene = "Gluten",
            Kalori = 550,
            CategoryId = sandwiches.Id
        }
    };

                context.MenuItems.AddRange(sandwichesItems);
                context.SaveChanges();

                var sidesItems = new List<MenuItems>
    {
        new MenuItems
        {
            Emertimi = "Loaded Fries",
            Pershkrimi = "Fries topped with cheese and sauces",
            Cmimi = 3.80m,
            Foto = "buffaloburgers/loadedfries.png",
            Disponueshme = true,
            Alergjene = "Milk",
            Kalori = 500,
            CategoryId = sides.Id
        },
        new MenuItems
        {
            Emertimi = "Garlic Fries",
            Pershkrimi = "Fries with garlic and herbs",
            Cmimi = 2.70m,
            Foto = "buffaloburgers/garlicfries.png",
            Disponueshme = true,
            Alergjene = "None",
            Kalori = 450,
            CategoryId = sides.Id
        }
    };

                context.MenuItems.AddRange(sidesItems);
                context.SaveChanges();

                var mexicanItems = new List<MenuItems>
    {
        new MenuItems
        {
            Emertimi = "Matador (Birria Tacos)",
            Pershkrimi = "Beef birria tacos served with consome",
            Cmimi = 3.90m,
            Foto = "buffaloburgers/matador.png",
            Disponueshme = true,
            Alergjene = "Gluten",
            Kalori = 700,
            CategoryId = mexican.Id
        },
        new MenuItems
        {
            Emertimi = "Chickarita (Chicken Quesadilla)",
            Pershkrimi = "Chicken quesadilla with onions, jalapenos, secret sauce, sriracha sauce, cheese",
            Cmimi = 4.20m,
            Foto = "buffaloburgers/chickarita.png",
            Disponueshme = true,
            Alergjene = "Gluten, Milk",
            Kalori = 750,
            CategoryId = mexican.Id
        },
        new MenuItems
        {
            Emertimi = "Toro (Beef Quesadilla)",
            Pershkrimi = "Beef quesadilla with consome, cheese, secret sauce, onions, jalapenos, sriracha sauce",
            Cmimi = 4.20m,
            Foto = "buffaloburgers/toro.png",
            Disponueshme = true,
            Alergjene = "Gluten, Milk",
            Kalori = 780,
            CategoryId = mexican.Id
        }
    };

                context.MenuItems.AddRange(mexicanItems);
                context.SaveChanges();
            }

            var categoriesHookFishandChips = new List<MenuCategory>();
            var fishandchips = context.Restaurants.FirstOrDefault(r => r.Emertimi == "Hook Fish&Chips");

            if (fishandchips != null)
            {
                var offers = new MenuCategory
                {
                    Emertimi = "Offers",
                    Pershkrimi = "Special offers and combo deals.",
                    Renditja = 1,
                    RestaurantId = fishandchips.Id
                };

                var fishChips = new MenuCategory
                {
                    Emertimi = "Fish & Chips",
                    Pershkrimi = "Classic fish and chips dishes.",
                    Renditja = 2,
                    RestaurantId = fishandchips.Id
                };

                var extras = new MenuCategory
                {
                    Emertimi = "Extras",
                    Pershkrimi = "Tasty extras and sides.",
                    Renditja = 3,
                    RestaurantId = fishandchips.Id
                };

                var drinks = new MenuCategory
                {
                    Emertimi = "Drinks",
                    Pershkrimi = "Refreshing beverages.",
                    Renditja = 4,
                    RestaurantId = fishandchips.Id
                };

                categoriesHookFishandChips.AddRange(new[] { offers, fishChips, extras, drinks });
                context.MenuCategories.AddRange(categoriesHookFishandChips);
                context.SaveChanges();


                var offersItems = new List<MenuItems>
    {
        new MenuItems
        {
            Emertimi = "Grill x Hook",
            Pershkrimi = "Super combo of Hake fish and Shrimp",
            Cmimi = 6.90m,
            Foto = "hookfishandchips/grillxhook.png",
            Disponueshme = true,
            Alergjene = "Fish, Shellfish",
            Kalori = 650,
            CategoryId = offers.Id
        }
                };

                context.MenuItems.AddRange(offersItems);
                context.SaveChanges();

                var fishChipsItems = new List<MenuItems>
    {
        new MenuItems
        {
            Emertimi = "Double Fillet",
            Pershkrimi = "Two crispy fish fillets served with fries.",
            Cmimi = 7.50m,
            Foto = "hookfishandchips/doublefillet.png",
            Disponueshme = true,
            Alergjene = "Fish, Gluten",
            Kalori = 900,
            CategoryId = fishChips.Id
        },
        new MenuItems
        {
            Emertimi = "Single Fillet",
            Pershkrimi = "One crispy fish fillet served with fries.",
            Cmimi = 5.50m,
            Foto = "hookfishandchips/singlefillet.png",
            Disponueshme = true,
            Alergjene = "Fish, Gluten",
            Kalori = 650,
            CategoryId = fishChips.Id
        },
        new MenuItems
        {
            Emertimi = "Single Hook",
            Pershkrimi = "Signature single fish fillet with fries.",
            Cmimi = 5.90m,
            Foto = "hookfishandchips/singlehook.png",
            Disponueshme = true,
            Alergjene = "Fish, Gluten",
            Kalori = 670,
            CategoryId = fishChips.Id
        },
        new MenuItems
        {
            Emertimi = "Crispy Shrimp",
            Pershkrimi = "Golden fried shrimp served with fries.",
            Cmimi = 6.90m,
            Foto = "hookfishandchips/crispyshrimp.png",
            Disponueshme = true,
            Alergjene = "Shellfish, Gluten",
            Kalori = 720,
            CategoryId = fishChips.Id
        },
        new MenuItems
        {
            Emertimi = "Grilled Shrimp",
            Pershkrimi = "Grilled shrimp served with fries.",
            Cmimi = 7.20m,
            Foto = "hookfishandchips/grilledshrimp.png",
            Disponueshme = true,
            Alergjene = "Shellfish",
            Kalori = 680,
            CategoryId = fishChips.Id
        },
        new MenuItems
        {
            Emertimi = "Fish Fingers",
            Pershkrimi = "Crispy fish fingers served with fries.",
            Cmimi = 4.90m,
            Foto = "hookfishandchips/fishfingers.png",
            Disponueshme = true,
            Alergjene = "Fish, Gluten",
            Kalori = 600,
            CategoryId = fishChips.Id
        },
        new MenuItems
        {
            Emertimi = "Fish Burger",
            Pershkrimi = "Fish fillet burger with lettuce and tartar sauce.",
            Cmimi = 5.90m,
            Foto = "hookfishandchips/fishburger.png",
            Disponueshme = true,
            Alergjene = "Fish, Gluten, Milk",
            Kalori = 750,
            CategoryId = fishChips.Id
        },
        new MenuItems
        {
            Emertimi = "Menu Combo",
            Pershkrimi = "Coca-Cola and onion rings.",
            Cmimi = 8.50m,
            Foto = "hookfishandchips/menucombo.png",
            Disponueshme = true,
            Alergjene = "Fish, Gluten, Milk",
            Kalori = 950,
            CategoryId = fishChips.Id
        },
        new MenuItems
        {
            Emertimi = "Crunchy Smelt",
            Pershkrimi = "Crispy fried smelt fish served with fries.",
            Cmimi = 6.20m,
            Foto = "hookfishandchips/crunchysmelt.png",
            Disponueshme = true,
            Alergjene = "Fish, Gluten",
            Kalori = 700,
            CategoryId = fishChips.Id
        }
    };

                context.MenuItems.AddRange(fishChipsItems);
                context.SaveChanges();



                var extrasItems = new List<MenuItems>
    {
        new MenuItems
        {
            Emertimi = "Extra Tartar",
            Pershkrimi = "Classic tartar sauce.",
            Cmimi = 0.80m,
            Foto = "hookfishandchips/extratartar.png",
            Disponueshme = true,
            Alergjene = "Egg, Milk",
            Kalori = 120,
            CategoryId = extras.Id
        },
        new MenuItems
        {
            Emertimi = "Extra Spicy 🌶️",
            Pershkrimi = "Hot and spicy sauce.",
            Cmimi = 0.80m,
            Foto = "hookfishandchips/extraspicy.png",
            Disponueshme = true,
            Alergjene = "None",
            Kalori = 90,
            CategoryId = extras.Id
        },
        new MenuItems
        {
            Emertimi = "Extra Sweet Chili",
            Pershkrimi = "Sweet chili sauce.",
            Cmimi = 0.80m,
            Foto = "hookfishandchips/extrasweetchili.png",
            Disponueshme = true,
            Alergjene = "None",
            Kalori = 100,
            CategoryId = extras.Id
        },
        new MenuItems
        {
            Emertimi = "Pomfrit",
            Pershkrimi = "Classic French fries.",
            Cmimi = 2.50m,
            Foto = "hookfishandchips/pomfrit.png",
            Disponueshme = true,
            Alergjene = "None",
            Kalori = 400,
            CategoryId = extras.Id
        }
    };

                context.MenuItems.AddRange(extrasItems);
                context.SaveChanges();

                var drinksItems = new List<MenuItems>
    {
        new MenuItems
        {
            Emertimi = "Coca Cola",
            Pershkrimi = "Classic Coca Cola.",
            Cmimi = 1.50m,
            Foto = "hookfishandchips/cocacola.png",
            Disponueshme = true,
            Alergjene = "None",
            Kalori = 140,
            CategoryId = drinks.Id
        },
        new MenuItems
        {
            Emertimi = "Coca Cola Zero",
            Pershkrimi = "Sugar-free Coca Cola Zero.",
            Cmimi = 1.50m,
            Foto = "hookfishandchips/cocacolazero.png",
            Disponueshme = true,
            Alergjene = "None",
            Kalori = 0,
            CategoryId = drinks.Id
        },
        new MenuItems
        {
            Emertimi = "Fanta",
            Pershkrimi = "Refreshing orange Fanta.",
            Cmimi = 1.50m,
            Foto = "hookfishandchips/fanta.png",
            Disponueshme = true,
            Alergjene = "None",
            Kalori = 160,
            CategoryId = drinks.Id
        },
        new MenuItems
        {
            Emertimi = "Ice Tea",
            Pershkrimi = "Cool and refreshing iced tea.",
            Cmimi = 1.50m,
            Foto = "hookfishandchips/icetea.png",
            Disponueshme = true,
            Alergjene = "None",
            Kalori = 90,
            CategoryId = drinks.Id
        },
        new MenuItems
        {
            Emertimi = "Schweppes",
            Pershkrimi = "Sparkling Schweppes tonic.",
            Cmimi = 1.50m,
            Foto = "hookfishandchips/schweppes.png",
            Disponueshme = true,
            Alergjene = "None",
            Kalori = 110,
            CategoryId = drinks.Id
        },
        new MenuItems
        {
            Emertimi = "Red Bull",
            Pershkrimi = "Energy drink Red Bull.",
            Cmimi = 2.00m,
            Foto = "hookfishandchips/redbull.png",
            Disponueshme = true,
            Alergjene = "None",
            Kalori = 120,
            CategoryId = drinks.Id
        },
        new MenuItems
        {
            Emertimi = "Water",
            Pershkrimi = "Still mineral water.",
            Cmimi = 1.00m,
            Foto = "hookfishandchips/water.png",
            Disponueshme = true,
            Alergjene = "None",
            Kalori = 0,
            CategoryId = drinks.Id
        },
        new MenuItems
        {
            Emertimi = "Sparkling Water",
            Pershkrimi = "Carbonated sparkling water.",
            Cmimi = 1.20m,
            Foto = "hookfishandchips/sparklingwater.png",
            Disponueshme = true,
            Alergjene = "None",
            Kalori = 0,
            CategoryId = drinks.Id
        }
    };

                context.MenuItems.AddRange(drinksItems);
                context.SaveChanges();

            }

            var categoriesFrix = new List<MenuCategory>();
            var frix = context.Restaurants.FirstOrDefault(r => r.Emertimi == "Frix");

            if (frix != null)
            {

                var fries = new MenuCategory
                {
                    Emertimi = "Fries",
                    Pershkrimi = "Crispy and delicious fries.",
                    Renditja = 1,
                    RestaurantId = frix.Id

                };
                var burgers = new MenuCategory
                {
                    Emertimi = "Burgers",
                    Pershkrimi = "Juicy and delicious burgers.",
                    Renditja = 2,
                    RestaurantId = frix.Id

                };
                var hotdog = new MenuCategory
                {
                    Emertimi = "Hotdog",
                    Pershkrimi = "Delicious and savory hotdogs.",
                    Renditja = 3,
                    RestaurantId = frix.Id

                };
                var sandwich = new MenuCategory
                {
                    Emertimi = "Sandwich",
                    Pershkrimi = "Delicious and savory sandwiches.",
                    Renditja = 4,
                    RestaurantId = frix.Id

                };
                var drinks = new MenuCategory
                {
                    Emertimi = "Drinks",
                    Pershkrimi = "Refreshing beverages.",
                    Renditja = 5,
                    RestaurantId = frix.Id

                };
                categoriesFrix.AddRange(new[] { fries, burgers, hotdog, sandwich, drinks });
                context.MenuCategories.AddRange(categoriesFrix);
                context.SaveChanges();

                var friesItems = new List<MenuItems>
                {
                new MenuItems
                {
                    Emertimi = "Veggie Mushroom",
                    Pershkrimi = "Stir-fried vegetables with mushrooms in a light, savory sauce.",
                    Cmimi = 3.49m,
                    Foto = "frix/1.png",
                    Disponueshme = true,
                    Alergjene = "Soy,Eggs",
                    Kalori = 300,
                    CategoryId = fries.Id
                },
                new MenuItems
                {
                    Emertimi = "Conquer the Everest",
                    Pershkrimi = "A hearty, loaded dish with a rich mix of premium ingredients, bold flavors, and a satisfying finish.",
                    Cmimi = 3.49m,
                    Foto = "frix/2.png",
                    Disponueshme = true,
                    Alergjene = "Soy,Eggs",
                    Kalori = 800,
                    CategoryId = fries.Id
                },
                new MenuItems
                {
                    Emertimi = "Cheesy Cheese",
                    Pershkrimi = "Rich and creamy dish loaded with melted cheese for an extra indulgent, comforting bite.",
                    Cmimi = 3.49m,
                    Foto = "frix/33.png",
                    Disponueshme = true,
                    Alergjene = "Soy,Eggs,Milk",
                    Kalori = 600,
                    CategoryId = fries.Id
                },
                new MenuItems
                {
                    Emertimi = "Patato Wedges",
                    Pershkrimi = "Crispy and golden potato wedges, perfect as a side or snack.",
                    Cmimi = 2.99m,
                    Foto = "frix/44.png",
                    Disponueshme = true,
                    Alergjene = "None",
                    Kalori = 600,
                    CategoryId = fries.Id
                },
                new MenuItems
                {
                    Emertimi = "Fitil",
                    Pershkrimi = "Crispy fried strips served golden and crunchy with a flavorful dipping sauce",
                    Cmimi = 2.49m,
                    Foto = "frix/55.png",
                    Disponueshme = true,
                    Alergjene = "None",
                    Kalori = 250,
                    CategoryId = fries.Id
                }
                };
                context.MenuItems.AddRange(friesItems);
                context.SaveChanges();

                var burgerItems = new List<MenuItems>
                {

                    new MenuItems{
                        Emertimi = "Frix Burger",
                        Pershkrimi = "Juicy beef patty with lettuce, tomato, and our special sauce.",
                        Cmimi = 3.99m,
                        Foto = "frix/bb1.png",
                        Disponueshme = true,
                        Alergjene = "Gluten, Soy, Eggs",
                        Kalori = 700,
                        CategoryId = burgers.Id
                    },
                    new MenuItems{
                        Emertimi = "Chicken Burger",
                        Pershkrimi = "Juicy chicken patty with lettuce, tomato, and our special sauce.",
                        Cmimi = 3.49m,
                        Foto = "frix/bb2.png",
                        Disponueshme = true,
                        Alergjene = "Gluten, Soy, Eggs",
                        Kalori = 450,
                        CategoryId = burgers.Id
                    },

                    new MenuItems{
                        Emertimi = "Hash Brown Beef Burger",
                        Pershkrimi = "Juicy beef patty topped with crispy hash brown, melted cheese, fresh lettuce, and creamy sauce in a toasted bun.",
                        Cmimi = 3.89m,
                        Foto = "frix/bb3.png",
                        Disponueshme = true,
                        Alergjene = "Gluten, Soy, Eggs",
                        Kalori = 900,
                        CategoryId = burgers.Id
                    },
                    new MenuItems{
                        Emertimi = "Dutch Sliders",
                        Pershkrimi = "Mini burgers with juicy beef, melted cheese, and soft buns, served in a tasty bite-sized portion.",
                        Cmimi = 3.89m,
                        Foto = "frix/bb4.png",
                        Disponueshme = true,
                        Alergjene = "Gluten, Soy, Eggs",
                        Kalori = 400,
                        CategoryId = burgers.Id
                    }
                };
                context.MenuItems.AddRange(burgerItems);
                context.SaveChanges();

                var hotdogItems = new List<MenuItems>
                {
                new MenuItems
                {
                    Emertimi = "Hotdog Classic",
                    Pershkrimi = "Grilled sausage served in a soft bun with classic toppings and sauces.",
                    Cmimi = 3.49m,
                    Foto = "frix/h11.png",
                    Disponueshme = true,
                    Alergjene = "Gluten, Soy, Eggs",
                    Kalori = 300,
                    CategoryId = hotdog.Id
                },
                new MenuItems
                {
                    Emertimi = "Hotdog Mexican",
                    Pershkrimi = "Grilled sausage in a soft bun topped with spicy sauce, fresh vegetables, and a Mexican-style flavor twist.",
                    Cmimi = 3.49m,
                    Foto = "frix/h222.png",
                    Disponueshme = true,
                    Alergjene = "Gluten, Soy, Eggs",
                    Kalori = 400,
                    CategoryId = hotdog.Id
                },
                new MenuItems
                {
                    Emertimi = "Wrap Dog",
                    Pershkrimi = "Grilled sausage wrapped in a soft tortilla with fresh toppings and savory sauce.",
                    Cmimi = 2.99m,
                    Foto = "frix/w1.png",
                    Disponueshme = true,
                    Alergjene = "Gluten, Soy, Eggs,Milk",
                    Kalori = 350,
                    CategoryId = hotdog.Id
                },
                new MenuItems
                {
                    Emertimi = "Chicken Burrito",
                    Pershkrimi = "Flour tortilla filled with grilled chicken, rice, beans, cheese, and fresh salsa, wrapped and served warm.",
                    Cmimi = 2.99m,
                    Foto = "frix/w2.png",
                    Disponueshme = true,
                    Alergjene = "Gluten, Soy, Eggs",
                    Kalori = 750,
                    CategoryId = hotdog.Id
                }
                };
                context.MenuItems.AddRange(hotdogItems);
                context.SaveChanges();

                var sandwichItems = new List<MenuItems>
                {
                new MenuItems
                {
                    Emertimi = "Frix Chicken Sandwich",
                    Pershkrimi = "Crispy fried chicken fillet served in a soft bun with lettuce and signature sauce.",
                    Cmimi = 3.49m,
                    Foto = "frix/s1.png",
                    Disponueshme = true,
                    Alergjene = "Gluten, Soy, Eggs",
                    Kalori = 600,
                    CategoryId = hotdog.Id
                },
                new MenuItems
                {
                    Emertimi = "Chicken Pesto Sandwich",
                    Pershkrimi = "Grilled chicken with basil pesto, fresh greens, and cheese served in a toasted sandwich bun.",
                    Cmimi = 3.49m,
                    Foto = "frix/s22.png",
                    Disponueshme = true,
                    Alergjene = "Gluten, Soy, Eggs",
                    Kalori = 550,
                    CategoryId = hotdog.Id
                },
                new MenuItems
                {
                    Emertimi = "Tuna Sandwich",
                    Pershkrimi = "Tuna mixed with mayo, served with fresh lettuce in soft bread or a toasted bun.",
                    Cmimi = 3.49m,
                    Foto = "frix/s33.png",
                    Disponueshme = true,
                    Alergjene = "Gluten, Soy, Eggs,Fish",
                    Kalori = 550,
                    CategoryId = hotdog.Id
                }
                };

                context.MenuItems.AddRange(sandwichItems);
                context.SaveChanges();

                var drinkItems = new List<MenuItems> {
                new MenuItems
                 {
                    Emertimi = "Coca Cola",
                    Pershkrimi = "Coca Cola",
                    Cmimi = 1.50m,
                    Foto = "pastafasta/cocacola.png",
                    Disponueshme = true,
                    Alergjene = "None",
                    Kalori = 150,
                    CategoryId = drinks.Id
                },
                new MenuItems
                 {
                    Emertimi = "Fanta Orange",
                    Pershkrimi = "Fanta orange",
                    Cmimi = 1.50m,
                    Foto = "pastafasta/fantaorange.png",
                    Disponueshme = true,
                    Alergjene = "None",
                    Kalori = 150,
                    CategoryId = drinks.Id
                },

                new MenuItems
                 {
                    Emertimi = "Sprite",
                    Pershkrimi = "Sprite",
                    Cmimi = 1.50m,
                    Foto = "pastafasta/sprite.png",
                    Disponueshme = true,
                    Alergjene = "None",
                    Kalori = 150,
                    CategoryId = drinks.Id
                },
                new MenuItems
                 {
                    Emertimi = "Water",
                    Pershkrimi = "Water",
                    Cmimi = 1.00m,
                    Foto = "pastafasta/water.png",
                    Disponueshme = true,
                    Alergjene = "None",
                    Kalori = 0,
                    CategoryId = drinks.Id
                }




                };
                context.MenuItems.AddRange(drinkItems);
                context.SaveChanges();
            }


        }


    }

    private static async Task MigrateLegacyRoleAsync(
        RoleManager<Role> roleManager,
        UserManager<User> userManager,
        string legacyRole,
        string targetRole)
    {
        if (!await roleManager.RoleExistsAsync(legacyRole))
        {
            return;
        }

        var legacyUsers = await userManager.GetUsersInRoleAsync(legacyRole);
        foreach (var user in legacyUsers)
        {
            if (!await userManager.IsInRoleAsync(user, targetRole))
            {
                await userManager.AddToRoleAsync(user, targetRole);
            }

            await userManager.RemoveFromRoleAsync(user, legacyRole);
        }

        var legacyRoleEntity = await roleManager.FindByNameAsync(legacyRole);
        if (legacyRoleEntity != null)
        {
            await roleManager.DeleteAsync(legacyRoleEntity);
        }

        Console.WriteLine($"Migrated legacy role '{legacyRole}' to '{targetRole}'");
    }
}


