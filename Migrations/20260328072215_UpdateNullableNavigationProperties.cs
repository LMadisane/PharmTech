using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PharmTech.Migrations
{
    /// <inheritdoc />
    public partial class UpdateNullableNavigationProperties : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Id",
                table: "Users",
                newName: "UserId");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "OrderRequests",
                newName: "OrderId");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "Medicines",
                newName: "MedId");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "InventoryItems",
                newName: "InventoryId");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "Facilities",
                newName: "FacilityId");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "DrugReturns",
                newName: "ReturnId");

            migrationBuilder.AddColumn<int>(
                name: "SupplierId",
                table: "OrderRequests",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LotNumber",
                table: "Medicines",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "MedicineMedId",
                table: "InventoryItems",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Prescriptions",
                columns: table => new
                {
                    PrescriptionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    PatientId = table.Column<int>(type: "int", nullable: false),
                    MedId = table.Column<int>(type: "int", nullable: false),
                    MedicineMedId = table.Column<int>(type: "int", nullable: false),
                    DosagePerDay = table.Column<int>(type: "int", nullable: false),
                    DurationDays = table.Column<int>(type: "int", nullable: false),
                    PrescribedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ReferenceCode = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Status = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Prescriptions", x => x.PrescriptionId);
                    table.ForeignKey(
                        name: "FK_Prescriptions_Medicines_MedicineMedId",
                        column: x => x.MedicineMedId,
                        principalTable: "Medicines",
                        principalColumn: "MedId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Prescriptions_Users_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Suppliers",
                columns: table => new
                {
                    SupplierId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ContactInfo = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Address = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Suppliers", x => x.SupplierId);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "DispenseRecords",
                columns: table => new
                {
                    DispenseRecordId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    PrescriptionId = table.Column<int>(type: "int", nullable: false),
                    DispensedById = table.Column<int>(type: "int", nullable: false),
                    FacilityId = table.Column<int>(type: "int", nullable: false),
                    QuantityDispensed = table.Column<int>(type: "int", nullable: false),
                    DispensedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DispenseRecords", x => x.DispenseRecordId);
                    table.ForeignKey(
                        name: "FK_DispenseRecords_Facilities_FacilityId",
                        column: x => x.FacilityId,
                        principalTable: "Facilities",
                        principalColumn: "FacilityId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DispenseRecords_Prescriptions_PrescriptionId",
                        column: x => x.PrescriptionId,
                        principalTable: "Prescriptions",
                        principalColumn: "PrescriptionId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DispenseRecords_Users_DispensedById",
                        column: x => x.DispensedById,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_OrderRequests_SupplierId",
                table: "OrderRequests",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryItems_MedicineMedId",
                table: "InventoryItems",
                column: "MedicineMedId");

            migrationBuilder.CreateIndex(
                name: "IX_DispenseRecords_DispensedById",
                table: "DispenseRecords",
                column: "DispensedById");

            migrationBuilder.CreateIndex(
                name: "IX_DispenseRecords_FacilityId",
                table: "DispenseRecords",
                column: "FacilityId");

            migrationBuilder.CreateIndex(
                name: "IX_DispenseRecords_PrescriptionId",
                table: "DispenseRecords",
                column: "PrescriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_Prescriptions_MedicineMedId",
                table: "Prescriptions",
                column: "MedicineMedId");

            migrationBuilder.CreateIndex(
                name: "IX_Prescriptions_PatientId",
                table: "Prescriptions",
                column: "PatientId");

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryItems_Medicines_MedicineMedId",
                table: "InventoryItems",
                column: "MedicineMedId",
                principalTable: "Medicines",
                principalColumn: "MedId");

            migrationBuilder.AddForeignKey(
                name: "FK_OrderRequests_Suppliers_SupplierId",
                table: "OrderRequests",
                column: "SupplierId",
                principalTable: "Suppliers",
                principalColumn: "SupplierId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InventoryItems_Medicines_MedicineMedId",
                table: "InventoryItems");

            migrationBuilder.DropForeignKey(
                name: "FK_OrderRequests_Suppliers_SupplierId",
                table: "OrderRequests");

            migrationBuilder.DropTable(
                name: "DispenseRecords");

            migrationBuilder.DropTable(
                name: "Suppliers");

            migrationBuilder.DropTable(
                name: "Prescriptions");

            migrationBuilder.DropIndex(
                name: "IX_OrderRequests_SupplierId",
                table: "OrderRequests");

            migrationBuilder.DropIndex(
                name: "IX_InventoryItems_MedicineMedId",
                table: "InventoryItems");

            migrationBuilder.DropColumn(
                name: "SupplierId",
                table: "OrderRequests");

            migrationBuilder.DropColumn(
                name: "LotNumber",
                table: "Medicines");

            migrationBuilder.DropColumn(
                name: "MedicineMedId",
                table: "InventoryItems");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "Users",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "OrderId",
                table: "OrderRequests",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "MedId",
                table: "Medicines",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "InventoryId",
                table: "InventoryItems",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "FacilityId",
                table: "Facilities",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "ReturnId",
                table: "DrugReturns",
                newName: "Id");
        }
    }
}
