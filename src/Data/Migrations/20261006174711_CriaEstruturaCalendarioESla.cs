using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChamadosTi.Data
{
    /// <inheritdoc />
    public partial class CriaEstruturaCalendarioESla : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Feriados",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nome = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Dia = table.Column<DateOnly>(type: "date", nullable: false),
                    Ativo = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Feriados", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Horarios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DiaDaSemana = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    JornadaInicio = table.Column<TimeOnly>(type: "time", nullable: false),
                    JornadaFim = table.Column<TimeOnly>(type: "time", nullable: false),
                    AlmocoInicio = table.Column<TimeOnly>(type: "time", nullable: false),
                    AlmocoFim = table.Column<TimeOnly>(type: "time", nullable: false),
                    Ativo = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Horarios", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SetoresHorarios",
                columns: table => new
                {
                    SetorId = table.Column<int>(type: "int", nullable: false),
                    HorarioId = table.Column<int>(type: "int", nullable: false),
                    Ativo = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SetoresHorarios", x => new { x.SetorId, x.HorarioId });
                    table.ForeignKey(
                        name: "FK_SetoresHorarios_Horarios_HorarioId",
                        column: x => x.HorarioId,
                        principalTable: "Horarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SetoresHorarios_Setores_SetorId",
                        column: x => x.SetorId,
                        principalTable: "Setores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SetoresHorarios_HorarioId",
                table: "SetoresHorarios",
                column: "HorarioId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Feriados");

            migrationBuilder.DropTable(
                name: "SetoresHorarios");

            migrationBuilder.DropTable(
                name: "Horarios");
        }
    }
}
