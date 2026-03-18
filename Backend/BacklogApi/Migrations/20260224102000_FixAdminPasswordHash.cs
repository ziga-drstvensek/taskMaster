using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BacklogApi.Migrations
{
    public partial class FixAdminPasswordHash : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Update existing admin user with a valid Base64 hash if it's currently invalid
            // The hash below is for "Password123!" using ASP.NET Identity V3
            migrationBuilder.Sql("UPDATE AspNetUsers SET PasswordHash = 'AQAAAAIAAYagAAAAEASKkA2KVRl+JRiEPebiyCs2aq6bB2C5ce5MaYu4rHiuldqz5toSHNwN4A1UTxIVhw==' WHERE UserName = 'admin';");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No reverse action needed for this fix in development
        }
    }
}
