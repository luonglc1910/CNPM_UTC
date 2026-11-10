using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HotelManagement.Web.Migrations
{
    /// <inheritdoc />
    public partial class UpdatePromotionDbSets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RoomTypeAmenities_Amenities_AmenityId",
                table: "RoomTypeAmenities");

            migrationBuilder.DropForeignKey(
                name: "FK_RoomTypeAmenities_RoomTypes_RoomTypeId",
                table: "RoomTypeAmenities");

            migrationBuilder.DropPrimaryKey(
                name: "PK_RoomTypeAmenities",
                table: "RoomTypeAmenities");

            migrationBuilder.RenameTable(
                name: "RoomTypeAmenities",
                newName: "RoomTypeAmenity");

            migrationBuilder.RenameIndex(
                name: "IX_RoomTypeAmenities_AmenityId",
                table: "RoomTypeAmenity",
                newName: "IX_RoomTypeAmenity_AmenityId");

            migrationBuilder.AddColumn<decimal>(
                name: "DiscountPercentage",
                table: "Promotions",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "PromoCode",
                table: "Promotions",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_RoomTypeAmenity",
                table: "RoomTypeAmenity",
                columns: new[] { "RoomTypeId", "AmenityId" });

            migrationBuilder.AddForeignKey(
                name: "FK_RoomTypeAmenity_Amenities_AmenityId",
                table: "RoomTypeAmenity",
                column: "AmenityId",
                principalTable: "Amenities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RoomTypeAmenity_RoomTypes_RoomTypeId",
                table: "RoomTypeAmenity",
                column: "RoomTypeId",
                principalTable: "RoomTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RoomTypeAmenity_Amenities_AmenityId",
                table: "RoomTypeAmenity");

            migrationBuilder.DropForeignKey(
                name: "FK_RoomTypeAmenity_RoomTypes_RoomTypeId",
                table: "RoomTypeAmenity");

            migrationBuilder.DropPrimaryKey(
                name: "PK_RoomTypeAmenity",
                table: "RoomTypeAmenity");

            migrationBuilder.DropColumn(
                name: "DiscountPercentage",
                table: "Promotions");

            migrationBuilder.DropColumn(
                name: "PromoCode",
                table: "Promotions");

            migrationBuilder.RenameTable(
                name: "RoomTypeAmenity",
                newName: "RoomTypeAmenities");

            migrationBuilder.RenameIndex(
                name: "IX_RoomTypeAmenity_AmenityId",
                table: "RoomTypeAmenities",
                newName: "IX_RoomTypeAmenities_AmenityId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_RoomTypeAmenities",
                table: "RoomTypeAmenities",
                columns: new[] { "RoomTypeId", "AmenityId" });

            migrationBuilder.AddForeignKey(
                name: "FK_RoomTypeAmenities_Amenities_AmenityId",
                table: "RoomTypeAmenities",
                column: "AmenityId",
                principalTable: "Amenities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RoomTypeAmenities_RoomTypes_RoomTypeId",
                table: "RoomTypeAmenities",
                column: "RoomTypeId",
                principalTable: "RoomTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
