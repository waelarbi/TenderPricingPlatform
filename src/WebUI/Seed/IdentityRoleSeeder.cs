using Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Shared.Auth;

public static class IdentityRoleSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var roleMgr = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userMgr = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // Apply pending migrations for Identity DB (safe if already applied)
        await db.Database.MigrateAsync();

        // 1) Ensure roles (idempotent, race-safe)
        var roleNames = new[] { Roles.Admin, Roles.PricingManager, Roles.Reviewer, Roles.User };
        foreach (var name in roleNames)
        {
            // Try find first (case-insensitive via normalized name)
            var existing = await roleMgr.FindByNameAsync(name);
            if (existing is null)
            {
                var create = await roleMgr.CreateAsync(new IdentityRole(name));
                if (!create.Succeeded)
                {
                    // Ignore duplicates from a race (role was created between check & create)
                    var dup = create.Errors.Any(e =>
                        e.Code.Contains("Duplicate", StringComparison.OrdinalIgnoreCase) ||
                        e.Description.Contains("already", StringComparison.OrdinalIgnoreCase));
                    if (!dup)
                        throw new InvalidOperationException($"Failed to create role '{name}': " +
                                                            string.Join("; ", create.Errors.Select(e => e.Description)));
                }
            }
        }

        // 2) Ensure an admin user exists (dev)
        const string adminEmail = "admin@local.test";
        const string password = "Change_me_123!";

        var admin = await userMgr.FindByEmailAsync(adminEmail)
                    ?? await userMgr.FindByNameAsync(adminEmail);

        if (admin is null)
        {
            admin = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true
            };

            var created = await userMgr.CreateAsync(admin, password);
            if (!created.Succeeded)
            {
                // If it failed because it already exists (race), re-fetch
                var maybe = await userMgr.FindByEmailAsync(adminEmail)
                            ?? await userMgr.FindByNameAsync(adminEmail);
                if (maybe is null)
                    throw new InvalidOperationException($"Failed to create admin user: " +
                        string.Join("; ", created.Errors.Select(e => e.Description)));
                admin = maybe;
            }
        }

        // 3) Ensure admin user has required roles (idempotent)
        var adminRoles = new[] { Roles.Admin, Roles.PricingManager, Roles.Reviewer };
        foreach (var r in adminRoles)
        {
            if (!await userMgr.IsInRoleAsync(admin, r))
            {
                var added = await userMgr.AddToRoleAsync(admin, r);
                if (!added.Succeeded)
                {
                    var already = added.Errors.Any(e =>
                        e.Code.Contains("UserAlreadyInRole", StringComparison.OrdinalIgnoreCase) ||
                        e.Description.Contains("already", StringComparison.OrdinalIgnoreCase));
                    if (!already)
                        throw new InvalidOperationException($"Failed to add '{r}' to admin: " +
                                                            string.Join("; ", added.Errors.Select(e => e.Description)));
                }
            }
        }
    }
}