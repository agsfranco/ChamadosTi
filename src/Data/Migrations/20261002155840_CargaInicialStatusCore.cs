using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ChamadosTi.Data
{
    /// <inheritdoc />
    public partial class CargaInicialStatusCore : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Ativo",
                table: "Status");

            migrationBuilder.InsertData(
                table: "Status",
                columns: new[] { "Id", "Codigo", "Nome" },
                values: new object[,]
                {
                    { 1, 1, "Status_Aberto" },
                    { 2, 2, "Status_EmAtendimento" },
                    { 3, 3, "Status_Pendente" },
                    { 4, 4, "Status_Resolvido" },
                    { 5, 5, "Status_Fechado" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Status",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Status",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Status",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Status",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "Status",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.AddColumn<bool>(
                name: "Ativo",
                table: "Status",
                type: "bit",
                nullable: false,
                defaultValue: true);
        }
    }
}
