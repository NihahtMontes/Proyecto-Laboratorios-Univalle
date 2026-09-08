using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;
using System.Drawing;

namespace Proyecto_Laboratorios_Univalle.Services
{
    public partial class ReportService
    {
        public async Task<byte[]> GenerateSolicitudMantenimientoExcel(Request request)
        {
            try
            {
                // 1. EL CONTROLADOR YA OBTUVO LOS DATOS COMPLETOS DE LA SOLICITUD
                if (request == null)
                {
                    _logger.LogWarning("No se proporcionó una solicitud válida.");
                    throw new Exception("La solicitud no existe.");
                }

                // 2. CARGAR PLANTILLA
                var templatePath = Path.Combine(_env.WebRootPath, "templates", "L7.xlsx");

                if (!File.Exists(templatePath))
                {
                    throw new FileNotFoundException("Plantilla de Excel no encontrada.");
                }

                using var package = new ExcelPackage(new FileInfo(templatePath));
                var worksheet = package.Workbook.Worksheets[0];

                // ========================================
                // LIMPIEZA DE SEGURIDAD
                // ========================================
                // LIMPIEZA SEGURA: No usar .Merge=false en rangos amplios (crash 0xffffffff)
                for (int r = 40; r <= 60; r++)
                    for (int c = 1; c <= 26; c++)
                        try { worksheet.Cells[r, c].Value = null; } catch { }

                // ========================================
                // 4. RELLENAR DATOS SEGÚN TU PLANTILLA
                // ========================================

                // TÍTULO TOTALMENTE SIMÉTRICO (Arial 17, Fusionando A-E)
                worksheet.Row(2).Height = 35;
                worksheet.Row(3).Height = 35;

                // Configuración Fila 2 (Arial 16, Fusionando A-E)
                var range1 = worksheet.Cells["A2:E2"];
                try { range1.Merge = true; } catch { }
                range1.Value = "SOLICITUD MANTENIMIENTO Y CALIBRACION DE";
                range1.Style.Font.Bold = true;
                range1.Style.Font.Name = "Arial";
                range1.Style.Font.Size = 16;
                range1.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range1.Style.VerticalAlignment = ExcelVerticalAlignment.Center;

                // Configuración Fila 3
                var range2 = worksheet.Cells["A3:E3"];
                try { range2.Merge = true; } catch { }
                range2.Value = "EQUIPOS DE LABORATORIO";
                range2.Style.Font.Bold = true;
                range2.Style.Font.Name = "Arial";
                range2.Style.Font.Size = 16;
                range2.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range2.Style.VerticalAlignment = ExcelVerticalAlignment.Center;

                // Aplicamos un único borde al bloque completo (A2 a E3) para eliminar la línea del medio
                worksheet.Cells["A2:E3"].Style.Border.BorderAround(ExcelBorderStyle.Thin);

                // ENCABEZADO
                // Código de registro (celda D4)
                worksheet.Cells["D4"].Value = $"RE-10-LAB-{request.Id:D3}";

                // SECCIÓN: EQUIPO
                // Equipo (celda B11 - merged B11:D11)
                worksheet.Cells["B11"].Value = request.Equipment?.Name?.ToUpper() ?? "";

                // Laboratorio (celda B12)
                var labName = (request.EquipmentUnit?.Laboratory?.Name ?? request.Laboratory?.Name ?? "").ToUpper();
                worksheet.Cells["B12"].Value = labName;
                worksheet.Cells["B12"].Style.WrapText = true;
                AjustarAlturaFila(worksheet, 12, labName.ToString());

                // Fecha (celda D12)
                worksheet.Cells["D12"].Value = (request.RequestDate ?? request.CreatedDate).ToString("M/d/yyyy");
                worksheet.Cells["D12"].Style.Font.Bold = true;

                // Responsable (celda B13) - BORRADO por solicitud de usuario
                worksheet.Cells["B13"].Value = "";

                // SECCIÓN: MARCA, MODELO, SERIE
                // Marca (celda B17)
                worksheet.Cells["B17"].Value = request.Equipment?.Brand ?? "";

                // Serie (celda D17)
                worksheet.Cells["D17"].Value = request.EquipmentUnit?.SerialNumber ?? "";

                // Modelo (celda B18)
                worksheet.Cells["B18"].Value = request.Equipment?.Model ?? "";

                // Procedencia (celda D18) format: Ciudad - País
                var ciudad = request.Equipment?.City?.Name ?? "";
                var pais = request.Equipment?.Country?.Name ?? "";
                var procedencia = string.IsNullOrEmpty(ciudad) && string.IsNullOrEmpty(pais)
                                  ? ""
                                  : $"{ciudad} - {pais}".Trim(new char[] { ' ', '-' });

                worksheet.Cells["D18"].Value = procedencia;

                // # de Inventario (celda B19)
                worksheet.Cells["B19"].Value = request.EquipmentUnit?.InventoryNumber ?? "";

                // FALLAS O PROBLEMAS DEL EQUIPO (Fila 23)
                // Sobreescribimos el texto de la plantilla en la fila 23
                var fallasData = string.IsNullOrWhiteSpace(request.Description) ? "Sin descripción" : request.Description;
                var problemasCell = worksheet.Cells["A23"];
                problemasCell.Value = fallasData;
                problemasCell.Style.WrapText = true;
                AjustarAlturaFila(worksheet, 23, fallasData);
                worksheet.Cells["A24"].Value = ""; // Limpiamos la fila 24 que antes tenía el dato

                // SUGERENCIAS U OBSERVACIONES (Fila 28)
                // Sobreescribimos el texto de la plantilla en la fila 28
                var observacionesData = string.IsNullOrWhiteSpace(request.Observations) ? "Sin observaciones" : request.Observations;
                var observacionesCell = worksheet.Cells["A28"];
                observacionesCell.Value = observacionesData;
                observacionesCell.Style.WrapText = true;
                AjustarAlturaFila(worksheet, 28, observacionesData);
                worksheet.Cells["A29"].Value = ""; // Limpiamos la fila 29 que antes tenía el dato

                // PERIODO EN QUE FUE UTILIZADO (Fila 32 y 33)
                // Ponemos en negrita el título de la sección
                worksheet.Cells["A32"].Style.Font.Bold = true;

                var yearsUsed = request.EquipmentUnit?.YearsInOperation ?? 0;
                worksheet.Cells["A33"].Value = $"AÑOS: {yearsUsed}";
                worksheet.Cells["A33"].Style.Font.Bold = true;

                // TIEMPO ESTIMADO DE REPARACIÓN (Fila 35)
                var repairTime = request.EstimatedRepairTime ?? "";
                worksheet.Cells["A35"].Value = $"TIEMPO ESTIMADO DE REPARACIÓN: {repairTime}";
                worksheet.Cells["A35"].Style.Font.Bold = true;

                // ===============================================
                // NUEVO: FIRMA DEL ENCARGADO DE LABORATORIO
                // ===============================================
                // Se coloca en la fila 50, centrado entre la columna B y D
                var firmaRange = worksheet.Cells["B50:D50"];
                try { firmaRange.Merge = true; } catch { }
                firmaRange.Value = "____________________________________";
                firmaRange.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                var labelFirmaRange = worksheet.Cells["B51:D51"];
                try { labelFirmaRange.Merge = true; } catch { }
                labelFirmaRange.Value = "Firma Encargado de Laboratorio";
                labelFirmaRange.Style.Font.Bold = true;
                labelFirmaRange.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                // 5. ASEGURAR FORMATO (bordes, fuentes, etc.)
                AplicarEstilosACeldas(worksheet);

                // 7. RETORNAR ARCHIVO COMO BYTE ARRAY
                return SavePackage(package);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generando reporte Excel para solicitud #{RequestId}", request?.Id ?? 0);
                throw new Exception($"Error al generar reporte: {ex.Message}", ex);
            }
        }


