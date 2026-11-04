using System.Security.Claims;

namespace SportLab.Models
{
    /// <summary>
    /// Central place for role names used by the role-based authorization (Lab 6).
    /// The technical specification uses "Administrator"; the older admin panel
    /// still accepts the legacy "Admin" value, so both are treated as equal.
    /// </summary>
    public static class Roles
    {
        public const string Administrator = "Administrator";
        public const string Trainer = "Trainer";
        public const string Client = "Client";
        public const string Moderator = "Moderator";

        // Legacy role name kept for records created before Lab 6.
        public const string AdminAlias = "Admin";

        // Ready-to-use values for [Authorize(Roles = ...)].
        public const string AdminRoles = Administrator + "," + AdminAlias;
        public const string TrainerRoles = Trainer + "," + Administrator + "," + AdminAlias;
        public const string ModeratorRoles = Moderator + "," + Administrator + "," + AdminAlias;
        public const string ClientRoles = Client + "," + Administrator + "," + AdminAlias;

        public static bool IsAdmin(this ClaimsPrincipal user) =>
            user.IsInRole(Administrator) || user.IsInRole(AdminAlias);

        public static bool IsModerator(this ClaimsPrincipal user) =>
            user.IsInRole(Moderator) || user.IsAdmin();

        /// <summary>Maps role names (including the legacy "Admin") to canonical values.</summary>
        public static string Normalize(string? role)
        {
            if (string.IsNullOrWhiteSpace(role))
            {
                return Client;
            }

            if (role.Equals(Administrator, StringComparison.OrdinalIgnoreCase) ||
                role.Equals(AdminAlias, StringComparison.OrdinalIgnoreCase))
            {
                return Administrator;
            }

            if (role.Equals(Trainer, StringComparison.OrdinalIgnoreCase))
            {
                return Trainer;
            }

            if (role.Equals(Moderator, StringComparison.OrdinalIgnoreCase))
            {
                return Moderator;
            }

            if (role.Equals(Client, StringComparison.OrdinalIgnoreCase))
            {
                return Client;
            }

            return role;
        }
    }
}
