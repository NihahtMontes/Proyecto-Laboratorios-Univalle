using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Proyecto_Laboratorios_Univalle.Migrations
{
    /// <inheritdoc />
    public partial class OptimizacionSolicitudMantenimiento : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Mantenimientos_Solicitudes_IdSolicitud",
                table: "Mantenimientos");

            migrationBuilder.DropForeignKey(
                name: "FK_Solicitudes_Usuarios_IdTecnico",
                table: "Solicitudes");

            migrationBuilder.DropIndex(
                name: "IX_Solicitudes_IdTecnico",
                table: "Solicitudes");

            migrationBuilder.DropIndex(
                name: "IX_Mantenimientos_IdSolicitud",
                table: "Mantenimientos");

            migrationBuilder.DropColumn(
                name: "FechaAtencion",
                table: "Solicitudes");

            migrationBuilder.DropColumn(
                name: "FechaProgramada",
                table: "Solicitudes");

            migrationBuilder.DropColumn(
                name: "IdTecnico",
                table: "Solicitudes");

            migrationBuilder.DropColumn(
                name: "PresupuestoEstimado",
                table: "Solicitudes");

            migrationBuilder.AlterColumn<string>(
                name: "Prioridad",
                table: "Solicitudes",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Observaciones",
                table: "Solicitudes",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(1000)",
                oldMaxLength: 1000,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Mantenimientos_IdSolicitud",
                table: "Mantenimientos",
                column: "IdSolicitud",
                unique: true,
                filter: "[IdSolicitud] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_Mantenimientos_Solicitudes_IdSolicitud",
                table: "Mantenimientos",
                column: "IdSolicitud",
                principalTable: "Solicitudes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Mantenimientos_Solicitudes_IdSolicitud",
                table: "Mantenimientos");

            migrationBuilder.DropIndex(
                name: "IX_Mantenimientos_IdSolicitud",
                table: "Mantenimientos");

            migrationBuilder.AlterColumn<string>(
                name: "Prioridad",
                table: "Solicitudes",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Observaciones",
                table: "Solicitudes",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaAtencion",
                table: "Solicitudes",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaProgramada",
                table: "Solicitudes",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "IdTecnico",
                table: "Solicitudes",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PresupuestoEstimado",
                table: "Solicitudes",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Solicitudes_IdTecnico",
                table: "Solicitudes",
                column: "IdTecnico");

            migrationBuilder.CreateIndex(
                name: "IX_Mantenimientos_IdSolicitud",
                table: "Mantenimientos",
                column: "IdSolicitud");

            migrationBuilder.AddForeignKey(
                name: "FK_Mantenimientos_Solicitudes_IdSolicitud",
                table: "Mantenimientos",
                column: "IdSolicitud",
                principalTable: "Solicitudes",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Solicitudes_Usuarios_IdTecnico",
                table: "Solicitudes",
                column: "IdTecnico",
                principalTable: "Usuarios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
