using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HotelManagement.Web.Migrations
{
    /// <inheritdoc />
    public partial class OperationalCore : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "AmountPaid",
                table: "Invoices",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "DebtAmount",
                table: "Invoices",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "NumberSequences",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Prefix = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Period = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    LastValue = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NumberSequences", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "UX_Housekeeping_OpenPerRoom",
                table: "HousekeepingTasks",
                column: "RoomId",
                unique: true,
                filter: "[Status] IN (1, 2)");

            migrationBuilder.CreateIndex(
                name: "UX_Shift_OpenPerEmployee",
                table: "CashierShifts",
                column: "EmployeeId",
                unique: true,
                filter: "[Status] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_NumberSequences_Prefix_Period",
                table: "NumberSequences",
                columns: new[] { "Prefix", "Period" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NumberSequences");

            migrationBuilder.DropIndex(
                name: "UX_Housekeeping_OpenPerRoom",
                table: "HousekeepingTasks");

            migrationBuilder.DropIndex(
                name: "UX_Shift_OpenPerEmployee",
                table: "CashierShifts");

            migrationBuilder.DropColumn(
                name: "AmountPaid",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "DebtAmount",
                table: "Invoices");
        }
    }
}
