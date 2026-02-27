using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Proyecto_Laboratorios_Univalle.Migrations
{
    /// <inheritdoc />
    public partial class Sprint0 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Career",
                table: "EquipmentUnits");

            migrationBuilder.AddColumn<int>(
                name: "CareerId",
                table: "EquipmentUnits",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Careers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    FacultadId = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    CreatedById = table.Column<int>(type: "int", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedById = table.Column<int>(type: "int", nullable: true),
                    LastModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Careers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Careers_Faculties_FacultadId",
                        column: x => x.FacultadId,
                        principalTable: "Faculties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Careers_Users_CreatedById",
                        column: x => x.CreatedById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Careers_Users_ModifiedById",
                        column: x => x.ModifiedById,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_EquipmentUnits_CareerId",
                table: "EquipmentUnits",
                column: "CareerId");

            migrationBuilder.CreateIndex(
                name: "IX_Careers_CreatedById",
                table: "Careers",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_Careers_FacultadId",
                table: "Careers",
                column: "FacultadId");

            migrationBuilder.CreateIndex(
                name: "IX_Careers_ModifiedById",
                table: "Careers",
                column: "ModifiedById");

            migrationBuilder.AddForeignKey(
                name: "FK_EquipmentUnits_Careers_CareerId",
                table: "EquipmentUnits",
                column: "CareerId",
                principalTable: "Careers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EquipmentUnits_Careers_CareerId",
                table: "EquipmentUnits");

            migrationBuilder.DropTable(
                name: "Careers");

            migrationBuilder.DropIndex(
                name: "IX_EquipmentUnits_CareerId",
                table: "EquipmentUnits");

            migrationBuilder.DropColumn(
                name: "CareerId",
                table: "EquipmentUnits");

            migrationBuilder.AddColumn<string>(
                name: "Career",
                table: "EquipmentUnits",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);
        }
    }
}
