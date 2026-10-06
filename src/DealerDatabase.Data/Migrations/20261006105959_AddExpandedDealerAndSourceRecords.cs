using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DealerDatabase.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddExpandedDealerAndSourceRecords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Name",
                table: "Dealers",
                newName: "LegalName");

            migrationBuilder.AddColumn<string>(
                name: "CompanyRegistrationNumber",
                table: "Dealers",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CompanyStatus",
                table: "Dealers",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "Dealers",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FcaReferenceNumber",
                table: "Dealers",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FcaStatus",
                table: "Dealers",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "IcoExpiry",
                table: "Dealers",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IcoRegistrationNumber",
                table: "Dealers",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "IncorporationDate",
                table: "Dealers",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Postcode",
                table: "Dealers",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RegisteredAddress",
                table: "Dealers",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SafExpiry",
                table: "Dealers",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SafStatus",
                table: "Dealers",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StockCount",
                table: "Dealers",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Telephone",
                table: "Dealers",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TradingAddress",
                table: "Dealers",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TradingName",
                table: "Dealers",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "VatIsValid",
                table: "Dealers",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VatNumber",
                table: "Dealers",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Website",
                table: "Dealers",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SourceRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DealerId = table.Column<int>(type: "INTEGER", nullable: false),
                    SourceSystem = table.Column<string>(type: "TEXT", nullable: false),
                    SourceRecordId = table.Column<string>(type: "TEXT", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SourceRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SourceRecords_Dealers_DealerId",
                        column: x => x.DealerId,
                        principalTable: "Dealers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Dealers_CompanyRegistrationNumber",
                table: "Dealers",
                column: "CompanyRegistrationNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SourceRecords_DealerId",
                table: "SourceRecords",
                column: "DealerId");

            migrationBuilder.CreateIndex(
                name: "IX_SourceRecords_SourceSystem_SourceRecordId",
                table: "SourceRecords",
                columns: new[] { "SourceSystem", "SourceRecordId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SourceRecords");

            migrationBuilder.DropIndex(
                name: "IX_Dealers_CompanyRegistrationNumber",
                table: "Dealers");

            migrationBuilder.DropColumn(
                name: "CompanyRegistrationNumber",
                table: "Dealers");

            migrationBuilder.DropColumn(
                name: "CompanyStatus",
                table: "Dealers");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "Dealers");

            migrationBuilder.DropColumn(
                name: "FcaReferenceNumber",
                table: "Dealers");

            migrationBuilder.DropColumn(
                name: "FcaStatus",
                table: "Dealers");

            migrationBuilder.DropColumn(
                name: "IcoExpiry",
                table: "Dealers");

            migrationBuilder.DropColumn(
                name: "IcoRegistrationNumber",
                table: "Dealers");

            migrationBuilder.DropColumn(
                name: "IncorporationDate",
                table: "Dealers");

            migrationBuilder.DropColumn(
                name: "Postcode",
                table: "Dealers");

            migrationBuilder.DropColumn(
                name: "RegisteredAddress",
                table: "Dealers");

            migrationBuilder.DropColumn(
                name: "SafExpiry",
                table: "Dealers");

            migrationBuilder.DropColumn(
                name: "SafStatus",
                table: "Dealers");

            migrationBuilder.DropColumn(
                name: "StockCount",
                table: "Dealers");

            migrationBuilder.DropColumn(
                name: "Telephone",
                table: "Dealers");

            migrationBuilder.DropColumn(
                name: "TradingAddress",
                table: "Dealers");

            migrationBuilder.DropColumn(
                name: "TradingName",
                table: "Dealers");

            migrationBuilder.DropColumn(
                name: "VatIsValid",
                table: "Dealers");

            migrationBuilder.DropColumn(
                name: "VatNumber",
                table: "Dealers");

            migrationBuilder.DropColumn(
                name: "Website",
                table: "Dealers");

            migrationBuilder.RenameColumn(
                name: "LegalName",
                table: "Dealers",
                newName: "Name");
        }
    }
}
