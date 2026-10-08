using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChamadosTi.Data
{
    /// <inheritdoc />
    public partial class AdicionadoCampoTiEmSetores : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Ti",
                table: "Setores",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Ti",
                table: "Setores");
        }
    }
}
