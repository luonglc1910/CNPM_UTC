using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HotelManagement.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddRoomTypeFilterFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BedType",
                table: "RoomTypes",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsFreeCancellation",
                table: "RoomTypes",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsPayAtHotel",
                table: "RoomTypes",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "SizeSqm",
                table: "RoomTypes",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BedType",
                table: "RoomTypes");

            migrationBuilder.DropColumn(
                name: "IsFreeCancellation",
                table: "RoomTypes");

            migrationBuilder.DropColumn(
                name: "IsPayAtHotel",
                table: "RoomTypes");

            migrationBuilder.DropColumn(
                name: "SizeSqm",
                table: "RoomTypes");
        }
    }
}
