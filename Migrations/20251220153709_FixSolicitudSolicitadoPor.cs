using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Proyecto_Laboratorios_Univalle.Migrations
{
    /// <inheritdoc />
    public partial class FixSolicitudSolicitadoPor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Solicitudes_Usuarios_SolicitadoPorId",
                table: "Solicitudes");

            migrationBuilder.DropIndex(
                name: "IX_Solicitudes_SolicitadoPorId",
                table: "Solicitudes");

            migrationBuilder.AddColumn<int>(
                name: "UsuarioId",
                table: "Solicitudes",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Solicitudes_UsuarioId",
                table: "Solicitudes",
                column: "UsuarioId");

            migrationBuilder.AddForeignKey(
                name: "FK_Solicitudes_Usuarios_UsuarioId",
                table: "Solicitudes",
                column: "UsuarioId",
                principalTable: "Usuarios",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Solicitudes_Usuarios_UsuarioId",
                table: "Solicitudes");

            migrationBuilder.DropIndex(
                name: "IX_Solicitudes_UsuarioId",
                table: "Solicitudes");

            migrationBuilder.DropColumn(
                name: "UsuarioId",
                table: "Solicitudes");

            migrationBuilder.CreateIndex(
                name: "IX_Solicitudes_SolicitadoPorId",
                table: "Solicitudes",
                column: "SolicitadoPorId");

            migrationBuilder.AddForeignKey(
                name: "FK_Solicitudes_Usuarios_SolicitadoPorId",
                table: "Solicitudes",
                column: "SolicitadoPorId",
                principalTable: "Usuarios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
