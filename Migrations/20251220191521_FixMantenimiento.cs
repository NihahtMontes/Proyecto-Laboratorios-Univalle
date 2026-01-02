using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Proyecto_Laboratorios_Univalle.Migrations
{
    /// <inheritdoc />
    public partial class FixMantenimiento : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Mantenimientos_Usuarios_AprobadoPorId",
                table: "Mantenimientos");

            migrationBuilder.DropColumn(
                name: "FechaAprobacion",
                table: "Mantenimientos");

            migrationBuilder.AddForeignKey(
                name: "FK_Mantenimientos_Usuarios_AprobadoPorId",
                table: "Mantenimientos",
                column: "AprobadoPorId",
                principalTable: "Usuarios",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Mantenimientos_Usuarios_AprobadoPorId",
                table: "Mantenimientos");

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaAprobacion",
                table: "Mantenimientos",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Mantenimientos_Usuarios_AprobadoPorId",
                table: "Mantenimientos",
                column: "AprobadoPorId",
                principalTable: "Usuarios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
