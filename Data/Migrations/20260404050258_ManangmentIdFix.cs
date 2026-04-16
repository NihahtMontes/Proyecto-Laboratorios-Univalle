using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Proyecto_Laboratorios_Univalle.Data.Migrations
{
    /// <inheritdoc />
    public partial class ManangmentIdFix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ManagementId",
                table: "Verifications",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ManagementId",
                table: "Requests",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ManagementId",
                table: "Maintenances",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ManagementId",
                table: "EquipmentUnits",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ManagementId",
                table: "Departures",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.UpdateData(
                table: "VerificationCheckItems",
                keyColumn: "Id",
                keyValue: 1,
                column: "Name",
                value: "Desconexión del cable de la alimentación eléctrica para mantenimiento preventivo/correctivo 12 horas antes.");

            migrationBuilder.UpdateData(
                table: "VerificationCheckItems",
                keyColumn: "Id",
                keyValue: 2,
                column: "Name",
                value: "Limpieza y desinfección interna con productos no abrasivos.");

            migrationBuilder.UpdateData(
                table: "VerificationCheckItems",
                keyColumn: "Id",
                keyValue: 3,
                column: "Name",
                value: "Limpieza externa de condensador, serpentín, evaporador y retiro de polvo y grasas adheridas.");

            migrationBuilder.UpdateData(
                table: "VerificationCheckItems",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "Category", "Name" },
                values: new object[] { "Refrigeración", "Verificación de presión del refrigerante." });

            migrationBuilder.UpdateData(
                table: "VerificationCheckItems",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "Category", "Name" },
                values: new object[] { "Refrigeración", "Revisión de fugas y/o microfugas en serpentín." });

            migrationBuilder.UpdateData(
                table: "VerificationCheckItems",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "Category", "Name" },
                values: new object[] { "Refrigeración", "Revisión de formaciones de hielo y condensaciones superficiales no esporádicas." });

            migrationBuilder.UpdateData(
                table: "VerificationCheckItems",
                keyColumn: "Id",
                keyValue: 7,
                column: "Name",
                value: "Control de temperatura y termostatos según norma.");

            migrationBuilder.UpdateData(
                table: "VerificationCheckItems",
                keyColumn: "Id",
                keyValue: 8,
                columns: new[] { "Category", "Name" },
                values: new object[] { "Mecánica", "Revisión de puertas y sellos de goma (empaques)." });

            migrationBuilder.UpdateData(
                table: "VerificationCheckItems",
                keyColumn: "Id",
                keyValue: 9,
                column: "Name",
                value: "Limpieza de drenajes de deshielo.");

            migrationBuilder.UpdateData(
                table: "VerificationCheckItems",
                keyColumn: "Id",
                keyValue: 10,
                columns: new[] { "Category", "Name" },
                values: new object[] { "Mecánica", "Verificación del funcionamiento de ventiladores." });

            migrationBuilder.UpdateData(
                table: "VerificationCheckItems",
                keyColumn: "Id",
                keyValue: 11,
                columns: new[] { "Category", "Name" },
                values: new object[] { "Eléctrico", "Mantenimiento eléctrico: inspección de cableado, terminales, protecciones eléctricas, etc." });

            migrationBuilder.UpdateData(
                table: "VerificationCheckItems",
                keyColumn: "Id",
                keyValue: 12,
                columns: new[] { "Category", "Name" },
                values: new object[] { "Mecánica", "Lubricación de partes móviles." });

            migrationBuilder.UpdateData(
                table: "VerificationCheckItems",
                keyColumn: "Id",
                keyValue: 13,
                columns: new[] { "Category", "Name" },
                values: new object[] { "Gestión", "Mantenimiento con personal externo capacitado." });

            migrationBuilder.CreateIndex(
                name: "IX_Verifications_ManagementId",
                table: "Verifications",
                column: "ManagementId");

            migrationBuilder.CreateIndex(
                name: "IX_Requests_ManagementId",
                table: "Requests",
                column: "ManagementId");

            migrationBuilder.CreateIndex(
                name: "IX_Maintenances_ManagementId",
                table: "Maintenances",
                column: "ManagementId");

            migrationBuilder.CreateIndex(
                name: "IX_EquipmentUnits_ManagementId",
                table: "EquipmentUnits",
                column: "ManagementId");

            migrationBuilder.CreateIndex(
                name: "IX_Departures_ManagementId",
                table: "Departures",
                column: "ManagementId");

            migrationBuilder.AddForeignKey(
                name: "FK_Departures_Managements_ManagementId",
                table: "Departures",
                column: "ManagementId",
                principalTable: "Managements",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_EquipmentUnits_Managements_ManagementId",
                table: "EquipmentUnits",
                column: "ManagementId",
                principalTable: "Managements",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Maintenances_Managements_ManagementId",
                table: "Maintenances",
                column: "ManagementId",
                principalTable: "Managements",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Requests_Managements_ManagementId",
                table: "Requests",
                column: "ManagementId",
                principalTable: "Managements",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Verifications_Managements_ManagementId",
                table: "Verifications",
                column: "ManagementId",
                principalTable: "Managements",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Departures_Managements_ManagementId",
                table: "Departures");

            migrationBuilder.DropForeignKey(
                name: "FK_EquipmentUnits_Managements_ManagementId",
                table: "EquipmentUnits");

            migrationBuilder.DropForeignKey(
                name: "FK_Maintenances_Managements_ManagementId",
                table: "Maintenances");

            migrationBuilder.DropForeignKey(
                name: "FK_Requests_Managements_ManagementId",
                table: "Requests");

            migrationBuilder.DropForeignKey(
                name: "FK_Verifications_Managements_ManagementId",
                table: "Verifications");

            migrationBuilder.DropIndex(
                name: "IX_Verifications_ManagementId",
                table: "Verifications");

            migrationBuilder.DropIndex(
                name: "IX_Requests_ManagementId",
                table: "Requests");

            migrationBuilder.DropIndex(
                name: "IX_Maintenances_ManagementId",
                table: "Maintenances");

            migrationBuilder.DropIndex(
                name: "IX_EquipmentUnits_ManagementId",
                table: "EquipmentUnits");

            migrationBuilder.DropIndex(
                name: "IX_Departures_ManagementId",
                table: "Departures");

            migrationBuilder.DropColumn(
                name: "ManagementId",
                table: "Verifications");

            migrationBuilder.DropColumn(
                name: "ManagementId",
                table: "Requests");

            migrationBuilder.DropColumn(
                name: "ManagementId",
                table: "Maintenances");

            migrationBuilder.DropColumn(
                name: "ManagementId",
                table: "EquipmentUnits");

            migrationBuilder.DropColumn(
                name: "ManagementId",
                table: "Departures");

            migrationBuilder.UpdateData(
                table: "VerificationCheckItems",
                keyColumn: "Id",
                keyValue: 1,
                column: "Name",
                value: "Desconexion del cable de alimentacion electrica para mantenimiento 12 horas antes");

            migrationBuilder.UpdateData(
                table: "VerificationCheckItems",
                keyColumn: "Id",
                keyValue: 2,
                column: "Name",
                value: "Limpieza y desinfeccion interna con productos no abrasivos");

            migrationBuilder.UpdateData(
                table: "VerificationCheckItems",
                keyColumn: "Id",
                keyValue: 3,
                column: "Name",
                value: "Limpieza externa de condensador, serpentin, evaporador y retiro de polvo y grasas");

            migrationBuilder.UpdateData(
                table: "VerificationCheckItems",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "Category", "Name" },
                values: new object[] { "Refrigeracion", "Verificacion de presion del refrigerante" });

            migrationBuilder.UpdateData(
                table: "VerificationCheckItems",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "Category", "Name" },
                values: new object[] { "Refrigeracion", "Revision de fugas y/o microfugas en serpentin" });

            migrationBuilder.UpdateData(
                table: "VerificationCheckItems",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "Category", "Name" },
                values: new object[] { "Refrigeracion", "Revision de formaciones de hielo y condensaciones superficiales no esporadicas" });

            migrationBuilder.UpdateData(
                table: "VerificationCheckItems",
                keyColumn: "Id",
                keyValue: 7,
                column: "Name",
                value: "Control de temperatura y termostatos segun norma");

            migrationBuilder.UpdateData(
                table: "VerificationCheckItems",
                keyColumn: "Id",
                keyValue: 8,
                columns: new[] { "Category", "Name" },
                values: new object[] { "Mecanica", "Revision de puertas y sellos de goma (empaques)" });

            migrationBuilder.UpdateData(
                table: "VerificationCheckItems",
                keyColumn: "Id",
                keyValue: 9,
                column: "Name",
                value: "Limpieza de drenajes de deshielo");

            migrationBuilder.UpdateData(
                table: "VerificationCheckItems",
                keyColumn: "Id",
                keyValue: 10,
                columns: new[] { "Category", "Name" },
                values: new object[] { "Mecanica", "Verificacion del funcionamiento de ventiladores" });

            migrationBuilder.UpdateData(
                table: "VerificationCheckItems",
                keyColumn: "Id",
                keyValue: 11,
                columns: new[] { "Category", "Name" },
                values: new object[] { "Electrico", "Mantenimiento electrico: inspeccion de cableado, terminales, protecciones electricas" });

            migrationBuilder.UpdateData(
                table: "VerificationCheckItems",
                keyColumn: "Id",
                keyValue: 12,
                columns: new[] { "Category", "Name" },
                values: new object[] { "Mecanica", "Lubricacion de partes moviles" });

            migrationBuilder.UpdateData(
                table: "VerificationCheckItems",
                keyColumn: "Id",
                keyValue: 13,
                columns: new[] { "Category", "Name" },
                values: new object[] { "Gestion", "Mantenimiento con personal externo capacitado" });
        }
    }
}
