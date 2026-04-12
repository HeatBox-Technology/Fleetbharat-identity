using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FleetBharat.IdentityService.Migrations
{
    [DbContext(typeof(IdentityDbContext))]
    [Migration("20260412173000_AddAccountIdToVehicleType")]
    public class AddAccountIdToVehicleType : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AccountId",
                table: "mst_vehicle_type",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AccountId",
                table: "mst_vehicle_type");
        }
    }
}
