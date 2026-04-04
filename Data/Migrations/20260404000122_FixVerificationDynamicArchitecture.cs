using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Proyecto_Laboratorios_Univalle.Data.Migrations
{
    /// <inheritdoc />
    public partial class FixVerificationDynamicArchitecture : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BurnerCheck",
                table: "Verifications");

            migrationBuilder.DropColumn(
                name: "CablingCheck",
                table: "Verifications");

            migrationBuilder.DropColumn(
                name: "CombustionFlameCheck",
                table: "Verifications");

            migrationBuilder.DropColumn(
                name: "CriticalFindings",
                table: "Verifications");

            migrationBuilder.DropColumn(
                name: "ElectrodeIgniterCheck",
                table: "Verifications");

            migrationBuilder.DropColumn(
                name: "ExternalCleaningCheck",
                table: "Verifications");

            migrationBuilder.DropColumn(
                name: "FanCheck",
                table: "Verifications");

            migrationBuilder.DropColumn(
                name: "FlameSensorCheck",
                table: "Verifications");

            migrationBuilder.DropColumn(
                name: "GasHoseCheck",
                table: "Verifications");

            migrationBuilder.DropColumn(
                name: "HeatExchangerCheck",
                table: "Verifications");

            migrationBuilder.DropColumn(
                name: "HighTempSteamCheck",
                table: "Verifications");

            migrationBuilder.DropColumn(
                name: "InternalCleaningCheck",
                table: "Verifications");

            migrationBuilder.DropColumn(
                name: "LedDisplayCheck",
                table: "Verifications");

            migrationBuilder.DropColumn(
                name: "LightsCheck",
                table: "Verifications");

            migrationBuilder.DropColumn(
                name: "LubricationCheck",
                table: "Verifications");

            migrationBuilder.DropColumn(
                name: "OvenIgnitionCheck",
                table: "Verifications");

            migrationBuilder.DropColumn(
                name: "Recommendations",
                table: "Verifications");

            migrationBuilder.DropColumn(
                name: "SolenoidValveCheck",
                table: "Verifications");

            migrationBuilder.DropColumn(
                name: "SoundAlarmCheck",
                table: "Verifications");

            migrationBuilder.DropColumn(
                name: "SteamOutletCheck",
                table: "Verifications");

            migrationBuilder.DropColumn(
                name: "TemperatureControlCheck",
                table: "Verifications");

            migrationBuilder.DropColumn(
                name: "ThermocoupleCheck",
                table: "Verifications");

            migrationBuilder.RenameColumn(
                name: "WaterHoseCheck",
                table: "Verifications",
                newName: "PhysicalCondition");

            migrationBuilder.AlterColumn<string>(
                name: "Observations",
                table: "Verifications",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(2000)",
                oldMaxLength: 2000,
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "VerificationCheckItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Order = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VerificationCheckItems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VerificationCheckResults",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VerificationId = table.Column<int>(type: "int", nullable: false),
                    CheckItemId = table.Column<int>(type: "int", nullable: false),
                    Result = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VerificationCheckResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VerificationCheckResults_VerificationCheckItems_CheckItemId",
                        column: x => x.CheckItemId,
                        principalTable: "VerificationCheckItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VerificationCheckResults_Verifications_VerificationId",
                        column: x => x.VerificationId,
                        principalTable: "Verifications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "VerificationCheckItems",
                columns: new[] { "Id", "Category", "IsActive", "Name", "Order" },
                values: new object[,]
                {
                    { 1, "Seguridad", true, "Desconexion del cable de alimentacion electrica para mantenimiento 12 horas antes", 1 },
                    { 2, "Higiene", true, "Limpieza y desinfeccion interna con productos no abrasivos", 2 },
                    { 3, "Higiene", true, "Limpieza externa de condensador, serpentin, evaporador y retiro de polvo y grasas", 3 },
                    { 4, "Refrigeracion", true, "Verificacion de presion del refrigerante", 4 },
                    { 5, "Refrigeracion", true, "Revision de fugas y/o microfugas en serpentin", 5 },
                    { 6, "Refrigeracion", true, "Revision de formaciones de hielo y condensaciones superficiales no esporadicas", 6 },
                    { 7, "Control", true, "Control de temperatura y termostatos segun norma", 7 },
                    { 8, "Mecanica", true, "Revision de puertas y sellos de goma (empaques)", 8 },
                    { 9, "Higiene", true, "Limpieza de drenajes de deshielo", 9 },
                    { 10, "Mecanica", true, "Verificacion del funcionamiento de ventiladores", 10 },
                    { 11, "Electrico", true, "Mantenimiento electrico: inspeccion de cableado, terminales, protecciones electricas", 11 },
                    { 12, "Mecanica", true, "Lubricacion de partes moviles", 12 },
                    { 13, "Gestion", true, "Mantenimiento con personal externo capacitado", 13 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_VerificationCheckResults_CheckItemId",
                table: "VerificationCheckResults",
                column: "CheckItemId");

            migrationBuilder.CreateIndex(
                name: "IX_VerificationCheckResults_VerificationId",
                table: "VerificationCheckResults",
                column: "VerificationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VerificationCheckResults");

            migrationBuilder.DropTable(
                name: "VerificationCheckItems");

            migrationBuilder.RenameColumn(
                name: "PhysicalCondition",
                table: "Verifications",
                newName: "WaterHoseCheck");

            migrationBuilder.AlterColumn<string>(
                name: "Observations",
                table: "Verifications",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BurnerCheck",
                table: "Verifications",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CablingCheck",
                table: "Verifications",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CombustionFlameCheck",
                table: "Verifications",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "CriticalFindings",
                table: "Verifications",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ElectrodeIgniterCheck",
                table: "Verifications",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ExternalCleaningCheck",
                table: "Verifications",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "FanCheck",
                table: "Verifications",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "FlameSensorCheck",
                table: "Verifications",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "GasHoseCheck",
                table: "Verifications",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "HeatExchangerCheck",
                table: "Verifications",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "HighTempSteamCheck",
                table: "Verifications",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "InternalCleaningCheck",
                table: "Verifications",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "LedDisplayCheck",
                table: "Verifications",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "LightsCheck",
                table: "Verifications",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "LubricationCheck",
                table: "Verifications",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "OvenIgnitionCheck",
                table: "Verifications",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Recommendations",
                table: "Verifications",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SolenoidValveCheck",
                table: "Verifications",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SoundAlarmCheck",
                table: "Verifications",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SteamOutletCheck",
                table: "Verifications",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TemperatureControlCheck",
                table: "Verifications",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ThermocoupleCheck",
                table: "Verifications",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
