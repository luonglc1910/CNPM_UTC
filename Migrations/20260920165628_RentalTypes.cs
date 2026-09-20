using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HotelManagement.Web.Migrations
{
    /// <inheritdoc />
    public partial class RentalTypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BilledHours",
                table: "Stays",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "PriceExtraHour",
                table: "Stays",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "PriceFirstHour",
                table: "Stays",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "PriceOvernight",
                table: "Stays",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            // Mặc định 1 = RentalType.Daily. Để 0 thì các bản ghi cũ mang một giá trị không ứng với
            // thành viên nào của enum, và mọi switch trên hình thức thuê sẽ rơi vào nhánh mặc định
            // một cách tình cờ chứ không phải do chủ đích.
            migrationBuilder.AddColumn<int>(
                name: "RentalType",
                table: "Stays",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<decimal>(
                name: "PriceExtraHour",
                table: "RoomTypes",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "PriceFirstHour",
                table: "RoomTypes",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "PriceOvernight",
                table: "RoomTypes",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "Hours",
                table: "Reservations",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "RentalType",
                table: "Reservations",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<decimal>(
                name: "PriceExtraHour",
                table: "ReservationRooms",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "PriceFirstHour",
                table: "ReservationRooms",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "PriceOvernight",
                table: "ReservationRooms",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BilledHours",
                table: "Stays");

            migrationBuilder.DropColumn(
                name: "PriceExtraHour",
                table: "Stays");

            migrationBuilder.DropColumn(
                name: "PriceFirstHour",
                table: "Stays");

            migrationBuilder.DropColumn(
                name: "PriceOvernight",
                table: "Stays");

            migrationBuilder.DropColumn(
                name: "RentalType",
                table: "Stays");

            migrationBuilder.DropColumn(
                name: "PriceExtraHour",
                table: "RoomTypes");

            migrationBuilder.DropColumn(
                name: "PriceFirstHour",
                table: "RoomTypes");

            migrationBuilder.DropColumn(
                name: "PriceOvernight",
                table: "RoomTypes");

            migrationBuilder.DropColumn(
                name: "Hours",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "RentalType",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "PriceExtraHour",
                table: "ReservationRooms");

            migrationBuilder.DropColumn(
                name: "PriceFirstHour",
                table: "ReservationRooms");

            migrationBuilder.DropColumn(
                name: "PriceOvernight",
                table: "ReservationRooms");
        }
    }
}
