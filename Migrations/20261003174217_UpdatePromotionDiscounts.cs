using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HotelManagement.Web.Migrations
{
    /// <inheritdoc />
    public partial class UpdatePromotionDiscounts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "DisplayPrice",
                table: "Promotions",
                newName: "DiscountOvernightPercent");

            migrationBuilder.RenameColumn(
                name: "DiscountPercentage",
                table: "Promotions",
                newName: "DiscountHourlyPercent");

            migrationBuilder.AddColumn<decimal>(
                name: "DiscountDailyPercent",
                table: "Promotions",
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
                name: "DiscountDailyPercent",
                table: "Promotions");

            migrationBuilder.RenameColumn(
                name: "DiscountOvernightPercent",
                table: "Promotions",
                newName: "DisplayPrice");

            migrationBuilder.RenameColumn(
                name: "DiscountHourlyPercent",
                table: "Promotions",
                newName: "DiscountPercentage");
        }
    }
}
