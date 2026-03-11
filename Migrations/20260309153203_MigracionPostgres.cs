using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Proyecto_Laboratorios_Univalle.Migrations
{
    /// <inheritdoc />
    public partial class MigracionPostgres : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    normalized_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    concurrency_stamp = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_roles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    phone_number = table.Column<string>(type: "text", nullable: false),
                    first_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    last_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    second_last_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    identity_card = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    role = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    position = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    department = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    hire_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by_id = table.Column<int>(type: "integer", nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    modified_by_id = table.Column<int>(type: "integer", nullable: true),
                    last_modified_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    normalized_user_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    normalized_email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    email_confirmed = table.Column<bool>(type: "boolean", nullable: false),
                    password_hash = table.Column<string>(type: "text", nullable: true),
                    security_stamp = table.Column<string>(type: "text", nullable: true),
                    concurrency_stamp = table.Column<string>(type: "text", nullable: true),
                    phone_number_confirmed = table.Column<bool>(type: "boolean", nullable: false),
                    two_factor_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    lockout_end = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    lockout_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    access_failed_count = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_users", x => x.id);
                    table.ForeignKey(
                        name: "fk_users_users_created_by_id",
                        column: x => x.created_by_id,
                        principalTable: "Users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_users_users_modified_by_id",
                        column: x => x.modified_by_id,
                        principalTable: "Users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "RoleClaims",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    role_id = table.Column<int>(type: "integer", nullable: false),
                    claim_type = table.Column<string>(type: "text", nullable: true),
                    claim_value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_role_claims", x => x.id);
                    table.ForeignKey(
                        name: "fk_role_claims_roles_role_id",
                        column: x => x.role_id,
                        principalTable: "Roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "countries",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_id = table.Column<int>(type: "integer", nullable: true),
                    modified_by_id = table.Column<int>(type: "integer", nullable: true),
                    last_modified_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_countries", x => x.id);
                    table.ForeignKey(
                        name: "fk_countries_user_created_by_id",
                        column: x => x.created_by_id,
                        principalTable: "Users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_countries_user_modified_by_id",
                        column: x => x.modified_by_id,
                        principalTable: "Users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "equipment_types",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    requires_calibration = table.Column<bool>(type: "boolean", nullable: false),
                    maintenance_frequency_months = table.Column<int>(type: "integer", nullable: true),
                    created_by_id = table.Column<int>(type: "integer", nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    modified_by_id = table.Column<int>(type: "integer", nullable: true),
                    last_modified_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_equipment_types", x => x.id);
                    table.ForeignKey(
                        name: "fk_equipment_types_user_created_by_id",
                        column: x => x.created_by_id,
                        principalTable: "Users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_equipment_types_user_modified_by_id",
                        column: x => x.modified_by_id,
                        principalTable: "Users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "faculties",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_id = table.Column<int>(type: "integer", nullable: true),
                    modified_by_id = table.Column<int>(type: "integer", nullable: true),
                    last_modified_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_faculties", x => x.id);
                    table.ForeignKey(
                        name: "fk_faculties_user_created_by_id",
                        column: x => x.created_by_id,
                        principalTable: "Users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_faculties_user_modified_by_id",
                        column: x => x.modified_by_id,
                        principalTable: "Users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "maintenance_types",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_by_id = table.Column<int>(type: "integer", nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    modified_by_id = table.Column<int>(type: "integer", nullable: true),
                    last_modified_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_maintenance_types", x => x.id);
                    table.ForeignKey(
                        name: "fk_maintenance_types_user_created_by_id",
                        column: x => x.created_by_id,
                        principalTable: "Users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_maintenance_types_user_modified_by_id",
                        column: x => x.modified_by_id,
                        principalTable: "Users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "People",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    status = table.Column<int>(type: "integer", nullable: false),
                    email = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    phone_number = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_id = table.Column<int>(type: "integer", nullable: true),
                    modified_by_id = table.Column<int>(type: "integer", nullable: true),
                    last_modified_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_People", x => x.id);
                    table.ForeignKey(
                        name: "fk_people_users_created_by_id",
                        column: x => x.created_by_id,
                        principalTable: "Users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_people_users_modified_by_id",
                        column: x => x.modified_by_id,
                        principalTable: "Users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "UserClaims",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    claim_type = table.Column<string>(type: "text", nullable: true),
                    claim_value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_claims", x => x.id);
                    table.ForeignKey(
                        name: "fk_user_claims_users_user_id",
                        column: x => x.user_id,
                        principalTable: "Users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserLogins",
                columns: table => new
                {
                    login_provider = table.Column<string>(type: "text", nullable: false),
                    provider_key = table.Column<string>(type: "text", nullable: false),
                    provider_display_name = table.Column<string>(type: "text", nullable: true),
                    user_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_logins", x => new { x.login_provider, x.provider_key });
                    table.ForeignKey(
                        name: "fk_user_logins_users_user_id",
                        column: x => x.user_id,
                        principalTable: "Users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserRoles",
                columns: table => new
                {
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    role_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_roles", x => new { x.user_id, x.role_id });
                    table.ForeignKey(
                        name: "fk_user_roles_roles_role_id",
                        column: x => x.role_id,
                        principalTable: "Roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_user_roles_users_user_id",
                        column: x => x.user_id,
                        principalTable: "Users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserTokens",
                columns: table => new
                {
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    login_provider = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_tokens", x => new { x.user_id, x.login_provider, x.name });
                    table.ForeignKey(
                        name: "fk_user_tokens_users_user_id",
                        column: x => x.user_id,
                        principalTable: "Users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "cities",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    country_id = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    region = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by_id = table.Column<int>(type: "integer", nullable: true),
                    modified_by_id = table.Column<int>(type: "integer", nullable: true),
                    last_modified_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_cities", x => x.id);
                    table.ForeignKey(
                        name: "fk_cities_countries_country_id",
                        column: x => x.country_id,
                        principalTable: "countries",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_cities_user_created_by_id",
                        column: x => x.created_by_id,
                        principalTable: "Users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_cities_user_modified_by_id",
                        column: x => x.modified_by_id,
                        principalTable: "Users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "careers",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    facultad_id = table.Column<int>(type: "integer", nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    created_by_id = table.Column<int>(type: "integer", nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    modified_by_id = table.Column<int>(type: "integer", nullable: true),
                    last_modified_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_careers", x => x.id);
                    table.ForeignKey(
                        name: "fk_careers_faculties_facultad_id",
                        column: x => x.facultad_id,
                        principalTable: "faculties",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_careers_user_created_by_id",
                        column: x => x.created_by_id,
                        principalTable: "Users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_careers_user_modified_by_id",
                        column: x => x.modified_by_id,
                        principalTable: "Users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "laboratories",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    faculty_id = table.Column<int>(type: "integer", nullable: false),
                    code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    building = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    floor = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    created_by_id = table.Column<int>(type: "integer", nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    modified_by_id = table.Column<int>(type: "integer", nullable: true),
                    last_modified_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    city_id = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_laboratories", x => x.id);
                    table.ForeignKey(
                        name: "fk_laboratories_faculties_faculty_id",
                        column: x => x.faculty_id,
                        principalTable: "faculties",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_laboratories_user_created_by_id",
                        column: x => x.created_by_id,
                        principalTable: "Users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_laboratories_user_modified_by_id",
                        column: x => x.modified_by_id,
                        principalTable: "Users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "Externs",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    is_entity = table.Column<bool>(type: "boolean", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    extern_status = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Externs", x => x.id);
                    table.ForeignKey(
                        name: "fk_externs_people_id",
                        column: x => x.id,
                        principalTable: "People",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Interns",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    intern_status = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Interns", x => x.id);
                    table.ForeignKey(
                        name: "fk_interns_people_id",
                        column: x => x.id,
                        principalTable: "People",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "equipments",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    equipment_type_id = table.Column<int>(type: "integer", nullable: true),
                    category = table.Column<int>(type: "integer", nullable: false),
                    image_url = table.Column<string>(type: "text", nullable: true),
                    country_id = table.Column<int>(type: "integer", nullable: true),
                    city_id = table.Column<int>(type: "integer", nullable: true),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    brand = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    model = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    useful_life_years = table.Column<int>(type: "integer", nullable: true),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    created_by_id = table.Column<int>(type: "integer", nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    modified_by_id = table.Column<int>(type: "integer", nullable: true),
                    last_modified_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_equipments", x => x.id);
                    table.ForeignKey(
                        name: "fk_equipments_cities_city_id",
                        column: x => x.city_id,
                        principalTable: "cities",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_equipments_countries_country_id",
                        column: x => x.country_id,
                        principalTable: "countries",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_equipments_equipment_types_equipment_type_id",
                        column: x => x.equipment_type_id,
                        principalTable: "equipment_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_equipments_user_created_by_id",
                        column: x => x.created_by_id,
                        principalTable: "Users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_equipments_user_modified_by_id",
                        column: x => x.modified_by_id,
                        principalTable: "Users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "equipment_units",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    equipment_id = table.Column<int>(type: "integer", nullable: false),
                    laboratory_id = table.Column<int>(type: "integer", nullable: true),
                    inventory_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    serial_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    career_id = table.Column<int>(type: "integer", nullable: true),
                    internal_location = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    acquisition_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    manufacturing_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    acquisition_value = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    current_status = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    physical_condition = table.Column<int>(type: "integer", nullable: true),
                    notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    created_by_id = table.Column<int>(type: "integer", nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    modified_by_id = table.Column<int>(type: "integer", nullable: true),
                    last_modified_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_equipment_units", x => x.id);
                    table.ForeignKey(
                        name: "fk_equipment_units_careers_career_id",
                        column: x => x.career_id,
                        principalTable: "careers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_equipment_units_equipments_equipment_id",
                        column: x => x.equipment_id,
                        principalTable: "equipments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_equipment_units_laboratories_laboratory_id",
                        column: x => x.laboratory_id,
                        principalTable: "laboratories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_equipment_units_user_created_by_id",
                        column: x => x.created_by_id,
                        principalTable: "Users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_equipment_units_user_modified_by_id",
                        column: x => x.modified_by_id,
                        principalTable: "Users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "equipment_state_histories",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    equipment_unit_id = table.Column<int>(type: "integer", nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    start_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    end_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_by_id = table.Column<int>(type: "integer", nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    modified_by_id = table.Column<int>(type: "integer", nullable: true),
                    last_modified_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_equipment_state_histories", x => x.id);
                    table.ForeignKey(
                        name: "fk_equipment_state_histories_equipment_units_equipment_unit_id",
                        column: x => x.equipment_unit_id,
                        principalTable: "equipment_units",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_equipment_state_histories_user_created_by_id",
                        column: x => x.created_by_id,
                        principalTable: "Users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_equipment_state_histories_user_modified_by_id",
                        column: x => x.modified_by_id,
                        principalTable: "Users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "loans",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    equipment_unit_id = table.Column<int>(type: "integer", nullable: false),
                    borrower_id = table.Column<int>(type: "integer", nullable: false),
                    loan_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    estimated_return_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    actual_return_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    departure_observations = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    return_observations = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    created_by_id = table.Column<int>(type: "integer", nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    modified_by_id = table.Column<int>(type: "integer", nullable: true),
                    last_modified_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_loans", x => x.id);
                    table.ForeignKey(
                        name: "fk_loans_equipment_units_equipment_unit_id",
                        column: x => x.equipment_unit_id,
                        principalTable: "equipment_units",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_loans_people_borrower_id",
                        column: x => x.borrower_id,
                        principalTable: "People",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_loans_user_created_by_id",
                        column: x => x.created_by_id,
                        principalTable: "Users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_loans_user_modified_by_id",
                        column: x => x.modified_by_id,
                        principalTable: "Users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "maintenance_plans",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    laboratory_snapshot = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    block_snapshot = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    equipment_unit_id = table.Column<int>(type: "integer", nullable: true),
                    service = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    service_type = table.Column<int>(type: "integer", nullable: false),
                    created_by_id = table.Column<int>(type: "integer", nullable: true),
                    modified_by_id = table.Column<int>(type: "integer", nullable: true),
                    last_modified_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    estimated_time = table.Column<decimal>(type: "numeric(10,2)", nullable: true),
                    actual_time = table.Column<decimal>(type: "numeric(10,2)", nullable: true),
                    assigned_technician_id = table.Column<int>(type: "integer", nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    laboratory_id = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_maintenance_plans", x => x.id);
                    table.ForeignKey(
                        name: "fk_maintenance_plans_equipment_units_equipment_unit_id",
                        column: x => x.equipment_unit_id,
                        principalTable: "equipment_units",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_maintenance_plans_laboratories_laboratory_id",
                        column: x => x.laboratory_id,
                        principalTable: "laboratories",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_maintenance_plans_user_assigned_technician_id",
                        column: x => x.assigned_technician_id,
                        principalTable: "Users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_maintenance_plans_user_created_by_id",
                        column: x => x.created_by_id,
                        principalTable: "Users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_maintenance_plans_user_modified_by_id",
                        column: x => x.modified_by_id,
                        principalTable: "Users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "requests",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    laboratory_id = table.Column<int>(type: "integer", nullable: false),
                    equipment_id = table.Column<int>(type: "integer", nullable: false),
                    equipment_unit_id = table.Column<int>(type: "integer", nullable: true),
                    requested_by_id = table.Column<int>(type: "integer", nullable: true),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    priority = table.Column<int>(type: "integer", nullable: false),
                    observations = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    estimated_repair_time = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    approved_by_id = table.Column<int>(type: "integer", nullable: true),
                    approval_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    rejection_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_by_id = table.Column<int>(type: "integer", nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    modified_by_id = table.Column<int>(type: "integer", nullable: true),
                    last_modified_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    type = table.Column<int>(type: "integer", nullable: false),
                    investment_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    cost_center = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_requests", x => x.id);
                    table.ForeignKey(
                        name: "fk_requests_equipment_units_equipment_unit_id",
                        column: x => x.equipment_unit_id,
                        principalTable: "equipment_units",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_requests_equipments_equipment_id",
                        column: x => x.equipment_id,
                        principalTable: "equipments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_requests_laboratories_laboratory_id",
                        column: x => x.laboratory_id,
                        principalTable: "laboratories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_requests_user_approved_by_id",
                        column: x => x.approved_by_id,
                        principalTable: "Users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_requests_user_created_by_id",
                        column: x => x.created_by_id,
                        principalTable: "Users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_requests_user_modified_by_id",
                        column: x => x.modified_by_id,
                        principalTable: "Users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_requests_user_requested_by_id",
                        column: x => x.requested_by_id,
                        principalTable: "Users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "verifications",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    equipment_unit_id = table.Column<int>(type: "integer", nullable: false),
                    cabling_check = table.Column<int>(type: "integer", nullable: false),
                    gas_hose_check = table.Column<int>(type: "integer", nullable: false),
                    water_hose_check = table.Column<int>(type: "integer", nullable: false),
                    burner_check = table.Column<int>(type: "integer", nullable: false),
                    heat_exchanger_check = table.Column<int>(type: "integer", nullable: false),
                    flame_sensor_check = table.Column<int>(type: "integer", nullable: false),
                    electrode_igniter_check = table.Column<int>(type: "integer", nullable: false),
                    fan_check = table.Column<int>(type: "integer", nullable: false),
                    combustion_flame_check = table.Column<int>(type: "integer", nullable: false),
                    lubrication_check = table.Column<int>(type: "integer", nullable: false),
                    oven_ignition_check = table.Column<int>(type: "integer", nullable: false),
                    temperature_control_check = table.Column<int>(type: "integer", nullable: false),
                    internal_cleaning_check = table.Column<int>(type: "integer", nullable: false),
                    external_cleaning_check = table.Column<int>(type: "integer", nullable: false),
                    lights_check = table.Column<int>(type: "integer", nullable: false),
                    high_temp_steam_check = table.Column<int>(type: "integer", nullable: false),
                    led_display_check = table.Column<int>(type: "integer", nullable: false),
                    solenoid_valve_check = table.Column<int>(type: "integer", nullable: false),
                    sound_alarm_check = table.Column<int>(type: "integer", nullable: false),
                    thermocouple_check = table.Column<int>(type: "integer", nullable: false),
                    steam_outlet_check = table.Column<int>(type: "integer", nullable: false),
                    date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    observations = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    critical_findings = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    recommendations = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    created_by_id = table.Column<int>(type: "integer", nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    modified_by_id = table.Column<int>(type: "integer", nullable: true),
                    last_modified_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_verifications", x => x.id);
                    table.ForeignKey(
                        name: "fk_verifications_equipment_units_equipment_unit_id",
                        column: x => x.equipment_unit_id,
                        principalTable: "equipment_units",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_verifications_user_created_by_id",
                        column: x => x.created_by_id,
                        principalTable: "Users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_verifications_user_modified_by_id",
                        column: x => x.modified_by_id,
                        principalTable: "Users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "maintenances",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    equipment_unit_id = table.Column<int>(type: "integer", nullable: false),
                    maintenance_type_id = table.Column<int>(type: "integer", nullable: false),
                    technician_id = table.Column<int>(type: "integer", nullable: true),
                    request_id = table.Column<int>(type: "integer", nullable: true),
                    scheduled_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    start_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    end_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    estimated_cost = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    actual_cost = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    recommendations = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    suggested_next_maintenance_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    satisfaction_level = table.Column<int>(type: "integer", nullable: true),
                    observations = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    created_by_id = table.Column<int>(type: "integer", nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    modified_by_id = table.Column<int>(type: "integer", nullable: true),
                    last_modified_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_maintenances", x => x.id);
                    table.ForeignKey(
                        name: "fk_maintenances_equipment_units_equipment_unit_id",
                        column: x => x.equipment_unit_id,
                        principalTable: "equipment_units",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_maintenances_maintenance_types_maintenance_type_id",
                        column: x => x.maintenance_type_id,
                        principalTable: "maintenance_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_maintenances_people_technician_id",
                        column: x => x.technician_id,
                        principalTable: "People",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_maintenances_requests_request_id",
                        column: x => x.request_id,
                        principalTable: "requests",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_maintenances_user_created_by_id",
                        column: x => x.created_by_id,
                        principalTable: "Users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_maintenances_user_modified_by_id",
                        column: x => x.modified_by_id,
                        principalTable: "Users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "cost_details",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    request_id = table.Column<int>(type: "integer", nullable: true),
                    maintenance_id = table.Column<int>(type: "integer", nullable: true),
                    concept = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    quantity = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    unit_of_measure = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    unit_price = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    category = table.Column<int>(type: "integer", nullable: false),
                    provider = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    invoice_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    created_by_id = table.Column<int>(type: "integer", nullable: true),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    modified_by_id = table.Column<int>(type: "integer", nullable: true),
                    last_modified_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_cost_details", x => x.id);
                    table.ForeignKey(
                        name: "fk_cost_details_maintenances_maintenance_id",
                        column: x => x.maintenance_id,
                        principalTable: "maintenances",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_cost_details_requests_request_id",
                        column: x => x.request_id,
                        principalTable: "requests",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_cost_details_user_created_by_id",
                        column: x => x.created_by_id,
                        principalTable: "Users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_cost_details_user_modified_by_id",
                        column: x => x.modified_by_id,
                        principalTable: "Users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "ix_careers_created_by_id",
                table: "careers",
                column: "created_by_id");

            migrationBuilder.CreateIndex(
                name: "ix_careers_facultad_id",
                table: "careers",
                column: "facultad_id");

            migrationBuilder.CreateIndex(
                name: "ix_careers_modified_by_id",
                table: "careers",
                column: "modified_by_id");

            migrationBuilder.CreateIndex(
                name: "ix_cities_country_id",
                table: "cities",
                column: "country_id");

            migrationBuilder.CreateIndex(
                name: "ix_cities_created_by_id",
                table: "cities",
                column: "created_by_id");

            migrationBuilder.CreateIndex(
                name: "ix_cities_modified_by_id",
                table: "cities",
                column: "modified_by_id");

            migrationBuilder.CreateIndex(
                name: "ix_cost_details_created_by_id",
                table: "cost_details",
                column: "created_by_id");

            migrationBuilder.CreateIndex(
                name: "ix_cost_details_maintenance_id",
                table: "cost_details",
                column: "maintenance_id");

            migrationBuilder.CreateIndex(
                name: "ix_cost_details_modified_by_id",
                table: "cost_details",
                column: "modified_by_id");

            migrationBuilder.CreateIndex(
                name: "ix_cost_details_request_id",
                table: "cost_details",
                column: "request_id");

            migrationBuilder.CreateIndex(
                name: "ix_countries_created_by_id",
                table: "countries",
                column: "created_by_id");

            migrationBuilder.CreateIndex(
                name: "ix_countries_modified_by_id",
                table: "countries",
                column: "modified_by_id");

            migrationBuilder.CreateIndex(
                name: "ix_equipment_state_histories_created_by_id",
                table: "equipment_state_histories",
                column: "created_by_id");

            migrationBuilder.CreateIndex(
                name: "ix_equipment_state_histories_equipment_unit_id",
                table: "equipment_state_histories",
                column: "equipment_unit_id");

            migrationBuilder.CreateIndex(
                name: "ix_equipment_state_histories_modified_by_id",
                table: "equipment_state_histories",
                column: "modified_by_id");

            migrationBuilder.CreateIndex(
                name: "ix_equipment_types_created_by_id",
                table: "equipment_types",
                column: "created_by_id");

            migrationBuilder.CreateIndex(
                name: "ix_equipment_types_modified_by_id",
                table: "equipment_types",
                column: "modified_by_id");

            migrationBuilder.CreateIndex(
                name: "ix_equipment_units_career_id",
                table: "equipment_units",
                column: "career_id");

            migrationBuilder.CreateIndex(
                name: "ix_equipment_units_created_by_id",
                table: "equipment_units",
                column: "created_by_id");

            migrationBuilder.CreateIndex(
                name: "ix_equipment_units_equipment_id",
                table: "equipment_units",
                column: "equipment_id");

            migrationBuilder.CreateIndex(
                name: "ix_equipment_units_inventory_number",
                table: "equipment_units",
                column: "inventory_number",
                unique: true,
                filter: "current_status != 99");

            migrationBuilder.CreateIndex(
                name: "ix_equipment_units_laboratory_id",
                table: "equipment_units",
                column: "laboratory_id");

            migrationBuilder.CreateIndex(
                name: "ix_equipment_units_modified_by_id",
                table: "equipment_units",
                column: "modified_by_id");

            migrationBuilder.CreateIndex(
                name: "ix_equipments_city_id",
                table: "equipments",
                column: "city_id");

            migrationBuilder.CreateIndex(
                name: "ix_equipments_country_id",
                table: "equipments",
                column: "country_id");

            migrationBuilder.CreateIndex(
                name: "ix_equipments_created_by_id",
                table: "equipments",
                column: "created_by_id");

            migrationBuilder.CreateIndex(
                name: "ix_equipments_equipment_type_id",
                table: "equipments",
                column: "equipment_type_id");

            migrationBuilder.CreateIndex(
                name: "ix_equipments_modified_by_id",
                table: "equipments",
                column: "modified_by_id");

            migrationBuilder.CreateIndex(
                name: "ix_faculties_created_by_id",
                table: "faculties",
                column: "created_by_id");

            migrationBuilder.CreateIndex(
                name: "ix_faculties_modified_by_id",
                table: "faculties",
                column: "modified_by_id");

            migrationBuilder.CreateIndex(
                name: "ix_laboratories_code",
                table: "laboratories",
                column: "code",
                unique: true,
                filter: "status != 2");

            migrationBuilder.CreateIndex(
                name: "ix_laboratories_created_by_id",
                table: "laboratories",
                column: "created_by_id");

            migrationBuilder.CreateIndex(
                name: "ix_laboratories_faculty_id",
                table: "laboratories",
                column: "faculty_id");

            migrationBuilder.CreateIndex(
                name: "ix_laboratories_modified_by_id",
                table: "laboratories",
                column: "modified_by_id");

            migrationBuilder.CreateIndex(
                name: "ix_loans_borrower_id",
                table: "loans",
                column: "borrower_id");

            migrationBuilder.CreateIndex(
                name: "ix_loans_created_by_id",
                table: "loans",
                column: "created_by_id");

            migrationBuilder.CreateIndex(
                name: "ix_loans_equipment_unit_id",
                table: "loans",
                column: "equipment_unit_id");

            migrationBuilder.CreateIndex(
                name: "ix_loans_modified_by_id",
                table: "loans",
                column: "modified_by_id");

            migrationBuilder.CreateIndex(
                name: "ix_maintenance_plans_assigned_technician_id",
                table: "maintenance_plans",
                column: "assigned_technician_id");

            migrationBuilder.CreateIndex(
                name: "ix_maintenance_plans_created_by_id",
                table: "maintenance_plans",
                column: "created_by_id");

            migrationBuilder.CreateIndex(
                name: "ix_maintenance_plans_equipment_unit_id",
                table: "maintenance_plans",
                column: "equipment_unit_id");

            migrationBuilder.CreateIndex(
                name: "ix_maintenance_plans_laboratory_id",
                table: "maintenance_plans",
                column: "laboratory_id");

            migrationBuilder.CreateIndex(
                name: "ix_maintenance_plans_modified_by_id",
                table: "maintenance_plans",
                column: "modified_by_id");

            migrationBuilder.CreateIndex(
                name: "ix_maintenance_types_created_by_id",
                table: "maintenance_types",
                column: "created_by_id");

            migrationBuilder.CreateIndex(
                name: "ix_maintenance_types_modified_by_id",
                table: "maintenance_types",
                column: "modified_by_id");

            migrationBuilder.CreateIndex(
                name: "ix_maintenances_created_by_id",
                table: "maintenances",
                column: "created_by_id");

            migrationBuilder.CreateIndex(
                name: "ix_maintenances_equipment_unit_id",
                table: "maintenances",
                column: "equipment_unit_id");

            migrationBuilder.CreateIndex(
                name: "ix_maintenances_maintenance_type_id",
                table: "maintenances",
                column: "maintenance_type_id");

            migrationBuilder.CreateIndex(
                name: "ix_maintenances_modified_by_id",
                table: "maintenances",
                column: "modified_by_id");

            migrationBuilder.CreateIndex(
                name: "ix_maintenances_request_id",
                table: "maintenances",
                column: "request_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_maintenances_technician_id",
                table: "maintenances",
                column: "technician_id");

            migrationBuilder.CreateIndex(
                name: "ix_people_created_by_id",
                table: "People",
                column: "created_by_id");

            migrationBuilder.CreateIndex(
                name: "ix_people_modified_by_id",
                table: "People",
                column: "modified_by_id");

            migrationBuilder.CreateIndex(
                name: "ix_requests_approved_by_id",
                table: "requests",
                column: "approved_by_id");

            migrationBuilder.CreateIndex(
                name: "ix_requests_created_by_id",
                table: "requests",
                column: "created_by_id");

            migrationBuilder.CreateIndex(
                name: "ix_requests_equipment_id",
                table: "requests",
                column: "equipment_id");

            migrationBuilder.CreateIndex(
                name: "ix_requests_equipment_unit_id",
                table: "requests",
                column: "equipment_unit_id");

            migrationBuilder.CreateIndex(
                name: "ix_requests_laboratory_id",
                table: "requests",
                column: "laboratory_id");

            migrationBuilder.CreateIndex(
                name: "ix_requests_modified_by_id",
                table: "requests",
                column: "modified_by_id");

            migrationBuilder.CreateIndex(
                name: "ix_requests_requested_by_id",
                table: "requests",
                column: "requested_by_id");

            migrationBuilder.CreateIndex(
                name: "ix_role_claims_role_id",
                table: "RoleClaims",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "Roles",
                column: "normalized_name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_user_claims_user_id",
                table: "UserClaims",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_logins_user_id",
                table: "UserLogins",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_roles_role_id",
                table: "UserRoles",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "Users",
                column: "normalized_email");

            migrationBuilder.CreateIndex(
                name: "ix_users_created_by_id",
                table: "Users",
                column: "created_by_id");

            migrationBuilder.CreateIndex(
                name: "ix_users_identity_card",
                table: "Users",
                column: "identity_card",
                unique: true,
                filter: "status != 2");

            migrationBuilder.CreateIndex(
                name: "ix_users_modified_by_id",
                table: "Users",
                column: "modified_by_id");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "Users",
                column: "normalized_user_name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_verifications_created_by_id",
                table: "verifications",
                column: "created_by_id");

            migrationBuilder.CreateIndex(
                name: "ix_verifications_equipment_unit_id",
                table: "verifications",
                column: "equipment_unit_id");

            migrationBuilder.CreateIndex(
                name: "ix_verifications_modified_by_id",
                table: "verifications",
                column: "modified_by_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "cost_details");

            migrationBuilder.DropTable(
                name: "equipment_state_histories");

            migrationBuilder.DropTable(
                name: "Externs");

            migrationBuilder.DropTable(
                name: "Interns");

            migrationBuilder.DropTable(
                name: "loans");

            migrationBuilder.DropTable(
                name: "maintenance_plans");

            migrationBuilder.DropTable(
                name: "RoleClaims");

            migrationBuilder.DropTable(
                name: "UserClaims");

            migrationBuilder.DropTable(
                name: "UserLogins");

            migrationBuilder.DropTable(
                name: "UserRoles");

            migrationBuilder.DropTable(
                name: "UserTokens");

            migrationBuilder.DropTable(
                name: "verifications");

            migrationBuilder.DropTable(
                name: "maintenances");

            migrationBuilder.DropTable(
                name: "Roles");

            migrationBuilder.DropTable(
                name: "maintenance_types");

            migrationBuilder.DropTable(
                name: "People");

            migrationBuilder.DropTable(
                name: "requests");

            migrationBuilder.DropTable(
                name: "equipment_units");

            migrationBuilder.DropTable(
                name: "careers");

            migrationBuilder.DropTable(
                name: "equipments");

            migrationBuilder.DropTable(
                name: "laboratories");

            migrationBuilder.DropTable(
                name: "cities");

            migrationBuilder.DropTable(
                name: "equipment_types");

            migrationBuilder.DropTable(
                name: "faculties");

            migrationBuilder.DropTable(
                name: "countries");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
