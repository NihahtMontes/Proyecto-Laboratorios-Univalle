using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Proyecto_Laboratorios_Univalle.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddVerificationFaults : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "VerificationFaults",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VerificationId = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedById = table.Column<int>(type: "int", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedById = table.Column<int>(type: "int", nullable: true),
                    LastModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VerificationFaults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VerificationFaults_Users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_VerificationFaults_Users_ModifiedById",
                        column: x => x.ModifiedById,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_VerificationFaults_Verifications_VerificationId",
                        column: x => x.VerificationId,
                        principalTable: "Verifications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VerificationFaults_CreatedById",
                table: "VerificationFaults",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_VerificationFaults_ModifiedById",
                table: "VerificationFaults",
                column: "ModifiedById");

            migrationBuilder.CreateIndex(
                name: "IX_VerificationFaults_VerificationId",
                table: "VerificationFaults",
                column: "VerificationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VerificationFaults");
        }
    }
}
