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
                name: "map_FormRole_right",
                columns: table => new
                {
                    RoleFormRightId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RoleId = table.Column<int>(type: "integer", nullable: false),
                    FormId = table.Column<int>(type: "integer", nullable: false),
                    CanRead = table.Column<bool>(type: "boolean", nullable: false),
                    CanWrite = table.Column<bool>(type: "boolean", nullable: false),
                    CanDelete = table.Column<bool>(type: "boolean", nullable: false),
                    CanExport = table.Column<bool>(type: "boolean", nullable: false),
                    CanAll = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_map_FormRole_right", x => x.RoleFormRightId);
                });

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
                name: "mst_city",
                columns: table => new
                {
                    CityId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StateId = table.Column<int>(type: "integer", nullable: false),
                    CityName = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mst_city", x => x.CityId);
                });

            migrationBuilder.CreateTable(
                name: "mst_country",
                columns: table => new
                {
                    CountryId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Iso2Code = table.Column<string>(type: "text", nullable: false),
                    Iso3Code = table.Column<string>(type: "text", nullable: false),
                    CountryName = table.Column<string>(type: "text", nullable: false),
                    MobileDialCode = table.Column<string>(type: "text", nullable: false),
                    CurrencyCode = table.Column<string>(type: "text", nullable: false),
                    CurrencySymbol = table.Column<string>(type: "text", nullable: false),
                    TimezoneName = table.Column<string>(type: "text", nullable: false),
                    UtcOffset = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mst_country", x => x.CountryId);
                });

            migrationBuilder.CreateTable(
                name: "mst_form",
                columns: table => new
                {
                    FormId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FormCode = table.Column<string>(type: "text", nullable: false),
                    FormName = table.Column<string>(type: "text", nullable: false),
                    ModuleName = table.Column<string>(type: "text", nullable: false),
                    PageUrl = table.Column<string>(type: "text", nullable: false),
                    PageComponent = table.Column<string>(type: "text", nullable: false),
                    IconName = table.Column<string>(type: "text", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: true),
                    IsMenu = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    ParentFormId = table.Column<int>(type: "integer", nullable: true),
                    IsVisible = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mst_form", x => x.FormId);
                });

            migrationBuilder.CreateTable(
                name: "mst_role",
                columns: table => new
                {
                    RoleId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RoleCode = table.Column<string>(type: "text", nullable: false),
                    RoleName = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mst_role", x => x.RoleId);
                });

            migrationBuilder.CreateTable(
                name: "mst_state",
                columns: table => new
                {
                    StateId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CountryId = table.Column<int>(type: "integer", nullable: false),
                    StateCode = table.Column<string>(type: "text", nullable: false),
                    StateName = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mst_state", x => x.StateId);
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

            migrationBuilder.CreateIndex(
                name: "IX_map_FormRole_right_RoleId_FormId",
                table: "map_FormRole_right",
                columns: new[] { "RoleId", "FormId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_mst_city_StateId_CityName",
                table: "mst_city",
                columns: new[] { "StateId", "CityName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_mst_state_CountryId_StateCode",
                table: "mst_state",
                columns: new[] { "CountryId", "StateCode" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "map_FormRole_right");

            migrationBuilder.DropTable(
                name: "mst_account");

            migrationBuilder.DropTable(
                name: "mst_city");

            migrationBuilder.DropTable(
                name: "mst_country");

            migrationBuilder.DropTable(
                name: "mst_form");

            migrationBuilder.DropTable(
                name: "mst_role");

            migrationBuilder.DropTable(
                name: "mst_state");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
