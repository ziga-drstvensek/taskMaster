using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BacklogApi.Migrations
{
    /// <inheritdoc />
    public partial class SeedAdminUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var adminUserId = Guid.NewGuid().ToString();
            var adminRoleId = Guid.NewGuid().ToString();
            var managerRoleId = Guid.NewGuid().ToString();
            var userRoleId = Guid.NewGuid().ToString();

            // Insert Roles
            migrationBuilder.InsertData(
                table: "AspNetRoles",
                columns: new[] { "Id", "Name", "NormalizedName", "ConcurrencyStamp" },
                values: new object[,]
                {
                    { adminRoleId, "Admin", "ADMIN", Guid.NewGuid().ToString() },
                    { managerRoleId, "Manager", "MANAGER", Guid.NewGuid().ToString() },
                    { userRoleId, "User", "USER", Guid.NewGuid().ToString() }
                });

            // Insert Admin User (Password: Password123!)
            // Note: In a real production environment, you should use a properly hashed password.
            // This is a simplified seed for local development/reset.
            // The hash below is for "Password123!" using default ASP.NET Identity settings.
            migrationBuilder.InsertData(
                table: "AspNetUsers",
                columns: new[] { "Id", "UserName", "NormalizedUserName", "Email", "NormalizedEmail", "EmailConfirmed", "PasswordHash", "SecurityStamp", "ConcurrencyStamp", "TwoFactorEnabled", "PhoneNumberConfirmed", "LockoutEnabled", "AccessFailedCount" },
                values: new object[] 
                { 
                    adminUserId, 
                    "admin", 
                    "ADMIN", 
                    "admin@example.com", 
                    "ADMIN@EXAMPLE.COM", 
                    true, 
                    "AQAAAAIAAYagAAAAEASKkA2KVRl+JRiEPebiyCs2aq6bB2C5ce5MaYu4rHiuldqz5toSHNwN4A1UTxIVhw==", // Password: Password123!
                    Guid.NewGuid().ToString(), 
                    Guid.NewGuid().ToString(), 
                    false, 
                    false, 
                    true, 
                    0 
                });

            // Assign Admin Role to Admin User
            migrationBuilder.InsertData(
                table: "AspNetUserRoles",
                columns: new[] { "UserId", "RoleId" },
                values: new object[] { adminUserId, adminRoleId });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
