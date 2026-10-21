using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChamadosTi.Data
{
    /// <inheritdoc />
    public partial class CorrigidoAuditoriaTicketNome : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Status",
                columns: new[] { "Id", "Codigo", "Nome" },
                values: new object[] { 6, 6, "Status_Cancelado" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Status",
                keyColumn: "Id",
                keyValue: 6);
        }
    }
}
