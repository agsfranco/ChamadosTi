using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChamadosTi.Data
{
    /// <inheritdoc />
    public partial class AdicionadoCampoPrioridadePadrao : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Padrao",
                table: "Prioridades",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Padrao",
                table: "Prioridades");
        }
    }
}
