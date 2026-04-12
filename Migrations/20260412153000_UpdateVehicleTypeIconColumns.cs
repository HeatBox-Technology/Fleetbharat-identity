using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FleetBharat.IdentityService.Migrations
{
    [DbContext(typeof(IdentityDbContext))]
    [Migration("20260412153000_UpdateVehicleTypeIconColumns")]
    public class UpdateVehicleTypeIconColumns : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DefaultVehicleIcon",
                table: "mst_vehicle_type");

            migrationBuilder.DropColumn(
                name: "DefaultAlarmIcon",
                table: "mst_vehicle_type");

            migrationBuilder.DropColumn(
                name: "DefaultIconColor",
                table: "mst_vehicle_type");

            migrationBuilder.AddColumn<string>(
                name: "BreakdownIcon",
                table: "mst_vehicle_type",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IdleIcon",
                table: "mst_vehicle_type",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MovingIcon",
                table: "mst_vehicle_type",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OfflineIcon",
                table: "mst_vehicle_type",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ParkedIcon",
                table: "mst_vehicle_type",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StoppedIcon",
                table: "mst_vehicle_type",
                type: "text",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BreakdownIcon",
                table: "mst_vehicle_type");

            migrationBuilder.DropColumn(
                name: "IdleIcon",
                table: "mst_vehicle_type");

            migrationBuilder.DropColumn(
                name: "MovingIcon",
                table: "mst_vehicle_type");

            migrationBuilder.DropColumn(
                name: "OfflineIcon",
                table: "mst_vehicle_type");

            migrationBuilder.DropColumn(
                name: "ParkedIcon",
                table: "mst_vehicle_type");

            migrationBuilder.DropColumn(
                name: "StoppedIcon",
                table: "mst_vehicle_type");

            migrationBuilder.AddColumn<string>(
                name: "DefaultAlarmIcon",
                table: "mst_vehicle_type",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DefaultIconColor",
                table: "mst_vehicle_type",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DefaultVehicleIcon",
                table: "mst_vehicle_type",
                type: "text",
                nullable: true);
        }
    }
}
