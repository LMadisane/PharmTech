using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PharmTech.Migrations
{
    /// <inheritdoc />
    public partial class AddReceiptAndDispensingUpdates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExpiryDate",
                table: "Medicines");

            migrationBuilder.DropColumn(
                name: "LotNumber",
                table: "Medicines");

            migrationBuilder.AddColumn<int>(
                name: "FacilityId",
                table: "DrugReturns",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "MedicineBatches",
                columns: table => new
                {
                    BatchId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    MedId = table.Column<int>(type: "int", nullable: false),
                    FacilityId = table.Column<int>(type: "int", nullable: false),
                    LotNumber = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    ExpiryDate = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    DateReceived = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    IsFlagged = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MedicineBatches", x => x.BatchId);
                    table.ForeignKey(
                        name: "FK_MedicineBatches_Facilities_FacilityId",
                        column: x => x.FacilityId,
                        principalTable: "Facilities",
                        principalColumn: "FacilityId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MedicineBatches_Medicines_MedId",
                        column: x => x.MedId,
                        principalTable: "Medicines",
                        principalColumn: "MedId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "DisposalRecords",
                columns: table => new
                {
                    DisposalId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    MedId = table.Column<int>(type: "int", nullable: false),
                    FacilityId = table.Column<int>(type: "int", nullable: false),
                    BatchId = table.Column<int>(type: "int", nullable: true),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RecordedById = table.Column<int>(type: "int", nullable: false),
                    RecordedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Notes = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DisposalRecords", x => x.DisposalId);
                    table.ForeignKey(
                        name: "FK_DisposalRecords_Facilities_FacilityId",
                        column: x => x.FacilityId,
                        principalTable: "Facilities",
                        principalColumn: "FacilityId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DisposalRecords_MedicineBatches_BatchId",
                        column: x => x.BatchId,
                        principalTable: "MedicineBatches",
                        principalColumn: "BatchId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_DisposalRecords_Medicines_MedId",
                        column: x => x.MedId,
                        principalTable: "Medicines",
                        principalColumn: "MedId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DisposalRecords_Users_RecordedById",
                        column: x => x.RecordedById,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_DrugReturns_FacilityId",
                table: "DrugReturns",
                column: "FacilityId");

            migrationBuilder.CreateIndex(
                name: "IX_DisposalRecords_BatchId",
                table: "DisposalRecords",
                column: "BatchId");

            migrationBuilder.CreateIndex(
                name: "IX_DisposalRecords_FacilityId",
                table: "DisposalRecords",
                column: "FacilityId");

            migrationBuilder.CreateIndex(
                name: "IX_DisposalRecords_MedId",
                table: "DisposalRecords",
                column: "MedId");

            migrationBuilder.CreateIndex(
                name: "IX_DisposalRecords_RecordedById",
                table: "DisposalRecords",
                column: "RecordedById");

            migrationBuilder.CreateIndex(
                name: "IX_MedicineBatches_FacilityId",
                table: "MedicineBatches",
                column: "FacilityId");

            migrationBuilder.CreateIndex(
                name: "IX_MedicineBatches_MedId_FacilityId",
                table: "MedicineBatches",
                columns: new[] { "MedId", "FacilityId" });

            migrationBuilder.AddForeignKey(
                name: "FK_DrugReturns_Facilities_FacilityId",
                table: "DrugReturns",
                column: "FacilityId",
                principalTable: "Facilities",
                principalColumn: "FacilityId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DrugReturns_Facilities_FacilityId",
                table: "DrugReturns");

            migrationBuilder.DropTable(
                name: "DisposalRecords");

            migrationBuilder.DropTable(
                name: "MedicineBatches");

            migrationBuilder.DropIndex(
                name: "IX_DrugReturns_FacilityId",
                table: "DrugReturns");

            migrationBuilder.DropColumn(
                name: "FacilityId",
                table: "DrugReturns");

            migrationBuilder.AddColumn<DateTime>(
                name: "ExpiryDate",
                table: "Medicines",
                type: "datetime(6)",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "LotNumber",
                table: "Medicines",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");
        }
    }
}
