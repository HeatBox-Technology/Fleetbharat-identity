using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace FleetBharat.IdentityService.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "mst_account",
                columns: table => new
                {
                    AccountId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AccountCode = table.Column<string>(type: "text", nullable: false),
                    AccountName = table.Column<string>(type: "text", nullable: false),
                    AccountType = table.Column<string>(type: "text", nullable: false),
                    Fk_userid = table.Column<int>(type: "integer", nullable: false),
                    ParentAccountId = table.Column<int>(type: "integer", nullable: true),
                    HierarchyPath = table.Column<string>(type: "text", nullable: false),
                    PrimaryDomain = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mst_account", x => x.AccountId);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountId = table.Column<int>(type: "integer", nullable: false),
                    roleId = table.Column<int>(type: "integer", nullable: false),
                    Role = table.Column<string>(type: "text", nullable: false),
                    GlobalUserCode = table.Column<string>(type: "text", nullable: true),
                    FirstName = table.Column<string>(type: "text", nullable: false),
                    LastName = table.Column<string>(type: "text", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: false),
                    MobileNo = table.Column<string>(type: "text", nullable: false),
                    CountryCode = table.Column<string>(type: "text", nullable: false),
                    Timezone = table.Column<string>(type: "text", nullable: false),
                    Language = table.Column<string>(type: "text", nullable: false),
                    Password_hash = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<bool>(type: "boolean", nullable: false),
                    UserStatusLkpId = table.Column<int>(type: "integer", nullable: false),
                    EmailVerified = table.Column<bool>(type: "boolean", nullable: false),
                    MobileVerified = table.Column<bool>(type: "boolean", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    LastLoginAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReferralCode = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RefreshToken = table.Column<string>(type: "text", nullable: true),
                    RefreshTokenExpiry = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PasswordResetTokenHash = table.Column<string>(type: "text", nullable: true),
                    PasswordResetExpiry = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.UserId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "mst_account");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
