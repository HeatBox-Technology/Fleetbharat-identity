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
            migrationBuilder.Sql(
                """
                ALTER TABLE public."mst_vehicle_type"
                    ADD COLUMN IF NOT EXISTS "BreakdownIcon" text,
                    ADD COLUMN IF NOT EXISTS "IdleIcon" text,
                    ADD COLUMN IF NOT EXISTS "MovingIcon" text,
                    ADD COLUMN IF NOT EXISTS "OfflineIcon" text,
                    ADD COLUMN IF NOT EXISTS "ParkedIcon" text,
                    ADD COLUMN IF NOT EXISTS "StoppedIcon" text;

                ALTER TABLE public."mst_vehicle_type"
                    DROP COLUMN IF EXISTS "DefaultVehicleIcon",
                    DROP COLUMN IF EXISTS "DefaultAlarmIcon",
                    DROP COLUMN IF EXISTS "DefaultIconColor";
                """);
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
