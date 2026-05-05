using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PharmTech.Migrations
{
    /// <inheritdoc />
    public partial class AddFacilityIdToReceipts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FacilityId",
                table: "Receipts",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Receipts_FacilityId",
                table: "Receipts",
                column: "FacilityId");

            migrationBuilder.AddForeignKey(
                name: "FK_Receipts_Facilities_FacilityId",
                table: "Receipts",
                column: "FacilityId",
                principalTable: "Facilities",
                principalColumn: "FacilityId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Receipts_Facilities_FacilityId",
                table: "Receipts");

            migrationBuilder.DropIndex(
                name: "IX_Receipts_FacilityId",
                table: "Receipts");

            migrationBuilder.DropColumn(
                name: "FacilityId",
                table: "Receipts");
        }
    }
}
