using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Proyecto_Laboratorios_Univalle.Migrations
{
    /// <inheritdoc />
    public partial class AddInterfacesFixingDataIssues1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_People_Users_UserId",
                table: "People");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "People",
                newName: "ModifiedById");

            migrationBuilder.RenameIndex(
                name: "IX_People_UserId",
                table: "People",
                newName: "IX_People_ModifiedById");

            migrationBuilder.AlterColumn<string>(
                name: "IdentityCard",
                table: "Users",
                type: "nvarchar(8)",
                maxLength: 8,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20);

            migrationBuilder.AddColumn<int>(
                name: "CreatedById",
                table: "People",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastModifiedDate",
                table: "People",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_People_CreatedById",
                table: "People",
                column: "CreatedById");

            migrationBuilder.AddForeignKey(
                name: "FK_People_Users_CreatedById",
                table: "People",
                column: "CreatedById",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_People_Users_ModifiedById",
                table: "People",
                column: "ModifiedById",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_People_Users_CreatedById",
                table: "People");

            migrationBuilder.DropForeignKey(
                name: "FK_People_Users_ModifiedById",
                table: "People");

            migrationBuilder.DropIndex(
                name: "IX_People_CreatedById",
                table: "People");

            migrationBuilder.DropColumn(
                name: "CreatedById",
                table: "People");

            migrationBuilder.DropColumn(
                name: "LastModifiedDate",
                table: "People");

            migrationBuilder.RenameColumn(
                name: "ModifiedById",
                table: "People",
                newName: "UserId");

            migrationBuilder.RenameIndex(
                name: "IX_People_ModifiedById",
                table: "People",
                newName: "IX_People_UserId");

            migrationBuilder.AlterColumn<string>(
                name: "IdentityCard",
                table: "Users",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(8)",
                oldMaxLength: 8);

            migrationBuilder.AddForeignKey(
                name: "FK_People_Users_UserId",
                table: "People",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
