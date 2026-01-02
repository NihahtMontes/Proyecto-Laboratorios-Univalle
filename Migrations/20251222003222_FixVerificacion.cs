using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Proyecto_Laboratorios_Univalle.Migrations
{
    /// <inheritdoc />
    public partial class FixVerificacion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Verificaciones_Mantenimientos_IdMantenimiento",
                table: "Verificaciones");

            migrationBuilder.DropForeignKey(
                name: "FK_Verificaciones_Usuarios_AprobadoPorId",
                table: "Verificaciones");

            migrationBuilder.DropForeignKey(
                name: "FK_Verificaciones_Usuarios_IdTecnico",
                table: "Verificaciones");

            migrationBuilder.DropIndex(
                name: "IX_Verificaciones_AprobadoPorId",
                table: "Verificaciones");

            migrationBuilder.DropIndex(
                name: "IX_Verificaciones_IdMantenimiento",
                table: "Verificaciones");

            migrationBuilder.DropIndex(
                name: "IX_Verificaciones_IdTecnico",
                table: "Verificaciones");

            migrationBuilder.DropColumn(
                name: "AprobadoPorId",
                table: "Verificaciones");

            migrationBuilder.DropColumn(
                name: "FechaAprobacion",
                table: "Verificaciones");

            migrationBuilder.DropColumn(
                name: "IdMantenimiento",
                table: "Verificaciones");

            migrationBuilder.DropColumn(
                name: "IdTecnico",
                table: "Verificaciones");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AprobadoPorId",
                table: "Verificaciones",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaAprobacion",
                table: "Verificaciones",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "IdMantenimiento",
                table: "Verificaciones",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "IdTecnico",
                table: "Verificaciones",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Verificaciones_AprobadoPorId",
                table: "Verificaciones",
                column: "AprobadoPorId");

            migrationBuilder.CreateIndex(
                name: "IX_Verificaciones_IdMantenimiento",
                table: "Verificaciones",
                column: "IdMantenimiento");

            migrationBuilder.CreateIndex(
                name: "IX_Verificaciones_IdTecnico",
                table: "Verificaciones",
                column: "IdTecnico");

            migrationBuilder.AddForeignKey(
                name: "FK_Verificaciones_Mantenimientos_IdMantenimiento",
                table: "Verificaciones",
                column: "IdMantenimiento",
                principalTable: "Mantenimientos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Verificaciones_Usuarios_AprobadoPorId",
                table: "Verificaciones",
                column: "AprobadoPorId",
                principalTable: "Usuarios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Verificaciones_Usuarios_IdTecnico",
                table: "Verificaciones",
                column: "IdTecnico",
                principalTable: "Usuarios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
