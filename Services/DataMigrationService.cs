using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Proyecto_Laboratorios_Univalle.Data;
using Proyecto_Laboratorios_Univalle.Models;
using Proyecto_Laboratorios_Univalle.Models.Enums;

namespace Proyecto_Laboratorios_Univalle.Services
{
    public class DataMigrationService
    {
        private readonly ApplicationDbContext _context;

        public DataMigrationService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<string> MigrateFromJsonAsync(string jsonPath)
        {
            if (!File.Exists(jsonPath))
                return "Error: Inventario JSON no encontrado.";

            var jsonContent = await File.ReadAllTextAsync(jsonPath);
            var data = JsonSerializer.Deserialize<MigrationDataRoot>(jsonContent);

            if (data == null) return "Error: Falló la deserialización del JSON.";

            int addedTypes = 0;
            int addedEquipment = 0;
            int addedUnits = 0;

            // 1. Setup Base Hierarchy
            var country = await _context.Countries.FirstOrDefaultAsync(c => c.Name.Contains("Bolivia")) 
                          ?? new Country { Name = "Bolivia" };
            if (country.Id == 0) _context.Countries.Add(country);
            await _context.SaveChangesAsync();

            var city = await _context.Cities.FirstOrDefaultAsync(c => c.Name.Contains("Cochabamba"))
                       ?? new City { Name = "Cochabamba", CountryId = country.Id };
            if (city.Id == 0) _context.Cities.Add(city);
            await _context.SaveChangesAsync();

            var faculty = await _context.Faculties.FirstOrDefaultAsync(f => f.Name.Contains("Gastronomía") || f.Name.Contains("Arquitectura"))
                          ?? new Faculty { Name = "FACULTAD DE ARQUITECTURA Y TURISMO" };
            if (faculty.Id == 0) _context.Faculties.Add(faculty);
            await _context.SaveChangesAsync();

            var career = await _context.Careers.FirstOrDefaultAsync(c => c.Name.Contains("Gastronomía"))
                         ?? new Career { Name = "GASTRONOMÍA", FacultadId = faculty.Id };
            if (career.Id == 0) _context.Careers.Add(career);
            await _context.SaveChangesAsync();

            var lab = await _context.Laboratories.FirstOrDefaultAsync(l => l.Name.Contains("Gastronomía") || l.Code == "GAS-01")
                      ?? new Laboratory { Name = "LABORATORIO DE GASTRONOMÍA", Code = "GAS-01", FacultyId = faculty.Id, Status = GeneralStatus.Activo };
            if (lab.Id == 0) _context.Laboratories.Add(lab);
            await _context.SaveChangesAsync();

            // 2. Process Equipos
            if (data.equipos != null)
            {
                foreach (var item in data.equipos)
                {
                    if (string.IsNullOrWhiteSpace(item.TypeName)) continue;

                    // Type
                    var type = await _context.EquipmentTypes.FirstOrDefaultAsync(t => t.Name == item.TypeName.Trim());
                    if (type == null)
                    {
                        type = new EquipmentType { Name = item.TypeName.Trim(), CreatedDate = DateTime.UtcNow };
                        _context.EquipmentTypes.Add(type);
                        await _context.SaveChangesAsync();
                        addedTypes++;
                    }

                    // Equipment Definition
                    var equipment = await _context.Equipments.FirstOrDefaultAsync(e => e.Name == item.TypeName.Trim() && e.Description == item.Description);
                    if (equipment == null)
                    {
                        equipment = new Equipment 
                        { 
                            Name = item.TypeName.Trim(), 
                            Description = item.Description, 
                            EquipmentTypeId = type.Id,
                            Category = EquipmentCategory.Equipment,
                            CreatedDate = DateTime.UtcNow
                        };
                        _context.Equipments.Add(equipment);
                        await _context.SaveChangesAsync();
                        addedEquipment++;
                    }

                    // Unit
                    if (!string.IsNullOrWhiteSpace(item.InventoryNumber))
                    {
                        var exists = await _context.EquipmentUnits.AnyAsync(u => u.InventoryNumber == item.InventoryNumber.Trim());
                        if (!exists)
                        {
                            var unit = new EquipmentUnit
                            {
                                EquipmentId = equipment.Id,
                                InventoryNumber = item.InventoryNumber.Trim(),
                                LaboratoryId = lab.Id,
                                CareerId = career.Id,
                                CurrentStatus = EquipmentStatus.Operational,
                                Notes = "Migrado de Excel: " + item.OldCode,
                                CreatedDate = DateTime.UtcNow
                            };
                            _context.EquipmentUnits.Add(unit);
                            addedUnits++;
                        }
                    }
                }
            }

            // 3. Process Utensilios
            if (data.utensilios != null)
            {
                foreach (var item in data.utensilios)
                {
                    if (string.IsNullOrWhiteSpace(item.Name)) continue;

                    // Equipment Definition for Utensil
                    var equipment = await _context.Equipments.FirstOrDefaultAsync(e => e.Name == item.Name.Trim() && e.Category == EquipmentCategory.Utensil);
                    if (equipment == null)
                    {
                        equipment = new Equipment 
                        { 
                            Name = item.Name.Trim(), 
                            Description = $"Presentación: {item.Presentation}, Unidad: {item.Unit}", 
                            Category = EquipmentCategory.Utensil,
                            CreatedDate = DateTime.UtcNow
                        };
                        _context.Equipments.Add(equipment);
                        await _context.SaveChangesAsync();
                        addedEquipment++;
                    }

                    // Unit
                    if (!string.IsNullOrWhiteSpace(item.InventoryNumber))
                    {
                        var exists = await _context.EquipmentUnits.AnyAsync(u => u.InventoryNumber == item.InventoryNumber.Trim());
                        if (!exists)
                        {
                            var unit = new EquipmentUnit
                            {
                                EquipmentId = equipment.Id,
                                InventoryNumber = item.InventoryNumber.Trim(),
                                LaboratoryId = lab.Id,
                                CareerId = career.Id,
                                CurrentStatus = EquipmentStatus.Operational,
                                Notes = $"Stock Excel: {item.Stock}. {item.Notes}",
                                CreatedDate = DateTime.UtcNow
                            };
                            _context.EquipmentUnits.Add(unit);
                            addedUnits++;
                        }
                    }
                }
            }

            await _context.SaveChangesAsync();

            return $"Migración exitosa: {addedTypes} tipos, {addedEquipment} definiciones, {addedUnits} unidades físicas agregadas.";
        }
    }

    public class MigrationDataRoot
    {
        public List<EquipmentJson>? equipos { get; set; }
        public List<UtensilJson>? utensilios { get; set; }
        public List<TypeJson>? tipos_utensilios { get; set; }
    }

    public class EquipmentJson
    {
        public string? InventoryNumber { get; set; }
        public string? TypeName { get; set; }
        public string? Description { get; set; }
        public string? OldCode { get; set; }
    }

    public class UtensilJson
    {
        public string? InventoryNumber { get; set; }
        public string? Name { get; set; }
        public string? Presentation { get; set; }
        public string? Unit { get; set; }
        public string? Stock { get; set; }
        public string? Notes { get; set; }
    }

    public class TypeJson
    {
        public string? Name { get; set; }
        public string? Details { get; set; }
        public string? Unit { get; set; }
        public string? Quantity { get; set; }
        public string? Category { get; set; }
    }
}
