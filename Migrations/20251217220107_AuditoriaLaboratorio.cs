using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Proyecto_Laboratorios_Univalle.Migrations
{
    /// <inheritdoc />
    public partial class AuditoriaLaboratorio : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CreadoPorId",
                table: "Laboratorios",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ModificadoPorId",
                table: "Laboratorios",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UltimaModificacion",
                table: "Laboratorios",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Laboratorios_CreadoPorId",
                table: "Laboratorios",
                column: "CreadoPorId");

            migrationBuilder.CreateIndex(
                name: "IX_Laboratorios_ModificadoPorId",
                table: "Laboratorios",
                column: "ModificadoPorId");

            migrationBuilder.AddForeignKey(
                name: "FK_Laboratorios_Usuarios_CreadoPorId",
                table: "Laboratorios",
                column: "CreadoPorId",
                principalTable: "Usuarios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Laboratorios_Usuarios_ModificadoPorId",
                table: "Laboratorios",
                column: "ModificadoPorId",
                principalTable: "Usuarios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Laboratorios_Usuarios_CreadoPorId",
                table: "Laboratorios");

            migrationBuilder.DropForeignKey(
                name: "FK_Laboratorios_Usuarios_ModificadoPorId",
                table: "Laboratorios");

            migrationBuilder.DropIndex(
                name: "IX_Laboratorios_CreadoPorId",
                table: "Laboratorios");

            migrationBuilder.DropIndex(
                name: "IX_Laboratorios_ModificadoPorId",
                table: "Laboratorios");

            migrationBuilder.DropColumn(
                name: "CreadoPorId",
                table: "Laboratorios");

            migrationBuilder.DropColumn(
                name: "ModificadoPorId",
                table: "Laboratorios");

            migrationBuilder.DropColumn(
                name: "UltimaModificacion",
                table: "Laboratorios");
        }
    }
}
