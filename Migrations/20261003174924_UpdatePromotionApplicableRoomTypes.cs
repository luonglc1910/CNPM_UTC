using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HotelManagement.Web.Migrations
{
    /// <inheritdoc />
    public partial class UpdatePromotionApplicableRoomTypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Promotions_RoomTypes_RelatedRoomTypeId",
                table: "Promotions");

            migrationBuilder.RenameColumn(
                name: "RelatedRoomTypeId",
                table: "Promotions",
                newName: "RoomTypeId");

            migrationBuilder.RenameIndex(
                name: "IX_Promotions_RelatedRoomTypeId",
                table: "Promotions",
                newName: "IX_Promotions_RoomTypeId");

            migrationBuilder.AddColumn<string>(
                name: "ApplicableRoomTypeIds",
                table: "Promotions",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Promotions_RoomTypes_RoomTypeId",
                table: "Promotions",
                column: "RoomTypeId",
                principalTable: "RoomTypes",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Promotions_RoomTypes_RoomTypeId",
                table: "Promotions");

            migrationBuilder.DropColumn(
                name: "ApplicableRoomTypeIds",
                table: "Promotions");

            migrationBuilder.RenameColumn(
                name: "RoomTypeId",
                table: "Promotions",
                newName: "RelatedRoomTypeId");

            migrationBuilder.RenameIndex(
                name: "IX_Promotions_RoomTypeId",
                table: "Promotions",
                newName: "IX_Promotions_RelatedRoomTypeId");

            migrationBuilder.AddForeignKey(
                name: "FK_Promotions_RoomTypes_RelatedRoomTypeId",
                table: "Promotions",
                column: "RelatedRoomTypeId",
                principalTable: "RoomTypes",
                principalColumn: "Id");
        }
    }
}
