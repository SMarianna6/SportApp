using System.Security.Claims;

namespace SportLab.Models
{
    
    public static class Roles
    {
        public const string Administrator = "Administrator";
        public const string Trainer = "Trainer";
        public const string Client = "Client";
        public const string Moderator = "Moderator";

        
        public const string AdminAlias = "Admin";

        
        public const string AdminRoles = Administrator + "," + AdminAlias;
        public const string TrainerRoles = Trainer + "," + Administrator + "," + AdminAlias;
        public const string ModeratorRoles = Moderator + "," + Administrator + "," + AdminAlias;
        public const string ClientRoles = Client + "," + Administrator + "," + AdminAlias;

        public static bool IsAdmin(this ClaimsPrincipal user) =>
            user.IsInRole(Administrator) || user.IsInRole(AdminAlias);

        public static bool IsModerator(this ClaimsPrincipal user) =>
            user.IsInRole(Moderator) || user.IsAdmin();

        
        public static string Normalize(string? role) //Lab6
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