        private void AjustarAlturaFila(ExcelWorksheet worksheet, int rowNumber, string texto)
        {
            if (string.IsNullOrEmpty(texto))
            {
                worksheet.Row(rowNumber).Height = 25; // Altura mínima estándar
                return;
            }

            // Calcular líneas aproximadas (asumiendo 60 caracteres por línea)
            var lineas = Math.Ceiling((double)texto.Length / 60);
            worksheet.Row(rowNumber).Height = Math.Max(25, (double)lineas * 15.0);
        }

        private void AplicarEstilosACeldas(ExcelWorksheet worksheet)
        {
            // Aplicar bordes a celdas de datos
            var celdasDatos = new[] { "B11", "B12", "D12", "B13", "B17", "D17", "B18", "D18", "B19" };

            foreach (var celda in celdasDatos)
            {
                var cell = worksheet.Cells[celda];
                cell.Style.Font.Bold = true;
                cell.Style.Font.Size = 10;
                cell.Style.Border.BorderAround(ExcelBorderStyle.Thin, Color.Black);
                cell.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            }

            // Asegurar que las celdas de texto largo tengan wrap
            worksheet.Cells["B12"].Style.WrapText = true;
            worksheet.Cells["A23"].Style.WrapText = true;
            worksheet.Cells["A28"].Style.WrapText = true;
        }

    }
}
