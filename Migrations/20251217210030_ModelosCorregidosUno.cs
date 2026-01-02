using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Proyecto_Laboratorios_Univalle.Migrations
{
    /// <inheritdoc />
    public partial class ModelosCorregidosUno : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Facultades",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Codigo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Descripcion = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Estado = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Facultades", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Paises",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Estado = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Paises", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TiposEquipamiento",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RequiereCalibracion = table.Column<bool>(type: "bit", nullable: false),
                    FrecuenciaMantenimientoMeses = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TiposEquipamiento", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TiposMantenimiento",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TiposMantenimiento", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Usuarios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombres = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PrimerApellido = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SegundoApellido = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CI = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Telefono = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    NombreUsuario = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Contrasena = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Rol = table.Column<int>(type: "int", nullable: false),
                    Estado = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    Cargo = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Departamento = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    FechaIngreso = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreadoPorId = table.Column<int>(type: "int", nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModificadoPorId = table.Column<int>(type: "int", nullable: true),
                    UltimaModificacion = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Usuarios", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Usuarios_Usuarios_CreadoPorId",
                        column: x => x.CreadoPorId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Usuarios_Usuarios_ModificadoPorId",
                        column: x => x.ModificadoPorId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Laboratorios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdFacultad = table.Column<int>(type: "int", nullable: false),
                    Siglas = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Edificio = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Piso = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Descripcion = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Estado = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Laboratorios", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Laboratorios_Facultades_IdFacultad",
                        column: x => x.IdFacultad,
                        principalTable: "Facultades",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Ciudades",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PaisId = table.Column<int>(type: "int", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Region = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Estado = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ciudades", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Ciudades_Paises_PaisId",
                        column: x => x.PaisId,
                        principalTable: "Paises",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Equipamientos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdTipoEquipamiento = table.Column<int>(type: "int", nullable: false),
                    IdLaboratorio = table.Column<int>(type: "int", nullable: false),
                    CiudadId = table.Column<int>(type: "int", nullable: false),
                    PaisId = table.Column<int>(type: "int", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NroInventario = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Marca = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Modelo = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Serie = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    VidaUtilAnios = table.Column<int>(type: "int", nullable: true),
                    FechaAdquisicion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ValorAdquisicion = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    EstadoActual = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    Observaciones = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreadoPorId = table.Column<int>(type: "int", nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModificadoPorId = table.Column<int>(type: "int", nullable: true),
                    UltimaModificacion = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Equipamientos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Equipamientos_Ciudades_CiudadId",
                        column: x => x.CiudadId,
                        principalTable: "Ciudades",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Equipamientos_Laboratorios_IdLaboratorio",
                        column: x => x.IdLaboratorio,
                        principalTable: "Laboratorios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Equipamientos_Paises_PaisId",
                        column: x => x.PaisId,
                        principalTable: "Paises",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Equipamientos_TiposEquipamiento_IdTipoEquipamiento",
                        column: x => x.IdTipoEquipamiento,
                        principalTable: "TiposEquipamiento",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Equipamientos_Usuarios_CreadoPorId",
                        column: x => x.CreadoPorId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Equipamientos_Usuarios_ModificadoPorId",
                        column: x => x.ModificadoPorId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EstadosEquipamiento",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdEquipo = table.Column<int>(type: "int", nullable: false),
                    Estado = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    FechaInicio = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaFin = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Motivo = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RegistradoPorId = table.Column<int>(type: "int", nullable: true),
                    FechaRegistro = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EstadosEquipamiento", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EstadosEquipamiento_Equipamientos_IdEquipo",
                        column: x => x.IdEquipo,
                        principalTable: "Equipamientos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EstadosEquipamiento_Usuarios_RegistradoPorId",
                        column: x => x.RegistradoPorId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PlanesMantenimiento",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LaboratorioSnapshot = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    BloqueSnapshot = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IdEquipo = table.Column<int>(type: "int", nullable: false),
                    Servicio = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    TipoServicio = table.Column<int>(type: "int", nullable: false),
                    TiempoEstimado = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    TiempoReal = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
                    IdUserTecnico = table.Column<int>(type: "int", nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LaboratorioId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlanesMantenimiento", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlanesMantenimiento_Equipamientos_IdEquipo",
                        column: x => x.IdEquipo,
                        principalTable: "Equipamientos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PlanesMantenimiento_Laboratorios_LaboratorioId",
                        column: x => x.LaboratorioId,
                        principalTable: "Laboratorios",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PlanesMantenimiento_Usuarios_IdUserTecnico",
                        column: x => x.IdUserTecnico,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Solicitudes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdEquipo = table.Column<int>(type: "int", nullable: false),
                    SolicitadoPorId = table.Column<int>(type: "int", nullable: true),
                    IdTecnico = table.Column<int>(type: "int", nullable: true),
                    Descripcion = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Prioridad = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Observaciones = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    FechaSolicitada = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FechaProgramada = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FechaAtencion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Estado = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    AprobadoPorId = table.Column<int>(type: "int", nullable: true),
                    FechaAprobacion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    MotivoRechazo = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PresupuestoEstimado = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    CreadoPorId = table.Column<int>(type: "int", nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModificadoPorId = table.Column<int>(type: "int", nullable: true),
                    UltimaModificacion = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Solicitudes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Solicitudes_Equipamientos_IdEquipo",
                        column: x => x.IdEquipo,
                        principalTable: "Equipamientos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Solicitudes_Usuarios_AprobadoPorId",
                        column: x => x.AprobadoPorId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Solicitudes_Usuarios_CreadoPorId",
                        column: x => x.CreadoPorId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Solicitudes_Usuarios_IdTecnico",
                        column: x => x.IdTecnico,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Solicitudes_Usuarios_ModificadoPorId",
                        column: x => x.ModificadoPorId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Solicitudes_Usuarios_SolicitadoPorId",
                        column: x => x.SolicitadoPorId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Mantenimientos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdEquipo = table.Column<int>(type: "int", nullable: false),
                    IdTipoMantenimiento = table.Column<int>(type: "int", nullable: false),
                    IdTecnico = table.Column<int>(type: "int", nullable: true),
                    IdSolicitud = table.Column<int>(type: "int", nullable: true),
                    FechaProgramada = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FechaIniciada = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FechaTerminada = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Descripcion = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Estado = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    CostoEstimado = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    CostoReal = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    ServicioEfectuadoPor = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Recomendaciones = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    FechaSugeridaProximoMtto = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Observaciones = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    AprobadoPorId = table.Column<int>(type: "int", nullable: true),
                    FechaAprobacion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreadoPorId = table.Column<int>(type: "int", nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModificadoPorId = table.Column<int>(type: "int", nullable: true),
                    UltimaModificacion = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Mantenimientos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Mantenimientos_Equipamientos_IdEquipo",
                        column: x => x.IdEquipo,
                        principalTable: "Equipamientos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Mantenimientos_Solicitudes_IdSolicitud",
                        column: x => x.IdSolicitud,
                        principalTable: "Solicitudes",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Mantenimientos_TiposMantenimiento_IdTipoMantenimiento",
                        column: x => x.IdTipoMantenimiento,
                        principalTable: "TiposMantenimiento",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Mantenimientos_Usuarios_AprobadoPorId",
                        column: x => x.AprobadoPorId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Mantenimientos_Usuarios_CreadoPorId",
                        column: x => x.CreadoPorId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Mantenimientos_Usuarios_IdTecnico",
                        column: x => x.IdTecnico,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Mantenimientos_Usuarios_ModificadoPorId",
                        column: x => x.ModificadoPorId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DetallesCostos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdMantenimiento = table.Column<int>(type: "int", nullable: false),
                    Concepto = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Cantidad = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    UnidadMedida = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    PrecioUnitario = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Categoria = table.Column<int>(type: "int", nullable: false),
                    Proveedor = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    NumeroFactura = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    FechaRegistro = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RegistradoPorId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DetallesCostos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DetallesCostos_Mantenimientos_IdMantenimiento",
                        column: x => x.IdMantenimiento,
                        principalTable: "Mantenimientos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DetallesCostos_Usuarios_RegistradoPorId",
                        column: x => x.RegistradoPorId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Verificaciones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdEquipo = table.Column<int>(type: "int", nullable: false),
                    IdTecnico = table.Column<int>(type: "int", nullable: true),
                    IdMantenimiento = table.Column<int>(type: "int", nullable: true),
                    VerificacionCableado = table.Column<bool>(type: "bit", nullable: false),
                    VerificacionMangueraGas = table.Column<bool>(type: "bit", nullable: false),
                    VerificacionMangueraAgua = table.Column<bool>(type: "bit", nullable: false),
                    VerificacionQuemador = table.Column<bool>(type: "bit", nullable: false),
                    InspeccionIntercambiadorCalor = table.Column<bool>(type: "bit", nullable: false),
                    VerificacionSensorLlama = table.Column<bool>(type: "bit", nullable: false),
                    RevisionEncendedorElectrodo = table.Column<bool>(type: "bit", nullable: false),
                    VerificacionVentilador = table.Column<bool>(type: "bit", nullable: false),
                    VerificacionLlamaCombustion = table.Column<bool>(type: "bit", nullable: false),
                    VerificacionLubricacion = table.Column<bool>(type: "bit", nullable: false),
                    InspeccionEncendidoHorno = table.Column<bool>(type: "bit", nullable: false),
                    VerificacionTemperatura = table.Column<bool>(type: "bit", nullable: false),
                    VerificacionLimpiezaInterna = table.Column<bool>(type: "bit", nullable: false),
                    VerificacionLimpiezaExterna = table.Column<bool>(type: "bit", nullable: false),
                    VerificacionLuminarias = table.Column<bool>(type: "bit", nullable: false),
                    VerificacionVaporAltaTemp = table.Column<bool>(type: "bit", nullable: false),
                    VerificacionPantallaLED = table.Column<bool>(type: "bit", nullable: false),
                    VerificacionAjusteElectrovalvula = table.Column<bool>(type: "bit", nullable: false),
                    VerificacionAlarmasSonoras = table.Column<bool>(type: "bit", nullable: false),
                    VerificacionPosicionamientoTermocupla = table.Column<bool>(type: "bit", nullable: false),
                    VerificacionSalidaVapor = table.Column<bool>(type: "bit", nullable: false),
                    Fecha = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Observaciones = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    HallazgosCriticos = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Recomendaciones = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Estado = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    AprobadoPorId = table.Column<int>(type: "int", nullable: true),
                    FechaAprobacion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreadoPorId = table.Column<int>(type: "int", nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModificadoPorId = table.Column<int>(type: "int", nullable: true),
                    UltimaModificacion = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Verificaciones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Verificaciones_Equipamientos_IdEquipo",
                        column: x => x.IdEquipo,
                        principalTable: "Equipamientos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Verificaciones_Mantenimientos_IdMantenimiento",
                        column: x => x.IdMantenimiento,
                        principalTable: "Mantenimientos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Verificaciones_Usuarios_AprobadoPorId",
                        column: x => x.AprobadoPorId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Verificaciones_Usuarios_CreadoPorId",
                        column: x => x.CreadoPorId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Verificaciones_Usuarios_IdTecnico",
                        column: x => x.IdTecnico,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Verificaciones_Usuarios_ModificadoPorId",
                        column: x => x.ModificadoPorId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Ciudades_PaisId",
                table: "Ciudades",
                column: "PaisId");

            migrationBuilder.CreateIndex(
                name: "IX_DetallesCostos_IdMantenimiento",
                table: "DetallesCostos",
                column: "IdMantenimiento");

            migrationBuilder.CreateIndex(
                name: "IX_DetallesCostos_RegistradoPorId",
                table: "DetallesCostos",
                column: "RegistradoPorId");

            migrationBuilder.CreateIndex(
                name: "IX_Equipamientos_CiudadId",
                table: "Equipamientos",
                column: "CiudadId");

            migrationBuilder.CreateIndex(
                name: "IX_Equipamientos_CreadoPorId",
                table: "Equipamientos",
                column: "CreadoPorId");

            migrationBuilder.CreateIndex(
                name: "IX_Equipamientos_IdLaboratorio",
                table: "Equipamientos",
                column: "IdLaboratorio");

            migrationBuilder.CreateIndex(
                name: "IX_Equipamientos_IdTipoEquipamiento",
                table: "Equipamientos",
                column: "IdTipoEquipamiento");

            migrationBuilder.CreateIndex(
                name: "IX_Equipamientos_ModificadoPorId",
                table: "Equipamientos",
                column: "ModificadoPorId");

            migrationBuilder.CreateIndex(
                name: "IX_Equipamientos_NroInventario",
                table: "Equipamientos",
                column: "NroInventario",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Equipamientos_PaisId",
                table: "Equipamientos",
                column: "PaisId");

            migrationBuilder.CreateIndex(
                name: "IX_EstadosEquipamiento_IdEquipo",
                table: "EstadosEquipamiento",
                column: "IdEquipo");

            migrationBuilder.CreateIndex(
                name: "IX_EstadosEquipamiento_RegistradoPorId",
                table: "EstadosEquipamiento",
                column: "RegistradoPorId");

            migrationBuilder.CreateIndex(
                name: "IX_Laboratorios_IdFacultad",
                table: "Laboratorios",
                column: "IdFacultad");

            migrationBuilder.CreateIndex(
                name: "IX_Laboratorios_Siglas",
                table: "Laboratorios",
                column: "Siglas",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Mantenimientos_AprobadoPorId",
                table: "Mantenimientos",
                column: "AprobadoPorId");

            migrationBuilder.CreateIndex(
                name: "IX_Mantenimientos_CreadoPorId",
                table: "Mantenimientos",
                column: "CreadoPorId");

            migrationBuilder.CreateIndex(
                name: "IX_Mantenimientos_IdEquipo",
                table: "Mantenimientos",
                column: "IdEquipo");

            migrationBuilder.CreateIndex(
                name: "IX_Mantenimientos_IdSolicitud",
                table: "Mantenimientos",
                column: "IdSolicitud");

            migrationBuilder.CreateIndex(
                name: "IX_Mantenimientos_IdTecnico",
                table: "Mantenimientos",
                column: "IdTecnico");

            migrationBuilder.CreateIndex(
                name: "IX_Mantenimientos_IdTipoMantenimiento",
                table: "Mantenimientos",
                column: "IdTipoMantenimiento");

            migrationBuilder.CreateIndex(
                name: "IX_Mantenimientos_ModificadoPorId",
                table: "Mantenimientos",
                column: "ModificadoPorId");

            migrationBuilder.CreateIndex(
                name: "IX_PlanesMantenimiento_IdEquipo",
                table: "PlanesMantenimiento",
                column: "IdEquipo");

            migrationBuilder.CreateIndex(
                name: "IX_PlanesMantenimiento_IdUserTecnico",
                table: "PlanesMantenimiento",
                column: "IdUserTecnico");

            migrationBuilder.CreateIndex(
                name: "IX_PlanesMantenimiento_LaboratorioId",
                table: "PlanesMantenimiento",
                column: "LaboratorioId");

            migrationBuilder.CreateIndex(
                name: "IX_Solicitudes_AprobadoPorId",
                table: "Solicitudes",
                column: "AprobadoPorId");

            migrationBuilder.CreateIndex(
                name: "IX_Solicitudes_CreadoPorId",
                table: "Solicitudes",
                column: "CreadoPorId");

            migrationBuilder.CreateIndex(
                name: "IX_Solicitudes_IdEquipo",
                table: "Solicitudes",
                column: "IdEquipo");

            migrationBuilder.CreateIndex(
                name: "IX_Solicitudes_IdTecnico",
                table: "Solicitudes",
                column: "IdTecnico");

            migrationBuilder.CreateIndex(
                name: "IX_Solicitudes_ModificadoPorId",
                table: "Solicitudes",
                column: "ModificadoPorId");

            migrationBuilder.CreateIndex(
                name: "IX_Solicitudes_SolicitadoPorId",
                table: "Solicitudes",
                column: "SolicitadoPorId");

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_CI",
                table: "Usuarios",
                column: "CI",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_CreadoPorId",
                table: "Usuarios",
                column: "CreadoPorId");

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_ModificadoPorId",
                table: "Usuarios",
                column: "ModificadoPorId");

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_NombreUsuario",
                table: "Usuarios",
                column: "NombreUsuario",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Verificaciones_AprobadoPorId",
                table: "Verificaciones",
                column: "AprobadoPorId");

            migrationBuilder.CreateIndex(
                name: "IX_Verificaciones_CreadoPorId",
                table: "Verificaciones",
                column: "CreadoPorId");

            migrationBuilder.CreateIndex(
                name: "IX_Verificaciones_IdEquipo",
                table: "Verificaciones",
                column: "IdEquipo");

            migrationBuilder.CreateIndex(
                name: "IX_Verificaciones_IdMantenimiento",
                table: "Verificaciones",
                column: "IdMantenimiento");

            migrationBuilder.CreateIndex(
                name: "IX_Verificaciones_IdTecnico",
                table: "Verificaciones",
                column: "IdTecnico");

            migrationBuilder.CreateIndex(
                name: "IX_Verificaciones_ModificadoPorId",
                table: "Verificaciones",
                column: "ModificadoPorId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DetallesCostos");

            migrationBuilder.DropTable(
                name: "EstadosEquipamiento");

            migrationBuilder.DropTable(
                name: "PlanesMantenimiento");

            migrationBuilder.DropTable(
                name: "Verificaciones");

            migrationBuilder.DropTable(
                name: "Mantenimientos");

            migrationBuilder.DropTable(
                name: "Solicitudes");

            migrationBuilder.DropTable(
                name: "TiposMantenimiento");

            migrationBuilder.DropTable(
                name: "Equipamientos");

            migrationBuilder.DropTable(
                name: "Ciudades");

            migrationBuilder.DropTable(
                name: "Laboratorios");

            migrationBuilder.DropTable(
                name: "TiposEquipamiento");

            migrationBuilder.DropTable(
                name: "Usuarios");

            migrationBuilder.DropTable(
                name: "Paises");

            migrationBuilder.DropTable(
                name: "Facultades");
        }
    }
}
