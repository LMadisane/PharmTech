using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PharmTech.Migrations
{
    /// <inheritdoc />
    public partial class RepairPrescriptionSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // The March migration created a shadow MedicineMedId FK and omitted FacilityId.
            // Preserve the related medicine value before removing the incorrect shadow column.
            migrationBuilder.DropForeignKey(
                name: "FK_Prescriptions_Medicines_MedicineMedId",
                table: "Prescriptions");

            migrationBuilder.DropForeignKey(
                name: "FK_Prescriptions_Users_PatientId",
                table: "Prescriptions");

            migrationBuilder.DropIndex(
                name: "IX_Prescriptions_MedicineMedId",
                table: "Prescriptions");

            migrationBuilder.Sql(
                "UPDATE `Prescriptions` SET `MedId` = `MedicineMedId` WHERE `MedicineMedId` IS NOT NULL;");

            migrationBuilder.DropColumn(
                name: "MedicineMedId",
                table: "Prescriptions");

            migrationBuilder.AddColumn<int>(
                name: "FacilityId",
                table: "Prescriptions",
                type: "int",
                nullable: true);

            migrationBuilder.Sql(
                "UPDATE `Prescriptions` p JOIN `Users` u ON p.`PatientId` = u.`UserId` SET p.`FacilityId` = u.`FacilityId` WHERE p.`FacilityId` IS NULL AND u.`FacilityId` IS NOT NULL;");

            migrationBuilder.Sql(
                "UPDATE `Prescriptions` SET `FacilityId` = (SELECT MIN(`FacilityId`) FROM `Facilities`) WHERE `FacilityId` IS NULL;");

            migrationBuilder.AlterColumn<int>(
                name: "FacilityId",
                table: "Prescriptions",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Prescriptions_FacilityId",
                table: "Prescriptions",
                column: "FacilityId");

            migrationBuilder.CreateIndex(
                name: "IX_Prescriptions_MedId",
                table: "Prescriptions",
                column: "MedId");

            migrationBuilder.AddForeignKey(
                name: "FK_Prescriptions_Facilities_FacilityId",
                table: "Prescriptions",
                column: "FacilityId",
                principalTable: "Facilities",
                principalColumn: "FacilityId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Prescriptions_Medicines_MedId",
                table: "Prescriptions",
                column: "MedId",
                principalTable: "Medicines",
                principalColumn: "MedId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Prescriptions_Users_PatientId",
                table: "Prescriptions",
                column: "PatientId",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Prescriptions_Facilities_FacilityId",
                table: "Prescriptions");

            migrationBuilder.DropForeignKey(
                name: "FK_Prescriptions_Medicines_MedId",
                table: "Prescriptions");

            migrationBuilder.DropForeignKey(
                name: "FK_Prescriptions_Users_PatientId",
                table: "Prescriptions");

            migrationBuilder.DropIndex(
                name: "IX_Prescriptions_FacilityId",
                table: "Prescriptions");

            migrationBuilder.DropIndex(
                name: "IX_Prescriptions_MedId",
                table: "Prescriptions");

            migrationBuilder.AddColumn<int>(
                name: "MedicineMedId",
                table: "Prescriptions",
                type: "int",
                nullable: true);

            migrationBuilder.Sql(
                "UPDATE `Prescriptions` SET `MedicineMedId` = `MedId`;");

            migrationBuilder.AlterColumn<int>(
                name: "MedicineMedId",
                table: "Prescriptions",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.DropColumn(
                name: "FacilityId",
                table: "Prescriptions");

            migrationBuilder.CreateIndex(
                name: "IX_Prescriptions_MedicineMedId",
                table: "Prescriptions",
                column: "MedicineMedId");

            migrationBuilder.AddForeignKey(
                name: "FK_Prescriptions_Medicines_MedicineMedId",
                table: "Prescriptions",
                column: "MedicineMedId",
                principalTable: "Medicines",
                principalColumn: "MedId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Prescriptions_Users_PatientId",
                table: "Prescriptions",
                column: "PatientId",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
