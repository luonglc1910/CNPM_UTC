using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HotelManagement.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAdditionalImageUrls : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AdditionalImageUrls",
                table: "RoomTypes",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AdditionalImageUrls",
                table: "RoomTypes");
        }
    }
}
