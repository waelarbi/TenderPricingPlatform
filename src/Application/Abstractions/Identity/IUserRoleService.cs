using System.Security.Claims;

namespace Application.Abstractions.Identity;

public sealed record SaveUserRolesResult(
    IReadOnlyList<string> Added,
    IReadOnlyList<string> Removed,
    IReadOnlyList<string> Errors);

public interface IUserRoleService
{
    /// <summary>
    /// Save the exact set of roles for a user (adds the missing ones and removes the extra ones).
    /// </summary>
    Task<SaveUserRolesResult> SaveUserRolesAsync(
        string targetUserId,
        IEnumerable<string> selectedRoles,
        ClaimsPrincipal actingPrincipal,
        bool preventSelfAdminDemotion = true,
        CancellationToken ct = default);
}