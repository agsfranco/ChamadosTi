using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChamadosTi.Data
{
    /// <inheritdoc />
    public partial class AdicionadoCampoEspecialidadeIdEmCategoria : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PrazoResolucaoSla",
                table: "Categorias");

            migrationBuilder.AlterColumn<int>(
                name: "EspecialidadeId",
                table: "DadosTecnicos",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "EspecialidadeId",
                table: "Categorias",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Categorias_EspecialidadeId",
                table: "Categorias",
                column: "EspecialidadeId");

            migrationBuilder.AddForeignKey(
                name: "FK_Categorias_Especialidades_EspecialidadeId",
                table: "Categorias",
                column: "EspecialidadeId",
                principalTable: "Especialidades",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Categorias_Especialidades_EspecialidadeId",
                table: "Categorias");

            migrationBuilder.DropIndex(
                name: "IX_Categorias_EspecialidadeId",
                table: "Categorias");

            migrationBuilder.DropColumn(
                name: "EspecialidadeId",
                table: "Categorias");

            migrationBuilder.AlterColumn<int>(
                name: "EspecialidadeId",
                table: "DadosTecnicos",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PrazoResolucaoSla",
                table: "Categorias",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
