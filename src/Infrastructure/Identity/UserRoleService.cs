using System.Security.Claims;
using Application.Identity;
using Infrastructure.Data;                    // ApplicationDbContext, ApplicationUser
using Microsoft.AspNetCore.Identity;

namespace Infrastructure.Identity;

public sealed class UserRoleService(
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole> roleManager) : IUserRoleService
{
    private readonly UserManager<ApplicationUser> _userManager = userManager;
    private readonly RoleManager<IdentityRole> _roleManager = roleManager;

    public async Task<SaveUserRolesResult> SaveUserRolesAsync(
        string targetUserId,
        IEnumerable<string> selectedRoles,
        ClaimsPrincipal actingPrincipal,
        bool preventSelfAdminDemotion = true,
        CancellationToken ct = default)
    {
        var selected = selectedRoles
            .Where(r => !string.IsNullOrWhiteSpace(r))
            .Select(r => r.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        // Ensure target user exists
        var target = await _userManager.FindByIdAsync(targetUserId)
                     ?? throw new InvalidOperationException("User not found.");

        // Ensure roles exist
        foreach (var r in selected)
            if (!await _roleManager.RoleExistsAsync(r))
                return new SaveUserRolesResult([], [], [$"Role '{r}' does not exist."]);

        var current = await _userManager.GetRolesAsync(target);
        var toAdd = selected.Except(current, StringComparer.OrdinalIgnoreCase).ToArray();
        var toRemove = current.Except(selected, StringComparer.OrdinalIgnoreCase).ToArray();

        // Optional safety: don’t allow removing your own Admin role
        if (preventSelfAdminDemotion)
        {
            var acting = await _userManager.GetUserAsync(actingPrincipal);
            if (acting?.Id == target.Id &&
                toRemove.Contains("Admin", StringComparer.OrdinalIgnoreCase))
            {
                return new SaveUserRolesResult([], [], ["You cannot remove your own Admin role."]);
            }
        }

        var errors = new List<string>();

        if (toAdd.Length > 0)
        {
            var res = await _userManager.AddToRolesAsync(target, toAdd);
            if (!res.Succeeded) errors.AddRange(res.Errors.Select(e => e.Description));
        }

        if (toRemove.Length > 0)
        {
            var res = await _userManager.RemoveFromRolesAsync(target, toRemove);
            if (!res.Succeeded) errors.AddRange(res.Errors.Select(e => e.Description));
        }

        return new SaveUserRolesResult(toAdd, toRemove, errors);
    }
}